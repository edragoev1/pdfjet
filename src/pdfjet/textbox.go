package pdfjet

/**
 * textbox.go
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
	"align"
	"border"
	"color"
	"strings"
)

// TextBox creates box containing line-wrapped text.
// <p>Defaults:<br />
// x = 0f<br />
// y = 0f<br />
// width = 300f<br />
// height = 0f<br />
// alignment = align.Left<br />
// valign = align.Top<br />
// spacing = 3f<br />
// margin = 1f<br />
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
	background         uint32
	pen                uint32
	brush              uint32
	valign             int
	colors             map[string]uint32

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
//  @param font the font.
func NewTextBox(font *Font) *TextBox {
	textBox := new(TextBox)
	textBox.font = font
	textBox.width = 300.0
	textBox.spacing = 3.0
	textBox.margin = 1.0
	textBox.background = color.White
	textBox.pen = color.Black
	textBox.brush = color.Black
	textBox.properties = 0x000F0001
	return textBox
}

/**
//  Creates a text box and sets the font.
 *
//  @param text the text.
//  @param font the font.
*/
/*
func TextBox(Font font, String text) {
    textBox.font = font
    textBox.text = text
}
*/

/**
//  Creates a text box and sets the font and the text.
 *
//  @param font the font.
//  @param text the text.
//  @param width the width.
//  @param height the height.
*/
/*
func NewTextBox(Font font, String text, float width, float height) {
    textBox.font = font
    textBox.text = text
    textBox.width = width
    textBox.height = height
}
*/

// SetFont sets the font for textBox text box.
//  @param font the font.
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
//  @return the y coordinate of the top left corner of the text box.
func (textBox *TextBox) GetY() float32 {
	return textBox.y
}

// SetWidth sets the width of textBox text box.
//  @param width the specified width.
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
func (textBox *TextBox) SetBgColor(color uint32) {
	textBox.background = color
}

/*
// SetBgColor sets the background to the specified color.
// @param color the color specified as array of integer values from 0x00 to 0xFF.
func (textBox *TextBox) SetBgColor(color []int) {
	textBox.background = color[0]<<16 | color[1]<<8 | color[2]
}
*/

// GetBgColor returns the background color.
// @return int the color as 0xRRGGBB integer.
func (textBox *TextBox) GetBgColor() uint32 {
	return textBox.background
}

//  Sets the pen and brush colors to the specified color.
//  @param color the color specified as 0xRRGGBB integer.
/*
func (textBox *TextBox) SetFgColor(color uint32) {
	textBox.pen = color
	textBox.brush = color
}
*/

// SetFgColor sets the pen and brush colors to the specified color.
// @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetFgColor(color []uint32) {
	textBox.pen = color[0]<<16 | color[1]<<8 | color[2]
	textBox.brush = textBox.pen
}

/**
//  Sets the pen color.
//  @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetPenColor(color uint32) {
	textBox.pen = color
}
*/

// SetPenColor sets the pen color.
// @param color the color specified as an array of int values from 0x00 to 0xFF.
func (textBox *TextBox) SetPenColor(color []uint32) {
	textBox.pen = color[0]<<16 | color[1]<<8 | color[2]
}

// GetPenColor returns the pen color as 0xRRGGBB integer.
// @return int the pen color.
func (textBox *TextBox) GetPenColor() uint32 {
	return textBox.pen
}

/*
//  Sets the brush color.
//  @param color the color specified as 0xRRGGBB integer.
func (textBox *TextBox) SetBrushColor(color uint32) {
	textBox.brush = color
}
*/

// SetBrushColor sets the brush color.
// @param color the color specified as an array of int values from 0x00 to 0xFF.
func (textBox *TextBox) SetBrushColor(color []uint32) {
	textBox.brush = color[0]<<16 | color[1]<<8 | color[2]
}

// GetBrushColor returns the brush color.
// @return int the brush color specified as 0xRRGGBB integer.
func (textBox *TextBox) GetBrushColor() uint32 {
	return textBox.brush
}

// SetBorder sets the TextBox border object.
// @param border the border object.
func (textBox *TextBox) SetBorder(border int, visible bool) {
	if visible {
		textBox.properties |= border
	} else {
		textBox.properties &= (^border & 0x00FFFFFF)
	}
}

// GetBorder returns the text box border.
// @return boolean the text border object.
func (textBox *TextBox) GetBorder(border int) bool {
	return (textBox.properties & border) != 0
}

