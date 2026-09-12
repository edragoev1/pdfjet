// textframe.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/single"
)

// TextFrame is paragraphs of text lines, wrapped at the width of the frame,
// with an optional border. A frame with a height draws as much of the text as
// fits and keeps the rest for the next frame, so text flows from frame to
// frame. A frame without a height draws all of the text, as Text does. Please
// see Example_47.
type TextFrame struct {
	paragraphs       []*Paragraph
	x, y, w, h       float32
	paragraphLeading float32
	border           bool
	borderColor      [3]float32
	borderWidth      float32
	borderPattern    string

	// The text that is not drawn yet starts at this paragraph, at this text
	// line of the paragraph and at this token of the text line. The tokens are
	// nil when the text line has not been started.
	paragraphIndex int
	lineIndex      int
	tokens         []string
	tokenIndex     int

	// The row of text being drawn: where the text goes next, whether the row
	// can take more text, whether the frame has a row yet, and where the next
	// row goes.
	xText, yText float32
	rowOpen      bool
	rowPlaced    bool
	nextBaseline float32
}

// NewTextFrame creates a text frame from strings, one paragraph each, in the
// font at its size. An empty line separates the paragraphs.
func NewTextFrame(f1 *Font, inputList []string) *TextFrame {
	paragraphs := make([]*Paragraph, 0, len(inputList))
	for _, text := range inputList {
		paragraphs = append(paragraphs, NewParagraph().Add(NewTextLine(f1, text)))
	}
	tf := NewTextFrameFromParagraphs(paragraphs)
	tf.paragraphLeading = 2 * f1.GetBodyHeight()
	return tf
}

// NewTextFrameFromParagraphs creates a text frame from paragraphs of text
// lines. The paragraphs are 24 points apart unless SetParagraphLeading says
// otherwise.
func NewTextFrameFromParagraphs(paragraphs []*Paragraph) *TextFrame {
	return &TextFrame{
		paragraphs:       paragraphs,
		paragraphLeading: 24.0,
		borderColor:      [3]float32{0.0, 0.0, 1.0},
		borderWidth:      0.5,
		borderPattern:    "[] 0",
	}
}

// SetLocation sets the location of the top left corner of this text frame.
func (tf *TextFrame) SetLocation(x, y float32) Drawable {
	tf.x = x
	tf.y = y
	return tf
}

// SetWidth sets the width at which the lines wrap.
func (tf *TextFrame) SetWidth(w float32) *TextFrame {
	tf.w = w
	return tf
}

// SetHeight sets the height of this text frame. With a height of 0, the
// default, the frame draws all of its text.
func (tf *TextFrame) SetHeight(h float32) *TextFrame {
	tf.h = h
	return tf
}

// GetWidth returns the width of this text frame.
func (tf *TextFrame) GetWidth() float32 {
	return tf.w
}

// GetHeight returns the height of this text frame.
func (tf *TextFrame) GetHeight() float32 {
	return tf.h
}

// SetParagraphLeading sets the vertical distance between paragraphs, from the
// baseline of the last line of a paragraph to the baseline of the first line
// of the next.
func (tf *TextFrame) SetParagraphLeading(paragraphLeading float32) *TextFrame {
	tf.paragraphLeading = paragraphLeading
	return tf
}

// SetBorder sets whether a border is drawn around this text frame.
func (tf *TextFrame) SetBorder(border bool) *TextFrame {
	tf.border = border
	return tf
}

// SetBorderColor sets the border color as a 0xRRGGBB value and draws a border
// around this text frame. color.Transparent removes the border.
func (tf *TextFrame) SetBorderColor(c int32) *TextFrame {
	if c == color.Transparent {
		tf.border = false
		return tf
	}
	return tf.SetBorderColorRGB(colorToRGB(c))
}

// SetBorderColorRGB sets the border color from red, green and blue values and
// draws a border around this text frame.
func (tf *TextFrame) SetBorderColorRGB(borderColor [3]float32) *TextFrame {
	tf.borderColor = borderColor
	tf.border = true
	return tf
}

