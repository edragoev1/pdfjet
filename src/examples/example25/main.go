package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example25 draws a donut chart.
func Example25() {
	pdf, err := pdfjet.NewPDFFile("Example_25.pdf")
	if err != nil {
		log.Fatal(err)
	}

	page := pdfjet.NewPage(pdf, letter.Portrait())

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2.SetSize(10.0)

	chart := pdfjet.NewDonutChart(f1, f2)
	chart.SetLocation(100.0, 200.0)
	chart.SetRadii(200.0, 120.0) // an inner radius of 0 makes a pie chart

	chart.AddSlice(pdfjet.NewSlice(25.0, 0xC1121F, "Apples"))  // deep red
	chart.AddSlice(pdfjet.NewSlice(20.0, 0x1D3557, "Oranges")) // navy blue
	chart.AddSlice(pdfjet.NewSlice(30.0, 0x1A7468, "Bananas")) // dark teal
	chart.AddSlice(pdfjet.NewSlice(15.0, 0xD97706, "Grapes"))  // burnt orange
	chart.AddSlice(pdfjet.NewSlice(10.0, 0xCAAA2F, "Lemons"))  // dark gold
	chart.DrawOn(page)

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
