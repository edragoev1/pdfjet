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
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const company = "Granite Bay Logistics"
const formNumber = "Form HS-04, revised September 2026"

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
const third = (width - 2.0*gap) / 3.0

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

// Example70 draws an incident report form to print and fill in by hand, kept
// in the first aid room of a warehouse: on the first page, who reports, when
// and where it happened, what happened, who was hurt, the witnesses and what
// was done right away, and the signature; on the second, the follow-up that
// the supervisor fills in.
//
// This is a form, not a finished document: the employee and then the
// supervisor fill it in. It is drawn as the job application of Example_72
// is, a label above each box, with larger boxes where the answer takes a few
// lines. The form is two pages, each with its form number and "Page 1 of 2"
// at the foot. The pages are made detached and kept in a list, then added to
// the document at the end, once their number is known; a third page added to
// the form needs no change to the footers.
//
// The document is PDF/UA-1: the title and the supervisor's section are
// headings, the labels and the answers are text, and the boxes and the
// footers are artifacts. The layout is that of the incident report template
// of PDFjet Forms. The company is made up.
func Example70() {
	pdf, err := pdfjet.NewPDFFile("Example_70.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Incident report")
	pdf.SetAuthor(company)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	pages := make([]*pdfjet.Page, 0)

	// The first page, for the person who reports
	f.page = pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, f.page)
	f.heading("Incident report", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Report any accident, injury, damage or near "+
		"miss as soon as you can, while the details are fresh.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	y += 40.0
	f.field("Your name *", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")
	f.field("Date of the incident *", left, y, third, 1, "")
	f.field("Time", left+third+gap, y, third, 1, "")
	y = f.field("Where it happened *", left+2.0*(third+gap), y, third, 1, "")

	// The kind of incident, one of four, in a row under its label
	f.label("What happened? *", left, y)
	x := left
	for _, kind := range []string{"Injury", "Damage to property", "Near miss", "Other"} {
		x = pdfjet.NewRadioButton(f.textFont, kind).SetLocation(x, y+18.0).DrawOn(f.page)[0] + 18.0
	}
	y += 48.0

	y = f.field("Describe what happened, step by step *", left, y, width, 4, "")
	y = f.field("Was anyone hurt? Who, and how?", left, y, width, 2, "")
	y = f.checkBox("First aid or medical care was given", y)
	y = f.field("Witnesses: names and phone numbers", left, y+10.0, width, 2, "")
	y = f.field("What was done right away?", left, y, width, 2, "")
	y = f.checkBox("I confirm this report is true to the best of my knowledge "+
		"and sign below. *", y)
	f.signature("Signature *", "Date *", y+10.0)

	// The second page, for the supervisor
	f.page = pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, f.page)
	y = f.heading("For the supervisor", structelem.H2, 14.0, 62.0)
	f.field("Received by", left, y, half, 1, "")
	y = f.field("Date received", left+half+gap, y, half, 1, "")
	y = f.field("What caused it?", left, y, width, 3, "")
	y = f.field("What will be done so that it does not happen again, by whom and by when?",
		left, y, width, 4, "")
	f.label("Reported to", left, y)
	y += 18.0
	for _, to := range []string{"The health and safety committee", "Our insurer",
		"The owner of the building"} {
		y = f.checkBox(to, y)
	}
	y = f.field("Closed on", left, y+10.0, third, 1, "")
	f.signature("Supervisor's signature", "Date", y)

	// The footer of each page, now that their number is known
	for i, p := range pages {
		formLine := pdfjet.NewTextLine(f.textFont, company+", "+formNumber)
		formLine.SetFontSize(9.0)
		formLine.SetTextColor(color.DimGray)
		formLine.SetLocation(left, p.GetHeight()-36.0)
		number := pdfjet.NewTextLine(f.textFont, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		number.SetFontSize(9.0)
		number.SetTextColor(color.DimGray)
		number.SetLocation(left+width-number.GetWidth(), p.GetHeight()-36.0)
		p.AddArtifactBMC()
		formLine.DrawOn(p)
		number.DrawOn(p)
		p.AddEMC()
	}
	pdf.AddPages(pages)
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

// checkBox draws a check box and its text, its top at y, and returns where
// the next one starts.
func (f *form) checkBox(text string, y float32) float32 {
	pdfjet.NewCheckBox(f.textFont, text).
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	return y + lineHeight
}

// signature draws the signature and the date beside it, 60 and 40 percent of
// the row.
func (f *form) signature(text, date string, y float32) {
	f.field(text, left, y, float32(0.6)*width-gap/2.0, 1, "")
	f.field(date, left+float32(0.6)*width+gap/2.0, y, float32(0.4)*width-gap/2.0, 1, "")
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
	Example70()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_70 => %4d ms\n", time1-time0)
}
