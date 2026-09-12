// textblock.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/structtype"
)

// TextBlock is a block of text that wraps at its width, with an optional border, background and padding.
type TextBlock struct {
	x            float32
	y            float32
	width        float32
	height       float32
	font         *Font
	fallbackFont *Font
	fontSize     float32
	textContent  string
	lineSpacing  float32
	textPadding  float32

	fillColor          [3]float32
	hasFillColor       bool
	textColor          [3]float32
	hasTextColor       bool
	borderColor        [3]float32
	hasBorderColor     bool
	borderWidth        float32
	borderCornerRadius float32

	language               string
	uri                    string
	textAlignment          int
	underline              bool
	keywordHighlightColors map[string]int32
	rightToLeft            bool
}

// NewTextBlock creates a text block and sets the font and the text.
// The font is the fallback font too.
func NewTextBlock(font *Font, textContent string) *TextBlock {
	textBlock := new(TextBlock)
	textBlock.x = 0.0
	textBlock.y = 0.0
	textBlock.width = 500.0
	textBlock.height = 0.0
	textBlock.font = font
	textBlock.fallbackFont = font
	textBlock.fontSize = font.size

	textBlock.textContent = textContent
	textBlock.lineSpacing = 1.0
	textBlock.textColor = [3]float32{0.0, 0.0, 0.0}
	textBlock.textPadding = 0.0
	textBlock.textAlignment = alignment.Left

	textBlock.borderWidth = 0.5
	textBlock.borderCornerRadius = 0.0

	return textBlock
}

// SetFont sets the font of the text. It also becomes the fallback font.
func (textBlock *TextBlock) SetFont(font *Font) *TextBlock {
	textBlock.font = font
	textBlock.fallbackFont = font
	return textBlock
}

// SetFallbackFont sets the fallback font.
func (textBlock *TextBlock) SetFallbackFont(fallbackFont *Font) *TextBlock {
	textBlock.fallbackFont = fallbackFont
	return textBlock
}

// SetFontSize sets the font size of the text.
func (textBlock *TextBlock) SetFontSize(size float32) *TextBlock {
	textBlock.fontSize = size
	return textBlock
}

// SetFallbackFontSize sets the size of the fallback font.
func (textBlock *TextBlock) SetFallbackFontSize(size float32) *TextBlock {
	textBlock.fallbackFont.SetSize(size)
	return textBlock
}

// SetText sets the text of this text block.
func (textBlock *TextBlock) SetText(text string) *TextBlock {
	textBlock.textContent = text
	return textBlock
}

// GetFont returns the font of the text.
func (textBlock *TextBlock) GetFont() *Font {
	return textBlock.font
}

// GetText returns the text of this text block.
func (textBlock *TextBlock) GetText() string {
	return textBlock.textContent
}

// SetLocation sets the location of the top left corner of this text block on the page.
func (textBlock *TextBlock) SetLocation(x, y float32) Drawable {
	textBlock.x = x
	textBlock.y = y
	return textBlock
}

// SetSize sets the width and height of this text block.
func (textBlock *TextBlock) SetSize(w, h float32) *TextBlock {
	textBlock.width = w
	textBlock.height = h
	return textBlock
}

// SetWidth sets the width of this text block.
// The height is adjusted automatically to fit the text.
func (textBlock *TextBlock) SetWidth(w float32) *TextBlock {
	textBlock.width = w
	textBlock.height = 0.0
	return textBlock
}

// SetHeight sets the height of this text block.
func (textBlock *TextBlock) SetHeight(h float32) *TextBlock {
	textBlock.height = h
	return textBlock
}

// GetWidth returns the width of this text block.
func (textBlock *TextBlock) GetWidth() float32 {
	return textBlock.width
}

// GetHeight returns the height of this text block.
func (textBlock *TextBlock) GetHeight() float32 {
	return textBlock.height
}

// SetBorderCornerRadius sets the border corner radius.
func (textBlock *TextBlock) SetBorderCornerRadius(borderCornerRadius float32) *TextBlock {
	textBlock.borderCornerRadius = borderCornerRadius
	return textBlock
}

