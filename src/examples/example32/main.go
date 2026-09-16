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
	"github.com/edragoev1/pdfjet/v9/src/JetBrainsMono"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example32 draws highlighted source code using the draw string method and a color map.
func Example32() {
	pdf, err := pdfjet.NewPDFFile("Example_32.pdf")
	if err != nil {
		log.Fatal(err)
	}

	font := pdfjet.NewFontFromFile(pdf, JetBrainsMono.Regular)
	font.SetSize(10.0)

	colors := make(map[string]int32)
	colors["new"] = color.Red
	colors["class"] = color.Blue
	colors["void"] = color.Green
	grayColor := [3]float32{0.2, 0.2, 0.2}

	page := pdfjet.NewPage(pdf, letter.Portrait())
	x := float32(50.0)
	y := float32(50.0)
	leading := font.GetBodyHeight(font.GetSize())
	lines := content.LinesOfTextFile("examples/Example_02.java")
	for _, line := range lines {
		pdfjet.NewTextLine(font, line).SetTextColorRGB(grayColor).SetHighlightColors(colors).SetLocation(x, y).DrawOn(page)
		y += leading
		if y > (page.GetHeight() - 20.0) {
			page = pdfjet.NewPage(pdf, letter.Portrait())
			y = 50.0
		}
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example32()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_32 => %4d ms\n", time1-time0)
}
