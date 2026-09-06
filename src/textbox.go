package pdfjet

/**
 * textbox.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"strings"

	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/border"
	"github.com/edragoev1/pdfjet/src/direction"
	"github.com/edragoev1/pdfjet/src/single"
)

// TextBox is a box containing line-wrapped text.
// Please see Example_19 and Example_30.
type TextBox struct {
	font          *Font
	fallbackFont  *Font
	fontSize      float32
	text          string
	x             float32
	y             float32
	width         float32
	height        float32
	spacing       float32
	margin        float32
	lineWidth     float32
	fillColor     *[3]float32 // The background fill color
	textColor     [3]float32
	strokeWidth   float32
	strokeColor   *[3]float32
	valign        int
	textAlignment int
	colors        map[string]int32
	// Border:
	// bit 16 - top
	// bit 17 - bottom
	// bit 18 - left
	// bit 19 - right
	// Text Decoration:
	// bit 22 - underline
	// bit 23 - strikeout
	properties        uint32
	language          string
	altDescription    string
	uri               string
	key               string
	uriLanguage       string
	uriActualText     string
	uriAltDescription string
	textDirection     direction.Direction
}

// NewTextBox creates a text box and sets the font.
func NewTextBox(font *Font) *TextBox {
	textBox := new(TextBox)
	textBox.font = font
	textBox.fontSize = font.GetSize()
	textBox.width = 300.0
	textBox.strokeWidth = 0.5
	textBox.textColor = [3]float32{0.0, 0.0, 0.0}
	textBox.valign = alignment.Top
	textBox.textAlignment = alignment.Left
	textBox.properties = 0x00000001
	textBox.language = "en-US"
	textBox.altDescription = ""
	textBox.textDirection = direction.LeftToRight
	return textBox
}

// NewTextBoxWithText creates a text box and sets the font and the text.
func NewTextBoxWithText(font *Font, text string) *TextBox {
	textBox := NewTextBox(font)
	textBox.text = text
	return textBox
}

// SetFont sets the font of this text box.
func (textBox *TextBox) SetFont(font *Font) {
	textBox.font = font
	textBox.fontSize = font.GetSize()
}

// GetFont returns the font of this text box.
func (textBox *TextBox) GetFont() *Font {
	return textBox.font
}

// SetFontSize sets the font size of this text box.
func (textBox *TextBox) SetFontSize(fontSize float32) {
	textBox.fontSize = fontSize
}

// SetFallbackFont sets the fallback font.
func (textBox *TextBox) SetFallbackFont(fallbackFont *Font) {
	textBox.fallbackFont = fallbackFont
}

// GetFallbackFont returns the fallback font.
func (textBox *TextBox) GetFallbackFont() *Font {
	return textBox.fallbackFont
}

// SetText sets the text of this text box.
func (textBox *TextBox) SetText(text string) {
	textBox.text = text
}

// GetText returns the text of this text box.
func (textBox *TextBox) GetText() string {
	return textBox.text
}

// SetPosition sets the location of this text box.
func (textBox *TextBox) SetPosition(x, y float32) {
	textBox.SetLocation(x, y)
}

// SetLocation sets the location of this text box.
func (textBox *TextBox) SetLocation(x, y float32) *TextBox {
	textBox.x = x
	textBox.y = y
	return textBox
}

// GetLocation returns the location of this text box.
func (textBox *TextBox) GetLocation() [2]float32 {
	return [2]float32{textBox.x, textBox.y}
}

// SetSize sets the size of this text box.
func (textBox *TextBox) SetSize(w, h float32) {
	textBox.width = w
	textBox.height = h
}

// SetWidth sets the width of this text box.
func (textBox *TextBox) SetWidth(width float32) {
	textBox.width = width
}

// GetWidth returns the width of this text box.
func (textBox *TextBox) GetWidth() float32 {
	return textBox.width
}

// SetHeight sets the height of this text box.
func (textBox *TextBox) SetHeight(height float32) *TextBox {
	textBox.height = height
	return textBox
}

// GetHeight returns the height of this text box.
func (textBox *TextBox) GetHeight() float32 {
	return textBox.height
}

// SetMargin sets the margin of this text box.
func (textBox *TextBox) SetMargin(margin float32) *TextBox {
	textBox.margin = margin
	return textBox
}

// GetMargin returns the margin of this text box.
func (textBox *TextBox) GetMargin() float32 {
	return textBox.margin
}

// SetLineWidth sets the border line width.
func (textBox *TextBox) SetLineWidth(lineWidth float32) {
	textBox.lineWidth = lineWidth
}

// GetLineWidth returns the border line width.
func (textBox *TextBox) GetLineWidth() float32 {
	return textBox.lineWidth
}

// SetSpacing sets the spacing between the lines of text.
func (textBox *TextBox) SetSpacing(spacing float32) {
	textBox.spacing = spacing
}

// GetSpacing returns the spacing between the lines of text.
func (textBox *TextBox) GetSpacing() float32 {
	return textBox.spacing
}

func colorToRGB(color int32) [3]float32 {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	return [3]float32{r, g, b}
}

// SetBgColor sets the background color of this text box.
func (textBox *TextBox) SetBgColor(color int32) {
	rgb := colorToRGB(color)
	textBox.fillColor = &rgb
}

// SetFillColor sets the background color of this text box.
func (textBox *TextBox) SetFillColor(color int32) {
	textBox.SetBgColor(color)
}

// SetTextColor sets the text color of this text box.
func (textBox *TextBox) SetTextColor(color int32) {
	textBox.textColor = colorToRGB(color)
}

// GetTextColor returns the text color of this text box.
func (textBox *TextBox) GetTextColor() [3]float32 {
	return textBox.textColor
}

// SetStrokeWidth sets the width of the border lines.
func (textBox *TextBox) SetStrokeWidth(strokeWidth float32) {
	textBox.strokeWidth = strokeWidth
}

// SetStrokeColor sets the color of the border lines.
func (textBox *TextBox) SetStrokeColor(color int32) {
	rgb := colorToRGB(color)
	textBox.strokeColor = &rgb
}

// SetBorder sets the border with the specified bit mask.
func (textBox *TextBox) SetBorder(b uint32) {
	textBox.properties |= b
}

// GetBorder returns true if the specified border is set.
func (textBox *TextBox) GetBorder(b uint32) bool {
	switch b {
	case border.None:
		return (textBox.properties & 0x000F0000) == 0x00000000
	case border.Top:
		return (textBox.properties & 0x00010000) != 0x00000000
	case border.Bottom:
		return (textBox.properties & 0x00020000) != 0x00000000
	case border.Left:
		return (textBox.properties & 0x00040000) != 0x00000000
	case border.Right:
		return (textBox.properties & 0x00080000) != 0x00000000
	case border.All:
		return (textBox.properties & 0x000F0000) == 0x000F0000
	}
	return false
}

// SetBorders sets all the borders on or off.
func (textBox *TextBox) SetBorders(borders bool) {
	if borders {
		textBox.SetBorder(border.All)
	} else {
		textBox.properties &= 0x00F0FFFF
	}
}

// SetTextAlignment sets the text alignment.
func (textBox *TextBox) SetTextAlignment(textAlignment int) *TextBox {
	textBox.textAlignment = textAlignment
	return textBox
}

// GetTextAlignment returns the text alignment.
func (textBox *TextBox) GetTextAlignment() int {
	return textBox.textAlignment
}

// SetVerticalAlignment sets the vertical alignment of the text.
func (textBox *TextBox) SetVerticalAlignment(valign int) {
	textBox.valign = valign
}

// GetVerticalAlignment returns the vertical alignment of the text.
func (textBox *TextBox) GetVerticalAlignment() int {
	return textBox.valign
}

// SetUnderline underlines the text.
func (textBox *TextBox) SetUnderline(underline bool) {
	if underline {
		textBox.properties |= 0x00400000
	} else {
		textBox.properties &= 0x00BFFFFF
	}
}

// GetUnderline returns true if the text is underlined.
func (textBox *TextBox) GetUnderline() bool {
	return (textBox.properties & 0x00400000) != 0x00000000
}

// SetStrikeout strikes out the text.
func (textBox *TextBox) SetStrikeout(strikeout bool) {
	if strikeout {
		textBox.properties |= 0x00800000
	} else {
		textBox.properties &= 0x007FFFFF
	}
}

// GetStrikeout returns true if the text is stricken out.
func (textBox *TextBox) GetStrikeout() bool {
	return (textBox.properties & 0x00800000) != 0x00000000
}

// SetTextColors sets the highlight colors of the keywords.
func (textBox *TextBox) SetTextColors(colors map[string]int32) {
	textBox.colors = colors
}

// GetTextColors returns the highlight colors of the keywords.
func (textBox *TextBox) GetTextColors() map[string]int32 {
	return textBox.colors
}

// SetLanguage sets the language of the text.
func (textBox *TextBox) SetLanguage(language string) *TextBox {
	textBox.language = language
	return textBox
}

// GetLanguage returns the language of the text.
func (textBox *TextBox) GetLanguage() string {
	return textBox.language
}

// SetAltDescription sets the alternate description of the text.
func (textBox *TextBox) SetAltDescription(altDescription string) *TextBox {
	textBox.altDescription = altDescription
	return textBox
}

// GetAltDescription returns the alternate description of the text.
func (textBox *TextBox) GetAltDescription() string {
	return textBox.altDescription
}

// SetURIAction sets the URI for the "click text box" action.
func (textBox *TextBox) SetURIAction(uri string) *TextBox {
	textBox.uri = uri
	return textBox
}

// SetTextDirection sets the text direction.
func (textBox *TextBox) SetTextDirection(textDirection direction.Direction) *TextBox {
	textBox.textDirection = textDirection
	return textBox
}

func (textBox *TextBox) drawBorders(page *Page) {
	page.AddArtifactBMC()
	if textBox.strokeColor != nil {
		page.SetPenColorRGB(*textBox.strokeColor)
	}
	page.SetPenWidth(textBox.strokeWidth)
	if textBox.GetBorder(border.All) {
		page.DrawRect(textBox.x, textBox.y, textBox.width, textBox.height)
	} else {
		if textBox.GetBorder(border.Top) {
			page.MoveTo(textBox.x, textBox.y)
			page.LineTo(textBox.x+textBox.width, textBox.y)
			page.StrokePath()
		}
		if textBox.GetBorder(border.Bottom) {
			page.MoveTo(textBox.x, textBox.y+textBox.height)
			page.LineTo(textBox.x+textBox.width, textBox.y+textBox.height)
			page.StrokePath()
		}
		if textBox.GetBorder(border.Left) {
			page.MoveTo(textBox.x, textBox.y)
			page.LineTo(textBox.x, textBox.y+textBox.height)
			page.StrokePath()
		}
		if textBox.GetBorder(border.Right) {
			page.MoveTo(textBox.x+textBox.width, textBox.y)
			page.LineTo(textBox.x+textBox.width, textBox.y+textBox.height)
			page.StrokePath()
		}
	}
	page.AddEMC()
}

func (textBox *TextBox) textIsCJK(str string) bool {
	// CJK Unified Ideographs Range: 4E00-9FD5
	// Hiragana Range: 3040-309F
	// Katakana Range: 30A0-30FF
	// Hangul Jamo Range: 1100-11FF
	numOfCJK := 0
	count := 0
	for _, ch := range str {
		count++
		if (ch >= 0x4E00 && ch <= 0x9FD5) ||
			(ch >= 0x3040 && ch <= 0x309F) ||
			(ch >= 0x30A0 && ch <= 0x30FF) ||
			(ch >= 0x1100 && ch <= 0x11FF) {
			numOfCJK++
		}
	}
	return numOfCJK > (count / 2)
}

func (textBox *TextBox) getTextLines() []string {
	list := make([]string, 0)

	var textAreaWidth float32
	if textBox.textDirection == direction.LeftToRight {
		textAreaWidth = textBox.width - 2*textBox.margin
	} else {
		textAreaWidth = textBox.height - 2*textBox.margin
	}
	// Only the core fonts apply kerning between adjacent characters, so for
	// every other font the width of a line is the sum of the widths of its
	// parts. That lets the loops below track the wrapped line's width
	// incrementally instead of re-measuring the whole accumulated line on
	// every token, which made wrapping a long paragraph an O(n^2) operation.
	additive := !textBox.font.isCoreFont
	font := textBox.font
	fallbackFont := textBox.fallbackFont
	fontSize := textBox.fontSize
	lines := strings.Split(strings.ReplaceAll(textBox.text, "\r\n", "\n"), "\n")
	for _, line := range lines {
		if font.StringWidthFB(fallbackFont, fontSize, line) <= textAreaWidth {
			list = append(list, line)
			continue
		}
		if textBox.textIsCJK(line) {
			var buf strings.Builder
			var bufWidth float32
			for _, ch := range line {
				var chWidth float32
				if additive {
					chWidth = font.StringWidthFB(fallbackFont, fontSize, string(ch))
				}
				var lineWidth float32
				if additive {
					lineWidth = bufWidth + chWidth
				} else {
					lineWidth = font.StringWidthFB(fallbackFont, fontSize, buf.String()+string(ch))
				}
				if lineWidth <= textAreaWidth {
					buf.WriteRune(ch)
					bufWidth = lineWidth
				} else {
					if buf.Len() > 0 { // Don't emit an empty line
						list = append(list, buf.String())
					}
					buf.Reset()
					buf.WriteRune(ch)
					bufWidth = chWidth
				}
			}
			if buf.Len() > 0 {
				list = append(list, buf.String())
			}
		} else {
			var buf strings.Builder
			var bufWidth float32
			var spaceWidth float32
			if additive {
				spaceWidth = font.StringWidthFB(fallbackFont, fontSize, single.Space)
			}
			for _, token := range strings.Fields(line) {
				var tokenWidth float32
				if additive {
					tokenWidth = font.StringWidthFB(fallbackFont, fontSize, token)
				}
				var lineWidth float32
				if additive {
					if buf.Len() == 0 {
						lineWidth = tokenWidth
					} else {
						lineWidth = bufWidth + spaceWidth + tokenWidth
					}
				} else {
					lineWidth = font.StringWidthFB(fallbackFont, fontSize, buf.String()+token)
				}
				if lineWidth <= textAreaWidth {
					buf.WriteString(token)
					buf.WriteString(single.Space)
					bufWidth = lineWidth
				} else {
					if buf.Len() > 0 { // Don't emit an empty line
						list = append(list, strings.TrimSpace(buf.String()))
					}
					buf.Reset()
					buf.WriteString(token)
					buf.WriteString(single.Space)
					bufWidth = tokenWidth
				}
			}
			last := strings.TrimSpace(buf.String())
			if last != "" {
				list = append(list, last)
			}
		}
	}

	return list
}

// DrawOn draws this text box on the specified page.
// Returns the x and y coordinates of the bottom right corner of this component.
func (textBox *TextBox) DrawOn(page *Page) [2]float32 {
	lines := textBox.getTextLines()
	font := textBox.font
	fallbackFont := textBox.fallbackFont
	fontSize := textBox.fontSize
	leading := font.GetAscent(fontSize) + font.GetDescent(fontSize) + textBox.spacing
	if textBox.height > 0.0 { // TextBox with fixed height
		if (float32(len(lines))*leading - textBox.spacing) > (textBox.height - 2*textBox.margin) {
			list := make([]string, 0)
			for i, line := range lines {
				if (float32(i+1)*leading - textBox.spacing) > (textBox.height - 2*textBox.margin) {
					break
				}
				list = append(list, line)
			}
			if len(list) > 0 { // At least one line must fit in the text box
				lastLine := list[len(list)-1]
				if len(lastLine) > 3 {
					lastLine = lastLine[:len(lastLine)-3]
				}
				list[len(list)-1] = lastLine + "..."
				lines = list
			}
		}
		if page != nil {
			if textBox.fillColor != nil {
				page.SetBrushColorRGB(*textBox.fillColor)
				page.AddArtifactBMC()
				page.FillRect(textBox.x, textBox.y, textBox.width, textBox.height)
				page.AddEMC()
			}
			if textBox.strokeColor != nil {
				page.SetPenColorRGB(*textBox.strokeColor)
			}
			if textBox.fillColor != nil {
				page.SetBrushColorRGB(*textBox.fillColor)
			}
			page.SetPenWidth(font.GetUnderlineThickness(fontSize))
		}
		xText := textBox.x + textBox.margin
		yText := textBox.y + textBox.margin + font.GetAscent(fontSize)
		if textBox.textDirection == direction.LeftToRight {
			if textBox.valign == alignment.Top {
				yText = textBox.y + textBox.margin + font.GetAscent(fontSize)
			} else if textBox.valign == alignment.Bottom {
				yText = (textBox.y + textBox.height) -
					(float32(len(lines))*leading + textBox.margin)
				yText += font.GetAscent(fontSize)
			} else if textBox.valign == alignment.Center {
				yText = textBox.y + (textBox.height-float32(len(lines))*leading)/2
				yText += font.GetAscent(fontSize)
			}
		} else {
			yText = textBox.x + textBox.margin + font.GetAscent(fontSize)
		}
		for _, line := range lines {
			if textBox.textDirection == direction.LeftToRight {
				if textBox.GetTextAlignment() == alignment.Left {
					xText = textBox.x + textBox.margin
				} else if textBox.GetTextAlignment() == alignment.Right {
					xText = (textBox.x + textBox.width) -
						(font.StringWidthFB(fallbackFont, fontSize, line) + textBox.margin)
				} else if textBox.GetTextAlignment() == alignment.Center {
					xText = textBox.x +
						(textBox.width-font.StringWidthFB(fallbackFont, fontSize, line))/2
				}
			} else {
				xText = textBox.y + textBox.margin
			}
			if page != nil {
				textBox.drawTextLine(page, line, xText, yText)
			}
			if textBox.textDirection == direction.LeftToRight ||
				textBox.textDirection == direction.BottomToTop {
				yText += leading
			} else {
				yText -= leading
			}
		}
	} else { // TextBox that expands to fit the content
		if page != nil {
			if textBox.fillColor != nil {
				page.SetBrushColorRGB(*textBox.fillColor)
				page.AddArtifactBMC()
				page.FillRect(textBox.x, textBox.y, textBox.width,
					(float32(len(lines))*leading-textBox.spacing)+2*textBox.margin)
				page.AddEMC()
			}
			page.SetBrushColorRGB(textBox.textColor)
			if textBox.strokeColor != nil {
				page.SetPenColorRGB(*textBox.strokeColor)
			}
			page.SetPenWidth(font.GetUnderlineThickness(fontSize))
		}
		xText := textBox.x + textBox.margin
		yText := textBox.y + textBox.margin + font.GetAscent(fontSize)
		for _, line := range lines {
			if textBox.textDirection == direction.LeftToRight {
				if textBox.GetTextAlignment() == alignment.Left {
					xText = textBox.x + textBox.margin
				} else if textBox.GetTextAlignment() == alignment.Right {
					xText = (textBox.x + textBox.width) -
						(font.StringWidthFB(fallbackFont, fontSize, line) + textBox.margin)
				} else if textBox.GetTextAlignment() == alignment.Center {
					xText = textBox.x +
						(textBox.width-font.StringWidthFB(fallbackFont, fontSize, line))/2
				}
			} else {
				xText = textBox.x + textBox.margin
			}
			if page != nil {
				textBox.drawTextLine(page, line, xText, yText)
			}
			if textBox.textDirection == direction.LeftToRight ||
				textBox.textDirection == direction.BottomToTop {
				yText += leading
			} else {
				yText -= leading
			}
		}
		textBox.height = ((yText - textBox.y) -
			(font.GetAscent(fontSize) + textBox.spacing)) + textBox.margin
	}
	if page != nil {
		textBox.drawBorders(page)
		if textBox.textDirection == direction.LeftToRight &&
			(textBox.uri != "" || textBox.key != "") {
			page.AddAnnotation(&Annotation{
				annotationType: AnnotationLink,
				x1:             textBox.x,
				y1:             textBox.y,
				x2:             textBox.x + textBox.width,
				y2:             textBox.y + textBox.height,
				vertices:       nil,
				fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
				transparency:   0.0,
				title:          "",
				contents:       "",
				uri:            textBox.uri,
				key:            textBox.key, // The destination name
				language:       textBox.uriLanguage,
				actualText:     textBox.uriActualText,
				altDescription: textBox.uriAltDescription,
			})
		}
		page.SetTextDirection(0)
	}
	return [2]float32{textBox.x + textBox.width, textBox.y + textBox.height}
}

func (textBox *TextBox) drawTextLine(page *Page, text string, xText, yText float32) {
	font := textBox.font
	fallbackFont := textBox.fallbackFont
	fontSize := textBox.fontSize

	page.AddBMC("P", textBox.language, text, textBox.altDescription)

	if textBox.textDirection == direction.LeftToRight {
		page.DrawStringUsingColorMap(
			font, fallbackFont, fontSize, text, xText, yText, textBox.textColor, textBox.colors)
	} else if textBox.textDirection == direction.BottomToTop {
		page.SetTextDirection(90)
		page.DrawStringUsingColorMap(
			font, fallbackFont, fontSize, text, yText, xText+textBox.height,
			textBox.textColor, textBox.colors)
	} else if textBox.textDirection == direction.TopToBottom {
		page.SetTextDirection(270)
		page.DrawStringUsingColorMap(
			font, fallbackFont, fontSize, text,
			(yText+textBox.width)-(textBox.margin+2*font.GetAscent(fontSize)), xText,
			textBox.textColor, textBox.colors)
	}

	page.AddEMC()

	if textBox.textDirection == direction.LeftToRight {
		lineLength := font.StringWidthFB(fallbackFont, fontSize, text)
		if textBox.GetUnderline() {
			page.AddArtifactBMC()
			page.MoveTo(xText, yText+font.GetUnderlinePosition(fontSize))
			page.LineTo(xText+lineLength, yText+font.GetUnderlinePosition(fontSize))
			page.StrokePath()
			page.AddEMC()
		}
		if textBox.GetStrikeout() {
			page.AddArtifactBMC()
			page.MoveTo(xText, yText-(font.GetBodyHeight(fontSize)/4))
			page.LineTo(xText+lineLength, yText-(font.GetBodyHeight(fontSize)/4))
			page.StrokePath()
			page.AddEMC()
		}
	}
}
