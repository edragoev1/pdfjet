// table.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"log"
	"math"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// Table is used to create table objects and draw them on a page.
// Please see Example_08.
type Table struct {
	tableData       [][]*Cell
	numOfHeaderRows int
	// The index of the next row to draw, or -1 when all rows are drawn.
	rendered           int
	x1, y1             float32
	firstPageTopMargin float32
	bottomMargin       float32
}

// Constants
const (
	TableWith0HeaderRows = iota
	TableWith1HeaderRow
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
	table.tableData = make([][]*Cell, 0)
	table.numOfHeaderRows = 1
	table.rendered = 1
	return table
}

// NewTableFromFile creates a table from a text file with comma, pipe or tab
// separated values. The first line is the header row and uses f1; the other
// lines use f2. Every row gets as many cells as the first line has fields.
func NewTableFromFile(f1, f2 *Font, fileName string) *Table {
	table := NewTable()
	delimiterRegex := ""
	numberOfFields := 0
	lineNumber := 0
	f, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer func(f *os.File) {
		err := f.Close()
		if err != nil {
			log.Fatal(err)
		}
	}(f)
	scanner := bufio.NewScanner(f)
	// A line can be longer than the 64 KB that the scanner reads by default.
	scanner.Buffer(make([]byte, 0, 64*1024), math.MaxInt32)
	for scanner.Scan() {
		line := scanner.Text()
		if lineNumber == 0 {
			delimiterRegex = getDelimiterRegex(line)
			numberOfFields = len(strings.Split(line, delimiterRegex))
		}
		row := make([]*Cell, 0)
		fields := strings.Split(line, delimiterRegex)
		for _, field := range fields {
			if lineNumber == 0 {
				row = append(row, NewCell(f1, field))
			} else {
				row = append(row, NewCell(f2, field))
			}
		}
		if len(row) > numberOfFields {
			row2 := make([]*Cell, 0)
			for i := 0; i < numberOfFields; i++ {
				row2 = append(row2, row[i])
			}
			table.tableData = append(table.tableData, row2)
		} else if len(row) < numberOfFields {
			diff := numberOfFields - len(row)
			for i := 0; i < diff; i++ {
				row = append(row, NewCell(f2, ""))
			}
			table.tableData = append(table.tableData, row)
		} else {
			table.tableData = append(table.tableData, row)
		}
		lineNumber++
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}
	return table
}

// SetLocation sets the location (x, y) of the top left corner of table on the page.
// @param x the x coordinate of the top left point of the table.
// @param y the y coordinate of the top left point of the table.
func (table *Table) SetLocation(x, y float32) Drawable {
	table.x1 = x
	table.y1 = y
	return table
}

// SetBottomMargin sets the bottom margin for table.
// @param bottomMargin the margin.
func (table *Table) SetBottomMargin(bottomMargin float32) *Table {
	table.bottomMargin = bottomMargin
	return table
}

// SetData sets the table data and specifies the number of header rows in table data.
// The header rows are drawn again at the top of every page.
func (table *Table) SetData(tableData [][]*Cell, numOfHeaderRows int) *Table {
	table.tableData = tableData
	table.numOfHeaderRows = numOfHeaderRows
	table.rendered = numOfHeaderRows
	table.addCellsToCompleteTheGrid()
	return table
}

// addCellsToCompleteTheGrid adds empty cells to the rows that are shorter
// than the first row.
func (table *Table) addCellsToCompleteTheGrid() {
	numOfColumns := len(table.tableData[0])
	font := table.tableData[0][0].font
	for i, row := range table.tableData {
		diff := numOfColumns - len(row)
		for j := 0; j < diff; j++ {
			table.tableData[i] = append(table.tableData[i], NewCell(font, ""))
		}
	}
}

// RightAlignNumbers aligns to the right the cells whose text is a number,
// such as 1,234.50, (1,234.50), -5 or 1.5E+3. The periods, commas and
// apostrophes are ignored, and the number can be in parentheses.
func (table *Table) RightAlignNumbers() {
	for _, row := range table.tableData {
		for _, cell := range row {
			if isNumber(cell.text) {
				cell.SetTextAlignment(alignment.Right)
			}
		}
	}
}

