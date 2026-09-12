package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/JetBrainsMono"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/util"
)

// Example32 draws highlighted source code using the draw string method and a color map.
func Example32() {
	pdf := pdfjet.NewPDFFile("Example_32.pdf")

	font := pdfjet.NewFontFromFile(pdf, JetBrainsMono.Regular)
	font.SetSize(10.0)

	colors := make(map[string]int32)
	colors["new"] = color.Red
	colors["class"] = color.Blue
	colors["void"] = color.Green
	grayColor := [3]float32{0.2, 0.2, 0.2}

	page := pdfjet.NewPage(pdf, letter.Portrait)
	x := float32(50.0)
	y := float32(50.0)
	leading := font.GetBodyHeight()
	lines := util.ReadLines("examples/Example_02.java")
	for _, line := range lines {
		page.DrawStringUsingColorMap(font, nil, font.GetSize(), line, x, y, grayColor, colors)
		y += leading
		if y > (page.GetHeight() - 20.0) {
			page = pdfjet.NewPage(pdf, letter.Portrait)
			y = 50.0
		}
	}

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example32()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_32", time0, time1)
}
