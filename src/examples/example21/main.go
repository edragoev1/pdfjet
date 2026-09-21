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
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example21 draws the same web address as a QR code with each of the four
// error correction levels. A higher level lets a scanner read a code that is
// more damaged or covered, and leaves room for less data in the code.
func Example21() {
	pdf, err := pdfjet.NewPDFFile("Example_21.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("QR Code Error Correction")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "QR Code Error Correction")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"Each QR code below holds the same address, https://pdfjet.com. "+
			"A higher error correction level lets a scanner read the code when "+
			"more of it is damaged or covered, and leaves room for less data, "+
			"so longer data at a higher level makes a larger code.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(70.0, 95.0)
	textBlock.SetWidth(470.0)
	textBlock.DrawOn(page)

	levels := []errorcorrectionlevel.ErrorCorrectionLevel{
		errorcorrectionlevel.L,
		errorcorrectionlevel.M,
		errorcorrectionlevel.Q,
		errorcorrectionlevel.H,
	}
	names := []string{
		"L (Low)",
		"M (Medium)",
		"Q (Quartile)",
		"H (High)",
	}
	notes := []string{
		"About 7% can be restored, 78 bytes fit in this size",
		"About 15% can be restored, 62 bytes fit in this size",
		"About 25% can be restored, 46 bytes fit in this size",
		"About 30% can be restored, 34 bytes fit in this size",
	}

	// Two rows of two codes.
	for i := 0; i < len(levels); i++ {
		x := 70.0 + float32(i%2)*250.0
		y := 200.0 + float32(i/2)*250.0

		qr := qrcode.NewQRCode("https://pdfjet.com", levels[i])
		qr.SetModuleLength(5.0)
		qr.SetLocation(x, y)
		xy := qr.DrawOn(page)

		text = pdfjet.NewTextLine(f2, names[i])
		text.SetFontSize(12.0)
		text.SetLocation(x, xy[1]+20.0)
		text.DrawOn(page)

		text = pdfjet.NewTextLine(f1, notes[i])
		text.SetFontSize(10.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(x, xy[1]+35.0)
		text.DrawOn(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example21()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_21 => %4d ms\n", time1-time0)
}
