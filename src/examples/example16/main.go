package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example16 draws a text box with highlighted keywords.
func Example16() {
	pdf := pdfjet.NewPDFFile("Example_16.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Text block with highlighted keywords")

	// f1 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)
	// f1 := pdfjet.NewFontFromFile(pdf, NotoSans.Regular)
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

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
	textBox := pdfjet.NewTextBoxWithText(f1, englishText)
	// textBox.SetLocation(50.0, 50.0)
	// textBox.SetLocation(50.0, 100.0)
	textBox.SetLocation(100.0, 50.0)
	textBox.SetWidth(400.0)
	// If no height is specified the height will be calculated based on the text.
	textBox.SetHeight(450.0)
	// textBox.SetTextDirection(direction.LeftToRight)
	// textBox.SetTextDirection(direction.BottomToTop)
	// textBox.SetTextDirection(direction.TopToBottom)

	textBox.SetVerticalAlignment(alignment.Top)
	// textBox.SetVerticalAlignment(alignment.Bottom)
	// textBox.SetVerticalAlignment(alignment.Center)

	// textBox.SetTextAlignment(alignment.Center)
	// textBox.SetHeight(400.0)

	textBox.SetBackgroundColor(color.WhiteSmoke)
	textBox.SetTextColors(colors)
	textBox.SetBorders(true)
	xy := textBox.DrawOn(page)

	page.SetGraphicsState(pdfjet.NewGraphicsState()) // Reset GS
	// page.RestoreGraphicsState()

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example16()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_16", time0, time1)
}