// isNumber returns true if the text, without its periods, commas and
// apostrophes and the parentheses around it, is an optional sign, ASCII
// digits and an optional exponent, with optional spaces before and after.
func isNumber(text string) bool {
	str := text
	if len(str) >= 2 && str[0] == '(' && str[len(str)-1] == ')' {
		str = str[1 : len(str)-1]
	}
	var buf strings.Builder
	for _, ch := range str {
		if ch != '.' && ch != ',' && ch != '\'' {
			buf.WriteRune(ch)
		}
	}
	number := trimSpace(buf.String())
	i := 0
	if i < len(number) && (number[i] == '+' || number[i] == '-') {
		i++
	}
	start := i
	for i < len(number) && number[i] >= '0' && number[i] <= '9' {
		i++
	}
	if i == start {
		return false
	}
	if i < len(number) && (number[i] == 'e' || number[i] == 'E') {
		i++
		if i < len(number) && (number[i] == '+' || number[i] == '-') {
			i++
		}
		start = i
		for i < len(number) && number[i] >= '0' && number[i] <= '9' {
			i++
		}
		if i == start {
			return false
		}
	}
	return i == len(number)
}

// RemoveLineBetweenRows removes the horizontal lines between the rows from index1 to index2.
func (table *Table) RemoveLineBetweenRows(index1, index2 int) {
	for i := index1; i < index2; i++ {
		row := table.tableData[i]
		for _, cell := range row {
			cell.SetBottomBorder(false)
		}
		row = table.tableData[i+1]
		for _, cell := range row {
			cell.SetTopBorder(false)
		}
	}
}

// SetTextAlignInColumn sets the text alignment in the specified column.
//
// @param index the index of the specified column.
// @param alignment the specified alignment.
// Supported values: alignment.Left, alignment.Right, alignment.Center and alignment.Justify.
func (table *Table) SetTextAlignInColumn(index, alignment int) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetTextAlignment(alignment)
			if cell.textBox != nil {
				cell.textBox.SetTextAlignment(alignment)
			}
		}
	}
	return table
}

// SetTextColorInColumn sets the color of the text in the specified column.
// @param index the index of the specified column.
// @param color the color specified as an integer.
func (table *Table) SetTextColorInColumn(index int, color int32) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetTextColor(color)
			if cell.textBox != nil {
				cell.textBox.SetTextColor(color)
			}
		}
	}
	return table
}

// SetFontInColumn sets the font and the font size of the cells in the specified column.
// @param index the column index.
// @param font the font.
func (table *Table) SetFontInColumn(index int, font *Font) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetFont(font).SetFontSize(font.size)
			if cell.textBox != nil {
				cell.textBox.font = font
			}
		}
	}
	return table
}

// SetTextColorInRow sets the color of the text in the specified row.
// @param index the index of the specified row.
// @param color the color specified as an integer.
func (table *Table) SetTextColorInRow(index int, color int32) *Table {
	if index < len(table.tableData) {
		row := table.tableData[index]
		for _, cell := range row {
			cell.SetTextColor(color)
			if cell.textBox != nil {
				cell.textBox.SetTextColor(color)
			}
		}
	}
	return table
}

// SetFontInRow sets the font and the font size of the cells in the specified row.
// @param index the row index.
// @param font the font.
func (table *Table) SetFontInRow(index int, font *Font) *Table {
	if index < len(table.tableData) {
		row := table.tableData[index]
		for _, cell := range row {
			cell.SetFont(font).SetFontSize(font.size)
			if cell.textBox != nil {
				cell.textBox.font = font
			}
		}
	}
	return table
}

// SetColumnWidth sets the width of the column with the specified index.
// @param index the index of specified column.
// @param width the specified width.
func (table *Table) SetColumnWidth(index int, width float32) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			row[index].SetWidth(width)
		}
	}
	return table
}

// GetColumnWidth returns the column width of the column at the specified index.
// @param index the index of the column.
// @return the width of the column.
func (table *Table) GetColumnWidth(index int) float32 {
	return table.GetCellAtRowColumn(0, index).GetWidth()
}

