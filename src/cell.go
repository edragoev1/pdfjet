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
	font            *Font
	fallbackFont    *Font
	fontSize        float32
	text            string
	drawable        Drawable // The image, barcode, text block, text column or other drawable
	point           *Point
	markerAlignment alignment.Alignment
	width           float32
	topPadding      float32
	bottomPadding   float32
	leftPadding     float32
	rightPadding    float32

	// The colors are packed 0xRRGGBB values, 4 bytes each instead of a
	// [3]float32 for every cell; color.Transparent marks a background or a
	// border that is not set, and the text color is black until it is set.
	backgroundColor int32
	textColor       int32
	borderWidth     float32
	borderColor     int32

	colspan int
	// The borders and the underline and strikeout of the text are the bits of
	// one uint32, 4 bytes instead of 6 bools and the padding they need. The
	// four borders are the bits the border package gives them; only the top
	// and the left are drawn unless SetBorder says otherwise.
	properties uint32

	textAlignment alignment.Alignment
	uri           string
	valign        alignment.Alignment

	// Last, so that the byte it needs comes out of the padding of the struct.
	hasText bool // Java's null text is a cell without text, which is 0 tall
}

// The bits of Cell.properties that are not the borders of the cell.
const (
	cellUnderline uint32 = 0x00100000
	cellStrikeout uint32 = 0x00200000
)

// NewEmptyCell creates a cell without text, like Cell(font) in the other ports.
// It is 0 tall until it gets text, an image or a barcode.
func NewEmptyCell(font *Font) *Cell {
	cell := NewCell(font, "")
	cell.hasText = false
	return cell
}

// NewCell creates a cell object and sets the font and the cell text.
// The font is also the fallback font until SetFallbackFont changes it.
//   - font: the font.
//   - text: the text.
func NewCell(font *Font, text string) *Cell {
	cell := new(Cell)
	cell.markerAlignment = alignment.Right
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
	cell.backgroundColor = color.Transparent
	cell.borderColor = color.Transparent
	cell.textColor = color.Black
	cell.properties = border.Top | border.Left
	cell.textAlignment = alignment.Left
	cell.valign = alignment.Top
	return cell
}

// SetFont sets the font for this cell. The font size does not change; set it with SetFontSize.
// The fallback font changes with the font, unless a different fallback font was set.
//   - font: the font.
func (cell *Cell) SetFont(font *Font) *Cell {
	if cell.fallbackFont == cell.font {
		cell.fallbackFont = font
	}
	cell.font = font
	return cell
}

// GetFont returns the font used by this cell.
// Returns the font.
func (cell *Cell) GetFont() *Font {
	return cell.font
}

// SetFallbackFont sets the fallback font for this cell.
//   - fallbackFont: the fallback font.
func (cell *Cell) SetFallbackFont(fallbackFont *Font) *Cell {
	cell.fallbackFont = fallbackFont
	return cell
}

// GetFallbackFont returns the fallback font used by this cell.
// Returns the fallback font.
func (cell *Cell) GetFallbackFont() *Font {
	return cell.fallbackFont
}

// SetText sets the cell text.
//   - text: the cell text.
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

// SetDrawable sets the drawable inside this cell and clears the cell text. A
// cell holds one drawable, so this replaces the image, barcode, text block or
// text column set before. The drawable is placed by its top left corner, at the
// padding, and aligned in the cell as the text is; it is measured with
// DrawOn(nil). A text block gets the width of the cell.
//   - drawable: the drawable, for example a QRCode, an SVGImage or a Table.
func (cell *Cell) SetDrawable(drawable Drawable) *Cell {
	cell.drawable = drawable
	cell.text = ""
	cell.hasText = false
	return cell
}

// GetDrawable returns the drawable inside this cell, or nil.
func (cell *Cell) GetDrawable() Drawable {
	return cell.drawable
}

