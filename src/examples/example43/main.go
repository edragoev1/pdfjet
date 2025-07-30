package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/font"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example43 --
func Example43() {
	pdf := pdfjet.NewPDFFile("Example_43.pdf")
	pdf.SetCompliance(compliance.PDF_UA)

	// Used for performance testing. Results in 2000+ pages PDF.
	// fileName := "data/Electric_Vehicle_Population_Data.csv"
	fileName := "data/Electric_Vehicle_Population_10_Pages.csv"

	f1 := pdfjet.NewFontFromFile(pdf, font.IBMPlexSans.SemiBold)
	f1.SetSize(8.0)

	f2 := pdfjet.NewFontFromFile(pdf, font.IBMPlexSans.Regular)
	f2.SetSize(8.0)

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Landscape)
	table.SetNumberOfColumns(9)
	table.SetTableData(fileName, ",")
	table.SetLocation(0.0, 0.0)
	table.SetBottomMargin(20.0)
	table.Complete()

	pages := table.GetPages()
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+1)+" of "+fmt.Sprint((len(pages)))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example43()
	pdfjet.PrintDuration("Example_43", time.Since(start))
}
