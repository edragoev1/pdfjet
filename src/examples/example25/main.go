package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example25 draws a donut chart.
func Example25() {
	pdf := pdfjet.NewPDFFile("Example_25.pdf")

	page := pdfjet.NewPage(pdf, letter.Portrait)

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2.SetSize(10.0)

	chart := pdfjet.NewDonutChart(f1, f2, true) // true = full donut (with hole)
	chart.SetLocation(300.0, 400.0)
	chart.SetR1AndR2(200.0, 120.0)

	chart.AddSlice(pdfjet.NewSlice(90.0, 0xC1121F, "Apples", ""))   // deep red
	chart.AddSlice(pdfjet.NewSlice(72.0, 0x1D3557, "Oranges", ""))  // navy blue
	chart.AddSlice(pdfjet.NewSlice(108.0, 0x1A7468, "Bananas", "")) // dark teal
	chart.AddSlice(pdfjet.NewSlice(54.0, 0xD97706, "Grapes", ""))   // burnt orange
	chart.AddSlice(pdfjet.NewSlice(36.0, 0xCAAA2F, "Lemons", ""))   // dark gold
	chart.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example25()
	pdfjet.PrintDuration("Example_25", time.Since(start))
}
