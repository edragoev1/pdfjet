package pdfjet

/**
 * table.go
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
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/border"
)

// Table is used to create table objects and draw them on a page.
// Please see Example_08.
type Table struct {
	tableData       [][]*Cell
	rendered        int
	numOfHeaderRows int
	x1, y1          float32
	y1FirstPage     float32
	bottomMargin    float32
}

// Constants
const (
	TableWith0HeaderRows = iota
	TableWith1HeaderRows
	TableWith2HeaderRows
	TableWith3HeaderRows
	TableWith4HeaderRows
	TableWith5HeaderRows
	TableWith6HeaderRows
	TableWith7HeaderRows
	TableWith8HeaderRows
	TableWith9HeaderRows
)

// NewTable creates table objects.
func NewTable() *Table {
	table := new(Table)
	table.bottomMargin = 30.0
	return table
}

// SetLocation sets the location (x, y) of the top left corner of table on the page.
// @param x the x coordinate of the top left point of the table.
// @param y the y coordinate of the top left point of the table.
func (table *Table) SetLocation(x, y float32) {
	table.x1 = x
	table.y1 = y
}

// SetBottomMargin sets the bottom margin for table.
// @param bottomMargin the margin.
func (table *Table) SetBottomMargin(bottomMargin float32) {
	table.bottomMargin = bottomMargin
}

// SetData sets the table data and specifies the number of header rows in table data.
func (table *Table) SetData(tableData [][]*Cell, numOfHeaderRows int) {
	table.tableData = tableData
	table.numOfHeaderRows = numOfHeaderRows
	table.rendered = numOfHeaderRows

	numOfColumns := len(tableData[0])
	font := tableData[0][0].font
	for _, row := range tableData {
		diff := numOfColumns - len(row)
		for i := 0; i < diff; i++ {
			row = append(row, NewCell(font, ""))
		}
	}
}

// RightAlignNumbers sets the alignment of the numbers.
func (table *Table) RightAlignNumbers() {
	var buf strings.Builder
	for _, row := range table.tableData {
		for _, cell := range row {
			if cell.text != nil {
				buf.Reset()
				runes := []rune(*cell.text)
				var index1 = 0
				var index2 = len(runes)
				if len(runes) > 2 && runes[0] == '(' && runes[len(runes)-1] == ')' {
					index1 = 1
					index2 = len(runes) - 1
				}
				for i := index1; i < index2; i++ {
					ch := runes[i]
					if ch != '.' && ch != ',' && ch != '\'' {
						buf.WriteRune(ch)
					}
				}
				_, err := strconv.ParseFloat(buf.String(), 64)
				if err == nil {
					cell.SetTextAlignment(align.Right)
				}
			}
		}
	}
}

// RemoveLineBetweenRows removes the horizontal lines between the rows from index1 to index2.
func (table *Table) RemoveLineBetweenRows(index1, index2 int) {
	for i := index1; i < index2; i++ {
		row := table.tableData[i]
		for _, cell := range row {
			cell.SetBorder(border.Bottom, false)
		}
		row = table.tableData[i+1]
		for _, cell := range row {
			cell.SetBorder(border.Top, false)
		}
	}
}

// SetTextAlignInColumn sets the text alignment in the specified column.
//
// @param index the index of the specified column.
// @param alignment the specified alignment.
// Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.
func (table *Table) SetTextAlignInColumn(index, alignment int) {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetTextAlignment(alignment)
			if cell.textBox != nil {
				cell.textBox.SetTextAlignment(alignment)
			}
		}
	}
}

// SetTextColorInColumn sets the color of the text in the specified column.
// @param index the index of the specified column.
// @param color the color specified as an integer.
func (table *Table) SetTextColorInColumn(index int, color int32) {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetBrushColor(color)
			if cell.textBox != nil {
				cell.textBox.SetBrushColor(color)
			}
		}
	}
}

// SetFontInColumn sets the font for the specified column.
// @param index the column index.
// @param font the font.
func (table *Table) SetFontInColumn(index int, font *Font) {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.font = font
			if cell.textBox != nil {
				cell.textBox.font = font
			}
		}
	}
}

// SetTextColorInRow sets the color of the text in the specified row.
// @param index the index of the specified row.
// @param color the color specified as an integer.
func (table *Table) SetTextColorInRow(index, color int32) {
	row := table.tableData[index]
	for _, cell := range row {
		cell.SetBrushColor(color)
		if cell.textBox != nil {
			cell.textBox.SetBrushColor(color)
		}
	}
}

// SetFontInRow sets the font for the specified row.
// @param index the row index.
// @param font the font.
func (table *Table) SetFontInRow(index int, font *Font) {
	row := table.tableData[index]
	for _, cell := range row {
		cell.font = font
		if cell.textBox != nil {
			cell.textBox.font = font
		}
	}
}

// SetColumnWidth sets the width of the column with the specified index.
// @param index the index of specified column.
// @param width the specified width.
func (table *Table) SetColumnWidth(index int, width float32) {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetWidth(width)
			if cell.textBox != nil {
				cell.textBox.SetWidth(width - (cell.leftPadding + cell.rightPadding))
			}
		}
	}
}

// GetColumnWidth returns the column width of the column at the specified index.
// @param index the index of the column.
// @return the width of the column.
func (table *Table) GetColumnWidth(index int) float32 {
	cell := table.GetCellAtRowColumn(0, index)
	return cell.GetWidth()
}

// GetCellAt returns the cell at the specified row and column.
// @param row the specified row.
// @param col the specified column.
// @return the cell at the specified row and column.
func (table *Table) GetCellAt(rowIndex, colIndex int) *Cell {
	if rowIndex >= 0 {
		row := table.tableData[rowIndex]
		return row[colIndex]
	}
	return table.tableData[len(table.tableData)+rowIndex][colIndex]
}

// GetCellAtRowColumn returns the cell at the specified row and column.
// @param row the specified row.
// @param col the specified column.
// @return the cell at the specified row and column.
func (table *Table) GetCellAtRowColumn(rowIndex, colIndex int) *Cell {
	return table.GetCellAt(rowIndex, colIndex)
}

// GetRow returns a list of cell for the specified row.
// @param index the index of the specified row.
// @return the list of cells.
func (table *Table) GetRow(index int) []*Cell {
	return table.tableData[index]
}

// GetRowAtIndex returns the row at the specified index.
func (table *Table) GetRowAtIndex(index int) []*Cell {
	return table.GetRow(index)
}

// GetColumn returns a list of cell for the specified column.
// @param index the index of the specified column.
// @return the list of cells.
func (table *Table) GetColumn(index int) []*Cell {
	column := make([]*Cell, 0)
	for _, row := range table.tableData {
		if index < len(row) {
			column = append(column, row[index])
		}
	}
	return column
}

// GetColumnAtIndex return the column at the specified index.
func (table *Table) GetColumnAtIndex(index int) []*Cell {
	return table.GetColumn(index)
}

// DrawOn draws this table on the specified page.
// @param page the page to draw this table on.
// @return Point the point on the page where to draw the next component.
func (table *Table) DrawOn(page *Page) [2]float32 {
	table.wrapAroundCellText()
	table.setRightBorderOnLastColumn()
	table.setBottomBorderOnLastRow()
	return table.drawTableRows(page, table.drawHeaderRows(page))
}

// DrawOnPages draws the table on pdf pages with the specified size.
func (table *Table) DrawOnPages(pdf *PDF, pages *[]*Page, pageSize [2]float32) [2]float32 {
	table.wrapAroundCellText()
	table.setRightBorderOnLastColumn()
	table.setBottomBorderOnLastRow()
	var xy [2]float32
	for table.hasMoreData() {
		page := NewPageDetached(pdf, pageSize)
		*pages = append(*pages, page)
		xy = table.drawTableRows(page, table.drawHeaderRows(page))
	}
	return xy
}

/**
 *  Draws table table on the specified page.
 *  @param page the page to draw table table on.
 *  @return Point the point on the page where to draw the next component.
 */
