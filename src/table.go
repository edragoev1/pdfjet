// table.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"math"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/internal/utf8text"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Table is used to create table objects and draw them on a page.
// Please see Example_08.
type Table struct {
	tableData       [][]*Cell
	numOfHeaderRows int
	numOfFooterRows int
	// The index of the next row to draw, or -1 when all rows are drawn.
	rendered           int
	x1, y1             float32
	firstPageTopMargin float32
	bottomMargin       float32
	// The Table element of a PDF/UA document, while the table is drawn, and
	// the TH or TD elements of the last row, by column, that the rows with
	// the next lines of its wrapped text add to.
	structElement *structElement
	cellElements  []*structElement
}

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
// lines use f2. Every row gets as many cells as the first line has fields. A
// quoted field is read as RFC 4180 reads it, and a line break inside one goes
// on to the next line of the file and is drawn as a space.
func NewTableFromFile(f1, f2 *Font, fileName string) *Table {
	table := NewTable()
	delimiter := ""
	numberOfFields := 0
	lineNumber := 0
	f, err := os.Open(fileName)
	if err != nil {
		panic(err)
	}
	defer func(f *os.File) {
		err := f.Close()
		if err != nil {
			panic(err)
		}
	}(f)
	scanner := bufio.NewScanner(f)
	// A line can be longer than the 64 KB that the scanner reads by default.
	scanner.Buffer(make([]byte, 0, 64*1024), math.MaxInt32)
	nextLine := func() (string, bool) {
		if scanner.Scan() {
			return utf8text.Decode(scanner.Bytes()), true
		}
		return "", false
	}
	for scanner.Scan() {
		line := utf8text.Decode(scanner.Bytes())
		if lineNumber == 0 {
			// A byte order mark at the start of the file is not part of the text.
			line = strings.TrimPrefix(line, "\uFEFF")
			delimiter = getDelimiter(line)
		}
		row := make([]*Cell, 0)
		// The empty fields at the end of the line are kept, a quoted field holds
		// its delimiters instead of being cut at them, and its line breaks, which
		// go on to the next lines, are spaces.
		fields := readDelimitedRecord(line, delimiter, nextLine)
		if lineNumber == 0 {
			numberOfFields = len(fields)
		}
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
		panic(err)
	}
	return table
}

// SetLocation sets the location (x, y) of the top left corner of table on the page.
//   - x: the x coordinate of the top left point of the table.
//   - y: the y coordinate of the top left point of the table.
func (table *Table) SetLocation(x, y float32) Drawable {
	table.x1 = x
	table.y1 = y
	return table
}

// SetBottomMargin sets the bottom margin for table.
//   - bottomMargin: the margin.
func (table *Table) SetBottomMargin(bottomMargin float32) *Table {
	table.bottomMargin = bottomMargin
	return table
}

// SetTableData sets the table data and specifies the number of header rows in table data.
// The header rows are drawn again at the top of every page.
func (table *Table) SetTableData(tableData [][]*Cell, numOfHeaderRows int) *Table {
	table.tableData = tableData
	table.numOfHeaderRows = numOfHeaderRows
	table.rendered = numOfHeaderRows
	table.addCellsToCompleteTheGrid()
	return table
}

// SetNumberOfFooterRows makes the last rows of the table data footer rows,
// which are drawn again at the end of the table on every page, under the last
// row the page holds, as the header rows are drawn again at the top. A page
// leaves room for them. In a PDF/UA document they are rows of the table once,
// where the table ends, and artifacts on the other pages.
func (table *Table) SetNumberOfFooterRows(numOfFooterRows int) *Table {
	table.numOfFooterRows = max(0, numOfFooterRows)
	return table
}

// footerStart returns the index of the first footer row, after the header
// rows and the rows of the table.
func (table *Table) footerStart() int {
	return max(table.numOfHeaderRows, len(table.tableData)-table.numOfFooterRows)
}

// addCellsToCompleteTheGrid adds empty cells to the rows that are shorter
// than the first row.
func (table *Table) addCellsToCompleteTheGrid() {
	if len(table.tableData) == 0 || len(table.tableData[0]) == 0 {
		return
	}
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
func (table *Table) RightAlignNumbers() *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			if isNumber(cell.text) {
				cell.SetTextAlignment(alignment.Right)
			}
		}
	}
	return table
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
func (table *Table) RemoveLineBetweenRows(index1, index2 int) *Table {
	for i := index1; i < index2; i++ {
		row := table.tableData[i]
		for _, cell := range row {
			cell.properties &= ^border.Bottom
		}
		row = table.tableData[i+1]
		for _, cell := range row {
			cell.properties &= ^border.Top
		}
	}
	return table
}

// SetPageSum makes the cell of a footer row show the sum of its column over
// the rows of each page: the page total. The numbers are read as
// RightAlignNumbers reads them, with commas and apostrophes between the
// thousands and a period before the decimals, and a cell that has no number
// is left out. The sum has the number of decimals, from 0 to 9, rounded half
// away from zero, and commas between the thousands. Until the table is drawn
// the cell has the sum of all the rows, so that AutoAdjustColumnWidths leaves
// room for it; its text is not wrapped. The row is the index of a footer row,
// see SetNumberOfFooterRows.
func (table *Table) SetPageSum(row, column, decimals int) *Table {
	return table.setSum(row, column, decimals, cellPageSum)
}

