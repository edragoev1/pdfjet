// cell.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
)

// Cell is used to create table cell objects.
// See the Table class for more information.
type Cell struct {
	font              *Font
	fallbackFont      *Font
	fontSize          float32
	text              string
	hasText           bool // Java's null text is a cell without text, which is 0 tall
	textBlock         *TextBlock
	textColumn        *TextColumn
	textBox           *TextBox
	compositeTextLine *CompositeTextLine
	image             *Image
	barcode           *Barcode
	point             *Point
	width             float32
	topPadding        float32
	bottomPadding     float32
	leftPadding       float32
	rightPadding      float32

	backgroundColor    [3]float32
	hasBackgroundColor bool
	strokeColor        [3]float32
	hasStrokeColor     bool
	strokeWidth        float32
	textColor          [3]float32

	colspan      int
	topBorder    bool
	bottomBorder bool
	leftBorder   bool
	rightBorder  bool

	textAlignment alignment.Alignment
	uri           string
	valign        alignment.Alignment

	underline bool
	strikeout bool
}

// NewCell creates a cell object and sets the font and the cell text.
// The font is also the fallback font until SetFallbackFont changes it.
// @param font the font.
// @param text the text.
func NewCell(font *Font, text string) *Cell {
	cell := new(Cell)
	cell.font = font
	cell.fallbackFont = font
	cell.fontSize = font.size
	cell.text = text
	cell.hasText = true
	cell.width = 75.0
	cell.colspan = 1
	cell.topPadding = 2.0
	cell.bottomPadding = 2.0
	cell.leftPadding = 2.0
	cell.rightPadding = 2.0
	// Java's Cell defaults its properties to 0x00050001 - only the top and
	// left borders are on.
	cell.topBorder = true
	cell.leftBorder = true
	cell.textAlignment = alignment.Left
	cell.valign = alignment.Top
	return cell
}

// SetFont sets the font for this cell. The font size does not change; set it with SetFontSize.
// @param font the font.
func (cell *Cell) SetFont(font *Font) *Cell {
	cell.font = font
	return cell
}

// GetFont returns the font used by this cell.
// @return the font.
func (cell *Cell) GetFont() *Font {
	return cell.font
}

// SetFallbackFont sets the fallback font for this cell.
// @param fallbackFont the fallback font.
func (cell *Cell) SetFallbackFont(fallbackFont *Font) *Cell {
	cell.fallbackFont = fallbackFont
	return cell
}

// GetFallbackFont returns the fallback font used by this cell.
// @return the fallback font.
func (cell *Cell) GetFallbackFont() *Font {
	return cell.fallbackFont
}

// SetText sets the cell text.
// @param text the cell text.
func (cell *Cell) SetText(text string) *Cell {
	cell.text = text
	cell.hasText = true
	return cell
}

// GetText returns the cell text.
func (cell *Cell) GetText() string {
	return cell.text
}

// SetFontSize sets the font size of the cell text.
func (cell *Cell) SetFontSize(fontSize float32) *Cell {
	cell.fontSize = fontSize
	return cell
}

// SetImage sets the image inside this cell and clears the cell text.
func (cell *Cell) SetImage(image *Image) *Cell {
	cell.image = image
	cell.text = ""
	cell.hasText = false
	return cell
}

// GetImage returns the cell image.
func (cell *Cell) GetImage() *Image {
	return cell.image
}

// SetBarcode sets the barcode inside this cell and clears the cell text.
func (cell *Cell) SetBarcode(barcode *Barcode) *Cell {
	cell.barcode = barcode
	cell.text = ""
	cell.hasText = false
	return cell
}

// GetBarcode returns the barcode drawn in this cell.
func (cell *Cell) GetBarcode() *Barcode {
	return cell.barcode
}

// SetMarker sets the marker drawn in this cell: a Point, placed at its left or
// right by its alignment and centered vertically.
// See the Point class and Example_09 for more information.
func (cell *Cell) SetMarker(point *Point) *Cell {
	cell.point = point
	return cell
}

// GetMarker returns the marker drawn in this cell.
func (cell *Cell) GetMarker() *Point {
	return cell.point
}

// SetTextBlock sets the text block drawn in this cell and clears the cell text.
func (cell *Cell) SetTextBlock(textBlock *TextBlock) *Cell {
	cell.textBlock = textBlock
	cell.text = ""
	cell.hasText = false
	return cell
}

