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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansTC"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example19 uses the TextBlock component to draw text next to images.
func Example19() {
	pdf, err := pdfjet.NewPDFFile("Example_19.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSansTC.Regular)
	f2.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())
	// Columns x coordinates
	x1 := float32(50.0)
	y1 := float32(50.0)
	x2 := float32(300.0)
	w2 := float32(300.0) // Width of the second column

	image1 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2 := pdfjet.NewImageFromFile(pdf, "images/spain-admin.jpg")

	// Draw the first image
	image1.SetLocation(x1, y1)
	image1.ScaleBy(0.3)
	image1.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/calculus-short.txt"))
	textBlock.SetLocation(x2, y1)
	textBlock.SetWidth(w2)
	textBlock.SetBorderColor(color.Black)
	xy := textBlock.DrawOn(page)

	// Draw the second image
	image2.SetLocation(x1, xy[1]+10.0)
	image2.ScaleBy(0.1)
	image2.DrawOn(page)

	textBlock = pdfjet.NewTextBlock(f1, content.OfTextFile("data/physics.txt"))
	textBlock.SetLocation(x2, xy[1]+10.0)
	textBlock.SetWidth(w2)
	textBlock.SetBorderColor(color.Black)
	xy = textBlock.DrawOn(page)

	rect := pdfjet.NewRect(xy[0], xy[1], 20.0, 20.0)
	rect.SetBorderColor(color.Black)
	rect.DrawOn(page)

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
