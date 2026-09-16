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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansDevanagari"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example31 draws Devanagari text and fills rectangles through a transparent graphics state.
func Example31() {
	pdf, err := pdfjet.NewPDFFile("Example_31.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSansDevanagari.Regular)
	f1.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/marathi.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(500.0)
	textBlock.DrawOn(page)

	str := "असम के बाद UP में भी CM कैंडिडेट का ऐलान करेगी BJP?"
	textLine := pdfjet.NewTextLine(f1, str)
	textLine.SetLocation(50.0, 175.0)
	textLine.DrawOn(page)

	page.SetPenColor(color.Blue)
	page.SetBrushColor(color.Blue)
	page.FillRect(50.0, 200.0, 200.0, 200.0)

	page.SaveGraphicsState()

	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // The stroking alpha constant
	gs.SetAlphaNonStroking(0.5) // The non-stroking alpha constant
	page.SetGraphicsState(gs)

	page.SetPenColor(color.Green)
	page.SetBrushColor(color.Green)
	page.FillRect(100.0, 250.0, 200.0, 200.0)

	page.SetPenColor(color.Red)
	page.SetBrushColor(color.Red)
	page.FillRect(150.0, 300.0, 200.0, 200.0)

	page.RestoreGraphicsState()

	page.SetPenColor(color.Orange)
	page.SetBrushColor(color.Orange)
	page.FillRect(200.0, 350.0, 200.0, 200.0)

	page.SetBrushColor(0x00003865)
	page.FillRect(50.0, 550.0, 200.0, 200.0)

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
