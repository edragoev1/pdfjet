package main

import (
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/datamatrix"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example14 draws Data Matrix barcodes.
func Example14() {
	pdf := pdfjet.NewPDFFile("Example_14.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

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

	barcode = datamatrix.NewDataMatrixWithShape("PDFjet 9.0.0", datamatrix.Rectangle)
	barcode.SetLocation(50.0, 250.0)
	barcode.SetModuleLength(4.0)
	barcode.SetColor(color.Blue)
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

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example14()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_14", time0, time1)
}
