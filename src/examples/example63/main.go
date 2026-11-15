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

const office = "Maple Lane Accounting"
const contact = "Maple Lane Accounting, 85 Mill Street, Elora, ON N0B 1S0. " +
	"Phone 519-555-0142, or email appointments@example.com."

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d
const shade = 0xf1f3f6

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

// Example63 draws an appointment request form to print and fill in by hand,
// for the front desk of a small accounting office: the name and how to reach
// the client, what the appointment is for, two dates that suit, how to meet,
// and room for a note; at the foot, on a shade, the part the office fills in
// when it books the time.
//
// This is a form, not a finished document: the client fills it in. It is
// drawn as the job application of Example_72 is, a label above each box, and
// the questions with a few answers are radio buttons. Those answers are laid
// out by a helper that measures each button first, with DrawOn(nil), and
// starts a new line when the next one would pass the width of its field. So
// the four answers of "What is it for?" wrap to two lines in half the page,
// and the row under it starts below the taller of the two fields beside each
// other.
//
// The document is PDF/UA-1: the title and the three sections are headings,
// the labels and the answers are text, and the boxes are artifacts. The
// layout is that of the appointment request template of PDFjet Forms. The
// office is made up.
func Example63() {
	pdf, err := pdfjet.NewPDFFile("Example_63.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Appointment request")
	pdf.SetAuthor(office)
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

	// The title, what happens to the form, and a rule
	f.heading("Book an appointment", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Tell us when suits you, and we will confirm "+
		"by email or phone within one working day.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// Who you are and how to reach you
	y = f.heading("Your details", structelem.H2, 14.0, y+46.0)
	y = f.field("Full name *", left, y, width, 1, "")
	f.field("Email *", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")

	// The appointment. The two choices share a row, and the next row
	// starts under the one that took more lines.
	y = f.heading("The appointment", structelem.H2, 14.0, y+18.0)
	end := f.choice("What is it for? *", left, y, half,
		"First consultation", "Follow-up", "Review of a document", "Other")
	y = max(end, f.choice("Preferred time", left+half+gap, y, half,
		"Morning", "Afternoon", "Evening"))
	f.field("Preferred date *", left, y, half, 1, "")
	y = f.field("Another date that works", left+half+gap, y, half, 1, "")
	y = f.choice("How would you like to meet?", left, y, width,
		"In person", "Video call", "Phone call")
	y = f.field("Anything we should know before we meet", left, y, width, 3, "")

	pdfjet.NewCheckBox(f.textFont, "Send me a reminder the day before").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(page)

	// The office fills in the rest when it books the time, so its part is
	// at the foot of the page, on a shade that sets it apart. The shade is
	// a Rect, and a Rect is drawn as an artifact.
	officeY := page.GetHeight() - 132.0
	pdfjet.NewRect(left-8.0, officeY-8.0, width+16.0, 84.0).SetFillColor(shade).DrawOn(page)
	f.heading("For office use", structelem.H2, 12.0, officeY+8.0)
	third := (width - float32(2.0*gap)) / 3.0
	f.field("Booked for", left, officeY+18.0, third, 1, "")
	f.field("With", left+third+gap, officeY+18.0, third, 1, "")
	f.field("Confirmed by, and how", left+float32(2.0*(third+gap)), officeY+18.0, third, 1, "")

	// How to reach the office, at the foot of the page
	contactLine := pdfjet.NewTextLine(f.textFont, contact)
	contactLine.SetFontSize(9.0)
	contactLine.SetTextColor(color.DimGray)
	contactLine.SetLocation(left, page.GetHeight()-36.0)
	contactLine.DrawOn(page)

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

// choice draws a question of a few answers: its label at y and a radio
// button for each answer under it, left to right. A button that would pass
// the width goes to the start of the next line; DrawOn(nil) measures it
// without drawing it. Returns where the next row starts.
func (f *form) choice(text string, x, y, width float32, answers ...string) float32 {
	f.label(text, x, y)
	top := y + 18.0
	left := x
	for _, answer := range answers {
		button := pdfjet.NewRadioButton(f.textFont, answer)
		right := button.SetLocation(left, top).DrawOn(nil)[0]
		if right > x+width && left > x {
			left = x
			top += lineHeight
			button.SetLocation(left, top)
		}
		left = button.DrawOn(f.page)[0] + 12.0
	}
	return top + lineHeight + 10.0
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
	Example63()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_63 => %4d ms\n", time1-time0)
}
