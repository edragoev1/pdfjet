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
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example19 uses the TextBlock component to draw text next to images. The
// DrawOn methods of Image and TextBlock return the bottom of what they drew,
// so each row starts below the taller of the image and the text next to it.
func Example19() {
	pdf, err := pdfjet.NewPDFFile("Example_19.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Text Next to Images")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Text Next to Images")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"Each map below is an Image with a TextBlock next to it. The drawOn "+
			"method of both returns the bottom of what it drew, so every row "+
			"starts below the taller of the two.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	imageFiles := []string{
		"images/ee-map.png",
		"images/spain-admin.jpg",
	}
	titles := []string{
		"The European Union",
		"The Regions of Spain",
	}
	descriptions := []string{
		"A map of Europe with the member states of the European Union and " +
			"the countries that were candidates to join it when the map was " +
			"made. The image is a PNG file of 687 by 710 pixels, drawn 200 " +
			"points wide.",
		"A map of the 17 autonomous communities of Spain and its two " +
			"autonomous cities, Ceuta and Melilla, with their capitals. The " +
			"image is a JPEG file of 2,017 by 2,412 pixels, drawn 200 points " +
			"wide, which prints at more than 700 dots per inch.",
	}

	var x1 float32 = 50.0  // The images
	var x2 float32 = 270.0 // The text next to them
	y := xy[1] + 25.0
	for i := 0; i < len(imageFiles); i++ {
		image := pdfjet.NewImageFromFile(pdf, imageFiles[i])
		image.ResizeWidth(200.0)
		image.SetLocation(x1, y)
		imageXY := image.DrawOn(page)

		text = pdfjet.NewTextLine(f2, titles[i])
		text.SetFontSize(14.0)
		text.SetLocation(x2, y+f2.GetAscent(14.0))
		text.DrawOn(page)

		textBlock = pdfjet.NewTextBlock(f1, descriptions[i])
		textBlock.SetFontSize(11.0)
		textBlock.SetLineSpacing(1.5)
		textBlock.SetLocation(x2, y+25.0)
		textBlock.SetWidth(292.0)
		textXY := textBlock.DrawOn(page)

		y = max(imageXY[1], textXY[1]) + 25.0
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example19()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_19 => %4d ms\n", time1-time0)
}
