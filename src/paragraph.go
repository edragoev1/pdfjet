// paragraph.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// Paragraph is used to create paragraph objects.
// See the TextColumn class for more information.
type Paragraph struct {
	xText, yText, x1, y1, x2, y2 float32
	lines                        []*TextLine
	alignment                    int
}

// NewParagraph creates a paragraph.
func NewParagraph() *Paragraph {
	paragraph := new(Paragraph)
	paragraph.lines = make([]*TextLine, 0)
	paragraph.alignment = alignment.Left
	return paragraph
}

// GetTextX returns the x coordinate where the text of this paragraph starts.
func (paragraph *Paragraph) GetTextX() float32 {
	return paragraph.xText
}

// GetTextY returns the baseline y coordinate of the first line of this paragraph.
func (paragraph *Paragraph) GetTextY() float32 {
	return paragraph.yText
}

// GetX1 returns the x coordinate of the top left corner of this paragraph.
func (paragraph *Paragraph) GetX1() float32 {
	return paragraph.x1
}

// GetY1 returns the y coordinate of the top left corner of this paragraph.
func (paragraph *Paragraph) GetY1() float32 {
	return paragraph.y1
}

// GetX2 returns the x coordinate where the last line of this paragraph ends.
func (paragraph *Paragraph) GetX2() float32 {
	return paragraph.x2
}

// GetY2 returns the y coordinate of the bottom of the last line of this paragraph.
func (paragraph *Paragraph) GetY2() float32 {
	return paragraph.y2
}

// Add adds a text line to this paragraph.
func (paragraph *Paragraph) Add(text *TextLine) *Paragraph {
	paragraph.lines = append(paragraph.lines, text)
	return paragraph
}

// SetTextAlignment sets the alignment of the text in this paragraph:
// alignment.Left, alignment.Right, alignment.Center or alignment.Justify.
func (paragraph *Paragraph) SetTextAlignment(alignment int) *Paragraph {
	paragraph.alignment = alignment
	return paragraph
}

// GetTextLines returns the text lines of this paragraph.
func (paragraph *Paragraph) GetTextLines() []*TextLine {
	return paragraph.lines
}

// StartsWith returns true if the first line of this paragraph starts with the specified token.
func (paragraph *Paragraph) StartsWith(token string) bool {
	return strings.HasPrefix(paragraph.lines[0].GetText(), token)
}

// SetColor sets the text color of all lines in this paragraph.
func (paragraph *Paragraph) SetColor(color int32) *Paragraph {
	for _, line := range paragraph.lines {
		line.SetTextColor(color)
	}
	return paragraph
}

// SetColorMap sets the word highlight colors of all lines in this paragraph.
func (paragraph *Paragraph) SetColorMap(colorMap map[string]int32) *Paragraph {
	for _, line := range paragraph.lines {
		line.SetColorMap(colorMap)
	}
	return paragraph
}
