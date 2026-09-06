package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/IBMPlexSansTC"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example19 draws images and line-wrapped text boxes in two columns.
func Example19() {
	pdf := pdfjet.NewPDFFile("Example_19.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSansTC.Regular)
	f2.SetSize(10.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)
	// Columns x coordinates
	x1 := float32(50.0)
	y1 := float32(50.0)
	x2 := float32(300.0)
	w2 := float32(300.0) // Width of the second column

	image1 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image2 := pdfjet.NewImageFromFile(pdf, "images/spain-admin.jpg")

	// Draw the first image
	image1.SetLocation(x1, y1)
	image1.ScaleBy(0.3)
	image1.DrawOn(page)

	textBox := pdfjet.NewTextBoxWithText(f1, content.OfTextFile("data/calculus-short.txt"))
	textBox.SetLocation(x2, y1)
	textBox.SetWidth(w2)
	textBox.SetBorders(true)
	xy := textBox.DrawOn(page)

	// Draw the second image
	image2.SetLocation(x1, xy[1]+10.0)
	image2.ScaleBy(0.1)
	image2.DrawOn(page)

	textBox = pdfjet.NewTextBox(f1)
	textBox.SetText(content.OfTextFile("data/physics.txt"))
	textBox.SetLocation(x2, xy[1]+10.0)
	textBox.SetWidth(w2)
	textBox.SetBorders(true)
	xy = textBox.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example19()
	pdfjet.PrintDuration("Example_19", time.Since(start))
}
