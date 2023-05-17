package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example11 tests the one dimenstional barcodes.
func Example11() {
	pdf := pdfjet.NewPDFFile("Example_11.pdf", compliance.PDF15)
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

	page := pdfjet.NewPage(pdf, letter.Portrait)

	barcode := pdfjet.NewBarcode(pdfjet.CODE128, "Hellö, World!")
	barcode.SetLocation(170.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE128, "G86513JVW0C")
	barcode.SetLocation(170.0, 170.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE39, "WIKIPEDIA")
	barcode.SetLocation(270.0, 370.0)
	barcode.SetModuleLength(0.75)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE39, "CODE39")
	barcode.SetLocation(400.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.CODE39, "CODE39")
	barcode.SetLocation(450.0, 70.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.BottomToTop)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	barcode = pdfjet.NewBarcode(pdfjet.Upc, "712345678904")
	barcode.SetLocation(450.0, 270.0)
	barcode.SetModuleLength(0.75)
	barcode.SetDirection(pdfjet.TopToBottom)
	barcode.SetFont(f1)
	barcode.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example11()
	pdfjet.PrintDuration("Example_11", time.Since(start))
}
