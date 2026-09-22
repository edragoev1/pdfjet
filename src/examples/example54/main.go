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
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example54 draws a Markdown text, data/markdown/pdfjet.md, down the pages by
// Markdown as a PDF/UA document: its headings, paragraphs, lists, quote, code,
// table and image, each tagged for screen readers, with a number at the foot
// of every page. The image is read from data/markdown, the directory that
// SetImageDirectory names.
func Example54() {
	pdf, err := pdfjet.NewPDFFile("Example_54.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PDFjet")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(11.0)
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold).SetSize(11.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic).SetSize(11.0)
	boldItalic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.BoldItalic).SetSize(11.0)
	code := pdfjet.NewFontFromFile(pdf, IBMPlexMono.Regular).SetSize(9.5)

	markdown := pdfjet.NewMarkdown(regular, bold, italic, boldItalic, code)
	markdown.SetImageDirectory("data/markdown")
	pages := make([]*pdfjet.Page, 0)
	markdown.DrawOnPages(pdf, content.OfTextFile("data/markdown/pdfjet.md"), &pages, letter.Portrait())

	for i, page := range pages {
		number := pdfjet.NewTextLine(regular, strconv.Itoa(i+1))
		number.SetFontSize(9.0)
		page.AddFooterOffsetBy(number, 36.0)
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example54()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_54 => %4d ms\n", time1-time0)
}
