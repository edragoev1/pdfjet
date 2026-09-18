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
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example40 draws two bar charts with vertical bars from the same data: the
// two series grouped by month, and the same series stacked, with a legend
// under each title. The second page is the calendar of 2026, a CalendarMonth
// for each month, with the weeks starting on Monday.
func Example40() {
	pdf, err := pdfjet.NewPDFFile("Example_40.pdf")
	if err != nil {
		log.Fatal(err)
	}

	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(10.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

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
	chart.AddSeriesWithColor("2025", units2025, color.SeaGreen)
	chart.AddSeriesWithColor("2026", units2026, color.IndianRed)
	chart.SetGroupGap(0.4)
	chart.SetBarGap(0.1)
	chart.SetLocation(70.0, 50.0)
	chart.DrawOn(page)

	stacked := pdfjet.NewBarChart(f1, f2)
	stacked.SetSize(500.0, 300.0)
	stacked.SetTitle("Units sold by month, stacked")
	stacked.SetXAxisTitle("Month")
	stacked.SetYAxisTitle("Units")
	stacked.SetCategories(months...)
	stacked.AddSeriesWithColor("2025", units2025, color.SeaGreen)
	stacked.AddSeriesWithColor("2026", units2026, color.IndianRed)
	stacked.SetStacked(true)
	stacked.SetDrawValueLabels(true)
	stacked.SetLocation(70.0, 400.0)
	stacked.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	title := pdfjet.NewTextLine(f1, "Calendar 2026").SetFontSize(14.0)
	title.SetLocation(50.0, 45.0)
	title.DrawOn(page)
	monthNames := []string{
		"January", "February", "March", "April", "May", "June",
		"July", "August", "September", "October", "November", "December"}
	for i := 0; i < 12; i++ {
		x := 50.0 + float32(i%3)*182.0
		y := 65.0 + float32(i/3)*177.0
		monthName := pdfjet.NewTextLine(f1, monthNames[i])
		monthName.SetLocation(x, y+12.0)
		monthName.DrawOn(page)
		calendar := pdfjet.NewCalendarMonth(f1, f2, 2026, i+1)
		calendar.SetFirstDayOfWeek(time.Monday)
		calendar.SetCellWidth(21.0)
		calendar.SetCellHeight(21.0)
		calendar.SetLocation(x, y+18.0)
		calendar.DrawOn(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example40()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_40 => %4d ms\n", time1-time0)
}
