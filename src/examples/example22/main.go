package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example22 ...
func Example22() {
	pdf := pdfjet.NewPDFFile("Example_22.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

	dest1 := "dest#1"
	dest2 := "dest#2"
	dest3 := "dest#3"
	dest4 := "dest#4"

	page := pdfjet.NewPage(pdf, letter.Portrait)
	text := pdfjet.NewTextLine(f1, "Page #1 -> Go to Destination #3.")
	text.SetGoToAction(&dest3)
	text.SetLocation(90.0, 50.0)
	page.AddDestination(&dest1, 0.0, 0.0)
	text.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #2 -> Go to Destination #3.")
	text.SetGoToAction(&dest3)
	text.SetLocation(90.0, 550.0)
	page.AddDestination(&dest2, 0.0, text.GetDestinationY())
	text.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #3 -> Go to Destination #4.")
	text.SetGoToAction(&dest4)
	text.SetLocation(90.0, 700.0)
	page.AddDestination(&dest3, 0.0, text.GetDestinationY())
	text.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #4 -> Go to Destination #1.")
	text.SetGoToAction(&dest1)
	text.SetLocation(90.0, 100.0)
	page.AddDestination(&dest4, 0.0, text.GetDestinationY())
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "Page #4 -> Go to Destination #2.")
	text.SetGoToAction(&dest2)
	text.SetLocation(90.0, 200.0)
	text.DrawOn(page)

	// Create a box with invisible borders
	box := pdfjet.NewBoxAt(20.0, 20.0, 20.0, 20.0)
	box.SetColor(color.White)
	box.SetGoToAction(&dest1)
	box.DrawOn(page)

	// Create an up arrow and place it in the box
	path := pdfjet.NewPath()
	path.Add(pdfjet.NewPoint(10.0, 1.0))
	path.Add(pdfjet.NewPoint(17.0, 9.0))
	path.Add(pdfjet.NewPoint(13.0, 9.0))
	path.Add(pdfjet.NewPoint(13.0, 19.0))
	path.Add(pdfjet.NewPoint(7.0, 19.0))
	path.Add(pdfjet.NewPoint(7.0, 9.0))
	path.Add(pdfjet.NewPoint(3.0, 9.0))
	path.SetClosePath(true)
	path.SetColor(color.Oldgloryblue)
	path.SetColor(color.Deepskyblue)
	path.SetFillShape(true)
	path.PlaceAtZeroZeroIn(box)
	path.DrawOn(page)

	image := pdfjet.NewImageFromFile(pdf, "images/up-arrow.png")
	image.SetLocation(40.0, 40.0)
	image.SetGoToAction(&dest1)
	image.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example22()
	pdfjet.PrintDuration("Example_22", time.Since(start))
}
