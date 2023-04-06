package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example35 -- TODO:
func Example35() {
	pdf := pdfjet.NewPDFFile("Example_35.pdf", compliance.PDF15)

	image1 := pdfjet.NewImageFromFile(pdf, "images/photoshop.jpg")

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example35()
	elapsed := time.Since(start)
	fmt.Printf("Example_35 => %dµs\n", elapsed.Microseconds())
}
