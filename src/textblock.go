// textblock.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// TextBlock creates block of line-wrapped text.
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
	altDescription         string
	uri                    string
	key                    string
	uriLanguage            string
	uriActualText          string
	uriAltDescription      string
	textAlignment          int
	underline              bool
	strikeout              bool
	keywordHighlightColors map[string]int32
	rightToLeft            bool
}

// NewTextBlock creates a text block and sets the font.
//
//	@param font the font.
func NewTextBlock(font *Font, textContent string) *TextBlock {
	textBlock := new(TextBlock)
	textBlock.x = 0.0
	textBlock.y = 0.0
	textBlock.width = 500.0
	textBlock.height = 500.0
	textBlock.font = font
	textBlock.fontSize = font.size

	textBlock.textContent = textContent
	textBlock.lineSpacing = 1.0
	textBlock.textColor = [3]float32{0.0, 0.0, 0.0}
	textBlock.textPadding = 0.0
	textBlock.textAlignment = alignment.Left

	textBlock.borderWidth = 0.5
	textBlock.borderCornerRadius = 0.0

	textBlock.language = ""
	textBlock.altDescription = ""
	textBlock.underline = false
	textBlock.strikeout = false

	return textBlock
}

// SetFont sets the font for textBlock text block.
//
// @param font the font.
func (textBlock *TextBlock) SetFont(font *Font) *TextBlock {
	textBlock.font = font
	return textBlock
}

// SetFallbackFont sets the fallback font.
func (textBlock *TextBlock) SetFallbackFont(fallbackFont *Font) *TextBlock {
	textBlock.fallbackFont = fallbackFont
	return textBlock
}

// SetFontSize sets the font size for the text block.
//
// @param size the font size.
func (textBlock *TextBlock) SetFontSize(size float32) *TextBlock {
	textBlock.font.SetSize(size)
	return textBlock
}

// SetFallbackFontSize sets the font size for the text block.
//
// @param size the font size.
func (textBlock *TextBlock) SetFallbackFontSize(size float32) *TextBlock {
	textBlock.fallbackFont.SetSize(size)
	return textBlock
}

// SetText sets the text block text.
// @param text the text block text.
func (textBlock *TextBlock) SetText(text string) *TextBlock {
	textBlock.textContent = text
	return textBlock
}

// GetFont returns the font used by textBlock text block.
// @return the font.
func (textBlock *TextBlock) GetFont() *Font {
	return textBlock.font
}

// GetText returns the text block text.
// @return the text block text.
func (textBlock *TextBlock) GetText() string {
	return textBlock.textContent
}

// SetLocation sets the location where textBlock text block will be drawn on the page.
// @param x the x coordinate of the top left corner of the text block.
// @param y the y coordinate of the top left corner of the text block.
func (textBlock *TextBlock) SetLocation(x, y float32) Drawable {
	textBlock.x = x
	textBlock.y = y
	return textBlock
}

// SetSize sets the size of the textBlock.
// @param w the width of the text block.
// @param h the height of the text block.
func (textBlock *TextBlock) SetSize(w, h float32) *TextBlock {
	textBlock.width = w
	textBlock.height = h
	return textBlock
}

// SetWidth sets the width of the text block.
// The height is adjusted automatically to fit the text.
// @param w the width of the text block.
// @param h the height of the text block.
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
// @param borderRadius float the border corner radius.
func (textBlock *TextBlock) SetBorderCornerRadius(borderCornerRadius float32) *TextBlock {
	textBlock.borderCornerRadius = borderCornerRadius
	return textBlock
}

// SetTextPadding sets the padding around the block of text.
// @param padding the padding between the text and the border.
func (textBlock *TextBlock) SetTextPadding(padding float32) *TextBlock {
	textBlock.textPadding = padding
	return textBlock
}

