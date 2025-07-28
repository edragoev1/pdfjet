package pdfjet

/**
 * textcolumn.go
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
	"log"
	"strings"

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/single"
)

// TextColumn is used to create text column objects and draw them on a page.
//
// Please see Example_10.
type TextColumn struct {
	alignment              int // = align.Left
	rotate                 int
	x                      float32 // This variable is set in the beginning and only reset after the DrawOn
	y                      float32 // This variable is set in the beginning and only reset after the DrawOn
	w                      float32
	h                      float32
	x1                     float32
	y1                     float32
	lineHeight             float32
	spaceBetweenLines      float32
	spaceBetweenParagraphs float32
	paragraphs             []*Paragraph
	lineBetweenParagraphs  bool
}

/**
 *  Create a text column object.
 *
 */
/*
func (textColumn *TextColumn) TextColumn() {
    this.paragraphs = new ArrayList<Paragraph>()
}
*/

// NewTextColumn used to create a text column object and set the rotation angle.
// @param rotateByDegrees the specified rotation angle in degrees.
func NewTextColumn(rotateByDegrees int) *TextColumn {
	textColumn := new(TextColumn)
	textColumn.alignment = align.Left
	textColumn.spaceBetweenLines = 1.0
	textColumn.spaceBetweenParagraphs = 2.0
	textColumn.rotate = rotateByDegrees
	if rotateByDegrees != 0 && rotateByDegrees != 90 && rotateByDegrees != 270 {
		log.Fatal("Invalid rotation angle. Please use 0, 90 or 270 degrees.")
	}
	textColumn.paragraphs = make([]*Paragraph, 0)
	return textColumn
}

// SetLineBetweenParagraphs sets the lineBetweenParagraphs private variable value.
// If the value is set to true - an empty line will be inserted between the current and next paragraphs.
// @param lineBetweenParagraphs the specified boolean value.
func (textColumn *TextColumn) SetLineBetweenParagraphs(lineBetweenParagraphs bool) {
	textColumn.lineBetweenParagraphs = lineBetweenParagraphs
}

// SetSpaceBetweenLines sets the space between the lines.
func (textColumn *TextColumn) SetSpaceBetweenLines(spaceBetweenLines float32) {
	textColumn.spaceBetweenLines = spaceBetweenLines
}

// SetSpaceBetweenParagraphs sets the space between the paragraphs.
func (textColumn *TextColumn) SetSpaceBetweenParagraphs(spaceBetweenParagraphs float32) {
	textColumn.spaceBetweenParagraphs = spaceBetweenParagraphs
}

// SetLocation sets the position of this text column on the page.
// @param x the x coordinate of the top left corner of this text column when drawn on the page.
// @param y the y coordinate of the top left corner of this text column when drawn on the page.
func (textColumn *TextColumn) SetLocation(x, y float32) {
	textColumn.x = x
	textColumn.y = y
	textColumn.x1 = x
	textColumn.y1 = y
}

// SetSize sets the size of this text column.
// @param w the width of this text column.
// @param h the height of this text column.
func (textColumn *TextColumn) SetSize(w, h float32) {
	textColumn.w = w
	textColumn.h = h
}

// SetWidth sets the desired width of this text column.
// @param w the width of this text column.
func (textColumn *TextColumn) SetWidth(w float32) {
	textColumn.w = w
}

// SetAlignment sets the text alignment.
// Supported values: align.Left, align.Right, align.Center and align.Justify
func (textColumn *TextColumn) SetAlignment(alignment int) {
	textColumn.alignment = alignment
}

// SetLineSpacing sets the spacing between the lines in this text column.
func (textColumn *TextColumn) SetLineSpacing(spacing float32) {
	textColumn.spaceBetweenLines = spacing
}

// AddParagraph adds a new paragraph to this text column.
func (textColumn *TextColumn) AddParagraph(paragraph *Paragraph) {
	textColumn.paragraphs = append(textColumn.paragraphs, paragraph)
}

// RemoveLastParagraph removes the last paragraph added to this text column.
func (textColumn *TextColumn) RemoveLastParagraph() {
	if len(textColumn.paragraphs) >= 1 {
		textColumn.paragraphs = textColumn.paragraphs[0 : len(textColumn.paragraphs)-1]
	}
}

