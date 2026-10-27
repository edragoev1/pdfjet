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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example27 draws bar charts. The first page is a horizontal bar chart of the ten
// longest rivers, each bar in its own color with its length inside it, and a
// color key under the chart. The second page draws two charts with vertical
// bars from the same data: the two series grouped by month, and the same
// series stacked. The third, the units of 2026 with a line over the bars for
// the target of each month.
func Example27() {
	pdf, err := pdfjet.NewPDFFile("Example_27.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Bar Charts")

	drawHorizontal(pdf)
	drawVertical(pdf)
	drawTarget(pdf)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func drawHorizontal(pdf *pdfjet.PDF) {
	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(15.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(9.0)

	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f3.SetSize(9.0)

	f4 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f4.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The heading of the page, above the chart
	pdfjet.NewTextLine(f1, "Rivers of the World").
		SetStructureType(structelem.H1).
		SetFontSize(18.0).
		SetLocation(36.0, 40.0).
		DrawOn(page)

	rivers := []string{
		"Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
		"Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"}
	lengths := []float32{6650.0, 6400.0, 6300.0, 5971.0, 5540.0, 5464.0, 5410.0, 4880.0, 4700.0, 4444.0}
	colors := []int32{
		0x477aa8, 0x5f7e5f, 0x8b6f47, 0x8e7152, 0x517aa0,
		0x967110, 0x687b68, 0x8e7155, 0x5c5c5c, 0x897154}

	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(540.0, 400.0)
	chart.SetTitle("10 Longest Rivers in the World")
	chart.SetSubtitle("Length in kilometers · Color reflects typical sediment / pollution character")
	chart.SetCategories(rivers...)
	chart.AddSeriesWithColors("", lengths, colors)
	chart.SetHorizontal(true)
	chart.SetGroupGap(0.4)
	chart.SetValueAxisRange(0.0, 8000.0)
	chart.SetGridLineColor(0xe0e0e0)
	chart.SetValueLabels(true)
	chart.SetValueLabelsInside(true)
	chart.SetGroupingUsed(true)
	chart.SetAltDescription(
		"The ten longest rivers in the world, in kilometers: Nile 6,650, Amazon 6,400, Yangtze 6,300, Mississippi-Missouri 5,971, Yenisey-Baikal-Selenga 5,540, Huang He (Yellow) 5,464, Ob-Irtysh 5,410, Paraná 4,880, Congo 4,700, Amur 4,444.")
	chart.SetLocation(36.0, 70.0)
	chart.DrawOn(page)

	// The color key under the chart
	gray := int32(0x444444)
	pdfjet.NewTextLine(f3, "Color key (illustrative):").
		SetTextColor(gray).SetLocation(171.0, 496.0).DrawOn(page)
	keyColors := []int32{0x477aa8, 0x5f7e5f, 0x8e7155, 0x967110, 0x5c5c5c}
	keyTexts := []string{
		"Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
		"Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"}
	// Three on the first row and two on the second, each after the one
	// before it by the width of its text
	x := float32(171.0)
	y := float32(512.0)
	for i := range keyColors {
		if i == 3 {
			x = 171.0
			y = 527.0
		}
		// The swatches are rectangles, drawn as artifacts next to their text.
		pdfjet.NewRect(x, y-8.5, 10.5, 10.5).SetFillColor(keyColors[i]).DrawOn(page)
		pdfjet.NewTextLine(f4, keyTexts[i]).
			SetTextColor(gray).SetLocation(x+15.0, y).DrawOn(page)
		x += 15.0 + f4.StringWidth(f4.GetSize(), keyTexts[i]) + 12.0
	}

	note := "Color mapping is illustrative; lengths and conditions vary by source and season."
	pdfjet.NewTextLine(f4, note).
		SetTextColor(0x696969).SetLocation(576.0-f4.StringWidth(f4.GetSize(), note), 550.0).DrawOn(page)

	// The data of the chart in a table under it
	lengthTexts := []string{
		"6,650", "6,400", "6,300", "5,971", "5,540", "5,464", "5,410", "4,880", "4,700", "4,444"}
	characters := []string{
		keyTexts[0], keyTexts[1], keyTexts[2], keyTexts[2], keyTexts[0],
		keyTexts[3], keyTexts[1], keyTexts[2], keyTexts[4], keyTexts[2]}
	rows := [][]*pdfjet.Cell{{
		pdfjet.NewCell(f3, "River").SetWidth(180.0),
		pdfjet.NewCell(f3, "Length, km").SetWidth(90.0).SetTextAlignment(alignment.Right),
		pdfjet.NewCell(f3, "Character").SetWidth(270.0)}}
	for i := range rivers {
		rows = append(rows, []*pdfjet.Cell{
			pdfjet.NewCell(f2, rivers[i]).SetWidth(180.0),
			pdfjet.NewCell(f2, lengthTexts[i]).SetWidth(90.0).SetTextAlignment(alignment.Right),
			pdfjet.NewCell(f2, characters[i]).SetWidth(270.0)})
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(f3, 0x000000, 0xEEF0F3)
	table.SetCellBorderColor(0xC8CCD2)
	table.SetLocation(36.0, 572.0)
	table.DrawOn(page)
}

func drawVertical(pdf *pdfjet.PDF) {
	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

	// The heading of the document, above the charts
	pdfjet.NewTextLine(f1, "Sales in 2025 and 2026").
		SetStructureType(structelem.H1).
		SetFontSize(18.0).
		SetLocation(70.0, 45.0).
		DrawOn(page)

	months := []string{
		"Jan", "Feb", "Mar", "Apr", "May", "Jun",
		"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"}
	units2025 := []float32{45.0, 65.0, 31.0, 45.0, 65.0, 31.0, 38.0, 52.0, 47.0, 59.0, 66.0, 72.0}
	units2026 := []float32{75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0}

	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Units sold by month")
	chart.SetXAxisTitle("Month")
	chart.SetYAxisTitle("Units")
	chart.SetCategories(months...)
	chart.AddSeries("2025", units2025)
	chart.AddSeries("2026", units2026)
	chart.SetGroupGap(0.4)
	chart.SetBarGap(0.1)
	chart.SetAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.")
	chart.SetLocation(70.0, 70.0)
	chart.DrawOn(page)

	stacked := pdfjet.NewBarChart(f1, f2)
	stacked.SetSize(500.0, 300.0)
	stacked.SetTitle("Units sold by month, stacked")
	stacked.SetXAxisTitle("Month")
	stacked.SetYAxisTitle("Units")
	stacked.SetCategories(months...)
	stacked.AddSeries("2025", units2025)
	stacked.AddSeries("2026", units2026)
	stacked.SetStacked(true)
	stacked.SetValueLabels(true)
	stacked.SetAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.")
	stacked.SetLocation(70.0, 420.0)
	stacked.DrawOn(page)
}

func drawTarget(pdf *pdfjet.PDF) {
	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

	months := []string{
		"Jan", "Feb", "Mar", "Apr", "May", "Jun",
		"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"}
	units2026 := []float32{75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0}
	target := []float32{60.0, 60.0, 62.0, 64.0, 66.0, 68.0, 68.0, 68.0, 70.0, 72.0, 75.0, 80.0}

	// The description of the chart is made from its data
	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Units sold by month in 2026, and the target")
	chart.SetXAxisTitle("Month")
	chart.SetYAxisTitle("Units")
	chart.SetCategories(months...)
	chart.AddSeries("2026", units2026)
	chart.AddLineSeries("Target", target)
	chart.SetLocation(70.0, 70.0)
	chart.DrawOn(page)
}

func main() {
	time0 := time.Now().UnixMilli()
	Example27()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_27 => %4d ms\n", time1-time0)
}