// SetBorderWidth sets the border width.
func (tf *TextFrame) SetBorderWidth(borderWidth float32) *TextFrame {
	tf.borderWidth = borderWidth
	return tf
}

// SetBorderPattern sets the dash pattern of the border, for example "[3] 0".
func (tf *TextFrame) SetBorderPattern(borderPattern string) *TextFrame {
	tf.borderPattern = borderPattern
	return tf
}

// HasMoreText returns true if some of the text has not been drawn yet.
func (tf *TextFrame) HasMoreText() bool {
	return tf.paragraphIndex < len(tf.paragraphs)
}

// DrawOn draws the text on the page: all of it when this frame has no height,
// or as much as fits in the height, keeping the rest for the next frame. The
// first line of a frame is drawn even when it does not fit, so the text always
// flows, and a word wider than the frame is broken. With no page nothing is
// drawn, the paragraphs get their coordinates and the text is kept. It returns
// the x and y coordinates of the bottom right corner of this frame, or of the
// text when the frame has no height.
func (tf *TextFrame) DrawOn(page *Page) [2]float32 {
	startParagraph := tf.paragraphIndex
	startLine := tf.lineIndex
	var startTokens []string
	if tf.tokens != nil {
		startTokens = make([]string, len(tf.tokens))
		copy(startTokens, tf.tokens)
	}
	startToken := tf.tokenIndex

	bottom := tf.drawParagraphs(page)
	if tf.h > 0 {
		bottom = tf.y + tf.h
	}
	if tf.border {
		rect := NewRect(tf.x, tf.y, tf.w, bottom-tf.y)
		rect.SetBorderColorRGB(tf.borderColor)
		rect.SetBorderWidth(tf.borderWidth)
		rect.SetBorderPattern(tf.borderPattern)
		rect.DrawOn(page)
	}

	if page == nil {
		tf.paragraphIndex = startParagraph
		tf.lineIndex = startLine
		tf.tokens = startTokens
		tf.tokenIndex = startToken
	}
	return [2]float32{tf.x + tf.w, bottom}
}

// drawParagraphs draws the text that is left, as much of it as fits in the
// height of the frame, and returns the bottom of the text drawn.
func (tf *TextFrame) drawParagraphs(page *Page) float32 {
	tf.xText = tf.x
	tf.rowOpen = false
	tf.rowPlaced = false
	bottom := tf.y
	for tf.paragraphIndex < len(tf.paragraphs) {
		paragraph := tf.paragraphs[tf.paragraphIndex]
		for tf.lineIndex < len(paragraph.lines) {
			textLine := paragraph.lines[tf.lineIndex]
			if !tf.rowOpen && !tf.openRow(textLine) {
				return bottom
			}
			if tf.tokens == nil {
				if tf.lineIndex == 0 {
					paragraph.x1 = tf.x
					paragraph.y1 = tf.yText - textLine.font.GetAscentAt(textLine.fontSize)
					paragraph.xText = tf.xText
					paragraph.yText = tf.yText
				}
				tf.tokens = tf.tokenize(textLine)
				tf.tokenIndex = 0
			}
			if !tf.drawTokens(page, textLine) {
				return bottom
			}
			paragraph.x2 = tf.xText
			paragraph.y2 = tf.yText + textLine.font.GetDescentAt(textLine.fontSize)
			bottom = paragraph.y2
			tf.tokens = nil
			tf.tokenIndex = 0
			tf.lineIndex++
		}
		tf.xText = tf.x
		tf.rowOpen = false
		tf.nextBaseline = tf.yText + tf.paragraphLeading
		tf.paragraphIndex++
		tf.lineIndex = 0
	}
	return bottom
}

// openRow starts a row of text for the text line, below the previous row or at
// the top of the frame. It returns false when the row does not fit in the
// height of the frame. The first row of a frame always fits, so the text keeps
// flowing.
func (tf *TextFrame) openRow(textLine *TextLine) bool {
	baseline := tf.y + textLine.font.GetAscentAt(textLine.fontSize)
	if tf.rowPlaced {
		baseline = tf.nextBaseline
	}
	if tf.h > 0 && tf.rowPlaced &&
		(baseline+textLine.font.GetDescentAt(textLine.fontSize)) > (tf.y+tf.h) {
		return false
	}
	tf.xText = tf.x
	tf.yText = baseline
	tf.rowOpen = true
	tf.rowPlaced = true
	return true
}