// SetRunningSum makes the cell of a footer row show the sum of its column
// over all the rows up to the end of each page: the total carried forward,
// which is the total of the table on its last page. The numbers are read and
// the sum is written as SetPageSum reads and writes them.
func (table *Table) SetRunningSum(row, column, decimals int) *Table {
	return table.setSum(row, column, decimals, cellRunningSum)
}

// SetBroughtForwardSum makes the cell of a header row show the sum of its
// column over all the rows of the pages before: the total brought forward,
// which the cell that SetRunningSum marks shows at the end of the page
// before. A header row with such a cell is drawn from the second page on,
// under the header rows above it, and not on the first page, which has
// nothing to bring forward; in a PDF/UA document it is an artifact, as the
// header rows are on those pages. The numbers are read and the sum is written
// as SetPageSum reads and writes them. The row is the index of a header row,
// see SetTableData.
func (table *Table) SetBroughtForwardSum(row, column, decimals int) *Table {
	return table.setSum(row, column, decimals, cellBroughtForwardSum)
}

func (table *Table) setSum(row, column, decimals int, kind uint32) *Table {
	if row < 0 || row >= len(table.tableData) || column < 0 || column >= len(table.tableData[row]) {
		return table
	}
	places := max(0, min(9, decimals))
	cell := table.tableData[row][column]
	cell.properties = (cell.properties &^ cellSumBits) | kind | (uint32(places) << cellSumDecimals)
	end := min(row, table.footerStart())
	if kind == cellBroughtForwardSum {
		end = table.footerStart()
	}
	cell.SetText(formatSum(table.sumOf(column, table.numOfHeaderRows, end, places), places))
	return table
}

// setSums writes the sums in the cells of the rows from start to stop that
// SetPageSum, SetRunningSum and SetBroughtForwardSum mark: those of the rows
// of the page, from first to end, those of all the rows to end, and those of
// all the rows before first.
func (table *Table) setSums(start, stop, first, end int) {
	for r := start; r < stop && r < len(table.tableData); r++ {
		for j, cell := range table.tableData[r] {
			kind := cell.properties & cellSumKind
			if kind == 0 {
				continue
			}
			places := int((cell.properties >> cellSumDecimals) & 0xF)
			from := table.numOfHeaderRows
			if kind == cellPageSum {
				from = first
			}
			to := end
			if kind == cellBroughtForwardSum {
				to = first
			}
			cell.SetText(formatSum(table.sumOf(j, from, to, places), places))
		}
	}
}

func (table *Table) setFooterSums(first, end int) {
	table.setSums(table.footerStart(), len(table.tableData), first, end)
}

// isBroughtForwardRow is true when the row, or the row whose wrapped text it
// holds, has a cell that brings a total forward, which the first page does
// not draw.
func (table *Table) isBroughtForwardRow(r int) bool {
	for r > 0 && table.isContinuation(r) {
		r--
	}
	for _, cell := range table.tableData[r] {
		if cell.properties&cellSumKind == cellBroughtForwardSum {
			return true
		}
	}
	return false
}

// maxSum is the largest sum, and number, in units of the decimals: 18 digits.
const maxSum int64 = 999999999999999999

// sumOf returns the sum of the numbers in the column of the rows from first
// to end, in units of the decimals. A number that would take the sum past 18
// digits is left out.
func (table *Table) sumOf(column, first, end, decimals int) int64 {
	sum := int64(0)
	for r := max(0, first); r < end && r < len(table.tableData); r++ {
		row := table.tableData[r]
		if column >= len(row) {
			continue
		}
		cell := row[column]
		if cell.properties&cellCovered != 0 || !cell.hasText {
			continue
		}
		if value, ok := numberOf(cell.text, decimals); ok {
			if total := sum + value; total <= maxSum && total >= -maxSum {
				sum = total
			}
		}
	}
	return sum
}

// numberOf returns the number the text is, in units of the decimals, rounded
// half away from zero, and false when the text is not a number, as isNumber
// reads it, has more than one period, or is more than 18 digits in those
// units. Commas and apostrophes are between the thousands, a period is before
// the decimals, and parentheses make a number negative.
func numberOf(text string, decimals int) (int64, bool) {
	if !isNumber(text) {
		return 0, false
	}
	str := text
	negative := false
	if len(str) >= 2 && str[0] == '(' && str[len(str)-1] == ')' {
		str = str[1 : len(str)-1]
		negative = true
	}
	str = strings.TrimSpace(strings.NewReplacer(",", "", "'", "").Replace(str))
	if str[0] == '+' || str[0] == '-' {
		negative = negative != (str[0] == '-')
		str = str[1:]
	}
	exponent := 0
	if e := strings.IndexAny(str, "eE"); e != -1 {
		digits := str[e+1:]
		sign := 1
		if digits[0] == '+' || digits[0] == '-' {
			if digits[0] == '-' {
				sign = -1
			}
			digits = digits[1:]
		}
		if len(digits) > 4 || strings.Contains(digits, ".") {
			return 0, false
		}
		n, _ := strconv.Atoi(digits)
		exponent = sign * n
		str = str[:e]
	}
	point := strings.Index(str, ".")
	if point != strings.LastIndex(str, ".") {
		return 0, false
	}
	fraction := ""
	digits := str
	if point != -1 {
		fraction = str[point+1:]
		digits = str[:point] + fraction
	}
	digits = strings.TrimLeft(digits, "0")
	if digits == "" {
		return 0, true
	}
	// The number is the digits times 10 to the shift, in units of the
	// decimals.
	shift := exponent - len(fraction) + decimals
	var value int64
	if shift >= 0 {
		if len(digits)+shift > 18 {
			return 0, false
		}
		value, _ = strconv.ParseInt(digits, 10, 64)
		for i := 0; i < shift; i++ {
			value *= 10
		}
	} else {
		keep := len(digits) + shift
		if keep < 0 {
			return 0, true
		}
		if keep > 18 {
			return 0, false
		}
		if keep > 0 {
			value, _ = strconv.ParseInt(digits[:keep], 10, 64)
		}
		if digits[keep] >= '5' {
			value++
		}
		if value > maxSum {
			return 0, false
		}
	}
	if negative {
		value = -value
	}
	return value, true
}

