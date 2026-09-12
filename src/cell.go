// cell.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"log"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
)

// Cell is used to create table cell objects.
// See the Table class for more information.
type Cell struct {
	font              *Font
	fallbackFont      *Font
	fontSize          float32
	text              string
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
	lineWidth         float32

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
	cell.fontSize = font.size
	cell.text = text
	cell.width = 75.0
	cell.colspan = 1
	cell.topPadding = 2.0
	cell.bottomPadding = 2.0
	cell.leftPadding = 2.0
	cell.rightPadding = 2.0
	cell.lineWidth = 0.0
	// Java's Cell defaults its properties to 0x00050001 - only the top and
	// left borders are on.
	cell.topBorder = true
	cell.leftBorder = true
	cell.textAlignment = alignment.Left
	cell.valign = alignment.Top
	return cell
}

// SetFont sets the font for this cell.
// @param font the font.
func (cell *Cell) SetFont(font *Font) *Cell {
	cell.font = font
	cell.fontSize = font.size
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

// SetImage sets the image inside this cell.
func (cell *Cell) SetImage(image *Image) *Cell {
	cell.image = image
	cell.text = ""
	return cell
}

// GetImage returns the cell image.
func (cell *Cell) GetImage() *Image {
	return cell.image
}

// SetBarcode sets the barcode for this cell.
func (cell *Cell) SetBarcode(barcode *Barcode) *Cell {
	cell.barcode = barcode
	cell.text = ""
	return cell
}

// GetBarcode returns the barcode drawn in this cell.
func (cell *Cell) GetBarcode() *Barcode {
	return cell.barcode
}

// SetPoint sets the point inside this cell.
// See the Point class and Example_09 for more information.
func (cell *Cell) SetPoint(point *Point) *Cell {
	cell.point = point
	return cell
}

// GetPoint returns the cell point.
func (cell *Cell) GetPoint() *Point {
	return cell.point
}

// SetTextBlock sets the composite text object.
func (cell *Cell) SetTextBlock(textBlock *TextBlock) *Cell {
	cell.textBlock = textBlock
	return cell
}

// SetTextColumn sets the text column that this cell holds.
func (cell *Cell) SetTextColumn(textColumn *TextColumn) *Cell {
	cell.textColumn = textColumn
	cell.width = textColumn.w + cell.leftPadding + cell.rightPadding
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

// SetTextBox sets the text box that this cell holds.
func (cell *Cell) SetTextBox(textBox *TextBox) *Cell {
	cell.textBox = textBox
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
	} else if cell.textColumn != nil {
		cellHeight = (cell.textColumn.DrawOn(nil)[1] - cell.textColumn.y) + cell.topPadding + cell.bottomPadding
	} else if cell.textBlock != nil {
		cell.textBlock.SetWidth(width)
		cellHeight = (cell.textBlock.DrawOn(nil)[1] - cell.textBlock.y) + cell.topPadding + cell.bottomPadding
	} else if cell.image != nil {
		cellHeight = cell.image.GetHeight() + cell.topPadding + cell.bottomPadding
	} else if cell.barcode != nil {
		cellHeight = cell.barcode.GetHeight() + cell.topPadding + cell.bottomPadding
	} else {
		fontHeight := cell.font.GetHeight()
		if cell.fallbackFont != nil && cell.fallbackFont.GetHeight() > fontHeight {
			fontHeight = cell.fallbackFont.GetHeight()
		}
		cellHeight = fontHeight + cell.topPadding + cell.bottomPadding
	}
	return cellHeight
}

// SetLineWidth sets the border width.
func (cell *Cell) SetLineWidth(lineWidth float32) *Cell {
	cell.lineWidth = lineWidth
	return cell
}

// GetLineWidth returns the border width.
func (cell *Cell) GetLineWidth() float32 {
	return cell.lineWidth
}

// SetBackgroundColorRGB sets the background color from red, green and blue values.
func (cell *Cell) SetBackgroundColorRGB(color [3]float32) *Cell {
	cell.backgroundColor = color
	cell.hasBackgroundColor = true
	return cell
}

// SetBackgroundColor sets the background color of this cell as a 0xRRGGBB value.
func (cell *Cell) SetBackgroundColor(color int32) *Cell {
	r := float32((color>>16)&0xff) / 255.0
	g := float32((color>>8)&0xff) / 255.0
	b := float32((color)&0xff) / 255.0
	cell.backgroundColor = [3]float32{r, g, b}
	cell.hasBackgroundColor = true
	return cell
}

// GetBackgroundColor returns the background color of this cell.
func (cell *Cell) GetBackgroundColor() [3]float32 {
	return cell.backgroundColor
}

// SetStrokeColorRGB sets the color of the cell borders from red, green and blue values.
func (cell *Cell) SetStrokeColorRGB(color [3]float32) *Cell {
	cell.strokeColor = color
	cell.hasStrokeColor = true
	return cell
}

// SetStrokeColor sets the color of the cell borders.
// @param color the color specified as 0xRRGGBB integer.
func (cell *Cell) SetStrokeColor(color int32) *Cell {
	cell.strokeColor = colorToRGB(color)
	cell.hasStrokeColor = true
	return cell
}

// GetStrokeColor returns the color of the cell borders.
func (cell *Cell) GetStrokeColor() [3]float32 {
	return cell.strokeColor
}

// SetStrokeWidth sets the stroke width.
func (cell *Cell) SetStrokeWidth(strokeWidth float32) *Cell {
	cell.strokeWidth = strokeWidth
	return cell
}

// GetStrokeWidth returns the stroke width.
func (cell *Cell) GetStrokeWidth() float32 {
	return cell.strokeWidth
}

// SetTextColorRGB sets the text color.
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
// @return the brushColor color.
func (cell *Cell) GetTextColor() [3]float32 {
	return cell.textColor
}

// SetColSpan sets the column span func (cell *Cell) variable.
// @param colspan the specified column span value.
func (cell *Cell) SetColSpan(colspan int) *Cell {
	cell.colspan = colspan
	return cell
}

// GetColSpan returns the column span func (cell *Cell) variable value.
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

// SetTopBorder sets whether the top border of this cell is drawn.
func (cell *Cell) SetTopBorder(topBorder bool) *Cell {
	cell.topBorder = topBorder
	return cell
}

// GetTopBorder returns true if the top border of this cell is drawn.
func (cell *Cell) GetTopBorder() bool {
	return cell.topBorder
}

// SetBottomBorder sets whether the bottom border of this cell is drawn.
func (cell *Cell) SetBottomBorder(bottomBorder bool) *Cell {
	cell.bottomBorder = bottomBorder
	return cell
}

// GetBottomBorder returns true if the bottom border of this cell is drawn.
func (cell *Cell) GetBottomBorder() bool {
	return cell.bottomBorder
}

// SetLeftBorder sets whether the left border of this cell is drawn.
func (cell *Cell) SetLeftBorder(leftBorder bool) *Cell {
	cell.leftBorder = leftBorder
	return cell
}

// GetLeftBorder returns true if the left border of this cell is drawn.
func (cell *Cell) GetLeftBorder() bool {
	return cell.leftBorder
}

// SetRightBorder sets whether the right border of this cell is drawn.
func (cell *Cell) SetRightBorder(rightBorder bool) *Cell {
	cell.rightBorder = rightBorder
	return cell
}

// GetRightBorder returns true if the right border of this cell is drawn.
func (cell *Cell) GetRightBorder() bool {
	return cell.rightBorder
}

// SetTextAlignment sets the cell text alignment.
// @param alignment the alignment code.
// Supported values: align.Left, align.Right and align.Center
func (cell *Cell) SetTextAlignment(textAlignment int) *Cell {
	cell.textAlignment = textAlignment
	return cell
}

// GetTextAlignment returns the text alignment.
// @return the text horizontal alignment code.
func (cell *Cell) GetTextAlignment() int {
	return cell.textAlignment
}

// SetVerTextAlignment sets the cell text vertical alignment.
// @param alignment the alignment code.
// Supported values: align.Top, align.Center and align.Bottom
func (cell *Cell) SetVerTextAlignment(alignment int) *Cell {
	cell.valign = alignment
	return cell
}

// GetVerTextAlignment returns the cell text vertical alignment.
// @return the vertical alignment code.
func (cell *Cell) GetVerTextAlignment() int {
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
		// Java checks the text first, so a cell that carries both text and a
		// text box, column or block renders its text.
		cell.drawText(page, x, y, w, h)
	} else if cell.textBox != nil {
		cell.textBox.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textBox.SetWidth(w - (cell.leftPadding + cell.rightPadding))
		cell.textBox.DrawOn(page)
	} else if cell.textColumn != nil {
		cell.textColumn.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		cell.textColumn.DrawOn(page)
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
	} else {
		cell.drawText(page, x, y, w, h)
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

func (cell *Cell) drawBackground(page *Page, x, y, wCell, hCell float32) {
	page.AddArtifactBMC()
	page.SetBrushColorRGB(cell.backgroundColor)
	page.FillRect(x, y+cell.lineWidth/2, wCell, hCell)
	page.AddEMC()
}

func (cell *Cell) drawBorders(page *Page, x, y, cellW, cellH float32) {
	page.AddArtifactBMC()
	if cell.hasStrokeColor {
		page.SetPenColorRGB(cell.strokeColor)
	}
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
	page.AddEMC()
}

// drawText draws the cell text.
func (cell *Cell) drawText(page *Page, x, y, wCell, hCell float32) {
	var xText float32
	var yText float32
	ascent := cell.font.GetAscentAt(cell.fontSize)
	switch cell.valign {
	case alignment.Top:
		yText = y + ascent + cell.topPadding
	case alignment.Center:
		yText = y + hCell/2.0 + ascent/2.0
	case alignment.Bottom:
		yText = (y + hCell) - cell.bottomPadding
	default:
		log.Fatal("Invalid vertical text alignment option.")
	}

	if cell.hasStrokeColor {
		page.SetPenColorRGB(cell.strokeColor)
	}
	if cell.GetTextAlignment() == alignment.Left {
		xText = x + cell.leftPadding
		if cell.compositeTextLine != nil {
			cell.compositeTextLine.SetLocation(xText, yText)
			// The text lines of the composite mark their own text.
			cell.compositeTextLine.DrawOn(page)
			return
		}
		page.AddBMC("P", "", cell.text, cell.text)
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.fontSize, cell.text, xText, yText, cell.textColor, nil)
		page.AddEMC()
		if cell.underline {
			cell.underlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.strikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else if cell.GetTextAlignment() == alignment.Right {
		if cell.compositeTextLine != nil {
			xText = (x + wCell) - (cell.compositeTextLine.GetWidth() + cell.rightPadding)
			cell.compositeTextLine.SetLocation(xText, yText)
			// The text lines of the composite mark their own text.
			cell.compositeTextLine.DrawOn(page)
			return
		}
		xText = (x + wCell) - (cell.font.StringWidth(cell.fontSize, cell.text) + cell.rightPadding)
		page.AddBMC("P", "", cell.text, cell.text)
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.fontSize, cell.text, xText, yText, cell.textColor, nil)
		page.AddEMC()
		if cell.underline {
			cell.underlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.strikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else if cell.GetTextAlignment() == alignment.Center {
		if cell.compositeTextLine != nil {
			xText = x + cell.leftPadding +
				(((wCell - (cell.leftPadding + cell.rightPadding)) - cell.compositeTextLine.GetWidth()) / 2)
			cell.compositeTextLine.SetLocation(xText, yText)
			// The text lines of the composite mark their own text.
			cell.compositeTextLine.DrawOn(page)
			return
		}
		xText = x + cell.leftPadding +
			(((wCell - (cell.leftPadding + cell.rightPadding)) - cell.font.StringWidth(cell.fontSize, cell.text)) / 2)
		page.AddBMC("P", "", cell.text, cell.text)
		page.DrawStringUsingColorMap(
			cell.font, cell.fallbackFont, cell.fontSize, cell.text, xText, yText, cell.textColor, nil)
		page.AddEMC()
		if cell.underline {
			cell.underlineText(page, cell.font, cell.text, xText, yText)
		}
		if cell.strikeout {
			cell.strikeoutText(page, cell.font, cell.text, xText, yText)
		}
	} else {
		log.Fatal("Invalid Text Alignment!")
	}

	if cell.uri != "" {
		w := cell.font.StringWidth(cell.fontSize, cell.text)
		page.addAnnotation(&Annotation{
			annotationType: AnnotationLink,
			x1:             xText,
			y1:             yText - ascent,
			x2:             xText + w,
			y2:             yText + cell.font.GetDescentAt(cell.fontSize),
			vertices:       nil,
			fillColor:      [3]float32{1.0, 1.0, 1.0}, // White color
			transparency:   0.0,
			title:          "",
			contents:       "",
			uri:            cell.uri,
			key:            "",
			language:       "",
			actualText:     "",
			altDescription: "",
		})
	}
}

// underlineText underlines the cell text.
func (cell *Cell) underlineText(page *Page, font *Font, text string, x, y float32) {
	descent := font.GetDescentAt(cell.fontSize)
	page.SetPenWidth(font.GetUnderlineThicknessAt(cell.fontSize))
	page.MoveTo(x, y+descent)
	page.LineTo(x+font.StringWidth(cell.fontSize, text), y+descent)
	page.StrokePath()
}

// strikeoutText strikes out the cell text.
func (cell *Cell) strikeoutText(page *Page, font *Font, text string, x, y float32) {
	ascent := font.GetAscentAt(cell.fontSize)
	page.SetPenWidth(font.GetUnderlineThicknessAt(cell.fontSize))
	page.MoveTo(x, y-ascent/3.0)
	page.LineTo(x+font.StringWidth(cell.fontSize, text), y-ascent/3.0)
	page.StrokePath()
}
