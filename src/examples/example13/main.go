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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example13 draws a table that spans multiple pages.
func Example13() {
	pdf, err := pdfjet.NewPDFFile("Example_13.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Winter Reading Scores")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(7.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(7.0)

	tableData := make([][]*pdfjet.Cell, 0)
	lines := content.LinesOfTextFile("data/winter-2009.txt")
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

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1,
			"Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages))))
		pdf.AddPage(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
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

func main() {
	time0 := time.Now().UnixMilli()
	Example13()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_13 => %4d ms\n", time1-time0)
}
