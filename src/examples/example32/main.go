package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example32 -- TODO:
func Example32() {
	pdf := pdfjet.NewPDFFile("Example_32.pdf", compliance.PDF15)

	font := pdfjet.NewCoreFont(pdf, corefont.Courier())
	font.SetSize(8.0)

	colors := make(map[string]int32)
	colors["new"] = color.Red
	colors["ArrayList"] = color.Blue
	colors["List"] = color.Blue
	colors["String"] = color.Blue
	colors["Field"] = color.Blue
	colors["Form"] = color.Blue
	colors["Smart"] = color.Green
	colors["Widget"] = color.Green
	colors["Designs"] = color.Green

	page := pdfjet.NewPage(pdf, a4.Portrait)
	x := float32(50.0)
	y := float32(50.0)
	leading := font.GetBodyHeight()
	lines := pdfjet.ReadTextLines("examples/Example_02.java")
	for _, line := range lines {
		page.DrawStringUsingColorMap(font, nil, line, x, y, colors)
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
	elapsed := time.Since(start)
	fmt.Printf("Example_32 => %dµs\n", elapsed.Microseconds())
}
