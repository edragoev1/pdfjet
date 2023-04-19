package pdfjet

/**
 * textframe.go
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
	"fmt"
	"strings"

	"github.com/edragoev1/pdfjet/src/single"
)

// TextFrame please see Example_47
type TextFrame struct {
	paragraphs               []*TextLine
	font                     *Font
	fallbackFont             *Font
	x, y, w, h, xText, yText float32
	leading                  float32
	paragraphLeading         float32
	beginParagraphPoints     [][]float32
	spaceBetweenTextLines    float32
}

// NewTextFrame constructs new text frame.
func NewTextFrame(paragraphs []*TextLine) *TextFrame {
	textFrame := new(TextFrame)
	if paragraphs != nil {
		textFrame.paragraphs = paragraphs
		textFrame.font = textFrame.paragraphs[0].GetFont()
		textFrame.fallbackFont = textFrame.paragraphs[0].GetFallbackFont()
		textFrame.leading = textFrame.font.GetBodyHeight()
		textFrame.paragraphLeading = 2 * textFrame.leading
		textFrame.beginParagraphPoints = make([][]float32, 0)
		textFrame.spaceBetweenTextLines = textFrame.font.StringWidth(textFrame.fallbackFont, single.Space)
		// Reverse the paragraphs
		for i, j := 0, len(paragraphs)-1; i < j; i, j = i+1, j-1 {
			paragraphs[i], paragraphs[j] = paragraphs[j], paragraphs[i]
		}
	}
	return textFrame
}

// SetLocation sets the location of the frame on the page.
func (frame *TextFrame) SetLocation(x, y float32) *TextFrame {
	frame.x = x
	frame.y = y
	return frame
}

// SetWidth sets the width of the frame.
func (frame *TextFrame) SetWidth(w float32) *TextFrame {
	frame.w = w
	return frame
}

// SetHeight sets the height of the frame.
func (frame *TextFrame) SetHeight(h float32) *TextFrame {
	frame.h = h
	return frame
}

// SetLeading sets the text lines leading.
func (frame *TextFrame) SetLeading(leading float32) *TextFrame {
	frame.leading = leading
	return frame
}

// SetParagraphLeading sets the paragraph leading.
func (frame *TextFrame) SetParagraphLeading(paragraphLeading float32) *TextFrame {
	frame.paragraphLeading = paragraphLeading
	return frame
}

// GetBeginParagraphPoints returns the begin paragraph points.
func (frame *TextFrame) GetBeginParagraphPoints() [][]float32 {
	return frame.beginParagraphPoints
}

// SetSpaceBetweenTextLines sets the space between the text lines.
func (frame *TextFrame) SetSpaceBetweenTextLines(spaceBetweenTextLines float32) *TextFrame {
	frame.spaceBetweenTextLines = spaceBetweenTextLines
	return frame
}

// GetParagraphs returns the paragraphs.
func (frame *TextFrame) GetParagraphs() []*TextLine {
	return frame.paragraphs
}

// SetPosition sets the position of the text frame on the page.
func (frame *TextFrame) SetPosition(x, y float32) {
	frame.SetLocation(x, y)
}

// DrawOn draws the text frame on the page.
func (frame *TextFrame) DrawOn(page *Page) []float32 {
	frame.xText = frame.x
	frame.yText = frame.y + frame.font.ascent
	for len(frame.paragraphs) > 0 {
		// The paragraphs are reversed so we can efficiently remove the first one:
		textLine := frame.paragraphs[len(frame.paragraphs)-1]
		textLine.SetLocation(frame.xText, frame.yText)
		frame.paragraphs = frame.paragraphs[:len(frame.paragraphs)-1]
		frame.beginParagraphPoints = append(frame.beginParagraphPoints, []float32{frame.xText, frame.yText})
		for {
			textLine = frame.drawLineOnPage(page, textLine)
			if textLine.text == "" {
				break
			}
			frame.yText = textLine.advance(frame.leading)
			if frame.yText+frame.font.descent >= (frame.y + frame.h) {
				// The paragraphs are reversed so we can efficiently add new first paragraph:
				frame.paragraphs = append(frame.paragraphs, textLine)
				return []float32{frame.x + frame.w, frame.y + frame.h}
			}
		}
		frame.xText = frame.x
		frame.yText += frame.paragraphLeading
	}
	return []float32{frame.x + frame.w, frame.y + frame.h}
}

func (frame *TextFrame) drawLineOnPage(page *Page, textLine *TextLine) *TextLine {
	var sb1 strings.Builder
	var sb2 strings.Builder
	tokens := strings.Fields(textLine.text)
	testForFit := true
	for _, token := range tokens {
		fmt.Println(textLine.GetWidth())
		fmt.Println()
		if testForFit && textLine.font.stringWidth(sb1.String()+token) < textLine.GetWidth() {
			sb1.WriteString(token + single.Space)
		} else {
			testForFit = false
			sb2.WriteString(token + single.Space)
		}
	}
	textLine.SetText(strings.TrimSpace(sb1.String()))
	if page != nil {
		textLine.DrawOn(page)
	}
	textLine.SetText(strings.TrimSpace(sb2.String()))
	return textLine
}

// IsNotEmpty returns true if there is more text to draw, otherwise it returns false.
func (frame *TextFrame) IsNotEmpty() bool {
	return len(frame.paragraphs) > 0
}
