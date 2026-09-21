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

// Example24 draws a JPEG, a PNG and a BMP image.
func Example24() {
	pdf, err := pdfjet.NewPDFFile("Example_24.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("JPEG, PNG and BMP Images")

	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	image1 := pdfjet.NewImageFromFile(pdf, "images/gr-map.jpg")
	image1.SetAltDescription(
		"A map of Greece with its cities, roads and airports, the Ionian Sea to the west, the Aegean Sea to the east and Crete to the south.")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2.SetAltDescription(
		"A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.")
	image3 := pdfjet.NewImageFromFile(pdf, "images/rgb24pal.bmp")
	image3.SetAltDescription(
		"The letters BMP in white over bars of red, green, blue, yellow, magenta and cyan.")
	image4 := pdfjet.NewImageFromFile(pdf, "images/cmyk.jpg")
	image4.SetAltDescription(
		"A CMYK test chart: rows of cyan, magenta, yellow and black from 0 to 100 percent in steps of 10, and bars of red, green, blue and rich black.")

	page := pdfjet.NewPage(pdf, letter.Portrait())
	textLine1 := pdfjet.NewTextLine(font, "This is a JPEG image.")
	textLine1.SetTextRotation(0)
	textLine1.SetLocation(50.0, 50.0)
	point := textLine1.DrawOn(page)
	image1.ScaleBy(0.25).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	textLine2 := pdfjet.NewTextLine(font, "This is a PNG image.")
	textLine2.SetTextRotation(0)
	textLine2.SetLocation(50.0, 50.0)
	point = textLine2.DrawOn(page)
	image2.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	textLine3 := pdfjet.NewTextLine(font, "This is a BMP image.")
	textLine3.SetTextRotation(0)
	textLine3.SetLocation(50.0, 620.0)
	point = textLine3.DrawOn(page)
	image3.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	textLine4 := pdfjet.NewTextLine(font, "This is a CMYK JPEG image, with its inks stored inverted, as Photoshop saves them.")
	textLine4.SetLocation(50.0, 50.0)
	point = textLine4.DrawOn(page)
	image4.ScaleBy(0.425).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example24()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_24 => %4d ms\n", time1-time0)
}
