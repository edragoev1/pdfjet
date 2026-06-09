package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example31 -- TODO:
func Example31() {
	pdf := pdfjet.NewPDFFile("Example_31.pdf")
	pdf.SetTitle("Hello")
	pdf.SetAuthor("Eugene")
	pdf.SetSubject("Example")
	pdf.SetKeywords("Hello World This is a test")
	pdf.SetCreator("Application Name")

	f1 := pdfjet.NewFontFromFile(
		pdf, "fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf.stream")
	f1.SetSize(15.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/marathi.txt"))
	textBlock.SetLocation(500.0, 300.0)
	textBlock.SetLocation(50.0, 50.0)
	//textBlock.SetBorder(border.Left)
	//textBlock.SetBorder(border.Right)
	textBlock.DrawOn(page)

	str := "असम के बाद UP में भी CM कैंडिडेट का ऐलान करेगी BJP?"
	textLine := pdfjet.NewTextLine(f1, str)
	textLine.SetLocation(50.0, 175.0)
	textLine.DrawOn(page)

	page.SetPenColor(color.Blue)
	page.SetBrushColor(color.Blue)
	page.FillRect(50.0, 200.0, 200.0, 200.0)

	page.SaveGraphicsState()

	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // The stroking alpha constant
	gs.SetAlphaNonStroking(0.5) // The non-stroking alpha constant
	page.SetGraphicsState(gs)

	page.SetPenColor(color.Green)
	page.SetBrushColor(color.Green)
	page.FillRect(100.0, 250.0, 200.0, 200.0)

	page.SetPenColor(color.Red)
	page.SetBrushColor(color.Red)
	page.FillRect(150.0, 300.0, 200.0, 200.0)

	page.RestoreGraphicsState()

	page.SetPenColor(color.Orange)
	page.SetBrushColor(color.Orange)
	page.FillRect(200.0, 350.0, 200.0, 200.0)

	page.SetBrushColor(0x00003865)
	page.FillRect(50.0, 550.0, 200.0, 200.0)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example31()
	pdfjet.PrintDuration("Example_31", time.Since(start))
}