// formatSum returns the sum in units of the decimals as text: the decimals
// after a period, and commas between the thousands.
func formatSum(sum int64, decimals int) string {
	magnitude := sum
	if magnitude < 0 {
		magnitude = -magnitude
	}
	digits := strconv.FormatInt(magnitude, 10)
	for len(digits) <= decimals {
		digits = "0" + digits
	}
	point := len(digits) - decimals
	var text strings.Builder
	if sum < 0 {
		text.WriteByte('-')
	}
	for i := 0; i < point; i++ {
		if i > 0 && (point-i)%3 == 0 {
			text.WriteByte(',')
		}
		text.WriteByte(digits[i])
	}
	if decimals > 0 {
		text.WriteByte('.')
		text.WriteString(digits[point:])
	}
	return text.String()
}

// KeepRowWithNext keeps the row with the specified index on the same page as
// the next row, as a heading row is kept with the rows under it: a page break
// does not fall between them, and moves both to the next page. Rows kept with
// the next one one after another are kept together, unless together they are
// taller than a page. Call it before the table is drawn.
func (table *Table) KeepRowWithNext(index int) *Table {
	if index >= 0 && index < len(table.tableData) {
		for _, cell := range table.tableData[index] {
			cell.properties |= cellKeptWithNext
		}
	}
	return table
}

// SetTextAlignmentInColumn sets the text alignment in the specified column.
//
//   - index: the index of the specified column.
//   - alignment: the specified alignment.
//
// Supported values: alignment.Left, alignment.Right, alignment.Center and alignment.Justify.
func (table *Table) SetTextAlignmentInColumn(index int, textAlignment alignment.Alignment) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetTextAlignment(textAlignment)
			if cell.GetTextBlock() != nil {
				cell.GetTextBlock().SetTextAlignment(textAlignment)
			}
		}
	}
	return table
}

// SetTextColorInColumn sets the color of the text in the specified column.
//   - index: the index of the specified column.
//   - color: the color specified as an integer.
func (table *Table) SetTextColorInColumn(index int, color int32) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetTextColor(color)
			if cell.GetTextBlock() != nil {
				cell.GetTextBlock().SetTextColor(color)
			}
		}
	}
	return table
}

// SetFontInColumn sets the font and the font size of the cells in the specified column.
//   - index: the column index.
//   - font: the font.
func (table *Table) SetFontInColumn(index int, font *Font) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			cell := row[index]
			cell.SetFont(font).SetFontSize(font.size)
			if cell.GetTextBlock() != nil {
				cell.GetTextBlock().SetFont(font).SetFontSize(font.GetSize())
			}
		}
	}
	return table
}

// SetTextColorInRow sets the color of the text in the specified row.
//   - index: the index of the specified row.
//   - color: the color specified as an integer.
func (table *Table) SetTextColorInRow(index int, color int32) *Table {
	if index < len(table.tableData) {
		row := table.tableData[index]
		for _, cell := range row {
			cell.SetTextColor(color)
			if cell.GetTextBlock() != nil {
				cell.GetTextBlock().SetTextColor(color)
			}
		}
	}
	return table
}

// SetFontInRow sets the font and the font size of the cells in the specified row.
//   - index: the row index.
//   - font: the font.
func (table *Table) SetFontInRow(index int, font *Font) *Table {
	if index < len(table.tableData) {
		row := table.tableData[index]
		for _, cell := range row {
			cell.SetFont(font).SetFontSize(font.size)
			if cell.GetTextBlock() != nil {
				cell.GetTextBlock().SetFont(font).SetFontSize(font.GetSize())
			}
		}
	}
	return table
}

// SetColumnWidth sets the width of the column with the specified index.
//   - index: the index of specified column.
//   - width: the specified width.
func (table *Table) SetColumnWidth(index int, width float32) *Table {
	for _, row := range table.tableData {
		if index < len(row) {
			row[index].SetWidth(width)
		}
	}
	return table
}

// GetColumnWidth returns the column width of the column at the specified index.
//   - index: the index of the column.
//
// Returns the width of the column.
func (table *Table) GetColumnWidth(index int) float32 {
	return table.GetCellAt(0, index).GetWidth()
}

// GetCellAt returns the cell at the specified row and column.
//   - row: the specified row.
//   - col: the specified column.
//
// Returns the cell at the specified row and column.
func (table *Table) GetCellAt(rowIndex, colIndex int) *Cell {
	if rowIndex >= 0 {
		return table.tableData[rowIndex][colIndex]
	}
	return table.tableData[len(table.tableData)+rowIndex][colIndex]
}

