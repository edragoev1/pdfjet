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

//
// Example_51.java
//  
// This example shows how to add "Page X of N" footer to every page of
// the PDF file. In this case we create new PDF and store it in a buffer.
//
func Example51() {
	file, err := os.Create("temp.pdf")
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

	AddFooterToPDF()
}

func AddFooterToPDF() {
	file, err := os.Create("Example_51.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)
	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	buf, err := os.ReadFile("temp.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects := pdf.Read(buf)

	file, err = os.Open("fonts/Droid/DroidSans.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader := bufio.NewReader(file)
	font := pdfjet.NewFontStream2(&objects, reader)
	font.SetSize(12.0)

	pages := pdf.GetPageObjects(objects)
	for i := 0; i < len(pages); i++ {
		footer := "Page " + fmt.Sprint(i+1) + " of " + fmt.Sprint((len(pages)))
		page := pdfjet.NewPageFromObject(pdf, pages[i])
		page.AddFontResource(font, &objects)
		page.SetBrushColor(color.Transparent) // Required!
		page.SetBrushColor(color.Black)
		page.DrawString(
			font,
			nil,
			footer,
			(page.GetWidth()-font.StringWidth(font, footer))/2.0,
			(page.GetHeight() - 5.0))
		page.Complete(&objects)
	}
	pdf.AddObjects(&objects)
	pdf.Complete()
}

func main() {
	start := time.Now()
	Example51()
	elapsed := time.Since(start)
	fmt.Printf("Example_51 => %dµs\n", elapsed.Microseconds())
}