// SetImage sets the image inside this cell and clears the cell text.
func (cell *Cell) SetImage(image *Image) *Cell {
	if image == nil {
		return cell.SetDrawable(nil)
	}
	return cell.SetDrawable(image)
}

// GetImage returns the cell image, or nil when the cell holds none.
func (cell *Cell) GetImage() *Image {
	image, _ := cell.drawable.(*Image)
	return image
}

// SetBarcode sets the barcode inside this cell and clears the cell text.
func (cell *Cell) SetBarcode(barcode *Barcode) *Cell {
	if barcode == nil {
		return cell.SetDrawable(nil)
	}
	return cell.SetDrawable(barcode)
}

// GetBarcode returns the barcode drawn in this cell, or nil when the cell holds
// none.
func (cell *Cell) GetBarcode() *Barcode {
	barcode, _ := cell.drawable.(*Barcode)
	return barcode
}

// SetMarker sets the marker drawn in this cell: a Point, placed at the left or
// the right of the cell and centered vertically.
// See the Point class and Example_09 for more information.
//   - point: the point.
//   - align: alignment.Left or alignment.Right.
func (cell *Cell) SetMarker(point *Point, align alignment.Alignment) *Cell {
	cell.point = point
	cell.markerAlignment = align
	return cell
}

// GetMarker returns the marker drawn in this cell.
func (cell *Cell) GetMarker() *Point {
	return cell.point
}

// SetTextBlock sets the text block drawn in this cell and clears the cell text.
func (cell *Cell) SetTextBlock(textBlock *TextBlock) *Cell {
	if textBlock == nil {
		return cell.SetDrawable(nil)
	}
	return cell.SetDrawable(textBlock)
}

// SetTextColumn sets the text column drawn in this cell, widens the cell to fit it
// and clears the cell text.
func (cell *Cell) SetTextColumn(textColumn *TextColumn) *Cell {
	cell.width = textColumn.w + cell.leftPadding + cell.rightPadding
	return cell.SetDrawable(textColumn)
}

// SetCompositeTextLine sets the composite text line that this cell holds.
func (cell *Cell) SetCompositeTextLine(compositeTextLine *CompositeTextLine) *Cell {
	// A composite text line is the content of the cell, in the drawable it
	// holds, and is drawn where the cell text would be. The cell text is left
	// as it is, and the composite is drawn instead of it.
	cell.drawable = compositeTextLine
	return cell
}

// GetCompositeTextLine returns the composite text line that this cell holds.
func (cell *Cell) GetCompositeTextLine() *CompositeTextLine {
	if composite, ok := cell.drawable.(*CompositeTextLine); ok {
		return composite
	}
	return nil
}

// GetTextColumn returns the text column that this cell holds, or nil when it
// holds none.
func (cell *Cell) GetTextColumn() *TextColumn {
	textColumn, _ := cell.drawable.(*TextColumn)
	return textColumn
}

// GetTextBlock returns the text block drawn in this cell, or nil when it holds
// none.
func (cell *Cell) GetTextBlock() *TextBlock {
	textBlock, _ := cell.drawable.(*TextBlock)
	return textBlock
}

// SetWidth sets the width of this cell.
//   - width: the specified width.
func (cell *Cell) SetWidth(width float32) *Cell {
	cell.width = width
	if textBlock, ok := cell.drawable.(*TextBlock); ok {
		textBlock.SetWidth(cell.width - (cell.leftPadding + cell.rightPadding))
	}
	return cell
}

// GetWidth returns the cell width.
// Returns the cell width.
func (cell *Cell) GetWidth() float32 {
	return cell.width
}

// SetTopPadding sets the top padding of this cell.
//   - padding: the top padding.
func (cell *Cell) SetTopPadding(padding float32) *Cell {
	cell.topPadding = padding
	return cell
}

// GetTopPadding returns the top padding of this cell.
func (cell *Cell) GetTopPadding() float32 {
	return cell.topPadding
}