func (table *Table) drawHeaderRows(page *Page) [2]float32 {
	x := table.x1
	y := table.y1
	for i := 0; i < table.numOfHeaderRows; i++ {
		row := table.tableData[i]
		h := table.getMaxCellHeight(row)
		for j := 0; j < len(row); j++ {
			cell := row[j]
			w := cell.GetWidth()
			colspan := cell.GetColSpan()
			for k := 1; k < colspan; k++ {
				j++
				w += row[j].width
			}
			if table != nil {
				page.SetBrushColor(cell.GetBrushColor())
				cell.DrawOn(page, x, y, w, h)
			}
			x += w
		}
		x = table.x1
		y += h
	}
	return [2]float32{x, y}
}

func (table *Table) drawTableRows(page *Page, xy [2]float32) [2]float32 {
	x := xy[0]
	y := xy[1]
	for i := table.rendered; i < len(table.tableData); i++ {
		row := table.tableData[table.rendered]
		h := table.getMaxCellHeight(row)
		if page != nil && (y+h) > (page.height-table.bottomMargin) {
			return [2]float32{x, y}
		}
		for i := 0; i < len(row); i++ {
			cell := row[i]
			w := cell.GetWidth()
			colspan := cell.GetColSpan()
			for j := 1; j < colspan; j++ {
				i++
				w += row[i].GetWidth()
			}
			if page != nil {
				page.SetBrushColor(cell.GetBrushColor())
				cell.DrawOn(page, x, y, w, h)
			}
			x += w
		}
		x = table.x1
		y += h
		table.rendered++
	}
	table.rendered = -1 // We are done!
	return [2]float32{x, y}
}

