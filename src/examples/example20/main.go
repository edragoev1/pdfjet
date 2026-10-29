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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

var makes = []string{"TESLA", "NISSAN", "CHEVROLET", "BMW", "FORD"}
var names = []string{"Tesla", "Nissan", "Chevrolet", "BMW", "Ford"}

const firstYear = 2019
const lastYear = 2023

// Example20 draws a page of results from a CSV file of electric vehicles: two
// simple bar charts, the five makes and the model years, and under them a
// table of the same numbers, then a table of one car with its photo and its
// VIN as a barcode, in their cells.
func Example20() {
	pdf, err := pdfjet.NewPDFFile("Example_20.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Electric Vehicles by Make and Model Year")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f1.SetSize(11.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(9.0)

	// The cars of the file: by make, the battery electric ones and the
	// electric range of those that give one, and by model year
	lines := content.LinesOfTextFile("resources/data/Electric_Vehicle_Population_10_Pages.csv")
	cars := make([]int, len(makes))
	battery := make([]int, len(makes))
	ranges := make([]int, len(makes))
	ranged := make([]int, len(makes))
	years := make([]int, lastYear-firstYear+1)
	var modelX []string // The first Tesla Model X of the file
	for _, line := range lines[1:] {
		field := strings.Split(line, ",")
		year, _ := strconv.Atoi(field[5])
		if year >= firstYear && year <= lastYear {
			years[year-firstYear]++
		}
		i := indexOf(makes, field[6])
		if i >= 0 {
			cars[i]++
			if strings.HasPrefix(field[8], "Battery") {
				battery[i]++
			}
			electricRange, _ := strconv.Atoi(field[10])
			if electricRange > 0 {
				ranges[i] += electricRange
				ranged[i]++
			}
		}
		if modelX == nil && field[7] == "MODEL X" {
			modelX = field
		}
	}
	total := len(lines) - 1

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f1, "Electric Vehicles by Make and Model Year")
	title.SetStructureType(structelem.H1).SetFontSize(20.0)
	title.SetLocation(50.0, 70.0)
	title.DrawOn(page)
	intro := []*pdfjet.Paragraph{pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f2, "The "+strconv.Itoa(total)+
		" electric vehicles of the first rows of the data file that Example_24 draws whole: the five makes "+
		"with the most cars, and the cars of each model year from "+strconv.Itoa(firstYear)+" to "+
		strconv.Itoa(lastYear)+", with the numbers in the table under the charts.").SetFontSize(10.0))}
	frame := pdfjet.NewTextFrameFromParagraphs(intro).SetWidth(512.0)
	frame.SetLocation(50.0, 84.0)
	frame.DrawOn(page)
	y := frame.GetTextBottom() + 18.0

	byMake := pdfjet.NewSimpleBarChart(f1, f2).SetTitle("The five makes").SetSubtitle("cars").SetHighlight(0)
	for i := range makes {
		byMake.Add(names[i], float32(cars[i]))
	}
	byMake.SetWidth(248.0)
	byMake.SetLocation(50.0, y)
	byMake.DrawOn(page)

	// The chart highlights the busiest year itself, and marks the average,
	// in whole cars, with a dashed line; its bars are rounded
	byYear := pdfjet.NewSimpleBarChart(f1, f2).SetTitle("By model year").SetSubtitle("cars").
		SetHighlightHighest().SetRoundedBars(true)
	allYears := 0
	for i := range years {
		byYear.Add(strconv.Itoa(firstYear+i), float32(years[i]))
		allYears += years[i]
	}
	byYear.SetReferenceLine(float32(allYears/len(years)), "Average")
	byYear.SetWidth(248.0)
	byYear.SetLocation(314.0, y)
	byYear.DrawOn(page)
	y += max(byMake.GetHeight(), byYear.GetHeight()) + 30.0

	// The numbers of the charts
	heading := pdfjet.NewTextLine(f1, "The five makes")
	heading.SetStructureType(structelem.H2).SetFontSize(14.0)
	heading.SetLocation(50.0, y)
	heading.DrawOn(page)
	data := [][]*pdfjet.Cell{row(f1, true, "Make", "Cars", "Share of all", "Battery electric", "Average range")}
	for i := range makes {
		average := ""
		if ranged[i] != 0 {
			average = strconv.Itoa(ranges[i]/ranged[i]) + " mi"
		}
		data = append(data, row(f2, false, names[i], strconv.Itoa(cars[i]),
			fmt.Sprintf("%.1f%%", float32(100*cars[i])/float32(total)), strconv.Itoa(battery[i]), average))
	}
	table := pdfjet.NewTable()
	table.SetTableData(data, 1)
	for i, width := range []float32{142.0, 80.0, 90.0, 100.0, 100.0} {
		table.SetColumnWidth(i, width)
	}
	table.SetLocation(50.0, y+10.0)
	y = table.DrawOn(page)[1] + 30.0

	// One car of the data, its photo and its VIN in their cells
	heading = pdfjet.NewTextLine(f1, "One car of the data")
	heading.SetStructureType(structelem.H2).SetFontSize(14.0)
	heading.SetLocation(50.0, y)
	heading.DrawOn(page)
	image := pdfjet.NewImageFromFile(pdf, "images/TeslaX.png")
	image.SetAltDescription("A white Tesla Model X with a black glass roof, seen from the front left.")
	image.ScaleBy(1.0 / 3.0) // The image is 120 DPI, so 0.6 of its pixels
	vin := pdfjet.NewBarcode(pdfjet.CODE_128, modelX[0])
	vin.SetModuleLength(0.75)
	vin.SetFont(f2)
	data = [][]*pdfjet.Cell{row(f1, true, "Car", "Model", "VIN")}
	for _, cell := range data[0] {
		cell.SetTextAlignment(alignment.Left) // Text, not numbers
	}
	car := row(f2, false, "", "Tesla Model X, "+modelX[5]+", "+modelX[2]+", "+modelX[10]+" mi of range", "")
	for _, cell := range car {
		cell.SetTextAlignment(alignment.Left)
	}
	car[0].SetImage(image)
	car[2].SetBarcode(vin)
	data = append(data, car)
	table = pdfjet.NewTable()
	table.SetTableData(data, 1)
	table.SetColumnWidth(0, image.GetWidth()+8.0)
	table.SetColumnWidth(1, 220.0)
	table.SetColumnWidth(2, 512.0-image.GetWidth()-228.0)
	table.SetLocation(50.0, y+10.0)
	table.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// row returns a row of cells of the font at 9 points, one for each text, in
// the look of the charts: a light band behind the header row, a thin gray
// line under each row, and the numbers, the cells after the first, aligned
// right.
func row(font *pdfjet.Font, header bool, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for i, text := range texts {
		cell := pdfjet.NewCell(font, text).SetFontSize(9.0)
		cell.SetTopPadding(5.0).SetBottomPadding(5.0).SetLeftPadding(6.0).SetRightPadding(6.0)
		cell.SetBorders(false).SetBorder(border.Bottom, true)
		cell.SetBorderWidth(0.5).SetBorderColor(0xD5D9DE)
		if header {
			cell.SetBackgroundColor(0xF1F3F6)
		}
		if i > 0 {
			cell.SetTextAlignment(alignment.Right)
		}
		cells = append(cells, cell)
	}
	return cells
}

// indexOf returns the index of the text in the list, or -1.
func indexOf(list []string, text string) int {
	for i, item := range list {
		if item == text {
			return i
		}
	}
	return -1
}

func main() {
	time0 := time.Now().UnixMilli()
	Example20()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_20 => %4d ms\n", time1-time0)
}
