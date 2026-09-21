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
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example36 draws two map pages first and their contents page last, and
// then adds the pages to the PDF in reading order, with the contents first.
func Example36() {
	pdf, err := pdfjet.NewPDFFile("Example_36.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Maps")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	titles := []string{"Europe", "Spain"}
	files := []string{"images/ee-map.png", "images/spain-admin.jpg"}

	// 1. Draw the map pages. They are detached, so they are not in the PDF yet.
	mapPages := make([]*pdfjet.Page, len(titles))
	for i := 0; i < len(titles); i++ {
		page := pdfjet.NewPageDetached(pdf, a4.Portrait())

		title := pdfjet.NewTextLine(f2, titles[i])
		title.SetFontSize(24.0)
		title.SetLocation(50.0, 80.0)
		title.DrawOn(page)

		// Scale the image to the width of the page between the margins.
		image := pdfjet.NewImageFromFile(pdf, files[i])
		image.ScaleBy((page.GetWidth() - 100.0) / image.GetWidth())
		image.SetLocation(50.0, 100.0)
		image.DrawOn(page)

		footer := pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+2))
		footer.SetFontSize(10.0)
		footer.SetTextColor(color.Gray)
		footer.SetLocation(50.0, page.GetHeight()-40.0)
		footer.DrawOn(page)

		mapPages[i] = page
	}

	// 2. Draw the contents page last, now that the map pages are ready.
	contents := pdfjet.NewPageDetached(pdf, a4.Portrait())

	text := pdfjet.NewTextLine(f2, "Maps")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(24.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(contents)

	y := float32(130.0)
	for i := 0; i < len(titles); i++ {
		text = pdfjet.NewTextLine(f1, titles[i]+" . . . . . . . . . . page "+fmt.Sprint(i+2))
		text.SetFontSize(14.0)
		text.SetLocation(50.0, y)
		text.DrawOn(contents)
		y += 25.0
	}

	textBlock := pdfjet.NewTextBlock(f1,
		"This page was drawn after the two map pages, but it is the first page "+
			"of the document, because the pages were created detached and added "+
			"to the PDF in reading order with addPage.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetTextColor(color.Gray)
	textBlock.SetLocation(50.0, y+20.0)
	textBlock.SetWidth(contents.GetWidth() - 100.0)
	textBlock.DrawOn(contents)

	// 3. Add the pages in reading order.
	pdf.AddPage(contents)
	for i := 0; i < len(mapPages); i++ {
		pdf.AddPage(mapPages[i])
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example36()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_36 => %4d ms\n", time1-time0)
}
