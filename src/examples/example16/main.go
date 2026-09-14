package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example16 draws a text block with highlighted keywords.
func Example16() {
	pdf := pdfjet.NewPDFFile("Example_16.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Text block with highlighted keywords")

	// f1 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)
	// f1 := pdfjet.NewFontFromFile(pdf, NotoSans.Regular)
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	colors := make(map[string]int32)
	colors["Everyone"] = color.Red
	colors["pay"] = color.Green
	colors["freedom"] = color.Blue

	// page.SaveGraphicsState()

	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // Stroking alpha
	gs.SetAlphaNonStroking(0.5) // Non-Stroking alpha
	page.SetGraphicsState(gs)

	englishText := content.OfTextFile("data/languages/english.txt")
	// f1.SetSize(14.0)
	textBlock := pdfjet.NewTextBlock(f1, englishText)
	textBlock.SetLocation(100.0, 50.0)
	textBlock.SetWidth(400.0)
	// With a height the text that does not fit is cut; without one the
	// block is as tall as its text.
	textBlock.SetHeight(450.0)
	textBlock.SetVerticalAlignment(alignment.Top)
	// textBlock.SetVerticalAlignment(alignment.Bottom)
	// textBlock.SetVerticalAlignment(alignment.Center)
	// textBlock.SetTextAlignment(alignment.Center)
	textBlock.SetBackgroundColor(color.WhiteSmoke)
	textBlock.SetHighlightColors(colors)
	textBlock.SetBorderColor(color.Black)
	xy := textBlock.DrawOn(page)

	page.SetGraphicsState(pdfjet.NewGraphicsState()) // Reset GS
	// page.RestoreGraphicsState()

	rect := pdfjet.NewRect(xy[0], xy[1], 20.0, 20.0)
	rect.SetBorderColor(color.Black)
	rect.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example16()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_16 => %d ms\n", time1-time0)
}
