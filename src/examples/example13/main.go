package main

import (
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example13 draws a table that spans multiple pages.
func Example13() {
	pdf := pdfjet.NewPDFFile("Example_13.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(7.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetSize(7.0)

	tableData := make([][]*pdfjet.Cell, 0)
	lines := pdfjet.ReadTextLines("data/winter-2009.txt")
	for _, line := range lines {
		row := make([]*pdfjet.Cell, 0)
		for _, column := range strings.Split(line, "|") {
			row = append(row, pdfjet.NewCell(f2, column))
		}
		tableData = append(tableData, row)
	}

	table := pdfjet.NewTable()
	table.SetData(tableData, pdfjet.TableWith2HeaderRows)
	table.SetLocation(100.0, 50.0)
	table.SetBottomMargin(10.0)

	table.SetFontInRow(0, f1)
	table.SetFontInRow(1, f1)

	table.SetColumnWidths()
	table.RemoveLineBetweenRows(0, 1)

	cell := table.GetCellAt(1, 1)
	cell.SetTopBorder(true)

	cell = table.GetCellAt(1, 2)
	cell.SetTopBorder(true)

	cell = table.GetCellAt(0, 1)
	cell.SetColSpan(2)
	cell.SetTextAlignment(alignment.Center)

	column := table.GetColumnAtIndex(7)
	for _, cell := range column {
		cell.SetTextAlignment(alignment.Center)
	}

	column = table.GetColumnAtIndex(4)
	for i := 2; i < len(column); i++ {
		cell := column[i]
		cell.SetTextAlignment(alignment.Center)
		if n, err := strconv.Atoi(cell.GetText()); err == nil && n > 40 {
			cell.SetBackgroundColor(color.DarkSeaGreen)
		} else {
			cell.SetBackgroundColor(color.Yellow)
		}
	}

	column = table.GetColumnAtIndex(2)
	for i := 2; i < len(column); i++ {
		cell := column[i]
		if cell.GetText() == "Smith" {
			cell.SetUnderline(true)
		}
		if cell.GetText() == "Bowden" {
			cell.SetStrikeout(true)
		}
	}

	column = table.GetColumnAtIndex(2)
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

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait)
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1,
			"Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func blankOutColumn(table *pdfjet.Table, index int) {
	for _, cell := range table.GetColumnAtIndex(index) {
		cell.SetBackgroundColor(color.White)
		cell.SetTopBorder(false)
		cell.SetBottomBorder(false)
	}
}

func setBgColorForRow(table *pdfjet.Table, index int, color int32) {
	for _, cell := range table.GetRowAtIndex(index) {
		cell.SetBackgroundColor(color)
	}
}

func main() {
	start := time.Now()
	Example13()
	pdfjet.PrintDuration("Example_13", time.Since(start))
}
