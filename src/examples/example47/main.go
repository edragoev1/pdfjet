package main

import (
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example47 -- TODO:
func Example47() {
	pdf := pdfjet.NewPDFFile("Example_47.pdf")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(14.0)

	page := pdfjet.NewPage(pdf, letter.Landscape)

	paragraphs := make([]*pdfjet.TextLine, 0)
	contents := content.OfTextFile("data/dostoevsky.txt")
	textLines := strings.Split(contents, "\n\n")
	for _, textLine := range textLines {
		paragraphs = append(paragraphs, pdfjet.NewTextLine(f1, textLine))
	}

	xPos := float32(50.0)
	yPos := float32(50.0)
	width := float32(220.0)
	height := float32(500.0)

	frame := pdfjet.NewTextFrame(paragraphs)
	frame.SetLocation(xPos, yPos)
	frame.SetWidth(width)
	frame.SetHeight(height)
	frame.SetDrawBorder(true)
	frame.DrawOn(page)

	if frame.IsNotEmpty() {
		xPos += 250.0
		frame.SetLocation(xPos, yPos)
		frame.SetWidth(width)
		frame.SetHeight(height)
		frame.SetDrawBorder(false)
		frame.DrawOn(page)
	}

	if frame.IsNotEmpty() {
		xPos += 250.0
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
