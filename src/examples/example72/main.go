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
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const company = "Northwind Studio"
const returnTo = "Please return the form to Northwind Studio, " +
	"12 Harbour Street, Port Elgin, ON N0H 2C0, or scan it to jobs@example.com."
const position = "Junior graphic designer (opening 2026-17)"

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half = (width - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each line
// more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// form is the page being drawn on and the fonts of the form.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example72 draws a job application form to print and fill in by hand: a box
// under each label, larger boxes for the address and the experience, radio
// buttons for the hours, check boxes for the consent, and a box to sign in.
//
// The form is drawn, not interactive: PDFjet draws text, lines and boxes,
// and makes no fields that a viewer types into. That is the form most people
// meet, on a clipboard at the front desk; it can also be filled in on screen
// with the tools of a PDF viewer that type or sign on a page. So the boxes
// are sized for handwriting, 24 points, about a third of an inch, tall, and
// each label is above its box, where it stays readable once the box is
// filled. A required field says so in its label, with an asterisk, as the
// note at the top explains.
//
// The employer prints the form for one opening, so the position is filled in
// already: the field helper draws a value in its box when it is given one.
// The fields are placed on a grid of two columns with a gap between them, so
// that the boxes of the rows line up.
//
// The document is PDF/UA-1: the title and the two sections are headings, the
// labels and the options are text, and the boxes are artifacts. The layout
// is that of the job application template of PDFjet Forms. The company is
// made up.
func Example72() {
	pdf, err := pdfjet.NewPDFFile("Example_72.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Job application")
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

	// The title, what happens to the form, and a rule
	f.heading("Job application", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, company+" thanks you for your interest in working "+
		"with us. We read every application and answer within two weeks.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	// Your contact details
	y = f.heading("Your contact details", structelem.H2, 14.0, y+46.0)
	f.field("First name *", left, y, half, 1, "")
	y = f.field("Last name *", left+half+gap, y, half, 1, "")
	f.field("Email *", left, y, half, 1, "")
	y = f.field("Phone *", left+half+gap, y, half, 1, "")
	y = f.field("Street address", left, y, width, 2, "")
	quarter := (half - gap) / 2.0
	f.field("City", left, y, half, 1, "")
	f.field("State or province", left+half+gap, y, quarter, 1, "")
	y = f.field("Postal code", left+half+gap+quarter+gap, y, quarter, 1, "")

	// The position
	y = f.heading("The position", structelem.H2, 14.0, y+18.0)
	f.field("Position you apply for *", left, y, half, 1, position)
	y = f.field("Available from", left+half+gap, y, half, 1, "")

	// The hours, one of three, beside their label: each button starts where
	// the one before it ended
	f.label("Hours", left, y)
	x := left + 70.0
	for _, option := range []string{"Full time", "Part time", "Either"} {
		x = pdfjet.NewRadioButton(f.textFont, option).SetLocation(x, y-1.0).DrawOn(f.page)[0] + 24.0
	}
	y += 30.0

	y = f.field("Your experience for this position *", left, y, width, 4, "")

	// The consent, and the signature under it
	pdfjet.NewCheckBox(f.textFont, "You may keep my application for other positions for six months").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	pdfjet.NewCheckBox(f.textFont, "I confirm the above is correct and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y+20.0).
		DrawOn(f.page)
	y += 50.0
	f.field("Signature *", left, y, float32(0.6)*width-gap/2.0, 1, "")
	f.field("Date *", left+float32(0.6)*width+gap/2.0, y, float32(0.4)*width-gap/2.0, 1, "")

	// Where the form goes, at the foot of the page
	line := pdfjet.NewTextLine(f.textFont, returnTo)
	line.SetFontSize(9.0)
	line.SetTextColor(color.DimGray)
	line.SetLocation(left, f.page.GetHeight()-36.0)
	line.DrawOn(f.page)

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
// lines of handwriting, with the value in it when there is one, a value that
// is not "". Returns where the next row starts.
func (f *form) field(text string, x, y, w float32, lines int, value string) float32 {
	f.label(text, x, y)
	top := y + 16.0
	height := boxHeight + float32(float32(lines-1)*lineHeight)
	rect := pdfjet.NewRect(x, top, w, height)
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
	Example72()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_72 => %4d ms\n", time1-time0)
}
