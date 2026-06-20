package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example49 -- TODO:
func Example49() {
	pdf := pdfjet.NewPDFFile("Example_49.pdf")
	image1 := pdfjet.NewImageFromFile(pdf, "images/photoshop.jpg")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f1.SetSize(12.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	textLine := pdfjet.NewTextLine(f1, "Hello, World!")
	textLine.SetLocation(100.0, 300.0)
	textLine.SetTextDirection(30)
	textLine.SetVerticalOffset(50)
	textLine.SetUnderline(true)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example49()
	pdfjet.PrintDuration("Example_49", time.Since(start))
}
