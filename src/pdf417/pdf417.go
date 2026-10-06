// pdf417.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pdf417 creates PDF417 barcodes.
package pdf417

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"strconv"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// PDF417 is used to generate PDF417 2D barcodes.
//
// The bars are drawn from the location set with SetLocation. ISO/IEC 15438
// asks for a quiet zone of at least two modules (twice the module width) on
// all four sides of the symbol, so leave that much space around it; scanners
// reject symbols with less.
//
// Please see Example_12.
type PDF417 struct {
	x1, y1         float32
	w1             float32
	h1             float32
	rows           int
	cols           int
	codewords      []int
	str            string
	altDescription string
}

// Constants
const (
	alpha        = 0x08
	lower        = 0x04
	mixed        = 0x02
	punct        = 0x01
	latchToLower = 27
	shiftToAlpha = 27
	latchToMixed = 28
	latchToAlpha = 28
	shiftToPunct = 29
)

// NewPDF417 constructor for 2D barcodes.
// The symbol has 18 columns and as many rows as the string needs, up to the
// 928 codewords a PDF417 symbol can hold: 864 data codewords, or about 1,300
// characters of mixed text, with the error correction level 5 used here.
// The string is ASCII, and a control character other than HT, LF and CR
// takes a codeword of its own and one more, in byte compaction.
// It panics if there are unencodable characters or the string does not fit in a symbol.
// @param str the specified string.
func NewPDF417(str string) *PDF417 {
	barcode := new(PDF417)
	barcode.str = str
	barcode.w1 = 0.75
	barcode.h1 = float32(3.0 * barcode.w1)
	barcode.cols = 18

	for _, ch := range str {
		if ch > 126 {
			panic("The string contains unencodable characters.")
		}
	}

	// The data codewords, after the symbol length descriptor
	list := barcode.dataCodewords()
	dataCodewords := 1 + len(list)
	barcode.rows = (dataCodewords + len(l5ECCInstance.Table) + barcode.cols - 1) / barcode.cols
	if barcode.rows < 3 {
		barcode.rows = 3
	}
	if barcode.rows*barcode.cols > 928 {
		panic("The string is too long for a PDF417 barcode.")
	}
	barcode.codewords = make([]int, barcode.rows*(barcode.cols+2))

	lfBuffer := make([]int, barcode.rows)
	lrBuffer := make([]int, barcode.rows)
	buffer := make([]int, barcode.rows*barcode.cols)

	// Left and right row indicators - see page 34 of the ISO specification
	compression := 5 // Compression Level
	k := 1
	for i := 0; i < barcode.rows; i++ {
		lf := 0
		lr := 0
		cf := 30 * (i / 3)
		switch k {
		case 1:
			lf = cf + (barcode.rows-1)/3
			lr = cf + (barcode.cols - 1)
		case 2:
			lf = cf + 3*compression + (barcode.rows-1)%3
			lr = cf + (barcode.rows-1)/3
		case 3:
			lf = cf + (barcode.cols - 1)
			lr = cf + 3*compression + (barcode.rows-1)%3
		}
		lfBuffer[i] = lf
		lrBuffer[i] = lr
		k++
		if k == 4 {
			k = 1
		}
	}

	dataLen := (barcode.rows * barcode.cols) - len(l5ECCInstance.Table)
	for i := 0; i < dataLen; i++ {
		buffer[i] = 900 // The default pad codeword
	}
	buffer[0] = dataLen
	copy(buffer[1:], list)

	barcode.addECC(buffer)

	for i := 0; i < barcode.rows; i++ {
		index := (barcode.cols + 2) * i
		barcode.codewords[index] = lfBuffer[i]
		for j := 0; j < barcode.cols; j++ {
			barcode.codewords[index+j+1] = buffer[barcode.cols*i+j]
		}
		barcode.codewords[index+barcode.cols+1] = lrBuffer[i]
	}

	return barcode
}