// SetTextPadding sets the padding between the text and the border.
func (textBlock *TextBlock) SetTextPadding(padding float32) *TextBlock {
	textBlock.textPadding = padding
	return textBlock
}

// SetBorderWidth sets the border width.
func (textBlock *TextBlock) SetBorderWidth(borderWidth float32) *TextBlock {
	textBlock.borderWidth = borderWidth
	return textBlock
}

// SetBorderColor sets the border color as a 0xRRGGBB value. color.Transparent removes the border.
func (textBlock *TextBlock) SetBorderColor(c int32) *TextBlock {
	if c == color.Transparent {
		textBlock.borderColor = [3]float32{}
		textBlock.hasBorderColor = false
		return textBlock
	}
	return textBlock.SetBorderColorRGB(colorToRGB(c))
}

// SetBorderColorRGB sets the border color from the red, green and blue components, from 0.0 to 1.0.
func (textBlock *TextBlock) SetBorderColorRGB(borderColor [3]float32) *TextBlock {
	textBlock.borderColor = borderColor
	textBlock.hasBorderColor = true
	return textBlock
}

// SetLineSpacing sets the line spacing as a multiple of the font's body height.
func (textBlock *TextBlock) SetLineSpacing(lineSpacing float32) *TextBlock {
	textBlock.lineSpacing = lineSpacing
	return textBlock
}

// SetTextColorRGB sets the text color from the red, green and blue components, from 0.0 to 1.0.
func (textBlock *TextBlock) SetTextColorRGB(textColor [3]float32) *TextBlock {
	textBlock.textColor = textColor
	return textBlock
}

// SetTextColor sets the text color as a 0xRRGGBB value.
func (textBlock *TextBlock) SetTextColor(c int32) *TextBlock {
	return textBlock.SetTextColorRGB(colorToRGB(c))
}

// SetFillColor sets the background color as a 0xRRGGBB value. color.Transparent removes the background.
func (textBlock *TextBlock) SetFillColor(c int32) *TextBlock {
	if c == color.Transparent {
		textBlock.fillColor = [3]float32{}
		textBlock.hasFillColor = false
		return textBlock
	}
	return textBlock.SetFillColorRGB(colorToRGB(c))
}

// SetFillColorRGB sets the background color from the red, green and blue components, from 0.0 to 1.0.
func (textBlock *TextBlock) SetFillColorRGB(fillColor [3]float32) *TextBlock {
	textBlock.fillColor = fillColor
	textBlock.hasFillColor = true
	return textBlock
}

// SetBackgroundColor sets the background color as a 0xRRGGBB value. color.Transparent removes the background.
func (textBlock *TextBlock) SetBackgroundColor(c int32) *TextBlock {
	return textBlock.SetFillColor(c)
}

// SetBackgroundColorRGB sets the background color from the red, green and blue components, from 0.0 to 1.0.
func (textBlock *TextBlock) SetBackgroundColorRGB(c [3]float32) *TextBlock {
	return textBlock.SetFillColorRGB(c)
}

// GetBackgroundColor returns the background color, or black if none was set.
func (textBlock *TextBlock) GetBackgroundColor() [3]float32 {
	return textBlock.fillColor
}

// SetTextAlignment sets the horizontal alignment of the text.
func (textBlock *TextBlock) SetTextAlignment(textAlignment int) *TextBlock {
	textBlock.textAlignment = textAlignment
	return textBlock
}

// SetLanguage sets the language of the text, for example "he", "ar" or "fa",
// as a BCP 47 language tag. The text is marked with it, for screen readers and
// text extraction.
func (textBlock *TextBlock) SetLanguage(language string) *TextBlock {
	textBlock.language = language
	return textBlock
}

// SetRightToLeft sets whether the text is right to left, like Arabic and
// Hebrew text. Each paragraph is wrapped at the width in logical order, and
// each line is then reordered with Bidi reordering, which also shapes the
// Arabic letters, and aligned to the right, unless the text alignment is
// alignment.Center.
func (textBlock *TextBlock) SetRightToLeft(rightToLeft bool) *TextBlock {
	textBlock.rightToLeft = rightToLeft
	return textBlock
}

// SetURIAction sets the URI opened when this text block is clicked.
func (textBlock *TextBlock) SetURIAction(uri string) *TextBlock {
	textBlock.uri = uri
	return textBlock
}

