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
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example29 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example29() {
	f, err := os.Create("Example_29.pdf")
	if err != nil {
		log.Fatal(err)
	}
	w := bufio.NewWriter(f)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	/*
		f, err = os.Open("fonts/Droid/DroidSerif-Regular.ttf.stream")
		reader := bufio.NewReader(f)
		font2 := pdfjet.NewFontStream1(pdf, reader)

		f, err = os.Open("fonts/Droid/DroidSansMono.ttf.stream")
		reader = bufio.NewReader(f)
		font3 := pdfjet.NewFontStream1(pdf, reader)

		f, err = os.Open("images/ee-map.png")
		reader = bufio.NewReader(f)
		image := pdfjet.NewImage(pdf, reader, imagetype.PNG)
		f.Close()
	*/
	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)

	font.SetSize(16.0)
	paragraph := pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(font,
		"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis."))

	column := pdfjet.NewTextColumn(0) // TODO:
	column.SetLocation(50.0, 50.0)
	column.SetSize(540.0, 0.0)
	// column.SetLineBetweenParagraphs(true)
	column.SetLineBetweenParagraphs(false)
	column.AddParagraph(paragraph)

	// dim0 := column.GetSize()
	column.DrawOn(page)
	point2 := column.DrawOn(nil)
	// dim1 := column.GetSize()
	// dim2 := column.GetSize()
	// dim3 := column.GetSize()
	/*
		System.out.println("height0: " + dim0.getHeight())
		System.out.println("point1.x: " + point1[0] + "    point1,y " + point1[1])
		System.out.println("point2.x: " + point2[0] + "    point2.y " + point2[1])
		System.out.println("height1: " + dim1.getHeight())
		System.out.println("height2: " + dim2.getHeight())
		System.out.println("height3: " + dim3.getHeight())
		System.out.println()
	*/
	column.RemoveLastParagraph()
	column.SetLocation(50.0, point2[1])
	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(font,
		"Peter Blood, bachelor of medicine and several other things besides, smoked a pipe and tended the geraniums boxed on the sill of his window above Water Lane in the town of Bridgewater."))
	column.AddParagraph(paragraph)

	// dim4 := column.GetSize()
	xy := column.DrawOn(page) // Draw the updated text column

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(540.0, 25.0)
	box.SetLineWidth(2.0)
	box.SetColor(color.Darkblue)
	box.DrawOn(page)

	pdf.Complete()

	f.Close()
}

func main() {
	start := time.Now()
	Example29()
	elapsed := time.Since(start)
	fmt.Printf("Example_29 => %dµs\n", elapsed.Microseconds())
}