// SetLocation sets the location of this barcode on the page.
// @param x the x coordinate of the top left corner of the barcode.
// @param y the y coordinate of the top left corner of the barcode.
func (barcode *PDF417) SetLocation(x, y float32) pdfjet.Drawable {
	barcode.x1 = x
	barcode.y1 = y
	return barcode
}

// SetModuleLength sets the module length of this barcode, the width of its narrowest bar.
// This changes the barcode size while preserving the aspect.
// Use value between 0.5 and 0.75
// If the value is too small some scanners may have difficulty reading the barcode.
func (barcode *PDF417) SetModuleLength(moduleLength float32) *PDF417 {
	barcode.w1 = moduleLength
	barcode.h1 = float32(3 * barcode.w1)
	return barcode
}

func (barcode *PDF417) textToArrayOfIntegers() []int {
	list := make([]int, 0)

	currentMode := alpha
	for _, ch := range barcode.str {
		if ch == 0x20 {
			list = append(list, 26) // The codeword for space
			continue
		}
		if isByteShifted(ch) {
			list = append(list, -1-int(ch)) // Below 0, see dataCodewords
			continue
		}

		value := textCompactInstance.Table[ch][1]
		mode := textCompactInstance.Table[ch][2]
		if mode == currentMode {
			list = append(list, value)
		} else {
			if mode == alpha && currentMode == lower {
				list = append(list, shiftToAlpha)
				list = append(list, value)
			} else if mode == alpha && currentMode == mixed {
				list = append(list, latchToAlpha)
				list = append(list, value)
				currentMode = mode
			} else if mode == lower && currentMode == alpha {
				list = append(list, latchToLower)
				list = append(list, value)
				currentMode = mode
			} else if mode == lower && currentMode == mixed {
				list = append(list, latchToLower)
				list = append(list, value)
				currentMode = mode
			} else if mode == mixed && currentMode == alpha {
				list = append(list, latchToMixed)
				list = append(list, value)
				currentMode = mode
			} else if mode == mixed && currentMode == lower {
				list = append(list, latchToMixed)
				list = append(list, value)
				currentMode = mode
			} else if mode == punct && currentMode == alpha {
				list = append(list, shiftToPunct)
				list = append(list, value)
			} else if mode == punct && currentMode == lower {
				list = append(list, shiftToPunct)
				list = append(list, value)
			} else if mode == punct && currentMode == mixed {
				list = append(list, shiftToPunct)
				list = append(list, value)
			}
		}
	}

	return list
}

// byteShift is the codeword that shifts from text compaction to byte
// compaction for the one codeword after it.
const byteShift = 913

// isByteShifted tells if the character is a control character that text
// compaction has no value for, and that is so encoded in byte compaction.
func isByteShifted(ch rune) bool {
	return ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r'
}

// dataCodewords returns the data codewords of the string in text compaction,
// two values to a codeword; a control character other than HT, LF and CR is
// the byte compaction shift and its byte, which starts a codeword, so the
// value before it may be padded, and after which text compaction goes on in
// the submode it was in.
func (barcode *PDF417) dataCodewords() []int {
	list := barcode.textToArrayOfIntegers()
	codewords := make([]int, 0, len(list)/2+1)
	hi := -1 // The first value of a codeword, if any
	for _, value := range list {
		if value < 0 {
			if hi != -1 {
				codewords = append(codewords, 30*hi+shiftToPunct) // Pad
				hi = -1
			}
			codewords = append(codewords, byteShift, -1-value)
		} else if hi == -1 {
			hi = value
		} else {
			codewords = append(codewords, 30*hi+value)
			hi = -1
		}
	}
	if hi != -1 {
		codewords = append(codewords, 30*hi+shiftToPunct) // Pad
	}
	return codewords
}

