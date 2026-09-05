package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example11 tests the one dimensional barcodes.
func Example11() {
	pdf := pdfjet.NewPDFFile("Example_11.pdf")
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	barcode := pdfjet.NewBarcode(pdfjet.CODE_128, "Hellö, World!")
	barcode.SetLocation(170.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE_128, "G86513JVW0C")
	barcode.SetLocation(170.0, 170.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE_39, "WIKIPEDIA")
	barcode.SetLocation(270.0, 370.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE_39, "CODE39")
	barcode.SetLocation(400.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE_39, "CODE39")
	barcode.SetLocation(450.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.UPC_A, "51234567890") // TODO: Do not allow more than 11 digits!!!
	barcode.SetLocation(450.0, 250.0)
	barcode.SetModuleLength(1.0)
	barcode.SetDirection(pdfjet.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.EAN_13, "051234567890") // EAN-13 without the check digit which we calculate!!
	barcode.SetLocation(450.0, 450.0)
	barcode.SetModuleLength(1.0)
	barcode.SetDirection(pdfjet.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example11()
	pdfjet.PrintDuration("Example_11", time.Since(start))
}