// drawTokens draws the tokens of the text line that are left, wrapping them at
// the width of the frame. It returns false when a row does not fit in the
// height of the frame; the tokens that are left stay for the next frame.
func (tf *TextFrame) drawTokens(page *Page, textLine *TextLine) bool {
	font := textLine.font
	fallbackFont := textLine.fallbackFont
	fontSize := textLine.fontSize
	var buf strings.Builder
	for tf.tokenIndex < len(tf.tokens) {
		if !tf.rowOpen && !tf.openRow(textLine) {
			return false
		}
		token := tf.tokens[tf.tokenIndex]
		runLength := font.StringWidthFB(fallbackFont, fontSize, buf.String())
		tokenWidth := font.StringWidthFB(fallbackFont, fontSize, token+single.Space)
		if (runLength + tokenWidth) < ((tf.x + tf.w) - tf.xText) {
			buf.WriteString(token)
			buf.WriteString(single.Space)
			tf.tokenIndex++
			continue
		}
		if buf.Len() == 0 && tf.xText == tf.x {
			// The token does not fit in an empty row, so the row takes as much of it as fits.
			head := tf.headThatFits(textLine, token)
			buf.WriteString(head)
			if len(head) == len(token) {
				tf.tokenIndex++
			} else {
				tf.tokens[tf.tokenIndex] = token[len(head):]
			}
		}
		tf.drawLine(page, textLine, buf.String())
		buf.Reset()
		tf.xText = tf.x
		tf.rowOpen = false
		tf.nextBaseline = tf.yText + textLine.GetHeight()
	}
	tf.drawLine(page, textLine, buf.String())
	tf.xText += font.StringWidthFB(fallbackFont, fontSize, buf.String())
	return true
}

// headThatFits returns the longest start of the token that is narrower than the
// frame, and at least the first character of the token.
func (tf *TextFrame) headThatFits(textLine *TextLine, token string) string {
	_, size := utf8.DecodeRuneInString(token)
	end := size
	for end < len(token) {
		_, size = utf8.DecodeRuneInString(token[end:])
		if textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, token[:end+size]) >= tf.w {
			break
		}
		end += size
	}
	return token[:end]
}

// drawLine draws the string at the current text position, with the text line's
// font, colors and decorations.
func (tf *TextFrame) drawLine(page *Page, textLine *TextLine, str string) {
	line := NewTextLine(textLine.font, str)
	line.SetFallbackFont(textLine.GetFallbackFont())
	line.SetFontSize(textLine.GetFontSize())
	line.SetTextColorRGB(textLine.GetTextColor())
	line.SetColorMap(textLine.GetColorMap())
	line.SetUnderline(textLine.GetUnderline())
	line.SetStrikeout(textLine.GetStrikeout())
	line.SetLanguage(textLine.GetLanguage())
	line.SetLocation(tf.xText, tf.yText)
	line.DrawOn(page)
}

// tokenize splits the text of the text line into words, or, for CJK text,
// which has no spaces between its words, into runs of characters that fit in
// the width. The tokens are never nil.
func (tf *TextFrame) tokenize(textLine *TextLine) []string {
	list := make([]string, 0)
	if !isCJK(textLine.text) {
		return append(list, splitOnWhitespace(textLine.text)...)
	}
	var buf strings.Builder
	for _, ch := range textLine.text {
		if textLine.font.StringWidthFB(textLine.fallbackFont, textLine.fontSize, buf.String()+string(ch)) < tf.w {
			buf.WriteRune(ch)
		} else {
			if buf.Len() > 0 {
				list = append(list, buf.String())
			}
			buf.Reset()
			buf.WriteRune(ch)
		}
	}
	if buf.Len() > 0 {
		list = append(list, buf.String())
	}
	return list
}
