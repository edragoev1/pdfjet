// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/CascadiaMono"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example21 draws two tables. The first, of reading scores, has two header rows, a
// header cell that spans two columns, and cells with background colors. The
// second spans its cells over columns, with setColSpan, and over rows, with
// setRowSpan.
func Example21() {
	pdf, err := pdfjet.NewPDFFile("Example_21.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Table Headers and Spans")

	drawHeaderRows(pdf)
	drawSpans(pdf)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func drawHeaderRows(pdf *pdfjet.PDF) {
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(7.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(7.0)

	tableData := make([][]*pdfjet.Cell, 0)
	lines := content.LinesOfTextFile("resources/data/winter-2009.txt")
	for _, line := range lines {
		row := make([]*pdfjet.Cell, 0)
		for _, column := range strings.Split(line, "|") {
			row = append(row, pdfjet.NewCell(f2, column))
		}
		tableData = append(tableData, row)
	}

	table := pdfjet.NewTable()
	table.SetTableData(tableData, 2)
	table.SetLocation(100.0, 50.0)
	table.SetBottomMargin(10.0)

	table.SetFontInRow(0, f1)
	table.SetFontInRow(1, f1)

	table.AutoAdjustColumnWidths()
	table.RemoveLineBetweenRows(0, 1)

	cell := table.GetCellAt(1, 1)
	cell.SetBorder(border.Top, true)

	cell = table.GetCellAt(1, 2)
	cell.SetBorder(border.Top, true)

	cell = table.GetCellAt(0, 1)
	cell.SetColSpan(2)
	cell.SetTextAlignment(alignment.Center)

	column := table.GetColumn(7)
	for _, cell := range column {
		cell.SetTextAlignment(alignment.Center)
	}

	column = table.GetColumn(4)
	for i := 2; i < len(column); i++ {
		cell := column[i]
		cell.SetTextAlignment(alignment.Center)
		n, err := strconv.Atoi(cell.GetText())
		if err != nil {
			continue
		}
		if n > 40 {
			cell.SetBackgroundColor(color.DarkSeaGreen)
		} else {
			cell.SetBackgroundColor(color.Yellow)
		}
	}

	column = table.GetColumn(2)
	for i := 2; i < len(column); i++ {
		cell := column[i]
		if cell.GetText() == "Smith" {
			cell.SetUnderline(true)
		}
		if cell.GetText() == "Bowden" {
			cell.SetStrikeout(true)
		}
	}

	column = table.GetColumn(2)
	for i := 2; i < len(column); i++ {
		cell := column[i]
		if cell.GetText() == "Bowden" {
			cell.SetStrikeout(false)
		}
	}

	setBgColorForRow(table, 0, color.LightGray)
	setBgColorForRow(table, 1, color.LightGray)

	table.SetColumnWidth(3, 10.0)
	blankOutColumn(table, 3)

	table.SetColumnWidth(8, 10.0)
	blankOutColumn(table, 8)

	// A heading on the first page only, above the table: the first page is
	// made here, and the table goes on to new pages.
	first := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pdfjet.NewTextLine(f1, "Winter Reading Scores").
		SetStructureType(structelem.H1).
		SetFontSize(16.0).
		SetLocation(100.0, 38.0).
		DrawOn(first)
	table.SetFirstPageTopMargin(50.0)

	pages := []*pdfjet.Page{first}
	table.DrawOnPagesFrom(pdf, first, &pages, letter.Portrait())
	for _, page := range pages {
		pdf.AddPage(page)
	}
}

func drawSpans(pdf *pdfjet.PDF) {
	font := pdfjet.NewFontFromFile(pdf, CascadiaMono.Regular)
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Landscape())

	title := pdfjet.NewTextLine(f1, "Table Cells That Span Rows and Columns")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(18.0)
	title.SetLocation(50.0, 50.0)
	title.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f2,
		"The cells of this table span up to five columns and up to four rows, as "+
			"the name in each cell says: 1x3 is one column wide and three rows tall. "+
			"A cell spans columns with setColSpan and rows with setRowSpan, and draws "+
			"its text, its background and its borders once over all of them. The table "+
			"keeps its shape, so every row holds a cell for every column and the cells "+
			"a span covers are left empty. The example is also a check of the geometry "+
			"of the cells: their backgrounds meet without gaps and their borders line "+
			"up. The header row above them numbers the columns.")
	textBlock.SetFontSize(11.0)
	textBlock.SetLineSpacing(1.3)
	textBlock.SetLocation(50.0, 65.0)
	textBlock.SetWidth(500.0)
	xy := textBlock.DrawOn(page)

	table := pdfjet.NewTable()
	table.SetTableData(createTableData(font, f1), 1)
	table.SetBottomMargin(10.0)
	table.SetLocation(50.0, xy[1]+20.0)
	table.DrawOn(page)
}

func blankOutColumn(table *pdfjet.Table, index int) {
	for _, cell := range table.GetColumn(index) {
		cell.SetBackgroundColor(color.White)
		cell.SetBorder(border.Top, false)
		cell.SetBorder(border.Bottom, false)
	}
}

