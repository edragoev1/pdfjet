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
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example43 draws a very large table across thousands of pages.
func Example43() {
	pdf, err := pdfjet.NewPDFFile("Example_43.pdf")
	if err != nil {
		log.Fatal(err)
	}
	// Uncomment the line below to make this a PDF/UA document. A BigTable is
	// tagged as a table: a TR for each row, holding a TH or a TD with the
	// text of each cell, which is what a screen reader reads a table from.
	// It is off here because of what it costs at this size: every tagged
	// cell is an object of its own, so this document goes from 5,108 objects
	// and 11.8 MB to 1.25 million objects and 249 MB. The 10-page file below
	// is the size to see the tagging at.
	// pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicle Population Data") // Required for PDF/UA !

	// Used for performance testing. Results in 2000+ pages PDF.
	fileName := "data/Electric_Vehicle_Population_Data.csv"
	// fileName := "data/Electric_Vehicle_Population_10_Pages.csv"

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
	time0 := time.Now().UnixMilli()
	Example43()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_43 => %4d ms\n", time1-time0)
}