// SetBottomPadding sets the bottom padding of this cell.
//   - padding: the bottom padding.
func (cell *Cell) SetBottomPadding(padding float32) *Cell {
	cell.bottomPadding = padding
	return cell
}

// GetBottomPadding returns the bottom padding of this cell.
func (cell *Cell) GetBottomPadding() float32 {
	return cell.bottomPadding
}

// SetLeftPadding sets the left padding of this cell.
//   - padding: the left padding.
func (cell *Cell) SetLeftPadding(padding float32) *Cell {
	cell.leftPadding = padding
	return cell
}

// GetLeftPadding returns the left padding of this cell.
func (cell *Cell) GetLeftPadding() float32 {
	return cell.leftPadding
}

// SetRightPadding sets the right padding of this cell.
//   - padding: the right padding.
func (cell *Cell) SetRightPadding(padding float32) *Cell {
	cell.rightPadding = padding
	return cell
}

// GetRightPadding returns the right padding of this cell.
func (cell *Cell) GetRightPadding() float32 {
	return cell.rightPadding
}

// SetPadding sets the top, bottom, left and right paddings of this cell.
//   - padding: the right padding.
func (cell *Cell) SetPadding(padding float32) *Cell {
	cell.topPadding = padding
	cell.bottomPadding = padding
	cell.leftPadding = padding
	cell.rightPadding = padding
	return cell
}

// GetHeight returns the cell height.
// Returns the cell height.
func (cell *Cell) GetHeight(width float32) float32 {
	cellHeight := float32(0.0)
	if _, ok := cell.drawable.(BaselineDrawable); ok {
		// A line of text is drawn on the baseline of the cell text, so the
		// cell makes room for the ascent and the descent of both.
		cellHeight = cell.ascent() + cell.descent() + cell.topPadding + cell.bottomPadding
	} else if cell.text == "" && cell.drawable != nil { // The text is drawn first
		if textBlock, ok := cell.drawable.(*TextBlock); ok {
			textBlock.SetWidth(width)
		}
		cellHeight = measureDrawable(cell.drawable)[1] + cell.topPadding + cell.bottomPadding
	} else if cell.hasText {
		fontHeight := cell.font.GetBodyHeight(cell.fontSize)
		if cell.fallbackFont != nil && cell.fallbackFont.GetBodyHeight(cell.fontSize) > fontHeight {
			fontHeight = cell.fallbackFont.GetBodyHeight(cell.fontSize)
		}
		cellHeight = fontHeight + cell.topPadding + cell.bottomPadding
	}
	return cellHeight
}

// SetBackgroundColorRGB sets the background color from red, green and blue
// values. The cell keeps each component to the nearest of 256 steps.
func (cell *Cell) SetBackgroundColorRGB(rgbColor [3]float32) *Cell {
	cell.backgroundColor = rgbToColor(rgbColor)
	return cell
}

// SetBackgroundColor sets the background color of this cell as a 0xRRGGBB value.
// color.Transparent removes the background.
func (cell *Cell) SetBackgroundColor(c int32) *Cell {
	if c == color.Transparent {
		cell.backgroundColor = color.Transparent
		return cell
	}
	cell.backgroundColor = c & 0xFFFFFF
	return cell
}

// GetBackgroundColor returns a copy of the background color of this cell, or nil if it has none.
func (cell *Cell) GetBackgroundColor() *[3]float32 {
	if cell.backgroundColor == color.Transparent {
		return nil
	}
	backgroundColor := colorToRGB(cell.backgroundColor)
	return &backgroundColor
}

// SetBorderColorRGB sets the color of the cell borders from red, green and
// blue values. The cell keeps each component to the nearest of 256 steps.
func (cell *Cell) SetBorderColorRGB(rgbColor [3]float32) *Cell {
	cell.borderColor = rgbToColor(rgbColor)
	return cell
}