func setBgColorForRow(table *pdfjet.Table, index int, color int32) {
	for _, cell := range table.GetRow(index) {
		cell.SetBackgroundColor(color)
	}
}

// createTableData returns a header row that numbers the columns, in the
// header font, and under it the cells of a 10 by 10 table whose cells span
// columns and rows. They are the table of this HTML, cell for cell:
//
//	<table border="solid">
//	<tr><td colspan="2" rowspan="2">2x2</td><td colspan="2">2x1</td>
//	    <td colspan="2">2x1</td><td colspan="2">2x1</td><td colspan="2">2x1</td></tr>
//	<tr><td colspan="2" rowspan="2">2x2</td><td>1x1</td><td colspan="5">5x1</td></tr>
//	<tr><td rowspan="2">1x2</td><td>1x1</td><td colspan="2" rowspan="2">2x2</td>
//	    <td rowspan="2">1x2</td><td colspan="3">3x1</td></tr>
//	<tr><td>1x1</td><td rowspan="3">1x3</td><td>1x1</td><td colspan="2">2x1</td>
//	    <td rowspan="2">1x2</td></tr>
//	<tr><td rowspan="2">1x2</td><td>1x1</td><td colspan="2">2x1</td>
//	    <td colspan="4" rowspan="4">4x4</td></tr>
//	<tr><td>1x1</td><td rowspan="3">1x3</td><td rowspan="3">1x3</td>
//	    <td rowspan="3">1x3</td></tr>
//	<tr><td rowspan="2">1x2</td><td>1x1</td><td rowspan="4">1x4</td></tr>
//	<tr><td>1x1</td></tr>
//	<tr><td rowspan="2">1x2</td><td>1x1</td><td colspan="2">2x1</td>
//	    <td colspan="2" rowspan="2">2x2</td><td rowspan="2">1x2</td><td>1x1</td>
//	    <td>1x1</td></tr>
//	<tr><td>1x1</td><td>1x1</td><td>1x1</td><td>1x1</td><td>1x1</td></tr>
//	</table>
func createTableData(font, headerFont *pdfjet.Font) [][]*pdfjet.Cell {
	// The columns and the rows each cell spans, in the order a browser reads
	// the cells of the HTML above.
	spans := [][][2]int{
		{{2, 2}, {2, 1}, {2, 1}, {2, 1}, {2, 1}},
		{{2, 2}, {1, 1}, {5, 1}},
		{{1, 2}, {1, 1}, {2, 2}, {1, 2}, {3, 1}},
		{{1, 1}, {1, 3}, {1, 1}, {2, 1}, {1, 2}},
		{{1, 2}, {1, 1}, {2, 1}, {4, 4}},
		{{1, 1}, {1, 3}, {1, 3}, {1, 3}},
		{{1, 2}, {1, 1}, {1, 4}},
		{{1, 1}},
		{{1, 2}, {1, 1}, {2, 1}, {2, 2}, {1, 2}, {1, 1}, {1, 1}},
		{{1, 1}, {1, 1}, {1, 1}, {1, 1}, {1, 1}},
	}
	columns := 10
	grid := make([][]*pdfjet.Cell, len(spans))
	for r := range grid {
		grid[r] = make([]*pdfjet.Cell, columns)
	}
	for r := range spans {
		c := 0
		for _, span := range spans[r] {
			// The next column that no cell of a row above spans over.
			for c < columns && grid[r][c] != nil {
				c++
			}
			grid[r][c] = getCell(font, span[0], span[1],
				fmt.Sprintf("%dx%d", span[0], span[1]))
			// A table keeps its shape, so every row holds a cell for every
			// column: the cells a span covers are there and are empty.
			for r2 := r; r2 < r+span[1] && r2 < len(spans); r2++ {
				for c2 := c; c2 < c+span[0] && c2 < columns; c2++ {
					if grid[r2][c2] == nil {
						grid[r2][c2] = getCell(font, 1, 1, "")
					}
				}
			}
			c += span[0]
		}
	}
	// The header row, above the cells: it numbers the columns.
	header := make([]*pdfjet.Cell, columns)
	for c := range header {
		header[c] = getCell(headerFont, 1, 1, fmt.Sprintf("%d", c+1))
	}
	return append([][]*pdfjet.Cell{header}, grid...)
}

func getCell(font *pdfjet.Font, colSpan, rowSpan int, text string) *pdfjet.Cell {
	cell := pdfjet.NewCell(font, text)
	cell.SetColSpan(colSpan)
	cell.SetRowSpan(rowSpan)
	cell.SetWidth(50.0)
	cell.SetBorder(border.Top, true)
	cell.SetBorder(border.Bottom, true)
	cell.SetBorder(border.Left, true)
	cell.SetBorder(border.Right, true)
	cell.SetTextAlignment(alignment.Center)
	cell.SetVerticalAlignment(alignment.Center)
	cell.SetBackgroundColor(0xD8F0E4) // A pastel mint
	cell.SetBorderWidth(1.0)
	return cell
}

func main() {
	time0 := time.Now().UnixMilli()
	Example21()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_21 => %4d ms\n", time1-time0)
}
