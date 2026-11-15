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

const company = "Northwind Studio"

var kindsOfLeave = []string{
	"Vacation", "Sick leave", "Personal day", "Parental leave", "Other",
}

const blue = 0x1f58b0
const red = 0xb00020
const ruleColor = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and
// the gap between two fields of a row
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half = (width - gap) / 2.0
const quarter = (half - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each
// line more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// The signature box and the date box beside it, 60 and 40 percent, in
// float32 arithmetic, as the other ports compute them
var signatureShare float32 = 0.6
var dateShare float32 = 0.4
var signature = float32(signatureShare*width) - gap/2.0
var dateWidth = float32(dateShare*width) - gap/2.0

// form holds the page and the fonts of the form.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example82 draws a time off request, to print and fill in by hand: the
// employee writes the kind of leave, the first and the last day, and signs;
// the manager writes the decision in a part of the form of its own, and
// signs too.
//
// This is a form people fill in, not a document with data. It is drawn as
// Example_72 draws the job application, with the same field helper: the
// label above its box, the boxes on a grid, and an asterisk on the label of
// a field that is required. The kind of leave and the decision are radio
// buttons, one of the options, under their label.
//
// Two people fill in the form, one after the other, so the part of the
// manager is set apart: a rule across the page and a heading of its own.
// Its fields have no asterisk, as the employee leaves them empty.
//
// The document is PDF/UA-1: the title and the two parts are headings,
// the labels and the options are text, and the boxes are artifacts. The
// layout is that of the time off request template of PDFjet Forms. The
// company is made up.
func Example82() {
	pdf, err := pdfjet.NewPDFFile("Example_82.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Time off request")
	pdf.SetAuthor(company)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	f.page = pdfjet.NewPage(pdf, letter.Portrait())

	// The title, when to send the form, and a rule
	f.heading("Time off request", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Fill in this form at least two weeks before "+
		"your time off, sign it and give it to your manager.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	// The employee's part
	y = f.heading("Your request", structelem.H2, 14.0, y+46.0)
	f.field("Your name *", left, y, half, 1, "")
	y = f.field("Department", left+half+gap, y, half, 1, "")
	y = f.choice("Type of leave *", left, y, kindsOfLeave...)
	f.field("First day *", left, y, quarter, 1, "")
	f.field("Last day *", left+quarter+gap, y, quarter, 1, "")
	f.field("Working days", left+half+gap, y, quarter, 1, "")
	y = f.field("Phone while away", left+half+gap+quarter+gap, y, quarter, 1, "")
	y = f.field("Notes for your manager", left, y, width, 2, "")

	pdfjet.NewCheckBox(f.textFont, "I confirm this request and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	y += 30.0
	f.field("Your signature *", left, y, signature, 1, "")
	y = f.field("Date *", left+signature+gap, y, dateWidth, 1, "")

	// The manager's part, below a rule, filled in after the employee's
	f.rule(y + 12.0)
	y = f.heading("For your manager", structelem.H2, 14.0, y+42.0)
	y = f.choice("Decision", left, y, "Approved", "Not approved")
	y = f.field("Manager's name", left, y, half, 1, "")
	pdfjet.NewCheckBox(f.textFont, "I confirm this decision and sign below.").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	y += 30.0
	f.field("Manager's signature", left, y, signature, 1, "")
	f.field("Date of decision", left+signature+gap, y, dateWidth, 1, "")

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
// lines of handwriting, with the value in it when there is one, "" for none.
// Returns where the next row starts.
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

// choice draws a question of a few answers: its label at y and a radio
// button for each answer under it, where a box would be, each starting where
// the one before it ended. DrawOn returns the end of a button with some room
// after its text already. Returns where the next row starts.
func (f *form) choice(text string, x, y float32, answers ...string) float32 {
	f.label(text, x, y)
	for _, answer := range answers {
		x = pdfjet.NewRadioButton(f.textFont, answer).SetLocation(x, y+20.0).DrawOn(f.page)[0] + 12.0
	}
	return y + 50.0
}

// rule draws a thin line across the page, decoration only, so an artifact
// that a screen reader skips.
func (f *form) rule(y float32) {
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(ruleColor)
	f.page.AddArtifactBMC()
	line.DrawOn(f.page)
	f.page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example82()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_82 => %4d ms\n", time1-time0)
}