// SetBorderColor sets the color of the cell borders.
// color.Transparent leaves the borders the color of the pen the page draws with.
//   - c: the color specified as 0xRRGGBB integer.
func (cell *Cell) SetBorderColor(c int32) *Cell {
	if c == color.Transparent {
		cell.borderColor = color.Transparent
		return cell
	}
	cell.borderColor = c & 0xFFFFFF
	return cell
}

// GetBorderColor returns a copy of the color of the cell borders, or nil if none was set.
func (cell *Cell) GetBorderColor() *[3]float32 {
	if cell.borderColor == color.Transparent {
		return nil
	}
	borderColor := colorToRGB(cell.borderColor)
	return &borderColor
}

// SetBorderWidth sets the width of the cell borders.
//   - borderWidth: the width of the cell borders.
//
// Returns this Cell object.
func (cell *Cell) SetBorderWidth(borderWidth float32) *Cell {
	cell.borderWidth = borderWidth
	return cell
}

// GetBorderWidth returns the width of the cell borders.
// Returns the width of the cell borders.
func (cell *Cell) GetBorderWidth() float32 {
	return cell.borderWidth
}

// SetTextColorRGB sets the text color from red, green and blue values.
// The cell keeps each component to the nearest of 256 steps.
func (cell *Cell) SetTextColorRGB(textColor [3]float32) *Cell {
	cell.textColor = rgbToColor(textColor)
	return cell
}

// SetTextColor sets the text color. color.Transparent leaves the text color unchanged.
//   - c: the color specified as 0xRRGGBB integer.
func (cell *Cell) SetTextColor(c int32) *Cell {
	if c == color.Transparent {
		return cell
	}
	cell.textColor = c & 0xFFFFFF
	return cell
}

// GetTextColor returns the text color.
func (cell *Cell) GetTextColor() [3]float32 {
	return colorToRGB(cell.textColor)
}

// SetColSpan sets the number of columns this cell spans.
//   - colspan: the specified column span value.
func (cell *Cell) SetColSpan(colspan int) *Cell {
	cell.colspan = colspan
	return cell
}

// GetColSpan returns the number of columns this cell spans.
// Returns the column span value.
func (cell *Cell) GetColSpan() int {
	return cell.colspan
}

// SetBorder sets whether the specified borders are drawn.
//   - b: the borders, for example border.Top | border.Bottom.
//   - visible: true to draw the borders.
func (cell *Cell) SetBorder(b uint32, visible bool) *Cell {
	if visible {
		cell.properties |= b & border.All
	} else {
		cell.properties &= ^(b & border.All)
	}
	return cell
}

// GetBorder returns true if any of the specified borders is drawn.
//   - b: the borders, for example border.Top.
func (cell *Cell) GetBorder(b uint32) bool {
	return cell.properties&b&border.All != 0
}

// SetBorders sets whether all four borders of this cell are drawn.
func (cell *Cell) SetBorders(visible bool) *Cell {
	return cell.SetBorder(border.All, visible)
}

// SetTextAlignment sets the cell text alignment.
//   - alignment: the alignment code.
//
// Supported values: alignment.Left, alignment.Right, alignment.Center and
// alignment.Justify, which draws the single line of cell text left aligned.
func (cell *Cell) SetTextAlignment(textAlignment alignment.Alignment) *Cell {
	cell.textAlignment = textAlignment
	return cell
}

// GetTextAlignment returns the text alignment.
// Returns the text horizontal alignment code.
func (cell *Cell) GetTextAlignment() alignment.Alignment {
	return cell.textAlignment
}

// SetVerticalAlignment sets the cell text vertical alignment.
//   - alignment: the alignment code.
//
// Supported values: alignment.Top, alignment.Center and alignment.Bottom.
func (cell *Cell) SetVerticalAlignment(valign alignment.Alignment) *Cell {
	cell.valign = valign
	return cell
}

// GetVerticalAlignment returns the cell text vertical alignment.
// Returns the vertical alignment code.
func (cell *Cell) GetVerticalAlignment() alignment.Alignment {
	return cell.valign
}

