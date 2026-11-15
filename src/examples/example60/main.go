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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSerif"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const issuer = "Northwind Studio"
const title = "Certificate of completion"
const name = "Robin Brook"
const workshop = "Design with PDF"

var date = time.Date(2026, time.November, 14, 0, 0, 0, 0, time.UTC)

const instructor = "Alex Morgan, instructor"

const navy = 0x1d3557
const ink = 0x333333
const gray = 0x4d4d4d

const margin float32 = 48.0
const band float32 = 16.0 // The height of the bands

// fonts are the fonts of the certificate.
type fonts struct {
	sans        *pdfjet.Font
	serif       *pdfjet.Font
	serifBold   *pdfjet.Font
	serifItalic *pdfjet.Font
}

// Example60 draws a certificate of completion of a workshop, on Letter paper
// turned on its side: a band of color at the top and the bottom, the studio
// that gives it, the title, the name of the participant on a line, what was
// completed, and the lines of the instructor's signature and of the date.
//
// It is a document people receive, finished: everything on it is centered,
// so each line is measured and placed half its width left of the middle of
// the page, and the paragraph of what was completed is a TextBlock centered
// in a column narrower than the page. The serif faces of IBM Plex give it
// the look of a printed certificate; the name is the largest text after the
// title, as it is what the reader looks for.
//
// The certificate is drawn by a function that takes the name, so that a
// certificate for each person of a class is a loop over the list of names, a
// page each in one document: the fonts are embedded once, however many
// pages there are, and the file is printed in one go.
//
// The document is PDF/UA-1: the title is a heading, the bands and the lines
// are artifacts, and the language is set. The layout is that of the
// certificate template of PDFjet Forms. The people and the studio are made up.
func Example60() {
	pdf, err := pdfjet.NewPDFFile("Example_60.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle(title)
	pdf.SetAuthor(issuer)
	pdf.SetLanguage("en-CA")

	f := &fonts{
		sans:        pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold),
		serif:       pdfjet.NewFontFromFile(pdf, IBMPlexSerif.Regular),
		serifBold:   pdfjet.NewFontFromFile(pdf, IBMPlexSerif.SemiBold),
		serifItalic: pdfjet.NewFontFromFile(pdf, IBMPlexSerif.Italic),
	}

	// One certificate. For a class, a page each from the list of names:
	// for _, name := range names {
	//     drawCertificate(pdfjet.NewPage(pdf, letter.Landscape()), f, name)
	// }
	drawCertificate(pdfjet.NewPage(pdf, letter.Landscape()), f, name)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawCertificate draws the certificate of the person on the page.
func drawCertificate(page *pdfjet.Page, f *fonts, name string) {
	width := page.GetWidth()
	inner := width - 2*margin // The width between the margins
	middle := width / 2.0

	// The bands at the top and the bottom. A Rect is decoration, an
	// artifact, on its own.
	pdfjet.NewRect(margin, margin, inner, band).SetFillColor(navy).DrawOn(page)
	pdfjet.NewRect(margin, page.GetHeight()-margin-band, inner, band).
		SetFillColor(navy).DrawOn(page)

	// Who gives it, and the title, the heading of the page
	center(page, f.sans, 13.0, issuer, gray, 112.0).DrawOn(page)
	heading := center(page, f.serifBold, 38.0, title, navy, 170.0)
	heading.SetStructureType(structelem.H1)
	heading.DrawOn(page)

	// The name, on a line as wide as three fifths of the page
	center(page, f.serifItalic, 15.0, "This certifies that", ink, 214.0).DrawOn(page)
	center(page, f.serifBold, 30.0, name, ink, 264.0).DrawOn(page)
	lineWidth := float32(0.6 * inner)
	rule(page, middle-lineWidth/2.0, middle+lineWidth/2.0, 286.0)

	// What was completed, in a column of seven tenths of the page, each
	// line centered in it. Go writes the names of the months in English,
	// whatever the locale of the computer.
	date := date.Format("January 2, 2006")
	columnWidth := float32(0.7 * inner)
	what := pdfjet.NewTextBlock(f.serif, "has completed "+workshop+", a hands-on workshop "+
		"of one day, on "+date+", in Port Elgin, Ontario: layouts that print the "+
		"same everywhere, fillable forms, and accessible PDF.")
	what.SetFontSize(14.0)
	what.SetTextColor(ink)
	what.SetLineSpacing(1.15)
	what.SetTextAlignment(alignment.Center)
	what.SetLocation(middle-columnWidth/2.0, 306.0)
	what.SetWidth(columnWidth)
	y := what.DrawOn(page)[1] + 64.0

	// The signature and the date, each on its line, a little over a third
	// of the page wide, in the left and the right half of the page
	signWidth := float32(0.35 * inner)
	starts := []float32{margin + float32(0.1*inner), margin + float32(0.55*inner)}
	captions := []string{instructor, date}
	for i := 0; i < 2; i++ {
		x := starts[i]
		rule(page, x, x+signWidth, y)
		caption := pdfjet.NewTextLine(f.serif, captions[i])
		caption.SetFontSize(11.0)
		caption.SetTextColor(gray)
		caption.SetLocation(x+(signWidth-caption.GetWidth())/2.0, y+22.0)
		caption.DrawOn(page)
	}
}

// center returns a line of text centered on the page, its baseline at y, for
// the caller to draw, after it has made it a heading where it is one.
func center(page *pdfjet.Page, font *pdfjet.Font, size float32, text string, c int32, y float32) *pdfjet.TextLine {
	line := pdfjet.NewTextLine(font, text)
	line.SetFontSize(size)
	line.SetTextColor(c)
	line.SetLocation((page.GetWidth()-line.GetWidth())/2.0, y)
	return line
}

// rule draws a line to write or sign on, decoration only, so an artifact
// that a screen reader skips.
func rule(page *pdfjet.Page, x1, x2, y float32) {
	line := pdfjet.NewLine(x1, y, x2, y)
	line.SetStrokeWidth(0.75)
	line.SetStrokeColor(ink)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example60()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_60 => %4d ms\n", time1-time0)
}
