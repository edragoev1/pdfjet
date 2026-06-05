package pdfjet

/**
 * title.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
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
