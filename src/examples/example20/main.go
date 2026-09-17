// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// Example20 draws a letterhead: a logo read from a PDF file, a maple leaf
// drawn as a path with curves, and a QR code with the address of a web site.
func Example20() {
	pdf, err := pdfjet.NewPDFFile("Example_20.pdf")
	if err != nil {
		log.Fatal(err)
	}

	// Read the logo from a PDF file, and add its fonts and images to this PDF.
	buf, err := os.ReadFile("data/testPDFs/PDFjetLogo.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdf.Read(buf)
	if err != nil {
		log.Fatal(err)
	}

	pdf.AddResourceObjects(objects)

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(11.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(11.0)

	pages := pdf.GetPageObjects(objects)
	content := pages[0].GetContentObject(objects)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// Draw the content of the first page of the logo PDF, at half its size.
	height := float32(105.0) // The logo height in points.
	x := float32(60.0)
	y := float32(40.0)
	xScale := float32(0.5)
	yScale := float32(0.5)

	page.DrawContents(
		content.GetData(),
		height,
		x,
		y,
		xScale,
		yScale)

	pdfjet.NewTextLine(f2, "PDFjet Software").SetLocation(390.0, 60.0).DrawOn(page)
	pdfjet.NewTextLine(f1, "Unionville, Ontario, Canada").SetLocation(390.0, 76.0).DrawOn(page)
	pdfjet.NewTextLine(f1, "https://pdfjet.com").SetLocation(390.0, 92.0).DrawOn(page)

	// A thin rule under the letterhead.
	page.SetPenColor(color.DarkRed)
	page.SetPenWidth(1.0)
	page.DrawLine(60.0, 115.0, 552.0, 115.0)

	text := pdfjet.NewTextLine(f2, "The logo on this page was read from a PDF file.")
	text.SetFontSize(16.0)
	text.SetLocation(60.0, 170.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"The logo is the content of the first page of data/testPDFs/PDFjetLogo.pdf, "+
			"drawn here at half its size with page.drawContents. It stays sharp "+
			"at any zoom, because it is drawn as vector graphics and not as an image.\n\n"+
			"The maple leaf below is a Path with curves, and the QR code "+
			"holds the address of the PDFjet web site.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(60.0, 185.0)
	textBlock.SetWidth(490.0)
	textBlock.DrawOn(page)

	// A maple leaf, drawn with lines and with curves from control points.
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
	path.SetClosed(true)
	path.SetStrokeColor(color.Red)
	path.SetFillShape(true)
	path.SetLocation(60.0, 330.0)
	path.ScaleBy(6.0)
	path.DrawOn(page)

	qr := qrcode.NewQRCode(
		"https://pdfjet.com",
		errorcorrectionlevel.M) // Medium
	qr.SetModuleLength(5.0)
	qr.SetLocation(300.0, 340.0)
	xy := qr.DrawOn(page)

	// A frame around the QR code.
	page.SetPenColor(color.LightGray)
	page.SetPenWidth(0.5)
	page.DrawRect(290.0, 330.0, xy[0]-280.0, xy[1]-320.0)

	caption := pdfjet.NewTextLine(f1, "A Path with curves")
	caption.SetTextColor(color.Gray)
	caption.SetLocation(60.0, xy[1]+35.0)
	caption.DrawOn(page)

	caption = pdfjet.NewTextLine(f1, "Scan to visit https://pdfjet.com")
	caption.SetTextColor(color.Gray)
	caption.SetLocation(290.0, xy[1]+35.0)
	caption.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example20()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_20 => %4d ms\n", time1-time0)
}
