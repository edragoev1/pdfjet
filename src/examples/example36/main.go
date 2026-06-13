package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example36 shows how you can add pages to PDF in random order.
func Example36() {
	pdf := pdfjet.NewPDFFile("Example_36.pdf")

	image1 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2 := pdfjet.NewImageFromFile(pdf, "images/spain-admin.jpg")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page1 := pdfjet.NewPageDetached(pdf, a4.Portrait)

	text := pdfjet.NewTextLine(f1, "The map below is an embedded PNG image")
	text.SetLocation(90.0, 30.0)
	xy1 := text.DrawOn(page1)

	image1.SetLocation(90.0, xy1[1]+10.0)
	image1.ScaleBy(0.3)
	image1.DrawOn(page1)

	page2 := pdfjet.NewPageDetached(pdf, a4.Portrait)

	text.SetText("This page was created after the second one but it was drawn first!")
	text.SetLocation(90.0, 30.0)
	xy7 := text.DrawOn(page2)

	image2.SetLocation(90.0, xy7[1]+10.0)
	image2.ScaleBy(0.1)
	image2.DrawOn(page2)

	pdf.AddPage(page2)
	pdf.AddPage(page1)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example36()
	pdfjet.PrintDuration("Example_36", time.Since(start))
}