// GetCellAt returns the cell at the specified row and column.
// @param row the specified row.
// @param col the specified column.
// @return the cell at the specified row and column.
func (table *Table) GetCellAt(rowIndex, colIndex int) *Cell {
	if rowIndex >= 0 {
		return table.tableData[rowIndex][colIndex]
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

// GetRow returns a list of cells for the specified row.
// @param index the index of the specified row.
// @return the list of cells.
func (table *Table) GetRow(index int) []*Cell {
	return table.tableData[index]
}

// GetRowAtIndex returns the cells in the specified row. Same as GetRow.
func (table *Table) GetRowAtIndex(index int) []*Cell {
	return table.GetRow(index)
}

// GetColumn returns a list of cells for the specified column.
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

// GetColumnAtIndex returns the cells in the specified column. Same as GetColumn.
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
	return table.drawTableRows(page, table.drawHeaderRows(page, 0))
}

// DrawOnPages draws this table on as many new pages as it needs.
// The pages are created detached and added to the list; add them to the PDF afterwards.
// It returns the x and y coordinates below the table on the last page.
func (table *Table) DrawOnPages(pdf *PDF, pages *[]*Page, pageSize [2]float32) [2]float32 {
	table.wrapAroundCellText()
	table.setRightBorderOnLastColumn()
	table.setBottomBorderOnLastRow()
	var xy [2]float32
	pageNumber := 1
	for table.hasMoreData() {
		page := NewPageDetached(pdf, pageSize)
		*pages = append(*pages, page)
		xy = table.drawTableRows(page, table.drawHeaderRows(page, pageNumber))
		pageNumber++
	}
	return xy
}

// drawHeaderRows draws the header rows at the top of the page and returns
// the point below them.
func (table *Table) drawHeaderRows(page *Page, pageNumber int) [2]float32 {
	x := table.x1
	y := table.y1
	if pageNumber == 1 && table.firstPageTopMargin > 0.0 {
		y = table.firstPageTopMargin
	}
	for i := 0; i < table.numOfHeaderRows; i++ {
		row := table.tableData[i]
		h := table.getMaxCellHeight(row)
		for j := 0; j < len(row); {
			cell := row[j]
			colspan := cell.GetColSpan()
			w := float32(0.0)
			for k := 0; k < colspan; k++ {
				w += row[j].GetWidth()
				j++
			}
			if page != nil {
				page.SetBrushColorRGB(cell.GetTextColor())
				if i == (table.numOfHeaderRows - 1) {
					cell.SetBottomBorder(true)
				}
				cell.drawOn(page, x, y, w, h)
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
	for table.rendered < len(table.tableData) {
		row := table.tableData[table.rendered]
		h := table.getMaxCellHeight(row)
		if page != nil && (y+h) > (page.height-table.bottomMargin) {
			return [2]float32{x, y}
		}
		for i := 0; i < len(row); {
			cell := row[i]
			colspan := cell.GetColSpan()
			w := float32(0.0)
			for j := 0; j < colspan; j++ {
				w += row[i].GetWidth()
				i++
			}
			if page != nil {
				page.SetBrushColorRGB(cell.GetTextColor())
				cell.drawOn(page, x, y, w, h)
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
	for i, cell := range row {
		totalWidth := getTotalWidth(row, i)
		cellHeight := cell.GetHeight(totalWidth)
		if cellHeight > maxCellHeight {
			maxCellHeight = cellHeight
		}
	}
	return maxCellHeight
}

// hasMoreData returns true if the table contains more data that needs to be drawn on a page.
func (table *Table) hasMoreData() bool {
	return table.rendered != -1
}

// GetWidth returns the width of the table when drawn on a page.
// @return the width of the table.
func (table *Table) GetWidth() float32 {
	var tableWidth float32
	row := table.tableData[0]
	for _, cell := range row {
		tableWidth += cell.GetWidth()
	}
	return tableWidth
}

// GetRowsRendered returns the number of rows below the header rows that are
// drawn so far, counting each line of wrapped cell text as a row, or -1 when
// all rows are drawn.
func (table *Table) GetRowsRendered() int {
	if table.rendered != -1 {
		return table.rendered - table.numOfHeaderRows
	}
	return -1
}

// SetCellBorders sets all table cells borders to false or true.
func (table *Table) SetCellBorders(borders bool) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetBorders(borders)
		}
	}
	return table
}

// SetCellBordersColor sets the color of the cell border lines.
// @param color the color of the cell border lines.
func (table *Table) SetCellBordersColor(color int32) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetStrokeColor(color)
		}
	}
	return table
}

// SetCellBordersWidth sets the width of the cell border lines.
// @param width the width of the border lines.
func (table *Table) SetCellBordersWidth(width float32) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetStrokeWidth(width)
		}
	}
	return table
}

