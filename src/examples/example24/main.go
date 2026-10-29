// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example24 draws the rows of a CSV file as a table of as many pages as it
// takes, thousands for the full data file.
func Example24(fileName string) {
	pdf, err := pdfjet.NewPDFFile("Example_24.pdf")
	if err != nil {
		log.Fatal(err)
	}
	// Uncomment the line below to make this a PDF/UA document. A BigTable is
	// tagged as a table: a TR for each row, holding a TH or a TD with the
	// text of each cell, which is what a screen reader reads a table from.
	// It is off here because of what it costs with the full data file: every
	// tagged cell is an object of its own, so that document goes from 5,108
	// objects and 11.8 MB to 1.25 million objects and 249 MB. The 10-page
	// sample is the size to see the tagging at.
	// pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicle Population Data") // Required for PDF/UA !

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

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	// The 10-page sample, or the file given as the first argument: the full
	// Electric Vehicle Population Data, 124,716 rows, makes 2,546 pages and
	// is used for performance testing.
	fileName := "resources/data/Electric_Vehicle_Population_10_Pages.csv"
	if len(os.Args) > 1 {
		fileName = os.Args[1]
	}
	time0 := time.Now().UnixMilli()
	Example24(fileName)
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_24 => %4d ms\n", time1-time0)
}
