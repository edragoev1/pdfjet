// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexMono"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example38 draws a table whose cells span columns and rows, and explains how.
// A cell spans columns with setColSpan and rows with setRowSpan. The table is
// also a check of the geometry of the cells: their backgrounds meet without
// gaps and their borders line up.
func Example38() {
	pdf, err := pdfjet.NewPDFFile("Example_38.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Table Cells That Span Rows and Columns")
	font := pdfjet.NewFontFromFile(pdf, IBMPlexMono.Regular)
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
			"up.")
	textBlock.SetFontSize(11.0)
	textBlock.SetLineSpacing(1.3)
	textBlock.SetLocation(50.0, 65.0)
	textBlock.SetWidth(500.0)
	xy := textBlock.DrawOn(page)

	table := pdfjet.NewTable()
	table.SetTableData(createTableData(font), 0)
	table.SetBottomMargin(10.0)
	table.SetLocation(50.0, xy[1]+20.0)
	table.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// createTableData returns the cells of a 10 by 10 table whose cells span
// columns and rows. It is the table of this HTML, cell for cell:
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
func createTableData(font *pdfjet.Font) [][]*pdfjet.Cell {
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
	return grid
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
	Example38()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_38 => %4d ms\n", time1-time0)
}
