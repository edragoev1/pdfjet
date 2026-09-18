// textcolumn.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/internal/single"
)

// TextColumn is a column of paragraphs, each a list of TextLine objects that
// can differ in font, size and color, aligned left, right, center or
// justified, with a line spacing, a paragraph spacing and an optional line
// between the paragraphs. It draws all of its paragraphs where it is placed,
// top down; to rotate a column, add it to a Container and rotate that.
//
// Use a TextColumn for an article or a page of mixed text: bold or colored
// words in a paragraph, justified paragraphs, CJK paragraphs. Use a TextBlock
// for one run of text in one font, and a TextFrame when the text must
// continue from one frame to the next, across columns or pages. Please see
// Example_10, Example_29, Example_44 and Example_49.
type TextColumn struct {
	alignment             alignment.Alignment
	x                     float32 // This variable is set in the beginning and only reset after the DrawOn
	y                     float32 // This variable is set in the beginning and only reset after the DrawOn
	w                     float32
	h                     float32
	x1                    float32
	y1                    float32
	lineSpacing           float32
	paragraphSpacing      float32
	paragraphs            []*Paragraph
	lineBetweenParagraphs bool
}

// NewTextColumn creates a text column object.
func NewTextColumn() *TextColumn {
	textColumn := new(TextColumn)
	textColumn.alignment = alignment.Left
	textColumn.lineSpacing = 1.0
	textColumn.paragraphSpacing = 1.0
	textColumn.lineBetweenParagraphs = false
	textColumn.paragraphs = make([]*Paragraph, 0)
	return textColumn
}

// SetLineBetweenParagraphs sets whether an empty line is inserted between the
// current and next paragraphs.
func (textColumn *TextColumn) SetLineBetweenParagraphs(lineBetweenParagraphs bool) *TextColumn {
	textColumn.lineBetweenParagraphs = lineBetweenParagraphs
	return textColumn
}

// SetLineSpacing sets the space between the lines.
func (textColumn *TextColumn) SetLineSpacing(lineSpacing float32) *TextColumn {
	textColumn.lineSpacing = lineSpacing
	return textColumn
}

// SetParagraphSpacing sets the space between paragraphs.
func (textColumn *TextColumn) SetParagraphSpacing(paragraphSpacing float32) *TextColumn {
	textColumn.paragraphSpacing = paragraphSpacing
	return textColumn
}

// SetLocation sets the location of the top left corner of this text column on the page.
func (textColumn *TextColumn) SetLocation(x, y float32) Drawable {
	textColumn.x = x
	textColumn.y = y
	textColumn.x1 = x
	textColumn.y1 = y
	return textColumn
}

// SetWidth sets the desired width of this text column.
func (textColumn *TextColumn) SetWidth(w float32) *TextColumn {
	textColumn.w = w
	return textColumn
}

// SetHeight sets the height of this text column.
func (textColumn *TextColumn) SetHeight(h float32) *TextColumn {
	textColumn.h = h
	return textColumn
}

// GetWidth returns the width of this text column.
func (textColumn *TextColumn) GetWidth() float32 {
	return textColumn.w
}

// GetHeight returns the height of this text column.
func (textColumn *TextColumn) GetHeight() float32 {
	return textColumn.h
}

// SetTextAlignment sets the text alignment:
// alignment.Left, alignment.Right, alignment.Center or alignment.Justify.
func (textColumn *TextColumn) SetTextAlignment(textAlignment alignment.Alignment) *TextColumn {
	textColumn.alignment = textAlignment
	return textColumn
}

// AddParagraph adds a new paragraph to this text column.
func (textColumn *TextColumn) AddParagraph(paragraph *Paragraph) *TextColumn {
	textColumn.paragraphs = append(textColumn.paragraphs, paragraph)
	return textColumn
}

// RemoveLastParagraph removes the last paragraph added to this text column.
func (textColumn *TextColumn) RemoveLastParagraph() *TextColumn {
	if len(textColumn.paragraphs) >= 1 {
		textColumn.paragraphs = textColumn.paragraphs[0 : len(textColumn.paragraphs)-1]
	}
	return textColumn
}

// GetSize returns dimension object containing the width and height of this component.
func (textColumn *TextColumn) GetSize() *Dimension {
	xy := textColumn.DrawOn(nil)
	return NewDimension(textColumn.w, xy[1]-textColumn.y)
}

// DrawOn draws this text column on the specified page and returns the x and y
// coordinates of its bottom right corner. With no page nothing is drawn and the
// corner is computed.
func (textColumn *TextColumn) DrawOn(page *Page) [2]float32 {
	xy := [2]float32{textColumn.x, textColumn.y}
	for i, paragraph := range textColumn.paragraphs {
		xy = textColumn.drawParagraphOn(page, paragraph, i == (len(textColumn.paragraphs)-1))
	}
	// Restore the original location
	textColumn.SetLocation(textColumn.x, textColumn.y)
	// A column with a height reaches at least that far down from its location
	if textColumn.y+textColumn.h > xy[1] {
		xy[1] = textColumn.y + textColumn.h
	}
	return [2]float32{textColumn.x + textColumn.w, xy[1]}
}

