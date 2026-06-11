package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/NotoSans"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example19 show how to use the TextBlock class.
func Example19() {
	pdf := pdfjet.NewPDFFile("Example_19.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, NotoSans.Regular)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream")
	f2.SetSize(10.0)

	contents := content.OfTextFile("data/calculus-short.txt")

	page := pdfjet.NewPage(pdf, letter.Landscape)

	// Columns x coordinates
	x1 := float32(50.0)
	y1 := float32(50.0)
	x2 := float32(300.0)
	w2 := float32(300.0) // Width of the second column

	image1 := pdfjet.NewImageFromFile(pdf, "images/fruit.jpg")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")

	// Draw the first image and text:
	image1.SetLocation(x1, y1)
	image1.ScaleBy(0.75)
	xy := image1.DrawOn(page)

	textBox := pdfjet.NewTextBlock(f1, contents)
	textBox.SetLocation(x2, y1)
	textBox.SetWidth(w2)
	// textBox.SetBorders(true)
	// textBox.SetTextAlignment(align.Right)
	// textBox.SetTextAlignment(align.Center)
	xy = textBox.DrawOn(page)

	// Draw the second row image and text:
	image2.SetLocation(x1, xy[1]+10.0)
	image2.ScaleBy(1.0 / 3.0)
	image2.DrawOn(page)

	textBox = pdfjet.NewTextBlock(f1, content.OfTextFile("data/latin.txt"))
	textBox.SetLocation(x2, xy[1]+10.0)
	textBox.SetWidth(w2)
	// textBox.SetBorders(true)
	xy = textBox.DrawOn(page)

	textBox = pdfjet.NewTextBlock(f1, content.OfTextFile("data/chinese.txt"))
	textBox.SetFallbackFont(f2)
	textBox.SetLocation(x1, 570.0)
	textBox.SetWidth(350.0)
	// textBox.SetBorders(true)
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