// SetKeywordHighlightColors sets the colors used to highlight keywords, matched ignoring case.
func (textBlock *TextBlock) SetKeywordHighlightColors(keywordHighlightColors map[string]int32) *TextBlock {
	textBlock.keywordHighlightColors = make(map[string]int32)
	for key, value := range keywordHighlightColors {
		textBlock.keywordHighlightColors[strings.ToLower(key)] = value
	}
	return textBlock
}

func (textBlock *TextBlock) getTextLines() []*TextLine {
	var textLines []*TextLine

	var textAreaWidth = textBlock.width - 2*textBlock.textPadding
	// Like String.split in Java: the trailing empty lines are dropped, but an
	// empty text is one empty line.
	lines := strings.Split(strings.ReplaceAll(textBlock.textContent, "\r\n", "\n"), "\n")
	for len(lines) > 1 && lines[len(lines)-1] == "" {
		lines = lines[:len(lines)-1]
	}
	for _, line := range lines {
		if textBlock.rightToLeft {
			textLines = textBlock.appendRightToLeftLines(textLines, line, textAreaWidth)
			continue
		}
		// A zero width space marks a place where the line may break in text
		// without spaces between its words, like Thai text. It is not drawn.
		text := strings.ReplaceAll(line, "\u200B", "")
		if textBlock.font.StringWidthFB(textBlock.fallbackFont, textBlock.fontSize, text) <= textAreaWidth {
			textLines = append(
				textLines,
				NewTextLine(textBlock.font, text))
		} else {
			if isCJK(text) {
				var sb strings.Builder
				for _, ch := range text {
					if textBlock.font.StringWidthFB(textBlock.fallbackFont,
						textBlock.fontSize, sb.String()+string(ch)) <= textAreaWidth {
						sb.WriteRune(ch)
					} else {
						if sb.Len() > 0 { // Don't emit an empty line
							textLines = append(
								textLines,
								NewTextLine(textBlock.font, sb.String()))
						}
						sb.Reset()
						sb.WriteRune(ch)
					}
				}
				if last := trimSpace(sb.String()); last != "" {
					textLines = append(textLines, NewTextLine(textBlock.font, last))
				}
			} else {
				var sb strings.Builder
				for _, token := range splitOnWhitespace(line) {
					// The words between the zero width spaces of a token are
					// joined with no space.
					words := strings.Split(token, "\u200B")
					for i, word := range words {
						separator := " "
						if i < len(words)-1 {
							separator = ""
						}
						if textBlock.font.StringWidthFB(textBlock.fallbackFont,
							textBlock.fontSize, sb.String()+word) <= textAreaWidth {
							sb.WriteString(word + separator)
						} else {
							if sb.Len() > 0 {
								textLines = append(
									textLines,
									NewTextLine(textBlock.font, trimSpace(sb.String())))
								sb.Reset()
							}
							// A word too wide for a line by itself is broken.
							var rest int
							textLines, rest = textBlock.appendBrokenWordLines(textLines, word, textAreaWidth)
							if rest < len(word) {
								sb.WriteString(word[rest:] + separator)
							}
						}
					}
				}
				if last := trimSpace(sb.String()); last != "" {
					textLines = append(textLines, NewTextLine(textBlock.font, last))
				}
			}
		}
	}

	return textLines
}

// appendBrokenWordLines appends the lines of a word too wide for a line by
// itself, broken between its characters, and returns the rest of the word,
// which fits on a line. No line starts with a combining mark, or with a Thai or
// Lao vowel or sign written after its consonant, and none ends with a Thai or
// Lao vowel written before its consonant.
func (textBlock *TextBlock) appendBrokenWordLines(
	textLines []*TextLine, word string, textAreaWidth float32) ([]*TextLine, int) {
	runes := []rune(word)
	start := 0 // The rune index where the rest of the word starts
	for textBlock.lineWidth(string(runes), byteIndex(runes, start)) > textAreaWidth {
		// Each line gets at least one character, however narrow the block.
		end := nextCharacterBreak(runes, start)
		next := end
		if end < len(runes) {
			next = nextCharacterBreak(runes, end)
		}
		for next < len(runes) &&
			textBlock.lineWidth(string(runes[:next]), byteIndex(runes, start)) <= textAreaWidth {
			end = next
			next = nextCharacterBreak(runes, end)
		}
		textLines = append(textLines, textBlock.newTextLine(string(runes[:end]), byteIndex(runes, start)))
		start = end
	}
	return textLines, byteIndex(runes, start)
}

