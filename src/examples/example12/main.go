package main

import (
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/pdf417"
)

// Example12 constructs and draws PDF417 barcode.
func Example12() {
	pdf := pdfjet.NewPDFFile("Example_12.pdf")
	font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	page := pdfjet.NewPage(pdf, letter.Portrait)

	lines := pdfjet.ReadTextLines("data/Example_12.java")
	var buf strings.Builder
	for _, line := range lines {
		buf.WriteString(line)
		buf.WriteString("\r\n") // CR and LF are both required!
	}

	barcode := pdf417.NewPDF417(buf.String())
	barcode.SetModuleWidth(0.5)
	barcode.SetLocation(100.0, 60.0)
	barcode.DrawOn(page)

	textLine := pdfjet.NewTextLine(font,
		"PDF417 barcode containing the program that created it.")
	textLine.SetLocation(100.0, 40.0)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example12()
	pdfjet.PrintDuration("Example_12", time.Since(start))
}
