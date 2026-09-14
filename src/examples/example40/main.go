package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example40 draws a bar chart with vertical bars: two series grouped by
// month, with a legend under the title.
func Example40() {
	pdf := pdfjet.NewPDFFile("Example_40.pdf")

	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(10.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetSize(8.0)

	chart := pdfjet.NewBarChart(f1, f2)
	chart.SetSize(500.0, 300.0)
	chart.SetTitle("Units sold by month")
	chart.SetXAxisTitle("Month")
	chart.SetYAxisTitle("Units")
	chart.SetCategories(
		"Jan", "Feb", "Mar", "Apr", "May", "Jun",
		"Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
	chart.AddSeriesWithColor("2025",
		[]float32{45.0, 65.0, 31.0, 45.0, 65.0, 31.0, 38.0, 52.0, 47.0, 59.0, 66.0, 72.0},
		color.SeaGreen)
	chart.AddSeriesWithColor("2026",
		[]float32{75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0},
		color.IndianRed)
	chart.SetGroupGap(0.4)
	chart.SetBarGap(0.1)
	chart.SetLocation(70.0, 50.0)
	chart.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example40()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_40", time0, time1)
}
