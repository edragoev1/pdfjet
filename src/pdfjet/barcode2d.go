package pdfjet

/**
 * barcode2d.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"github.com/edragoev1/pdfjet/src/pdfjet/ecclevel5"
	"github.com/edragoev1/pdfjet/src/pdfjet/pdf417"
	"github.com/edragoev1/pdfjet/src/pdfjet/textcompact"
	"strconv"
)

// BarCode2D describes PDF417 2D barcodes.
// Please see Example_12.
type BarCode2D struct {
	x1, y1    float32
	w1        float32
	h1        float32
	rows      int
	cols      int
	codewords []int
	str       string
}

// Constants
const (
	ALPHA        = 0x08
	LOWER        = 0x04
	MIXED        = 0x02
	PUNCT        = 0x01
	LatchToLower = 27
	ShiftToAlpha = 27
	LatchToMixed = 28
	LatchToAlpha = 28
	ShiftToPunct = 29
)

// NewBarCode2D constructor for 2D barcodes.
// @param str the specified string.
func NewBarCode2D(str string) *BarCode2D {
	barcode := new(BarCode2D)
	barcode.str = str
	barcode.w1 = 0.75
	barcode.h1 = 3.0 * barcode.w1
	barcode.rows = 50
	barcode.cols = 18
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
		if k == 1 {
			lf = cf + (barcode.rows-1)/3
			lr = cf + (barcode.cols - 1)
		} else if k == 2 {
			lf = cf + 3*compression + (barcode.rows-1)%3
			lr = cf + (barcode.rows-1)/3
		} else if k == 3 {
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

	dataLen := (barcode.rows * barcode.cols) - len(ecclevel5.TABLE)
	for i := 0; i < dataLen; i++ {
		buffer[i] = 900 // The default pad codeword
	}
	buffer[0] = dataLen

	barcode.addData(buffer, dataLen)
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
func (barcode *BarCode2D) SetLocation(x, y float32) {
	barcode.x1 = x
	barcode.y1 = y
}

// SetPosition sets the barcode location on the page.
func (barcode *BarCode2D) SetPosition(x, y float32) {
	barcode.SetLocation(x, y)
}

// SetModuleWidth sets the module width for this barcode.
// This changes the barcode size while preserving the aspect.
// Use value between 0.5 and 0.75
// If the value is too small some scanners may have difficulty reading the barcode.
func (barcode *BarCode2D) SetModuleWidth(width float32) {
	barcode.w1 = width
	barcode.h1 = 3 * barcode.w1
}

func (barcode *BarCode2D) textToArrayOfIntegers() []int {
	list := make([]int, 0)

	currentMode := ALPHA
	runes := []rune(barcode.str)
	for _, ch := range runes {
		if ch == 0x20 {
			list = append(list, 26) // The codeword for space
			continue
		}

		value := textcompact.TABLE[ch][1]
		mode := textcompact.TABLE[ch][2]
		if mode == currentMode {
			list = append(list, value)
		} else {
			if mode == ALPHA && currentMode == LOWER {
				list = append(list, ShiftToAlpha)
				list = append(list, value)
			} else if mode == ALPHA && currentMode == MIXED {
				list = append(list, LatchToAlpha)
				list = append(list, value)
				currentMode = mode
			} else if mode == LOWER && currentMode == ALPHA {
				list = append(list, LatchToLower)
				list = append(list, value)
				currentMode = mode
			} else if mode == LOWER && currentMode == MIXED {
				list = append(list, LatchToLower)
				list = append(list, value)
				currentMode = mode
			} else if mode == MIXED && currentMode == ALPHA {
				list = append(list, LatchToMixed)
				list = append(list, value)
				currentMode = mode
			} else if mode == MIXED && currentMode == LOWER {
				list = append(list, LatchToMixed)
				list = append(list, value)
				currentMode = mode
			} else if mode == PUNCT && currentMode == ALPHA {
				list = append(list, ShiftToPunct)
				list = append(list, value)
			} else if mode == PUNCT && currentMode == LOWER {
				list = append(list, ShiftToPunct)
				list = append(list, value)
			} else if mode == PUNCT && currentMode == MIXED {
				list = append(list, ShiftToPunct)
				list = append(list, value)
			}
		}
	}

	return list
}

func (barcode *BarCode2D) addData(buf []int, dataLen int) {
	list := barcode.textToArrayOfIntegers()
	bi := 1 // buffer index = 1 to skip the Symbol Length Descriptor
	hi := 0
	lo := 0
	for i := 0; i < len(list); i += 2 {
		hi = list[i]
		if i+1 == len(list) {
			lo = ShiftToPunct // Pad
		} else {
			lo = list[i+1]
		}
		bi++
		if bi == dataLen {
			break
		}
		buf[bi] = 30*hi + lo
	}
}

func (barcode *BarCode2D) addECC(buf []int) {
	ecc := make([]int, len(ecclevel5.TABLE))
	t2 := 0
	t3 := 0
	dataLen := len(buf) - len(ecc)
	for i := 0; i < dataLen; i++ {
		t1 := (buf[i] + ecc[len(ecc)-1]) % 929
		for j := len(ecc) - 1; j > 0; j-- {
			t2 := (t1 * ecclevel5.TABLE[j]) % 929
			t3 := 929 - t2
			ecc[j] = (ecc[j-1] + t3) % 929
		}
		t2 = (t1 * ecclevel5.TABLE[0]) % 929
		t3 = 929 - t2
		ecc[0] = t3 % 929
	}
	for i := 0; i < len(ecc); i++ {
		if ecc[i] != 0 {
			buf[(len(buf)-1)-i] = 929 - ecc[i]
		}
	}
}

// DrawOn draws this barcode on the specified page.
// @return x and y coordinates of the bottom right corner of this component.
func (barcode *BarCode2D) DrawOn(page *Page) []float32 {
	x := barcode.x1
	y := barcode.y1

	startSymbol := []int{8, 1, 1, 1, 1, 1, 1, 3}
	for i := 0; i < len(startSymbol); i++ {
		n := float32(startSymbol[i])
		if i%2 == 0 {
			barcode.drawBar(page, x, y, n*barcode.w1, float32(barcode.rows)*barcode.h1)
		}
		x += n * barcode.w1
	}
	barcode.x1 = x

	k := 1 // Cluster index
	for i := 0; i < len(barcode.codewords); i++ {
		row := barcode.codewords[i]
		symbol := strconv.Itoa(pdf417.TABLE[row][k])
		runes := []rune(symbol)
		for j := 0; j < 8; j++ {
			n := float32(runes[j] - 0x30)
			if j%2 == 0 {
				barcode.drawBar(page, x, y, n*barcode.w1, barcode.h1)
			}
			x += n * barcode.w1
		}
		if i == (len(barcode.codewords) - 1) {
			break
		}
		if (i+1)%(barcode.cols+2) == 0 {
			x = barcode.x1
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
			barcode.drawBar(page, x, y, n*barcode.w1, float32(barcode.rows)*barcode.h1)
		}
		x += n * barcode.w1
	}

	return []float32{x, y + barcode.h1*float32(barcode.rows)}
}

func (barcode *BarCode2D) drawBar(page *Page, x, y, w, h float32) {
	page.SetPenWidth(w)
	page.MoveTo(x+w/2, y)
	page.LineTo(x+w/2, y+h)
	page.StrokePath()
}
