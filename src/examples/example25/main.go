// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The vehicles of data/Electric_Vehicle_Population_Data.csv by model year
// and kind, which Example_24 draws as a table: the rows of the file whose
// "Electric Vehicle Type" begins with "Battery Electric" and with
// "Plug-in Hybrid", counted by the "Model Year" column. The file holds
// 124,716 rows, of which 124,419 are registered in Washington State, and
// model years from 1997; the years before 2011 are a few dozen vehicles
// in all and are left out here.
var (
	years   = []int{2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023}
	battery = []float32{753, 798, 2936, 1805, 3612, 3891, 4458, 9946, 8576, 9315, 14755, 23507, 11853}
	hybrid  = []float32{75, 870, 1645, 1804, 1323, 1811, 4100, 4278, 1874, 1611, 3541, 4015, 1500}
)

// Example25 draws a SimpleLineChart of two lines over the same years: the
// vehicles of the data file by model year, battery electric highlighted in
// navy, each line labeled at its end with its last count, so that the chart
// needs no legend, and a dot at each year so that each count can be read off
// its line.
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
			"The counts are the vehicles of "+
			"data/Electric_Vehicle_Population_Data.csv, the file Example_24 draws "+
			"as a table of 2,546 pages, by model year and by kind.")
	textBlock.SetFontSize(11.0)
	textBlock.SetLineSpacing(1.4)
	textBlock.SetLocation(50.0, 72.0)
	textBlock.SetWidth(512.0)
	xy := textBlock.DrawOn(page)

	f1.SetSize(12.0)
	f2.SetSize(9.0)

	categories := make([]string, len(years))
	for i, year := range years {
		categories[i] = strconv.Itoa(year)
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