// SetTextColumn sets the text column drawn in this cell, widens the cell to fit it
// and clears the cell text.
func (cell *Cell) SetTextColumn(textColumn *TextColumn) *Cell {
	cell.textColumn = textColumn
	cell.width = textColumn.w + cell.leftPadding + cell.rightPadding
	cell.text = ""
	cell.hasText = false
	return cell
}

// SetCompositeTextLine sets the composite text line that this cell holds.
func (cell *Cell) SetCompositeTextLine(compositeTextLine *CompositeTextLine) *Cell {
	cell.compositeTextLine = compositeTextLine
	return cell
}

// GetCompositeTextLine returns the composite text line that this cell holds.
func (cell *Cell) GetCompositeTextLine() *CompositeTextLine {
	return cell.compositeTextLine
}

// GetTextColumn returns the text column that this cell holds.
func (cell *Cell) GetTextColumn() *TextColumn {
	return cell.textColumn
}

// SetTextBox sets the text box drawn in this cell and clears the cell text.
func (cell *Cell) SetTextBox(textBox *TextBox) *Cell {
	cell.textBox = textBox
	cell.text = ""
	cell.hasText = false
	return cell
}

// GetTextBox returns the text box that this cell holds.
func (cell *Cell) GetTextBox() *TextBox {
	return cell.textBox
}

// GetTextBlock returns the text block drawn in this cell.
func (cell *Cell) GetTextBlock() *TextBlock {
	return cell.textBlock
}

// SetWidth sets the width of this cell.
// @param width the specified width.
func (cell *Cell) SetWidth(width float32) *Cell {
	cell.width = width
	if cell.textBox != nil {
		cell.textBox.SetWidth(cell.width - (cell.leftPadding + cell.rightPadding))
	} else if cell.textBlock != nil {
		cell.textBlock.SetWidth(cell.width - (cell.leftPadding + cell.rightPadding))
	}
	return cell
}

// GetWidth returns the cell width.
// @return the cell width.
func (cell *Cell) GetWidth() float32 {
	return cell.width
}

// SetTopPadding sets the top padding of this cell.
// @param padding the top padding.
func (cell *Cell) SetTopPadding(padding float32) *Cell {
	cell.topPadding = padding
	return cell
}

// GetTopPadding returns the top padding of this cell.
func (cell *Cell) GetTopPadding() float32 {
	return cell.topPadding
}

// SetBottomPadding sets the bottom padding of this cell.
// @param padding the bottom padding.
func (cell *Cell) SetBottomPadding(padding float32) *Cell {
	cell.bottomPadding = padding
	return cell
}

// GetBottomPadding returns the bottom padding of this cell.
func (cell *Cell) GetBottomPadding() float32 {
	return cell.bottomPadding
}

// SetLeftPadding sets the left padding of this cell.
// @param padding the left padding.
func (cell *Cell) SetLeftPadding(padding float32) *Cell {
	cell.leftPadding = padding
	return cell
}

// GetLeftPadding returns the left padding of this cell.
func (cell *Cell) GetLeftPadding() float32 {
	return cell.leftPadding
}

// SetRightPadding sets the right padding of this cell.
// @param padding the right padding.
func (cell *Cell) SetRightPadding(padding float32) *Cell {
	cell.rightPadding = padding
	return cell
}

// GetRightPadding returns the right padding of this cell.
func (cell *Cell) GetRightPadding() float32 {
	return cell.rightPadding
}

// SetPadding sets the top, bottom, left and right paddings of this cell.
// @param padding the right padding.
func (cell *Cell) SetPadding(padding float32) *Cell {
	cell.topPadding = padding
	cell.bottomPadding = padding
	cell.leftPadding = padding
	cell.rightPadding = padding
	return cell
}

// GetHeight returns the cell height.
// @return the cell height.
func (cell *Cell) GetHeight(width float32) float32 {
	cellHeight := float32(0.0)
	if cell.textBox != nil {
		cell.textBox.SetWidth(width)
		cellHeight = (cell.textBox.DrawOn(nil)[1] - cell.textBox.y) + cell.topPadding + cell.bottomPadding
	} else if cell.textBlock != nil {
		cell.textBlock.SetWidth(width)
		cellHeight = (cell.textBlock.DrawOn(nil)[1] - cell.textBlock.y) + cell.topPadding + cell.bottomPadding
	} else if cell.textColumn != nil {
		cellHeight = (cell.textColumn.DrawOn(nil)[1] - cell.textColumn.y) + cell.topPadding + cell.bottomPadding
	} else if cell.image != nil {
		cellHeight = cell.image.GetHeight() + cell.topPadding + cell.bottomPadding
	} else if cell.barcode != nil {
		cellHeight = cell.barcode.GetHeight() + cell.topPadding + cell.bottomPadding
	} else if cell.hasText {
		fontHeight := cell.font.GetBodyHeightAt(cell.fontSize)
		if cell.fallbackFont != nil && cell.fallbackFont.GetBodyHeightAt(cell.fontSize) > fontHeight {
			fontHeight = cell.fallbackFont.GetBodyHeightAt(cell.fontSize)
		}
		cellHeight = fontHeight + cell.topPadding + cell.bottomPadding
	}
	return cellHeight
}

