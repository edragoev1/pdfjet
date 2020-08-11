package pdfjet

/**
 * plaintext.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

import (
	"color"
	"single"
	"strings"
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

	page.AddBMC("Span", text.language, text.altDescription, text.actualText)
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
