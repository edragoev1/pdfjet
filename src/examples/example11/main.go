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
	"github.com/edragoev1/pdfjet/v9/src/direction"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example11 draws a Code 128, a Code 39, a UPC-A and an EAN-13 barcode,
// each next to a label, and then barcodes drawn from top to bottom and from
// bottom to top.
func Example11() {
	pdf, err := pdfjet.NewPDFFile("Example_11.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Linear Barcodes")
	text.SetFontSize(22.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page)

	labels := []string{
		"Code 128",
		"Code 39",
		"UPC-A",
		"EAN-13",
	}
	notes := []string{
		"Letters, digits and symbols",
		"Upper case letters and digits",
		"11 digits, the check digit is added",
		"12 digits, the check digit is added",
	}
	barcodes := []*pdfjet.Barcode{
		pdfjet.NewBarcode(pdfjet.CODE_128, "Hellö, World!"),
		pdfjet.NewBarcode(pdfjet.CODE_39, "WIKIPEDIA"),
		pdfjet.NewBarcode(pdfjet.UPC_A, "51234567890"),
		pdfjet.NewBarcode(pdfjet.EAN_13, "051234567890"),
	}
	// UPC-A and EAN-13 need wider bars for the digits under them.
	moduleLengths := []float32{0.75, 0.75, 1.0, 1.0}

	y := float32(130.0)
	for i := 0; i < len(barcodes); i++ {
		pdfjet.NewTextLine(f2, labels[i]).SetLocation(70.0, y+15.0).DrawOn(page)
		note := pdfjet.NewTextLine(f1, notes[i])
		note.SetFontSize(10.0)
		note.SetTextColor(color.Gray)
		note.SetLocation(70.0, y+32.0)
		note.DrawOn(page)

		barcode := barcodes[i]
		barcode.SetLocation(290.0, y)
		barcode.SetModuleLength(moduleLengths[i])
		barcode.SetFont(f1)
		xy := barcode.DrawOn(page)
		y = xy[1] + 30.0
	}

	// The same barcodes can be drawn from top to bottom and from bottom to top.
	pdfjet.NewTextLine(f2, "Vertical barcodes").SetLocation(70.0, y+15.0).DrawOn(page)

	barcode := pdfjet.NewBarcode(pdfjet.CODE_128, "G86513JVW0C")
	barcode.SetLocation(70.0, y+35.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(direction.TopToBottom)
	barcode.SetFont(f1)
	xy := barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE_39, "CODE39")
	barcode.SetLocation(xy[0]+60.0, y+35.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(direction.BottomToTop)
	barcode.SetFont(f1)
	xy = barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.EAN_13, "051234567890")
	barcode.SetLocation(xy[0]+60.0, y+35.0)
	barcode.SetModuleLength(1.0)
	barcode.SetDirection(direction.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example11()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_11 => %4d ms\n", time1-time0)
}
