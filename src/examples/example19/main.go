package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/direction"
	"github.com/edragoev1/pdfjet/src/font"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example19 show how to use the TextBlock class.
func Example19() {
	pdf := pdfjet.NewPDFFile("Example_19.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, font.SourceSerif4.Italic)
	f1.SetSize(18.0)

	page := pdfjet.NewPage(pdf, letter.Landscape)

	textBlock := pdfjet.NewTextBlock(f1,
		content.OfTextFile("data/languages/english.txt"))
	textBlock.SetLocation(30.0, 150.0)
	textBlock.SetSize(500.0, 300.0)
	// textBlock.SetTextLineHeight(1.2)
	textBlock.SetTextColor(color.Black)
	textBlock.SetTextPadding(10.0)
	textBlock.SetBorderColor(color.Blue)
	textBlock.SetTextDirection(direction.LeftToRight)
	// textBlock.SetTextDirection(direction.BottomToTop)
	textBlock.SetTextAlignment(alignment.Left)
	// textBlock.SetTextAlignment(alignment.Right)
	// textBlock.SetTextAlignment(alignment.Center)
	textBlock.SetBorderWidth(0.5)
	textBlock.SetBorderCornerRadius(10.0)
	textBlock.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example19()
	pdfjet.PrintDuration("Example_19", time.Since(start))
}
