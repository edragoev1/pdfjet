// textframe.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/single"
)

// TextFrame is paragraphs of text lines, wrapped at the width of the frame,
// with an optional border. A frame with a height draws as much of the text as
// fits and keeps the rest for the next frame, so text flows from frame to
// frame. A frame without a height draws all of the text. Drawing consumes the
// text: a second DrawOn draws what is left, so build a new frame to draw the
// same text again.
//
// Use a TextFrame for text that continues from one frame to the next: the
// columns of an article, or the pages of a long text. Use a TextColumn to
// draw paragraphs in one place, with justified text or a rotation, and a
// TextBlock for one run of text in one font. Please see Example_03 and
// Example_47.
type TextFrame struct {
	paragraphs      []*Paragraph
	x, y, w, h      float32
	paragraphGap    float32
	hasParagraphGap bool
	border          bool
	borderColor     [3]float32
	borderWidth     float32
	borderPattern   string

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
	xText, yText    float32
	rowOpen         bool
	rowPlaced       bool
	nextBaseline    float32
	startsParagraph bool // The next row starts a paragraph

	// The text of the row being drawn, drawn when the row is complete, so that
	// it can be aligned: each part is a text line with some of its text.
	row []*rowPart
}

type rowPart struct {
	textLine        *TextLine
	text            string
	x               float32
	paragraph       *Paragraph
	startsParagraph bool // The first text of the paragraph
	endsTextLine    bool // The last text of the text line
}

// NewTextFrame creates a text frame from strings, one paragraph each, in the
// font at its size. An empty line separates the paragraphs.
func NewTextFrame(f1 *Font, inputList []string) *TextFrame {
	paragraphs := make([]*Paragraph, 0, len(inputList))
	for _, text := range inputList {
		paragraphs = append(paragraphs, NewParagraph().Add(NewTextLine(f1, text)))
	}
	return NewTextFrameFromParagraphs(paragraphs)
}

