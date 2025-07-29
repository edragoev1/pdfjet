package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

func Example02() {
	// Create a new PDF file
	pdf := pdfjet.NewPDFFile("Example_02.pdf")

	// Load Japanese font and set size
	font1 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream")
	font1.SetSize(12.0)

	// Load Korean font and set size
	font2 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream")
	font2.SetSize(12.0)

	// Add a new page in portrait Letter size
	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Japanese text block
	textBlock := pdfjet.NewTextBlock(font1, content.OfTextFile("data/languages/japanese.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(415.0)
	textBlock.SetBorderColor(color.None)
	textBlock.DrawOn(page)

	// Korean text block
	textBlock = pdfjet.NewTextBlock(font2, content.OfTextFile("data/languages/korean.txt"))
	textBlock.SetLocation(50.0, 450.0)
	textBlock.SetWidth(415.0)
	textBlock.SetBorderColor(color.None)
	textBlock.DrawOn(page)

	// Finalize the PDF
	pdf.Complete()
}

func main() {
	start := time.Now()
	Example02()
	pdfjet.PrintDuration("Example_02", time.Since(start))
}
