package pdfjet

/**
 * paragraph.go
 *
Copyright 2020 Innovatics Inc.
*/

import (
	"github.com/edragoev1/pdfjet/src/align"
)

// Paragraph describes paragraph objects.
// See the TextColumn class for more information.
type Paragraph struct {
	lines     []*TextLine
	alignment int // = align.Left
}

// NewParagraph constructor paragraph objects.
func NewParagraph() *Paragraph {
	paragraph := new(Paragraph)
	paragraph.lines = make([]*TextLine, 0)
	paragraph.alignment = align.Left
	return paragraph
}

// Add is used to add new text lines to the paragraph.
//
// @param text the text line to add to paragraph paragraph.
// @return paragraph paragraph.
func (paragraph *Paragraph) Add(textLine *TextLine) *Paragraph {
	paragraph.lines = append(paragraph.lines, textLine)
	return paragraph
}

// SetAlignment sets the alignment of the text in paragraph paragraph.
// @param alignment the alignment code.
// @return paragraph paragraph.
// <pre>Supported values: align.Left, align.Right, align.Center and align.Justify.</pre>
func (paragraph *Paragraph) SetAlignment(alignment int) *Paragraph {
	paragraph.alignment = alignment
	return paragraph
}