// SetBorderWidth sets the border width.
// @param lineWidth float
func (textBlock *TextBlock) SetBorderWidth(borderWidth float32) *TextBlock {
	textBlock.borderWidth = borderWidth
	return textBlock
}

// SetBorderColor sets the border color as a 0xRRGGBB value.
func (textBlock *TextBlock) SetBorderColor(color int32) *TextBlock {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	textBlock.SetBorderColorRGB([3]float32{r, g, b})
	return textBlock
}

// SetBorderColorRGB sets the penColor color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetBorderColorRGB(borderColor [3]float32) *TextBlock {
	textBlock.borderColor = borderColor
	textBlock.hasBorderColor = true
	return textBlock
}

// SetLineSpacing sets the extra leading between lines of text.
// @param lineHeight
func (textBlock *TextBlock) SetLineSpacing(lineSpacing float32) *TextBlock {
	textBlock.lineSpacing = lineSpacing
	return textBlock
}

// SetTextColorRGB sets the text color.
func (textBlock *TextBlock) SetTextColorRGB(textColor [3]float32) *TextBlock {
	textBlock.textColor = textColor
	return textBlock
}

// SetTextColor sets the text color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetTextColor(color int32) *TextBlock {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	textBlock.textColor = [3]float32{r, g, b}
	return textBlock
}

// SetFillColor sets the text color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetFillColor(fillColor int32) *TextBlock {
	r := float32((fillColor>>16)&0xff) / 255.0
	g := float32((fillColor>>8)&0xff) / 255.0
	b := float32((fillColor)&0xff) / 255.0
	textBlock.SetFillColorRGB([3]float32{r, g, b})
	return textBlock
}

// SetFillColorRGB sets the fill color from red, green and blue values.
func (textBlock *TextBlock) SetFillColorRGB(fillColor [3]float32) *TextBlock {
	textBlock.fillColor = fillColor
	textBlock.hasFillColor = true
	return textBlock
}

// SetBackgroundColor sets the background color as a 0xRRGGBB value.
func (textBlock *TextBlock) SetBackgroundColor(color int32) *TextBlock {
	return textBlock.SetFillColor(color)
}

// SetBackgroundColorRGB sets the background color from red, green and blue values.
func (textBlock *TextBlock) SetBackgroundColorRGB(color [3]float32) *TextBlock {
	return textBlock.SetFillColorRGB(color)
}

// GetBackgroundColor returns the background color.
func (textBlock *TextBlock) GetBackgroundColor() [3]float32 {
	return textBlock.fillColor
}

// SetTextAlignment sets the brushColor color.
// @param color the color specified as 0xRRGGBB integer.
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
// each line is then reordered with ReorderVisually, which also shapes the
// Arabic letters, and aligned to the right, unless the text alignment is
// alignment.Center.
func (textBlock *TextBlock) SetRightToLeft(rightToLeft bool) *TextBlock {
	textBlock.rightToLeft = rightToLeft
	return textBlock
}

