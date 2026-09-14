// paragraph.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/content"
)

// Paragraph is used to create paragraph objects.
// See the TextColumn class for more information.
type Paragraph struct {
	xText, yText, x1, y1, x2, y2 float32
	lines                        []*TextLine
	alignment                    alignment.Alignment
	// True after SetTextAlignment. Otherwise the alignment of the text column applies.
	explicitAlignment bool
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
// A paragraph with no alignment set takes the alignment of the text column it
// is drawn in.
func (paragraph *Paragraph) SetTextAlignment(textAlignment alignment.Alignment) *Paragraph {
	paragraph.alignment = textAlignment
	paragraph.explicitAlignment = true
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

// SetTextColor sets the text color of all lines in this paragraph.
func (paragraph *Paragraph) SetTextColor(color int32) *Paragraph {
	for _, line := range paragraph.lines {
		line.SetTextColor(color)
	}
	return paragraph
}

// SetTextColorRGB sets the text color of all lines in this paragraph from
// red, green and blue values.
func (paragraph *Paragraph) SetTextColorRGB(rgbColor [3]float32) *Paragraph {
	for _, line := range paragraph.lines {
		line.SetTextColorRGB(rgbColor)
	}
	return paragraph
}

// SetHighlightColors sets the word highlight colors of all lines in this paragraph.
func (paragraph *Paragraph) SetHighlightColors(colorMap map[string]int32) *Paragraph {
	for _, line := range paragraph.lines {
		line.SetHighlightColors(colorMap)
	}
	return paragraph
}

// ParagraphsFromFile reads a text file and returns its paragraphs. An empty
// line separates the paragraphs. It panics if the file cannot be read.
func ParagraphsFromFile(f1 *Font, filePath string) []*Paragraph {
	paragraphs := make([]*Paragraph, 0)
	paragraph := NewParagraph()
	textLine := NewEmptyTextLine(f1)
	sb := make([]rune, 0)
	runes := []rune(content.OfTextFile(filePath))
	for i := 0; i < len(runes); i++ {
		ch := runes[i]
		// We need at least one character after the \n\n to begin new paragraph!
		if i < (len(runes)-2) &&
			ch == '\n' && runes[i+1] == '\n' {
			textLine.SetText(string(sb))
			paragraph.Add(textLine)
			paragraphs = append(paragraphs, paragraph)
			paragraph = NewParagraph()
			textLine = NewEmptyTextLine(f1)
			sb = nil
			i += 1
		} else {
			sb = append(sb, ch)
		}
	}
	if len(sb) != 0 {
		textLine.SetText(string(sb))
		paragraph.Add(textLine)
		paragraphs = append(paragraphs, paragraph)
	}
	return paragraphs
}
