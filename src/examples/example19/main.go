package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/contents"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example19 draws two images and three text boxes.
func Example19() {
	pdf := pdfjet.NewPDFFile("Example_19.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/Droid/DroidSansFallback.ttf.stream")

	f1.SetSize(10.0)
	f2.SetSize(10.0)

	image1 := pdfjet.NewImageFromFile(pdf, "images/fruit.jpg")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")

	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Columns x coordinates
	x1 := float32(75.0)
	y1 := float32(75.0)
	x2 := float32(325.0)
	w2 := float32(200.0) // Width of the second column:

	// Draw the first image
	image1.SetLocation(x1, y1)
	image1.ScaleBy(0.75)
	image1.DrawOn(page)

	textBlock := pdfjet.NewTextBox(f1)
	textBlock.SetText("Geometry arose independently in a number of early cultures as a practical way for dealing with lengths, areas, and volumes.")
	textBlock.SetLocation(x2, y1)
	textBlock.SetWidth(w2)
	textBlock.SetBorders(true)
	// textBlock.SetTextAlignment(align.Right)
	// textBlock.SetTextAlignment(align.Center)
	xy := textBlock.DrawOn(page)

	// Draw the second image
	image2.SetLocation(x1, xy[1]+10.0)
	image2.ScaleBy(1.0 / 3.0)
	image2.DrawOn(page)

	textBlock = pdfjet.NewTextBox(f1)
	textBlock.SetText(contents.OfTextFile("data/latin.txt"))
	textBlock.SetLocation(x2, xy[1]+10.0)
	textBlock.SetWidth(w2)
	textBlock.SetBorders(true)
	textBlock.DrawOn(page)

	textBlock = pdfjet.NewTextBox(f1)
	textBlock.SetFallbackFont(f2)
	textBlock.SetText(contents.OfTextFile("data/chinese.txt"))
	textBlock.SetLocation(x1, 600.0)
	textBlock.SetWidth(350.0)
	textBlock.SetBorders(true)
	textBlock.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example19()
	pdfjet.PrintDuration("Example_19", time.Since(start))
}