func (textBlock *TextBlock) textIsCJK(str string) bool {
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

func (textBlock *TextBlock) getTextLinesWithOffsets() []*TextLine {
	var textLines []*TextLine

	var textAreaWidth = textBlock.width - 2*textBlock.textPadding
	textBlock.textContent = strings.ReplaceAll(textBlock.textContent, "\r\n", "\n")
	textBlock.textContent = strings.TrimSpace(textBlock.textContent)
	lines := strings.Split(textBlock.textContent, "\n")
	for _, line := range lines {
		if textBlock.rightToLeft {
			textLines = textBlock.appendRightToLeftLines(textLines, line, textAreaWidth)
			continue
		}
		// A zero width space marks a place where the line may break in text
		// without spaces between its words, like Thai text. It is not drawn.
		text := strings.ReplaceAll(line, "\u200B", "")
		if textBlock.font.StringWidthFB(textBlock.fallbackFont, textBlock.font.size, text) <= textAreaWidth {
			textLines = append(
				textLines,
				NewTextLine(textBlock.font, text))
		} else {
			if textBlock.textIsCJK(text) {
				var sb strings.Builder
				for _, ch := range text {
					if textBlock.font.StringWidthFB(textBlock.fallbackFont,
						textBlock.font.size, sb.String()+string(ch)) <= textAreaWidth {
						sb.WriteRune(ch)
					} else {
						textLines = append(
							textLines,
							NewTextLine(textBlock.font, sb.String()))
						sb.Reset()
						sb.WriteRune(ch)
					}
				}
				if sb.Len() > 0 {
					textLines = append(textLines, NewTextLine(textBlock.font, sb.String()))
				}
			} else {
				var sb strings.Builder
				tokens := strings.Fields(line) // Split by whitespace
				for _, token := range tokens {
					// The words between the zero width spaces of a token are
					// joined with no space.
					words := strings.Split(token, "\u200B")
					for i, word := range words {
						separator := " "
						if i < len(words)-1 {
							separator = ""
						}
						if textBlock.font.StringWidthFB(textBlock.fallbackFont,
							textBlock.font.size, sb.String()+word) <= textAreaWidth {
							sb.WriteString(word + separator)
						} else {
							if sb.Len() > 0 {
								textLines = append(
									textLines,
									NewTextLine(textBlock.font, strings.TrimSpace(sb.String())))
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
				if sb.Len() > 0 {
					textLines = append(
						textLines,
						NewTextLine(textBlock.font, strings.TrimSpace(sb.String())))
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
		next := nextCharacterBreak(runes, end)
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
		textBlock.fallbackFont, textBlock.font.size, textBlock.part(text, from, len(text)))
}

// newTextLine returns a line of text, reordered if the text is right to left.
func (textBlock *TextBlock) newTextLine(text string, from int) *TextLine {
	to := len(strings.TrimRightFunc(text, unicode.IsSpace))
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
	for _, token := range strings.Fields(paragraph) {
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
// @param underline the underline flag.
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
			textBlock.font.StringWidth(textBlock.font.size, textLine.text)
	}
}

// centerText sets the offsets from the left edge of the text, inside the
// padding.
func (textBlock *TextBlock) centerText(textLines []*TextLine) {
	textAreaWidth := textBlock.width - 2*textBlock.textPadding
	for _, textLine := range textLines {
		textLine.xOffset = (textAreaWidth -
			textBlock.font.StringWidth(textBlock.font.size, textLine.text)) / 2.0
	}
}

// maxFloat32 returns the greater of a and b.
func maxFloat32(a, b float32) float32 {
	if a > b {
		return a
	}
	return b
}

// DrawOn draws text block on the specified page at specified location.
// @param page the Page where the TextBlock is to be drawn.
// @param draw flag specifying if text block component should actually be drawn on the page.
// @return x and y coordinates of the bottom right corner of text block component.
func (textBlock *TextBlock) DrawOn(page *Page) [2]float32 {
	ascent := textBlock.font.ascent
	descent := textBlock.font.descent
	leading := (ascent + descent) * textBlock.lineSpacing
	textLines := textBlock.getTextLinesWithOffsets()
	if page == nil {
		return [2]float32{
			textBlock.x + textBlock.width,
			maxFloat32(
				textBlock.y+textBlock.height,
				textBlock.y+(float32(len(textLines))*leading)+2*textBlock.textPadding),
		}
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
		rect := NewRect(
			textBlock.x,
			textBlock.y,
			textBlock.width,
			maxFloat32(textBlock.height, float32(len(textLines))*leading+2*textBlock.textPadding))
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

	page.AddBMC("P", textBlock.language, textBlock.textContent, "")
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

	return [2]float32{
		textBlock.x + textBlock.width,
		maxFloat32(
			textBlock.y+textBlock.height,
			textBlock.y+(float32(len(textLines))*leading)+2*textBlock.textPadding),
	}
}
