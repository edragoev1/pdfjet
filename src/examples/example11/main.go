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
	f1.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	code := pdfjet.NewBarcode(pdfjet.CODE_128, "Hellö, World!")
	code.SetLocation(170.0, 70.0)
	code.SetModuleLength(0.75)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.CODE_128, "G86513JVW0C")
	code.SetLocation(170.0, 170.0)
	code.SetModuleLength(0.75)
	code.SetDirection(pdfjet.TopToBottom)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.CODE_39, "WIKIPEDIA")
	code.SetLocation(270.0, 370.0)
	code.SetModuleLength(0.75)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.CODE_39, "CODE39")
	code.SetLocation(400.0, 70.0)
	code.SetModuleLength(0.75)
	code.SetDirection(pdfjet.TopToBottom)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.CODE_39, "CODE39")
	code.SetLocation(450.0, 70.0)
	code.SetModuleLength(0.75)
	code.SetDirection(pdfjet.BottomToTop)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.UPC_A, "51234567890") // UPC-A without the check digit which we calculate!!
	code.SetLocation(450.0, 250.0)
	code.SetModuleLength(1.0)
	code.SetDirection(pdfjet.BottomToTop)
	code.SetFont(f1)
	code.DrawOn(page)

	code = pdfjet.NewBarcode(pdfjet.EAN_13, "051234567890") // EAN-13 without the check digit which we calculate!!
	code.SetLocation(450.0, 450.0)
	code.SetModuleLength(1.0)
	code.SetDirection(pdfjet.BottomToTop)
	code.SetFont(f1)
	code.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example11()
	pdfjet.PrintDuration("Example_11", time.Since(start))
}
