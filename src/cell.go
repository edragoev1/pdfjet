package pdfjet

/**
 * cell.go
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
	"log"
	"strings"

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/border"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/single"
	"github.com/edragoev1/pdfjet/src/structuretype"
)

// Cell is used to create table cell objects.
// See the Table class for more information.
type Cell struct {
	font              *Font
	fallbackFont      *Font
	text              *string
	image             *Image
	barCode           *BarCode
	textBlock         *TextBlock
	point             *Point
	compositeTextLine *CompositeTextLine
	textBox           *TextBox
	width             float32
	topPadding        float32
	bottomPadding     float32
	leftPadding       float32
	rightPadding      float32
	lineWidth         float32
	background        uint32
	pen               uint32
	brush             uint32

	// Cell properties
	// Colspan:
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
	properties uint32
	uri, key   *string
	valign     int
	drawable   Drawable
}

// NewEmptyCell creates a cell object and sets the font.
// @param font the font.
func NewEmptyCell(font *Font) *Cell {
	return NewCell(font, "")
}

// NewCell creates a cell object and sets the font and the cell text.
// @param font the font.
// @param text the text.
func NewCell(font *Font, text string) *Cell {
	cell := new(Cell)
	cell.font = font
	cell.text = &text
	cell.width = 50.0
	cell.topPadding = 2.0
	cell.bottomPadding = 2.0
	cell.leftPadding = 2.0
	cell.rightPadding = 2.0
	cell.lineWidth = 0.2
	cell.background = color.White
	cell.pen = color.Black
	cell.brush = color.Black
	cell.properties = 0x000F0001
	cell.valign = align.Top
	return cell
}

// SetFont sets the font for this cell.
// @param font the font.
func (cell *Cell) SetFont(font *Font) {
	cell.font = font
}

// SetFallbackFont sets the fallback font for this cell.
// @param fallbackFont the fallback font.
func (cell *Cell) SetFallbackFont(fallbackFont *Font) {
	cell.fallbackFont = fallbackFont
}

// GetFont returns the font used by this cell.
// @return the font.
func (cell *Cell) GetFont() *Font {
	return cell.font
}

// GetFallbackFont returns the fallback font used by this cell.
// @return the fallback font.
func (cell *Cell) GetFallbackFont() *Font {
	return cell.fallbackFont
}

// SetText sets the cell text.
// @param text the cell text.
func (cell *Cell) SetText(text string) {
	cell.text = &text
}

// GetText returns the cell text.
func (cell *Cell) GetText() string {
	return *cell.text
}

// SetImage sets the image inside this cell.
func (cell *Cell) SetImage(image *Image) {
	cell.image = image
}

// GetImage returns the cell image.
func (cell *Cell) GetImage() *Image {
	return cell.image
}

// SetBarcode -- TODO:
func (cell *Cell) SetBarcode(barCode *BarCode) {
	cell.barCode = barCode
}

// SetTextBlock -- TODO:
func (cell *Cell) SetTextBlock(textBlock *TextBlock) {
	cell.textBlock = textBlock
}

// SetPoint sets the point inside this cell.
// See the Point class and Example_09 for more information.
func (cell *Cell) SetPoint(point *Point) {
	cell.point = point
}

// GetPoint returns the cell point.
func (cell *Cell) GetPoint() *Point {
	return cell.point
}

// SetCompositeTextLine sets the composite text object.
// @param compositeTextLine the composite text object.
func (cell *Cell) SetCompositeTextLine(compositeTextLine *CompositeTextLine) {
	cell.compositeTextLine = compositeTextLine
}

// GetCompositeTextLine returns the composite text object.
// @return the composite text object.
func (cell *Cell) GetCompositeTextLine() *CompositeTextLine {
	return cell.compositeTextLine
}

// SetTextBox sets the composite text object.
// @param compositeTextLine the composite text object.
func (cell *Cell) SetTextBox(textBox *TextBox) {
	cell.textBox = textBox
}

// GetCompositeTextLine returns the composite text object.
// @return the composite text object.
// func (cell *Cell) GetCompositeTextLine() *TextBox {
//	   return cell.textBox
// }

// SetWidth sets the width of this cell.
// @param width the specified width.
func (cell *Cell) SetWidth(width float32) {
	cell.width = width
	if cell.textBlock != nil {
		cell.textBlock.SetWidth(cell.width - (cell.leftPadding + cell.rightPadding))
	}
}

// GetWidth returns the cell width.
// @return the cell width.
func (cell *Cell) GetWidth() float32 {
	return cell.width
}

// SetTopPadding sets the top padding of this cell.
// @param padding the top padding.
func (cell *Cell) SetTopPadding(padding float32) {
	cell.topPadding = padding
}

// SetBottomPadding sets the bottom padding of this cell.
// @param padding the bottom padding.
func (cell *Cell) SetBottomPadding(padding float32) {
	cell.bottomPadding = padding
}

// SetLeftPadding sets the left padding of this cell.
// @param padding the left padding.
func (cell *Cell) SetLeftPadding(padding float32) {
	cell.leftPadding = padding
}

// SetRightPadding sets the right padding of this cell.
// @param padding the right padding.
func (cell *Cell) SetRightPadding(padding float32) {
	cell.rightPadding = padding
}

// SetPadding sets the top, bottom, left and right paddings of this cell.
// @param padding the right padding.
func (cell *Cell) SetPadding(padding float32) {
	cell.topPadding = padding
	cell.bottomPadding = padding
	cell.leftPadding = padding
	cell.rightPadding = padding
}

// GetHeight returns the cell height.
// @return the cell height.
func (cell *Cell) GetHeight() float32 {
	cellHeight := float32(0.0)

	if cell.image != nil {
		height := cell.image.GetHeight() + cell.topPadding + cell.bottomPadding
		if height > cellHeight {
			cellHeight = height
		}
	}

	if cell.barCode != nil {
		height := cell.barCode.GetHeight() + cell.topPadding + cell.bottomPadding
		if height > cellHeight {
			cellHeight = height
		}
	}

	if cell.textBlock != nil {
		height := cell.textBlock.DrawOn(nil)[1] + cell.topPadding + cell.bottomPadding
		if height > cellHeight {
			cellHeight = height
		}
	}

	if cell.drawable != nil {
		height := cell.drawable.DrawOn(nil)[1] + cell.topPadding + cell.bottomPadding
		if height > cellHeight {
			cellHeight = height
		}
	}

	if cell.text != nil {
		fontHeight := cell.font.GetHeight()
		if cell.fallbackFont != nil && cell.fallbackFont.GetHeight() > fontHeight {
			fontHeight = cell.fallbackFont.GetHeight()
		}
		height := fontHeight + cell.topPadding + cell.bottomPadding
		if height > cellHeight {
			cellHeight = height
		}
	}

	return cellHeight
}

// SetLineWidth sets the border line width.
func (cell *Cell) SetLineWidth(lineWidth float32) {
	cell.lineWidth = lineWidth
}

// GetLineWidth returns the border line width.
func (cell *Cell) GetLineWidth() float32 {
	return cell.lineWidth
}

// SetBgColor sets the background to the specified color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetBgColor(color uint32) {
	cell.background = color
}

// GetBgColor returns the background color of this cell.
func (cell *Cell) GetBgColor() uint32 {
	return cell.background
}

// SetPenColor sets the pen color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetPenColor(color uint32) {
	cell.pen = color
}

// GetPenColor returns the pen color.
func (cell *Cell) GetPenColor() uint32 {
	return cell.pen
}

// SetBrushColor sets the brush color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetBrushColor(color uint32) {
	cell.brush = color
}

// GetBrushColor returns the brush color.
// @return the brush color.
func (cell *Cell) GetBrushColor() uint32 {
	return cell.brush
}

// SetFgColor sets the pen and brush colors to the specified color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetFgColor(color uint32) {
	cell.pen = color
	cell.brush = color
}

// SetProperties sets the properties.
func (cell *Cell) SetProperties(properties uint32) {
	cell.properties = properties
}

// GetProperties returns the properties.
func (cell *Cell) GetProperties() uint32 {
	return cell.properties
}

// SetColSpan sets the column span func (cell *Cell) variable.
// @param colspan the specified column span value.
func (cell *Cell) SetColSpan(colspan int) {
	cell.properties &= 0x00FF0000
	cell.properties |= (uint32(colspan) & 0x0000FFFF)
}

// GetColSpan returns the column span func (cell *Cell) variable value.
// @return the column span value.
func (cell *Cell) GetColSpan() int {
	return int(cell.properties & 0x0000FFFF)
}

// SetBorder sets the cell border object.
// @param border the border object.
func (cell *Cell) SetBorder(border int, visible bool) {
	if visible {
		cell.properties |= uint32(border)
	} else {
		cell.properties &= (^uint32(border) & 0x00FFFFFF)
	}
}

// GetBorder returns the cell border object.
// @return the cell border object.
func (cell *Cell) GetBorder(border int) bool {
	return (cell.properties & uint32(border)) != 0
}

// SetNoBorders sets all border object parameters to false.
// This cell will have no borders when drawn on the page.
func (cell *Cell) SetNoBorders() {
	cell.properties &= 0x00F0FFFF
}

// SetTextAlignment sets the cell text alignment.
// @param alignment the alignment code.
// Supported values: align.Left, align.Right and align.Center
func (cell *Cell) SetTextAlignment(alignment int) {
	cell.properties &= 0x00CFFFFF
	cell.properties |= (uint32(alignment) & 0x00300000)
}

// GetTextAlignment returns the text alignment.
// @return the text horizontal alignment code.
func (cell *Cell) GetTextAlignment() int {
	return int(cell.properties & 0x00300000)
}

// SetVerTextAlignment sets the cell text vertical alignment.
// @param alignment the alignment code.
// Supported values: align.Top, align.Center and align.Bottom
func (cell *Cell) SetVerTextAlignment(alignment int) {
	cell.valign = alignment
}

// GetVerTextAlignment returns the cell text vertical alignment.
// @return the vertical alignment code.
func (cell *Cell) GetVerTextAlignment() int {
	return cell.valign
}

// SetUnderline sets the underline text parameter.
// If the value of the underline variable is 'true' - the text is underlined.
// @param underline the underline text parameter.
func (cell *Cell) SetUnderline(underline bool) {
	if underline {
		cell.properties |= 0x00400000
	} else {
		cell.properties &= 0x00BFFFFF
	}
}

// GetUnderline returns the underline text parameter.
// @return the underline text parameter.
func (cell *Cell) GetUnderline() bool {
	return (cell.properties & 0x00400000) != 0
}

// SetStrikeout sets the strikeout text parameter.
// @param strikeout the strikeout text parameter.
func (cell *Cell) SetStrikeout(strikeout bool) {
	if strikeout {
		cell.properties |= 0x00800000
	} else {
		cell.properties &= 0x007FFFFF
	}
}

// GetStrikeout returns the strikeout text parameter.
// @return the strikeout text parameter.
func (cell *Cell) GetStrikeout() bool {
	return (cell.properties & 0x00800000) != 0
}

// SetURIAction sets the URI action.
func (cell *Cell) SetURIAction(uri *string) {
	cell.uri = uri
}

// Paint draws the point, text and borders of this cell.
func (cell *Cell) Paint(page *Page, x, y, w, h float32) {
	if cell.background != color.White {
		cell.drawBackground(page, x, y, w, h)
	}
	if cell.image != nil {
		if cell.GetTextAlignment() == align.Left {
			cell.image.SetLocation(x+cell.leftPadding, y+cell.topPadding)
			cell.image.DrawOn(page)
		} else if cell.GetTextAlignment() == align.Center {
			cell.image.SetLocation((x+w/2.0)-cell.image.GetWidth()/2.0, y+cell.topPadding)
			cell.image.DrawOn(page)
		} else if cell.GetTextAlignment() == align.Right {
			cell.image.SetLocation((x+w)-(cell.image.GetWidth()+cell.leftPadding), y+cell.topPadding)
			cell.image.DrawOn(page)
		}
	}
	if cell.barCode != nil {
		if cell.GetTextAlignment() == align.Left {
			cell.barCode.drawOnPageAtLocation(page, x+cell.leftPadding, y+cell.topPadding)
		} else if cell.GetTextAlignment() == align.Center {
			barcodeWidth := cell.barCode.DrawOn(nil)[0]
			cell.barCode.drawOnPageAtLocation(page, (x+w/2.0)-barcodeWidth/2.0, y+cell.topPadding)
		} else if cell.GetTextAlignment() == align.Right {
			barcodeWidth := cell.barCode.DrawOn(nil)[0]
			cell.barCode.drawOnPageAtLocation(page, (x+w)-(barcodeWidth+cell.leftPadding), y+cell.topPadding)
		}
	}
	if cell.textBlock != nil {
		cell.textBlock.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textBlock.DrawOn(page)
	}
	cell.drawBorders(page, x, y, w, h)
	if cell.text != nil {
		cell.DrawText(page, x, y, w, h)
	}
	if cell.point != nil {
		if cell.point.align == align.Left {
			cell.point.x = x + 2*cell.point.r
		} else if cell.point.align == align.Right {
			cell.point.x = (x + w) - cell.rightPadding/2
		}
		cell.point.y = y + h/2
		page.SetBrushColor(cell.point.GetColor())

		if cell.point.uri != nil {
			page.AddAnnotation(NewAnnotation(
				cell.point.uri,
				nil,
				cell.point.x-cell.point.r,
				cell.point.y-cell.point.r,
				cell.point.x+cell.point.r,
				cell.point.y+cell.point.r,
				"",
				"",
				""))
		}

		page.DrawPoint(cell.point)
	}

	if cell.drawable != nil {
		cell.drawable.SetPosition(x+cell.leftPadding, y+cell.topPadding)
		cell.drawable.DrawOn(page)
	}
}

func (cell *Cell) drawBackground(page *Page, x, y, wCell, hCell float32) {
	page.SetBrushColor(cell.background)
	page.FillRect(x, y+cell.lineWidth/2, wCell, hCell+cell.lineWidth)
}

func (cell *Cell) drawBorders(page *Page, x, y, cellW, cellH float32) {
	page.SetPenColor(cell.pen)
	page.SetPenWidth(cell.lineWidth)

	if cell.GetBorder(border.Top) &&
		cell.GetBorder(border.Bottom) &&
		cell.GetBorder(border.Left) &&
		cell.GetBorder(border.Right) {
		page.AddBMC(structuretype.P, single.Space, single.Space, single.Space)
		page.DrawRect(x, y, cellW, cellH)
		page.AddEMC()
	} else {
		qWidth := cell.lineWidth / 4.0
		if cell.GetBorder(border.Top) {
			page.AddBMC(structuretype.P, single.Space, single.Space, single.Space)
			page.MoveTo(x-qWidth, y)
			page.LineTo(x+cellW, y)
			page.StrokePath()
			page.AddEMC()
		}
		if cell.GetBorder(border.Bottom) {
			page.AddBMC(structuretype.P, single.Space, single.Space, single.Space)
			page.MoveTo(x-qWidth, y+cellH)
			page.LineTo(x+cellW, y+cellH)
			page.StrokePath()
			page.AddEMC()
		}
		if cell.GetBorder(border.Left) {
			page.AddBMC(structuretype.P, single.Space, single.Space, single.Space)
			page.MoveTo(x, y-qWidth)
			page.LineTo(x, y+cellH+qWidth)
			page.StrokePath()
			page.AddEMC()
		}
		if cell.GetBorder(border.Right) {
			page.AddBMC(structuretype.P, single.Space, single.Space, single.Space)
			page.MoveTo(x+cellW, y-qWidth)
			page.LineTo(x+cellW, y+cellH+qWidth)
			page.StrokePath()
			page.AddEMC()
		}
	}
}

// DrawText draws the cell text.
func (cell *Cell) DrawText(page *Page, x, y, wCell, hCell float32) {
	var xText float32
	var yText float32
	if cell.valign == align.Top {
		yText = y + cell.font.ascent + cell.topPadding
	} else if cell.valign == align.Center {
		yText = y + hCell/2 + cell.font.ascent/2
	} else if cell.valign == align.Bottom {
		yText = (y + hCell) - cell.bottomPadding
	} else {
		log.Fatal("Invalid vertical text alignment option.")
	}

	page.SetPenColor(cell.pen)
	page.SetBrushColor(cell.brush)

	if cell.GetTextAlignment() == align.Right {
		if cell.compositeTextLine == nil {
			xText = (x + wCell) - (cell.font.stringWidth(*cell.text) + cell.rightPadding)
			page.AddBMC("Span", "", *cell.text, *cell.text)
			page.DrawString(cell.font, cell.fallbackFont, *cell.text, xText, yText)
			page.AddEMC()
			if cell.GetUnderline() {
				cell.UnderlineText(page, cell.font, *cell.text, xText, yText)
			}
			if cell.GetStrikeout() {
				cell.StrikeoutText(page, cell.font, *cell.text, xText, yText)
			}
		} else {
			xText = (x + wCell) - (cell.compositeTextLine.GetWidth() + cell.rightPadding)
			cell.compositeTextLine.SetLocation(xText, yText)
			page.AddBMC("Span", "", *cell.text, *cell.text)
			cell.compositeTextLine.DrawOn(page)
			page.AddEMC()
		}
	} else if cell.GetTextAlignment() == align.Center {
		if cell.compositeTextLine == nil {
			xText = x + cell.leftPadding +
				(((wCell - (cell.leftPadding + cell.rightPadding)) - cell.font.stringWidth(*cell.text)) / 2)
			page.AddBMC("Span", "", *cell.text, *cell.text)
			page.DrawString(cell.font, cell.fallbackFont, *cell.text, xText, yText)
			page.AddEMC()
			if cell.GetUnderline() {
				cell.UnderlineText(page, cell.font, *cell.text, xText, yText)
			}
			if cell.GetStrikeout() {
				cell.StrikeoutText(page, cell.font, *cell.text, xText, yText)
			}
		} else {
			xText = x + cell.leftPadding +
				(((wCell - (cell.leftPadding + cell.rightPadding)) - cell.compositeTextLine.GetWidth()) / 2)
			cell.compositeTextLine.SetLocation(xText, yText)
			page.AddBMC("Span", "", *cell.text, *cell.text)
			cell.compositeTextLine.DrawOn(page)
			page.AddEMC()
		}
	} else if cell.GetTextAlignment() == align.Left {
		xText = x + cell.leftPadding
		if cell.compositeTextLine == nil {
			page.AddBMC("Span", "", *cell.text, *cell.text)
			page.DrawString(cell.font, cell.fallbackFont, *cell.text, xText, yText)
			page.AddEMC()
			if cell.GetUnderline() {
				cell.UnderlineText(page, cell.font, *cell.text, xText, yText)
			}
			if cell.GetStrikeout() {
				cell.StrikeoutText(page, cell.font, *cell.text, xText, yText)
			}
		} else {
			cell.compositeTextLine.SetLocation(xText, yText)
			page.AddBMC("Span", "", *cell.text, *cell.text)
			cell.compositeTextLine.DrawOn(page)
			page.AddEMC()
		}
	} else {
		log.Fatal("Invalid Text Alignment!")
	}

	if cell.uri != nil || cell.key != nil {
		var w float32
		if cell.compositeTextLine != nil {
			w = cell.compositeTextLine.GetWidth()
		} else {
			w = cell.font.stringWidth(*cell.text)
		}
		page.AddAnnotation(NewAnnotation(
			cell.uri,
			nil,
			xText,
			yText-cell.font.ascent,
			xText+w,
			yText+cell.font.descent,
			"",
			"",
			""))
	}
}

// UnderlineText underlines the cell text.
func (cell *Cell) UnderlineText(page *Page, font *Font, text string, x, y float32) {
	page.AddBMC("Span", "", "underline", "underline")
	page.SetPenWidth(font.underlineThickness)
	page.MoveTo(x, y+font.descent)
	page.LineTo(x+font.stringWidth(text), y+font.descent)
	page.StrokePath()
	page.AddEMC()
}

// StrikeoutText strikes out the cell text.
func (cell *Cell) StrikeoutText(page *Page, font *Font, text string, x, y float32) {
	page.AddBMC("Span", "", "strike out", "strike out")
	page.SetPenWidth(font.underlineThickness)
	page.MoveTo(x, y-font.GetAscent()/3.0)
	page.LineTo(x+font.stringWidth(text), y-font.GetAscent()/3.0)
	page.StrokePath()
	page.AddEMC()
}

// getNumVerCells returns the number of vertical cells needed to wrap around the cell text.
func (cell *Cell) getNumVerCells() int {
	n := 1
	if cell.text == nil {
		return n
	}

	textLines := strings.Fields(*cell.text)
	if len(textLines) == 0 {
		return n
	}

	n = 0
	for _, textLine := range textLines {
		tokens := strings.Fields(textLine)
		var sb strings.Builder
		if len(tokens) > 1 {
			for i, token := range tokens {
				if cell.font.stringWidth(sb.String()+" "+token) >
					cell.width-(cell.leftPadding+cell.rightPadding) {
					sb.Reset()
					sb.WriteString(token)
					n++
				} else {
					if i > 0 {
						sb.WriteString(" ")
					}
					sb.WriteString(token)
				}
			}
		}
		n++
	}

	return n
}

// GetTextBlock -- TODO:
func (cell *Cell) GetTextBlock() *TextBlock {
	return cell.textBlock
}
