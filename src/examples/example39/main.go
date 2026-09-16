// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example39 draws a horizontal bar chart of the ten longest rivers, each bar
// in its own color with its length written inside it, under a title and a
// subtitle, and a color key and a source note under the chart.
func Example39() {
	pdf, err := pdfjet.NewPDFFile("Example_39.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(15.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(9.0)

	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f3.SetSize(9.0)

	f4 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f4.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	rivers := []string{
		"Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
		"Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"}
	lengths := []float32{6650.0, 6400.0, 6300.0, 5971.0, 5540.0, 5464.0, 5410.0, 4880.0, 4700.0, 4444.0}
	colors := []int32{
		0x5b9bd5, 0x6b8e6b, 0x8b6f47, 0x9b7b5a, 0x6fa8dc,
		0xd4a017, 0x8fa98f, 0xa08060, 0x5c5c5c, 0x8b7355}

	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(540.0, 400.0)
	chart.SetTitle("10 Longest Rivers in the World")
	chart.SetSubtitle("Length in kilometers · Color reflects typical sediment / pollution character")
	chart.SetCategories(rivers...)
	chart.AddSeriesWithColors("", lengths, colors)
	chart.SetHorizontal(true)
	chart.SetGroupGap(0.4)
	chart.SetValueAxisMinMax(0.0, 8000.0, 4)
	chart.SetGridLineWidth(0.75)
	chart.SetGridLineColor(0xe0e0e0)
	chart.SetGridLineDashPattern("[] 0")
	chart.SetAxisLineWidth(0.0)
	chart.SetDrawValueLabels(true)
	chart.SetValueLabelsInside(true)
	chart.SetGroupingUsed(true)
	chart.SetLocation(36.0, 40.0)
	chart.DrawOn(page)

	// The color key under the chart
	gray := int32(0x444444)
	pdfjet.NewTextLine(f3, "Color key (illustrative):").
		SetTextColor(gray).SetLocation(171.0, 466.0).DrawOn(page)
	keyColors := []int32{0x5b9bd5, 0x6b8e6b, 0xa08060, 0xd4a017, 0x5c5c5c}
	keyTexts := []string{
		"Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
		"Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"}
	keyX := []float32{171.0, 262.0, 398.0, 171.0, 313.0}
	keyY := []float32{482.0, 482.0, 482.0, 497.0, 497.0}
	for i := range keyColors {
		page.SetBrushColor(keyColors[i])
		page.FillRect(keyX[i], keyY[i]-8.5, 10.5, 10.5)
		pdfjet.NewTextLine(f4, keyTexts[i]).
			SetTextColor(gray).SetLocation(keyX[i]+15.0, keyY[i]).DrawOn(page)
	}

	note := "Color mapping is illustrative; lengths and conditions vary by source and season."
	pdfjet.NewTextLine(f4, note).
		SetTextColor(0x999999).SetLocation(576.0-f4.StringWidth(f4.GetSize(), note), 520.0).DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example39()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_39 => %4d ms\n", time1-time0)
}
