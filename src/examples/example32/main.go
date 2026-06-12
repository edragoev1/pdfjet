package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/JetBrainsMono"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
)

// Example32 -- TODO:
func Example32() {
	pdf := pdfjet.NewPDFFile("Example_32.pdf")

	font := pdfjet.NewFontFromFile(pdf, JetBrainsMono.Regular)
	font.SetSize(10.0)

	colors := make(map[string]int32)
	colors["new"] = color.Red
	colors["class"] = color.Blue
	colors["void"] = color.Green

	page := pdfjet.NewPage(pdf, a4.Portrait)
	x := float32(50.0)
	y := float32(50.0)
	leading := font.GetBodyHeight(font.GetSize())
	lines := pdfjet.ReadTextLines("examples/Example_02.java")
	for _, line := range lines {
		page.DrawStringUsingColorMap(font, font, font.GetSize(), line, x, y, [3]float32{0.0, 0.0, 0.0}, colors)
		y += leading
		if y > (page.GetHeight() - 20.0) {
			page = pdfjet.NewPage(pdf, a4.Portrait)
			y = 50.0
		}
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example32()
	pdfjet.PrintDuration("Example_32", time.Since(start))
}
