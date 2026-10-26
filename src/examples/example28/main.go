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
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example28 draws a donut chart.
func Example28() {
	pdf, err := pdfjet.NewPDFFile("Example_28.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Fruit Donut Chart")

	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2.SetSize(10.0)

	// A heading, which PAC asks a document for
	pdfjet.NewTextLine(f2, "Fruit Donut Chart").
		SetStructureType(structelem.H1).
		SetFontSize(18.0).
		SetLocation(100.0, 100.0).
		DrawOn(page)

	chart := pdfjet.NewDonutChart(f1, f2)
	chart.SetLocation(100.0, 200.0)
	chart.SetRadii(200.0, 120.0) // an inner radius of 0 makes a pie chart

	chart.SetTitle("Fruit sold this week")
	chart.SetSubtitle("Share of 100 crates, in the default colors")
	chart.AddSlice(pdfjet.NewSliceInPalette(25.0, "Apples"))
	chart.AddSlice(pdfjet.NewSliceInPalette(20.0, "Oranges"))
	chart.AddSlice(pdfjet.NewSliceInPalette(30.0, "Bananas"))
	chart.AddSlice(pdfjet.NewSliceInPalette(15.0, "Grapes"))
	chart.AddSlice(pdfjet.NewSliceInPalette(10.0, "Lemons"))
	chart.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example28()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_28 => %4d ms\n", time1-time0)
}
