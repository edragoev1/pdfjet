// text.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/single"
)

// Text is paragraphs of text lines, wrapped at a width, with an optional border.
// Please see Example_03, Example_41 and Example_49.
type Text struct {
	paragraphs                  []*Paragraph
	x1, y1, xText, yText, width float32
	paragraphLeading            float32
	hasBorder                   bool
	borderColor                 [3]float32
	borderWidth                 float32
	borderPattern               string
}

// NewText creates a text object from the paragraphs.
func NewText(paragraphs []*Paragraph) *Text {
	text := new(Text)
	text.paragraphs = paragraphs
	text.paragraphLeading = 24.0
	text.borderColor = [3]float32{0.0, 0.0, 0.0}
	text.borderWidth = 0.5
	text.borderPattern = "[] 0"
	return text
}

// SetLocation sets the location of the top left corner of this text.
func (text *Text) SetLocation(x, y float32) Drawable {
	text.x1 = x
	text.y1 = y
	return text
}

// SetWidth sets the width at which the lines wrap.
func (text *Text) SetWidth(width float32) *Text {
	text.width = width
	return text
}

// SetParagraphLeading sets the vertical distance between paragraphs.
func (text *Text) SetParagraphLeading(paragraphLeading float32) *Text {
	text.paragraphLeading = paragraphLeading
	return text
}

// SetBorderWidth sets the width of the border.
func (text *Text) SetBorderWidth(borderWidth float32) *Text {
	text.borderWidth = borderWidth
	return text
}

// SetBorderPattern sets the dash pattern of the border, for example "[3] 0".
func (text *Text) SetBorderPattern(borderPattern string) *Text {
	text.borderPattern = borderPattern
	return text
}

// SetBorderColor sets the border color as a 0xRRGGBB value and draws a border
// around this text. color.Transparent removes the border.
func (text *Text) SetBorderColor(c int32) *Text {
	if c == color.Transparent {
		text.hasBorder = false
		return text
	}
	r := float32((c>>16)&0xff) / 255.0
	g := float32((c>>8)&0xff) / 255.0
	b := float32((c)&0xff) / 255.0
	text.SetBorderColorRGB([3]float32{r, g, b})
	return text
}

// SetBorderColorRGB sets the border color from red, green and blue values and draws a border around this text.
func (text *Text) SetBorderColorRGB(borderColor [3]float32) *Text {
	text.borderColor = borderColor
	text.hasBorder = true
	return text
}

// DrawOn draws the paragraphs on the specified page and returns the x and y
// coordinates of the bottom right corner of this text. With no page nothing is
// drawn and the paragraphs get their coordinates.
func (text *Text) DrawOn(page *Page) [2]float32 {
	firstLine := text.paragraphs[0].lines[0]
	text.xText = text.x1
	text.yText = text.y1 + firstLine.font.GetAscentAt(firstLine.fontSize)
	for _, paragraph := range text.paragraphs {
		firstLine = paragraph.lines[0]
		paragraph.x1 = text.x1
		paragraph.y1 = text.yText - firstLine.font.GetAscentAt(firstLine.fontSize)
		paragraph.xText = text.xText
		paragraph.yText = text.yText
		for _, textLine := range paragraph.lines {
			point := text.drawTextLine(page, text.xText, text.yText, textLine)
			text.xText = point[0]
			text.yText = point[1]
			paragraph.x2 = text.xText
			paragraph.y2 = text.yText + textLine.font.GetDescentAt(textLine.fontSize)
		}
		text.xText = text.x1
		text.yText += text.paragraphLeading
	}

	lastParagraph := text.paragraphs[len(text.paragraphs)-1]
	lastTextLine := lastParagraph.GetTextLines()[len(lastParagraph.GetTextLines())-1]
	height := ((text.yText - text.paragraphLeading) - text.y1) +
		lastTextLine.font.GetDescentAt(lastTextLine.fontSize)
	if text.hasBorder {
		rect := NewRect(text.x1, text.y1, text.width, height)
		rect.SetBorderColorRGB(text.borderColor)
		rect.SetBorderWidth(text.borderWidth)
		rect.SetBorderPattern(text.borderPattern)
		rect.DrawOn(page)
	}

	return [2]float32{text.x1 + text.width, text.y1 + height}
}

