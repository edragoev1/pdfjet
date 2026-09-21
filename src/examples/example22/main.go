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

// Example22 links a contents page to three chapters, and each chapter
// back to the contents, with destinations and "Go To" actions.
func Example22() {
	pdf, err := pdfjet.NewPDFFile("Example_22.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Internal links and destinations")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	chapters := []string{
		"Destinations",
		"Go To actions",
		"Links on shapes and images",
	}
	texts := []string{
		"A destination is a named place in a document. The title of this chapter " +
			"is the destination \"chapter1\", set with setDestination.",
		"A Go To action makes something a link to a destination. The chapter titles " +
			"on the contents page are text lines with a Go To action.",
		"Rectangles and images can be links too. Click the arrow in the top left " +
			"corner, or the arrow image next to it, to go back to the contents.",
	}

	// The contents page. The destination is the top of the page.
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.AddDestinationAt("contents", 0.0, 0.0)

	text := pdfjet.NewTextLine(f2, "Contents")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(24.0)
	text.SetLocation(90.0, 100.0)
	text.DrawOn(page)

	y := float32(150.0)
	for i := 0; i < len(chapters); i++ {
		text = pdfjet.NewTextLine(f1, "Chapter "+fmt.Sprint(i+1)+": "+chapters[i])
		text.SetFontSize(14.0)
		text.SetTextColor(color.Blue)
		text.SetUnderline(true)
		text.SetGoToAction("chapter" + fmt.Sprint(i+1))
		text.SetLocation(90.0, y)
		text.DrawOn(page)
		y += 30.0
	}

	for i := 0; i < len(chapters); i++ {
		page = pdfjet.NewPage(pdf, letter.Portrait())

		// The title of the chapter is its destination.
		text = pdfjet.NewTextLine(f2, "Chapter "+fmt.Sprint(i+1)+": "+chapters[i])
		text.SetFontSize(20.0)
		text.SetDestination("chapter" + fmt.Sprint(i+1))
		text.SetLocation(90.0, 100.0)
		text.DrawOn(page)

		textBlock := pdfjet.NewTextBlock(f1, texts[i])
		textBlock.SetFontSize(12.0)
		textBlock.SetLineSpacing(1.5)
		textBlock.SetLocation(90.0, 125.0)
		textBlock.SetWidth(430.0)
		textBlock.DrawOn(page)

		text = pdfjet.NewTextLine(f1, "Back to the contents")
		text.SetFontSize(12.0)
		text.SetTextColor(color.Blue)
		text.SetUnderline(true)
		text.SetGoToAction("contents")
		text.SetLocation(90.0, 250.0)
		text.DrawOn(page)
	}

	// On the last page, a rect with no border links to the contents too.
	rect := pdfjet.NewRect(20.0, 20.0, 20.0, 20.0)
	rect.SetGoToAction("contents")
	rect.DrawOn(page)

	// Create an up arrow and place it in the rect
	path := pdfjet.NewPath()
	path.Add(pdfjet.NewPoint(30.0, 21.0))
	path.Add(pdfjet.NewPoint(37.0, 29.0))
	path.Add(pdfjet.NewPoint(33.0, 29.0))
	path.Add(pdfjet.NewPoint(33.0, 39.0))
	path.Add(pdfjet.NewPoint(27.0, 39.0))
	path.Add(pdfjet.NewPoint(27.0, 29.0))
	path.Add(pdfjet.NewPoint(23.0, 29.0))
	path.SetClosed(true)
	path.SetStrokeColor(color.DeepSkyBlue)
	path.SetFillShape(true)
	path.DrawOn(page)

	// And so does an image of an arrow.
	image := pdfjet.NewImageFromFile(pdf, "images/up-arrow.png")
	image.SetLocation(50.0, 20.0)
	image.SetGoToAction("contents")
	image.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example22()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_22 => %4d ms\n", time1-time0)
}
