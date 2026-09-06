package main

import (
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/effect"
)

// Example15 draws a table whose cells hold composite text lines.
func Example15() {
	pdf := pdfjet.NewPDFFile("Example_15.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PDF/UA compliant PDF")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f4 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f5 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	tableData := make([][]*pdfjet.Cell, 0)
	for i := 0; i < 60; i++ {
		row := make([]*pdfjet.Cell, 0)
		for j := 0; j < 5; j++ {
			var cell *pdfjet.Cell
			if i == 0 {
				cell = pdfjet.NewCell(f1, "")
			} else {
				cell = pdfjet.NewCell(f2, "")
			}

			cell.SetTopPadding(10.0)
			cell.SetBottomPadding(10.0)
			cell.SetLeftPadding(10.0)
			cell.SetRightPadding(10.0)

			cell.SetText("Hello " + strconv.Itoa(i) + " " + strconv.Itoa(j))

			composite := pdfjet.NewCompositeTextLine(0.0, 0.0)
			composite.SetFontSize(12.0)
			line1 := pdfjet.NewTextLine(f3, "H")
			line2 := pdfjet.NewTextLine(f4, "2")
			line3 := pdfjet.NewTextLine(f5, "O")

			line2.SetTextEffect(effect.Subscript)

			composite.AddComponent(line1)
			composite.AddComponent(line2)
			composite.AddComponent(line3)

			if i == 0 || j == 0 {
				cell.SetCompositeTextLine(composite)
				cell.SetBackgroundColor(color.DeepSkyBlue)
			} else {
				cell.SetBackgroundColor(color.DodgerBlue)
			}
			cell.SetPenColor([3]float32{0.83, 0.83, 0.83}) // Light gray
			cell.SetTextColor([3]float32{0.0, 0.0, 0.0})   // Black
			row = append(row, cell)
		}
		tableData = append(tableData, row)
	}

	table := pdfjet.NewTable()
	table.SetData(tableData, pdfjet.TableWith2HeaderRows)
	table.SetBottomMargin(15.0)
	table.SetLocation(70.0, 30.0)
	table.SetColumnWidths()

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, a4.Portrait)
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1,
			"Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example15()
	pdfjet.PrintDuration("Example_15", time.Since(start))
}
