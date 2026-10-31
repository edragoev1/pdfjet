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

// Example39 draws the pages of a document first and finishes them last. The two map
// pages are drawn first and the contents page last, and the three are added
// to the PDF in reading order, the contents first. They are made detached,
// outside the PDF, so a "Page X of N" footer is drawn on each once the
// number of pages is known.
func Example39() {
	pdf, err := pdfjet.NewPDFFile("Example_39.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Maps")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	titles := []string{"Europe", "Spain"}
	files := []string{"resources/images/ee-map.png", "resources/images/spain-admin.jpg"}
	descriptions := []string{
		"A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.",
		"A map of the 17 autonomous communities of Spain and its two autonomous cities, Ceuta and Melilla, with their capitals.",
	}

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
		image.SetAltDescription(descriptions[i])
		image.ScaleBy((page.GetWidth() - 100.0) / image.GetWidth())
		image.SetLocation(50.0, 100.0)
		image.DrawOn(page)

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
			"of the document: the pages were made detached and are added to the PDF "+
			"in reading order. The footer \"Page X of N\" is drawn on each page last, "+
			"when the number of pages is known.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetTextColor(color.DimGray)
	textBlock.SetLocation(50.0, y+20.0)
	textBlock.SetWidth(contents.GetWidth() - 100.0)
	textBlock.DrawOn(contents)

	// 3. The pages in reading order, and a footer on each, now that their
	// number is known.
	pages := append([]*pdfjet.Page{contents}, mapPages...)
	fontSize := float32(10.0)
	for i := 0; i < len(pages); i++ {
		page := pages[i]

		line := pdfjet.NewLine(70.0, page.GetHeight()-60.0,
			page.GetWidth()-70.0, page.GetHeight()-60.0)
		line.SetStrokeColor(color.LightGray)
		line.DrawOn(page)

		// A page number is an artifact: a screen reader skips it.
		footer := "Page " + fmt.Sprint(i+1) + " of " + fmt.Sprint(len(pages))
		page.AddArtifactBMC()
		page.SetBrushColor(color.Black)
		page.DrawStringUsingFontSize(
			f1,
			fontSize,
			footer,
			(page.GetWidth()-f1.StringWidth(fontSize, footer))/2.0,
			page.GetHeight()-40.0)
		page.AddEMC()
	}
	pdf.AddPages(pages)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example39()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_39 => %4d ms\n", time1-time0)
}
