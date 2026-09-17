// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/SourceSerif4"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example49 draws a menu with paragraphs that mix fonts, sizes and colors,
// currency signs raised with a vertical offset, and a rotated, underlined label.
func Example49() {
	pdf, err := pdfjet.NewPDFFile("Example_49.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Paragraphs with mixed text styles")

	f1 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)
	f1.SetSize(14.0)

	f2 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Italic)
	f2.SetSize(14.0)

	f3 := pdfjet.NewFontFromFile(pdf, SourceSerif4.SemiBold)
	f3.SetSize(14.0)

	f4 := pdfjet.NewFontFromFile(pdf, SourceSerif4.SemiBold)
	f4.SetSize(9.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f3, "Café Menu")
	title.SetFontSize(28.0)
	title.SetLocation(70.0, 100.0)
	title.DrawOn(page)

	// Each paragraph mixes a name, an italic description and a price
	// with a small dollar sign raised by a vertical offset.
	names := []string{"Espresso", "Cappuccino", "Hot chocolate"}
	notes := []string{"rich and intense", "with steamed milk foam", "made with dark cocoa"}
	prices := []string{"3.25", "4.50", "3.95"}

	column := pdfjet.NewTextColumn()
	for i := 0; i < len(names); i++ {
		paragraph := pdfjet.NewParagraph().
			Add(pdfjet.NewTextLine(f3, names[i])).
			Add(pdfjet.NewTextLine(f2, notes[i]).SetTextColor(color.Gray)).
			Add(pdfjet.NewTextLine(f4, "$").SetVerticalOffset(-4.0)).
			Add(pdfjet.NewTextLine(f1, prices[i]).SetTextColor(color.DarkRed))
		column.AddParagraph(paragraph)
	}

	// A paragraph that colors some of its words, aligned to the right.
	column.AddParagraph(pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(f2, "Freshly")).
		Add(pdfjet.NewTextLine(f3, "roasted").SetTextColor(color.SaddleBrown)).
		Add(pdfjet.NewTextLine(f2, "every")).
		Add(pdfjet.NewTextLine(f3, "morning").SetTextColor(color.DarkOrange)).
		SetTextAlignment(alignment.Right))

	column.SetLocation(70.0, 140.0)
	column.SetWidth(470.0)
	column.SetParagraphSpacing(1.8)
	xy := column.DrawOn(page)

	// A TextFrame wraps the words of its paragraphs to its width.
	paragraphs := make([]*pdfjet.Paragraph, 0)
	paragraphs = append(paragraphs, pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(f1, "Our beans come from small farms in")).
		Add(pdfjet.NewTextLine(f3, "Colombia,")).
		Add(pdfjet.NewTextLine(f3, "Ethiopia")).
		Add(pdfjet.NewTextLine(f1, "and")).
		Add(pdfjet.NewTextLine(f3, "Guatemala,")).
		Add(pdfjet.NewTextLine(f1, "and we roast them in small batches.")).
		Add(pdfjet.NewTextLine(f2, "Ask us about the beans of the week.").SetTextColor(color.DarkRed)))
	paragraphs = append(paragraphs, pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(f2, "Prices include tax.").SetTextColor(color.Gray)))

	frame := pdfjet.NewTextFrameFromParagraphs(paragraphs)
	frame.SetLocation(70.0, xy[1]+30.0)
	frame.SetWidth(470.0)
	frame.DrawOn(page)

	label := pdfjet.NewTextLine(f3, "Today's special!")
	label.SetFontSize(18.0)
	label.SetTextColor(color.Red)
	label.SetLocation(400.0, 90.0)
	label.SetTextRotation(15)
	label.SetUnderline(true)
	label.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example49()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_49 => %4d ms\n", time1-time0)
}
