package pdfjet

/**
 *  barcode.go
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
	"log"
	"math"
	"github.com/edragoev1/pdfjet/src/code128"
	"strconv"
	"strings"
)

// BarCode describes one dimentional barcodes - UPC, Code 39 and Code 128.
// Please see Example_11.
type BarCode struct {
	barcodeType     int
	text            string
	x1              float32
	y1              float32
	m1              float32
	barHeightFactor float32
	direction       int
	font            *Font
	tableA          []int
	tableB          map[byte]string
}

// Constants
const (
	Upc = iota
	CODE128
	CODE39
)

// Constants
const (
	LeftToRight = iota
	TopToBottom
	BottomToTop
)

// NewBarCode constructs barcode objects.
// @param type the type of the barcode.
// @param str the content string of the barcode.
func NewBarCode(barcodeType int, text string) *BarCode {
	barcode := new(BarCode)
	barcode.barcodeType = barcodeType
	barcode.text = text
	barcode.x1 = 0.0
	barcode.y1 = 0.0
	barcode.m1 = 0.75 // Module length
	barcode.barHeightFactor = 50.0
	barcode.direction = LeftToRight
	barcode.tableA = []int{3211, 2221, 2122, 1411, 1132, 1231, 1114, 1312, 1213, 3112}

	barcode.tableB = make(map[byte]string)
	barcode.tableB['*'] = "bWbwBwBwb"
	barcode.tableB['-'] = "bWbwbwBwB"
	barcode.tableB['$'] = "bWbWbWbwb"
	barcode.tableB['%'] = "bwbWbWbWb"
	barcode.tableB[' '] = "bWBwbwBwb"
	barcode.tableB['.'] = "BWbwbwBwb"
	barcode.tableB['/'] = "bWbWbwbWb"
	barcode.tableB['+'] = "bWbwbWbWb"
	barcode.tableB['0'] = "bwbWBwBwb"
	barcode.tableB['1'] = "BwbWbwbwB"
	barcode.tableB['2'] = "bwBWbwbwB"
	barcode.tableB['3'] = "BwBWbwbwb"
	barcode.tableB['4'] = "bwbWBwbwB"
	barcode.tableB['5'] = "BwbWBwbwb"
	barcode.tableB['6'] = "bwBWBwbwb"
	barcode.tableB['7'] = "bwbWbwBwB"
	barcode.tableB['8'] = "BwbWbwBwb"
	barcode.tableB['9'] = "bwBWbwBwb"
	barcode.tableB['A'] = "BwbwbWbwB"
	barcode.tableB['B'] = "bwBwbWbwB"
	barcode.tableB['C'] = "BwBwbWbwb"
	barcode.tableB['D'] = "bwbwBWbwB"
	barcode.tableB['E'] = "BwbwBWbwb"
	barcode.tableB['F'] = "bwBwBWbwb"
	barcode.tableB['G'] = "bwbwbWBwB"
	barcode.tableB['H'] = "BwbwbWBwb"
	barcode.tableB['I'] = "bwBwbWBwb"
	barcode.tableB['J'] = "bwbwBWBwb"
	barcode.tableB['K'] = "BwbwbwbWB"
	barcode.tableB['L'] = "bwBwbwbWB"
	barcode.tableB['M'] = "BwBwbwbWb"
	barcode.tableB['N'] = "bwbwBwbWB"
	barcode.tableB['O'] = "BwbwBwbWb"
	barcode.tableB['P'] = "bwBwBwbWb"
	barcode.tableB['Q'] = "bwbwbwBWB"
	barcode.tableB['R'] = "BwbwbwBWb"
	barcode.tableB['S'] = "bwBwbwBWb"
	barcode.tableB['T'] = "bwbwBwBWb"
	barcode.tableB['U'] = "BWbwbwbwB"
	barcode.tableB['V'] = "bWBwbwbwB"
	barcode.tableB['W'] = "BWBwbwbwb"
	barcode.tableB['X'] = "bWbwBwbwB"
	barcode.tableB['Y'] = "BWbwBwbwb"
	barcode.tableB['Z'] = "bWBwBwbwb"

	return barcode
}

// SetLocation sets the location where this barcode will be drawn on the page.
// @param x1 the x coordinate of the top left corner of the barcode.
// @param y1 the y coordinate of the top left corner of the barcode.
func (barcode *BarCode) SetLocation(x1, y1 float32) {
	barcode.x1 = x1
	barcode.y1 = y1
}

// SetModuleLength sets the module length of this barcode.
// The default value is 0.75
func (barcode *BarCode) SetModuleLength(moduleLength float32) {
	barcode.m1 = moduleLength
}

// SetBarHeightFactor sets the bar height factor.
// The height of the bars is the moduleLength * barHeightFactor
// The default value is 50.0f
func (barcode *BarCode) SetBarHeightFactor(barHeightFactor float32) {
	barcode.barHeightFactor = barHeightFactor
}

// SetDirection sets the drawing direction for this font.
// @param direction the specified direction.
func (barcode *BarCode) SetDirection(direction int) {
	barcode.direction = direction
}

// SetFont sets the font to be used with this barcode.
// @param font the specified font.
func (barcode *BarCode) SetFont(font *Font) {
	barcode.font = font
}

// DrawOn draws this barcode on the specified page.
func (barcode *BarCode) DrawOn(page *Page) []float32 {
	if barcode.barcodeType == Upc {
		return barcode.drawCodeUPC(page, barcode.x1, barcode.y1)
	} else if barcode.barcodeType == CODE128 {
		return barcode.drawCode128(page, barcode.x1, barcode.y1)
	} else if barcode.barcodeType == CODE39 {
		return barcode.drawCode39(page, barcode.x1, barcode.y1)
	} else {
		log.Fatal("Unsupported Barcode Type.")
	}
	return []float32{0.0, 0.0}
}

// drawOnPageAtLocation draws this barcode on the specified page at the spacified location.
func (barcode *BarCode) drawOnPageAtLocation(page *Page, x1, y1 float32) []float32 {
	if barcode.barcodeType == Upc {
		return barcode.drawCodeUPC(page, x1, y1)
	} else if barcode.barcodeType == CODE128 {
		return barcode.drawCode128(page, x1, y1)
	} else if barcode.barcodeType == CODE39 {
		return barcode.drawCode39(page, x1, y1)
	} else {
		log.Fatal("Unsupported Barcode Type.")
	}
	return []float32{0.0, 0.0}
}

func (barcode *BarCode) drawCodeUPC(page *Page, x1, y1 float32) []float32 {
	x := x1
	y := y1
	h := barcode.m1 * barcode.barHeightFactor // Barcode height when drawn horizontally

	// Calculate the check digit:
	// 1. Add the digits in the odd-numbered positions (first, third, fifth, etc.)
	// together and multiply by three.
	// 2. Add the digits in the even-numbered positions (second, fourth, sixth, etc.)
	// to the result.
	// 3. Subtract the result modulo 10 from ten.
	// 4. The answer modulo 10 is the check digit.
	sum := 0
	for i := 0; i < 11; i += 2 {
		sum += int(barcode.text[i]) - 48
	}
	sum *= 3
	for i := 1; i < 11; i += 2 {
		sum += int(barcode.text[i]) - 48
	}
	reminder := sum % 10
	checkDigit := (10 - reminder) % 10
	barcode.text += strconv.Itoa(checkDigit)

	x = barcode.drawEGuard(page, x, y, barcode.m1, h+8)
	for i := 0; i < 6; i++ {
		digit := barcode.text[i] - 0x30
		// page.drawString(Integer.toString(digit), x + 1, y + h + 12)
		symbol := strconv.Itoa(barcode.tableA[digit])
		for j := 0; j < len(symbol); j++ {
			n := symbol[j] - 0x30
			if j%2 != 0 {
				barcode.drawVertBar(page, x, y, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
	}
	x = barcode.drawMGuard(page, x, y, barcode.m1, h+8)
	for i := 6; i < 12; i++ {
		digit := barcode.text[i] - 0x30
		// page.drawString(Integer.toString(digit), x + 1, y + h + 12)
		symbol := strconv.Itoa(barcode.tableA[digit])
		for j := 0; j < len(symbol); j++ {
			n := symbol[j] - 0x30
			if j%2 == 0 {
				barcode.drawVertBar(page, x, y, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
	}
	x = barcode.drawEGuard(page, x, y, barcode.m1, h+8)

	xy := []float32{x, y}
	if barcode.font != nil {
		label := string(barcode.text[0]) +
			"  " +
			string(barcode.text[1]) +
			string(barcode.text[2]) +
			string(barcode.text[3]) +
			string(barcode.text[4]) +
			string(barcode.text[5]) +
			"   " +
			string(barcode.text[6]) +
			string(barcode.text[7]) +
			string(barcode.text[8]) +
			string(barcode.text[9]) +
			string(barcode.text[10]) +
			"  " +
			string(barcode.text[11])
		fontSize := barcode.font.GetSize()
		barcode.font.SetSize(10)

		textLine := NewTextLine(barcode.font, label)
		textLine.SetLocation(
			barcode.x1+((x-barcode.x1)-barcode.font.stringWidth(label))/2,
			barcode.y1+h+barcode.font.GetBodyHeight())
		xy = textLine.DrawOn(page)
		xy[0] = float32(math.Max(float64(x), float64(xy[0])))
		xy[1] = float32(math.Max(float64(y), float64(xy[1])))

		barcode.font.SetSize(fontSize)
	}

	return xy
}

func (barcode *BarCode) drawEGuard(page *Page, x, y, m1, h float32) float32 {
	if page != nil {
		// 101
		barcode.drawBar(page, x+(0.5*m1), y, m1, h)
		barcode.drawBar(page, x+(2.5*m1), y, m1, h)
	}
	return (x + (3.0 * m1))
}

func (barcode *BarCode) drawMGuard(page *Page, x, y, m1, h float32) float32 {
	if page != nil {
		// 01010
		barcode.drawBar(page, x+(1.5*m1), y, m1, h)
		barcode.drawBar(page, x+(3.5*m1), y, m1, h)
	}
	return (x + (5.0 * m1))
}

func (barcode *BarCode) drawBar(page *Page, x, y, m1, h float32) {
	if page != nil {
		page.SetPenWidth(m1)
		page.MoveTo(x, y)
		page.LineTo(x, y+h)
		page.StrokePath()
	}
}

func (barcode *BarCode) drawCode128(page *Page, x1, y1 float32) []float32 {
	x := x1
	y := y1

	w := barcode.m1
	h := barcode.m1

	if barcode.direction == TopToBottom {
		w *= barcode.barHeightFactor
	} else if barcode.direction == LeftToRight {
		h *= barcode.barHeightFactor
	}

	list := make([]rune, 0)
	runes := []rune(barcode.text)
	for _, symchar := range runes {
		if symchar < 32 {
			list = append(list, rune(code128.Shift))
			list = append(list, symchar+64)
		} else if symchar < 128 {
			list = append(list, symchar-32)
		} else if symchar < 256 {
			list = append(list, rune(code128.FNC4))
			list = append(list, symchar-160)    // 128 + 32
		} else {
			// list = append(list, rune(31))    // '?'
			list = append(list, rune(256))      // This will generate an exception.
		}
		if len(list) == 48 {
			// Maximum number of data characters is 48
			break
		}
	}

	var buf strings.Builder
	checkDigit := rune(code128.StartB)
	buf.WriteRune(rune(checkDigit))
	for i := 0; i < len(list); i++ {
		codeword := list[i]
		buf.WriteRune(codeword)
		checkDigit += rune(int(codeword) * (i + 1))
	}
	checkDigit %= code128.StartA
	buf.WriteRune(rune(checkDigit))
	buf.WriteRune(rune(code128.Stop))

	runes = []rune(buf.String())
	for _, si := range runes {
		symbol := strconv.Itoa(code128.TABLE[si])
		for i := 0; i < len(symbol); i++ {
			n := float32(symbol[i] - 0x30)
			if i%2 == 0 {
				if barcode.direction == LeftToRight {
					barcode.drawVertBar(page, x, y, n*barcode.m1, h)
				} else if barcode.direction == TopToBottom {
					barcode.drawHorzBar(page, x, y, n*barcode.m1, w)
				}
			}
			if barcode.direction == LeftToRight {
				x += n * barcode.m1
			} else if barcode.direction == TopToBottom {
				y += n * barcode.m1
			}
		}
	}

	xy := []float32{x, y}
	if barcode.font != nil {
		if barcode.direction == LeftToRight {
			text := NewTextLine(barcode.font, barcode.text)
			text.SetLocation(
				barcode.x1+((x-barcode.x1)-barcode.font.stringWidth(barcode.text))/2,
				barcode.y1+h+barcode.font.bodyHeight)
			xy = text.DrawOn(page)
			xy[0] = float32(math.Max(float64(x), float64(xy[0])))
		} else if barcode.direction == TopToBottom {
			text := NewTextLine(barcode.font, barcode.text)
			text.SetLocation(
				x+w+barcode.font.bodyHeight,
				y-((y-barcode.y1)-barcode.font.stringWidth(barcode.text))/2)
			text.SetTextDirection(90)
			xy = text.DrawOn(page)
			xy[1] = float32(math.Max(float64(y), float64(xy[1])))
		}
	}

	return xy
}

func (barcode *BarCode) drawCode39(page *Page, x1, y1 float32) []float32 {
	xy := []float32{0.0, 0.0}

	barcode.text = "*" + barcode.text + "*"
	x := x1
	y := y1
	w := barcode.m1 * barcode.barHeightFactor // Barcode width when drawn vertically
	h := barcode.m1 * barcode.barHeightFactor // Barcode height when drawn horizontally
	if barcode.direction == LeftToRight {
		for i := 0; i < len(barcode.text); i++ {
			code := barcode.tableB[barcode.text[i]]
			if code == "" {
				log.Fatal("The input string '" + barcode.text +
					"' contains characters that are invalid in a Code39 barcode.")
			}
			runes := []rune(code)
			for _, ch := range runes {
				if ch == 'w' {
					x += barcode.m1
				} else if ch == 'W' {
					x += 3 * barcode.m1
				} else if ch == 'b' {
					barcode.drawVertBar(page, x, y, barcode.m1, h)
					x += barcode.m1
				} else if ch == 'B' {
					barcode.drawVertBar(page, x, y, 3*barcode.m1, h)
					x += 3 * barcode.m1
				}
			}
			x += barcode.m1
		}

		if barcode.font != nil {
			text := NewTextLine(barcode.font, barcode.text)
			text.SetLocation(
				barcode.x1+((x-barcode.x1)-barcode.font.stringWidth(barcode.text))/2,
				barcode.y1+h+barcode.font.bodyHeight)
			xy = text.DrawOn(page)
			xy[0] = float32(math.Max(float64(x), float64(xy[0])))
		}
	} else if barcode.direction == TopToBottom {
		for i := 0; i < len(barcode.text); i++ {
			code := barcode.tableB[barcode.text[i]]
			if code == "" {
				log.Fatal("The input string '" + barcode.text +
					"' contains characters that are invalid in a Code39 barcode.")
			}
			runes := []rune(code)
			for _, ch := range runes {
				if ch == 'w' {
					y += barcode.m1
				} else if ch == 'W' {
					y += 3 * barcode.m1
				} else if ch == 'b' {
					barcode.drawHorzBar(page, x, y, barcode.m1, h)
					y += barcode.m1
				} else if ch == 'B' {
					barcode.drawHorzBar(page, x, y, 3*barcode.m1, h)
					y += 3 * barcode.m1
				}
			}
			y += barcode.m1
		}

		if barcode.font != nil {
			text := NewTextLine(barcode.font, barcode.text)
			text.SetLocation(
				x-barcode.font.bodyHeight,
				barcode.y1+((y-barcode.y1)-barcode.font.stringWidth(barcode.text))/2)
			text.SetTextDirection(270)
			xy = text.DrawOn(page)
			xy[0] = float32(math.Max(float64(x), float64(xy[0]))) + w
			xy[1] = float32(math.Max(float64(y), float64(xy[1])))
		}
	} else if barcode.direction == BottomToTop {
		var height float32
		for i := 0; i < len(barcode.text); i++ {
			code := barcode.tableB[barcode.text[i]]
			if code == "" {
				log.Fatal("The input string '" + barcode.text +
					"' contains characters that are invalid in a Code39 barcode.")
			}
			runes := []rune(code)
			for _, ch := range runes {
				if ch == 'w' || ch == 'b' {
					height += barcode.m1
				} else if ch == 'W' || ch == 'B' {
					height += 3 * barcode.m1
				}
			}
			height += barcode.m1
		}
		y += height - barcode.m1

		for i := 0; i < len(barcode.text); i++ {
			code := barcode.tableB[barcode.text[i]]
			runes := []rune(code)
			for _, ch := range runes {
				if ch == 'w' {
					y -= barcode.m1
				} else if ch == 'W' {
					y -= 3 * barcode.m1
				} else if ch == 'b' {
					barcode.drawHorzBar2(page, x, y, barcode.m1, h)
					y -= barcode.m1
				} else if ch == 'B' {
					barcode.drawHorzBar2(page, x, y, 3*barcode.m1, h)
					y -= 3 * barcode.m1
				}
			}
			y -= barcode.m1
		}

		if barcode.font != nil {
			y = barcode.y1 + (height - barcode.m1)
			text := NewTextLine(barcode.font, barcode.text)
			text.SetLocation(
				x+w+barcode.font.bodyHeight,
				y-((y-barcode.y1)-barcode.font.stringWidth(barcode.text))/2)
			text.SetTextDirection(90)
			xy = text.DrawOn(page)
			xy[1] = float32(math.Max(float64(y), float64(xy[1])))
		}
	}

	return xy
}

func (barcode *BarCode) drawVertBar(page *Page, x, y, m1, h float32) {
	if page != nil {
		page.SetPenWidth(m1)
		page.MoveTo(x+m1/2, y)
		page.LineTo(x+m1/2, y+h)
		page.StrokePath()
	}
}

func (barcode *BarCode) drawHorzBar(page *Page, x, y, m1, w float32) {
	if page != nil {
		page.SetPenWidth(m1)
		page.MoveTo(x, y+m1/2)
		page.LineTo(x+w, y+m1/2)
		page.StrokePath()
	}
}

func (barcode *BarCode) drawHorzBar2(page *Page, x, y, m1, w float32) {
	if page != nil {
		page.SetPenWidth(m1)
		page.MoveTo(x, y-m1/2)
		page.LineTo(x+w, y-m1/2)
		page.StrokePath()
	}
}

// GetHeight -- TODO:
func (barcode *BarCode) GetHeight() float32 {
	if barcode.font == nil {
		return barcode.m1 * barcode.barHeightFactor
	}
	return barcode.m1*barcode.barHeightFactor + barcode.font.GetHeight()
}
