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

// Example51 draws eight columns of the Electric Vehicle data of Example_24 on
// portrait pages, with SetWrapText: a field too long for its column wraps
// onto more lines, the row as tall as its tallest field, so no column is cut
// short and none is lost off the page; the memory stays flat, a row at a time.
func Example51() {
	pdf, err := pdfjet.NewPDFFile("Example_51.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicle Population Data, wrapped") // Required for PDF/UA !

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f1.SetSize(9.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Portrait())
	// Make, Model, Model Year, City, Electric Vehicle Type, Clean Alternative
	// Fuel Vehicle (CAFV) Eligibility, Electric Range and Electric Utility
	table.SetColumns(6, 7, 5, 2, 8, 9, 10, 15)
	table.SetWrapText(true)
	_, err = table.SetTableData("resources/data/Electric_Vehicle_Population_10_Pages.csv", ",")
	if err != nil {
		log.Printf("Failed to load table data: %v", err)
		return
	}
	table.SetVerticalPadding(2.0)
	table.SetLocation(20.0, 20.0)
	table.SetBottomMargin(30.0)
	err = table.Complete()
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
	Example51()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_51 => %4d ms\n", time1-time0)
}