// NewTextFrameFromParagraphs creates a text frame from paragraphs of text
// lines. An empty line separates the paragraphs unless SetParagraphGap sets
// another gap.
func NewTextFrameFromParagraphs(paragraphs []*Paragraph) *TextFrame {
	return &TextFrame{
		paragraphs:    paragraphs,
		borderColor:   [3]float32{0.0, 0.0, 0.0},
		borderWidth:   0.5,
		borderPattern: "[] 0",
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

// SetParagraphGap sets the space between paragraphs, in points, 0 or more:
// from the bottom of the text of a paragraph to the top of the text of the
// next, so paragraphs never overlap. The default is one empty line in the size
// of the next paragraph, so a heading is not followed by an empty line of its
// own size. A negative gap is taken as 0.
func (tf *TextFrame) SetParagraphGap(paragraphGap float32) *TextFrame {
	tf.paragraphGap = max(0, paragraphGap)
	tf.hasParagraphGap = true
	return tf
}

// SetBorders sets whether a border is drawn around this text frame.
func (tf *TextFrame) SetBorders(border bool) *TextFrame {
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

// SetBorderDashPattern sets the dash pattern of the border, for example "[3] 0".
func (tf *TextFrame) SetBorderDashPattern(borderPattern string) *TextFrame {
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
		rect.SetBorderDashPattern(tf.borderPattern)
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
	tf.startsParagraph = false
	tf.row = tf.row[:0]
	bottom := tf.y
	for tf.paragraphIndex < len(tf.paragraphs) {
		paragraph := tf.paragraphs[tf.paragraphIndex]
		for tf.lineIndex < len(paragraph.lines) {
			textLine := paragraph.lines[tf.lineIndex]
			if !tf.rowOpen && !tf.openRow(textLine) {
				tf.drawRow(page, false)
				return bottom
			}
			if tf.tokens == nil {
				if tf.lineIndex == 0 {
					paragraph.x1 = tf.x
					paragraph.y1 = tf.yText - textLine.font.GetAscent(textLine.fontSize)
					paragraph.xText = tf.xText
					paragraph.yText = tf.yText
				}
				tf.tokens = tf.tokenize(textLine)
				tf.tokenIndex = 0
			}
			if !tf.drawTokens(page, paragraph, textLine) {
				tf.drawRow(page, false)
				return bottom
			}
			paragraph.x2 = tf.xText
			paragraph.y2 = tf.yText + textLine.font.GetDescent(textLine.fontSize)
			bottom = paragraph.y2
			tf.tokens = nil
			tf.tokenIndex = 0
			tf.lineIndex++
		}
		tf.drawRow(page, true)
		tf.xText = tf.x
		tf.rowOpen = false
		if len(paragraph.lines) > 0 {
			// The next paragraph starts below the descent of this one, after the gap.
			lastLine := paragraph.lines[len(paragraph.lines)-1]
			tf.nextBaseline = tf.yText + lastLine.font.GetDescent(lastLine.fontSize)
			tf.startsParagraph = true
		}
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
	baseline := tf.y + textLine.font.GetAscent(textLine.fontSize)
	if tf.rowPlaced {
		baseline = tf.nextBaseline
		if tf.startsParagraph {
			// The gap, one empty line of this text by default, then its ascent.
			gap := textLine.GetHeight()
			if tf.hasParagraphGap {
				gap = tf.paragraphGap
			}
			baseline += gap + textLine.font.GetAscent(textLine.fontSize)
		}
	}
	if tf.h > 0 && tf.rowPlaced &&
		(baseline+textLine.font.GetDescent(textLine.fontSize)) > (tf.y+tf.h) {
		return false
	}
	tf.xText = tf.x
	tf.yText = baseline
	tf.rowOpen = true
	tf.rowPlaced = true
	tf.startsParagraph = false
	return true
}

// drawTokens draws the tokens of the text line that are left, wrapping them at
// the width of the frame. It returns false when a row does not fit in the
// height of the frame; the tokens that are left stay for the next frame.
func (tf *TextFrame) drawTokens(page *Page, paragraph *Paragraph, textLine *TextLine) bool {
	font := textLine.font
	fallbackFont := textLine.fallbackFont
	fontSize := textLine.fontSize
	var buf strings.Builder
	var runLength float32
	for tf.tokenIndex < len(tf.tokens) {
		if !tf.rowOpen && !tf.openRow(textLine) {
			return false
		}
		token := tf.tokens[tf.tokenIndex]
		// The token is measured without the space that follows it, as in
		// TextColumn: a row is as wide as the text it shows.
		if (runLength + textWidth(textLine, token)) <= ((tf.x + tf.w) - tf.xText) {
			buf.WriteString(token)
			buf.WriteString(single.Space)
			runLength += textWidth(textLine, token+single.Space)
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
		tf.addToRow(paragraph, textLine, buf.String(), false)
		tf.drawRow(page, false)
		buf.Reset()
		runLength = 0
		tf.xText = tf.x
		tf.rowOpen = false
		tf.nextBaseline = tf.yText + textLine.GetHeight()
	}
	tf.addToRow(paragraph, textLine, buf.String(), true)
	tf.xText += font.StringWidthUsingFallbackFont(fallbackFont, fontSize, buf.String())
	return true
}

// headThatFits returns the longest start of the token that fits in the width
// of the frame, and at least the first character of the token.
func (tf *TextFrame) headThatFits(textLine *TextLine, token string) string {
	_, size := utf8.DecodeRuneInString(token)
	end := size
	for end < len(token) {
		_, size = utf8.DecodeRuneInString(token[end:])
		if textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, token[:end+size]) > tf.w {
			break
		}
		end += size
	}
	return token[:end]
}

// addToRow adds the string to the row, at the current text position.
func (tf *TextFrame) addToRow(paragraph *Paragraph, textLine *TextLine, str string, endsTextLine bool) {
	first := len(tf.row) == 0 && tf.lineIndex == 0 && tf.xText == paragraph.xText &&
		tf.yText == paragraph.yText
	tf.row = append(tf.row, &rowPart{textLine, str, tf.xText, paragraph, first, endsTextLine})
}

// drawRow draws the parts of the row, with every setting of their text lines,
// including the vertical offset and the link, as TextColumn does. A paragraph
// aligned to the right or to the center moves the row, and a justified one
// widens the spaces of every row but its last.
func (tf *TextFrame) drawRow(page *Page, lastRowOfParagraph bool) {
	if len(tf.row) == 0 {
		return
	}
	paragraph := tf.row[0].paragraph
	textAlignment := alignment.Left
	if paragraph.explicitAlignment {
		textAlignment = paragraph.alignment
	}
	last := tf.row[len(tf.row)-1]
	rowWidth := last.x + textWidth(last.textLine, trimTrailingSpaces(last.text)) - tf.x

	if textAlignment == alignment.Justify && !lastRowOfParagraph {
		tf.drawJustifiedRow(page, rowWidth)
	} else {
		var shift float32
		if textAlignment == alignment.Right {
			shift = tf.w - rowWidth
		} else if textAlignment == alignment.Center {
			shift = (tf.w - rowWidth) / 2
		}
		for _, part := range tf.row {
			line := part.textLine.copyWithText(part.text)
			line.SetLocation(part.x+shift, tf.yText)
			line.DrawOn(page)
			if part.startsParagraph {
				part.paragraph.xText += shift
			}
			if part.endsTextLine {
				part.paragraph.x2 = part.x + textWidth(part.textLine, part.text) + shift
			}
		}
	}
	tf.row = tf.row[:0]
}

// drawJustifiedRow draws the words of the row one by one, with the width left
// in the row shared out among the spaces between them.
func (tf *TextFrame) drawJustifiedRow(page *Page, rowWidth float32) {
	spaces := 0
	for i, part := range tf.row {
		for j := 0; j < len(part.text); j++ {
			if part.text[j] == ' ' && tf.hasWordAfter(i, j+1) {
				spaces++
			}
		}
	}
	var dx float32
	if spaces > 0 {
		dx = (tf.w - rowWidth) / float32(spaces)
	}
	xWord := tf.x
	for i, part := range tf.row {
		text := part.text
		start := 0
		for start < len(text) {
			end := strings.IndexByte(text[start:], ' ')
			if end == -1 {
				end = len(text)
			} else {
				end += start
			}
			if end > start {
				word := text[start:end]
				line := part.textLine.copyWithText(word)
				line.SetLocation(xWord, tf.yText)
				line.DrawOn(page)
				xWord += textWidth(part.textLine, word)
			}
			if end < len(text) {
				xWord += textWidth(part.textLine, single.Space)
				if tf.hasWordAfter(i, end+1) {
					xWord += dx
				}
			}
			start = end + 1
		}
		if part.endsTextLine {
			part.paragraph.x2 = xWord
		}
	}
}

// hasWordAfter returns true when a word follows this position of the row.
func (tf *TextFrame) hasWordAfter(partIndex, byteIndex int) bool {
	for i := partIndex; i < len(tf.row); i++ {
		text := tf.row[i].text
		j := 0
		if i == partIndex {
			j = byteIndex
		}
		for ; j < len(text); j++ {
			if text[j] != ' ' {
				return true
			}
		}
	}
	return false
}

func trimTrailingSpaces(text string) string {
	return strings.TrimRight(text, " ")
}

func textWidth(textLine *TextLine, text string) float32 {
	return textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, text)
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
		if textLine.font.StringWidthUsingFallbackFont(textLine.fallbackFont, textLine.fontSize, buf.String()+string(ch)) <= tf.w {
			buf.WriteRune(ch)
		} else {
			full := []rune(buf.String())
			end := cjkLineEnd(full, ch)
			if end > 0 {
				list = append(list, string(full[:end]))
			}
			buf.Reset()
			buf.WriteString(string(full[end:]))
			buf.WriteRune(ch)
		}
	}
	if buf.Len() > 0 {
		list = append(list, buf.String())
	}
	return list
}