// SetBackgroundColorRGB sets the background color from red, green and blue values.
func (cell *Cell) SetBackgroundColorRGB(color [3]float32) *Cell {
	cell.backgroundColor = color
	cell.hasBackgroundColor = true
	return cell
}

// SetBackgroundColor sets the background color of this cell as a 0xRRGGBB value.
// color.Transparent removes the background.
func (cell *Cell) SetBackgroundColor(c int32) *Cell {
	if c == color.Transparent {
		cell.backgroundColor = [3]float32{}
		cell.hasBackgroundColor = false
		return cell
	}
	cell.backgroundColor = colorToRGB(c)
	cell.hasBackgroundColor = true
	return cell
}

// GetBackgroundColor returns a copy of the background color of this cell, or nil if it has none.
func (cell *Cell) GetBackgroundColor() *[3]float32 {
	if !cell.hasBackgroundColor {
		return nil
	}
	backgroundColor := cell.backgroundColor
	return &backgroundColor
}

// SetBorderColorRGB sets the color of the cell borders from red, green and blue values.
func (cell *Cell) SetBorderColorRGB(color [3]float32) *Cell {
	cell.strokeColor = color
	cell.hasStrokeColor = true
	return cell
}

// SetBorderColor sets the color of the cell borders.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetBorderColor(color int32) *Cell {
	cell.strokeColor = colorToRGB(color)
	cell.hasStrokeColor = true
	return cell
}

// GetBorderColor returns a copy of the color of the cell borders, or nil if none was set.
func (cell *Cell) GetBorderColor() *[3]float32 {
	if !cell.hasStrokeColor {
		return nil
	}
	strokeColor := cell.strokeColor
	return &strokeColor
}

// SetBorderWidth sets the width of the cell borders.
// @param strokeWidth the width of the cell borders.
// @return this Cell object.
func (cell *Cell) SetBorderWidth(strokeWidth float32) *Cell {
	cell.strokeWidth = strokeWidth
	return cell
}

// GetBorderWidth returns the width of the cell borders.
// @return the width of the cell borders.
func (cell *Cell) GetBorderWidth() float32 {
	return cell.strokeWidth
}

// SetTextColorRGB sets the text color from red, green and blue values.
func (cell *Cell) SetTextColorRGB(textColor [3]float32) *Cell {
	cell.textColor = textColor
	return cell
}

// SetTextColor sets the text color.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetTextColor(color int32) *Cell {
	cell.textColor = colorToRGB(color)
	return cell
}

// GetTextColor returns the text color.
func (cell *Cell) GetTextColor() [3]float32 {
	return cell.textColor
}

// SetColSpan sets the number of columns this cell spans.
// @param colspan the specified column span value.
func (cell *Cell) SetColSpan(colspan int) *Cell {
	cell.colspan = colspan
	return cell
}

// GetColSpan returns the number of columns this cell spans.
// @return the column span value.
func (cell *Cell) GetColSpan() int {
	return cell.colspan
}

// SetBorder sets whether the specified borders are drawn.
// @param b the borders, for example border.Top | border.Bottom.
// @param visible true to draw the borders.
func (cell *Cell) SetBorder(b uint32, visible bool) *Cell {
	if b&border.Top != 0 {
		cell.topBorder = visible
	}
	if b&border.Bottom != 0 {
		cell.bottomBorder = visible
	}
	if b&border.Left != 0 {
		cell.leftBorder = visible
	}
	if b&border.Right != 0 {
		cell.rightBorder = visible
	}
	return cell
}

// GetBorder returns true if any of the specified borders is drawn.
// @param b the borders, for example border.Top.
func (cell *Cell) GetBorder(b uint32) bool {
	return (b&border.Top != 0 && cell.topBorder) ||
		(b&border.Bottom != 0 && cell.bottomBorder) ||
		(b&border.Left != 0 && cell.leftBorder) ||
		(b&border.Right != 0 && cell.rightBorder)
}

