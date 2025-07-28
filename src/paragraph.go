package pdfjet

/**
 * paragraph.go
 *
©2025 PDFjet Software

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
	xText, yText, x1, y1, x2, y2 float32
	lines                        []*TextLine
	alignment                    int // = align.Left
}

// NewParagraph constructor paragraph objects.
func NewParagraph() *Paragraph {
	paragraph := new(Paragraph)
	paragraph.lines = make([]*TextLine, 0)
	paragraph.alignment = align.Left
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

func (paragraph *Paragraph) GetTextLines() []*TextLine {
	return paragraph.lines
}

func (paragraph *Paragraph) StartsWith(token string) bool {
	return strings.HasPrefix(paragraph.lines[0].GetText(), token)
}

func (paragraph *Paragraph) SetColor(color int32) {
	for _, line := range paragraph.lines {
		line.SetColor(color)
	}
}

func (paragraph *Paragraph) SetColorMap(colorMap map[string]int32) {
	for _, line := range paragraph.lines {
		line.SetColorMap(colorMap)
	}
}
