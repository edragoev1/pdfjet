// title.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Title is used to create title objects that have prefix and text.
// Please see Example_48
type Title struct {
	prefix   *TextLine
	textLine *TextLine
	offset   float32
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

// GetTextLine returns the text line of the title.
func (title *Title) GetTextLine() *TextLine {
	return title.textLine
}

// SetOffset sets the distance from the start of the prefix to the start of the
// title text, to make room for the prefix.
func (title *Title) SetOffset(offset float32) *Title {
	title.offset = offset
	title.textLine.SetLocation(title.prefix.x+offset, title.prefix.y)
	return title
}

// SetLocation sets the location of the prefix; the title text keeps its offset
// from it.
//   - x: the x coordinate.
//   - y: the y coordinate.
//
// Returns this Title.
func (title *Title) SetLocation(x, y float32) Drawable {
	title.prefix.SetLocation(x, y)
	title.textLine.SetLocation(x+title.offset, y)
	return title
}

// DrawOn draws the title.
func (title *Title) DrawOn(page *Page) [2]float32 {
	if title.prefix.text != "" {
		title.prefix.DrawOn(page)
	}
	return title.textLine.DrawOn(page)
}