func (text *Text) drawTextLine(page *Page, x, y float32, textLine *TextLine) []float32 {
	text.xText = x
	text.yText = y

	var tokens []string
	if text.textIsCJK(textLine.text) {
		tokens = text.tokenizeCJK(textLine, text.width)
	} else {
		tokens = strings.FieldsFunc(textLine.text, isASCIIWhitespace)
	}

	font := textLine.font
	fallbackFont := textLine.fallbackFont
	fontSize := textLine.fontSize
	var buf strings.Builder
	for _, token := range tokens {
		lineWidth := font.StringWidthFB(fallbackFont, fontSize, buf.String())
		tokenWidth := font.StringWidthFB(fallbackFont, fontSize, token+single.Space)
		if (lineWidth + tokenWidth) < (text.x1+text.width)-text.xText {
			buf.WriteString(token)
			buf.WriteString(single.Space)
		} else {
			text.drawLine(page, textLine, buf.String())
			text.xText = text.x1
			text.yText += textLine.GetHeight()
			buf.Reset()
			buf.WriteString(token)
			buf.WriteString(single.Space)
		}
	}
	text.drawLine(page, textLine, buf.String())

	return []float32{text.xText + font.StringWidthFB(fallbackFont, fontSize, buf.String()), text.yText}
}

// drawLine draws one wrapped line of the text line at the current location,
// with the text line's font, colors and decorations.
func (text *Text) drawLine(page *Page, textLine *TextLine, str string) {
	line := NewTextLine(textLine.font, str)
	line.SetFallbackFont(textLine.GetFallbackFont())
	line.SetFontSize(textLine.GetFontSize())
	line.SetTextColorRGB(textLine.GetTextColor())
	line.SetColorMap(textLine.GetColorMap())
	line.SetUnderline(textLine.GetUnderline())
	line.SetStrikeout(textLine.GetStrikeout())
	line.SetLanguage(textLine.GetLanguage())
	line.SetLocation(text.xText, text.yText)
	line.DrawOn(page)
}

func (text *Text) textIsCJK(str string) bool {
	// CJK Unified Ideographs Range: 4E00–9FD5
	// Hiragana Range: 3040–309F
	// Katakana Range: 30A0–30FF
	// Hangul Jamo Range: 1100–11FF
	numOfCJK := 0
	runes := []rune(str)
	for _, ch := range runes {
		if (ch >= 0x4E00 && ch <= 0x9FD5) ||
			(ch >= 0x3040 && ch <= 0x309F) ||
			(ch >= 0x30A0 && ch <= 0x30FF) ||
			(ch >= 0x1100 && ch <= 0x11FF) {
			numOfCJK++
		}
	}
	return numOfCJK > (len(runes) / 2)
}

func (text *Text) tokenizeCJK(textLine *TextLine, textWidth float32) []string {
	tokens := make([]string, 0)
	var sb strings.Builder
	for _, ch := range textLine.text {
		if textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, sb.String()+string(ch)) < textWidth {
			sb.WriteRune(ch)
		} else {
			tokens = append(tokens, sb.String())
			sb.Reset()
			sb.WriteRune(ch)
		}
	}
	if len(sb.String()) > 0 {
		tokens = append(tokens, sb.String())
	}
	return tokens
}

// ReadLines reads the lines of a UTF-8 text file, without carriage returns.
// It exits the program if the file cannot be read.
func ReadLines(filePath string) []string {
	lines := make([]string, 0)
	var buffer strings.Builder
	for _, ch := range content.OfTextFile(filePath) {
		if ch == '\n' {
			lines = append(lines, buffer.String())
			buffer.Reset()
		} else {
			buffer.WriteRune(ch)
		}
	}
	if buffer.Len() > 0 {
		lines = append(lines, buffer.String())
	}
	return lines
}

// ParagraphsFromFile reads a text file and returns its paragraphs. An empty line separates the paragraphs.
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
