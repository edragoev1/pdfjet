package pdfjet

/**
 * paragraph.go
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

import (
	"strings"

	"github.com/edragoev1/pdfjet/src/align"
)

// Paragraph describes paragraph objects.
// See the TextColumn class for more information.
type Paragraph struct {
	x, y      float32
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

func (paragraph *Paragraph) GetX() float32 {
	return paragraph.x
}

func (paragraph *Paragraph) GetY() float32 {
	return paragraph.y
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

func (paragraph *Paragraph) StartsWith(token string) bool {
	return strings.HasPrefix(paragraph.lines[0].GetText(), token)
}
