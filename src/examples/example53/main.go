package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/content"
)

// Example53 -- TODO:
func Example53(fileName string) {
	pdf := pdfjet.NewPDFFile("Example_53.pdf")
	objects := pdf.Read(content.OfBinaryFile(fileName))

	pages := pdf.GetPageObjects(objects)
	for _, pageObj := range pages {
		page := pdfjet.NewPageFromObject(pdf, pageObj)
		page.DrawLine(0.0, 0.0, 200.0, 200.0)
		page.Complete(&objects) // The graphics stack is unwinded automatically
	}
	pdf.AddObjects(&objects)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example53("../testPDFs/cairo-graphics-1.pdf")
	// Example53("../testPDFs/cairo-graphics-2.pdf")
	pdfjet.PrintDuration("Example_53", time.Since(start))
}
