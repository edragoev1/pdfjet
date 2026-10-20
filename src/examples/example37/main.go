// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/datamatrix"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example37 draws two-dimensional barcodes. The same web address as a QR code with
// each of the four error correction levels, where a higher level lets a
// scanner read a code that is more damaged or covered and leaves room for
// less data; GS1 data as a GS1 Digital Link in a QR code; and Data Matrix
// symbols of a web address, text in UTF-8, a rectangle, a larger symbol and
// GS1 data.
func Example37() {
	pdf, err := pdfjet.NewPDFFile("Example_37.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("QR Codes and Data Matrix Barcodes")

	drawQRCodes(pdf)
	drawDataMatrix(pdf)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func drawQRCodes(pdf *pdfjet.PDF) {
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "QR Code Error Correction")
	text.SetStructureType(structelem.H1)
	text.SetFontSize(22.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"Each QR code below holds the same address, the home page of PDFjet. "+
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
		text.SetTextColor(color.DimGray)
		text.SetLocation(x, xy[1]+35.0)
		text.DrawOn(page)
	}

	// GS1 data as a GS1 Digital Link, a web address an ordinary QR code carries.
	page2 := pdfjet.NewPage(pdf, letter.Portrait())
	text = pdfjet.NewTextLine(f2, "GS1 Digital Link")
	text.SetStructureType(structelem.H2)
	text.SetFontSize(18.0)
	text.SetLocation(70.0, 80.0)
	text.DrawOn(page2)

	link := pdfjet.GS1DigitalLink("https://id.gs1.org", "(01)09506000134352(10)ABC123(17)261231")
	note := pdfjet.NewTextBlock(f1, "The GS1 data (01)09506000134352(10)ABC123(17)261231, a GTIN, a batch and an expiry date, "+
		"is this web address, a GS1 Digital Link, which an ordinary QR code carries and any phone opens:")
	note.SetFontSize(12.0)
	note.SetLineSpacing(1.5)
	note.SetLocation(70.0, 95.0)
	note.SetWidth(470.0)
	corner := note.DrawOn(page2)

	// The address as a link, not as text in the paragraph, which PDF/UA
	// checkers take for a link without its Link element
	text = pdfjet.NewTextLine(f1, link)
	text.SetFontSize(12.0)
	text.SetTextColor(color.Blue)
	text.SetUnderline(true)
	text.SetURIAction(link)
	text.SetLocation(70.0, corner[1]+20.0)
	text.DrawOn(page2)

	code := qrcode.NewQRCode(link, errorcorrectionlevel.M)
	code.SetModuleLength(4.0)
	code.SetLocation(70.0, corner[1]+50.0)
	code.DrawOn(page2)
}

func drawDataMatrix(pdf *pdfjet.PDF) {
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The heading of the document, above the symbols
	pdfjet.NewTextLine(f1, "Data Matrix Barcodes").
		SetStructureType(structelem.H1).
		SetFontSize(18.0).
		SetLocation(50.0, 36.0).
		DrawOn(page)

	barcode := datamatrix.NewDataMatrix("https://github.com/edragoev1/pdfjet")
	barcode.SetLocation(50.0, 50.0)
	barcode.SetModuleLength(3.0)
	xy := barcode.DrawOn(page)
	caption := pdfjet.NewTextLine(f1, "A web address")
	caption.SetLocation(50.0, xy[1]+20.0)
	caption.DrawOn(page)

	barcode = datamatrix.NewDataMatrix("Grüße aus München! こんにちは 😀")
	barcode.SetLocation(300.0, 50.0)
	barcode.SetModuleLength(3.0)
	xy = barcode.DrawOn(page)
	caption = pdfjet.NewTextLine(f1, "Text in UTF-8")
	caption.SetLocation(300.0, xy[1]+20.0)
	caption.DrawOn(page)

	barcode = datamatrix.NewDataMatrixWithShape("PDFjet 9.1.1", datamatrix.Rectangle)
	barcode.SetLocation(50.0, 250.0)
	barcode.SetModuleLength(4.0)
	barcode.SetModuleColor(color.Blue)
	xy = barcode.DrawOn(page)
	caption = pdfjet.NewTextLine(f1, "A rectangular symbol")
	caption.SetLocation(50.0, xy[1]+20.0)
	caption.DrawOn(page)

	var sb strings.Builder
	for i := 1; i <= 20; i++ {
		sb.WriteString("Line " + strconv.Itoa(i) + " of a longer text in a larger symbol.\n")
	}
	barcode = datamatrix.NewDataMatrix(sb.String())
	barcode.SetLocation(300.0, 250.0)
	barcode.SetModuleLength(2.0)
	xy = barcode.DrawOn(page)
	caption = pdfjet.NewTextLine(f1, "A larger symbol")
	caption.SetLocation(300.0, xy[1]+20.0)
	caption.DrawOn(page)

	barcode = datamatrix.NewGS1DataMatrix("(01)09506000134352(17)261231(10)ABC123(21)XYZ-42")
	barcode.SetLocation(50.0, 400.0)
	barcode.SetModuleLength(3.0)
	xy = barcode.DrawOn(page)
	// On two lines, clear of the larger symbol at its right
	caption = pdfjet.NewTextLine(f1, "GS1: a GTIN, an expiry date,")
	caption.SetLocation(50.0, xy[1]+20.0)
	caption.DrawOn(page)
	caption = pdfjet.NewTextLine(f1, "a batch and a serial number")
	caption.SetLocation(50.0, xy[1]+20.0+14.0) // A line of 10 points, and its spacing
	caption.DrawOn(page)
}

func main() {
	time0 := time.Now().UnixMilli()
	Example37()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_37 => %4d ms\n", time1-time0)
}
