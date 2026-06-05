package pdfjet

/**
 * paragraph.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"strings"

	"github.com/edragoev1/pdfjet/src/alignment"
)

// Paragraph describes paragraph objects.
// See the TextColumn class for more information.
type Paragraph struct {
	xText, yText, x1, y1, x2, y2 float32
	lines                        []*TextLine
	alignment                    int // = align.Left
}

// NewParagraph constructor paragraph objects.
func NewParagraph() *Paragraph {
	paragraph := new(Paragraph)
	paragraph.lines = make([]*TextLine, 0)
	paragraph.alignment = alignment.Left
	return paragraph
}

func (paragraph *Paragraph) GetTextX() float32 {
	return paragraph.xText
}

func (paragraph *Paragraph) GetTextY() float32 {
	return paragraph.yText
}

func (paragraph *Paragraph) GetX1() float32 {
	return paragraph.x1
}

func (paragraph *Paragraph) GetY1() float32 {
	return paragraph.y1
}

func (paragraph *Paragraph) GetX2() float32 {
	return paragraph.x2
}

func (paragraph *Paragraph) GetY2() float32 {
	return paragraph.y2
}

// Add is used to add new text lines to the paragraph.
//
// @param text the text line to add to the paragraph.
// @return the paragraph.
func (paragraph *Paragraph) Add(textLine *TextLine) *Paragraph {
	paragraph.lines = append(paragraph.lines, textLine)
	return paragraph
}

// SetAlignment sets the alignment of the text in the paragraph.
// @param alignment the alignment code.
// @return the paragraph.
// <pre>Supported values: align.Left, align.Right, align.Center and align.Justify.</pre>
func (paragraph *Paragraph) SetAlignment(alignment int) *Paragraph {
	paragraph.alignment = alignment
	return paragraph
}

func (paragraph *Paragraph) GetTextLines() []*TextLine {
	return paragraph.lines
}

func (paragraph *Paragraph) StartsWith(token string) bool {
	return strings.HasPrefix(paragraph.lines[0].GetText(), token)
}

func (paragraph *Paragraph) SetColor(color int32) {
	for _, line := range paragraph.lines {
		line.SetTextColor(color)
	}
}

func (paragraph *Paragraph) SetColorMap(colorMap map[string]int32) {
	for _, line := range paragraph.lines {
		line.SetColorMap(colorMap)
	}
}
