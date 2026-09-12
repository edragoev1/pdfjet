package main

import (
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// Example20 reads a logo in PDF format and draws it on a new PDF document.
func Example20() {
	pdf := pdfjet.NewPDFFile("Example_20.pdf")

	buf, err := os.ReadFile("data/testPDFs/PDFjetLogo.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects := pdf.Read(buf)

	pdf.AddResourceObjects(objects)

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(18.0)

	pages := pdf.GetPageObjects(objects)
	content := pages[0].GetContentObject(objects)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	height := float32(105.0) // The logo height in points.
	x := float32(50.0)
	y := float32(50.0)
	xScale := float32(0.5)
	yScale := float32(0.5)

	page.DrawContents(
		content.GetData(),
		height,
		x,
		y,
		xScale,
		yScale)

	page.SetPenColor(color.DarkBlue)
	page.SetPenWidth(0.0)
	page.DrawRect(0.0, 0.0, 50.0, 50.0)

	path := pdfjet.NewPath()

	path.Add(pdfjet.NewPoint(13.0, 0.0))
	path.Add(pdfjet.NewPoint(15.5, 4.5))

	path.Add(pdfjet.NewPoint(18.0, 3.5))
	path.Add(pdfjet.NewControlPointC(15.5, 13.5))
	path.Add(pdfjet.NewControlPointC(15.5, 13.5))
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
	path.Add(pdfjet.NewControlPointC(10.5, 13.5))
	path.Add(pdfjet.NewControlPointC(10.5, 13.5))
	path.Add(pdfjet.NewPoint(8.0, 3.5))

	path.Add(pdfjet.NewPoint(10.5, 4.5))
	path.SetClosePath(true)
	path.SetStrokeColor(color.Red)
	// path.SetFillShape(true)
	path.SetLocation(100.0, 100.0)
	path.ScaleBy(10.0)

	path.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	line := pdfjet.NewTextLine(f1, "Hello, World!")
	line.SetLocation(50.0, 50.0)
	line.DrawOn(page)

	qr := qrcode.NewQRCode(
		"https://kazuhikoarase.github.io",
		qrcode.ErrorCorrectLevelL) // Low
	qr.SetModuleLength(3.0)
	qr.SetLocation(50.0, 200.0)
	qr.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example20()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_20", time0, time1)
}
