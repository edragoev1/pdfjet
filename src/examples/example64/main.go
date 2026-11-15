// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const studio = "Tidewater Studio"
const returnTo = "Please bring the form to our first meeting at 240 Front " +
	"Street, Kingston, or scan it to hello@example.com."

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half float32 = (width - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each line
// more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// form is the page the form is drawn on, with its fonts.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example64 draws a new client form to print and fill in by hand, that a
// design studio hands to a client before the first meeting: who the client
// is, what the project needs, the budget, the dates, and what the project
// should achieve.
//
// This is a form, not a finished document: the client fills it in. It is
// drawn as the job application of Example_72 is, a label above each box. The
// two questions of several answers, the kind of work and the budget, are
// lists of radio buttons, one answer a line, side by side; the list of five
// answers is longer than the list of four, so the row under them starts
// where the longer one ended. The last answer, "Something else", has a line
// after it to write what, drawn from where the button ended to the edge of
// its column.
//
// The document is PDF/UA-1: the title and the two sections are headings,
// the labels and the answers are text, and the boxes and the write-in line
// are artifacts. The layout is that of the client intake template of PDFjet
// Forms. The studio is made up.
func Example64() {
	pdf, err := pdfjet.NewPDFFile("Example_64.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("New client form")
	pdf.SetAuthor(studio)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	f.page = pdfjet.NewPage(pdf, letter.Portrait())
	page := f.page

	// The title, why the studio asks, and a rule
	f.heading("New client form", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Tell us about you and your project, so that "+
		"we can prepare for our first meeting. It takes about five minutes.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// About you
	y = f.heading("About you", structelem.H2, 14.0, y+46.0)
	f.field("Full name *", left, y, half, 1, "")
	y = f.field("Company", left+half+gap, y, half, 1, "")
	f.field("Email *", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")

	// Your project. The two lists are side by side, and the row under them
	// starts below the longer one.
	y = f.heading("Your project", structelem.H2, 14.0, y+18.0)
	end := f.choice("What do you need? *", left, y, half,
		"Brand identity", "Website", "Print design", "Photography", "Something else")
	y = max(end, f.choice("Budget", left+half+gap, y, half,
		"Under $1,000", "$1,000 to $5,000", "$5,000 to $15,000", "Over $15,000"))
	f.field("Ideal start date", left, y, half, 1, "")
	y = f.field("Deadline", left+half+gap, y, half, 1, "")
	y = f.field("What should the project achieve? *", left, y, width, 4, "")
	y = f.field("How did you hear about us?", left, y, width, 1, "")

	pdfjet.NewCheckBox(f.textFont, "Send me occasional news about our work").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(page)

	// Where the form goes, at the foot of the page
	returnToLine := pdfjet.NewTextLine(f.textFont, returnTo)
	returnToLine.SetFontSize(9.0)
	returnToLine.SetTextColor(color.DimGray)
	returnToLine.SetLocation(left, page.GetHeight()-36.0)
	returnToLine.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// heading draws a heading in blue, its baseline at y, and returns where the
// field under it starts.
func (f *form) heading(text string, level structelem.StructElem, size, y float32) float32 {
	heading := pdfjet.NewTextLine(f.labelFont, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(blue)
	heading.SetLocation(left, y)
	heading.DrawOn(f.page)
	return y + 16.0
}

// label draws the label of a field, its top at y.
func (f *form) label(text string, x, y float32) {
	label := pdfjet.NewTextLine(f.labelFont, text)
	label.SetLocation(x, y+f.labelFont.GetAscent(f.labelFont.GetSize()))
	label.DrawOn(f.page)
}

// field draws a field: its label at y and the box under it, of room for the
// lines of handwriting, with the value in it when there is one. Returns
// where the next row starts.
func (f *form) field(text string, x, y, width float32, lines int, value string) float32 {
	f.label(text, x, y)
	top := y + 16.0
	height := boxHeight + float32(float32(lines-1)*lineHeight)
	rect := pdfjet.NewRect(x, top, width, height)
	rect.SetBorderColor(box)
	rect.SetBorderWidth(0.5)
	rect.DrawOn(f.page)
	if value != "" {
		line := pdfjet.NewTextLine(f.textFont, value)
		line.SetTextColor(blue)
		line.SetLocation(x+6.0, top+16.0)
		line.DrawOn(f.page)
	}
	return top + height + 12.0
}

// choice draws a question of several answers: its label at y and a radio
// button for each answer under it, one a line. An answer that starts with
// "Something else" gets a line after it, to write what. Returns where the
// next row starts.
func (f *form) choice(text string, x, y, width float32, answers ...string) float32 {
	f.label(text, x, y)
	top := y + 18.0
	for _, answer := range answers {
		end := pdfjet.NewRadioButton(f.textFont, answer).SetLocation(x, top).DrawOn(f.page)
		if strings.HasPrefix(answer, "Something else") {
			line := pdfjet.NewLine(end[0], end[1], x+width, end[1])
			line.SetStrokeColor(box)
			line.SetStrokeWidth(0.5)
			f.page.AddArtifactBMC()
			line.DrawOn(f.page)
			f.page.AddEMC()
		}
		top += lineHeight
	}
	return top + 10.0
}

// rule draws a thin line across the page, decoration only, so an artifact
// that a screen reader skips.
func (f *form) rule(y float32) {
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	f.page.AddArtifactBMC()
	line.DrawOn(f.page)
	f.page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example64()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_64 => %4d ms\n", time1-time0)
}