func (table *Table) getMaxCellHeight(row []*Cell) float32 {
	var maxCellHeight float32 = 0.0
	for _, cell := range row {
		if cell.GetHeight() > maxCellHeight {
			maxCellHeight = cell.GetHeight()
		}
	}
	return maxCellHeight
}

// HasMoreData returns true if the table contains more data that needs to be drawn on a page.
func (table *Table) hasMoreData() bool {
	return table.rendered != -1
}

// GetWidth returns the width of table table when drawn on a page.
// @return the widht of table table.
func (table *Table) GetWidth() float32 {
	var tableWidth float32
	row := table.tableData[0]
	for _, cell := range row {
		tableWidth += cell.GetWidth()
	}
	return tableWidth
}

// GetRowsRendered returns the number of data rows that have been rendered so far.
// @return the number of data rows that have been rendered so far.
func (table *Table) GetRowsRendered() int {
	if table.rendered != -1 {
		return table.rendered - table.numOfHeaderRows
	}
	return -1
}

// SetNoCellBorders sets all table cells borders to <strong>false</strong> or <strong>true</strong>.
func (table *Table) SetCellBorders(borders bool) {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetBorders(borders)
		}
	}
}

// SetCellBordersColor sets the color of the cell border lines.
// @param color the color of the cell border lines.
func (table *Table) SetCellBordersColor(color int32) {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetPenColor(color)
		}
	}
}

// SetCellBordersWidth sets the width of the cell border lines.
// @param width the width of the border lines.
func (table *Table) SetCellBordersWidth(width float32) {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetLineWidth(width)
		}
	}
}

// SetFirstPageTopMargin -- TODO:
func (table *Table) SetFirstPageTopMargin(topMargin float32) {
	table.y1FirstPage = table.y1 + topMargin
}

// Sets the right border on all cells in the last column.
func (table *Table) setRightBorderOnLastColumn() {
	for _, row := range table.tableData {
		var cell *Cell
		var i = 0
		for i < len(row) {
			cell = row[i]
			i += cell.GetColSpan()
		}
		cell.SetBorder(border.Right, true)
	}
}

// Sets the bottom border on all cells in the last row.
func (table *Table) setBottomBorderOnLastRow() {
	lastRow := table.tableData[len(table.tableData)-1]
	for _, cell := range lastRow {
		cell.SetBorder(border.Bottom, true)
	}
}

// Auto adjusts the widths of all columns so that they are just wide enough to
// hold the text without truncation.
func (table *Table) SetColumnWidths() {
	maxColWidths := []float32{}
	firstRow := table.tableData[0]
	for range firstRow {
		maxColWidths = append(maxColWidths, 0.0)
	}
	for _, row := range table.tableData {
		for i := 0; i < len(row); i++ {
			cell := row[i]
			if cell.GetColSpan() == 1 {
				if cell.textBox != nil {
					tokens := strings.Fields(cell.textBox.text)
					for _, token := range tokens {
						tokenWidth := cell.textBox.font.StringWidth(cell.textBox.fallbackFont, token)
						tokenWidth += cell.leftPadding + cell.rightPadding
						if tokenWidth > maxColWidths[i] {
							maxColWidths[i] = tokenWidth
						}
					}
				} else if cell.image != nil {
					imageWidth := cell.image.GetWidth() + cell.leftPadding + cell.rightPadding
					if imageWidth > maxColWidths[i] {
						maxColWidths[i] = imageWidth
					}
				} else if cell.barcode != nil {
					barcodeWidth := cell.barcode.DrawOn(nil)[0] + cell.leftPadding + cell.rightPadding
					if barcodeWidth > maxColWidths[i] {
						maxColWidths[i] = barcodeWidth
					}
				} else if cell.text != nil {
					textWidth := cell.font.StringWidth(cell.fallbackFont, *cell.text)
					textWidth += cell.leftPadding + cell.rightPadding
					if textWidth > maxColWidths[i] {
						maxColWidths[i] = textWidth
					}
				}
			}
		}
	}
	for _, row := range table.tableData {
		for i, cell := range row {
			cell.SetWidth(maxColWidths[i])
		}
	}
	for _, row := range table.tableData {
		for i, cell := range row {
			colspan := cell.GetColSpan()
			if colspan > 1 {
				if cell.textBox != nil {
					sumOfWidths := float32(0.0)
					for j := 0; j < colspan; j++ {
						sumOfWidths += row[i+j].GetWidth()
					}
					cell.textBox.SetWidth(sumOfWidths - (cell.leftPadding + cell.rightPadding))
				}
			}
		}
	}
}