// GetSize returns dimension object containing the width and height of this component.
func (textColumn *TextColumn) GetSize() *Dimension {
	xy := textColumn.DrawOn(nil)
	return NewDimension(textColumn.w, xy[1]-textColumn.y)
}

// DrawOn draws this text column on the specified page if the 'draw' boolean value is 'true'.
//
// @param page the page to draw this text column on.
// @param draw the boolean value that specified if the text column should actually be drawn on the page.
// @return the point with x and y coordinates of the location where to draw the next component.
func (textColumn *TextColumn) DrawOn(page *Page) []float32 {
	var xy []float32
	for _, paragraph := range textColumn.paragraphs {
		textColumn.alignment = paragraph.alignment
		xy = textColumn.drawParagraphOn(page, paragraph)
	}
	// Restore the original location
	textColumn.SetLocation(textColumn.x, textColumn.y)
	return xy
}

func (textColumn *TextColumn) drawParagraphOn(page *Page, paragraph *Paragraph) []float32 {
	list := make([]*TextLine, 0)
	var runLength float32
	for i := 0; i < len(paragraph.lines); i++ {
		line := paragraph.lines[i]
		if i == 0 {
			textColumn.lineHeight = line.font.bodyHeight + textColumn.spaceBetweenLines
			if textColumn.rotate == 0 {
				textColumn.y1 += line.font.ascent
			} else if textColumn.rotate == 90 {
				textColumn.x1 += line.font.ascent
			} else if textColumn.rotate == 270 {
				textColumn.x1 -= line.font.ascent
			}
		}

		tokens := strings.Fields(line.text)
		var text *TextLine
		for _, token := range tokens {
			text = NewTextLine(line.font, token)
			text.SetColor(line.GetColor())
			text.SetUnderline(line.GetUnderline())
			text.SetStrikeout(line.GetStrikeout())
			text.SetVerticalOffset(line.GetVerticalOffset())
			text.SetURIAction(line.GetURIAction())
			text.SetGoToAction(line.GetGoToAction())
			text.SetFallbackFont(line.GetFallbackFont())
			runLength += line.font.StringWidth(line.GetFallbackFont(), token)
			if runLength < textColumn.w {
				list = append(list, text)
				runLength += line.font.StringWidth(line.GetFallbackFont(), single.Space)
			} else {
				textColumn.drawLineOfText(page, list)
				textColumn.moveToNextLine()
				list = make([]*TextLine, 0)
				list = append(list, text)
				runLength = line.font.StringWidth(line.GetFallbackFont(), token+single.Space)
			}
		}
		if !line.GetTrailingSpace() {
			runLength -= line.font.StringWidth(line.GetFallbackFont(), single.Space)
			text.SetTrailingSpace(false)
		}
	}
	textColumn.drawNonJustifiedLine(page, list)

	if textColumn.lineBetweenParagraphs {
		return textColumn.moveToNextLine()
	}

	return textColumn.moveToNextParagraph(textColumn.spaceBetweenParagraphs)
}

func (textColumn *TextColumn) moveToNextLine() []float32 {
	if textColumn.rotate == 0 {
		textColumn.x1 = textColumn.x
		textColumn.y1 += textColumn.lineHeight
	} else if textColumn.rotate == 90 {
		textColumn.x1 += textColumn.lineHeight
		textColumn.y1 = textColumn.y
	} else if textColumn.rotate == 270 {
		textColumn.x1 -= textColumn.lineHeight
		textColumn.y1 = textColumn.y
	}
	return []float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) moveToNextParagraph(spaceBetweenParagraphs float32) []float32 {
	if textColumn.rotate == 0 {
		textColumn.x1 = textColumn.x
		textColumn.y1 += spaceBetweenParagraphs
	} else if textColumn.rotate == 90 {
		textColumn.x1 += spaceBetweenParagraphs
		textColumn.y1 = textColumn.y
	} else if textColumn.rotate == 270 {
		textColumn.x1 -= spaceBetweenParagraphs
		textColumn.y1 = textColumn.y
	}
	return []float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) drawLineOfText(page *Page, textLines []*TextLine) {
	if textColumn.alignment == align.Justify {
		var sumOfWordWidths float32
		for _, textLine := range textLines {
			sumOfWordWidths += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text)
		}
		dx := (textColumn.w - sumOfWordWidths) / float32(len(textLines)-1)
		for _, textLine := range textLines {
			textLine.SetLocation(textColumn.x1, textColumn.y1+textLine.GetVerticalOffset())
			if textLine.GetGoToAction() != nil {
				page.AddAnnotation(NewAnnotation(
					nil,          // The URI
					textLine.key, // The destination name
					textColumn.x,
					page.height-(textColumn.y-textLine.font.ascent),
					textColumn.x+textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text),
					page.height-(textColumn.y+textLine.font.descent),
					"",
					"",
					""))
			}

			if textColumn.rotate == 0 {
				textLine.SetTextDirection(0)
				textLine.DrawOn(page)
				textColumn.x1 += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text) + dx
			} else if textColumn.rotate == 90 {
				textLine.SetTextDirection(90)
				textLine.DrawOn(page)
				textColumn.y1 -= textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text) + dx
			} else if textColumn.rotate == 270 {
				textLine.SetTextDirection(270)
				textLine.DrawOn(page)
				textColumn.y1 += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text) + dx
			}
		}
	} else {
		textColumn.drawNonJustifiedLine(page, textLines)
	}
}

