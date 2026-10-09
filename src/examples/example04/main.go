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

// Example04 draws the Universal Declaration of Human Rights in Japanese and
// Korean, with IBM Plex Sans JP and KR, embedded, as a PDF/UA document: each
// block with its language and a heading in IBM Plex Sans.
func Example04() {
	// Initialize new PDF document that will be saved as Example_04.pdf
	pdf, err := pdfjet.NewPDFFile("Example_04.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("The Universal Declaration of Human Rights in Japanese and Korean")

	f0 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f0.SetSize(12.0)

	// The .otf: a page of Japanese or Korean draws hundreds of glyphs, whose CFF
	// outlines make a smaller subset than the .ttf's, about a quarter.
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")
	f1.SetSize(12.0)

	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf")
	f2.SetSize(12.0)

	// Create a new page in portrait Letter size
	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The heading is in IBM Plex Sans, and the characters it has no glyph for,
	// the name of the language, are in the fallback font.
	// The line above each block is its heading
	pdfjet.NewTextLine(f0, "This block is Japanese: 日本語").SetFallbackFont(f1).SetStructureType(structelem.H1).SetLocation(50.0, 50.0).DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/japanese.txt"))
	textBlock.SetLanguage("ja")
	textBlock.SetLocation(50.0, 70.0)
	textBlock.SetWidth(512.0)
	_ = textBlock.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())

	pdfjet.NewTextLine(f0, "This block is Korean: 한국어").SetFallbackFont(f2).SetStructureType(structelem.H1).SetLocation(50.0, 50.0).DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f2, content.OfTextFile("data/languages/korean.txt"))
	textBlock.SetLanguage("ko")
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
	Example04()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_04 => %4d ms\n", time1-time0)
}
