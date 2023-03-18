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
	"strings"
	"unicode"

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/border"
)

// Table is used to create table objects and draw them on a page.
// Please see Example_08.
type Table struct {
	rendered        int
	numOfPages      int
	tableData       [][]*Cell
	numOfHeaderRows int
	x1, y1          float32
	y1FirstPage     float32
	rightMargin     float32
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
	for i := table.numOfHeaderRows; i < len(table.tableData); i++ {
		row := table.tableData[i]
		for _, cell := range row {
			if cell.text != nil {
				isNumber := true
				runes := []rune(*cell.text)
				k := 0
				for k < len(runes) {
					ch := runes[k]
					k++
					if !unicode.IsDigit(ch) &&
						ch != '(' &&
						ch != ')' &&
						ch != '-' &&
						ch != '.' &&
						ch != ',' &&
						ch != '\'' {
						isNumber = false
					}
				}
				if isNumber {
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
			if cell.textBlock != nil {
				cell.textBlock.SetTextAlignment(alignment)
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
			if cell.textBlock != nil {
				cell.textBlock.SetBrushColor(color)
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
			if cell.textBlock != nil {
				cell.textBlock.font = font
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
		if cell.textBlock != nil {
			cell.textBlock.SetBrushColor(color)
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
		if cell.textBlock != nil {
			cell.textBlock.font = font
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
			if cell.textBlock != nil {
				cell.textBlock.SetWidth(width - (cell.leftPadding + cell.rightPadding))
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

// GetNumberOfPages returns the total number of pages that are required to draw table table on.
// @param page the type of pages we are drawing table table on.
// @return the number of pages.
func (table *Table) GetNumberOfPages(page *Page) int {
	table.numOfPages = 1
	for table.HasMoreData() {
		table.DrawOn(page)
	}
	table.ResetRenderedPagesCount()
	return table.numOfPages
}

// DrawOn draws this table on the specified page.
// @param page the page to draw this table on.
// @return Point the point on the page where to draw the next component.
func (table *Table) DrawOn(page *Page) [2]float32 {
	return table.drawTableRows(page, table.drawHeaderRows(page))
}

// DrawOnPages draws the table on pdf pages with the specified size.
func (table *Table) DrawOnPages(pdf *PDF, pages *[]*Page, pageSize [2]float32) [2]float32 {
	var xy [2]float32
	for table.HasMoreData() {
		page := NewPage(pdf, pageSize)
		*pages = append(*pages, page)
		xy = table.drawTableRows(page, table.drawHeaderRows(page))
	}
	// Allow the table to be drawn again later:
	table.ResetRenderedPagesCount()
	return xy
}

/**
 *  Draws table table on the specified page.
 *  @param page the page to draw table table on.
 *  @return Point the point on the page where to draw the next component.
 */
func (table *Table) drawHeaderRows(page *Page) []float32 {
	x := table.x1
	y := table.y1
	var wCell float32
	var hCell float32

	for i := 0; i < table.numOfHeaderRows; i++ {
		dataRow := table.tableData[i]
		hCell = table.getMaxCellHeight(dataRow)

		for j := 0; j < len(dataRow); j++ {
			cell := dataRow[j]
			wCell = cell.GetWidth()
			colspan := cell.GetColSpan()
			for k := 1; k < colspan; k++ {
				j++
				wCell += dataRow[j].width
			}
			if table != nil {
				page.SetBrushColor(cell.GetBrushColor())
				cell.Paint(page, x, y, wCell, hCell)
			}
			x += wCell
		}
		x = table.x1
		y += hCell
	}

	return []float32{x, y, wCell, hCell}
}

func (table *Table) drawTableRows(page *Page, parameter []float32) [2]float32 {
	x := parameter[0]
	y := parameter[1]
	wCell := parameter[2]
	hCell := parameter[3]

	for i := table.rendered; i < len(table.tableData); i++ {
		dataRow := table.tableData[i]
		hCell = table.getMaxCellHeight(dataRow)

		for j := 0; j < len(dataRow); j++ {
			cell := dataRow[j]
			wCell = cell.GetWidth()
			colspan := cell.GetColSpan()
			for k := 1; k < colspan; k++ {
				j++
				wCell += dataRow[j].GetWidth()
			}
			if page != nil {
				page.SetBrushColor(cell.GetBrushColor())
				cell.Paint(page, x, y, wCell, hCell)
			}
			x += wCell
		}
		x = table.x1
		y += hCell

		// Consider the height of the next row when checking if we must go to a new page
		if i < len(table.tableData)-1 {
			nextRow := table.tableData[i+1]
			for _, cell := range nextRow {
				cellHeight := cell.GetHeight()
				if cellHeight > hCell {
					hCell = cellHeight
				}
			}
		}

		if (y + hCell) > (page.height - table.bottomMargin) {
			if i == len(table.tableData)-1 {
				table.rendered = -1
			} else {
				table.rendered = i + 1
				table.numOfPages++
			}
			return [2]float32{x, y}
		}
	}
	table.rendered = -1

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
func (table *Table) HasMoreData() bool {
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

// WrapAroundCellText wraps around the text in all cells so it fits the column width.
// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
func (table *Table) WrapAroundCellText() {
	tableData2 := make([][]*Cell, 0)

	for _, row := range table.tableData {
		maxNumVerCells := 1
		for j, cell := range row {
			colspan := cell.GetColSpan()
			for n := 1; n < colspan; n++ {
				next := row[j+n]
				cell.SetWidth(cell.GetWidth() + next.GetWidth())
				next.SetWidth(0.0)
			}
			numVerCells := cell.getNumVerCells()
			if numVerCells > maxNumVerCells {
				maxNumVerCells = numVerCells
			}
		}

		for j := 0; j < maxNumVerCells; j++ {
			row2 := make([]*Cell, 0)
			for _, cell := range row {

				cell2 := NewEmptyCell(cell.GetFont())
				cell2.SetFallbackFont(cell.GetFallbackFont())
				cell2.SetPoint(cell.GetPoint())
				cell2.SetWidth(cell.GetWidth())
				if j == 0 {
					cell2.SetTopPadding(cell.topPadding)
				}
				if j == (maxNumVerCells - 1) {
					cell2.SetBottomPadding(cell.bottomPadding)
				}
				cell2.SetLeftPadding(cell.leftPadding)
				cell2.SetRightPadding(cell.rightPadding)
				cell2.SetLineWidth(cell.lineWidth)
				cell2.SetBgColor(cell.GetBgColor())
				cell2.SetPenColor(cell.GetPenColor())
				cell2.SetBrushColor(cell.GetBrushColor())
				cell2.SetProperties(cell.GetProperties())
				cell2.SetVerTextAlignment(cell.GetVerTextAlignment())
				if j == 0 {
					if cell.GetImage() != nil {
						cell2.SetImage(cell.GetImage())
					}
					if cell.GetCompositeTextLine() != nil {
						cell2.SetCompositeTextLine(cell.GetCompositeTextLine())
					} else {
						cell2.SetText(cell.GetText())
					}
					if maxNumVerCells > 1 {
						cell2.SetBorder(border.Bottom, false)
					}
				} else {
					cell2.SetBorder(border.Top, false)
					if j < (maxNumVerCells - 1) {
						cell2.SetBorder(border.Bottom, false)
					}
				}
				row2 = append(row2, cell2)
			}
			tableData2 = append(tableData2, row2)
		}
	}

	for i, row := range tableData2 {
		for j, cell := range row {
			if cell.text != nil {
				n := 0
				textLines := strings.Fields(*cell.text)
				for _, textLine := range textLines {
					var sb strings.Builder
					tokens := strings.Fields(textLine)
					if len(tokens) == 1 {
						sb.WriteString(tokens[0])
					} else {
						for k := 0; k < len(tokens); k++ {
							token := tokens[k]
							if cell.font.StringWidth(
								cell.fallbackFont, sb.String()+" "+token) > cell.GetWidth()-(cell.leftPadding+cell.rightPadding) {
								tableData2[i+n][j].SetText(sb.String())
								sb.Reset()
								sb.WriteString(token)
								n++
							} else {
								if k > 0 {
									sb.WriteString(" ")
								}
								sb.WriteString(token)
							}
						}
					}
					tableData2[i+n][j].SetText(sb.String())
					n++
				}
			} else {
				tableData2[i][j].SetCompositeTextLine(cell.GetCompositeTextLine())
			}
		}
	}

	table.tableData = tableData2
}

// SetNoCellBorders sets all table cells borders to <strong>false</strong>.
func (table *Table) SetNoCellBorders() {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetNoBorders()
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

// ResetRenderedPagesCount resets the rendered pages count.
// Call table method if you have to draw table table more than one time.
func (table *Table) ResetRenderedPagesCount() {
	table.rendered = table.numOfHeaderRows
}

// MergeOverlaidBorders removes borders that have the same color and overlap 100%.
// The result is improved onscreen rendering of thin border lines by some PDF viewers.
func (table *Table) MergeOverlaidBorders() {
	for i, row := range table.tableData {
		for j, currentCell := range row {
			if j < len(row)-1 {
				cellAtRight := row[j+1]
				if cellAtRight.GetBorder(border.Left) &&
					(currentCell.GetPenColor() == cellAtRight.GetPenColor()) &&
					(currentCell.GetLineWidth() == cellAtRight.GetLineWidth()) &&
					(currentCell.GetColSpan()+j < (len(row) - 1)) {
					currentCell.SetBorder(border.Right, false)
				}
			}
			if i < (len(table.tableData) - 1) {
				nextRow := table.tableData[i+1]
				cellBelow := nextRow[j]
				if cellBelow.GetBorder(border.Top) &&
					(currentCell.GetPenColor() == cellBelow.GetPenColor()) &&
					(currentCell.GetLineWidth() == cellBelow.GetLineWidth()) {
					currentCell.SetBorder(border.Bottom, false)
				}
			}
		}
	}
}

// AutoAdjustColumnWidths adjusts the widths of all columns so that they are just wide enough to hold the text without truncation.
func (table *Table) AutoAdjustColumnWidths() {
	// Find the maximum text width for each column
	maxColWidths := make([]float32, len(table.tableData[0]))

	for i := 0; i < table.numOfHeaderRows; i++ {
		for j := 0; j < len(maxColWidths); j++ {
			cell := table.tableData[i][j]
			textWidth := cell.font.StringWidth(cell.fallbackFont, *cell.text)
			textWidth += cell.leftPadding + cell.rightPadding
			if textWidth > maxColWidths[j] {
				maxColWidths[j] = textWidth
			}
		}
	}

	for i := table.numOfHeaderRows; i < len(table.tableData); i++ {
		for j := 0; j < len(maxColWidths); j++ {
			cell := table.tableData[i][j]
			if cell.GetColSpan() > 1 {
				continue
			}
			if cell.text != nil {
				textWidth := cell.font.StringWidth(cell.fallbackFont, *cell.text)
				textWidth += cell.leftPadding + cell.rightPadding
				if textWidth > maxColWidths[j] {
					maxColWidths[j] = textWidth
				}
			}
			if cell.image != nil {
				imageWidth := cell.image.GetWidth() + cell.leftPadding + cell.rightPadding
				if imageWidth > maxColWidths[j] {
					maxColWidths[j] = imageWidth
				}
			}
			if cell.barCode != nil {
				barcodeWidth := cell.barCode.DrawOn(nil)[0] + cell.leftPadding + cell.rightPadding
				if barcodeWidth > maxColWidths[j] {
					maxColWidths[j] = barcodeWidth
				}
			}
			if cell.textBlock != nil {
				tokens := strings.Fields(cell.textBlock.text)
				for _, token := range tokens {
					tokenWidth := cell.textBlock.font.StringWidth(cell.textBlock.fallbackFont, token)
					tokenWidth += cell.leftPadding + cell.rightPadding
					if tokenWidth > maxColWidths[j] {
						maxColWidths[j] = tokenWidth
					}
				}
			}
		}
	}

	for i := 0; i < len(table.tableData); i++ {
		row := table.tableData[i]
		for j := 0; j < len(row); j++ {
			cell := row[j]
			cell.SetWidth(maxColWidths[j] + 0.1)
		}
	}

	table.AutoResizeColumnsWithColspanBiggerThanOne()
}

func (table *Table) isTextColumn(index int) bool {
	for i := table.numOfHeaderRows; i < len(table.tableData); i++ {
		dataRow := table.tableData[i]
		if dataRow[index].image != nil || dataRow[index].barCode != nil {
			return false
		}
	}
	return true
}

// FitToPage -- TODO:
func (table *Table) FitToPage(pageSize [2]float32) {
	table.AutoAdjustColumnWidths()

	tableWidth := (pageSize[0] - table.x1) - table.rightMargin
	textColumnWidths := float32(0.0)
	otherColumnWidths := float32(0.0)
	row := table.tableData[0]
	for i := 0; i < len(row); i++ {
		cell := row[i]
		if table.isTextColumn(i) {
			textColumnWidths += cell.GetWidth()
		} else {
			otherColumnWidths += cell.GetWidth()
		}
	}

	var adjusted float32 = 0.0
	if (tableWidth - otherColumnWidths) > textColumnWidths {
		adjusted = textColumnWidths + ((tableWidth - otherColumnWidths) - textColumnWidths)
	} else {
		adjusted = textColumnWidths - (textColumnWidths - (tableWidth - otherColumnWidths))
	}
	factor := adjusted / textColumnWidths
	for i := 0; i < len(row); i++ {
		if table.isTextColumn(i) {
			table.SetColumnWidth(i, table.GetColumnWidth(i)*factor)
		}
	}

	table.AutoResizeColumnsWithColspanBiggerThanOne()
	table.MergeOverlaidBorders()
}

// AutoResizeColumnsWithColspanBiggerThanOne -- TODO:
func (table *Table) AutoResizeColumnsWithColspanBiggerThanOne() {
	for i := 0; i < len(table.tableData); i++ {
		dataRow := table.tableData[i]
		for j := 0; j < len(dataRow); j++ {
			cell := dataRow[j]
			colspan := cell.GetColSpan()
			if colspan > 1 {
				if cell.textBlock != nil {
					sumOfWidths := cell.GetWidth()
					for k := 1; k < colspan; k++ {
						j++
						sumOfWidths += dataRow[j].GetWidth()
					}
					cell.textBlock.SetWidth(sumOfWidths - (cell.leftPadding + cell.rightPadding))
				}
			}
		}
	}
}

// SetRightMargin -- TODO:
func (table *Table) SetRightMargin(rightMargin float32) {
	table.rightMargin = rightMargin
}

// SetFirstPageTopMargin -- TODO:
func (table *Table) SetFirstPageTopMargin(topMargin float32) {
	table.y1FirstPage = table.y1 + topMargin
}

// AddToRow -- TODO:
func (table *Table) AddToRow(row []*Cell, cell *Cell) {
	row = append(row, cell)
	for i := 1; i < cell.GetColSpan(); i++ {
		row = append(row, NewCell(cell.GetFont(), ""))
	}
}
