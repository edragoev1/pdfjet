package pdfjet

/**
 * textbox.go
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

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/border"
	"github.com/edragoev1/pdfjet/src/color"
)

// TextBox creates box containing line-wrapped text.
// <p>Defaults:<br />
// x = 0.0<br />
// y = 0.0<br />
// width = 300.0<br />
// height = 0.0<br />
// alignment = align.Left<br />
// valign = align.Top<br />
// spacing = 0.0<br />
// margin = 0.0<br />
// </p>
// This class was originally developed by Ronald Bourret.
// It was completely rewritten in 2013 by Eugene Dragoev.
type TextBox struct {
	font, fallbackFont *Font
	text               string
	x, y               float32
	width              float32
	height             float32
	spacing            float32
	margin             float32
	lineWidth          float32
	background         int32
	pen                int32
	brush              int32
	valign             int
	colors             map[string]int32

	// TextBox properties
	// Future use:
	// bits 0 to 15
	// Border:
	// bit 16 - top
	// bit 17 - bottom
	// bit 18 - left
	// bit 19 - right
	// Text Alignment:
	// bit 20
	// bit 21
	// Text Decoration:
	// bit 22 - underline
	// bit 23 - strikeout
	// Future use:
	// bits 24 to 31
	properties int
}

// NewTextBox creates a text box and sets the font.
//
//	@param font the font.
func NewTextBox(font *Font) *TextBox {
	textBox := new(TextBox)
	textBox.font = font
	textBox.width = 300.0
	textBox.spacing = 0.0
	textBox.margin = 0.0
	textBox.background = color.White
	textBox.pen = color.Black
	textBox.brush = color.Black
	textBox.properties = 0x00000001
	textBox.SetTextAlignment(align.Left)
	textBox.valign = align.Top
	return textBox
}

// SetFont sets the font for textBox text box.
//
//	@param font the font.
func (textBox *TextBox) SetFont(font *Font) {
	textBox.font = font
}

// GetFont returns the font used by textBox text box.
// @return the font.
func (textBox *TextBox) GetFont() *Font {
	return textBox.font
}

// SetText sets the text box text.
// @param text the text box text.
func (textBox *TextBox) SetText(text string) {
	textBox.text = text
}

// GetText returns the text box text.
// @return the text box text.
func (textBox *TextBox) GetText() string {
	return textBox.text
}

// SetLocation sets the location where textBox text box will be drawn on the page.
// @param x the x coordinate of the top left corner of the text box.
// @param y the y coordinate of the top left corner of the text box.
func (textBox *TextBox) SetLocation(x, y float32) {
	textBox.x = x
	textBox.y = y
}

// GetLocation gets the location where textBox text box will be drawn on the page.
func (textBox *TextBox) GetLocation() [2]float32 {
	return [2]float32{textBox.x, textBox.y}
}

// SetPosition sets the location where textBox text box will be drawn on the page.
// @param x the x coordinate of the top left corner of the text box.
// @param y the y coordinate of the top left corner of the text box.
func (textBox *TextBox) SetPosition(x, y float32) {
	textBox.x = x
	textBox.y = y
}

// GetX gets the x coordinate where textBox text box will be drawn on the page.
// @return the x coordinate of the top left corner of the text box.
func (textBox *TextBox) GetX() float32 {
	return textBox.x
}

// GetY gets the y coordinate where textBox text box will be drawn on the page.
//
//	@return the y coordinate of the top left corner of the text box.
func (textBox *TextBox) GetY() float32 {
	return textBox.y
}

// SetWidth sets the width of textBox text box.
//
//	@param width the specified width.
func (textBox *TextBox) SetWidth(width float32) {
	textBox.width = width
}

// GetWidth returns the text box width.
// @return the text box width.
func (textBox *TextBox) GetWidth() float32 {
	return textBox.width
}

// SetHeight sets the height of textBox text box.
// @param height the specified height.
func (textBox *TextBox) SetHeight(height float32) {
	textBox.height = height
}

// GetHeight returns the text box height.
// @return the text box height.
func (textBox *TextBox) GetHeight() float32 {
	return textBox.height
}

// SetMargin sets the margin of textBox text box.
// @param margin the margin between the text and the box
func (textBox *TextBox) SetMargin(margin float32) {
	textBox.margin = margin
}

// GetMargin returns the text box margin.
// @return the margin between the text and the box
func (textBox *TextBox) GetMargin() float32 {
	return textBox.margin
}

// SetLineWidth sets the border line width.
// @param lineWidth float
func (textBox *TextBox) SetLineWidth(lineWidth float32) {
	textBox.lineWidth = lineWidth
}

// GetLineWidth returns the border line width.
// @return float the line width.
func (textBox *TextBox) GetLineWidth() float32 {
	return textBox.lineWidth
}

// SetSpacing sets the spacing between lines of text.
// @param spacing
func (textBox *TextBox) SetSpacing(spacing float32) {
	textBox.spacing = spacing
}

// GetSpacing returns the spacing between lines of text.
// @return float the spacing.
func (textBox *TextBox) GetSpacing() float32 {
	return textBox.spacing
}

// SetBgColor sets the background to the specified color.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetBgColor(color int32) {
	textBox.background = color
}

// SetBgColor sets the background to the specified color using RGB values.
// @param color the color specified as array of integer values from 0x00 to 0xFF.
func (textBox *TextBox) SetBgColorRGB(color []int32) {
	textBox.background = color[0]<<16 | color[1]<<8 | color[2]
}

// GetBgColor returns the background color.
// @return int the color as 0xRRGGBB integer.
func (textBox *TextBox) GetBgColor() int32 {
	return textBox.background
}

// Sets the pen and brush colors to the specified color.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetFgColor(color int32) {
	textBox.pen = color
	textBox.brush = color
}

// SetFgColor sets the pen and brush colors to the specified color using RGB values.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetFgColorRGB(color []int32) {
	textBox.pen = color[0]<<16 | color[1]<<8 | color[2]
	textBox.brush = textBox.pen
}

// Sets the pen color.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetPenColor(color int32) {
	textBox.pen = color
}

// SetPenColor sets the pen color using RGB values.
// @param color the color specified as an array of int values from 0x00 to 0xFF.
func (textBox *TextBox) SetPenColorRGB(color []int32) {
	textBox.pen = color[0]<<16 | color[1]<<8 | color[2]
}

// GetPenColor returns the pen color as 0xRRGGBB integer.
// @return int the pen color.
func (textBox *TextBox) GetPenColor() int32 {
	return textBox.pen
}

// Sets the brush color.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetBrushColor(color int32) {
	textBox.brush = color
}

// SetBrushColor sets the brush color.
// @param color the color specified as an array of int values from 0x00 to 0xFF.
func (textBox *TextBox) SetBrushColorRGB(color []int32) {
	textBox.brush = color[0]<<16 | color[1]<<8 | color[2]
}

// GetBrushColor returns the brush color.
// @return int the brush color specified as 0xRRGGBB integer.
func (textBox *TextBox) GetBrushColor() int32 {
	return textBox.brush
}

// SetBorder sets the TextBox border properties.
// @param border the border properties.
func (textBox *TextBox) SetBorder(border int) {
	textBox.properties |= border
}

// GetBorder returns the text box border property values.
// @return boolean true if the specific border property value is set.
func (textBox *TextBox) GetBorder(value int) bool {
	if value == border.None {
		if ((textBox.properties >> 16) & 0xF) == 0x0 {
			return true
		}
	} else if value == border.Top {
		if ((textBox.properties >> 16) & 0x1) == 0x1 {
			return true
		}
	} else if value == border.Bottom {
		if ((textBox.properties >> 16) & 0x2) == 0x2 {
			return true
		}
	} else if value == border.Left {
		if ((textBox.properties >> 16) & 0x4) == 0x4 {
			return true
		}
	} else if value == border.Right {
		if ((textBox.properties >> 16) & 0x8) == 0x8 {
			return true
		}
	} else if value == border.All {
		if ((textBox.properties >> 16) & 0xF) == 0xF {
			return true
		}
	}
	return false
}

// SetBorder sets all the TextBox borders.
// @param borders sets all borders if true, no borders otherwise.
func (textBox *TextBox) SetBorders(borders bool) {
	if borders {
		textBox.SetBorder(border.All)
	} else {
		textBox.SetBorder(border.None)
	}
}

// SetTextAlignment sets the cell text alignment.
// @param alignment the alignment code.
// Supported values: align.Left, align.Right and align.Center
func (textBox *TextBox) SetTextAlignment(alignment int) {
	textBox.properties &= 0x00CFFFFF
	textBox.properties |= (alignment & 0x00300000)
}

// GetTextAlignment returns the text alignment.
// @return alignment the alignment code.
// Supported values: align.Left, align.Right and align.Center
func (textBox *TextBox) GetTextAlignment() int {
	return (textBox.properties & 0x00300000)
}

// SetUnderline sets the underline variable.
// If the value of the underline variable is 'true' - the text is underlined.
func (textBox *TextBox) SetUnderline(underline bool) {
	if underline {
		textBox.properties |= 0x00400000
	} else {
		textBox.properties &= 0x00BFFFFF
	}
}

// GetUnderline returns underlined flag.
func (textBox *TextBox) GetUnderline() bool {
	return (textBox.properties & 0x00400000) != 0
}

// SetStrikeout sets the srikeout flag.
// In the flag is true - draw strikeout line through the text.
func (textBox *TextBox) SetStrikeout(strikeout bool) {
	if strikeout {
		textBox.properties |= 0x00800000
	} else {
		textBox.properties &= 0x007FFFFF
	}
}

// GetStrikeout returns the strikeout flag.
func (textBox *TextBox) GetStrikeout() bool {
	return (textBox.properties & 0x00800000) != 0
}

// SetFallbackFont sets the fallback font.
func (textBox *TextBox) SetFallbackFont(font *Font) {
	textBox.fallbackFont = font
}

// GetFallbackFont returns the fallback font.
func (textBox *TextBox) GetFallbackFont() *Font {
	return textBox.fallbackFont
}

// SetVerticalAlignment sets the vertical alignment of the text in textBox TextBox.
// Valid values are align.Top, align.Bottom and align.Center
func (textBox *TextBox) SetVerticalAlignment(alignment int) {
	textBox.valign = alignment
}

// GetVerticalAlignment returns the vertical alignment setting.
func (textBox *TextBox) GetVerticalAlignment() int {
	return textBox.valign
}

// SetTextColors sets the text colors map.
func (textBox *TextBox) SetTextColors(colors map[string]int32) {
	textBox.colors = colors
}

// GetTextColors returns the text colors map.
func (textBox *TextBox) GetTextColors() map[string]int32 {
	return textBox.colors
}

func (textBox *TextBox) drawBorders(page *Page) {
	page.SetPenColor(textBox.pen)
	page.SetPenWidth(textBox.lineWidth)
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
}

// Preserves the leading spaces and tabs
func getStringBuilder(line string) *strings.Builder {
	var buf strings.Builder
	for _, ch := range line {
		if ch == ' ' {
			buf.WriteRune(ch)
		} else if ch == '\t' {
			buf.WriteString("    ")
		} else {
			break
		}
	}
	return &buf
}

func (textBox *TextBox) getTextLines() []string {
	list := make([]string, 0)
	textAreaWidth := textBox.width - 2*textBox.margin
	lines := strings.Split(strings.ReplaceAll(textBox.text, "\r\n", "\n"), "\n")
	for _, line := range lines {
		if textBox.font.StringWidth(textBox.fallbackFont, line) <= textAreaWidth {
			list = append(list, line)
		} else {
			buf1 := getStringBuilder(line) // Preserves the indentation
			var tokens = strings.Fields(line)
			for _, token := range tokens {
				if textBox.font.StringWidth(textBox.fallbackFont, token) > textAreaWidth {
					// We have very long token, so we have to split it
					var buf2 strings.Builder
					for _, ch := range token {
						if textBox.font.StringWidth(
							textBox.fallbackFont, buf2.String()+string(ch)) > textAreaWidth {
							list = append(list, buf2.String())
							buf2.Reset()
						}
						buf2.WriteRune(ch)
					}
					if buf2.Len() > 0 {
						buf1.WriteString(buf2.String() + " ")
					}
				} else {
					if textBox.font.StringWidth(
						textBox.fallbackFont, buf1.String()+token) > textAreaWidth {
						list = append(list, buf1.String())
						buf1.Reset()
					}
					buf1.WriteString(token + " ")
				}
			}
			if buf1.Len() > 0 {
				str := strings.Trim(buf1.String(), " ")
				if textBox.font.StringWidth(textBox.fallbackFont, str) <= textAreaWidth {
					list = append(list, str)
				} else {
					// We have very long token, so we have to split it
					var buf2 strings.Builder
					for _, ch := range str {
						if textBox.font.StringWidth(textBox.fallbackFont, buf2.String()+string(ch)) > textAreaWidth {
							list = append(list, buf2.String())
							buf2.Reset()
						}
						buf2.WriteRune(ch)
					}
					if buf2.Len() > 0 {
						list = append(list, buf2.String())
					}
				}
			}
		}
	}
	if len(list) > 0 && strings.Trim(list[len(list)-1], " ") == "" {
		// Remove the last line if it is blank
		list = list[:len(list)-1]
	}
	return list
}

// DrawOn draws textBox text box on the specified page.
// @param page the Page where the TextBox is to be drawn.
// @param draw flag specifying if textBox component should actually be drawn on the page.
// @return x and y coordinates of the bottom right corner of textBox component.
func (textBox *TextBox) DrawOn(page *Page) [2]float32 {
	lines := textBox.getTextLines()
	leading := textBox.font.ascent + textBox.font.descent + textBox.spacing

	if textBox.height > 0.0 { // TextBox with fixed height
		if float32(len(lines))*leading-textBox.spacing > (textBox.height - 2*textBox.margin) {
			list := make([]string, 0)
			for _, line := range lines {
				if float32(len(list)+1)*leading-textBox.spacing > (textBox.height - 2*textBox.margin) {
					break
				}
				list = append(list, line)
			}
			if len(list) > 0 {
				lastLine := list[len(list)-1]
				runes := []rune(lastLine)
				runes = runes[:len(runes)-3]
				lastLine = string(runes)
				list[len(list)-1] = lastLine + "..."
				lines = list
			}
		}
		if page != nil {
			if textBox.GetBgColor() != color.Transparent {
				page.SetBrushColor(textBox.background)
				page.FillRect(textBox.x, textBox.y, textBox.width, textBox.height)
			}
			page.SetPenColor(textBox.pen)
			page.SetBrushColor(textBox.brush)
			page.SetPenWidth(textBox.font.underlineThickness)
		}
		xText := textBox.x + textBox.margin
		yText := textBox.y + textBox.margin + textBox.font.ascent
		if textBox.valign == align.Top {
			yText = textBox.y + textBox.margin + textBox.font.ascent
		} else if textBox.valign == align.Bottom {
			yText = (textBox.y + textBox.height) - (float32(len(lines))*leading + textBox.margin)
			yText += textBox.font.ascent
		} else if textBox.valign == align.Center {
			yText = textBox.y + (textBox.height-float32(len(lines))*leading)/2
			yText += textBox.font.ascent
		}
		for _, line := range lines {
			if textBox.GetTextAlignment() == align.Left {
				xText = textBox.x + textBox.margin
			} else if textBox.GetTextAlignment() == align.Right {
				xText = (textBox.x + textBox.width) - (textBox.font.StringWidth(textBox.fallbackFont, line) + textBox.margin)
			} else if textBox.GetTextAlignment() == align.Right {
				xText = textBox.x + (textBox.width-textBox.font.StringWidth(textBox.fallbackFont, line))/2
			}
			if yText+textBox.font.descent <= textBox.y+textBox.height {
				if page != nil {
					textBox.DrawText(page, textBox.font, textBox.fallbackFont, line, xText, yText, textBox.colors)
				}
				yText += leading
			}
		}
	} else { // TextBox that expands to fit the content
		if page != nil {
			if textBox.GetBgColor() != color.Transparent {
				page.SetBrushColor(textBox.background)
				page.FillRect(textBox.x, textBox.y, textBox.width, (float32(len(lines))*leading-textBox.spacing)+2*textBox.margin)
			}
			page.SetPenColor(textBox.pen)
			page.SetBrushColor(textBox.brush)
			page.SetPenWidth(textBox.font.underlineThickness)
		}
		xText := textBox.x + textBox.margin
		yText := textBox.y + textBox.margin + textBox.font.ascent
		for _, line := range lines {
			if textBox.GetTextAlignment() == align.Left {
				xText = textBox.x + textBox.margin
			} else if textBox.GetTextAlignment() == align.Right {
				xText = (textBox.x + textBox.width) - (textBox.font.StringWidth(textBox.fallbackFont, line) + textBox.margin)
			} else if textBox.GetTextAlignment() == align.Center {
				xText = textBox.x + (textBox.width-textBox.font.StringWidth(textBox.fallbackFont, line))/2
			}
			if page != nil {
				textBox.DrawText(page, textBox.font, textBox.fallbackFont, line, xText, yText, textBox.colors)
			}
			yText += leading
		}
		textBox.height = ((yText - textBox.y) - (textBox.font.ascent + textBox.spacing)) + textBox.margin
	}
	if page != nil {
		textBox.drawBorders(page)
	}

	return [2]float32{textBox.x + textBox.width, textBox.y + textBox.height}
}

// DrawText draws the text on the page.
func (textBox *TextBox) DrawText(
	page *Page,
	font, fallbackFont *Font,
	text string,
	xText float32,
	yText float32,
	colors map[string]int32) {
	page.DrawStringUsingColorMap(font, fallbackFont, text, xText, yText, color.Black, colors)
	lineLength := textBox.font.stringWidth(text)
	if textBox.GetUnderline() {
		yAdjust := font.underlinePosition
		page.MoveTo(xText, yText+yAdjust)
		page.LineTo(xText+lineLength, yText+yAdjust)
		page.StrokePath()
	}
	if textBox.GetStrikeout() {
		yAdjust := font.bodyHeight / 4
		page.MoveTo(xText, yText-yAdjust)
		page.LineTo(xText+lineLength, yText-yAdjust)
		page.StrokePath()
	}
}