func (barcode *PDF417) addECC(buf []int) {
	ecc := make([]int, len(l5ECCInstance.Table))
	t2 := 0
	t3 := 0
	dataLen := len(buf) - len(ecc)
	for i := 0; i < dataLen; i++ {
		t1 := (buf[i] + ecc[len(ecc)-1]) % 929
		for j := len(ecc) - 1; j > 0; j-- {
			t2 := (t1 * l5ECCInstance.Table[j]) % 929
			t3 := 929 - t2
			ecc[j] = (ecc[j-1] + t3) % 929
		}
		t2 = (t1 * l5ECCInstance.Table[0]) % 929
		t3 = 929 - t2
		ecc[0] = t3 % 929
	}
	for i := 0; i < len(ecc); i++ {
		if ecc[i] != 0 {
			buf[(len(buf)-1)-i] = 929 - ecc[i]
		}
	}
}

// SetAltDescription sets what the barcode says for a screen reader: a tagged
// document, PDF/UA or a PDF/A of level A, then has the barcode as a figure of
// that description. Without one, its bars are decoration, which a screen
// reader skips.
func (barcode *PDF417) SetAltDescription(altDescription string) *PDF417 {
	barcode.altDescription = altDescription
	return barcode
}

// DrawOn draws this barcode on the specified page. The bars are black, and
// the pen of the page is as it was after it.
// @return x and y coordinates of the bottom right corner of this component.
func (barcode *PDF417) DrawOn(page *pdfjet.Page) [2]float32 {
	if page != nil {
		// Described, the barcode is a figure of a tagged document; not
		// described, its bars, which carry no text, are decoration.
		if barcode.altDescription != "" {
			page.AddBDC(structelem.Figure, "", "", barcode.altDescription)
		} else {
			page.AddArtifactBMC()
		}
		page.SaveGraphicsState()
		page.SetPenColor(color.Black)
	}
	xy := barcode.drawBars(page)
	if page != nil {
		page.RestoreGraphicsState()
		if barcode.altDescription != "" {
			page.SetFigureBoundingBox(barcode.x1, barcode.y1, xy[0]-barcode.x1, xy[1]-barcode.y1)
		}
		page.AddEMC()
	}
	return xy
}

// drawBars draws the bars, or measures them with no page, and returns the
// bottom right corner of the barcode.
func (barcode *PDF417) drawBars(page *pdfjet.Page) [2]float32 {
	x := barcode.x1
	y := barcode.y1

	startSymbol := []int{8, 1, 1, 1, 1, 1, 1, 3}
	for i := 0; i < len(startSymbol); i++ {
		n := float32(startSymbol[i])
		if i%2 == 0 {
			barcode.drawBar(page, x, y, float32(n*barcode.w1), float32(float32(barcode.rows)*barcode.h1))
		}
		x += float32(n * barcode.w1)
	}
	x0 := x // Where the codewords of each row start

	k := 1 // Cluster index
	for i := 0; i < len(barcode.codewords); i++ {
		row := barcode.codewords[i]
		symbol := strconv.Itoa(patternTable[row][k])
		runes := []rune(symbol)
		for j := 0; j < 8; j++ {
			n := float32(runes[j] - 0x30)
			if j%2 == 0 {
				barcode.drawBar(page, x, y, float32(n*barcode.w1), barcode.h1)
			}
			x += float32(n * barcode.w1)
		}
		if i == (len(barcode.codewords) - 1) {
			break
		}
		if (i+1)%(barcode.cols+2) == 0 {
			x = x0
			y += barcode.h1
			k++
			if k == 4 {
				k = 1
			}
		}
	}

	y = barcode.y1
	endSymbol := []int{7, 1, 1, 3, 1, 1, 1, 2, 1}
	for i := 0; i < len(endSymbol); i++ {
		n := float32(endSymbol[i])
		if i%2 == 0 {
			barcode.drawBar(page, x, y, float32(n*barcode.w1), float32(float32(barcode.rows)*barcode.h1))
		}
		x += float32(n * barcode.w1)
	}

	return [2]float32{x, y + float32(barcode.h1*float32(barcode.rows))}
}

func (barcode *PDF417) drawBar(page *pdfjet.Page, x, y, w, h float32) {
	if page == nil {
		return // Measured, not drawn
	}
	page.SetPenWidth(w)
	page.MoveTo(x+float32(w/2), y)
	page.LineTo(x+float32(w/2), y+h)
	page.StrokePath()
}
