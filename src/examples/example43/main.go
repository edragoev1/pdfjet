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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example43 draws a large table over many pages, tagged as a table.
func Example43() {
	pdf, err := pdfjet.NewPDFFile("Example_43.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicle Population Data") // Required for PDF/UA !

	// The whole file is 2000+ pages and is the one to time the library with.
	fileName := "data/Electric_Vehicle_Population_10_Pages.csv"
	// fileName := "data/Electric_Vehicle_Population_Data.csv"

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
