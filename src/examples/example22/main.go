package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/imagetype"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example22 ...
func Example22() {
	f, err := os.Create("Example_22.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	var f1 *pdfjet.Font
	f, err = os.Open("fonts/OpenSans/OpenSans-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader := bufio.NewReader(f)
	f1 = pdfjet.NewFontStream1(pdf, reader)

	dest0 := "dest#0"
	dest1 := "dest#1"
	dest2 := "dest#2"
	dest3 := "dest#3"
	dest4 := "dest#4"

	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)
	text := pdfjet.NewTextLine(f1, "Page #1 -> Go to Destination #3.")
	text.SetGoToAction(&dest3)
	text.SetLocation(90.0, 50.0)
	page.AddDestination(&dest0, 0.0)
	page.AddDestination(&dest1, text.GetDestinationY())
	text.DrawOn(page)

	page = pdfjet.NewPageAddTo(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #2 -> Go to Destination #3.")
	text.SetGoToAction(&dest3)
	text.SetLocation(90.0, 550.0)
	page.AddDestination(&dest2, text.GetDestinationY())
	text.DrawOn(page)

	page = pdfjet.NewPageAddTo(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #3 -> Go to Destination #4.")
	text.SetGoToAction(&dest4)
	text.SetLocation(90.0, 700.0)
	page.AddDestination(&dest3, text.GetDestinationY())
	text.DrawOn(page)

	page = pdfjet.NewPageAddTo(pdf, letter.Portrait)
	text = pdfjet.NewTextLine(f1, "Page #4 -> Go to Destination #0.")
	text.SetGoToAction(&dest0)
	text.SetLocation(90.0, 100.0)
	page.AddDestination(&dest4, text.GetDestinationY())
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "Page #4 -> Go to Destination #2.")
	text.SetGoToAction(&dest2)
	text.SetLocation(90.0, 200.0)
	text.DrawOn(page)

	// Create a box with invisible borders
	box := pdfjet.NewBoxAt(20.0, 20.0, 20.0, 20.0)
	box.SetColor(color.White)
	box.SetGoToAction(&dest0)
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

	image := pdfjet.NewImageFromFile(pdf, "images/up-arrow.png", imagetype.PNG)
	image.SetLocation(40.0, 40.0)
	image.SetGoToAction(&dest0)
	image.DrawOn(page)

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example22()
	elapsed := time.Since(start)
	fmt.Printf("Example_22 => %dµs\n", elapsed.Microseconds())
}