// GetRow returns a list of cells for the specified row.
//   - index: the index of the specified row.
//
// Returns the list of cells.
func (table *Table) GetRow(index int) []*Cell {
	return table.tableData[index]
}

// GetColumn returns a list of cells for the specified column.
//   - index: the index of the specified column.
//
// Returns the list of cells.
func (table *Table) GetColumn(index int) []*Cell {
	column := make([]*Cell, 0)
	for _, row := range table.tableData {
		if index < len(row) {
			column = append(column, row[index])
		}
	}
	return column
}

// DrawOn draws this table on the specified page.
//   - page: the page to draw this table on.
//
// Returns the x and y coordinates of the bottom right corner of the table.
func (table *Table) DrawOn(page *Page) [2]float32 {
	if len(table.tableData) == 0 {
		return [2]float32{table.x1, table.y1} // An empty table draws nothing.
	}
	table.wrapAroundCellText()
	table.applyRowSpans()
	table.setRightBorderOnLastColumn()
	table.setBottomBorderOnLastRow()
	xy := table.drawTableRows(page, table.drawHeaderRows(page, 0))
	return [2]float32{table.x1 + table.GetWidth(), xy[1]}
}

// DrawOnPages draws this table on as many new pages as it needs.
// The pages are created detached and added to the list; add them to the PDF afterwards.
// It returns the x and y coordinates of the bottom right corner of the table on
// the last page.
func (table *Table) DrawOnPages(pdf *PDF, pages *[]*Page, pageSize pagesize.PageSize) [2]float32 {
	if len(table.tableData) == 0 {
		return [2]float32{table.x1, table.y1} // An empty table needs no page.
	}
	table.wrapAroundCellText()
	table.applyRowSpans()
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
	return [2]float32{table.x1 + table.GetWidth(), xy[1]}
}

// drawHeaderRows draws the header rows at the top of the page and returns
// the point below them.
func (table *Table) drawHeaderRows(page *Page, pageNumber int) [2]float32 {
	x := table.x1
	y := table.y1
	if pageNumber == 1 && table.firstPageTopMargin > 0.0 {
		y = table.firstPageTopMargin
	}
	// In a PDF/UA document the table is a Table element, which the rows drawn
	// on the next pages go on adding to. The header rows are TH cells the
	// first time they are drawn, and artifacts on the next pages.
	first := (table.rendered == table.numOfHeaderRows)
	if page != nil && (first || table.structElement == nil) {
		table.structElement = page.addStructElementOpen(
			page.structParent, structelem.Table, "", true)
	}
	if page != nil && !first && table.numOfHeaderRows > 0 {
		page.AddArtifactBMC()
	}
	heights := table.getRowHeights()
	// The rows that bring a total forward are drawn from the second page on,
	// with the total of the rows before the page.
	last := -1
	for i := 0; i < table.numOfHeaderRows && i < len(table.tableData); i++ {
		if !(first && table.isBroughtForwardRow(i)) {
			last = i
		}
	}
	if table.rendered != -1 {
		table.setSums(0, table.numOfHeaderRows, table.rendered, table.rendered)
	}
	for i := 0; i < table.numOfHeaderRows && i < len(table.tableData); i++ {
		if first && table.isBroughtForwardRow(i) {
			continue
		}
		row := table.tableData[i]
		if page != nil {
			if i == last {
				for _, cell := range row {
					cell.properties |= border.Bottom
				}
			}
			cellStructure := structelem.StructElem("")
			if first {
				cellStructure = structelem.TH
			}
			table.drawRow(page, row, x, y, heights, i, cellStructure)
		}
		y += heights[i]
	}
	if page != nil && !first && table.numOfHeaderRows > 0 {
		page.AddEMC()
	}
	return [2]float32{x, y}
}

// drawRow draws the cells of the row. In a PDF/UA document the row is a TR
// element and each cell a TH or TD element, which holds what the cell draws;
// a row that goes on with the wrapped text of the row above adds to its
// elements. With no cell structure the row is not tagged, as it is an artifact.
func (table *Table) drawRow(page *Page, row []*Cell, x, y float32, heights []float32,
	rowIndex int, cellStructure structelem.StructElem) {
	parent := page.structParent
	tagged := table.structElement != nil && cellStructure != ""
	continued := (row[0].properties & cellContinued) != 0
	var rowElement *structElement
	if tagged && !continued && !allCovered(row) {
		rowElement = page.addStructElement(table.structElement, structelem.TR, "")
		if len(table.cellElements) != len(row) {
			table.cellElements = make([]*structElement, len(row))
		}
	}
	for i := 0; i < len(row); {
		cell := row[i]
		colspan := cell.GetColSpan()
		if colspan < 1 {
			colspan = 1
		}
		covered := (cell.properties & cellCovered) != 0
		if tagged && !covered {
			if !continued {
				table.cellElements[i] = page.addStructElement(rowElement, cellStructure,
					cellAttributes(cellStructure, colspan, cell.rowsSpanned))
			}
			page.structParent = nil
			if i < len(table.cellElements) {
				page.structParent = table.cellElements[i]
			}
		}
		w := float32(0.0)
		for j := 0; j < colspan; j++ {
			w += row[i].GetWidth()
			i++
		}
		if !covered {
			// A cell that spans rows is as tall as all the rows it covers.
			cellHeight := float32(0.0)
			end := rowIndex + cell.rowsSpanned
			if end > len(heights) {
				end = len(heights)
			}
			for r := rowIndex; r < end; r++ {
				cellHeight += heights[r]
			}
			page.SetBrushColor(cell.textColor)
			cell.drawOn(page, x, y, w, cellHeight)
		}
		x += w
	}
	page.structParent = parent
}

