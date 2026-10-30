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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
)

// Example50 does what programs most often do with an existing PDF: it reads
// the PDF, stamps every page APPROVED with a date, and adds page numbers at
// the foot. What is drawn on a page that was read goes on top of its content,
// and the rest of the document stays as it was.
func Example50(fileName string) {
	pdf, err := pdfjet.NewPDFFile("Example_50.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdf.Read(content.OfBinaryFile(fileName))
	if err != nil {
		log.Fatal(err)
	}

	// A font added to an existing PDF is embedded as a subset of the glyphs
	// drawn, as in a new PDF. IBM Plex Sans, which PDFjet carries, is read
	// from the library; the path of a font file of your own reads that file.
	bold := pdfjet.NewFontFromFileForObjects(&objects, IBMPlexSans.Bold)
	bold.SetSize(14.0)
	regular := pdfjet.NewFontFromFileForObjects(&objects, IBMPlexSans.Regular)
	regular.SetSize(10.0)

	// The date is fixed, so that the four ports write the same PDF: a program
	// would stamp the date of the day.
	stamp := pdfjet.NewTextBlock(bold, "APPROVED\n2026-10-30")
	stamp.SetWidth(120.0)
	stamp.SetPadding(6.0)
	stamp.SetTextColor(color.Red)
	stamp.SetBorderColor(color.Red)
	stamp.SetTextAlignment(alignment.Center)

	pages := pdf.GetPageObjects(objects)
	for i, pageObj := range pages {
		page := pdfjet.NewPageFromObject(pdf, pageObj)
		page.AddFontResource(bold, &objects)
		page.AddFontResource(regular, &objects)

		stamp.SetLocation(page.GetWidth()-156.0, 36.0)
		stamp.DrawOn(page)

		text := fmt.Sprintf("Page %d of %d", i+1, len(pages))
		x := float32((page.GetWidth() - regular.StringWidth(regular.GetSize(), text)) / 2.0)
		pdfjet.NewTextLine(regular, text).SetLocation(x, page.GetHeight()-36.0).DrawOn(page)

		page.Complete(&objects) // The graphics stack is unwinded automatically
	}
	if err := pdf.AddObjects(objects); err != nil {
		log.Fatal(err)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example50("resources/data/testPDFs/sample-document.pdf")
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_50 => %4d ms\n", time1-time0)
}
