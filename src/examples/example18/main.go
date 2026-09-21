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

// Example18
// This example shows how to write "Page X of N" footer on every page.
func Example18() {
	pdf, err := pdfjet.NewPDFFile("Example_18.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("How to Number Pages")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	titles := []string{
		"1. Create the pages",
		"2. Draw the content",
		"3. Add the footers",
	}
	texts := []string{
		"The total number of pages is not known until all the content is drawn. " +
			"That is why the pages in this document are created with Page.DETACHED: " +
			"they are not added to the PDF yet, and they are kept in a list instead.",
		"Each page gets its content, like this heading and this paragraph. " +
			"Long documents would flow their text or tables from page to page here.",
		"Now the list holds every page, so its size is the total number of pages. " +
			"The footer \"Page X of N\" is drawn on each page, " +
			"and then all the pages are added to the PDF with addPages.",
	}

	pages := make([]*pdfjet.Page, 0)
	for i := 0; i < len(titles); i++ {
		page := pdfjet.NewPageDetached(pdf, a4.Portrait())

		header := pdfjet.NewTextLine(f1, "How to number pages")
		header.SetFontSize(10.0)
		header.SetTextColor(color.Gray)
		header.SetLocation(70.0, 50.0)
		header.DrawOn(page)

		line := pdfjet.NewLine(70.0, 60.0, page.GetWidth()-70.0, 60.0)
		line.SetStrokeColor(color.LightGray)
		line.DrawOn(page)

		title := pdfjet.NewTextLine(f2, titles[i])
		title.SetStructureType(structelem.H1)
		title.SetFontSize(20.0)
		title.SetLocation(70.0, 120.0)
		title.DrawOn(page)

		textBlock := pdfjet.NewTextBlock(f1, texts[i])
		textBlock.SetFontSize(12.0)
		textBlock.SetLineSpacing(1.5)
		textBlock.SetLocation(70.0, 140.0)
		textBlock.SetWidth(page.GetWidth() - 140.0)
		textBlock.DrawOn(page)

		pages = append(pages, page)
	}

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
	Example18()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_18 => %4d ms\n", time1-time0)
}