// byteIndex returns the byte index of the rune at the rune index.
func byteIndex(runes []rune, runeIndex int) int {
	return len(string(runes[:runeIndex]))
}

// part returns the part of the text from the byte index on, reordered and
// shaped in the context of the whole text if the text is right to left.
func (textBlock *TextBlock) part(text string, from, to int) string {
	if textBlock.rightToLeft {
		return ReorderVisuallyPart(text, from, to)
	}
	return text[from:to]
}

// lineWidth returns the width of a line of text, measured after the line is
// reordered if the text is right to left.
func (textBlock *TextBlock) lineWidth(text string, from int) float32 {
	return textBlock.font.StringWidthFB(
		textBlock.fallbackFont, textBlock.fontSize, textBlock.part(text, from, len(text)))
}

// newTextLine returns a line of text, reordered if the text is right to left.
func (textBlock *TextBlock) newTextLine(text string, from int) *TextLine {
	to := len(strings.TrimRightFunc(text, isJavaWhitespace))
	if to < from {
		to = from
	}
	return NewTextLine(textBlock.font, textBlock.part(text, from, to))
}

func nextCharacterBreak(runes []rune, i int) int {
	ch := runes[i]
	i++
	for isLeadingVowel(ch) && i < len(runes) {
		ch = runes[i]
		i++
	}
	for i < len(runes) && staysWithPrevious(runes[i]) {
		i++
	}
	return i
}

// isLeadingVowel returns true for the Thai and Lao vowels written before the
// consonant they follow in speech.
func isLeadingVowel(ch rune) bool {
	return (ch >= 0x0E40 && ch <= 0x0E44) || (ch >= 0x0EC0 && ch <= 0x0EC4)
}

// staysWithPrevious returns true for the combining marks, and the Thai and Lao
// vowels and signs written after a consonant, like SARA AA and MAI YAMOK, which
// do not start a line.
func staysWithPrevious(ch rune) bool {
	if ch == 0x200C || ch == 0x200D { // ZWNJ, ZWJ
		return true
	}
	return unicode.In(ch, unicode.Mn, unicode.Mc, unicode.Me) ||
		(ch >= 0x0E2F && ch <= 0x0E3A) || (ch >= 0x0E45 && ch <= 0x0E4E) ||
		(ch >= 0x0EAF && ch <= 0x0EBC) || (ch >= 0x0EC6 && ch <= 0x0ECE)
}

// appendRightToLeftLines wraps a paragraph of right to left text at the
// spaces between words and at its zero width spaces, and appends its lines in
// visual order. The paragraph is wrapped in logical order, so its first words
// go on the first line, and each line is measured after it is reordered, since
// the shaped Arabic letters differ in width from the letters they replace. A
// word too wide for a line by itself is broken between its characters.
func (textBlock *TextBlock) appendRightToLeftLines(
	textLines []*TextLine, paragraph string, textAreaWidth float32) []*TextLine {
	// sb holds the words of the line in logical order. When the line starts
	// with the rest of a word broken over the lines, sb holds the whole word
	// and from is the byte index where the rest starts: the part before it is
	// not drawn, but it is the context that gives the first letter of the
	// rest its joined form.
	var sb strings.Builder
	from := 0
	for _, token := range splitOnWhitespace(paragraph) {
		// The words between the zero width spaces of a token are joined with no
		// space.
		words := strings.Split(token, "\u200B")
		for i, word := range words {
			separator := " "
			if i < len(words)-1 {
				separator = ""
			}
			if textBlock.lineWidth(sb.String()+word, from) <= textAreaWidth {
				sb.WriteString(word + separator)
			} else {
				if sb.Len() > from {
					textLines = append(textLines, textBlock.newTextLine(sb.String(), from))
					sb.Reset()
					from = 0
				}
				// A word too wide for a line by itself is broken.
				var rest int
				textLines, rest = textBlock.appendBrokenWordLines(textLines, word, textAreaWidth)
				if rest < len(word) {
					sb.WriteString(word + separator)
					from = rest
				}
			}
		}
	}
	return append(textLines, textBlock.newTextLine(sb.String(), from))
}

