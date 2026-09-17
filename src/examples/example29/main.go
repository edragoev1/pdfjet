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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example29 draws a table whose cells hold text columns: each paragraph of
// English and Greek text wraps inside its cell, and the cell grows to fit it.
func Example29() {
	pdf, err := pdfjet.NewPDFFile("Example_29.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Text Columns in Table Cells")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 70.0)
	text.DrawOn(page)

	languages := []string{"English", "Greek"}
	files := []string{"data/languages/english.txt", "data/languages/greek.txt"}

	tableData := make([][]*pdfjet.Cell, 0)

	row := make([]*pdfjet.Cell, 0)
	row = append(row, pdfjet.NewCell(f2, "Language"))
	row = append(row, pdfjet.NewCell(f2, "Text"))
	tableData = append(tableData, row)

	for i := 0; i < len(languages); i++ {
		// Each line of the file after the first two is a paragraph.
		lines := content.LinesOfTextFile(files[i])
		column := pdfjet.NewTextColumn()
		column.SetWidth(400.0)
		for j := 2; j < len(lines); j++ {
			paragraph := pdfjet.NewParagraph()
			paragraph.Add(pdfjet.NewTextLine(f1, lines[j]))
			column.AddParagraph(paragraph)
		}

		row = make([]*pdfjet.Cell, 0)
		row = append(row, pdfjet.NewCell(f1, languages[i]))
		row = append(row, pdfjet.NewCell(f1, ""))
		row[1].SetTextColumn(column)
		tableData = append(tableData, row)
	}

	table := pdfjet.NewTable()
	table.SetTableData(tableData, 1)
	table.SetColumnWidth(0, 90.0)
	table.SetColumnWidth(1, 420.0)
	table.SetLocation(50.0, 100.0)
	table.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example29()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_29 => %4d ms\n", time1-time0)
}
