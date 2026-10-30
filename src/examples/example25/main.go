// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The vehicles of the Washington State Electric Vehicle Population Data by
// model year and kind, summarized in
// resources/data/Electric_Vehicles_by_Model_Year.csv: the rows of the data
// whose "Electric Vehicle Type" begins with "Battery Electric" and with
// "Plug-in Hybrid", counted by the "Model Year" column. The data holds
// 124,716 rows, of which 124,419 are registered in Washington State, and
// model years from 1997; the years before 2011 are a few dozen vehicles in
// all and are left out here.
const firstYear = 2011

// Example25 draws a SimpleLineChart of two lines over the same years: the
// vehicles of the data file by model year, battery electric highlighted in
// navy, each line labeled at its end with its last count, so that the chart
// needs no legend, and a dot at each year so that each count can be read off
// its line. The second page draws the plug-in hybrids alone as an area chart,
// the area under the line filled and each count written above its year.
func Example25() {
	pdf, err := pdfjet.NewPDFFile("Example_25.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicles by Model Year")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f1, "Electric Vehicles by Model Year")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(18.0)
	title.SetLocation(50.0, 50.0)
	title.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f2,
		"A SimpleLineChart draws a few lines over the same categories, here "+
			"the model years, in their order left to right. Each line is labeled "+
			"at its end with its name and its last value, so the chart needs no "+
			"legend, and one line is highlighted in navy, the other gray. The dots "+
			"of setMarkers show the count for each model year. In a PDF/UA "+
			"document the chart describes itself from its data: each line's first "+
			"and last values and its highest.\n\n"+
			"The counts are of the Washington State Electric Vehicle Population "+
			"Data, summarized by model year and by kind in "+
			"resources/data/Electric_Vehicles_by_Model_Year.csv.")
	textBlock.SetFontSize(11.0)
	textBlock.SetLineSpacing(1.4)
	textBlock.SetLocation(50.0, 72.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	f1.SetSize(12.0)
	f2.SetSize(9.0)

	// The model years from firstYear on, after the header line
	var categories []string
	var battery, hybrid []float32
	lines := content.LinesOfTextFile("resources/data/Electric_Vehicles_by_Model_Year.csv")
	for _, line := range lines[1:] {
		field := strings.Split(line, ",")
		year, _ := strconv.Atoi(field[0])
		if year >= firstYear {
			batteryCount, _ := strconv.Atoi(field[1])
			hybridCount, _ := strconv.Atoi(field[2])
			categories = append(categories, field[0])
			battery = append(battery, float32(batteryCount))
			hybrid = append(hybrid, float32(hybridCount))
		}
	}
	pdfjet.NewSimpleLineChart(f1, f2).
		SetTitle("Battery electric and plug-in hybrid vehicles").
		SetSubtitle("By model year, in the Washington State data file").
		SetYAxisTitle("Vehicles").
		SetCategories(categories...).
		AddLine("Battery electric", battery).
		AddLine("Plug-in hybrid", hybrid).
		SetHighlight(0).
		SetMarkers(true).
		SetSize(512.0, 340.0).
		SetLocation(50.0, xy[1]+30.0).
		DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewSimpleLineChart(f1, f2).
		SetTitle("Plug-in hybrid vehicles").
		SetSubtitle("By model year, the area under the line filled and each count written").
		SetYAxisTitle("Vehicles").
		SetCategories(categories...).
		AddLine("Plug-in hybrid", hybrid).
		SetHighlight(0).
		SetMarkers(true).
		SetAreaFilled(true).
		SetValueLabels(true).
		SetSize(512.0, 300.0).
		SetLocation(50.0, 50.0).
		DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example25()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_25 => %4d ms\n", time1-time0)
}