func (textColumn *TextColumn) drawNonJustifiedLine(page *Page, textLines []*TextLine) {
	var runLength float32
	for i := 0; i < len(textLines); i++ {
		textLine := textLines[i]
		if i < len(textLines)-1 {
			if textLine.GetTrailingSpace() {
				textLine.text += single.Space
			}
		}
		runLength += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text)
	}

	if textColumn.alignment == align.Center {
		if textColumn.rotate == 0 {
			textColumn.x1 = textColumn.x + ((textColumn.w - runLength) / 2)
		} else if textColumn.rotate == 90 {
			textColumn.y1 = textColumn.y - ((textColumn.w - runLength) / 2)
		} else if textColumn.rotate == 270 {
			textColumn.y1 = textColumn.y + ((textColumn.w - runLength) / 2)
		}
	} else if textColumn.alignment == align.Right {
		if textColumn.rotate == 0 {
			textColumn.x1 = textColumn.x + (textColumn.w - runLength)
		} else if textColumn.rotate == 90 {
			textColumn.y1 = textColumn.y - (textColumn.w - runLength)
		} else if textColumn.rotate == 270 {
			textColumn.y1 = textColumn.y + (textColumn.w - runLength)
		}
	}

	for _, textLine := range textLines {
		textLine.SetLocation(textColumn.x1, textColumn.y1+textLine.GetVerticalOffset())
		if textLine.uri != nil || textLine.key != nil {
			page.AddAnnotation(NewAnnotation(
				nil,                      // The URI
				textLine.GetGoToAction(), // The destination name
				textColumn.x,
				textColumn.y-textLine.font.ascent,
				textColumn.x+textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text),
				textColumn.y+textLine.font.descent,
				"",
				"",
				""))
		}

		if textColumn.rotate == 0 {
			textLine.SetTextDirection(0)
			textLine.DrawOn(page)
			textColumn.x1 += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text)
		} else if textColumn.rotate == 90 {
			textLine.SetTextDirection(90)
			textLine.DrawOn(page)
			textColumn.y1 -= textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text)
		} else if textColumn.rotate == 270 {
			textLine.SetTextDirection(270)
			textLine.DrawOn(page)
			textColumn.y1 += textLine.font.StringWidth(textLine.GetFallbackFont(), textLine.text)
		}
	}
}

// AddChineseParagraph adds a new paragraph with Chinese text to this text column.
//
// @param font the font used by this paragraph.
// @param chinese the Chinese text.
func (textColumn *TextColumn) AddChineseParagraph(font *Font, text string) {
	var paragraph *Paragraph
	var buf strings.Builder
	for _, ch := range text {
		if font.stringWidth(buf.String()+string(ch)) > textColumn.w {
			paragraph = NewParagraph()
			paragraph.Add(NewTextLine(font, buf.String()))
			textColumn.paragraphs = append(textColumn.paragraphs, paragraph)
			buf.Reset()
		}
		buf.WriteRune(ch)
	}
	paragraph = NewParagraph()
	paragraph.Add(NewTextLine(font, buf.String()))
	textColumn.AddParagraph(paragraph)
}

// AddJapaneseParagraph adds a new paragraph with Japanese text to this text column.
// @param font the font used by this paragraph.
// @param japanese the Japanese text.
func (textColumn *TextColumn) AddJapaneseParagraph(font *Font, text string) {
	textColumn.AddChineseParagraph(font, text)
}
