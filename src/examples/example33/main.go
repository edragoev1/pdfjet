package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/imagetype"
)

// Example33 -- TODO:
func Example33() {
	file, err := os.Create("Example_33.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	file1, err := os.Open("images/photoshop.jpg")
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	reader := bufio.NewReader(file1)
	image1 := pdfjet.NewImage(pdf, reader, imagetype.JPG)

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)

	image1.SetLocation(10.0, 10.0)
	image1.ScaleBy(0.25)
	image1.DrawOn(page)

	file3, err := os.Open("images/svg-test/test-CC.svg")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader3 := bufio.NewReader(file3)
	image3 := pdfjet.NewSVGImage(reader3)
	image3.SetLocation(20.0, 670.0)
	image3.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example33()
	elapsed := time.Since(start)
	fmt.Printf("Example_33 => %dµs\n", elapsed.Microseconds())
}