// Sets the right border on all cells in the last column.
func (table *Table) setRightBorderOnLastColumn() {
	for _, row := range table.tableData {
		if !row[0].GetLeftBorder() {
			return
		}
	}
	// Only run this code if all the cells in the first column have left border.
	for _, row := range table.tableData {
		var cell *Cell
		var i = 0
		for i < len(row) {
			cell = row[i]
			i += cell.GetColSpan()
		}
		cell.SetRightBorder(true)
	}
}

// Sets the bottom border on all cells in the last row.
func (table *Table) setBottomBorderOnLastRow() {
	firstRow := table.tableData[0]
	for _, cell := range firstRow {
		if !cell.GetTopBorder() {
			return
		}
	}
	// Only run this code if all the cells in the first row have top border.
	lastRow := table.tableData[len(table.tableData)-1]
	for _, cell := range lastRow {
		cell.SetBottomBorder(true)
	}
}

// SetColumnWidths auto adjusts the widths of all columns so that they are just wide enough to
// hold the text without truncation.
func (table *Table) SetColumnWidths() *Table {
	maxColWidths := make([]float32, len(table.tableData[0]))
	for _, row := range table.tableData {
		for i := 0; i < len(row); i++ {
			cell := row[i]
			if cell.GetColSpan() == 1 {
				if cell.textBox != nil {
					tokens := splitOnWhitespace(cell.textBox.text)
					for _, token := range tokens {
						tokenWidth := cell.textBox.font.StringWidthFB(
							cell.textBox.fallbackFont, cell.textBox.font.size, token)
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
				} else {
					textWidth := cell.font.StringWidthFB(cell.fallbackFont, cell.font.size, cell.text)
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
	return table
}

// addExtraTableRows returns the table data with a row added below each row
// for every extra line that its wrapped cell text needs. The rows added below
// a header row are header rows too.
func (table *Table) addExtraTableRows() [][]*Cell {
	tableData2 := make([][]*Cell, 0)
	numOfHeaderRows2 := 0
	for r, row := range table.tableData {
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
				cell2 := NewCell(cell.GetFont(), "")
				cell2.SetFallbackFont(cell.GetFallbackFont())
				cell2.SetWidth(cell.GetWidth())
				cell2.SetLeftPadding(cell.GetLeftPadding())
				cell2.SetRightPadding(cell.GetRightPadding())
				// Java copies a null background and a null pen color across as
				// null, which leaves the new cell without either.
				if cell.hasBackgroundColor {
					cell2.SetBackgroundColorRGB(cell.GetBackgroundColor())
				}
				cell2.SetStrokeWidth(cell.GetStrokeWidth())
				if cell.hasStrokeColor {
					cell2.SetStrokeColorRGB(cell.GetStrokeColor())
				}
				cell2.SetTextColorRGB(cell.GetTextColor())
				// Java copies these across with Cell.setProperties()
				cell2.SetColSpan(cell.GetColSpan())
				cell2.SetTopBorder(cell.GetTopBorder())
				cell2.SetBottomBorder(cell.GetBottomBorder())
				cell2.SetLeftBorder(cell.GetLeftBorder())
				cell2.SetRightBorder(cell.GetRightBorder())
				cell2.SetTextAlignment(cell.GetTextAlignment())
				cell2.SetUnderline(cell.GetUnderline())
				cell2.SetStrikeout(cell.GetStrikeout())
				cell2.SetVerTextAlignment(cell.GetVerTextAlignment())
				cell2.SetTopPadding(0.0)
				cell2.SetTopBorder(false)
				row2 = append(row2, cell2)
			}
			tableData2 = append(tableData2, row2)
		}
		if r < table.numOfHeaderRows {
			numOfHeaderRows2 = len(tableData2)
		}
	}
	if table.rendered != -1 {
		table.rendered += numOfHeaderRows2 - table.numOfHeaderRows
	}
	table.numOfHeaderRows = numOfHeaderRows2
	return tableData2
}

func getTotalWidth(row []*Cell, index int) float32 {
	cell := row[index]
	colspan := cell.GetColSpan()
	cellWidth := float32(0.0)
	for i := 0; i < colspan; i++ {
		cellWidth += row[index+i].GetWidth()
	}
	cellWidth -= cell.leftPadding + row[index+(colspan-1)].rightPadding
	return cellWidth
}

// wrapAroundCellText wraps around the text in all cells so it fits the column width.
// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
func (table *Table) wrapAroundCellText() {
	tableData2 := table.addExtraTableRows()
	for i := 0; i < len(tableData2); i++ {
		row := tableData2[i]
		for j := 0; j < len(row); j++ {
			cell := row[j]
			cellWidth := getTotalWidth(row, j)
			tokens := splitOnWhitespace(cell.text)
			var n = 0
			var buf strings.Builder
			for _, token := range tokens {
				if cell.font.StringWidthFB(cell.fallbackFont, cell.font.size, token) > cellWidth {
					if buf.Len() > 0 {
						buf.WriteString(" ")
					}
					for _, ch := range token {
						if cell.font.StringWidthFB(cell.fallbackFont,
							cell.font.size, buf.String()+string(ch)) > cellWidth {
							tableData2[i+n][j].SetText(buf.String())
							buf.Reset()
							n++
						}
						buf.WriteRune(ch)
					}
				} else {
					if cell.font.StringWidthFB(cell.fallbackFont,
						cell.font.size, trimSpace(buf.String()+" "+token)) > cellWidth {
						tableData2[i+n][j].SetText(trimSpace(buf.String()))
						buf.Reset()
						buf.WriteString(token)
						n++
					} else {
						if buf.Len() > 0 {
							buf.WriteString(" ")
						}
						buf.WriteString(token)
					}
				}
			}
			tableData2[i+n][j].SetText(trimSpace(buf.String()))
		}
	}
	table.tableData = tableData2
}

// getNumVerCells returns the number of vertically stacked cells that the
// wrapped text of the cell needs.
func getNumVerCells(row []*Cell, index int) int {
	cell := row[index]
	numOfVerCells := 1
	cellWidth := getTotalWidth(row, index)
	tokens := splitOnWhitespace(cell.text)
	var buf strings.Builder
	for _, token := range tokens {
		if cell.font.StringWidthFB(cell.fallbackFont, cell.font.size, token) > cellWidth {
			if buf.Len() > 0 {
				buf.WriteString(" ")
			}
			for _, ch := range token {
				if cell.font.StringWidthFB(cell.fallbackFont,
					cell.font.size, buf.String()+string(ch)) > cellWidth {
					numOfVerCells++
					buf.Reset()
				}
				buf.WriteRune(ch)
			}
		} else {
			if cell.font.StringWidthFB(cell.fallbackFont,
				cell.font.size, trimSpace(buf.String()+" "+token)) > cellWidth {
				numOfVerCells++
				buf.Reset()
				buf.WriteString(token)
			} else {
				if buf.Len() > 0 {
					buf.WriteString(" ")
				}
				buf.WriteString(token)
			}
		}
	}
	return numOfVerCells
}

func getDelimiterRegex(str string) string {
	comma := 0
	pipe := 0
	tab := 0
	for _, ch := range str {
		switch ch {
		case ',':
			comma++
		case '|':
			pipe++
		case '\t':
			tab++
		}
	}
	if comma >= pipe {
		if comma >= tab {
			return ","
		}
		return "\t"
	} else {
		if pipe >= tab {
			return "|"
		}
		return "\t"
	}
}

func (table *Table) contains(visible []int, index int) bool {
	for _, i := range visible {
		if i == index {
			return true
		}
	}
	return false
}

// SetVisibleColumns keeps only the columns with the specified indexes.
func (table *Table) SetVisibleColumns(visible ...int) *Table {
	list := make([][]*Cell, 0)
	for _, row := range table.tableData {
		row2 := make([]*Cell, 0)
		for i := 0; i < len(row); i++ {
			if table.contains(visible, i) {
				row2 = append(row2, row[i])
			}
		}
		list = append(list, row2)
	}
	table.tableData = list
	return table
}

// SetFirstPageTopMargin sets the top margin on the first page when the table spans several pages.
func (table *Table) SetFirstPageTopMargin(firstPageTopMargin float32) *Table {
	table.firstPageTopMargin = firstPageTopMargin
	return table
}