// cellAttributes returns the attributes of a table cell element: the scope of
// a header cell and the number of columns a cell spans, or "" when it has
// neither.
func cellAttributes(cellStructure structelem.StructElem, colspan, rowspan int) string {
	spans := ""
	if colspan > 1 {
		spans += " /ColSpan " + strconv.Itoa(colspan)
	}
	if rowspan > 1 {
		spans += " /RowSpan " + strconv.Itoa(rowspan)
	}
	if cellStructure == structelem.TH {
		return "<</O /Table /Scope /Column" + spans + ">>"
	}
	if spans != "" {
		return "<</O /Table" + spans + ">>"
	}
	return ""
}

// allCovered is true when every cell of the row is one that a cell above it
// spans over, so the row holds no cell of its own and is not a row of the
// table.
func allCovered(row []*Cell) bool {
	for _, cell := range row {
		if (cell.properties & cellCovered) == 0 {
			return false
		}
	}
	return len(row) > 0
}

// drawTableRows draws the rows from the next row to draw, as many as fit on
// the page, and the footer rows under them. With no page it measures them all
// and leaves the next row to draw as it is.
func (table *Table) drawTableRows(page *Page, xy [2]float32) [2]float32 {
	x := xy[0]
	y := xy[1]
	footer := table.footerStart()
	done := table.rendered == -1
	index := table.rendered
	if done {
		index = footer
	}
	first := index
	heights := table.getRowHeights()
	// Where the rows start on the next pages, under the header rows.
	top := table.y1
	for r := 0; r < table.numOfHeaderRows && r < len(heights); r++ {
		top += heights[r]
	}
	// Where the rows end on a page, over the footer rows.
	bottom := float32(0.0)
	if page != nil {
		bottom = page.height - table.bottomMargin
	}
	for r := footer; r < len(table.tableData); r++ {
		bottom -= heights[r]
	}
	// The rows before the first of these are not kept with the next row,
	// and the lines of the rows before the second are cut where the page
	// ends, as rows of their own.
	cutRowsUntil := -1
	cutLinesUntil := -1
	for index < footer {
		// The rows a cell spans, the lines a row wraps into and the rows kept
		// with the next one are drawn together, so that a page break never
		// cuts one of them in two. The footer rows are not in them.
		keepLines := index >= cutLinesUntil
		keepRows := keepLines && index >= cutRowsUntil
		end := min(table.rowGroupEnd(index, keepLines, keepRows), footer)
		groupHeight := float32(0.0)
		for r := index; r < end; r++ {
			groupHeight += heights[r]
		}
		if page != nil && (y+groupHeight) > bottom {
			if keepLines && groupHeight > bottom-top {
				// Rows that would not fit the next page either are drawn
				// from here: first each on its own, and then, for a row that
				// is taller than a page, each line on its own, cut where the
				// page ends.
				if keepRows {
					cutRowsUntil = end
				} else {
					cutLinesUntil = end
				}
				continue
			}
			// A row that does not fit goes on the next page, unless it is
			// the first row of this one: a row taller than the page fits no
			// page, and leaving it for the next page would ask for pages
			// forever.
			if index > first {
				table.rendered = index
				table.setFooterSums(first, index)
				return [2]float32{x, table.drawFooterRows(page, x, y, heights, false)}
			}
		}
		for r := index; r < end; r++ {
			if page != nil {
				table.drawRow(page, table.tableData[r], x, y, heights, r, structelem.TD)
			}
			y += heights[r]
		}
		index = end
	}
	if !done {
		// The rows of the table are all drawn, and the footer rows end it.
		table.setFooterSums(first, footer)
		y = table.drawFooterRows(page, x, y, heights, true)
	}
	if page != nil {
		table.rendered = -1 // We are done!
	}
	return [2]float32{x, y}
}

// drawFooterRows draws the footer rows at y and returns the y under them. In
// a PDF/UA document they are rows of the table where it ends, and artifacts on
// the pages before.
func (table *Table) drawFooterRows(page *Page, x, y float32, heights []float32, last bool) float32 {
	footer := table.footerStart()
	artifact := page != nil && !last && footer < len(table.tableData)
	if artifact {
		page.AddArtifactBMC()
	}
	cellStructure := structelem.StructElem("")
	if last {
		cellStructure = structelem.TD
	}
	for r := footer; r < len(table.tableData); r++ {
		if page != nil {
			if r == footer {
				for _, cell := range table.tableData[r] {
					cell.properties |= border.Top
				}
			}
			table.drawRow(page, table.tableData[r], x, y, heights, r, cellStructure)
		}
		y += heights[r]
	}
	if artifact {
		page.AddEMC()
	}
	return y
}

