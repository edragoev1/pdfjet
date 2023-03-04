package pdfjet

/**
 * title.go
 *
Copyright 2023 Innovatics Inc.

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