// SetUnderline underlines the text of this text block.
func (textBlock *TextBlock) SetUnderline(underline bool) *TextBlock {
	textBlock.underline = underline
	return textBlock
}

func (textBlock *TextBlock) underlineText(textLines []*TextLine) {
	for _, textLine := range textLines {
		textLine.underline = true
	}
}

// rightAlignText sets the offsets from the left edge of the text, inside the
// padding.
func (textBlock *TextBlock) rightAlignText(textLines []*TextLine) {
	textAreaWidth := textBlock.width - 2*textBlock.textPadding
	for _, textLine := range textLines {
		textLine.xOffset = textAreaWidth -
			textBlock.font.StringWidthFB(textBlock.fallbackFont, textBlock.fontSize, textLine.text)
	}
}

// centerText sets the offsets from the left edge of the text, inside the
// padding.
func (textBlock *TextBlock) centerText(textLines []*TextLine) {
	textAreaWidth := textBlock.width - 2*textBlock.textPadding
	for _, textLine := range textLines {
		textLine.xOffset = (textAreaWidth -
			textBlock.font.StringWidthFB(textBlock.fallbackFont, textBlock.fontSize, textLine.text)) / 2.0
	}
}

// DrawOn draws this text block on the specified page and returns the x and y
// coordinates of its bottom right corner.
func (textBlock *TextBlock) DrawOn(page *Page) [2]float32 {
	ascent := textBlock.font.GetAscentAt(textBlock.fontSize)
	descent := textBlock.font.GetDescentAt(textBlock.fontSize)
	leading := (ascent + descent) * textBlock.lineSpacing
	textLines := textBlock.getTextLines()
	blockHeight := max(textBlock.height, float32(len(textLines))*leading+2*textBlock.textPadding)
	if page == nil {
		return [2]float32{textBlock.x + textBlock.width, textBlock.y + blockHeight}
	}

	page.SaveGraphicsState()

	page.SetPenWidth(textBlock.borderWidth)
	switch {
	case textBlock.textAlignment == alignment.Center:
		textBlock.centerText(textLines)
	case textBlock.textAlignment == alignment.Right || textBlock.rightToLeft:
		textBlock.rightAlignText(textLines)
	}
	if textBlock.underline {
		textBlock.underlineText(textLines)
	}

	if textBlock.hasBorderColor || textBlock.hasFillColor {
		rect := NewRect(textBlock.x, textBlock.y, textBlock.width, blockHeight)
		if textBlock.hasBorderColor {
			rect.SetBorderColorRGB(textBlock.borderColor)
			rect.SetBorderWidth(textBlock.borderWidth)
			rect.SetCornerRadius(textBlock.borderCornerRadius)
		}
		if textBlock.hasFillColor {
			rect.SetFillColorRGB(textBlock.fillColor)
		}
		rect.DrawOn(page)
	}

	page.AddBMC(structtype.P, textBlock.language, textBlock.textContent, "")
	page.drawTextBlock(
		textBlock.font,
		textBlock.fontSize,
		textLines,
		textBlock.x+textBlock.textPadding,
		textBlock.y+textBlock.textPadding,
		leading,
		textBlock.textColor,
		textBlock.keywordHighlightColors,
		textBlock.language)
	page.AddEMC()

	page.RestoreGraphicsState()

	if textBlock.uri != "" {
		page.addAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             textBlock.x,
			y1:             textBlock.y,
			x2:             textBlock.x + textBlock.width,
			y2:             textBlock.y + blockHeight,
			vertices:       nil,
			fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
			transparency:   0.0,
			title:          "",
			contents:       "",
			uri:            textBlock.uri,
			key:            "",
			language:       "",
			actualText:     "",
			altDescription: "",
		})
	}

	return [2]float32{textBlock.x + textBlock.width, textBlock.y + blockHeight}
}
