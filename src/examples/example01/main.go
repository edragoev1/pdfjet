package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/font"
	"github.com/edragoev1/pdfjet/src/letter"
)

func Example01() {
	pdf := pdfjet.NewPDFFile("Example_01.pdf")

	font1 := pdfjet.NewFontFromFile(pdf, font.IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	textBlock := pdfjet.NewTextBlock(font1, content.OfTextFile("data/languages/english.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(430.0) // The height is adjusted automatically to fit the text.
	textBlock.SetBorderColor(color.None)
	textBlock.DrawOn(page)

	// Greek text
	textBlock = pdfjet.NewTextBlock(font1, content.OfTextFile("data/languages/greek.txt"))
	textBlock.SetLocation(50.0, 250.0)
	textBlock.SetWidth(430.0) // The height is adjusted automatically to fit the text.
	textBlock.SetBorderColor(color.None)
	textBlock.DrawOn(page)

	// Bulgarian text
	textBlock = pdfjet.NewTextBlock(font1, content.OfTextFile("data/languages/bulgarian.txt"))
	textBlock.SetLocation(50.0, 450.0)
	textBlock.SetWidth(430.0) // The height is adjusted automatically to fit the text.
	textBlock.SetBorderColor(color.None)
	textBlock.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example01()
	pdfjet.PrintDuration("Example_01", time.Since(start))
}
