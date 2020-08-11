package pdfjet

/**
 * title.go
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

// Title is used to create title objects that have prefix and text.
// Please see Example_51 and Example_52
type Title struct {
	prefix   *TextLine
	textLine *TextLine
}

// NewTitle is the constructor.
func NewTitle(font *Font, text string, x, y float32) *Title {
	title := new(Title)
	title.prefix = NewTextLine(font, "")
	title.textLine = NewTextLine(font, text)
	title.prefix.SetLocation(x, y)
	title.textLine.SetLocation(x, y)
	return title
}

// SetPrefix sets the prefix text.
func (title *Title) SetPrefix(text string) *Title {
	title.prefix.SetText(text)
	return title
}

// GetPrefix returns the prefix of the title.
func (title *Title) GetPrefix() *TextLine {
	return title.prefix
}

// SetOffset sets the offset of the title text.
func (title *Title) SetOffset(offset float32) *Title {
	title.textLine.SetLocation(title.textLine.x+offset, title.textLine.y)
	return title
}

// DrawOn draws the title.
func (title *Title) DrawOn(page *Page) {
	if title.prefix != nil {
		title.prefix.DrawOn(page)
	}
	title.textLine.DrawOn(page)
}