// SetNoBorders sets all borders to be invisible.
// This cell will have no borders when drawn on the page.
func (textBox *TextBox) SetNoBorders() {
	textBox.properties &= 0x00F0FFFF
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
func (textBox *TextBox) SetTextColors(colors map[string]uint32) {
	textBox.colors = colors
}

// GetTextColors returns the text colors map.
func (textBox *TextBox) GetTextColors() map[string]uint32 {
	return textBox.colors
}

// DrawOn draws textBox text box on the specified page.
// @param page the Page where the TextBox is to be drawn.
// @param draw flag specifying if textBox component should actually be drawn on the page.
// @return x and y coordinates of the bottom right corner of textBox component.
func (textBox *TextBox) DrawOn(page *Page) [2]float32 {
	return textBox.drawTextAndBorders(page)
}

func (textBox *TextBox) drawBackground(page *Page) {
	page.SetBrushColor(textBox.background)
	page.FillRect(textBox.x, textBox.y, textBox.width, textBox.height)
}

func (textBox *TextBox) drawBorders(page *Page) {
	page.SetPenColor(textBox.pen)
	page.SetPenWidth(textBox.lineWidth)

	if textBox.GetBorder(border.Top) &&
		textBox.GetBorder(border.Bottom) &&
		textBox.GetBorder(border.Left) &&
		textBox.GetBorder(border.Right) {
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

// Splits the text line and adds the line segments to the lines.
func (textBox *TextBox) reformat(line string, textAreaWidth float32) []string {
	lines := make([]string, 0)

	var buf strings.Builder
	runes := []rune(line)
	for i := 0; i < len(runes); i++ {
		ch := runes[i]
		buf.WriteRune(ch)
		str := buf.String()
		if textBox.font.stringWidth(str) > textAreaWidth {
			if (ch == ' ') || len(strings.Fields(str)) == 1 {
				lines = append(lines, str)
			} else {
				lines = append(lines, str[0:strings.LastIndex(str, " ")])
				for runes[i] != ' ' {
					i--
				}
			}
			buf.Reset()
		}
	}
	if len(buf.String()) > 0 {
		lines = append(lines, buf.String())
	}

	return lines
}

func (textBox *TextBox) drawTextAndBorders(page *Page) [2]float32 {
	textAreaWidth := textBox.width - (textBox.font.stringWidth("w") + 2*textBox.margin)
	textLines := make([]string, 0)
	lines := strings.Split(strings.Replace(textBox.text, "\r\n", "\n", -1), "\n")
	for _, line := range lines {
		if textBox.font.stringWidth(line) < textAreaWidth {
			textLines = append(textLines, line)
		} else {
			textLines = append(textLines, textBox.reformat(line, textAreaWidth)...)
		}
	}
	lines = textLines

	lineHeight := textBox.font.bodyHeight + textBox.spacing
	var xText float32 = 0.0
	yText := textBox.y + textBox.font.ascent + textBox.margin

	if float32(len(lines))*lineHeight > textBox.height {
		textBox.height = float32(len(lines)) * lineHeight
	}

	if page != nil {
		if textBox.background != color.White {
			textBox.drawBackground(page)
		}
		page.SetPenColor(textBox.pen)
		page.SetBrushColor(textBox.brush)
		page.SetPenWidth(textBox.font.underlineThickness)
	}

	if textBox.height > 0.0 {
		if textBox.valign == align.Bottom {
			yText += textBox.height - float32(len(lines))*lineHeight
		} else if textBox.valign == align.Center {
			yText += (textBox.height - float32(len(lines))*lineHeight) / 2
		}

		for i := 0; i < len(lines); i++ {
			if textBox.GetTextAlignment() == align.Right {
				xText = (textBox.x + textBox.width) - (textBox.font.stringWidth(lines[i]) + textBox.margin)
			} else if textBox.GetTextAlignment() == align.Center {
				xText = textBox.x + (textBox.width - textBox.font.stringWidth(lines[i])/2.0)
			} else { // align.Left
				xText = textBox.x + textBox.margin
			}

			if (yText+textBox.font.GetBodyHeight()+textBox.spacing+textBox.font.GetDescent() >= textBox.y+textBox.height) && (i < (len(lines) - 1)) {
				str := lines[i]
				index := strings.LastIndex(str, " ")
				if index != -1 {
					lines[i] = str[0:index] + " ..."
				} else {
					lines[i] = str + " ..."
				}
			}

			if yText+textBox.font.GetDescent() < textBox.y+textBox.height {
				if page != nil {
					textBox.DrawText(page, textBox.font, textBox.fallbackFont, lines[i], xText, yText, textBox.colors)
				}
				yText += textBox.font.GetBodyHeight() + textBox.spacing
			}
		}
	} else {
		for i := 0; i < len(lines); i++ {
			if textBox.GetTextAlignment() == align.Right {
				xText = (textBox.x + textBox.width) - (textBox.font.stringWidth(lines[i]) + textBox.margin)
			} else if textBox.GetTextAlignment() == align.Center {
				xText = textBox.x + (textBox.width-textBox.font.stringWidth(lines[i]))/2
			} else { // align.Left
				xText = textBox.x + textBox.margin
			}

			if page != nil {
				textBox.DrawText(page, textBox.font, textBox.fallbackFont, lines[i], xText, yText, textBox.colors)
			}
			yText += textBox.font.bodyHeight + textBox.spacing
		}
		textBox.height = yText - (textBox.y + textBox.font.ascent + textBox.margin)
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
	colors map[string]uint32) {
	page.DrawStringUsingColorMap(font, fallbackFont, text, xText, yText, colors)
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
