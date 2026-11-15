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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const sender = "Northwind Studio"
const senderAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"

var date = time.Date(2026, time.September, 25, 0, 0, 0, 0, time.UTC)

const recipient = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4"
const subject = "Your annual report, accessible edition"
const salutation = "Dear Ms. Raman,"

var paragraphs = []string{
	"Thank you for choosing us for this year's annual report. The printed copies are on " +
		"their way, and the accessible PDF edition is on the USB drive enclosed.",

	"The PDF meets the PDF/UA standard, so screen readers read it in the right order, " +
		"with every picture described. If any of your readers has trouble with it, tell us " +
		"and we will fix it at no charge.",

	"Our invoice for the work is enclosed. We look forward to working with you again.",
}

const closing = "Sincerely,"
const signer = "Dana Whitfield\nDesign Lead, Northwind Studio"
const enclosure = "Enclosures: Invoice 2026-042, USB drive"

const rule = 0xdde1e8

// The space between the parts of the letter, and between its paragraphs
const gap float32 = 14.0

// Example59 draws a business letter on Letter paper: the letterhead of the
// sender, the date, the address of the recipient, the subject, the
// paragraphs of the letter, the closing and the signature block, and a line
// for the enclosure.
//
// Each part is placed under the one before it: DrawOn returns where a
// TextBlock ended, so a longer address or one more paragraph moves
// everything below it down, and nothing is placed by hand twice. The
// paragraphs are TextBlocks, which wrap the text to the width of the page,
// with a blank line between them and no indent.
//
// The document is PDF/UA-1: the subject is the heading of the letter, the
// rule under the letterhead is an artifact, and the language is set, so that
// a screen reader reads the letter as it is written. The layout is that of
// the business letter template of PDFjet Forms. The people and the companies
// are made up.
func Example59() {
	pdf, err := pdfjet.NewPDFFile("Example_59.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle(subject)
	pdf.SetAuthor(sender)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())
	var left float32 = 48.0
	width := page.GetWidth() - float32(2*left)

	// The letterhead
	pdfjet.NewTextLine(semiBold, sender).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	y := paragraph(page, regular, senderAddress, left, 80.0, width)

	// A thin rule under the letterhead, decoration only
	y += gap
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The date, written out in words, so that 10/12 is not read as the
	// 10th of December. Go writes the names of the months in English,
	// whatever the locale of the computer.
	y = paragraph(page, regular, date.Format("January 2, 2006"), left, y+2*gap, width)

	// The recipient, as on the envelope
	y = paragraph(page, regular, recipient, left, y+gap, width)

	// The subject, the heading of the letter for a screen reader
	block := pdfjet.NewTextBlock(semiBold, subject)
	block.SetStructureType(structelem.H1)
	block.SetLocation(left, y+gap)
	block.SetWidth(width)
	y = block.DrawOn(page)[1]

	// The body: the salutation and the paragraphs, a blank line between them
	y = paragraph(page, regular, salutation, left, y+gap, width)
	for _, text := range paragraphs {
		y = paragraph(page, regular, text, left, y+gap, width)
	}

	// The closing, room to sign by hand, and the name under it
	y = paragraph(page, regular, closing, left, y+gap, width)
	y = paragraph(page, regular, signer, left, y+3*gap, width)

	paragraph(page, regular, enclosure, left, y+2*gap, width)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// paragraph draws the text wrapped to the width, its top at y, and returns
// the y where it ended, for the next part of the letter.
func paragraph(page *pdfjet.Page, font *pdfjet.Font, text string, x, y, width float32) float32 {
	block := pdfjet.NewTextBlock(font, text)
	block.SetLineSpacing(1.25)
	block.SetLocation(x, y)
	block.SetWidth(width)
	return block.DrawOn(page)[1]
}

func main() {
	time0 := time.Now().UnixMilli()
	Example59()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_59 => %4d ms\n", time1-time0)
}
