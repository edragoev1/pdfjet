package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example40 -- TODO:
func Example40() {
	pdf := pdfjet.NewPDFFile("Example_40.pdf", compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetItalic(true)
	f1.SetSize(10.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetItalic(true)
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	chart := pdfjet.NewChart(f1, f2)
	chart.SetData(GetData())
	chart.SetLocation(70.0, 50.0)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Vertical Bar Chart Example")
	chart.SetXAxisTitle("Bar Chart")
	chart.SetYAxisTitle("Vertical")
	chart.SetDrawYAxisLines(false)
	chart.SetDrawXAxisLabels(false)
	chart.SetXYChart(false)

	chart.DrawOn(page)

	pdf.Complete()
}

// GetData returns the chart data.
func GetData() [][]*pdfjet.Point {
	chartData := make([][]*pdfjet.Point, 0)
	/*
		AddVerticalBar(chartData, 15.0, 24f, 45f, Color.blue, " Vertical", Color.white)
		AddVerticalBar(chartData, 17.0, 24f, 75f, Color.yellow, " Vertical", Color.black)
		AddVerticalBar(chartData, 19.0, 24f, 65f, Color.peachpuff, " Vertical", Color.black)
		AddVerticalBar(chartData, 25.0, 24f, 20f, Color.green, " Bar", Color.white)
		AddVerticalBar(chartData, 35.0, 24f, 31f, Color.red, " Chart", Color.white)
		AddVerticalBar(chartData, 45.0, 24f, 73f, Color.gold, " Example", Color.black)
	*/
	w := float32(14.0)
	x := float32(10.0)
	dx1 := float32(16.0)
	dx2 := float32(26.0)
	AddVerticalBar(&chartData, x, w, 45.0, color.Green, " January", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 75.0, color.Red, " January", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 65.0, color.Green, " February", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 20.0, color.Red, " February", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " March", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " March", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 45.0, color.Green, " April", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 75.0, color.Red, " April", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 65.0, color.Green, " May", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 20.0, color.Red, " May", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " June", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " June", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " July", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " July", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " August", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " August", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " Septemeber", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " Septemeber", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " October", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " October", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " November", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " November", color.White)
	x += dx2
	AddVerticalBar(&chartData, x, w, 31.0, color.Green, " December", color.White)
	x += dx1
	AddVerticalBar(&chartData, x, w, 73.0, color.Red, " December", color.White)

	return chartData
}

// AddVerticalBar adds vertical bar to the chart.
func AddVerticalBar(
	chartData *[][]*pdfjet.Point,
	x, w, h float32,
	color int32,
	text string,
	textColor int32) {
	path1 := make([]*pdfjet.Point, 0)

	point := pdfjet.NewPoint(x, 0.0)
	point.SetDrawPath()
	point.SetX(x)
	// point.SetY(0.0)
	point.SetShape(shape.Invisible)
	point.SetColor(color)
	point.SetLineWidth(w)
	point.SetText(text)
	point.SetTextColor(textColor)
	point.SetTextDirection(90)
	path1 = append(path1, point)

	point = pdfjet.NewPoint(x, 0.0)
	// point.SetX(x)
	point.SetY(h)
	point.SetShape(shape.Invisible)
	path1 = append(path1, point)

	*chartData = append(*chartData, path1)
}

func main() {
	start := time.Now()
	Example40()
	elapsed := time.Since(start)
	fmt.Printf("Example_40 => %dµs\n", elapsed.Microseconds())
}
