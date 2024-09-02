package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/color"
)

/**
 *  Example_18.go
 *  This example shows how to write "Page X of N" footer on every page.
 */
func Example18() {
	pdf := pdfjet.NewPDFFile("Example_18.pdf")

	font := pdfjet.NewFontFromFile(pdf, "fonts/RedHatText/RedHatText-Regular.ttf.stream")
	font.SetSize(12.0)

	pages := make([]*pdfjet.Page, 0)

	page := pdfjet.NewPageDetached(pdf, a4.Portrait)
	box := pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Red)
	box.SetFillShape(true)
	box.DrawOn(page)
	pages = append(pages, page)

	page = pdfjet.NewPageDetached(pdf, a4.Portrait)
	box = pdfjet.NewBox()
	box.SetLocation(50.0, 50.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Green)
	box.SetFillShape(true)
	box.DrawOn(page)
	pages = append(pages, page)

	page = pdfjet.NewPageDetached(pdf, a4.Portrait)
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
	pdfjet.PrintDuration("Example_18", time.Since(start))
}