// SetBorders sets whether all four borders of this cell are drawn.
func (cell *Cell) SetBorders(visible bool) *Cell {
	return cell.SetBorder(border.All, visible)
}

// SetTextAlignment sets the cell text alignment.
// @param alignment the alignment code.
// Supported values: alignment.Left, alignment.Right, alignment.Center and
// alignment.Justify, which draws the single line of cell text left aligned.
func (cell *Cell) SetTextAlignment(textAlignment alignment.Alignment) *Cell {
	cell.textAlignment = textAlignment
	return cell
}

// GetTextAlignment returns the text alignment.
// @return the text horizontal alignment code.
func (cell *Cell) GetTextAlignment() alignment.Alignment {
	return cell.textAlignment
}

// SetVerticalAlignment sets the cell text vertical alignment.
// @param alignment the alignment code.
// Supported values: alignment.Top, alignment.Center and alignment.Bottom.
func (cell *Cell) SetVerticalAlignment(valign alignment.Alignment) *Cell {
	cell.valign = valign
	return cell
}

// GetVerticalAlignment returns the cell text vertical alignment.
// @return the vertical alignment code.
func (cell *Cell) GetVerticalAlignment() alignment.Alignment {
	return cell.valign
}

// SetUnderline sets the underline text parameter.
// If the value of the underline variable is 'true' - the text is underlined.
// @param underline the underline text parameter.
func (cell *Cell) SetUnderline(underline bool) *Cell {
	cell.underline = underline
	return cell
}

// GetUnderline returns the underline text parameter.
// @return the underline text parameter.
func (cell *Cell) GetUnderline() bool {
	return cell.underline
}

// SetStrikeout sets the strikeout text parameter.
// @param strikeout the strikeout text parameter.
func (cell *Cell) SetStrikeout(strikeout bool) *Cell {
	cell.strikeout = strikeout
	return cell
}

// GetStrikeout returns the strikeout text parameter.
// @return the strikeout text parameter.
func (cell *Cell) GetStrikeout() bool {
	return cell.strikeout
}

// SetURIAction sets the URI action.
func (cell *Cell) SetURIAction(uri string) *Cell {
	cell.uri = uri
	return cell
}

// drawOn draws the point, text and borders of this cell.
func (cell *Cell) drawOn(page *Page, x, y, w, h float32) {
	if cell.hasBackgroundColor {
		cell.drawBackground(page, x, y, w, h)
	}

	if cell.text != "" {
		cell.drawText(page, x, y, w, h)
	} else if cell.textBox != nil {
		cell.textBox.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textBox.SetWidth(w - (cell.leftPadding + cell.rightPadding))
		cell.textBox.DrawOn(page)
	} else if cell.textBlock != nil {
		cell.textBlock.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textBlock.SetWidth(w - (cell.leftPadding + cell.rightPadding))
		cell.textBlock.DrawOn(page)
	} else if cell.textColumn != nil {
		cell.textColumn.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textColumn.DrawOn(page)
	} else if cell.image != nil {
		if cell.textAlignment == alignment.Right {
			cell.image.SetLocation((x+w)-(cell.image.GetWidth()+cell.rightPadding), y+cell.topPadding)
		} else if cell.textAlignment == alignment.Center {
			cell.image.SetLocation((x+w/2.0)-cell.image.GetWidth()/2.0, y+cell.topPadding)
		} else {
			cell.image.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		}
		cell.image.DrawOn(page)
	} else if cell.barcode != nil {
		if cell.textAlignment == alignment.Right {
			barcodeWidth := cell.barcode.DrawOn(nil)[0]
			cell.barcode.drawOnPageAtLocation(page, (x+w)-(barcodeWidth+cell.rightPadding), y+cell.topPadding)
		} else if cell.textAlignment == alignment.Center {
			barcodeWidth := cell.barcode.DrawOn(nil)[0]
			cell.barcode.drawOnPageAtLocation(page, (x+w/2.0)-barcodeWidth/2.0, y+cell.topPadding)
		} else {
			cell.barcode.drawOnPageAtLocation(page, x+cell.leftPadding, y+cell.topPadding)
		}
	}

	cell.drawBorders(page, x, y, w, h)
	if cell.point != nil {
		switch cell.point.align {
		case alignment.Left:
			cell.point.x = x + 2*cell.point.r
		case alignment.Right:
			cell.point.x = (x + w) - cell.rightPadding/2
		}
		cell.point.y = y + h/2
		if cell.point.hasFillColor {
			page.SetBrushColorRGB(cell.point.fillColor)
		}
		if cell.point.uri != "" {
			page.addAnnotation(&Annotation{
				annotationType: AnnotationLink,
				x1:             cell.point.x - cell.point.r,
				y1:             cell.point.y - cell.point.r,
				x2:             cell.point.x + cell.point.r,
				y2:             cell.point.y + cell.point.r,
				vertices:       nil,
				uri:            cell.point.uri,
			})
		}
		page.DrawPoint(cell.point)
	}
}

