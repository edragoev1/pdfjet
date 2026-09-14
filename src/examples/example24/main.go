package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example24 draws a JPEG, a PNG and a BMP image.
func Example24() {
	pdf, err := pdfjet.NewPDFFile("Example_24.pdf")
	if err != nil {
		log.Fatal(err)
	}

	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	image1 := pdfjet.NewImageFromFile(pdf, "images/gr-map.jpg")
	image2 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png")
	image3 := pdfjet.NewImageFromFile(pdf, "images/rgb24pal.bmp")

	page := pdfjet.NewPage(pdf, letter.Portrait())
	textLine1 := pdfjet.NewTextLine(font, "This is a JPEG image.")
	textLine1.SetTextRotation(0)
	textLine1.SetLocation(50.0, 50.0)
	point := textLine1.DrawOn(page)
	image1.ScaleBy(0.25).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	textLine2 := pdfjet.NewTextLine(font, "This is a PNG image.")
	textLine2.SetTextRotation(0)
	textLine2.SetLocation(50.0, 50.0)
	point = textLine2.DrawOn(page)
	image2.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	textLine3 := pdfjet.NewTextLine(font, "This is a BMP image.")
	textLine3.SetTextRotation(0)
	textLine3.SetLocation(50.0, 620.0)
	point = textLine3.DrawOn(page)
	image3.ScaleBy(0.75).SetLocation(50.0, point[1]+5.0).DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example24()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_24 => %d ms\n", time1-time0)
}
