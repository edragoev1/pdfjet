// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pdf417"
)

// Example12 draws a PDF417 barcode that holds a whole source file, with an
// explanation and a caption.
func Example12() {
	pdf, err := pdfjet.NewPDFFile("Example_12.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PDF417 barcode example")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "PDF417 Barcode")
	text.SetFontSize(22.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"PDF417 is a stacked two-dimensional barcode, used on boarding passes, "+
			"identity cards and shipping labels. It holds text and binary data, "+
			"and its error correction lets a scanner read it even when part of "+
			"it is damaged.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(70.0, 95.0)
	textBlock.SetWidth(470.0)
	xy := textBlock.DrawOn(page)

	// A barcode that holds a whole source file.
	lines := content.LinesOfTextFile("data/Example_12.java")
	var buf strings.Builder
	for _, line := range lines {
		buf.WriteString(line)
		buf.WriteString("\r\n") // CR and LF are both required!
	}

	barcode := pdf417.NewPDF417(buf.String())
	barcode.SetModuleLength(1.0)
	barcode.SetLocation(70.0, xy[1]+30.0)
	xy = barcode.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "The source code of data/Example_12.java, "+
		fmt.Sprint(len(lines))+" lines")
	text.SetFontSize(10.0)
	text.SetTextColor(color.Gray)
	text.SetLocation(70.0, xy[1]+20.0)
	text.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example12()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_12 => %4d ms\n", time1-time0)
}
