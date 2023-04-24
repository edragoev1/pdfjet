package main

import (
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example20 -- TODO:
func Example20() {
	pdf := pdfjet.NewPDFFile("Example_20.pdf", compliance.PDF15)

	buf, err := os.ReadFile("data/testPDFs/PDFjetLogo.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects := pdf.Read(buf)

	pdf.AddResourceObjects(objects)

	font1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
	font1.SetSize(18.0)

	pages := pdf.GetPageObjects(objects)
	contents := pages[0].GetContentsObject(objects)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	height := float32(105.0) // The logo height in points.
	x := float32(50.0)
	y := float32(50.0)
	xScale := float32(0.5)
	yScale := float32(0.5)

	page.DrawContents(
		contents.GetData(),
		height,
		x,
		y,
		xScale,
		yScale)

	page.SetPenColor(color.Darkblue)
	page.SetPenWidth(0.0)
	page.DrawRect(0.0, 0.0, 50.0, 50.0)

	path := pdfjet.NewPath()

	path.Add(pdfjet.NewPoint(13.0, 0.0))
	path.Add(pdfjet.NewPoint(15.5, 4.5))

	path.Add(pdfjet.NewPoint(18.0, 3.5))
	path.Add(pdfjet.NewControlPoint(15.5, 13.5))
	path.Add(pdfjet.NewControlPoint(15.5, 13.5))
	path.Add(pdfjet.NewPoint(20.5, 7.5))

	path.Add(pdfjet.NewPoint(21.0, 9.5))
	path.Add(pdfjet.NewPoint(25.0, 9.0))
	path.Add(pdfjet.NewPoint(24.0, 13.0))
	path.Add(pdfjet.NewPoint(25.5, 14.0))
	path.Add(pdfjet.NewPoint(19.0, 19.0))
	path.Add(pdfjet.NewPoint(20.0, 21.5))
	path.Add(pdfjet.NewPoint(13.5, 20.5))
	path.Add(pdfjet.NewPoint(13.5, 27.0))
	path.Add(pdfjet.NewPoint(12.5, 27.0))
	path.Add(pdfjet.NewPoint(12.5, 20.5))
	path.Add(pdfjet.NewPoint(6.0, 21.5))
	path.Add(pdfjet.NewPoint(7.0, 19.0))
	path.Add(pdfjet.NewPoint(0.5, 14.0))
	path.Add(pdfjet.NewPoint(2.0, 13.0))
	path.Add(pdfjet.NewPoint(1.0, 9.0))
	path.Add(pdfjet.NewPoint(5.0, 9.5))

	path.Add(pdfjet.NewPoint(5.5, 7.5))
	path.Add(pdfjet.NewControlPoint(10.5, 13.5))
	path.Add(pdfjet.NewControlPoint(10.5, 13.5))
	path.Add(pdfjet.NewPoint(8.0, 3.5))

	path.Add(pdfjet.NewPoint(10.5, 4.5))
	path.SetClosePath(true)
	path.SetColor(color.Red)
	// path.SetFillShape(true)
	path.SetLocation(100.0, 100.0)
	path.ScaleBy(10.0)

	path.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	line := pdfjet.NewTextLine(font1, "Hello, World!")
	line.SetLocation(50.0, 50.0)
	line.DrawOn(page)
	/*
		qr := dproject.NewQRCode("https://kazuhikoarase.github.io", errorcorrectlevel.L) // Low
		qr.SetModuleLength(3.0)
		qr.SetLocation(50.0, 200.0)
		qr.DrawOn(page)
	*/
	pdf.Complete()
}

func main() {
	start := time.Now()
	Example20()
	pdfjet.PrintDuration("Example_20", time.Since(start))
}