func (cell *Cell) drawBackground(page *Page, x, y, cellW, cellH float32) {
	page.AddArtifactBMC()
	page.SetBrushColorRGB(cell.backgroundColor)
	page.FillRect(x, y+cell.strokeWidth/2, cellW, cellH)
	page.AddEMC()
}

func (cell *Cell) drawBorders(page *Page, x, y, cellW, cellH float32) {
	page.AddArtifactBMC()
	if cell.hasStrokeColor {
		page.SetPenColorRGB(cell.strokeColor)
	}
	page.SetPenWidth(cell.strokeWidth)
	qWidth := cell.strokeWidth / 4.0
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
	page.AddEMC()
}

// drawText draws the cell text, or the composite text line, and its link.
func (cell *Cell) drawText(page *Page, x, y, cellW, cellH float32) {
	ascent := cell.font.GetAscentAt(cell.fontSize)
	var yText float32
	switch cell.valign {
	case alignment.Top:
		yText = y + ascent + cell.topPadding
	case alignment.Center:
		yText = y + cellH/2.0 + ascent/2.0
	case alignment.Bottom:
		yText = (y + cellH) - cell.bottomPadding
	default:
		panic("Invalid vertical text alignment option.")
	}

	if cell.hasStrokeColor {
		page.SetPenColorRGB(cell.strokeColor)
	}
	var xText float32
	if cell.textAlignment == alignment.Right {
		xText = (x + cellW) - (cell.getTextWidth() + cell.rightPadding)
	} else if cell.textAlignment == alignment.Center {
		xText = x + cell.leftPadding +
			(((cellW - (cell.leftPadding + cell.rightPadding)) - cell.getTextWidth()) / 2)
	} else {
		// alignment.Left, and alignment.Justify, which a single line of text cannot use.
		xText = x + cell.leftPadding
	}
	if cell.compositeTextLine == nil {
		page.AddBDC("P", "", cell.text, cell.text)
		page.DrawStringUsingHighlightColors(
			cell.font, cell.fallbackFont, cell.fontSize, cell.text, xText, yText, cell.textColor, nil)
		page.AddEMC()
		if cell.underline {
			cell.underlineText(page, xText, yText)
		}
		if cell.strikeout {
			cell.strikeoutText(page, xText, yText)
		}
	} else {
		cell.compositeTextLine.SetLocation(xText, yText)
		// The text lines of the composite mark their own text.
		cell.compositeTextLine.DrawOn(page)
	}

	if cell.uri != "" {
		page.addAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             xText,
			y1:             yText - ascent,
			x2:             xText + cell.getTextWidth(),
			y2:             yText + cell.font.GetDescentAt(cell.fontSize),
			vertices:       nil,
			uri:            cell.uri,
		})
	}
}

// getTextWidth returns the width of the composite text line, or of the cell
// text drawn with the font and the fallback font at the font size of this cell.
func (cell *Cell) getTextWidth() float32 {
	if cell.compositeTextLine != nil {
		return cell.compositeTextLine.GetWidth()
	}
	return cell.font.StringWidthFB(cell.fallbackFont, cell.fontSize, cell.text)
}

// underlineText underlines the cell text.
func (cell *Cell) underlineText(page *Page, x, y float32) {
	descent := cell.font.GetDescentAt(cell.fontSize)
	page.AddBDC("P", "", "underline", "underline")
	page.SetPenWidth(cell.font.GetUnderlineThicknessAt(cell.fontSize))
	page.MoveTo(x, y+descent)
	page.LineTo(x+cell.getTextWidth(), y+descent)
	page.StrokePath()
	page.AddEMC()
}

// strikeoutText strikes out the cell text.
func (cell *Cell) strikeoutText(page *Page, x, y float32) {
	ascent := cell.font.GetAscentAt(cell.fontSize)
	page.AddBDC("P", "", "strike out", "strike out")
	page.SetPenWidth(cell.font.GetUnderlineThicknessAt(cell.fontSize))
	page.MoveTo(x, y-ascent/3.0)
	page.LineTo(x+cell.getTextWidth(), y-ascent/3.0)
	page.StrokePath()
	page.AddEMC()
}