// applyRowSpans works out what each cell that spans rows covers, after the
// text is wrapped: a row of the table is drawn as one row for each line its
// tallest cell needs, so a cell that spans two rows of the table spans as many
// rows of the drawing as those two were wrapped into. The cells the span
// covers are marked, and draw nothing.
func (table *Table) applyRowSpans() {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.properties &^= cellCovered
			cell.rowsSpanned = 1
		}
	}
	for r := 0; r < len(table.tableData); r++ {
		if table.isContinuation(r) {
			continue // A span starts in a row of the table, not in the wrap of one.
		}
		row := table.tableData[r]
		for i := 0; i < len(row); {
			cell := row[i]
			colspan := cell.GetColSpan()
			if colspan < 1 {
				colspan = 1
			}
			if cell.GetRowSpan() > 1 && (cell.properties&cellCovered) == 0 {
				end := table.rowAfter(r, cell.GetRowSpan())
				ownEnd := table.rowAfter(r, 1)
				cell.rowsSpanned = end - r
				// The rows the wrapped text of this cell takes keep their text
				// and lose the border that would cross the cell.
				for r2 := r + 1; r2 < ownEnd; r2++ {
					table.setSpanned(r2, i, colspan, false)
				}
				for r2 := ownEnd; r2 < end; r2++ {
					table.setSpanned(r2, i, colspan, true)
				}
			}
			i += colspan
		}
	}
}

// setSpanned marks the columns of the row that a span covers: a covered cell
// draws nothing, and a row of the wrapped text of the spanning cell keeps its
// text without the border under it.
func (table *Table) setSpanned(r, column, colspan int, covered bool) {
	row := table.tableData[r]
	for i := 0; i < len(row); {
		cell := row[i]
		if i >= column && i < column+colspan {
			if covered {
				cell.properties |= cellCovered
			} else {
				cell.properties &^= border.Bottom
			}
		}
		colspan2 := cell.GetColSpan()
		if colspan2 < 1 {
			colspan2 = 1
		}
		i += colspan2
	}
}

// rowAfter returns the index of the row after the count rows of the table that
// start at r, counting the rows the wrapped text of each of them takes.
func (table *Table) rowAfter(r, count int) int {
	index := r
	for i := 0; i < count && index < len(table.tableData); i++ {
		index++
		for index < len(table.tableData) && table.isContinuation(index) {
			index++
		}
	}
	return index
}

// isContinuation is true when the row holds the wrapped text of the row above it.
func (table *Table) isContinuation(r int) bool {
	row := table.tableData[r]
	return len(row) > 0 && (row[0].properties&cellContinued) != 0
}

// isKeptWithNext is true when the row, or the row whose wrapped text it holds,
// is kept with the next row.
func (table *Table) isKeptWithNext(r int) bool {
	row := table.tableData[r]
	return len(row) > 0 && (row[0].properties&cellKeptWithNext) != 0
}

// getRowHeights returns the height of each row of the table as it is drawn. A
// cell that spans rows is not what makes its first row tall; the rows it
// covers hold it together, and the last of them grows when they do not.
func (table *Table) getRowHeights() []float32 {
	heights := make([]float32, len(table.tableData))
	for r, row := range table.tableData {
		heights[r] = table.getMaxCellHeight(row)
	}
	for r, row := range table.tableData {
		for i, cell := range row {
			if cell.rowsSpanned < 2 {
				continue
			}
			end := r + cell.rowsSpanned
			if end > len(table.tableData) {
				end = len(table.tableData)
			}
			have := float32(0.0)
			for r2 := r; r2 < end; r2++ {
				have += heights[r2]
			}
			needed := cell.GetHeight(getTotalWidth(row, i))
			if needed > have && end > r {
				heights[end-1] += needed - have
			}
		}
	}
	return heights
}

// rowGroupEnd returns the row after the rows that a span holds together, with
// keepLines the lines the last of them wraps into, and with keepRows the rows
// that the last of them is kept with, which a page break keeps on one page.
func (table *Table) rowGroupEnd(index int, keepLines, keepRows bool) int {
	end := index + 1
	for r := index; r < end && r < len(table.tableData); r++ {
		for _, cell := range table.tableData[r] {
			if r+cell.rowsSpanned > end {
				end = r + cell.rowsSpanned
			}
		}
		if r+1 == end && end < len(table.tableData) {
			if keepLines && table.isContinuation(end) {
				end++
			} else if keepRows && table.isKeptWithNext(r) {
				end++
			}
		}
	}
	if end > len(table.tableData) {
		end = len(table.tableData)
	}
	return end
}

func (table *Table) getMaxCellHeight(row []*Cell) float32 {
	var maxCellHeight float32 = 0.0
	var spanned float32 = 0.0
	for i, cell := range row {
		if (cell.properties & cellCovered) != 0 {
			continue // A cell the one above it draws over.
		}
		cellHeight := cell.GetHeight(getTotalWidth(row, i))
		if cell.rowsSpanned > 1 {
			// A cell that spans rows is as tall as all of them together, which
			// getRowHeights shares out; it is the height of the row only when
			// nothing else is in it.
			if h := cellHeight / float32(cell.rowsSpanned); h > spanned {
				spanned = h
			}
			continue
		}
		if cellHeight > maxCellHeight {
			maxCellHeight = cellHeight
		}
	}
	if maxCellHeight > 0.0 {
		return maxCellHeight
	}
	return spanned
}

