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
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const sample = "Embedded fonts look the same in every viewer."

const background = 0xf1f4f8

// Example05 draws the same words in five weights of IBM Plex Sans, in a PDF/UA
// document.
//
// An embedded font travels with the document, so the text looks the same in
// every viewer, every character of the font can be drawn, and the document can
// be PDF/UA and PDF/A. The fourteen core fonts, Helvetica, Times and Courier,
// are in every viewer and make the smallest documents, but they draw only the
// WinAnsi characters, and PDF/UA and PDF/A do not allow them.
func Example05() {
	pdf, err := pdfjet.NewPDFFile("Example_05.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Embedded Fonts")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	bold.SetSize(24.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(bold, "Embedded Fonts")
	title.SetStructureType(structelem.H1)
	title.SetLocation(50.0, 70.0)
	title.DrawOn(page)

	about := pdfjet.NewTextBlock(regular,
		"An embedded font travels with the document: the PDF carries the font program, "+
			"so the text looks the same in every viewer, every character of the font can be "+
			"drawn, and the document can be PDF/UA and PDF/A. PDFjet comes with the IBM Plex "+
			"fonts, Sans, Serif and Mono, with Arabic, Hebrew, Thai, Japanese, Korean and "+
			"Chinese, in their weights.\n\n"+
			"The fourteen core fonts, Helvetica, Times and Courier with their bold and italic, "+
			"Symbol and ZapfDingbats, are in every viewer, so the document carries no font "+
			"program and is the smallest it can be. But they draw only the characters of "+
			"Windows Latin 1, the viewer draws them with its own version of the font, and "+
			"PDF/UA and PDF/A do not allow them. The boxes below are the same words in five "+
			"weights of IBM Plex Sans.")
	about.SetLineSpacing(1.4)
	about.SetLocation(50.0, 90.0)
	about.SetWidth(512.0)
	y := about.DrawOn(page)[1] + 30.0

	names := []string{"Light", "Regular", "Medium", "SemiBold", "Bold"}
	weights := []string{
		IBMPlexSans.Light,
		IBMPlexSans.Regular,
		IBMPlexSans.Medium,
		IBMPlexSans.SemiBold,
		IBMPlexSans.Bold,
	}
	for i := range weights {
		font := pdfjet.NewFontFromFile(pdf, weights[i])
		font.SetSize(20.0)
		y = drawSample(page, regular, font, "IBM Plex Sans "+names[i], y) + 22.0
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawSample draws the label, and under it the sample words in the font, in a
// text block with a light background, and returns the bottom of the block.
func drawSample(page *pdfjet.Page, labelFont, font *pdfjet.Font, label string, y float32) float32 {
	caption := pdfjet.NewTextLine(labelFont, label)
	caption.SetTextColor(color.DimGray)
	caption.SetLocation(50.0, y)
	caption.DrawOn(page)

	block := pdfjet.NewTextBlock(font, sample)
	block.SetBackgroundColor(background)
	block.SetPadding(10.0)
	block.SetLineSpacing(1.2)
	block.SetLocation(50.0, y+8.0)
	block.SetWidth(512.0)
	return block.DrawOn(page)[1]
}

func main() {
	time0 := time.Now().UnixMilli()
	Example05()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_05 => %4d ms\n", time1-time0)
}
