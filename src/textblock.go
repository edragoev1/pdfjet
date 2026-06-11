package pdfjet

/**
 * textblock.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"strings"

	"github.com/edragoev1/pdfjet/src/alignment"
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
	textBlock.keywordHighlightColors = make(map[string]int32)

	textBlock.borderWidth = 0.5
	textBlock.borderCornerRadius = 0.0

	textBlock.language = "en-US"
	textBlock.altDescription = ""
	textBlock.underline = false
	textBlock.strikeout = false

	return textBlock
}

// SetFont sets the font for textBlock text block.
//
// @param font the font.
func (textBlock *TextBlock) SetFont(font *Font) {
	textBlock.font = font
}

// SetFallbackFont sets the fallback font.
func (textBlock *TextBlock) SetFallbackFont(font *Font) {
	textBlock.fallbackFont = font
}

// SetFontSize sets the font size for the text block.
//
// @param size the font size.
func (textBlock *TextBlock) SetFontSize(size float32) {
	textBlock.font.SetSize(size)
}

// SetFallbackFontSize sets the font size for the text block.
//
// @param size the font size.
func (textBlock *TextBlock) SetFallbackFontSize(size float32) {
	textBlock.fallbackFont.SetSize(size)
}

// SetText sets the text block text.
// @param text the text block text.
func (textBlock *TextBlock) SetText(text string) {
	textBlock.textContent = text
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
func (textBlock *TextBlock) SetLocation(x, y float32) {
	textBlock.x = x
	textBlock.y = y
}

func (textBlock *TextBlock) SetPosition(x, y float32) {
	textBlock.x = x
	textBlock.y = y
}

// SetSize sets the size of the textBlock.
// @param w the width of the text block.
// @param h the height of the text block.
func (textBlock *TextBlock) SetSize(w, h float32) {
	textBlock.width = w
	textBlock.height = h
}

// SetWidth sets the width of the text block.
// The height is adjusted automatically to fit the text.
// @param w the width of the text block.
// @param h the height of the text block.
func (textBlock *TextBlock) SetWidth(w float32) {
	textBlock.width = w
	textBlock.height = 0.0
}

// SetBorderCornerRadius sets the border corner radius.
// @param borderRadius float the border corner radius.
func (textBlock *TextBlock) SetBorderCornerRadius(borderCornerRadius float32) {
	textBlock.borderCornerRadius = borderCornerRadius
}

// SetTextPadding sets the padding around the block of text.
// @param padding the padding between the text and the border.
func (textBlock *TextBlock) SetTextPadding(padding float32) {
	textBlock.textPadding = padding
}

// SetBorderWidth sets the border width.
// @param lineWidth float
func (textBlock *TextBlock) SetBorderWidth(borderWidth float32) {
	textBlock.borderWidth = borderWidth
}

func (textBlock *TextBlock) SetBorderColor(color int32) {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	textBlock.SetBorderColorRGB([3]float32{r, g, b})
}

// SetBorderColorRGB sets the penColor color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetBorderColorRGB(borderColor [3]float32) {
	textBlock.borderColor = borderColor
	textBlock.hasBorderColor = true
}

// SetLineSpacing sets the extra leading between lines of text.
// @param lineHeight
func (textBlock *TextBlock) SetLineSpacing(lineSpacing float32) {
	textBlock.lineSpacing = lineSpacing
}

// SetTextColorRGB sets the text color.
func (textBlock *TextBlock) SetTextColorRGB(textColor [3]float32) {
	textBlock.textColor = textColor
}

// SetTextColor sets the text color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetTextColor(color int32) {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	textBlock.textColor = [3]float32{r, g, b}
}

// SetFillColor sets the text color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetFillColor(fillColor int32) {
	r := float32((fillColor>>16)&0xff) / 255.0
	g := float32((fillColor>>8)&0xff) / 255.0
	b := float32((fillColor)&0xff) / 255.0
	textBlock.SetFillColorRGB([3]float32{r, g, b})
}

func (textBlock *TextBlock) SetFillColorRGB(fillColor [3]float32) {
	textBlock.fillColor = fillColor
	textBlock.hasFillColor = true
}

// SetHighlightColors sets the text colors map.
func (textBlock *TextBlock) SetHighlightColors(keywordHighlightColors map[string]int32) {
	textBlock.keywordHighlightColors = keywordHighlightColors
}

// SetTextAlignment sets the brushColor color.
// @param color the color specified as 0xRRGGBB integer.
func (textBlock *TextBlock) SetTextAlignment(textAlignment int) {
	textBlock.textAlignment = textAlignment
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

func (textBlock *TextBlock) SetURIAction(uri string) *TextBlock {
	textBlock.uri = uri
	return textBlock
}

func (textBlock *TextBlock) SetKeywordHighlightColors(keywordHighlightColors map[string]int32) {
	for key, value := range keywordHighlightColors {
		textBlock.keywordHighlightColors[strings.ToLower(key)] = value
	}
}

func (textBlock *TextBlock) getTextLinesWithOffsets() []*TextLine {
	var textLines []*TextLine

	var textAreaWidth = textBlock.width - 2*textBlock.textPadding
	textBlock.textContent = strings.ReplaceAll(textBlock.textContent, "\r\n", "\n")
	textBlock.textContent = strings.TrimSpace(textBlock.textContent)
	lines := strings.Split(textBlock.textContent, "\n")
	for _, line := range lines {
		if textBlock.font.StringWidthFB(textBlock.fallbackFont, textBlock.font.size, line) <= textAreaWidth {
			textLines = append(
				textLines,
				NewTextLine(textBlock.font, line))
		} else {
			if textBlock.textIsCJK(line) {
				var sb strings.Builder
				for _, ch := range line {
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
					if textBlock.font.StringWidthFB(textBlock.fallbackFont,
						textBlock.font.size, sb.String()+token) <= textAreaWidth {
						sb.WriteString(token + " ")
					} else {
						textLines = append(
							textLines,
							NewTextLine(textBlock.font, strings.TrimSpace(sb.String())))
						sb.Reset()
						sb.WriteString(token + " ")
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

func (textBlock *TextBlock) rightAlignText(textLines []*TextLine) {
	for _, textLine := range textLines {
		textLine.xOffset = textBlock.width -
			textBlock.font.StringWidth(textBlock.font.size, textLine.text)
	}
}

func (textBlock *TextBlock) centerText(textLines []*TextLine) {
	for _, textLine := range textLines {
		textLine.xOffset = (textBlock.width -
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
	if page == nil {
		// TODO
		return [2]float32{0.0, 0.0}
	}

	page.appendString("q\n")
	page.SetPenWidth(textBlock.borderWidth)
	ascent := textBlock.font.ascent
	descent := textBlock.font.descent
	leading := (ascent + descent) * textBlock.lineSpacing

	textLines := textBlock.getTextLinesWithOffsets()
	switch textBlock.textAlignment {
	case alignment.Right:
		textBlock.rightAlignText(textLines)
	case alignment.Center:
		textBlock.centerText(textLines)
	default:
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

	page.AddBMC("P", textBlock.uriLanguage, textBlock.textContent, "")
	page.drawTextBlock(
		textBlock.font,
		textBlock.fontSize,
		textLines,
		textBlock.x+textBlock.textPadding,
		textBlock.y+textBlock.textPadding,
		leading,
		textBlock.textColor,
		textBlock.keywordHighlightColors)
	page.AddEMC()
	page.appendString("Q\n")

	// You can uncomment and adapt if required.
	/*
		if t.TextDirection == LEFT_TO_RIGHT &&
			(t.Uri != "" || t.Key != "") {
			page.AddAnnotation(Annotation{
				Uri:                 t.Uri,
				Key:                 t.Key,
				X:                   t.X,
				Y:                   t.Y,
				Width:               t.X + t.Width,
				Height:              t.Y + t.Height,
				UriLanguage:         t.UriLanguage,
				UriActualText:       t.UriActualText,
				UriAltDescription:   t.UriAltDescription,
			})
		}
	*/

	return [2]float32{
		textBlock.x + textBlock.width,
		maxFloat32(
			textBlock.y+textBlock.height,
			textBlock.y+(float32(len(textLines))*leading)+(2*textBlock.textPadding)),
	}
}
