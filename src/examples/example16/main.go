package main

import (
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example16 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example16() {
	pdf := pdfjet.NewPDFFile("Example_16.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Text block with highlighted keywords")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	colors := make(map[string]int32)
	colors["Everyone"] = color.Red
	colors["freedom"] = color.Blue
	colors["information"] = color.Green

	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // Stroking alpha
	gs.SetAlphaNonStroking(0.5) // Non-Stroking alpha
	page.SetGraphicsState(gs)

	// f1.SetSize(72.0)
	// text := pdfjet.NewTextLine(f1, "Hello, World")
	// text.SetLocation(50.0, 300.0)
	// text.DrawOn(page)

	buf, err := os.ReadFile("data/languages/english.txt")
	if err != nil {
		log.Fatal(err)
	}

	f1.SetSize(15.0)
	textBox := pdfjet.NewTextBlock(f1, string(buf))
	textBox.SetLocation(100.0, 50.0)
	textBox.SetWidth(400.0)
	//textBox.SetLineHeight(1.2)		// TODO
	//// If no height is specified the height will be calculated based on the text.
	//textBox.SetHeight(450.0)
	//textBox.SetTextDirection(direction.LeftToRight)
	//// textBox.SetTextDirection(direction.BottomToTop)
	//// textBox.SetVerticalAlignment(align.Top)
	//// textBox.SetVerticalAlignment(align.Center)
	//textBox.SetBgColor(color.WhiteSmoke)
	//textBox.SetTextColors(colors)
	//textBox.SetBorder(border.All)
	xy := textBox.DrawOn(page)

	page.SetGraphicsState(pdfjet.NewGraphicsState()) // Reset GS

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example16()
	pdfjet.PrintDuration("Example_16", time.Since(start))
}
