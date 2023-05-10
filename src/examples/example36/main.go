package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example36 shows how you can add pages to PDF in random order.
func Example36() {
	pdf := pdfjet.NewPDFFile("Example_36.pdf", compliance.PDF15)

	image1 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2 := pdfjet.NewImageFromFile(pdf, "images/fruit.jpg")
	image3 := pdfjet.NewImageFromFile(pdf, "images/palette.bmp")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page1 := pdfjet.NewPageDetached(pdf, a4.Portrait)

	text := pdfjet.NewTextLine(f1, "The map below is an embedded PNG image")
	text.SetLocation(90.0, 30.0)
	xy1 := text.DrawOn(page1)

	image1.SetLocation(90.0, xy1[1]+10.0)
	image1.ScaleBy(2.0 / 3.0)
	xy2 := image1.DrawOn(page1)

	text.SetText("JPG image file embedded once and drawn 3 times")
	text.SetLocation(90.0, xy2[1]+10.0)
	xy3 := text.DrawOn(page1)

	image2.SetLocation(90.0, xy3[1]+10.0)
	image2.ScaleBy(0.5)
	xy4 := image2.DrawOn(page1)

	image2.SetLocation(xy4[0]+10.0, xy3[1]+10.0)
	image2.ScaleBy(0.5)
	image2.RotateClockwise(90)
	xy5 := image2.DrawOn(page1)

	image2.SetLocation(xy5[0]+10.0, xy3[1]+10.0)
	image2.RotateClockwise(0)
	image2.ScaleBy(0.5)
	xy6 := image2.DrawOn(page1)

	image3.SetLocation(xy6[0]+10.0, xy6[1]+10.0)
	image3.ScaleBy(0.5)
	image3.DrawOn(page1)

	page2 := pdfjet.NewPageDetached(pdf, a4.Portrait)

	text.SetText("This page was created after the second one but it was drawn first!")
	text.SetLocation(90.0, 30.0)
	xy7 := text.DrawOn(page2)

	image1.SetLocation(90.0, xy7[1]+10.0)
	image1.DrawOn(page2)

	pdf.AddPage(page2)
	pdf.AddPage(page1)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example36()
	pdfjet.PrintDuration("Example_36", time.Since(start))
}
