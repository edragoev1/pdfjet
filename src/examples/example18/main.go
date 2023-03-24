package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

/**
 *  Example_18.go
 *  This example shows how to write "Page X of N" footer on every page.
 */
func Example18() {
	file, err := os.Create("Example_18.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)
	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	page := pdfjet.NewPageAddTo(pdf, letter.Portrait)
	box := pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Red)
	box.SetFillShape(true)
	box.DrawOn(page)

	page = pdfjet.NewPageAddTo(pdf, letter.Portrait)
	box = pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Green)
	box.SetFillShape(true)
	box.DrawOn(page)

	page = pdfjet.NewPageAddTo(pdf, letter.Portrait)
	box = pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Blue)
	box.SetFillShape(true)
	box.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example18()
	elapsed := time.Since(start)
	fmt.Printf("Example_18 => %dµs\n", elapsed.Microseconds())
}
