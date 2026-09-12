// pdf417.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pdf417 creates PDF417 barcodes.
package pdf417

import (
	"log"
	"strconv"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
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
// @param str the specified string.
func NewPDF417(str string) *PDF417 {
	barcode := new(PDF417)
	barcode.str = str
	barcode.w1 = 0.75
	barcode.h1 = 3.0 * barcode.w1
	barcode.rows = 50
	barcode.cols = 18
	barcode.codewords = make([]int, barcode.rows*(barcode.cols+2))

	for _, ch := range str {
		if ch > 126 {
			log.Fatal("The string contains unencodable characters.")
		}
	}

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
func (barcode *PDF417) SetLocation(x, y float32) *PDF417 {
	barcode.x1 = x
	barcode.y1 = y
	return barcode
}

// SetModuleWidth sets the module width for this barcode.
// This changes the barcode size while preserving the aspect.
// Use value between 0.5 and 0.75
// If the value is too small some scanners may have difficulty reading the barcode.
func (barcode *PDF417) SetModuleWidth(width float32) *PDF417 {
	barcode.w1 = width
	barcode.h1 = 3 * barcode.w1
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

func (barcode *PDF417) addData(buf []int, dataLen int) {
	list := barcode.textToArrayOfIntegers()
	bi := 1 // buffer index = 1 to skip the Symbol Length Descriptor
	hi := 0
	lo := 0
	for i := 0; i < len(list); i += 2 {
		hi = list[i]
		if i+1 == len(list) {
			lo = shiftToPunct // Pad
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

// DrawOn draws this barcode on the specified page.
// @return x and y coordinates of the bottom right corner of this component.
func (barcode *PDF417) DrawOn(page *pdfjet.Page) []float32 {
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
		symbol := strconv.Itoa(patternTable[row][k])
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

func (barcode *PDF417) drawBar(page *pdfjet.Page, x, y, w, h float32) {
	page.AddArtifactBMC()
	page.SetPenWidth(w)
	page.MoveTo(x+w/2, y)
	page.LineTo(x+w/2, y+h)
	page.StrokePath()
	page.AddEMC()
}
