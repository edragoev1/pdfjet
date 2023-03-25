package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/imagetype"
)

// Example03 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example03() {
	file, err := os.Create("Example_03.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)
	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	image1 := pdfjet.NewImageFromFile(pdf, "images/ee-map.png", imagetype.PNG)
	image2 := pdfjet.NewImageFromFile(pdf, "images/fruit.jpg", imagetype.JPG)
	image3 := pdfjet.NewImageFromFile(pdf, "images/mt-map.bmp", imagetype.BMP)

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)

	text := pdfjet.NewTextLine(f1, "The map below is an embedded PNG image")
	text.SetLocation(20.0, 20.0)
	// text.SetStrikeout(true)
	text.SetUnderline(true)
	uri := "https://en.wikipedia.org/wiki/European_Union"
	text.SetURIAction(&uri)
	text.DrawOn(page)

	image1.SetLocation(50.0, 50.0)
	image1.ScaleBy(2.0 / 3.0)
	image1.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "JPG image file embedded once and drawn 3 times")
	text.SetLocation(90.0, 550.0)
	xy := text.DrawOn(page)

	image2.SetLocation(90.0, xy[1]+f1.GetDescent())
	image2.ScaleBy(0.5)
	image2.DrawOn(page)

	image3.SetLocation(300.0, 600.0)
	image3.ScaleBy(0.5)
	image3.DrawOn(page)
	/*
		image2.SetLocation(260.0, point[1]+f1.GetDescent())
		image2.ScaleBy(0.5)
		image2.SetRotate(clockwise.NinetyDegrees)
		image2.DrawOn(page)

		image2.SetLocation(350.0, point[1]+f1.GetDescent())
		image2.SetRotate(clockwise.ZeroDegrees)
		image2.ScaleBy(0.5)
		image2.DrawOn(page)

		text = pdfjet.NewTextLine(f1, "The map on the right is an embedded BMP image")
		text.SetColor(color.Black)
		text.SetUnderline(true)
		text.SetVerticalOffset(3.0)
		text.SetStrikeout(true)
		text.SetTextDirection(15)
		text.SetLocation(90.0, 800.0)
		text.DrawOn(page)

		image3.SetLocation(390.0, 630.0)
		image3.ScaleBy(0.5)
		image3.DrawOn(page)

		page2 := NewPageAddTo(pdf, a4.Portrait)
		var xy []float32

		xy = image1.DrawOn(page2)

		box := NewBox()
		box.SetLocation(xy[0], xy[1])
		box.SetSize(20.0, 20.0)
		box.DrawOn(page2)
	*/
	pdf.Complete()
}

func main() {
	start := time.Now()
	Example03()
	elapsed := time.Since(start)
	fmt.Printf("Example_03 => %dµs\n", elapsed.Microseconds())
}
