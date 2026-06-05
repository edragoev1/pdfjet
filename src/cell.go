package pdfjet

/**
 * cell.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"log"

	"github.com/edragoev1/pdfjet/src/alignment"
)

// Cell is used to create table cell objects.
// See the Table class for more information.
type Cell struct {
	font          *Font
	fallbackFont  *Font
	text          string
	textBlock     *TextBlock
	image         *Image
	barcode       *Barcode
	point         *Point
	width         float32
	topPadding    float32
	bottomPadding float32
	leftPadding   float32
	rightPadding  float32
	lineWidth     float32

	background    [3]float32
	hasBackground bool
	pen           [3]float32
	textColor     [3]float32

	colspan      int
	topBorder    bool
	bottomBorder bool
	leftBorder   bool
	rightBorder  bool

	textAlignment int
	uri, key      string
	valign        int

	underline bool
	strikeout bool
}

// NewCell creates a cell object and sets the font and the cell text.
// @param font the font.
// @param text the text.
func NewCell(font *Font, text string) *Cell {
	cell := new(Cell)
	cell.font = font
	cell.text = text
	cell.width = 50.0
	cell.colspan = 1
	cell.topPadding = 2.0
	cell.bottomPadding = 2.0
	cell.leftPadding = 2.0
	cell.rightPadding = 2.0
	cell.lineWidth = 0.0
	//cell.background = color.White		// TODO:
	//cell.pen = color.Black
	//cell.brush = color.Black
	cell.textAlignment = alignment.Left
	cell.valign = alignment.Top
	return cell
}

// SetFont sets the font for this cell.
// @param font the font.
func (cell *Cell) SetFont(font *Font) {
	cell.font = font
}

// GetFont returns the font used by this cell.
// @return the font.
func (cell *Cell) GetFont() *Font {
	return cell.font
}

// SetFallbackFont sets the fallback font for this cell.
// @param fallbackFont the fallback font.
func (cell *Cell) SetFallbackFont(fallbackFont *Font) {
	cell.fallbackFont = fallbackFont
}

// GetFallbackFont returns the fallback font used by this cell.
// @return the fallback font.
func (cell *Cell) GetFallbackFont() *Font {
	return cell.fallbackFont
}

// SetText sets the cell text.
// @param text the cell text.
func (cell *Cell) SetText(text string) {
	cell.text = text
}

// GetText returns the cell text.
func (cell *Cell) GetText() string {
	return cell.text
}

// SetImage sets the image inside this cell.
func (cell *Cell) SetImage(image *Image) {
	cell.image = image
	cell.text = ""
}

// GetImage returns the cell image.
func (cell *Cell) GetImage() *Image {
	return cell.image
}

// SetBarcode sets the barcode for this cell.
func (cell *Cell) SetBarcode(barcode *Barcode) {
	cell.barcode = barcode
	cell.text = ""
}

func (cell *Cell) GetBarcode() *Barcode {
	return cell.barcode
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

// SetTextBlock sets the composite text object.
func (cell *Cell) SetTextBlock(textBlock *TextBlock) {
	cell.textBlock = textBlock
	cell.text = ""
}

func (cell *Cell) GetTextBlock() *TextBlock {
	return cell.textBlock
}

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

func (cell *Cell) GetLeftPadding() float32 {
	return cell.leftPadding
}

// SetRightPadding sets the right padding of this cell.
// @param padding the right padding.
func (cell *Cell) SetRightPadding(padding float32) {
	cell.rightPadding = padding
}

func (cell *Cell) GetRightPadding() float32 {
	return cell.rightPadding
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
func (cell *Cell) GetHeight(width float32) float32 {
	cellHeight := float32(0.0)
	if cell.text != "" {
		fontHeight := cell.font.GetHeight()
		if cell.fallbackFont != nil && cell.fallbackFont.GetHeight() > fontHeight {
			fontHeight = cell.fallbackFont.GetHeight()
		}
		cellHeight = fontHeight + cell.topPadding + cell.bottomPadding
	} else if cell.textBlock != nil {
		cell.textBlock.SetWidth(width)
		cellHeight = (cell.textBlock.DrawOn(nil)[1] - cell.textBlock.y) + cell.topPadding + cell.bottomPadding
	} else if cell.image != nil {
		cellHeight = cell.image.GetHeight() + cell.topPadding + cell.bottomPadding
	} else if cell.barcode != nil {
		cellHeight = cell.barcode.GetHeight() + cell.topPadding + cell.bottomPadding
	}
	return cellHeight
}

func (cell *Cell) GetHeightHeader(width float32) float32 {
	cellHeight := float32(0.0)
	if cell.text != "" {
		fontHeight := cell.font.GetHeight()
		if cell.fallbackFont != nil && cell.fallbackFont.GetHeight() > fontHeight {
			fontHeight = cell.fallbackFont.GetHeight()
		}
		cellHeight = fontHeight + cell.topPadding + cell.bottomPadding
	} else if cell.textBlock != nil {
		cell.textBlock.SetWidth(width)
		cellHeight = (cell.textBlock.DrawOn(nil)[1] - cell.textBlock.y) + cell.topPadding + cell.bottomPadding
	} else if cell.image != nil {
		cellHeight = cell.image.GetHeight() + cell.topPadding + cell.bottomPadding
	} else if cell.barcode != nil {
		cellHeight = cell.barcode.GetHeight() + cell.topPadding + cell.bottomPadding
	}
	return cellHeight
}

// SetLineWidth sets the border width.
func (cell *Cell) SetLineWidth(lineWidth float32) {
	cell.lineWidth = lineWidth
}

// GetLineWidth returns the border width.
func (cell *Cell) GetLineWidth() float32 {
	return cell.lineWidth
}

// SetBgColorRGB sets the background to the specified color.
func (cell *Cell) SetBgColorRGB(color [3]float32) {
	cell.background = color
	cell.hasBackground = true
}

func (cell *Cell) SetBackgroundColor(color int32) {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	cell.background = [3]float32{r, g, b}
	cell.hasBackground = true
}

// GetBgColor returns the background color of this cell.
func (cell *Cell) GetBgColor() [3]float32 {
	return cell.background
}

// SetPenColor sets the penColor color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetPenColor(color [3]float32) {
	cell.pen = color
}

// GetPenColor returns the penColor color.
func (cell *Cell) GetPenColor() [3]float32 {
	return cell.pen
}

// SetTextColor sets the text color.
func (cell *Cell) SetTextColor(textColor [3]float32) {
	cell.textColor = textColor
}

// GetTextColor returns the text color.
// @return the brushColor color.
func (cell *Cell) GetTextColor() [3]float32 {
	return cell.textColor
}

// SetColSpan sets the column span func (cell *Cell) variable.
// @param colspan the specified column span value.
func (cell *Cell) SetColSpan(colspan int) {
	cell.colspan = colspan
}

// GetColSpan returns the column span func (cell *Cell) variable value.
// @return the column span value.
func (cell *Cell) GetColSpan() int {
	return cell.colspan
}

// SetTopBorder sets the cell border object.
// @param border the border object.
func (cell *Cell) SetTopBorder(topBorder bool) {
	cell.topBorder = topBorder
}

// GetTopBorder returns the cell border object.
func (cell *Cell) GetTopBorder() bool {
	return cell.topBorder
}

func (cell *Cell) SetBottomBorder(bottomBorder bool) {
	cell.bottomBorder = bottomBorder
}

func (cell *Cell) GetBottomBorder() bool {
	return cell.bottomBorder
}

func (cell *Cell) SetLeftBorder(leftBorder bool) {
	cell.leftBorder = leftBorder
}

func (cell *Cell) GetLeftBorder() bool {
	return cell.leftBorder
}

func (cell *Cell) SetRightBorder(rightBorder bool) {
	cell.rightBorder = rightBorder
}

func (cell *Cell) GetRightBorder() bool {
	return cell.rightBorder
}

// SetTextAlignment sets the cell text alignment.
// @param alignment the alignment code.
// Supported values: align.Left, align.Right and align.Center
func (cell *Cell) SetTextAlignment(textAlignment int) {
	cell.textAlignment = textAlignment
}

// GetTextAlignment returns the text alignment.
// @return the text horizontal alignment code.
func (cell *Cell) GetTextAlignment() int {
	return cell.textAlignment
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
	cell.underline = underline
}

// GetUnderline returns the underline text parameter.
// @return the underline text parameter.
func (cell *Cell) GetUnderline() bool {
	return cell.underline
}

// SetStrikeout sets the strikeout text parameter.
// @param strikeout the strikeout text parameter.
func (cell *Cell) SetStrikeout(strikeout bool) {
	cell.strikeout = strikeout
}

// GetStrikeout returns the strikeout text parameter.
// @return the strikeout text parameter.
func (cell *Cell) GetStrikeout() bool {
	return cell.strikeout
}

// SetURIAction sets the URI action.
func (cell *Cell) SetURIAction(uri string) {
	cell.uri = uri
}

// DrawOn draws the point, text and borders of this cell.
func (cell *Cell) DrawOn(page *Page, x, y, w, h float32) {
	if cell.hasBackground == true {
		cell.drawBackground(page, x, y, w, h)
	}

	if cell.text != "" {
		cell.DrawText(page, x, y, w, h)
	} else if cell.textBlock != nil {
		cell.textBlock.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textBlock.SetWidth(w - (cell.leftPadding + cell.rightPadding))
		cell.textBlock.DrawOn(page)
	} else if cell.image != nil {
		if cell.GetTextAlignment() == alignment.Left {
			cell.image.SetLocation(x+cell.leftPadding, y+cell.topPadding)
			cell.image.DrawOn(page)
		} else if cell.GetTextAlignment() == alignment.Center {
			cell.image.SetLocation((x+w/2.0)-cell.image.GetWidth()/2.0, y+cell.topPadding)
			cell.image.DrawOn(page)
		} else if cell.GetTextAlignment() == alignment.Right {
			cell.image.SetLocation((x+w)-(cell.image.GetWidth()+cell.leftPadding), y+cell.topPadding)
			cell.image.DrawOn(page)
		}
	} else if cell.barcode != nil {
		if cell.GetTextAlignment() == alignment.Left {
			cell.barcode.drawOnPageAtLocation(page, x+cell.leftPadding, y+cell.topPadding)
		} else if cell.GetTextAlignment() == alignment.Center {
			barcodeWidth := cell.barcode.DrawOn(nil)[0]
			cell.barcode.drawOnPageAtLocation(page, (x+w/2.0)-barcodeWidth/2.0, y+cell.topPadding)
		} else if cell.GetTextAlignment() == alignment.Right {
			barcodeWidth := cell.barcode.DrawOn(nil)[0]
			cell.barcode.drawOnPageAtLocation(page, (x+w)-(barcodeWidth+cell.leftPadding), y+cell.topPadding)
		}
	}

	cell.drawBorders(page, x, y, w, h)
	//if cell.point != nil {
	//	switch cell.point.align {
	//	case align.Left:
	//		cell.point.x = x + 2*cell.point.r
	//	case align.Right:
	//		cell.point.x = (x + w) - cell.rightPadding/2
	//	}
	//	cell.point.y = y + h/2
	//	page.SetBrushColor(cell.point.GetColor())
	//
	//	if cell.point.uri != nil {
	//		page.AddAnnotation(NewAnnotation(
	//			cell.point.uri,
	//			nil,
	//			cell.point.x-cell.point.r,
	//			cell.point.y-cell.point.r,
	//			cell.point.x+cell.point.r,
	//			cell.point.y+cell.point.r,
	//			"",
	//			"",
	//			""))
	//	}
	//
	//	page.DrawPoint(cell.point)
	//}
}

func (cell *Cell) drawBackground(page *Page, x, y, wCell, hCell float32) {
	page.SetBrushColorRGB(cell.background)
	page.FillRect(x, y+cell.lineWidth/2, wCell, hCell)
}

func (cell *Cell) drawBorders(page *Page, x, y, cellW, cellH float32) {
	page.SetPenColorRGB(cell.pen)
	page.SetPenWidth(cell.lineWidth)
	qWidth := cell.lineWidth / 4.0
	if cell.topBorder {
		page.MoveTo(x-qWidth, y)
		page.LineTo(x+cellW, y)
		page.StrokePath()
	}
	if cell.bottomBorder {
		page.MoveTo(x-qWidth, y+cellH)
		page.LineTo(x+cellW, y+cellH)
		page.StrokePath()
	}
	if cell.leftBorder {
		page.MoveTo(x, y-qWidth)
		page.LineTo(x, y+cellH+qWidth)
		page.StrokePath()
	}
	if cell.rightBorder {
		page.MoveTo(x+cellW, y-qWidth)
		page.LineTo(x+cellW, y+cellH+qWidth)
		page.StrokePath()
	}
}

// DrawText draws the cell text.
func (cell *Cell) DrawText(page *Page, x, y, wCell, hCell float32) {
	var xText float32
	var yText float32
	switch cell.valign {
	case alignment.Top:
		yText = y + cell.font.ascent + cell.topPadding
	case alignment.Center:
		yText = y + hCell/2.0 + cell.font.ascent/2.0
	case alignment.Bottom:
		yText = (y + hCell) - cell.bottomPadding
	default:
		log.Fatal("Invalid vertical text alignment option.")
	}

	page.SetPenColorRGB(cell.pen)
	if cell.GetTextAlignment() == alignment.Left {
		xText = x + cell.leftPadding
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.font.size, cell.text, xText, yText, cell.textColor, nil)
		if cell.underline {
			cell.UnderlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.StrikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else if cell.GetTextAlignment() == alignment.Right {
		xText = (x + wCell) - (cell.font.stringWidth(cell.font.size, cell.text) + cell.rightPadding)
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.font.size, cell.text, xText, yText, cell.textColor, nil)
		if cell.underline {
			cell.UnderlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.StrikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else if cell.GetTextAlignment() == alignment.Center {
		xText = x + cell.leftPadding +
			(((wCell - (cell.leftPadding + cell.rightPadding)) - cell.font.stringWidth(cell.font.size, cell.text)) / 2)
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.font.size, cell.text, xText, yText, cell.textColor, nil)
		if cell.underline {
			cell.UnderlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.StrikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else {
		log.Fatal("Invalid Text Alignment!")
	}

	//if cell.uri != nil || cell.key != nil {
	//	var w float32 = cell.font.stringWidth(cell.font.size, *cell.text)
	//	page.AddAnnotation(NewAnnotation(
	//		cell.uri,
	//		nil,
	//		xText,
	//		yText-cell.font.ascent,
	//		xText+w,
	//		yText+cell.font.descent,
	//		"",
	//		"",
	//		""))
	//}
}

// UnderlineText underlines the cell text.
func (cell *Cell) UnderlineText(page *Page, font *Font, text string, x, y float32) {
	page.SetPenWidth(font.underlineThickness)
	page.MoveTo(x, y+font.descent)
	page.LineTo(x+font.stringWidth(cell.font.size, text), y+font.descent)
	page.StrokePath()
}

// StrikeoutText strikes out the cell text.
func (cell *Cell) StrikeoutText(page *Page, font *Font, text string, x, y float32) {
	page.SetPenWidth(font.underlineThickness)
	page.MoveTo(x, y-font.ascent/3.0)
	page.LineTo(x+font.stringWidth(cell.font.size, text), y-font.ascent/3.0)
	page.StrokePath()
}
