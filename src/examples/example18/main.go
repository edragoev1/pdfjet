package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
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

	file, err = os.Open("fonts/RedHatText/RedHatText-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	reader := bufio.NewReader(file)
	font := pdfjet.NewFontStream1(pdf, reader)
	font.SetSize(12.0)

	pages := make([]*pdfjet.Page, 0)

	page := pdfjet.NewPage(pdf, a4.Portrait)
	box := pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Red)
	box.SetFillShape(true)
	box.DrawOn(page)
	pages = append(pages, page)

	page = pdfjet.NewPage(pdf, a4.Portrait)
	box = pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Green)
	box.SetFillShape(true)
	box.DrawOn(page)
	pages = append(pages, page)

	page = pdfjet.NewPage(pdf, a4.Portrait)
	box = pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Blue)
	box.SetFillShape(true)
	box.DrawOn(page)
	pages = append(pages, page)

	for i := 0; i < len(pages); i++ {
		page := pages[i]
		footer := "Page " + fmt.Sprint(i+1) + " of " + fmt.Sprint((len(pages)))
		page.SetBrushColor(color.Black)
		page.DrawString(
			font,
			nil,
			footer,
			(page.GetWidth()-font.StringWidth(font, footer))/2.0,
			(page.GetHeight() - 5.0))
	}

	for i := 0; i < len(pages); i++ {
		pdf.AddPage(pages[i])
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example18()
	elapsed := time.Since(start)
	fmt.Printf("Example_18 => %dµs\n", elapsed.Microseconds())
}