// SetUnderline sets the underline text parameter.
// If the value of the underline variable is 'true' - the text is underlined.
//   - underline: the underline text parameter.
func (cell *Cell) SetUnderline(underline bool) *Cell {
	if underline {
		cell.properties |= cellUnderline
	} else {
		cell.properties &= ^cellUnderline
	}
	return cell
}

// GetUnderline returns the underline text parameter.
// Returns the underline text parameter.
func (cell *Cell) GetUnderline() bool {
	return cell.properties&cellUnderline != 0
}

// SetStrikeout sets the strikeout text parameter.
//   - strikeout: the strikeout text parameter.
func (cell *Cell) SetStrikeout(strikeout bool) *Cell {
	if strikeout {
		cell.properties |= cellStrikeout
	} else {
		cell.properties &= ^cellStrikeout
	}
	return cell
}

// GetStrikeout returns the strikeout text parameter.
// Returns the strikeout text parameter.
func (cell *Cell) GetStrikeout() bool {
	return cell.properties&cellStrikeout != 0
}

// SetURIAction sets the URI action.
func (cell *Cell) SetURIAction(uri string) *Cell {
	cell.uri = uri
	return cell
}

// measureDrawable returns the width and the height of the drawable: its corner
// when it is placed at 0, 0 and measured without a page.
func measureDrawable(drawable Drawable) [2]float32 {
	drawable.SetLocation(0, 0)
	return drawable.DrawOn(nil)
}

