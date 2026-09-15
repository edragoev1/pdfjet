package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example43 draws a very large table across thousands of pages.
func Example43() {
	pdf, err := pdfjet.NewPDFFile("Example_43.pdf")
	if err != nil {
		log.Fatal(err)
	}
	// pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicle Population Data") // Required for PDF/UA !

	// Used for performance testing. Results in 2000+ pages PDF.
	fileName := "data/Electric_Vehicle_Population_Data.csv"
	// fileName := "data/Electric_Vehicle_Population_10_Pages.csv"
	// fileName := "data/Electric_Vehicle_Population_5_Lines.csv"

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(9.0)

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Landscape())
	table.SetNumberOfColumns(9)                // The order of the
	_, err = table.SetTableData(fileName, ",") // these statements
	if err != nil {
		log.Printf("Failed to load table data: %v", err)
		return
	}
	table.SetLocation(0.0, 0.0) // is
	table.SetBottomMargin(20.0) // very
	err = table.Complete()      // important!
	if err != nil {
		log.Printf("Failed to render table: %v", err)
		return
	}

	pages := table.GetPages()
	for i, page := range pages {
		footer := pdfjet.NewTextLine(f1, fmt.Sprintf("Page %d of %d", i+1, len(pages)))
		page.AddFooter(footer)
		pdf.AddPage(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example43()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_43 => %4d ms\n", time1-time0)
}
