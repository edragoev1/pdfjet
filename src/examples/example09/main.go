package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example09 draws the a chart that consists of three paths.
func Example09() {
	pdf := pdfjet.NewPDFFile("Example_09.pdf", compliance.PDF15)

	font1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream")
	font2 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

	font1.SetSize(8.0)
	font2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	chartData := make([][]*pdfjet.Point, 0)

	path1 := make([]*pdfjet.Point, 0)
	path1 = append(path1, pdfjet.NewPoint(50.0, 50.0).SetDrawPath().SetColor(color.Blue))
	path1 = append(path1, pdfjet.NewPoint(55.0, 55.0))
	path1 = append(path1, pdfjet.NewPoint(60.0, 60.0))
	path1 = append(path1, pdfjet.NewPoint(65.0, 58.0))
	path1 = append(path1, pdfjet.NewPoint(70.0, 59.0))
	path1 = append(path1, pdfjet.NewPoint(75.0, 63.0))
	path1 = append(path1, pdfjet.NewPoint(80.0, 65.0))
	chartData = append(chartData, path1)

	path2 := make([]*pdfjet.Point, 0)
	path2 = append(path2, pdfjet.NewPoint(50.0, 30.0).SetDrawPath().SetColor(color.Red))
	path2 = append(path2, pdfjet.NewPoint(55.0, 35.0))
	path2 = append(path2, pdfjet.NewPoint(60.0, 40.0))
	path2 = append(path2, pdfjet.NewPoint(65.0, 48.0).SetShape(shape.Diamond))
	path2 = append(path2, pdfjet.NewPoint(70.0, 49.0))
	path2 = append(path2, pdfjet.NewPoint(75.0, 53.0))
	path2 = append(path2, pdfjet.NewPoint(80.0, 55.0))
	chartData = append(chartData, path2)

	path3 := make([]*pdfjet.Point, 0)
	path3 = append(path3, pdfjet.NewPoint(50.0, 80.0).SetDrawPath().SetColor(color.Green))
	path3 = append(path3, pdfjet.NewPoint(55.0, 70.0))
	path3 = append(path3, pdfjet.NewPoint(60.0, 60.0))
	path3 = append(path3, pdfjet.NewPoint(65.0, 55.0))
	path3 = append(path3, pdfjet.NewPoint(70.0, 59.0))
	path3 = append(path3, pdfjet.NewPoint(75.0, 63.0))
	path3 = append(path3, pdfjet.NewPoint(80.0, 61.0))
	chartData = append(chartData, path3)

	chart := pdfjet.NewChart(font1, font2)
	// chart.SetData(GetData("data/world-communications.txt", "|"))
	chart.SetData(chartData)
	chart.SetLocation(70.0, 50.0)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("World View - Communications")
	chart.SetXAxisTitle("Cell phones per capita")
	chart.SetYAxisTitle("Internet users % of the population")
	// AddTrendLine(chart)

	chart.SetXAxisMinMax(0.0, 100.0, 10)
	chart.SetYAxisMinMax(0.0, 100.0, 10)

	chart.DrawOn(page)

	// AddTableToChart(page, chart, font1, font2)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example09()
	elapsed := time.Since(start)
	fmt.Printf("Example_09 => %.1fms\n", float32(elapsed.Microseconds())/float32(1000.0))
}
