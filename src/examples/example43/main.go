package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example43 demonstrates creating a PDF with a big table
func Example43() {
	pdf := pdfjet.NewPDFFile("Example_43.pdf")
	pdf.SetCompliance(compliance.PDF_UA)

	// Used for performance testing. Results in 2000+ pages PDF.
	fileName := "data/Electric_Vehicle_Population_Data.csv"
	// fileName := "data/Electric_Vehicle_Population_10_Pages.csv"

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream")
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
	f2.SetSize(9.0)

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Landscape)
	table.SetNumberOfColumns(9)       // The order of the
	table.SetTableData(fileName, ",") // these statements
	table.SetLocation(0.0, 0.0)       // is
	table.SetBottomMargin(20.0)       // very
	table.Complete()                  // important!

	pages := table.GetPages()
	for i, page := range pages {
		footer := pdfjet.NewTextLine(f1, fmt.Sprintf("Page %d of %d", i+1, len(pages)))
		page.AddFooter(footer)
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example43()
	pdfjet.PrintDuration("Example_43", time.Since(start))
}