// drawOn draws the point, text and borders of this cell.
func (cell *Cell) drawOn(page *Page, x, y, w, h float32) {
	if cell.backgroundColor != color.Transparent {
		cell.drawBackground(page, x, y, w, h)
	}

	if _, ok := cell.drawable.(BaselineDrawable); ok || cell.text != "" {
		// A line of text is drawn instead of the cell text, on its baseline.
		cell.drawText(page, x, y, w, h)
	} else if textBlock, ok := cell.drawable.(*TextBlock); ok {
		textBlock.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		textBlock.SetWidth(w - (cell.leftPadding + cell.rightPadding))
		textBlock.DrawOn(page)
	} else if cell.drawable != nil {
		if cell.textAlignment == alignment.Right {
			drawableWidth := measureDrawable(cell.drawable)[0]
			cell.drawable.SetLocation((x+w)-(drawableWidth+cell.rightPadding), y+cell.topPadding)
		} else if cell.textAlignment == alignment.Center {
			drawableWidth := measureDrawable(cell.drawable)[0]
			cell.drawable.SetLocation((x+w/2.0)-drawableWidth/2.0, y+cell.topPadding)
		} else {
			cell.drawable.SetLocation(x+cell.leftPadding, y+cell.topPadding)
		}
		cell.drawable.DrawOn(page)
	}

	cell.drawBorders(page, x, y, w, h)
	if cell.point != nil {
		switch cell.markerAlignment {
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
			page.addAnnotation(&annotationObject{
				annotationType: annotationLink,
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
	page.SetBrushColor(cell.backgroundColor)
	page.FillRect(x, y+cell.borderWidth/2, cellW, cellH)
	page.AddEMC()
}

func (cell *Cell) drawBorders(page *Page, x, y, cellW, cellH float32) {
	if cell.properties&border.All == 0 {
		return // Nothing to draw, so nothing to write.
	}
	page.AddArtifactBMC()
	if cell.borderColor != color.Transparent {
		page.SetPenColor(cell.borderColor)
	}
	page.SetPenWidth(cell.borderWidth)
	// Half the pen width, so that the corners of the borders close.
	hWidth := cell.borderWidth / 2.0
	// The borders of a cell are the subpaths of one path, stroked once.
	if cell.properties&border.Top != 0 {
		page.MoveTo(x-hWidth, y)
		page.LineTo(x+cellW, y)
	}
	if cell.properties&border.Bottom != 0 {
		page.MoveTo(x-hWidth, y+cellH)
		page.LineTo(x+cellW, y+cellH)
	}
	if cell.properties&border.Left != 0 {
		page.MoveTo(x, y-hWidth)
		page.LineTo(x, y+cellH+hWidth)
	}
	if cell.properties&border.Right != 0 {
		page.MoveTo(x+cellW, y-hWidth)
		page.LineTo(x+cellW, y+cellH+hWidth)
	}
	page.StrokePath()
	page.AddEMC()
}

// drawText draws the cell text, or the composite text line, and its link.
func (cell *Cell) drawText(page *Page, x, y, cellW, cellH float32) {
	ascent := cell.ascent()
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
	line, hasLine := cell.drawable.(BaselineDrawable)
	if !hasLine {
		page.AddBDC("P", "", cell.text, cell.text)
		page.drawStringUsingHighlightColors(
			cell.font, cell.fallbackFont, cell.fontSize, cell.text, xText, yText, colorToRGB(cell.textColor), nil)
		page.AddEMC()
		if cell.properties&cellUnderline != 0 {
			cell.underlineText(page, xText, yText)
		}
		if cell.properties&cellStrikeout != 0 {
			cell.strikeoutText(page, xText, yText)
		}
	} else {
		// A text line and a composite text line mark their own text.
		line.SetLocation(xText, yText)
		line.DrawOn(page)
	}

	if cell.uri != "" {
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             xText,
			y1:             yText - ascent,
			x2:             xText + cell.getTextWidth(),
			y2:             yText + cell.descent(),
			vertices:       nil,
			uri:            cell.uri,
		})
	}
}

// getTextWidth returns the width of the composite text line, or of the cell
// text drawn with the font and the fallback font at the font size of this cell.
func (cell *Cell) getTextWidth() float32 {
	if line, ok := cell.drawable.(BaselineDrawable); ok {
		return line.GetWidth()
	}
	return cell.font.StringWidthUsingFallbackFont(cell.fallbackFont, cell.fontSize, cell.text)
}

// underlineText underlines the cell text.
func (cell *Cell) underlineText(page *Page, x, y float32) {
	descent := cell.font.GetDescent(cell.fontSize)
	page.AddBDC("P", "", "underline", "underline")
	page.SetPenColor(cell.textColor)
	page.SetPenWidth(cell.font.GetUnderlineThickness(cell.fontSize))
	page.MoveTo(x, y+descent)
	page.LineTo(x+cell.getTextWidth(), y+descent)
	page.StrokePath()
	page.AddEMC()
}

// strikeoutText strikes out the cell text.
func (cell *Cell) strikeoutText(page *Page, x, y float32) {
	ascent := cell.font.GetAscent(cell.fontSize)
	page.AddBDC("P", "", "strike out", "strike out")
	page.SetPenColor(cell.textColor)
	page.SetPenWidth(cell.font.GetUnderlineThickness(cell.fontSize))
	page.MoveTo(x, y-ascent/3.0)
	page.LineTo(x+cell.getTextWidth(), y-ascent/3.0)
	page.StrokePath()
	page.AddEMC()
}

// ascent returns how far above the baseline the cell draws: the ascent of its
// font, and of the line of text it holds, whichever reaches higher.
func (cell *Cell) ascent() float32 {
	ascent := cell.font.GetAscent(cell.fontSize)
	if line, ok := cell.drawable.(BaselineDrawable); ok {
		if lineAscent := line.GetAscent(); lineAscent > ascent {
			ascent = lineAscent
		}
	}
	return ascent
}

// descent returns how far below the baseline the cell draws: the descent of
// its font, and of the line of text it holds, whichever reaches lower.
func (cell *Cell) descent() float32 {
	descent := cell.font.GetDescent(cell.fontSize)
	if line, ok := cell.drawable.(BaselineDrawable); ok {
		if lineDescent := line.GetDescent(); lineDescent > descent {
			descent = lineDescent
		}
	}
	return descent
}
