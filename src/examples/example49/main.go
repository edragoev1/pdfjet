package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example49 -- TODO:
func Example49() {
	pdf := pdfjet.NewPDFFile("Example_49.pdf", compliance.PDF15)
	image1 := pdfjet.NewImageFromFile(pdf, "images/photoshop.jpg")

	page := pdfjet.NewPage(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example49()
	elapsed := time.Since(start)
	fmt.Printf("Example_49 => %dµs\n", elapsed.Microseconds())
}
