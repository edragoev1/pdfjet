package main

import (
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example47 -- TODO:
func Example47() {
	pdf := pdfjet.NewPDFFile("Example_47.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf.stream")

	f1.SetSize(12.0)
	f2.SetSize(12.0)

	image1 := pdfjet.NewImageFromFile(pdf, "images/AU-map.png")
	image1.ScaleBy(0.50)

	image2 := pdfjet.NewImageFromFile(pdf, "images/HU-map.png")
	image2.ScaleBy(0.50)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	image1.SetLocation(20.0, 20.0)
	image1.DrawOn(page)

	image2.SetLocation(
		page.GetWidth()-(image2.GetWidth()+20.0),
		page.GetHeight()-(image2.GetHeight()+20.0))
	image2.DrawOn(page)

	paragraphs := make([]*pdfjet.TextLine, 0)
	contents := content.OfTextFile("data/austria_hungary.txt")
	textLines := strings.Split(contents, "\n\n")
	for _, textLine := range textLines {
		paragraphs = append(paragraphs, pdfjet.NewTextLine(f1, textLine))
	}

	xPos := float32(20.0)
	yPos := float32(250.0)

	width := float32(180.0)
	height := float32(315.0)

	frame := pdfjet.NewTextFrame(paragraphs)
	frame.SetLocation(xPos, yPos)
	frame.SetWidth(width)
	frame.SetHeight(height)
	frame.SetDrawBorder(true)
	frame.DrawOn(page)

	xPos += 200.0
	if frame.IsNotEmpty() {
		frame.SetLocation(xPos, yPos)
		frame.SetWidth(width)
		frame.SetHeight(height)
		frame.SetDrawBorder(false)
		frame.DrawOn(page)
	}

	xPos += 200.0
	if frame.IsNotEmpty() {
		frame.SetLocation(xPos, yPos)
		frame.SetWidth(width)
		frame.SetHeight(height)
		frame.SetDrawBorder(true)
		frame.DrawOn(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example47()
	pdfjet.PrintDuration("Example_47", time.Since(start))
}
