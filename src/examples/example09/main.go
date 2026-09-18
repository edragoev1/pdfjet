// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"bufio"
	"fmt"
	"log"
	"math"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// country is a country of the data file: its name and its point on the chart.
type country struct {
	name  string
	point *pdfjet.Point
}

// Example09 draws an XY chart of the countries of a data file, each a marker
// that links to a page about the country, with the trend line of the points,
// and a table of the countries with a marker of their own.
func Example09() {
	pdf, err := pdfjet.NewPDFFile("Example_09.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("World View - Communications")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(8.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	countries := readCountries("data/world-communications.txt", "|")

	chart := pdfjet.NewChart(f1, f2)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("World View - Communications")
	chart.SetXAxisTitle("Cell phones per capita")
	chart.SetYAxisTitle("Internet users % of the population")
	markers := chart.AddSeries("").SetStrokeColor(color.Gray)
	for _, c := range countries {
		markers.AddPointWithMarker(c.point)
	}
	addTrendLine(chart, countries)
	chart.SetLocation(70.0, 50.0)
	chart.DrawOn(page)

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	addTableToChart(page, countries, f1, f2)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// addTrendLine calculates and adds the trend line of the countries to the chart.
func addTrendLine(chart *pdfjet.Chart, countries []*country) {
	points := make([]*pdfjet.Point, 0, len(countries))
	for _, c := range countries {
		points = append(points, c.point)
	}
	m := slope(points)
	b := intercept(points, m)
	chart.AddSeries("Trend line").
		SetDrawPath(true).
		SetStrokeColor(color.Blue).
		SetShape(shape.Invisible).
		AddPoint(0.0, b).
		AddPoint(1.5, m*1.5+b)
}

// addTableToChart draws a table of the countries that have a marker of their own.
func addTableToChart(page *pdfjet.Page, countries []*country, f1, f2 *pdfjet.Font) {
	table := pdfjet.NewTable()
	tableData := make([][]*pdfjet.Cell, 0)
	for _, c := range countries {
		if c.point.GetShape() != shape.Circle {
			tableRow := make([]*pdfjet.Cell, 0)

			cell := pdfjet.NewCell(f2, "")
			cell.SetMarker(c.point, alignment.Left)
			tableRow = append(tableRow, cell)

			cell = pdfjet.NewCell(f1, c.name)
			tableRow = append(tableRow, cell)

			cell = pdfjet.NewCell(f2, c.point.GetURIAction())
			tableRow = append(tableRow, cell)

			tableData = append(tableData, tableRow)
		}
	}

	table.SetTableData(tableData, 0)
	table.AutoAdjustColumnWidths()
	table.SetCellBorderWidth(0.2)
	table.SetLocation(70.0, 360.0)
	table.SetColumnWidth(0, 9.0)
	table.DrawOn(page)
}

// readCountries reads the countries from the data file.
// format: country|population|...|cellphones|...|internet
func readCountries(fileName, delimiter string) []*country {
	countries := make([]*country, 0)

	file, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()

		var cols []string
		if delimiter == "|" {
			cols = strings.Split(line, "|")
		} else if delimiter == "\t" {
			cols = strings.Split(line, "\t")
		} else {
			log.Fatal("Only pipes and tabs can be used as delimiters")
		}

		if len(cols) < 8 {
			continue
		}

		populationStr := strings.TrimSpace(cols[1])
		populationStr = strings.ReplaceAll(populationStr, ",", "")
		population, err := strconv.ParseFloat(populationStr, 64)
		if err != nil {
			continue
		}

		name := strings.TrimSpace(cols[0])

		urlName := name
		urlName = strings.ReplaceAll(urlName, " ", "_")
		urlName = strings.ReplaceAll(urlName, "'", "_")
		urlName = strings.ReplaceAll(urlName, ",", "_")
		urlName = strings.ReplaceAll(urlName, "(", "_")
		urlName = strings.ReplaceAll(urlName, ")", "_")

		cellPhonesStr := strings.TrimSpace(cols[5])
		cellPhonesStr = strings.ReplaceAll(cellPhonesStr, ",", "")
		cellPhones, err := strconv.ParseFloat(cellPhonesStr, 64)
		if err != nil {
			continue
		}

		internetStr := strings.TrimSpace(cols[7])
		internetStr = strings.ReplaceAll(internetStr, ",", "")
		internet, err := strconv.ParseFloat(internetStr, 64)
		if err != nil {
			continue
		}

		point := pdfjet.NewPoint(float32(cellPhones/population), float32(internet/population*100))
		point.SetURIAction("http://pdfjet.com/country/" + urlName + ".txt")
		point.SetRadius(2.0)

		if point.GetX() > 1.25 {
			point.SetShape(shape.RightArrow)
			point.SetStrokeColor(color.Black)
		} else if point.GetY() > 80.0 {
			point.SetShape(shape.UpArrow)
			point.SetStrokeColor(color.Blue)
		} else if name == "France" {
			point.SetShape(shape.Multiply)
			point.SetStrokeColor(color.Green)
		} else if name == "Canada" {
			point.SetShape(shape.Box)
			point.SetStrokeColor(color.Orange)
		} else if strings.HasPrefix(name, "United States") {
			point.SetShape(shape.Star)
			point.SetStrokeColor(color.Red)
		}

		countries = append(countries, &country{name: name, point: point})
	}

	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	return countries
}

func main() {
	time0 := time.Now().UnixMilli()
	Example09()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_09 => %4d ms\n", time1-time0)
}

// The slope and intercept of the ordinary least squares trend line of the points.
func slope(points []*pdfjet.Point) float32 {
	return covar(points) / devsq(points) * float32(len(points)-1)
}

func intercept(points []*pdfjet.Point, slope float32) float32 {
	m := mean(points)
	return m[1] - slope*m[0]
}

func mean(points []*pdfjet.Point) []float32 {
	m := make([]float32, 2)
	for _, point := range points {
		m[0] += point.GetX()
		m[1] += point.GetY()
	}
	n := float32(len(points))
	m[0] /= n
	m[1] /= n
	return m
}

func covar(points []*pdfjet.Point) float32 {
	var covariance float32
	m := mean(points)
	for _, point := range points {
		covariance += (point.GetX() - m[0]) * (point.GetY() - m[1])
	}
	return covariance / float32(len(points)-1)
}

func devsq(points []*pdfjet.Point) float32 {
	var sum float32
	m := mean(points)
	for _, point := range points {
		sum += float32(math.Pow(float64(point.GetX()-m[0]), float64(2)))
	}
	return sum
}