// hasMoreData returns true if the table contains more data that needs to be drawn on a page.
func (table *Table) hasMoreData() bool {
	return table.rendered != -1
}

// GetWidth returns the width of the table when drawn on a page.
// Returns the width of the table.
func (table *Table) GetWidth() float32 {
	var tableWidth float32
	if len(table.tableData) > 0 {
		for _, cell := range table.tableData[0] {
			tableWidth += cell.GetWidth()
		}
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

// SetCellBorderColor sets the color of the cell border lines.
//   - color: the color of the cell border lines.
func (table *Table) SetCellBorderColor(color int32) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetBorderColor(color)
		}
	}
	return table
}

// SetCellBorderColorRGB sets the color of the cell border lines from red,
// green and blue values.
func (table *Table) SetCellBorderColorRGB(rgbColor [3]float32) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetBorderColorRGB(rgbColor)
		}
	}
	return table
}

// SetCellBorderWidth sets the width of the cell border lines.
//   - width: the width of the border lines.
func (table *Table) SetCellBorderWidth(width float32) *Table {
	for _, row := range table.tableData {
		for _, cell := range row {
			cell.SetBorderWidth(width)
		}
	}
	return table
}

// Sets the right border on all cells in the last column.
func (table *Table) setRightBorderOnLastColumn() {
	for _, row := range table.tableData {
		if len(row) > 0 && row[0].properties&border.Left == 0 {
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
		if cell != nil {
			cell.properties |= border.Right
		}
	}
}

// Sets the bottom border on all cells in the last row.
func (table *Table) setBottomBorderOnLastRow() {
	if len(table.tableData) == 0 {
		return
	}
	firstRow := table.tableData[0]
	for _, cell := range firstRow {
		if cell.properties&border.Top == 0 {
			return
		}
	}
	// Only run this code if all the cells in the first row have top border.
	lastRow := table.tableData[len(table.tableData)-1]
	for _, cell := range lastRow {
		cell.properties |= border.Bottom
	}
}

// AutoAdjustColumnWidths adjusts the widths of all columns so that they are just wide enough to
// hold the text without truncation.
func (table *Table) AutoAdjustColumnWidths() *Table {
	if len(table.tableData) == 0 {
		return table
	}
	maxColWidths := make([]float32, len(table.tableData[0]))
	for _, row := range table.tableData {
		for i := 0; i < len(row); i++ {
			cell := row[i]
			if cell.GetColSpan() == 1 {
				if textBlock := cell.GetTextBlock(); textBlock != nil {
					tokens := splitOnWhitespace(textBlock.textContent)
					for _, token := range tokens {
						tokenWidth := textBlock.font.StringWidthUsingFallbackFont(
							textBlock.fallbackFont, textBlock.font.size, token)
						tokenWidth += cell.paddingAt(cellLeftPadding) + cell.paddingAt(cellRightPadding)
						if tokenWidth > maxColWidths[i] {
							maxColWidths[i] = tokenWidth
						}
					}
				} else if cell.drawable != nil {
					drawableWidth := measureDrawable(cell.drawable)[0] + cell.paddingAt(cellLeftPadding) + cell.paddingAt(cellRightPadding)
					if drawableWidth > maxColWidths[i] {
						maxColWidths[i] = drawableWidth
					}
				} else if cell.hasText {
					textWidth := cell.font.StringWidthUsingFallbackFont(cell.fallbackFont, cell.fontSize, cell.text)
					textWidth += cell.paddingAt(cellLeftPadding) + cell.paddingAt(cellRightPadding)
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

func getTotalWidth(row []*Cell, index int) float32 {
	cell := row[index]
	colspan := cell.GetColSpan()
	cellWidth := float32(0.0)
	for i := 0; i < colspan; i++ {
		cellWidth += row[index+i].GetWidth()
	}
	cellWidth -= cell.paddingAt(cellLeftPadding) + row[index+(colspan-1)].paddingAt(cellRightPadding)
	return cellWidth
}

// wrapAroundCellText wraps around the text in all cells so it fits the column width.
// This method should be called after all calls to setColumnWidth and autoAdjustColumnWidths.
func (table *Table) wrapAroundCellText() {
	tableData2 := make([][]*Cell, 0)
	lines := make([][]string, 0)
	numOfHeaderRows2 := 0
	// The footer rows are the rows of the wrap of the rows they were.
	footer := len(table.tableData)
	if table.numOfFooterRows > 0 {
		footer = table.footerStart()
	}
	footer2 := 0
	for r, row := range table.tableData {
		first := len(tableData2)
		if r == footer {
			footer2 = first
		}
		tableData2 = append(tableData2, row) // Add the original row
		// Every cell of the row is wrapped once, here. The lines it needs are
		// what the cells stacked below it get, and the most lines any cell of
		// the row needs is how many rows to stack. The rows added below a
		// header row are header rows too.
		lines = lines[:0]
		maxNumVerCells := 1
		for i := 0; i < len(row); i++ {
			var cellLines []string
			// A cell that draws a line of text of its own draws no cell text,
			// so there is nothing to wrap, and a cell that shows a sum is not
			// wrapped either, as its text is the sum of each page.
			_, hasLine := row[i].drawable.(BaselineDrawable)
			if row[i].hasText && !hasLine && row[i].properties&cellSumBits == 0 {
				cellLines = wrapCellText(row, i)
				if len(cellLines) > maxNumVerCells {
					maxNumVerCells = len(cellLines)
				}
			}
			lines = append(lines, cellLines)
		}
		// A cell whose text wraps is one cell drawn as the rows its lines
		// take, so the border under it belongs under the last of them and not
		// under every line of it. A cell that spans rows is drawn over all of
		// them at once and so draws its own bottom border under the whole of
		// it; applyRowSpans clears the rows of its wrap.
		bottomBorder := make([]bool, len(row))
		for i, cell := range row {
			bottomBorder[i] = maxNumVerCells > 1 && cell.GetRowSpan() == 1 &&
				cell.GetBorder(border.Bottom)
			if bottomBorder[i] {
				cell.SetBorder(border.Bottom, false)
			}
		}
		for i := 1; i < maxNumVerCells; i++ {
			row2 := make([]*Cell, 0)
			for j, cell := range row {
				cell2 := NewCell(cell.GetFont(), "")
				cell2.hasText = false // Java's new Cell(font) has no text
				cell2.SetFallbackFont(cell.GetFallbackFont())
				cell2.SetFontSize(cell.fontSize)
				cell2.SetWidth(cell.GetWidth())
				cell2.SetLeftPadding(cell.GetLeftPadding())
				cell2.SetRightPadding(cell.GetRightPadding())
				cell2.backgroundColor = cell.backgroundColor
				cell2.SetBorderWidth(cell.GetBorderWidth())
				cell2.borderColor = cell.borderColor
				cell2.textColor = cell.textColor
				cell2.SetColSpan(cell.GetColSpan())
				cell2.properties = cell.properties
				cell2.SetTextAlignment(cell.GetTextAlignment())
				cell2.SetVerticalAlignment(cell.GetVerticalAlignment())
				cell2.SetTopPadding(0.0)
				cell2.properties &= ^border.Top
				cell2.SetBorder(border.Bottom, bottomBorder[j] && i == maxNumVerCells-1)
				cell2.properties |= cellContinued
				cell2.properties &^= cellSumBits
				row2 = append(row2, cell2)
			}
			tableData2 = append(tableData2, row2)
		}
		for j := 0; j < len(row); j++ {
			for n, line := range lines[j] {
				tableData2[first+n][j].SetText(line)
			}
		}
		// The stacked rows are wrapped in their turn, as they were when the
		// rows were all added first and the table wrapped in one pass
		// afterwards: a line that ends in a space loses it here.
		for i := first + 1; i < len(tableData2); i++ {
			row2 := tableData2[i]
			for j := 0; j < len(row2); j++ {
				if row2[j].hasText {
					for n, line := range wrapCellText(row2, j) {
						tableData2[i+n][j].SetText(line)
					}
				}
			}
		}
		if r < table.numOfHeaderRows {
			numOfHeaderRows2 = len(tableData2)
		}
	}
	if table.rendered != -1 {
		table.rendered += numOfHeaderRows2 - table.numOfHeaderRows
	}
	table.numOfHeaderRows = numOfHeaderRows2
	if footer < len(table.tableData) {
		table.numOfFooterRows = len(tableData2) - footer2
	}
	table.tableData = tableData2
}

// wrapCellText returns the lines the text of the cell needs to fit the width
// of its column. A token wider than the column is broken between two of its
// characters.
func wrapCellText(row []*Cell, index int) []string {
	cell := row[index]
	cellWidth := getTotalWidth(row, index)
	lines := make([]string, 0, 1)
	var buf strings.Builder
	for _, token := range splitOnWhitespace(cell.text) {
		if cell.font.StringWidthUsingFallbackFont(cell.fallbackFont, cell.fontSize, token) > cellWidth {
			if buf.Len() > 0 {
				buf.WriteString(" ")
			}
			for _, ch := range token {
				if cell.font.StringWidthUsingFallbackFont(cell.fallbackFont,
					cell.fontSize, buf.String()+string(ch)) > cellWidth {
					lines = append(lines, buf.String())
					buf.Reset()
				}
				buf.WriteRune(ch)
			}
		} else if buf.Len() == 0 {
			// A token that fits the column fits a line of its own, and its
			// width is the one measured just above.
			buf.WriteString(token)
		} else if cell.font.StringWidthUsingFallbackFont(cell.fallbackFont,
			cell.fontSize, trimSpace(buf.String()+" "+token)) > cellWidth {
			lines = append(lines, trimSpace(buf.String()))
			buf.Reset()
			buf.WriteString(token)
		} else {
			buf.WriteString(" ")
			buf.WriteString(token)
		}
	}
	return append(lines, trimSpace(buf.String()))
}

// getNumVerCells returns the number of vertically stacked cells that the
// wrapped text of the cell needs.
func getNumVerCells(row []*Cell, index int) int {
	if !row[index].hasText {
		return 1
	}
	return len(wrapCellText(row, index))
}

// getDelimiter returns the delimiter of the line: the commonest of a comma, a
// pipe and a tab. The ones inside a quoted field are not counted, or a file
// whose values hold commas could be split on the wrong character altogether.
func getDelimiter(str string) string {
	comma := 0
	pipe := 0
	tab := 0
	quoted := false
	for _, ch := range str {
		if ch == '"' {
			quoted = !quoted
			continue
		}
		if quoted {
			continue
		}
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
