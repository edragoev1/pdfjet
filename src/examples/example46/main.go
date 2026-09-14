package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example46 draws optional content groups - PDF layers.
func Example46() {
	pdf, err := pdfjet.NewPDFFile("Example_46.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	image1 := pdfjet.NewImageFromFile(pdf, "images/map407.png")
	image1.SetLocation(10.0, 100.0)

	image2 := pdfjet.NewImageFromFile(pdf, "images/qrcode.png")
	image2.SetLocation(10.0, 100.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	textLine := pdfjet.NewTextLine(f2, "© OpenStreetMap contributors")
	textLine.SetLocation(10.0, 655.0)
	xy := textLine.DrawOn(page)

	textLine = pdfjet.NewTextLine(f2, "http://www.openstreetmap.org/copyright")
	textLine.SetURIAction("http://www.openstreetmap.org/copyright")
	textLine.SetLocation(10.0, xy[1]+f2.GetBodyHeight())
	textLine.DrawOn(page)

	group := pdfjet.NewOptionalContentGroup(pdf, "Map")
	group.Add(image1)
	group.SetVisible(true)
	group.SetPrintable(true)
	group.DrawOn(page)

	textBox := pdfjet.NewTextBlock(f1, "Blue Layer Text")
	// textBox.SetFontSize(16.0)
	textBox.SetLocation(10.0, 130.0)

	line := pdfjet.NewLine(300.0, 150.0, 500.0, 150.0)
	line.SetStrokeWidth(2.0)
	line.SetStrokeColor(color.Blue)

	group = pdfjet.NewOptionalContentGroup(pdf, "Blue Line")
	group.Add(textBox)
	group.Add(line)
	group.SetVisible(true)
	group.DrawOn(page)

	line = pdfjet.NewLine(300.0, 160.0, 500.0, 160.0)
	line.SetStrokeWidth(2.0)
	line.SetStrokeColor(color.Red)

	group = pdfjet.NewOptionalContentGroup(pdf, "Barcode")
	group.Add(image2)
	group.Add(line)
	group.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example46()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_46 => %d ms\n", time1-time0)
}
