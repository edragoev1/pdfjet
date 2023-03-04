package pdfjet

/**
 * text.go
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

	"github.com/edragoev1/pdfjet/src/single"
)

// Text structure
// Please see Example_45
type Text struct {
	paragraphs                                       []*Paragraph
	font, fallbackFont                               *Font
	x1, y1, xText, yText, width                      float32
	leading, paragraphLeading, spaceBetweenTextLines float32
	beginParagraphPoints                             [][2]float32
	drawBorder                                       bool
}

// NewText is the constructor.
func NewText(paragraphs []*Paragraph) *Text {
	text := new(Text)
	text.paragraphs = paragraphs
	text.font = paragraphs[0].lines[0].GetFont()
	text.fallbackFont = paragraphs[0].lines[0].GetFallbackFont()
	text.leading = text.font.GetBodyHeight()
	text.paragraphLeading = 2 * text.leading
	text.beginParagraphPoints = make([][2]float32, 0)
	text.spaceBetweenTextLines = text.font.StringWidth(text.fallbackFont, single.Space)
	text.drawBorder = true
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

// GetBeginParagraphPoints returns the begin paragraph points.
func (text *Text) GetBeginParagraphPoints() [][2]float32 {
	return text.beginParagraphPoints
}

// SetSpaceBetweenTextLines sets the space between text lines.
func (text *Text) SetSpaceBetweenTextLines(spaceBetweenTextLines float32) *Text {
	text.spaceBetweenTextLines = spaceBetweenTextLines
	return text
}

// GetSize returns the size of the text block.
func (text *Text) GetSize() [2]float32 {
	return [2]float32{text.width, (text.yText + text.font.descent) - (text.y1 + text.paragraphLeading)}
}

// DrawOn draws the text on the page.
func (text *Text) DrawOn(page *Page) [2]float32 {
	text.xText = text.x1
	text.yText = text.y1 + text.font.ascent
	for _, paragraph := range text.paragraphs {
		var buf strings.Builder
		for _, textLine := range paragraph.lines {
			buf.WriteString(textLine.text)
		}
		for i, textLine := range paragraph.lines {
			if i == 0 {
				text.beginParagraphPoints = append(text.beginParagraphPoints, [2]float32{text.xText, text.yText})
			}
			xy := text.drawTextLine(page, text.xText, text.yText, textLine)
			text.xText = xy[0]
			if textLine.GetTrailingSpace() {
				text.xText += text.spaceBetweenTextLines
			}
			text.yText = xy[1]
		}
		text.xText = text.x1
		text.yText += text.paragraphLeading
	}

	height := ((text.yText - text.paragraphLeading) - text.y1) + text.font.descent
	if page != nil && text.drawBorder {
		box := NewBox()
		box.SetLocation(text.x1, text.y1)
		box.SetSize(text.width, height)
		box.DrawOn(page)
	}

	return [2]float32{text.x1 + text.width, text.y1 + height}
}

func (text *Text) drawTextLine(page *Page, x, y float32, textLine *TextLine) []float32 {
	text.xText = x
	text.yText = y

	var tokens []string
	if text.stringIsCJK(textLine.text) {
		tokens = text.tokenizeCJK(textLine, text.width)
	} else {
		tokens = strings.Fields(textLine.text)
	}

	var buf strings.Builder
	for i, token := range tokens {
		if i > 0 {
			token = single.Space + tokens[i]
		}
		lineWidth := textLine.font.StringWidth(textLine.fallbackFont, buf.String())
		tokenWidth := textLine.font.StringWidth(textLine.fallbackFont, token)
		if (lineWidth + tokenWidth) < (text.x1+text.width)-text.xText {
			buf.WriteString(token)
		} else {
			if page != nil {
				textLine2 := NewTextLine(textLine.font, buf.String())
				textLine2.SetFallbackFont(textLine.fallbackFont)
				textLine2.SetLocation(text.xText, text.yText+textLine.GetVerticalOffset())
				textLine2.SetColor(textLine.GetColor())
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
		textLine2.SetFallbackFont(textLine.fallbackFont)
		textLine2.SetLocation(text.xText, text.yText+textLine.GetVerticalOffset())
		textLine2.SetColor(textLine.GetColor())
		textLine2.SetUnderline(textLine.GetUnderline())
		textLine2.SetStrikeout(textLine.GetStrikeout())
		textLine2.SetLanguage(textLine.GetLanguage())
		textLine2.DrawOn(page)
	}

	return []float32{text.xText + textLine.font.StringWidth(textLine.fallbackFont, buf.String()), text.yText}
}

func (text *Text) stringIsCJK(str string) bool {
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
	runes := []rune(textLine.text)
	for _, ch := range runes {
		if text.font.StringWidth(text.fallbackFont, sb.String()+string(ch)) < textWidth {
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
