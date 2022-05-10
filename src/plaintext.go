package pdfjet

/**
 * plaintext.go
 *
Copyright 2022 Innovatics Inc.

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
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/single"
)

// PlainText - please see Example_45
type PlainText struct {
	font            *Font
	textLines       []string
	fontSize        float32
	x, y            float32
	w               float32
	leading         float32
	backgroundColor uint32
	borderColor     uint32
	textColor       uint32
	language        string
	altDescription  string
	actualText      string
}

// NewPlainText is the constructor.
func NewPlainText(font *Font, textLines []string) *PlainText {
	text := new(PlainText)
	text.font = font
	text.textLines = textLines
	text.fontSize = font.GetSize()
	text.w = 500.0
	text.backgroundColor = color.White
	text.borderColor = color.White
	text.textColor = color.Black
	var buf strings.Builder
	for _, str := range textLines {
		buf.WriteString(str)
		buf.WriteString(" ")
	}
	text.altDescription = buf.String()
	text.actualText = buf.String()
	return text
}

// SetFontSize sets the font size.
func (text *PlainText) SetFontSize(fontSize float32) *PlainText {
	text.fontSize = fontSize
	return text
}

// SetLocation sets the location of the plain text component.
func (text *PlainText) SetLocation(x, y float32) *PlainText {
	text.x = x
	text.y = y
	return text
}

// SetWidth sets the width of the plain text area.
func (text *PlainText) SetWidth(w float32) *PlainText {
	text.w = w
	return text
}

// SetLeading sets the text leading.
func (text *PlainText) SetLeading(leading float32) *PlainText {
	text.leading = leading
	return text
}

// SetBackgroundColor sets the background color.
func (text *PlainText) SetBackgroundColor(backgroundColor uint32) *PlainText {
	text.backgroundColor = backgroundColor
	return text
}

// SetBorderColor sets the corder color.
func (text *PlainText) SetBorderColor(borderColor uint32) *PlainText {
	text.borderColor = borderColor
	return text
}

// SetTextColor sets the text color.
func (text *PlainText) SetTextColor(textColor uint32) *PlainText {
	text.textColor = textColor
	return text
}

// DrawOn draws this PlainText on the specified page.
// @param page the page to draw this PlainText on.
// @return x and y coordinates of the bottom right corner of this component.
// @throws Exception
func (text *PlainText) DrawOn(page *Page) []float32 {
	originalSize := page.font.GetSize()
	page.font.SetSize(text.fontSize)
	yText := text.y + page.font.ascent

	page.AddBMC("Span", text.language, single.Space, single.Space)
	page.SetBrushColor(text.backgroundColor)
	leading := page.font.GetBodyHeight()
	h := page.font.GetBodyHeight() * float32(len(text.textLines))
	page.FillRect(text.x, text.y, text.w, h)
	page.SetPenColor(text.borderColor)
	page.SetPenWidth(0.0)
	page.DrawRect(text.x, text.y, text.w, h)
	page.AddEMC()

	page.AddBMC("Span", text.language, text.actualText, text.altDescription)
	page.SetTextStart()
	page.SetTextFont(text.font)
	page.SetBrushColor(text.textColor)
	page.SetTextLeading(leading)
	page.SetTextLocation(text.x, yText)
	for _, str := range text.textLines {
		if text.font.skew15 {
			text.SetTextSkew(page, 0.26, text.x, yText)
		}
		page.Println(str)
		yText += leading
	}
	page.SetTextEnd()
	page.AddEMC()

	text.font.SetSize(originalSize)

	return []float32{text.x + text.w, text.y + h}
}

// SetTextSkew sets the text skew parameter.
func (text *PlainText) SetTextSkew(page *Page, skew, x, y float32) {
	appendFloat32(&page.buf, 1.0)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, 0.0)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, skew)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, 1.0)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, x)
	appendString(&page.buf, " ")
	appendFloat32(&page.buf, page.height-y)
	appendString(&page.buf, " Tm\n")
}
