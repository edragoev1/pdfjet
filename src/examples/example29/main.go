package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example29 draws a table whose cell holds a text column.
func Example29() {
	pdf := pdfjet.NewPDFFile("Example_29.pdf")

	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	font.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	paragraph1 := pdfjet.NewParagraph()
	paragraph1.Add(pdfjet.NewTextLine(font, content.OfTextFile("data/languages/english.txt")))

	paragraph2 := pdfjet.NewParagraph()
	paragraph2.Add(pdfjet.NewTextLine(font, content.OfTextFile("data/languages/greek.txt")))

	column := pdfjet.NewTextColumn(0)
	column.SetLocation(50.0, 50.0)
	column.SetWidth(400.0)
	column.AddParagraph(paragraph1)
	column.AddParagraph(paragraph2)
	// column.DrawOn(page)

	tableData := make([][]*pdfjet.Cell, 0)
	row := make([]*pdfjet.Cell, 0)
	row = append(row, pdfjet.NewCell(font, "Hello"))
	row = append(row, pdfjet.NewCell(font, "World"))
	row[1].SetTextColumn(column)
	tableData = append(tableData, row)

	table := pdfjet.NewTable()
	table.SetData(tableData, pdfjet.TableWith0HeaderRows)
	table.SetLocation(50.0, 50.0)
	table.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example29()
	pdfjet.PrintDuration("Example_29", time.Since(start))
}
