package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example23 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example23() {
	pdf := pdfjet.NewPDFFile("Example_23.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream")

	// What is this?
	f3.SetSize(7.0 * 0.583)

	image1 := pdfjet.NewImageFromFile(pdf, "images/mt-map.png")
	image1.ScaleBy(0.75)

	tableData := make([][]*pdfjet.Cell, 0)

	row := make([]*pdfjet.Cell, 0)
	row = append(row, pdfjet.NewCell(f1, "Hello"))
	row = append(row, pdfjet.NewCell(f1, "World"))
	row = append(row, pdfjet.NewCell(f1, "Next Column"))
	row = append(row, pdfjet.NewCell(f1, "CompositeTextLine"))
	tableData = append(tableData, row)

	row = make([]*pdfjet.Cell, 0)
	row = append(row, pdfjet.NewCell(f2, "This is a test:"))
	// textBox := TextBox(f2,
	// 	"Here we are going to test the wrapAroundCellTextmethod.\n\nWe will create a table and place it near the bottom of the page. When we draw this table the text will wrap around the column edge and stay within the column.\n\nSo - let's  see how this is working?")
	// textBox.SetTextAlignment(align.Right)
	// cell := pdfjet.NewCell(f2, "Yahoo! AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA Hello World!")
	cell := pdfjet.NewCell(f2, "Yahoo! aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaAAAAAAAAAAAAAA Hello World!")
	cell.SetBgColor(color.Aliceblue)
	// cell.SetTextBox(textBox)
	cell.SetColSpan(2)
	row = append(row, cell)
	row = append(row, pdfjet.NewCell(f2, "")) // We need an empty cell here because the previous cell had colSpan == 2
	row = append(row, pdfjet.NewCell(f2, "Test 456"))
	tableData = append(tableData, row)

	table := pdfjet.NewTable()
	table.SetData(tableData, 1) // One header row
	table.SetLocation(50.0, 50.0)
	table.SetFirstPageTopMargin(650.0)
	table.SetBottomMargin(15.0)
	table.SetColumnWidth(0, 100.0)
	table.SetColumnWidth(1, 100.0)
	table.SetColumnWidth(2, 100.0)
	table.SetColumnWidth(3, 150.0)

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait)
	for _, page := range pages {
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example23()
	pdfjet.PrintDuration("Example_23", time.Since(start))
}
