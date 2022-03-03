package pdfjet

/**
 * textblock.go
 *
Copyright 2020 Innovatics Inc.

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
	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/color"
	"strings"
)

// TextBlock is used for creating blocks of text.
type TextBlock struct {
	font, fallbackFont *Font
	text               string
	space              float32
	textAlign          int
	x                  float32
	y                  float32
	w                  float32
	h                  float32
	background         uint32
	brush              uint32
	drawBorder         bool
	uri                *string
	key                *string
	uriLanguage        string
	uriActualText      string
	uriAltDescription  string
}

// NewTextBlock returns new text block.
func NewTextBlock(font *Font, text string) *TextBlock {
	textBlock := new(TextBlock)
	textBlock.font = font
	textBlock.text = text
	textBlock.space = font.descent
	textBlock.textAlign = align.Left
	textBlock.w = 300.0
	textBlock.h = 200.0
	textBlock.background = color.White
	textBlock.brush = color.Black
	textBlock.drawBorder = false
	return textBlock
}

// SetFallbackFont sets the fallback font.
func (textBlock *TextBlock) SetFallbackFont(fallbackFont *Font) *TextBlock {
	textBlock.fallbackFont = fallbackFont
	return textBlock
}

// SetText sets the block text.
func (textBlock *TextBlock) SetText(text string) *TextBlock {
	textBlock.text = text
	return textBlock
}

// SetLocation Sets the location where this text block will be drawn on the page.
// @param x the x coordinate of the top left corner of the text block.
// @param y the y coordinate of the top left corner of the text block.
// @return the TextBlock object.
func (textBlock *TextBlock) SetLocation(x, y float32) *TextBlock {
	textBlock.x = x
	textBlock.y = y
	return textBlock
}

// SetWidth Sets the width of this text block.
// @param width the specified width.
// @return the TextBlock object.
func (textBlock *TextBlock) SetWidth(width float32) *TextBlock {
	textBlock.w = width
	return textBlock
}

// GetWidth Returns the text block width.
// @return the text block width.
func (textBlock *TextBlock) GetWidth() float32 {
	return textBlock.w
}

// SetHeight Sets the height of this text block.
// @param height the specified height.
// @return the TextBlock object.
func (textBlock *TextBlock) SetHeight(height float32) *TextBlock {
	textBlock.h = height
	return textBlock
}

// GetHeight Returns the text block height.
// @return the text block height.
func (textBlock *TextBlock) GetHeight() float32 {
	return textBlock.DrawOn(nil)[1]
}

// SetSpaceBetweenLines Sets the space between two lines of text.
// @param space the space between two lines.
// @return the TextBlock object.
func (textBlock *TextBlock) SetSpaceBetweenLines(space float32) *TextBlock {
	textBlock.space = space
	return textBlock
}

// GetSpaceBetweenLines Returns the space between two lines of text.
// @return float the space.
func (textBlock *TextBlock) GetSpaceBetweenLines() float32 {
	return textBlock.space
}

// SetTextAlignment Sets the text alignment.
// @param textAlign the alignment parameter.
// Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
func (textBlock *TextBlock) SetTextAlignment(textAlign int) *TextBlock {
	textBlock.textAlign = textAlign
	return textBlock
}

// GetTextAlignment Returns the text alignment.
// @return the alignment code.
func (textBlock *TextBlock) GetTextAlignment() int {
	return textBlock.textAlign
}

// SetBgColor Sets the background to the specified color.
// @param color the color specified as 0xRRGGBB integer.
// @return the TextBlock object.
func (textBlock *TextBlock) SetBgColor(color uint32) *TextBlock {
	textBlock.background = color
	return textBlock
}

// GetBgColor Returns the background color.
// @return int the color as 0xRRGGBB integer.
func (textBlock *TextBlock) GetBgColor() uint32 {
	return textBlock.background
}

// SetBrushColor Sets the brush color.
// @param color the color specified as 0xRRGGBB integer.
// @return the TextBlock object.
func (textBlock *TextBlock) SetBrushColor(color uint32) *TextBlock {
	textBlock.brush = color
	return textBlock
}

// GetBrushColor Returns the brush color.
// @return int the brush color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) GetBrushColor() uint32 {
	return textBlock.brush
}

// SetDrawBorder sets the draw border flag.
func (textBlock *TextBlock) SetDrawBorder(drawBorder bool) *TextBlock {
	textBlock.drawBorder = drawBorder
	return textBlock
}

// IsCJK returns true if the the text is Chinese, Japanese or Korean.
// Otherwise it returns false.
func (textBlock *TextBlock) IsCJK(text string) bool {
	cjk := 0
	other := 0
	runes := []rune(text)
	for _, ch := range runes {
		if ch >= 0x4E00 && ch <= 0x9FFF || // Unified CJK
			ch >= 0xAC00 && ch <= 0xD7AF || // Hangul (Korean)
			ch >= 0x30A0 && ch <= 0x30FF || // Katakana (Japanese)
			ch >= 0x3040 && ch <= 0x309F { // Hiragana (Japanese)
			cjk++
		} else {
			other++
		}
	}
	return cjk > other
}

// DrawOn draws this text block on the specified page.
// @param page the page to draw this text block on.
// @return the TextBlock object.
func (textBlock *TextBlock) DrawOn(page *Page) [2]float32 {
	if page != nil {
		if textBlock.GetBgColor() != color.White {
			page.SetBrushColor(textBlock.background)
			page.FillRect(textBlock.x, textBlock.y, textBlock.w, textBlock.h)
		}
		page.SetBrushColor(textBlock.brush)
	}
	return textBlock.drawText(page)
}

func (textBlock *TextBlock) drawText(page *Page) [2]float32 {
	list := make([]string, 0)
	var buf strings.Builder
	lines := strings.Split(strings.Replace(textBlock.text, "\r\n", "\n", -1), "\n")
	for _, line := range lines {
		if textBlock.IsCJK(line) {
			buf.Reset()
			runes := []rune(line)
			for _, ch := range runes {
				if textBlock.font.StringWidth(textBlock.fallbackFont, buf.String()+string(ch)) < textBlock.w {
					buf.WriteRune(ch)
				} else {
					list = append(list, buf.String())
					buf.Reset()
					buf.WriteRune(ch)
				}
			}
			if strings.TrimSpace(buf.String()) != "" {
				list = append(list, strings.TrimSpace(buf.String()))
			}
		} else {
			if textBlock.font.StringWidth(textBlock.fallbackFont, line) < textBlock.w {
				list = append(list, line)
			} else {
				buf.Reset()
				tokens := SplitTextIntoTokens(line, textBlock.font, textBlock.fallbackFont, textBlock.w)
				for _, token := range tokens {
					if textBlock.font.StringWidth(textBlock.fallbackFont,
						strings.TrimSpace(buf.String()+" "+token)) < textBlock.w {
						buf.WriteString(" " + token)
					} else {
						list = append(list, strings.TrimSpace(buf.String()))
						buf.Reset()
						buf.WriteString(token)
					}
				}
				str := strings.TrimSpace(buf.String())
				if str != "" {
					list = append(list, str)
				}
			}
		}
	}
	lines = list

	var xText float32
	var yText float32 = textBlock.y + textBlock.font.ascent
	for i, line := range lines {
		if textBlock.textAlign == align.Left {
			xText = textBlock.x
		} else if textBlock.textAlign == align.Right {
			xText = (textBlock.x + textBlock.w) - textBlock.font.StringWidth(textBlock.fallbackFont, line)
		} else if textBlock.textAlign == align.Center {
			xText = textBlock.x + (textBlock.w-textBlock.font.StringWidth(textBlock.fallbackFont, line))/2
		} else {
			log.Fatal("Invalid text alignment option.")
		}
		if page != nil {
			page.DrawString(textBlock.font, textBlock.fallbackFont, line, xText, yText)
		}
		if i < (len(lines) - 1) {
			yText += textBlock.font.bodyHeight + textBlock.space
		}
	}

	textBlock.h = (yText - textBlock.y) + textBlock.font.descent
	if page != nil && textBlock.drawBorder {
		box := NewBox()
		box.SetLocation(textBlock.x, textBlock.y)
		box.SetSize(textBlock.w, textBlock.h)
		box.DrawOn(page)
	}

	if page != nil && (textBlock.uri != nil || textBlock.key != nil) {
		page.AddAnnotation(NewAnnotation(
			textBlock.uri,
			textBlock.key, // The destination name
			textBlock.x,
			textBlock.y,
			textBlock.x+textBlock.w,
			textBlock.y+textBlock.h,
			textBlock.uriLanguage,
			textBlock.uriActualText,
			textBlock.uriAltDescription))

	}

	return [2]float32{textBlock.x + textBlock.w, textBlock.y + textBlock.h}
}

// SetURIAction -- TODO:
func (textBlock *TextBlock) SetURIAction(uri string) *TextBlock {
	textBlock.uri = &uri
	return textBlock
}
