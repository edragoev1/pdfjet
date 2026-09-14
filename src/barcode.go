// barcode.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/direction"
	"github.com/edragoev1/pdfjet/v9/src/internal/code128"
)

// Barcode describes one dimensional barcodes - EAN-13, UPC-A, Code 39 and Code 128.
// Please see Example_11.
type Barcode struct {
	barcodeType     int
	text            string
	x1              float32
	y1              float32
	m1              float32
	barHeightFactor float32
	direction       direction.Direction
	font            *Font
	lCode           []string
	gCode           []string
	lgMap           []string
	tableB          map[byte]string
}

// Constants for the barcode type.
const (
	EAN_13 = iota
	UPC_A
	CODE_128
	CODE_39
)

// NewBarcode constructs barcode objects.
// @param barcodeType the type of the barcode.
// @param text the content string of the barcode.
func NewBarcode(barcodeType int, text string) *Barcode {
	barcode := new(Barcode)
	barcode.barcodeType = barcodeType
	barcode.text = text
	barcode.x1 = 0.0
	barcode.y1 = 0.0
	barcode.m1 = 0.75 // Module length
	barcode.barHeightFactor = 50.0
	barcode.direction = direction.LeftToRight

	if barcodeType == UPC_A && (len(text) != 11 || !hasOnlyDigits(text)) {
		panic("UPC-A barcodes must have exactly 11 digits!")
	} else if barcodeType == EAN_13 && (len(text) != 12 || !hasOnlyDigits(text)) {
		panic("EAN-13 barcodes must have exactly 12 digits!")
	}

	barcode.lCode = []string{
		"3211", "2221", "2122", "1411", "1132",
		"1231", "1114", "1312", "1213", "3112"}
	barcode.gCode = make([]string, 10)
	for i := 0; i < 10; i++ {
		barcode.gCode[i] = reverseString(barcode.lCode[i])
	}
	barcode.lgMap = []string{
		"LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
		"LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"}

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

// reverseString returns str with its bytes in reverse order (ASCII digits only, used for gCode).
func reverseString(str string) string {
	b := []byte(str)
	for i, j := 0, len(b)-1; i < j; i, j = i+1, j-1 {
		b[i], b[j] = b[j], b[i]
	}
	return string(b)
}

// SetLocation sets the location where this barcode will be drawn on the page.
// @param x1 the x coordinate of the top left corner of the barcode.
// @param y1 the y coordinate of the top left corner of the barcode.
func (barcode *Barcode) SetLocation(x1, y1 float32) Drawable {
	barcode.x1 = x1
	barcode.y1 = y1
	return barcode
}

// SetModuleLength sets the module length of this barcode.
// The default value is 0.75
func (barcode *Barcode) SetModuleLength(moduleLength float32) *Barcode {
	barcode.m1 = moduleLength
	return barcode
}

// SetBarHeightFactor sets the bar height factor.
// The height of the bars is the moduleLength * barHeightFactor
// The default value is 50.0f
func (barcode *Barcode) SetBarHeightFactor(barHeightFactor float32) *Barcode {
	barcode.barHeightFactor = barHeightFactor
	return barcode
}

// SetDirection sets the direction in which this barcode is drawn.
// @param direction the specified direction.
func (barcode *Barcode) SetDirection(direction direction.Direction) *Barcode {
	barcode.direction = direction
	return barcode
}

// SetFont sets the font to be used with this barcode.
// @param font the specified font.
func (barcode *Barcode) SetFont(font *Font) *Barcode {
	barcode.font = font
	return barcode
}

func hasOnlyDigits(text string) bool {
	for _, ch := range text {
		if ch < '0' || ch > '9' {
			return false
		}
	}
	return true
}

// DrawOn draws this barcode on the specified page.
func (barcode *Barcode) DrawOn(page *Page) [2]float32 {
	var xy [2]float32
	switch barcode.barcodeType {
	case EAN_13:
		xy = barcode.drawCodeEAN13(page, barcode.x1, barcode.y1)
	case UPC_A:
		xy = barcode.drawCodeUPC(page, barcode.x1, barcode.y1)
	case CODE_128:
		xy = barcode.drawCode128(page, barcode.x1, barcode.y1)
	case CODE_39:
		xy = barcode.drawCode39(page, barcode.x1, barcode.y1)
	default:
		panic("Unsupported Barcode Type.")
	}
	return xy
}

// drawOnPageAtLocation draws this barcode on the specified page at the specified location.
func (barcode *Barcode) drawOnPageAtLocation(page *Page, x1, y1 float32) [2]float32 {
	switch barcode.barcodeType {
	case EAN_13:
		return barcode.drawCodeEAN13(page, x1, y1)
	case UPC_A:
		return barcode.drawCodeUPC(page, x1, y1)
	case CODE_128:
		return barcode.drawCode128(page, x1, y1)
	case CODE_39:
		return barcode.drawCode39(page, x1, y1)
	default:
		panic("Unsupported Barcode Type.")
	}
}

func (barcode *Barcode) drawCodeUPC(page *Page, x1, y1 float32) [2]float32 {
	x := x1
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
	// Use a local variable instead of mutating the text field - DrawOn()
	// must be safe to call more than once on the same Barcode instance
	// (e.g. drawing the same barcode on several pages).
	fullText := barcode.text + strconv.Itoa(checkDigit)
	bars := &barcodeBars{x1: x1, y1: y1, length: 95 * barcode.m1, height: h + 8, direction: barcode.direction} // 95 modules

	x = barcode.drawEGuard(page, bars, x, h+8)
	xGroup1Start := x
	for i := 0; i < 6; i++ {
		digit := fullText[i] - 0x30
		str := barcode.lCode[digit]
		for j := 0; j < len(str); j++ {
			n := str[j] - 0x30
			if j%2 != 0 {
				barcode.drawBar(page, bars, x, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
		if i == 0 {
			xGroup1Start = x // Start of the 2nd-6th digit bars (digit 0 is drawn outside)
		}
	}
	xLeftGroupEnd := x
	x = barcode.drawMGuard(page, bars, x, h+8)
	xRightGroupStart := x
	var xGroup2End float32
	for i := 6; i < 12; i++ {
		if i == 11 {
			xGroup2End = x // End of the 7th-11th digit bars (digit 11 is drawn outside)
		}
		digit := fullText[i] - 0x30
		str := barcode.lCode[digit]
		for j := 0; j < len(str); j++ {
			n := str[j] - 0x30
			if j%2 == 0 {
				barcode.drawBar(page, bars, x, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
	}
	x = barcode.drawEGuard(page, bars, x, h+8)

	left := x1
	right := x
	bottom := y1 + h + 8
	if barcode.font != nil {
		// Standard UPC-A layout: the leading (number system) digit and the
		// trailing check digit are printed in the quiet zones outside the
		// guard bars, not centered under them together with the rest of
		// the label. The two groups of 5 digits are each centered under
		// their own bar section.
		firstDigit := fullText[0:1]
		group1 := fullText[1:6]
		group2 := fullText[6:11]
		lastDigit := fullText[11:12]

		fontSize := barcode.font.GetSize()
		barcode.font.SetSize(10)
		yText := y1 + h + barcode.font.GetBodyHeight()
		gap := barcode.font.StringWidth(barcode.font.size, " ")

		left = x1 - gap - barcode.font.StringWidth(barcode.font.size, firstDigit)
		barcode.drawText(page, bars, firstDigit, left, yText)
		barcode.drawText(page, bars, group1,
			xGroup1Start+((xLeftGroupEnd-xGroup1Start)-barcode.font.StringWidth(barcode.font.size, group1))/2,
			yText)
		barcode.drawText(page, bars, group2,
			xRightGroupStart+((xGroup2End-xRightGroupStart)-barcode.font.StringWidth(barcode.font.size, group2))/2,
			yText)
		xy := barcode.drawText(page, bars, lastDigit, x+gap, yText)
		right = xy[0]
		bottom = max(bottom, xy[1])

		barcode.font.SetSize(fontSize)
	}

	return bars.getBottomRight(left, right, bottom)
}

func (barcode *Barcode) drawEGuard(page *Page, bars *barcodeBars, x, h float32) float32 {
	m1 := barcode.m1
	if page != nil {
		// 101
		page.AddArtifactBMC()
		barcode.strokeBar(page, bars, x+(0.5*m1), m1, h)
		barcode.strokeBar(page, bars, x+(2.5*m1), m1, h)
		page.AddEMC()
	}
	return x + (3.0 * m1)
}

func (barcode *Barcode) drawMGuard(page *Page, bars *barcodeBars, x, h float32) float32 {
	m1 := barcode.m1
	if page != nil {
		// 01010
		page.AddArtifactBMC()
		barcode.strokeBar(page, bars, x+(1.5*m1), m1, h)
		barcode.strokeBar(page, bars, x+(3.5*m1), m1, h)
		page.AddEMC()
	}
	return x + (5.0 * m1)
}

// drawBar draws the bar of width w and height h that starts at x.
func (barcode *Barcode) drawBar(page *Page, bars *barcodeBars, x, w, h float32) {
	if page != nil {
		page.AddArtifactBMC()
		barcode.strokeBar(page, bars, x+w/2, w, h)
		page.AddEMC()
	}
}

// strokeBar strokes the bar of width w and height h centered on x.
func (barcode *Barcode) strokeBar(page *Page, bars *barcodeBars, x, w, h float32) {
	if page != nil {
		top := bars.turn(x, bars.y1)
		bottom := bars.turn(x, bars.y1+h)
		page.SetPenWidth(w)
		page.MoveTo(top[0], top[1])
		page.LineTo(bottom[0], bottom[1])
		page.StrokePath()
	}
}

// drawText draws the text with its baseline starting at (x, y). It returns the
// end of the baseline and the bottom of the text, before the turn.
func (barcode *Barcode) drawText(page *Page, bars *barcodeBars, text string, x, y float32) [2]float32 {
	textLine := NewTextLine(barcode.font, text)
	xy := bars.turn(x, y)
	textLine.SetLocation(xy[0], xy[1])
	if barcode.direction == direction.TopToBottom {
		textLine.SetTextRotation(270)
	} else if barcode.direction == direction.BottomToTop {
		textLine.SetTextRotation(90)
	}
	textLine.DrawOn(page)
	return [2]float32{x + barcode.font.StringWidth(barcode.font.size, text), y + barcode.font.GetDescent()}
}

func (barcode *Barcode) drawCode128(page *Page, x1, y1 float32) [2]float32 {
	h := barcode.m1 * barcode.barHeightFactor // Barcode height when drawn horizontally

	list := make([]rune, 0)
	for _, symchar := range barcode.text {
		// Some characters need two codewords (SHIFT/FNC_4 + value), so
		// checking len(list) == 48 only *after* adding them could skip
		// right over 48 (e.g. 47 -> 49) and never trip again, silently
		// encoding an unbounded number of characters past the documented
		// limit. Check before adding instead, so the cap always holds.
		codewordsNeeded := 1
		if symchar < 32 || (symchar >= 128 && symchar < 256) {
			codewordsNeeded = 2
		}
		if len(list)+codewordsNeeded > 48 {
			// Maximum number of data characters is 48
			break
		}
		if symchar < 32 {
			list = append(list, rune(code128.Shift))
			list = append(list, symchar+64)
		} else if symchar < 128 {
			list = append(list, symchar-32)
		} else if symchar < 256 {
			list = append(list, rune(code128.FNC4))
			list = append(list, symchar-160) // 128 + 32
		} else {
			list = append(list, rune(256)) // This will generate an exception.
		}
	}

	var buf strings.Builder
	checkDigit := rune(code128.StartB)
	buf.WriteRune(checkDigit)
	for i := 0; i < len(list); i++ {
		codeword := list[i]
		buf.WriteRune(codeword)
		checkDigit += rune(int(codeword) * (i + 1))
	}
	checkDigit %= code128.StartA
	buf.WriteRune(checkDigit)
	buf.WriteRune(rune(code128.Stop))

	var length float32
	for _, si := range buf.String() {
		symbol := strconv.Itoa(code128.TABLE[si])
		for i := 0; i < len(symbol); i++ {
			length += float32(symbol[i]-0x30) * barcode.m1
		}
	}

	bars := &barcodeBars{x1: x1, y1: y1, length: length, height: h, direction: barcode.direction}
	x := x1
	for _, si := range buf.String() {
		symbol := strconv.Itoa(code128.TABLE[si])
		for i := 0; i < len(symbol); i++ {
			n := float32(symbol[i] - 0x30)
			if i%2 == 0 {
				barcode.drawBar(page, bars, x, n*barcode.m1, h)
			}
			x += n * barcode.m1
		}
	}

	right := x
	bottom := y1 + h
	if barcode.font != nil {
		xy := barcode.drawText(page, bars, barcode.text,
			x1+((x-x1)-barcode.font.StringWidth(barcode.font.size, barcode.text))/2.0,
			y1+h+barcode.font.bodyHeight)
		right = max(right, xy[0])
		bottom = xy[1]
	}

	return bars.getBottomRight(x1, right, bottom)
}

func (barcode *Barcode) drawCode39(page *Page, x1, y1 float32) [2]float32 {
	// Use a local variable instead of mutating the text field - DrawOn()
	// must be safe to call more than once on the same Barcode instance
	// (e.g. drawing the same barcode on several pages).
	fullText := "*" + barcode.text + "*"
	h := barcode.m1 * barcode.barHeightFactor // Barcode height when drawn horizontally

	var length float32
	for i := 0; i < len(fullText); i++ {
		code := barcode.tableB[fullText[i]]
		if code == "" {
			panic("The input string '" + fullText +
				"' contains characters that are invalid in a Code39 barcode.")
		}
		for _, ch := range code {
			if ch == 'W' || ch == 'B' {
				length += 3 * barcode.m1
			} else {
				length += barcode.m1
			}
		}
		length += barcode.m1
	}
	length -= barcode.m1 // There is no gap after the last character

	bars := &barcodeBars{x1: x1, y1: y1, length: length, height: h, direction: barcode.direction}
	x := x1
	for i := 0; i < len(fullText); i++ {
		for _, ch := range barcode.tableB[fullText[i]] {
			if ch == 'w' {
				x += barcode.m1
			} else if ch == 'W' {
				x += 3 * barcode.m1
			} else if ch == 'b' {
				barcode.drawBar(page, bars, x, barcode.m1, h)
				x += barcode.m1
			} else if ch == 'B' {
				barcode.drawBar(page, bars, x, 3*barcode.m1, h)
				x += 3 * barcode.m1
			}
		}
		x += barcode.m1
	}

	right := x1 + length
	bottom := y1 + h
	if barcode.font != nil {
		xy := barcode.drawText(page, bars, fullText,
			x1+(length-barcode.font.StringWidth(barcode.font.size, fullText))/2,
			y1+h+barcode.font.bodyHeight)
		right = max(right, xy[0])
		bottom = xy[1]
	}

	return bars.getBottomRight(x1, right, bottom)
}

func (barcode *Barcode) drawCodeEAN13(page *Page, x1, y1 float32) [2]float32 {
	x := x1
	h := barcode.m1 * barcode.barHeightFactor // Barcode height when drawn horizontally

	sum := 0
	for i := 0; i < 12; i += 2 {
		sum += int(barcode.text[i]) - 0x30
	}
	for i := 1; i < 12; i += 2 {
		sum += (int(barcode.text[i]) - 0x30) * 3
	}
	checkDigit := 0
	remainder := sum % 10
	if remainder > 0 {
		checkDigit = 10 - remainder
	}
	// Use a local variable instead of mutating the text field - DrawOn()
	// must be safe to call more than once on the same Barcode instance
	// (e.g. drawing the same barcode on several pages).
	fullText := barcode.text + strconv.Itoa(checkDigit)
	bars := &barcodeBars{x1: x1, y1: y1, length: 95 * barcode.m1, height: h + 8, direction: barcode.direction} // 95 modules

	x = barcode.drawEGuard(page, bars, x, h+8)
	xLeftGroupStart := x
	group1 := barcode.lgMap[fullText[0]-'0']
	for i := 1; i < 7; i++ {
		digit := fullText[i] - '0'
		str := barcode.gCode[digit]
		if group1[i-1] == 'L' {
			str = barcode.lCode[digit]
		}
		for j := 0; j < len(str); j++ {
			n := str[j] - '0'
			if j%2 != 0 {
				barcode.drawBar(page, bars, x, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
	}
	xLeftGroupEnd := x
	x = barcode.drawMGuard(page, bars, x, h+8)
	xRightGroupStart := x
	for i := 7; i < 13; i++ {
		digit := fullText[i] - '0'
		str := barcode.lCode[digit]
		for j := 0; j < len(str); j++ {
			n := str[j] - '0'
			if j%2 == 0 {
				barcode.drawBar(page, bars, x, float32(n)*barcode.m1, h)
			}
			x += float32(n) * barcode.m1
		}
	}
	xRightGroupEnd := x
	x = barcode.drawEGuard(page, bars, x, h+8)

	left := x1
	right := x
	bottom := y1 + h + 8
	if barcode.font != nil {
		// Standard EAN-13 layout: the leading (number system) digit sits
		// in the quiet zone to the left of the start guard bars, not
		// centered under them together with the rest of the label. The
		// two groups of 6 digits are each centered under their own bar
		// section (left group / right group), not under the barcode as
		// a whole.
		firstDigit := fullText[0:1]
		leftGroup := fullText[1:7]
		rightGroup := fullText[7:13]

		fontSize := barcode.font.GetSize()
		barcode.font.SetSize(10)
		yText := y1 + h + barcode.font.GetBodyHeight()
		gap := barcode.font.StringWidth(barcode.font.size, " ")

		left = x1 - gap - barcode.font.StringWidth(barcode.font.size, firstDigit)
		barcode.drawText(page, bars, firstDigit, left, yText)
		barcode.drawText(page, bars, leftGroup,
			xLeftGroupStart+((xLeftGroupEnd-xLeftGroupStart)-barcode.font.StringWidth(barcode.font.size, leftGroup))/2,
			yText)
		xy := barcode.drawText(page, bars, rightGroup,
			xRightGroupStart+((xRightGroupEnd-xRightGroupStart)-barcode.font.StringWidth(barcode.font.size, rightGroup))/2,
			yText)
		right = max(right, xy[0])
		bottom = max(bottom, xy[1])

		barcode.font.SetSize(fontSize)
	}

	return bars.getBottomRight(left, right, bottom)
}

// barcodeBars are the bars of a barcode, length long and height high, drawn
// left to right from (x1, y1) and turned to the direction of the barcode: top
// to bottom is a quarter turn clockwise and bottom to top a quarter turn
// counter-clockwise, and the bars stay right of x1 and below y1. The draw
// methods take the coordinates of the barcode drawn left to right.
type barcodeBars struct {
	x1, y1, length, height float32
	direction              direction.Direction
}

// turn returns the point (x, y) turned to the direction of the barcode.
func (bars *barcodeBars) turn(x, y float32) [2]float32 {
	if bars.direction == direction.TopToBottom {
		return [2]float32{bars.x1 + bars.height - (y - bars.y1), bars.y1 + (x - bars.x1)}
	} else if bars.direction == direction.BottomToTop {
		return [2]float32{bars.x1 + (y - bars.y1), bars.y1 + bars.length - (x - bars.x1)}
	}
	return [2]float32{x, y}
}

// getBottomRight returns the bottom right corner, turned to the direction of
// the barcode, of a barcode that spans from left to right and from y1 to bottom.
func (bars *barcodeBars) getBottomRight(left, right, bottom float32) [2]float32 {
	if bars.direction == direction.TopToBottom {
		return [2]float32{bars.x1 + bars.height, bars.y1 + (right - bars.x1)}
	} else if bars.direction == direction.BottomToTop {
		return [2]float32{bars.x1 + (bottom - bars.y1), bars.y1 + bars.length + (bars.x1 - left)}
	}
	return [2]float32{right, bottom}
}

// GetHeight returns the height of this barcode.
func (barcode *Barcode) GetHeight() float32 {
	if barcode.font == nil {
		return barcode.m1 * barcode.barHeightFactor
	}
	return barcode.m1*barcode.barHeightFactor + barcode.font.GetBodyHeight()
}
