package main

import (
	"a4"
	"bufio"
	"corefont"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"pdfjet/src/color"
	"pdfjet/src/compliance"
	"pdfjet/src/shape"
	"strings"
	"time"
)

// Example39 -- TODO:
func Example39() {
	file, err := os.Create("Example_39.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	f1.SetItalic(true)
	f2.SetItalic(true)

	f1.SetSize(10.0)
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

	chart := pdfjet.NewChart(f1, f2)
	chart.SetLocation(70.0, 50.0)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Horizontal Bar Chart Example")
	chart.SetXAxisTitle("")
	chart.SetYAxisTitle("")
	chart.SetData(getData())
	chart.SetDrawYAxisLabels(false)

	chart.DrawOn(page)

	pdf.Complete()
}

func getData() [][]*pdfjet.Point {
	chartData := make([][]*pdfjet.Point, 0)

	path1 := make([]*pdfjet.Point, 0)

	point := pdfjet.NewPoint(0.0, 45.0)
	point.SetDrawPath()
	point.SetShape(shape.Invisible)
	point.SetColor(color.Blue)
	point.SetLineWidth(20.0)
	point.SetText(" Horizontal")
	point.SetTextColor(color.White)
	path1 = append(path1, point)

	point = pdfjet.NewPoint(35.0, 45.0)
	point.SetShape(shape.Invisible)
	path1 = append(path1, point)

	path2 := make([]*pdfjet.Point, 0)

	point = pdfjet.NewPoint(0.0, 35.0)
	point.SetDrawPath()
	point.SetShape(shape.Invisible)
	point.SetColor(color.Gold)
	point.SetLineWidth(20.0)
	point.SetText(" Bar")
	point.SetTextColor(color.Black)
	path2 = append(path2, point)

	point = pdfjet.NewPoint(22.0, 35.0)
	point.SetShape(shape.Invisible)
	path2 = append(path2, point)

	path3 := make([]*pdfjet.Point, 0)

	point = pdfjet.NewPoint(0.0, 25.0)
	point.SetDrawPath()
	point.SetShape(shape.Invisible)
	point.SetColor(color.Green)
	point.SetLineWidth(20.0)
	point.SetText(" Chart")
	point.SetTextColor(color.White)
	path3 = append(path3, point)

	point = pdfjet.NewPoint(30.0, 25.0)
	point.SetShape(shape.Invisible)
	path3 = append(path3, point)

	path4 := make([]*pdfjet.Point, 0)

	point = pdfjet.NewPoint(0.0, 15.0)
	point.SetDrawPath()
	point.SetShape(shape.Invisible)
	point.SetColor(color.Red)
	point.SetLineWidth(20.0)
	point.SetText(" Example")
	point.SetTextColor(color.White)
	path4 = append(path4, point)

	point = pdfjet.NewPoint(47.0, 15.0)
	point.SetShape(shape.Invisible)
	path4 = append(path4, point)

	chartData = append(chartData, path1)
	chartData = append(chartData, path2)
	chartData = append(chartData, path3)
	chartData = append(chartData, path4)

	return chartData
}

func main() {
	start := time.Now()
	Example39()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_39 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
