package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example39 draws a bar chart with horizontal bars: one series, the value
// written at the end of each bar.
func Example39() {
	pdf := pdfjet.NewPDFFile("Example_39.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(10.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Longest rivers")
	chart.SetXAxisTitle("Length in km")
	chart.SetCategories("Nile", "Amazon", "Yangtze", "Mississippi", "Yenisei", "Yellow River")
	chart.AddSeriesWithColor("", []float32{6650.0, 6400.0, 6300.0, 6275.0, 5539.0, 5464.0}, color.SteelBlue)
	chart.SetHorizontal(true)
	chart.SetDrawValueLabels(true)
	chart.SetLocation(70.0, 50.0)
	chart.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example39()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_39", time0, time1)
}
