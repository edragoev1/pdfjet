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
)

// Example07 adds a "DRAFT" watermark to every page of a two-page
// PDF/A-3B document. The watermark is drawn first, so the text of the page
// is drawn over it.
func Example07() {
	pdf, err := pdfjet.NewPDFFile("Example_07.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3B)
	pdf.SetTitle("PDF/A-3B compliant PDF")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)

	titles := []string{
		"Project Proposal",
		"Budget and Schedule",
	}
	texts := []string{
		"This proposal describes a new reporting service that creates invoices, " +
			"statements and delivery notes as PDF documents. The documents are " +
			"archived as PDF/A-3B, so they can be opened and printed exactly " +
			"the same way for many years.\n\n" +
			"The watermark tells every reader that this is a draft. It is drawn " +
			"in light gray behind the text, at an angle from the bottom left " +
			"corner to the top right corner of the page.",
		"The service will be built in three phases over six months. The first " +
			"phase delivers invoices, the second statements, and the third " +
			"delivery notes.\n\n" +
			"The budget and the schedule will be final once the proposal is " +
			"approved. Until then, every page of this document is marked as a draft.",
	}

	for i := 0; i < len(titles); i++ {
		page := pdfjet.NewPage(pdf, a4.Landscape())

		// The watermark is drawn before the content of the page.
		f3.SetSize(120.0)
		page.AddWatermark(f3, "DRAFT")

		title := pdfjet.NewTextLine(f2, titles[i])
		title.SetFontSize(28.0)
		title.SetLocation(70.0, 100.0)
		title.DrawOn(page)

		textBlock := pdfjet.NewTextBlock(f1, texts[i])
		textBlock.SetFontSize(14.0)
		textBlock.SetLineSpacing(1.5)
		textBlock.SetLocation(70.0, 130.0)
		textBlock.SetWidth(page.GetWidth() - 140.0)
		textBlock.DrawOn(page)

		footer := pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+1)+" of "+fmt.Sprint(len(titles)))
		footer.SetFontSize(10.0)
		footer.SetTextColor(color.Gray)
		footer.SetLocation(70.0, page.GetHeight()-40.0)
		footer.DrawOn(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example07()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_07 => %4d ms\n", time1-time0)
}
