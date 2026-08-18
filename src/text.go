/**
 * text.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pdfjet

import (
	"strings"

	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/single"
)

// Text structure
// Please see Example_41
type Text struct {
	paragraphs                  []*Paragraph
	font, fallbackFont          *Font
	x1, y1, xText, yText, width float32
	leading                     float32
	paragraphLeading            float32
	hasBorder                   bool
	borderColor                 [3]float32
	borderWidth                 float32
	borderPattern               string
}

// NewText is the constructor.
func NewText(paragraphs []*Paragraph) *Text {
	text := new(Text)
	text.paragraphs = paragraphs
	text.font = paragraphs[0].lines[0].GetFont()
	text.fallbackFont = paragraphs[0].lines[0].GetFallbackFont()
	text.leading = text.font.ascent + text.font.descent
	text.paragraphLeading = 2 * text.leading
	text.borderColor = [3]float32{0.0, 0.0, 0.0}
	text.borderWidth = 0.5
	text.borderPattern = "[] 0"
	return text
}

// SetLocation sets the location of the text.
func (text *Text) SetLocation(x, y float32) *Text {
	text.x1 = x
	text.y1 = y
	return text
}

// SetWidth sets the width of the text component.
func (text *Text) SetWidth(width float32) *Text {
	text.width = width
	return text
}

// SetLeading sets the leading of the text.
func (text *Text) SetLeading(leading float32) *Text {
	text.leading = leading
	return text
}

// SetParagraphLeading sets the paragraph leading.
func (text *Text) SetParagraphLeading(paragraphLeading float32) *Text {
	text.paragraphLeading = paragraphLeading
	return text
}

// GetSize returns the size of the text block.
func (text *Text) GetSize() [2]float32 {
	return [2]float32{text.width, text.yText + text.font.descent}
}

func (text *Text) SetBorderColor(color int32) {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	text.SetBorderColorRGB([3]float32{r, g, b})
}

func (text *Text) SetBorderColorRGB(borderColor [3]float32) {
	text.borderColor = borderColor
	text.hasBorder = true
}

// DrawOn draws the text on the page.
func (text *Text) DrawOn(page *Page) [2]float32 {
	text.xText = text.x1
	text.yText = text.y1 + text.paragraphs[0].GetTextLines()[0].GetFont().ascent
	for _, paragraph := range text.paragraphs {
		paragraph.x1 = text.x1
		paragraph.y1 = text.yText - paragraph.lines[0].font.ascent
		paragraph.xText = text.xText
		paragraph.yText = text.yText
		for _, textLine := range paragraph.lines {
			point := text.drawTextLine(page, text.xText, text.yText, textLine)
			text.xText = point[0]
			text.yText = point[1]
			paragraph.x2 = text.xText
			paragraph.y2 = text.yText + textLine.font.GetDescent(textLine.font.size)
		}
		text.xText = text.x1
		text.yText += text.paragraphLeading
	}

	height := ((text.yText - text.paragraphLeading) - text.y1) + text.font.descent
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
		tokens = strings.Fields(textLine.text)
	}

	var buf strings.Builder
	for i, token := range tokens {
		if i > 0 {
			token = single.Space + tokens[i]
		}
		lineWidth := textLine.font.StringWidthFB(textLine.fallbackFont, textLine.font.size, buf.String())
		tokenWidth := textLine.font.StringWidthFB(textLine.fallbackFont, textLine.font.size, token)
		if (lineWidth + tokenWidth) < (text.x1+text.width)-text.xText {
			buf.WriteString(token)
		} else {
			if page != nil {
				textLine2 := NewTextLine(textLine.font, buf.String())
				textLine2.SetFallbackFont(textLine.GetFallbackFont())
				textLine2.SetFontSize(textLine.GetFontSize())
				textLine2.SetLocation(text.xText, text.yText+textLine.GetVerticalOffset())
				textLine2.SetTextColorRGB(textLine.GetTextColor())
				textLine2.SetColorMap(textLine.GetColorMap())
				textLine2.SetUnderline(textLine.GetUnderline())
				textLine2.SetStrikeout(textLine.GetStrikeout())
				textLine2.SetLanguage(textLine.GetLanguage())
				textLine2.DrawOn(page)
			}
			text.xText = text.x1
			text.yText += text.leading
			buf.Reset()
			buf.WriteString(tokens[i])
		}
	}
	if page != nil {
		textLine2 := NewTextLine(textLine.font, buf.String())
		textLine2.SetFallbackFont(textLine.GetFallbackFont())
		textLine2.SetFontSize(textLine.GetFontSize())
		textLine2.SetLocation(text.xText, text.yText+textLine.GetVerticalOffset())
		textLine2.SetTextColorRGB(textLine.GetTextColor())
		textLine2.SetColorMap(textLine.GetColorMap())
		textLine2.SetUnderline(textLine.GetUnderline())
		textLine2.SetStrikeout(textLine.GetStrikeout())
		textLine2.SetLanguage(textLine.GetLanguage())
		textLine2.DrawOn(page)
	}

	return []float32{text.xText + textLine.font.StringWidthFB(
		textLine.fallbackFont, textLine.font.size, buf.String()), text.yText}
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
		if text.font.StringWidthFB(text.fallbackFont, text.font.size, sb.String()+string(ch)) < textWidth {
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
