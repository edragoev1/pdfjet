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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansDevanagari"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example31 draws Hindi and Marathi text, and filled rectangles, first
// opaque and then half transparent, so the colors mix where they overlap.
func Example31() {
	pdf, err := pdfjet.NewPDFFile("Example_31.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSansDevanagari.Regular)
	f1.SetSize(13.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(14.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// Hindi: the second line of the file, after its label.
	pdfjet.NewTextLine(f2, "Hindi").SetLocation(50.0, 60.0).DrawOn(page)
	lines := content.LinesOfTextFile("data/languages/devanagari.txt")
	textBlock := pdfjet.NewTextBlock(f1, lines[1])
	textBlock.SetLineSpacing(1.3)
	textBlock.SetLocation(50.0, 70.0)
	textBlock.SetWidth(510.0)
	xy := textBlock.DrawOn(page)

	pdfjet.NewTextLine(f2, "Marathi").SetLocation(50.0, xy[1]+35.0).DrawOn(page)
	textBlock = pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/marathi.txt"))
	textBlock.SetLineSpacing(1.3)
	textBlock.SetLocation(50.0, xy[1]+45.0)
	textBlock.SetWidth(510.0)
	xy = textBlock.DrawOn(page)

	y := xy[1] + 50.0
	colors := []int32{color.Blue, color.Green, color.Red}

	// Opaque rectangles: each one hides the one under it.
	pdfjet.NewTextLine(f2, "Opaque").SetLocation(50.0, y).DrawOn(page)
	for i := 0; i < len(colors); i++ {
		page.SetBrushColor(colors[i])
		page.FillRect(50.0+float32(i)*60.0, y+15.0+float32(i)*30.0, 120.0, 120.0)
	}

	// Half transparent rectangles: the colors mix where they overlap.
	pdfjet.NewTextLine(f2, "50% transparent").SetLocation(320.0, y).DrawOn(page)
	page.SaveGraphicsState()
	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // The stroking alpha constant
	gs.SetAlphaNonStroking(0.5) // The non-stroking alpha constant
	page.SetGraphicsState(gs)
	for i := 0; i < len(colors); i++ {
		page.SetBrushColor(colors[i])
		page.FillRect(320.0+float32(i)*60.0, y+15.0+float32(i)*30.0, 120.0, 120.0)
	}
	page.RestoreGraphicsState()

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example31()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_31 => %4d ms\n", time1-time0)
}