func (textColumn *TextColumn) drawParagraphOn(
	page *Page, paragraph *Paragraph, lastParagraph bool) [2]float32 {
	textAlignment := textColumn.alignment
	if paragraph.explicitAlignment {
		textAlignment = paragraph.alignment
	}
	list := make([]*TextLine, 0)
	var lineHeight = float32(0.0)
	var maxAscent = float32(0.0)
	var maxDescent = float32(0.0)
	for _, line := range paragraph.lines {
		height := (line.GetHeight() + line.font.GetLineGap(line.fontSize)) * textColumn.lineSpacing
		if height > lineHeight {
			lineHeight = height
		}
		if line.font.GetAscent(line.fontSize) > maxAscent {
			maxAscent = line.font.GetAscent(line.fontSize)
		}
		if line.font.GetDescent(line.fontSize) > maxDescent {
			maxDescent = line.font.GetDescent(line.fontSize)
		}
	}
	textColumn.y1 += maxAscent

	var runLength float32
	for _, line := range paragraph.lines {
		for _, token := range splitOnWhitespace(line.text) {
			text := line.copyWithText(token + single.Space)
			// The token is measured without the space that follows it: a line
			// is as wide as the text it shows. A token wider than the column
			// goes on a line of its own rather than after an empty one, which
			// would leave the line above it blank.
			if len(list) == 0 || (runLength+textLineWidth(text, token)) <= textColumn.w {
				list = append(list, text)
				runLength += text.GetWidth()
			} else {
				textColumn.drawLineOfText(page, list, textAlignment)
				textColumn.moveToNextLine(lineHeight)
				list = make([]*TextLine, 0)
				list = append(list, text)
				runLength = text.GetWidth()
			}
		}
	}
	// The last line of a paragraph is not justified.
	textColumn.drawNonJustifiedLine(page, list, textAlignment)

	// The paragraph reaches down to the descent of its last line. The spacing
	// and the blank line go between the paragraphs, not after the last one.
	if lastParagraph {
		return textColumn.moveToNextParagraph(maxDescent)
	}
	if textColumn.lineBetweenParagraphs {
		textColumn.moveToNextLine(lineHeight)
	}

	return textColumn.moveToNextParagraph(lineHeight * textColumn.paragraphSpacing)
}

// markLastToken marks the last token of a line drawn on the page as the one
// that ends it: its underline and its strikeout stop at its text, not after
// the space that follows it.
func markLastToken(textLines []*TextLine) {
	if len(textLines) > 0 {
		textLines[len(textLines)-1].isLastToken = true
	}
}

// visibleWidth returns the width of the text the line shows: every token with
// the space after it, and the last token without it.
func visibleWidth(textLines []*TextLine) float32 {
	var runLength float32
	for i, textLine := range textLines {
		if i == (len(textLines) - 1) {
			runLength += textLineWidth(textLine, strings.TrimRight(textLine.text, " "))
		} else {
			runLength += textLine.GetWidth()
		}
	}
	return runLength
}

func textLineWidth(textLine *TextLine, text string) float32 {
	return textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, text)
}

func (textColumn *TextColumn) moveToNextLine(lineHeight float32) [2]float32 {
	textColumn.x1 = textColumn.x
	textColumn.y1 += lineHeight
	return [2]float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) moveToNextParagraph(paragraphSpacing float32) [2]float32 {
	textColumn.x1 = textColumn.x
	textColumn.y1 += paragraphSpacing
	return [2]float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) drawLineOfText(page *Page, textLines []*TextLine, textAlignment alignment.Alignment) {
	if textAlignment == alignment.Justify {
		markLastToken(textLines)
		// The spaces are widened so that the text of the line reaches both
		// edges. A line of one token has no space to widen.
		var dx float32
		if len(textLines) > 1 {
			dx = (textColumn.w - visibleWidth(textLines)) / float32(len(textLines)-1)
		}
		// Each token draws its own link annotation when the line has a URI or GoTo action.
		for _, textLine := range textLines {
			textLine.SetLocation(textColumn.x1, textColumn.y1+textLine.GetVerticalOffset())
			textLine.DrawOn(page)
			textColumn.x1 += textLine.GetWidth() + dx
		}
	} else {
		textColumn.drawNonJustifiedLine(page, textLines, textAlignment)
	}
}

func (textColumn *TextColumn) drawNonJustifiedLine(page *Page, textLines []*TextLine, textAlignment alignment.Alignment) {
	markLastToken(textLines)
	runLength := visibleWidth(textLines)

	if textAlignment == alignment.Center {
		textColumn.x1 = textColumn.x + ((textColumn.w - runLength) / 2)
	} else if textAlignment == alignment.Right {
		textColumn.x1 = textColumn.x + (textColumn.w - runLength)
	}

	// Each token draws its own link annotation when the line has a URI or GoTo action.
	for _, textLine := range textLines {
		textLine.SetLocation(textColumn.x1, textColumn.y1+textLine.GetVerticalOffset())
		textLine.DrawOn(page)
		textColumn.x1 += textLine.GetWidth()
	}
}

// AddCJKParagraph adds a paragraph of Chinese, Japanese or Korean text, in the
// specified font, to this text column. The text is wrapped at any character to
// the width of the column.
func (textColumn *TextColumn) AddCJKParagraph(font *Font, text string) *TextColumn {
	var paragraph *Paragraph
	var buf strings.Builder
	for _, ch := range text {
		if font.StringWidth(font.size, buf.String()+string(ch)) > textColumn.w {
			paragraph = NewParagraph()
			paragraph.Add(NewTextLine(font, buf.String()))
			textColumn.AddParagraph(paragraph)
			buf.Reset()
		}
		buf.WriteRune(ch)
	}
	paragraph = NewParagraph()
	paragraph.Add(NewTextLine(font, buf.String()))
	return textColumn.AddParagraph(paragraph)
}
