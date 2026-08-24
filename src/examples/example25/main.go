package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example25 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example25() {
	pdf := pdfjet.NewPDFFile("Example_25.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())

	page := pdfjet.NewPage(pdf, letter.Portrait)

	chart := pdfjet.NewDonutChart(f1, f2, true)
	chart.SetLocation(300.0, 400.0)
	chart.SetR1AndR2(200.0, 120.0)

	chart.AddSlice(pdfjet.NewSlice(90.0, 0xC1121F, "Apples", ""))   // deep red
	chart.AddSlice(pdfjet.NewSlice(72.0, 0x1D3557, "Oranges", ""))  // navy blue
	chart.AddSlice(pdfjet.NewSlice(108.0, 0x1A7468, "Bananas", "")) // dark teal
	chart.AddSlice(pdfjet.NewSlice(54.0, 0xD97706, "Grapes", ""))   // burnt orange
	chart.AddSlice(pdfjet.NewSlice(36.0, 0xCAAA2F, "Lemons", ""))   // dark gold
	_ = chart.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example25()
	pdfjet.PrintDuration("Example_25", time.Since(start))
}
