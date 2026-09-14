package main

import (
	"fmt"
	"log"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pdf417"
)

// Example12 constructs and draws PDF417 barcode.
func Example12() {
	pdf, err := pdfjet.NewPDFFile("Example_12.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PDF417 barcode example")
	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	page := pdfjet.NewPage(pdf, letter.Portrait())

	lines := content.LinesOfTextFile("data/Example_12.java")
	var buf strings.Builder
	for _, line := range lines {
		buf.WriteString(line)
		buf.WriteString("\r\n") // CR and LF are both required!
	}

	barcode := pdf417.NewPDF417(buf.String())
	barcode.SetModuleLength(0.5)
	barcode.SetLocation(100.0, 60.0)
	barcode.DrawOn(page)

	textLine := pdfjet.NewTextLine(font,
		"PDF417 barcode containing the contents of data/Example_12.java")
	textLine.SetLocation(100.0, 40.0)
	textLine.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example12()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_12 => %d ms\n", time1-time0)
}
