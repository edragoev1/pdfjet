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
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example02 draws the Universal Declaration of Human Rights in Simplified and
// Traditional Chinese, with IBM Plex Sans SC and TC, embedded, as a PDF/UA
// document: each block with its language and a heading in IBM Plex Sans.
func Example02() {
	// Initialize new PDF document that will be saved as Example_02.pdf
	pdf, err := pdfjet.NewPDFFile("Example_02.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("The Universal Declaration of Human Rights in Simplified and Traditional Chinese")

	f0 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f0.SetSize(12.0)

	// The .otf: a page of Chinese draws hundreds of glyphs, whose CFF
	// outlines make a smaller subset than the .ttf's, about a quarter.
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf")
	f1.SetSize(12.0)

	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf")
	f2.SetSize(12.0)

	// Create a new page in portrait Letter size
	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The heading is in IBM Plex Sans, and the characters it has no glyph for,
	// the name of the language, are in the fallback font.
	// The line above each block is its heading
	pdfjet.NewTextLine(f0, "This block is Simplified Chinese: 简体中文").SetFallbackFont(f1).SetStructureType(structelem.H1).SetLocation(50.0, 50.0).DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/simplified-chinese.txt"))
	textBlock.SetLanguage("zh-Hans")
	textBlock.SetLocation(50.0, 70.0)
	textBlock.SetWidth(512.0)
	_ = textBlock.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())

	pdfjet.NewTextLine(f0, "This block is Traditional Chinese: 繁體中文").SetFallbackFont(f2).SetStructureType(structelem.H1).SetLocation(50.0, 50.0).DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f2, content.OfTextFile("data/languages/traditional-chinese.txt"))
	textBlock.SetLanguage("zh-Hant")
	textBlock.SetLocation(50.0, 70.0)
	textBlock.SetWidth(512.0)
	_ = textBlock.DrawOn(page)

	// Finalize the PDF document
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	// Measure and print execution time
	time0 := time.Now().UnixMilli()
	Example02()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_02 => %4d ms\n", time1-time0)
}
