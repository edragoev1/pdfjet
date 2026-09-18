// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"math"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example31 draws with transparency. A GraphicsState sets the alpha of the
// fills and of the strokes: opaque shapes hide what is under them, transparent
// ones mix with it, and the stroke of a shape can be more or less transparent
// than its fill.
func Example31() {
	pdf, err := pdfjet.NewPDFFile("Example_31.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Transparency")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "Transparency")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"A GraphicsState sets the alpha of the fills and of the strokes that "+
			"follow it, until the graphics state is restored. Opaque shapes hide "+
			"what is under them, transparent ones mix with it, and the stroke of "+
			"a shape can be more or less transparent than its fill.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	y := xy[1] + 30.0
	colors := []int32{color.Blue, color.Green, color.Red}

	// Opaque rectangles: each one hides the one under it.
	pdfjet.NewTextLine(f2, "Opaque").SetLocation(50.0, y).DrawOn(page)
	page.AddArtifactBMC() // The shapes carry no text, so they are artifacts.
	for i := 0; i < len(colors); i++ {
		page.SetBrushColor(colors[i])
		page.FillRect(50.0+float32(i)*60.0, y+15.0+float32(i)*30.0, 120.0, 120.0)
	}
	page.AddEMC()

	// Half transparent rectangles: the colors mix where they overlap.
	pdfjet.NewTextLine(f2, "50% transparent").SetLocation(320.0, y).DrawOn(page)
	page.AddArtifactBMC()
	page.SaveGraphicsState()
	gs := pdfjet.NewGraphicsState()
	gs.SetAlphaStroking(0.5)    // The stroking alpha constant
	gs.SetAlphaNonStroking(0.5) // The non-stroking alpha constant
	page.SetGraphicsState(gs)
	for i := 0; i < len(colors); i++ {
		page.SetBrushColor(colors[i])
		page.FillRect(320.0+float32(i)*60.0, y+15.0+float32(i)*30.0, 120.0, 120.0)
	}
	page.RestoreGraphicsState()
	page.AddEMC()

	// The same blue over a gray bar at four levels of alpha.
	y += 245.0
	pdfjet.NewTextLine(f2, "Fill alpha").SetLocation(50.0, y).DrawOn(page)
	page.AddArtifactBMC()
	page.SetBrushColor(color.Gray)
	page.FillRect(50.0, y+55.0, 506.0, 30.0)
	page.AddEMC()
	alphas := []float32{0.25, 0.5, 0.75, 1.0}
	for i := 0; i < len(alphas); i++ {
		x := 50.0 + float32(i)*132.0
		page.AddArtifactBMC()
		page.SaveGraphicsState()
		gs = pdfjet.NewGraphicsState()
		gs.SetAlphaNonStroking(alphas[i])
		page.SetGraphicsState(gs)
		page.SetBrushColor(color.Blue)
		page.FillRect(x, y+15.0, 110.0, 110.0)
		page.RestoreGraphicsState()
		page.AddEMC()

		text = pdfjet.NewTextLine(f1, fmt.Sprintf("%d%%", int(math.Round(float64(alphas[i]*100.0)))))
		text.SetFontSize(10.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(x, y+140.0)
		text.DrawOn(page)
	}

	// A thick stroke and a fill, each transparent on its own: the stroke
	// shows the fill through it, then the fill shows the stroke.
	y += 175.0
	pdfjet.NewTextLine(f2, "Stroke alpha and fill alpha").SetLocation(50.0, y).DrawOn(page)
	labels := []string{
		"Stroke 25%, fill 100%",
		"Stroke 100%, fill 25%",
		"Stroke 50%, fill 50%",
	}
	strokeAndFill := [][2]float32{{0.25, 1.0}, {1.0, 0.25}, {0.5, 0.5}}
	for i := 0; i < len(labels); i++ {
		x := 100.0 + float32(i)*180.0
		page.AddArtifactBMC()
		page.SaveGraphicsState()
		gs = pdfjet.NewGraphicsState()
		gs.SetAlphaStroking(strokeAndFill[i][0])
		gs.SetAlphaNonStroking(strokeAndFill[i][1])
		page.SetGraphicsState(gs)
		page.SetBrushColor(color.Red)
		page.FillCircle(x, y+65.0, 40.0)
		page.SetPenColor(color.Blue)
		page.SetPenWidth(16.0)
		page.DrawCircle(x, y+65.0, 40.0)
		page.RestoreGraphicsState()
		page.AddEMC()

		text = pdfjet.NewTextLine(f1, labels[i])
		text.SetFontSize(10.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(x-text.GetWidth()/2.0, y+130.0)
		text.DrawOn(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example31()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_31 => %4d ms\n", time1-time0)
}
