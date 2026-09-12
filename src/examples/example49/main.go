package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/SourceSerif4"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example49 draws paragraphs that mix several text styles.
func Example49() {
	pdf := pdfjet.NewPDFFile("Example_49.pdf")
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Paragraphs with mixed text styles")

	f1 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Regular)
	f1.SetSize(14.0)

	f2 := pdfjet.NewFontFromFile(pdf, SourceSerif4.Italic)
	f2.SetSize(16.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	paragraph1 := pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(f1, "Hello")).
		Add(pdfjet.NewTextLine(f1, "W").SetTextColor(color.Black)).
		Add(pdfjet.NewTextLine(f1, "o").SetTextColor(color.Red)).
		Add(pdfjet.NewTextLine(f1, "r").SetTextColor(color.Green)).
		Add(pdfjet.NewTextLine(f1, "l").SetTextColor(color.Blue)).
		Add(pdfjet.NewTextLine(f1, "d").SetTextColor(color.Black)).
		Add(pdfjet.NewTextLine(f1, "$").SetVerticalOffset(1.0)).
		Add(pdfjet.NewTextLine(f2, "29.95").SetTextColor(color.Blue)).
		SetAlignment(alignment.Right)

	paragraph2 := pdfjet.NewParagraph().
		Add(pdfjet.NewTextLine(f1, "Hello")).
		Add(pdfjet.NewTextLine(f1, "World")).
		Add(pdfjet.NewTextLine(f1, "$")).
		Add(pdfjet.NewTextLine(f2, "29.95").SetTextColor(color.Blue)).
		SetAlignment(alignment.Right)

	column := pdfjet.NewTextColumn(0)
	column.AddParagraph(paragraph1)
	column.AddParagraph(paragraph2)
	column.SetLocation(70.0, 150.0)
	column.SetWidth(500.0)
	column.DrawOn(page)

	paragraphs := make([]*pdfjet.Paragraph, 0)
	paragraphs = append(paragraphs, paragraph1)
	paragraphs = append(paragraphs, paragraph2)

	text := pdfjet.NewText(paragraphs)
	text.SetLocation(70.0, 200.0)
	text.SetWidth(500.0)
	text.DrawOn(page)

	textLine := pdfjet.NewTextLine(f1, "Hello, World!")
	textLine.SetLocation(100.0, 300.0)
	textLine.SetTextDirection(30)
	textLine.SetVerticalOffset(50.0)
	textLine.SetUnderline(true)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example49()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_49", time0, time1)
}
