// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexMono"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Example53 draws paragraphs written with the inline markup of Markdown:
// **bold**, *italic*, `code` and [links](url), each drawn in its own font by
// Markup, with the punctuation after a word in another style next to it. The
// paragraphs flow in a text frame, and the list is a list of paragraphs with
// labels.
func Example53() {
	pdf, err := pdfjet.NewPDFFile("Example_53.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Inline markup")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(11.0)
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold).SetSize(11.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic).SetSize(11.0)
	boldItalic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.BoldItalic).SetSize(11.0)
	code := pdfjet.NewFontFromFile(pdf, IBMPlexMono.Regular).SetSize(10.0)
	heading := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	markup := pdfjet.NewMarkup(regular, bold, italic, boldItalic, code)

	paragraphs := []*pdfjet.Paragraph{
		pdfjet.NewParagraph().Add(pdfjet.NewTextLine(heading, "Inline markup").SetFontSize(22.0)).
			SetStructureType(structelem.H1),
	}
	paragraphs = append(paragraphs, markup.Paragraphs(
		"**Markup** reads the inline markup of Markdown and draws each part in its own "+
			"font: **bold**, *italic*, ***bold italic***, `code` and "+
			"[links](https://pdfjet.com). A word keeps the punctuation after it, as in "+
			"*this*, and a mark with no match, such as the one in 2 * 3, is text.\n"+
			"\n"+
			"A backslash makes a mark text too: \\*not italic\\*. Code keeps its marks "+
			"as they are, as in `a*b*c`, and a link can have emphasis in it: "+
			"[the **PDFjet** repository](https://github.com/edragoev1/pdfjet).")...)

	items := []string{
		"`Markup.paragraph` makes one paragraph of a text.",
		"`Markup.paragraphs` makes one of each part between the empty lines.",
		"The paragraphs go in a **TextFrame** or a **TextColumn**, as any others do.",
	}
	for i, item := range items {
		paragraphs = append(paragraphs, markup.Paragraph(item).
			SetListLabel(pdfjet.NewTextLine(regular, strconv.Itoa(i+1)+"."), 16.0))
	}

	frame := pdfjet.NewTextFrameFromParagraphs(paragraphs)
	frame.SetLocation(70.0, 70.0)
	frame.SetWidth(470.0).SetParagraphGap(8.0)
	pages := make([]*pdfjet.Page, 0)
	frame.DrawOnPages(pdf, &pages, letter.Portrait())
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example53()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_53 => %4d ms\n", time1-time0)
}
