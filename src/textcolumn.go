package pdfjet

/**
 * textcolumn.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"log"
	"strings"

	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/single"
)

// TextColumn is used to create text column objects and draw them on a page.
//
// Please see Example_10.
type TextColumn struct {
	alignment             int // = align.Left
	rotate                int
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

// NewTextColumn used to create a text column object and set the rotation angle.
// @param rotateByDegrees the specified rotation angle in degrees.
func NewTextColumn(rotateByDegrees int) *TextColumn {
	textColumn := new(TextColumn)
	textColumn.alignment = alignment.Left
	textColumn.lineSpacing = 1.3
	textColumn.paragraphSpacing = 1.0
	textColumn.rotate = rotateByDegrees
	textColumn.lineBetweenParagraphs = true
	if rotateByDegrees != 0 && rotateByDegrees != 90 && rotateByDegrees != 270 {
		log.Fatal("Invalid rotation angle. Please use 0, 90 or 270 degrees.")
	}
	textColumn.paragraphs = make([]*Paragraph, 0)
	return textColumn
}

// SetLineBetweenParagraphs sets the lineBetweenParagraphs private variable value.
// If the value is set to true - an empty line will be inserted between the current and next lines.
// @param lineBetweenParagraphs the specified boolean value.
func (textColumn *TextColumn) SetLineBetweenParagraphs(lineBetweenParagraphs bool) {
	textColumn.lineBetweenParagraphs = lineBetweenParagraphs
}

// SetLineSpacing sets the space between the lines.
func (textColumn *TextColumn) SetLineSpacing(lineSpacing float32) {
	textColumn.lineSpacing = lineSpacing
}

// SetParagraphSpacing sets the space between the lines.
func (textColumn *TextColumn) SetParagraphSpacing(paragraphSpacing float32) {
	textColumn.paragraphSpacing = paragraphSpacing
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
	var lineHeight = float32(0.0)
	var maxAscent = float32(0.0)
	for _, line := range paragraph.lines {
		if (line.GetHeight() * textColumn.lineSpacing) > lineHeight {
			lineHeight = line.GetHeight() * textColumn.lineSpacing
		}
		if line.font.GetAscent(line.GetFontSize()) > maxAscent {
			maxAscent = line.font.GetAscent(line.GetFontSize())
		}
	}
	if textColumn.rotate == 0 {
		textColumn.y1 += maxAscent
	} else if textColumn.rotate == 90 {
		textColumn.x1 += maxAscent
	} else if textColumn.rotate == 270 {
		textColumn.x1 -= maxAscent
	}

	var runLength float32
	for _, line := range paragraph.lines {
		tokens := strings.Fields(line.text)
		var text *TextLine
		for _, token := range tokens {
			text = NewTextLine(line.font, token+single.Space)
			text.SetFallbackFont(line.GetFallbackFont())
			text.SetFontSize(line.GetFontSize())
			text.SetTextColorRGB(line.GetTextColor())
			text.SetUnderline(line.GetUnderline())
			text.SetStrikeout(line.GetStrikeout())
			text.SetTextEffect(line.GetTextEffect())
			text.SetURIAction(line.GetURIAction())
			text.SetGoToAction(line.GetGoToAction())
			runLength += text.GetWidth()
			if runLength < textColumn.w {
				list = append(list, text)
			} else {
				textColumn.drawLineOfText(page, list)
				textColumn.moveToNextLine(lineHeight)
				list = make([]*TextLine, 0)
				list = append(list, text)
				runLength = text.GetWidth()
			}
		}
		if text != nil {
			text.isLastToken = true
		}
	}
	textColumn.drawNonJustifiedLine(page, list)

	if textColumn.lineBetweenParagraphs {
		return textColumn.moveToNextLine(lineHeight)
	}

	return textColumn.moveToNextParagraph(lineHeight * textColumn.paragraphSpacing)
}

func (textColumn *TextColumn) moveToNextLine(lineHeight float32) []float32 {
	if textColumn.rotate == 0 {
		textColumn.x1 = textColumn.x
		textColumn.y1 += lineHeight
	} else if textColumn.rotate == 90 {
		textColumn.x1 += lineHeight
		textColumn.y1 = textColumn.y
	} else if textColumn.rotate == 270 {
		textColumn.x1 -= lineHeight
		textColumn.y1 = textColumn.y
	}
	return []float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) moveToNextParagraph(paragraphSpacing float32) []float32 {
	if textColumn.rotate == 0 {
		textColumn.x1 = textColumn.x
		textColumn.y1 += paragraphSpacing
	} else if textColumn.rotate == 90 {
		textColumn.x1 += paragraphSpacing
		textColumn.y1 = textColumn.y
	} else if textColumn.rotate == 270 {
		textColumn.x1 -= paragraphSpacing
		textColumn.y1 = textColumn.y
	}
	return []float32{textColumn.x1, textColumn.y1}
}

func (textColumn *TextColumn) drawLineOfText(page *Page, textLines []*TextLine) {
	if textColumn.alignment == alignment.Justify {
		var sumOfWordWidths float32
		for _, textLine := range textLines {
			sumOfWordWidths += textLine.GetWidth()
		}
		dx := (textColumn.w - sumOfWordWidths) / float32(len(textLines)-1)
		for _, textLine := range textLines {
			textLine.SetLocation(textColumn.x1, textColumn.y1+textLine.GetVerticalOffset())
			if textLine.GetGoToAction() != "" {
				page.AddAnnotation(NewAnnotation(
					"Link",
					textColumn.x,
					page.height-(textColumn.y-textLine.font.ascent),
					textColumn.x+textLine.GetWidth(),
					page.height-(textColumn.y+textLine.font.descent),
					nil,
					nil,
					0.0,
					"",
					"",
					"",           // The URI
					textLine.key, // The destination name
					"",
					"",
					""))
			}

			if textColumn.rotate == 0 {
				textLine.SetTextDirection(0)
				textLine.DrawOn(page)
				textColumn.x1 += textLine.GetWidth() + dx
			} else if textColumn.rotate == 90 {
				textLine.SetTextDirection(90)
				textLine.DrawOn(page)
				textColumn.y1 -= textLine.GetWidth() + dx
			} else if textColumn.rotate == 270 {
				textLine.SetTextDirection(270)
				textLine.DrawOn(page)
				textColumn.y1 += textLine.GetWidth() + dx
			}
		}
	} else {
		textColumn.drawNonJustifiedLine(page, textLines)
	}
}

func (textColumn *TextColumn) drawNonJustifiedLine(page *Page, textLines []*TextLine) {
	var runLength float32
	for _, textLine := range textLines {
		runLength += textLine.GetWidth()
	}

	if textColumn.alignment == alignment.Center {
		if textColumn.rotate == 0 {
			textColumn.x1 = textColumn.x + ((textColumn.w - runLength) / 2)
		} else if textColumn.rotate == 90 {
			textColumn.y1 = textColumn.y - ((textColumn.w - runLength) / 2)
		} else if textColumn.rotate == 270 {
			textColumn.y1 = textColumn.y + ((textColumn.w - runLength) / 2)
		}
	} else if textColumn.alignment == alignment.Right {
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
		if textLine.uri != "" || textLine.key != "" {
			page.AddAnnotation(NewAnnotation(
				"Link",
				textColumn.x,
				textColumn.y-textLine.font.ascent,
				textColumn.x+textLine.GetWidth(),
				textColumn.y+textLine.font.descent,
				nil,
				nil,
				0.0,
				"",
				"",
				"",                       // The URI
				textLine.GetGoToAction(), // The destination name
				"",
				"",
				""))
		}

		if textColumn.rotate == 0 {
			textLine.SetTextDirection(0)
			textLine.DrawOn(page)
			textColumn.x1 += textLine.GetWidth()
		} else if textColumn.rotate == 90 {
			textLine.SetTextDirection(90)
			textLine.DrawOn(page)
			textColumn.y1 -= textLine.GetWidth()
		} else if textColumn.rotate == 270 {
			textLine.SetTextDirection(270)
			textLine.DrawOn(page)
			textColumn.y1 += textLine.GetWidth()
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
		if font.StringWidth(font.size, buf.String()+string(ch)) > textColumn.w {
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