func (table *Table) addExtraTableRows() [][]*Cell {
	tableData2 := make([][]*Cell, 0)
	for _, row := range table.tableData {
		tableData2 = append(tableData2, row) // Add the original row
		maxNumVerCells := 0
		for i := 0; i < len(row); i++ {
			numVerCells := getNumVerCells(row, i)
			if numVerCells > maxNumVerCells {
				maxNumVerCells = numVerCells
			}
		}
		for i := 1; i < maxNumVerCells; i++ {
			row2 := make([]*Cell, 0)
			for _, cell := range row {
				cell2 := NewEmptyCell(cell.GetFont())
				cell2.SetFallbackFont(cell.GetFallbackFont())
				cell2.SetWidth(cell.GetWidth())
				cell2.SetLeftPadding(cell.leftPadding)
				cell2.SetRightPadding(cell.rightPadding)
				cell2.SetLineWidth(cell.lineWidth)
				cell2.SetBgColor(cell.GetBgColor())
				cell2.SetPenColor(cell.GetPenColor())
				cell2.SetBrushColor(cell.GetBrushColor())
				cell2.SetProperties(cell.GetProperties())
				cell2.SetVerTextAlignment(cell.GetVerTextAlignment())
				cell2.SetTopPadding(0.0)
				cell2.SetBorder(border.Top, false)
				row2 = append(row2, cell2)
			}
			tableData2 = append(tableData2, row2)
		}
	}
	return tableData2
}

func getTotalWidth(row []*Cell, index int) float32 {
	cell := row[index]
	colspan := cell.GetColSpan()
	cellWidth := float32(0.0)
	for i := 0; i < colspan; i++ {
		cellWidth += row[index+i].GetWidth()
	}
	cellWidth -= (cell.leftPadding + row[index+(colspan-1)].rightPadding)
	return cellWidth
}

// WrapAroundCellText wraps around the text in all cells so it fits the column width.
// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
func (table *Table) wrapAroundCellText() {
	tableData2 := table.addExtraTableRows()
	for i := 0; i < len(tableData2); i++ {
		row := tableData2[i]
		for j := 0; j < len(row); j++ {
			cell := row[j]
			if cell.text != nil {
				cellWidth := getTotalWidth(row, j)
				tokens := strings.Fields(cell.GetText())
				var n = 0
				var buf strings.Builder
				for _, token := range tokens {
					if cell.font.StringWidth(cell.fallbackFont, token) > cellWidth {
						if len(buf.String()) > 0 {
							buf.WriteString(" ")
						}
						for _, ch := range token {
							if cell.font.StringWidth(cell.fallbackFont, strings.TrimSpace(buf.String()+" "+string(ch))) > cellWidth {
								tableData2[i+n][j].SetText(buf.String())
								buf.Reset()
								n++
							}
							buf.WriteRune(ch)
						}
					} else {
						if cell.font.StringWidth(cell.fallbackFont, strings.TrimSpace(buf.String()+" "+token)) > cellWidth {
							tableData2[i+n][j].SetText(strings.TrimSpace(buf.String()))
							buf.Reset()
							buf.WriteString(token)
							n++
						} else {
							if len(buf.String()) > 0 {
								buf.WriteString(" ")
							}
							buf.WriteString(token)
						}
					}
				}
				tableData2[i+n][j].SetText(strings.TrimSpace(buf.String()))
			}
		}
	}
	table.tableData = tableData2
}

func getNumVerCells(row []*Cell, index int) int {
	cell := row[index]
	numOfVerCells := 1
	if cell.text == nil {
		return numOfVerCells
	}
	cellWidth := getTotalWidth(row, index)
	tokens := strings.Fields(*cell.text)
	var buf strings.Builder
	for _, token := range tokens {
		if cell.font.StringWidth(cell.fallbackFont, token) > cellWidth {
			if len(buf.String()) > 0 {
				buf.WriteString(" ")
			}
			for _, ch := range token {
				if cell.font.StringWidth(cell.fallbackFont, strings.TrimSpace(buf.String()+" "+string(ch))) > cellWidth {
					numOfVerCells++
					buf.Reset()
				}
				buf.WriteRune(ch)
			}
		} else {
			if cell.font.StringWidth(cell.fallbackFont, strings.TrimSpace(buf.String()+" "+token)) > cellWidth {
				numOfVerCells++
				buf.Reset()
				buf.WriteString(token)
			} else {
				if len(buf.String()) > 0 {
					buf.WriteString(" ")
				}
				buf.WriteString(token)
			}
		}
	}
	return numOfVerCells
}
