package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example01 demonstrates creating a PDF with multilingual text blocks
func Example01() {
	pdf := pdfjet.NewPDFFile("Example_01.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)

	// Load font and set size
	font1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	font1.SetSize(12.0)

	// Create a new page in portrait orientation
	page := pdfjet.NewPage(pdf, letter.Portrait)

	colorMap := make(map[string]int32)
	colorMap["Everyone"] = color.DarkRed
	colorMap["Pay"] = color.DarkGreen
	colorMap["Freedom"] = color.Blue

	// English text block setup and drawing
	textBlock := pdfjet.NewTextBlock(font1,
		content.OfTextFile("data/languages/english.txt"))
	textBlock.SetLocation(50, 50)
	textBlock.SetWidth(473) // Why 473f? To match the Google Fonts samples.
	textBlock.SetTextPadding(10)
	textBlock.SetKeywordHighlightColors(colorMap)
	xy := textBlock.DrawOn(page)

	// Draw a blue rectangle near the English text block
	rect := pdfjet.NewRect(xy[0], xy[1], 30.0, 30.0)
	rect.SetBorderColor(color.Blue)
	rect.DrawOn(page)

	// Greek text block, positioned below the English text
	textBlock = pdfjet.NewTextBlock(font1,
		content.OfTextFile("data/languages/greek.txt"))
	textBlock.SetLocation(50.0, xy[1]+30.0)
	textBlock.SetWidth(430.0)
	textBlock.SetTextPadding(10.0)
	xy = textBlock.DrawOn(page)

	// Bulgarian text block with blue border and rounded corners
	textBlock = pdfjet.NewTextBlock(font1,
		content.OfTextFile("data/languages/bulgarian.txt"))
	textBlock.SetLocation(50.0, xy[1]+30.0)
	textBlock.SetWidth(473.0)
	textBlock.SetTextPadding(10.0)
	textBlock.SetBorderColor(color.Blue)
	textBlock.SetBorderCornerRadius(10.0)
	textBlock.DrawOn(page)

	// Complete the PDF file
	pdf.Complete()
}

func main() {
	start := time.Now()
	Example01()
	pdfjet.PrintDuration("Example_01", time.Since(start))
}
