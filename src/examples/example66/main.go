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

const company = "Oakridge Property Management"
const returnTo = "Please return the form in the envelope enclosed, or to the " +
	"office at 310 Queen Street, Fredericton, NB E3B 1B2."

// The tenants: the name, the tenant number, and the details on file. In an
// application they come from the database.
var tenants = [][]string{
	{"Maria Alvarez", "T-1042", "Unit 4, 55 Charlotte Street, Fredericton, NB E3B 1L4\n" +
		"maria.alvarez@example.com, 506-555-0117"},
	{"Samuel Okafor", "T-1057", "Unit 12, 55 Charlotte Street, Fredericton, NB E3B 1L4\n" +
		"s.okafor@example.com, 506-555-0183"},
	{"Hannah Lindqvist", "T-1103", "88 Waterloo Row, Fredericton, NB E3B 1Y9\n" +
		"hannah.l@example.com, 506-555-0129"},
}

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row. They are variables, not constants, so
// that the products of the grid are rounded to float32 as they are computed.
var left float32 = 48.0
var width float32 = 516.0
var gap float32 = 12.0
var half = (width - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each line
// more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// form is the page the form is drawn on, with its fonts.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
	italic    *pdfjet.Font
}

// Example66 draws a contact details update form, mailed by a property
// manager to each of its tenants: one page a tenant, with the name and the
// tenant number printed in, the details on file shown above the boxes, and
// room for the new address, email and phone, the date they apply from, and
// a signature.
//
// This is a form, not a finished document: the tenant fills it in. It is
// drawn as the job application of Example_72 is, a label above each box. The
// forms come from a list of tenants, the way a mail merge does: the same
// code draws a page for each one, and the field helper draws the name and
// the number in their boxes. What is on file is printed in gray, so the
// tenant fills in only what has changed. The address row is split 40, 30 and
// 30 percent, the signature row 60 and 40, with a gap between the fields.
//
// The document is PDF/UA-1: the title and the section of each page are
// headings, the labels and the details are text, and the boxes are
// artifacts. The layout is that of the contact details update template of
// PDFjet Forms. The company and the tenants are made up.
func Example66() {
	pdf, err := pdfjet.NewPDFFile("Example_66.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Contact details update")
	pdf.SetAuthor(company)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	f.italic = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	f.italic.SetSize(10.0)

	for _, tenant := range tenants {
		f.page = pdfjet.NewPage(pdf, letter.Portrait())
		f.drawForm(tenant[0], tenant[1], tenant[2])
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawForm draws the form of one tenant on the page.
func (f *form) drawForm(name, number, onFile string) {
	page := f.page

	// The title, what to fill in, and a rule
	f.heading("Update your contact details", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Tell us what has changed. "+
		"Fill in only the details that are new.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(f.italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// Who the form is for, printed in, and what is on file now
	y += 40.0
	f.field("Full name *", left, y, half, 1, name)
	y = f.field("Tenant number", left+half+gap, y, half, 1, number)
	f.label("On file now", left, y)
	current := pdfjet.NewTextBlock(f.textFont, onFile)
	current.SetLineSpacing(1.15)
	current.SetTextColor(color.DimGray)
	current.SetLocation(left, y+16.0)
	current.SetWidth(width)
	y = current.DrawOn(page)[1] + 12.0

	// The new details. The address row is split 40, 30 and 30 percent.
	y = f.heading("Your new details", structelem.H2, 14.0, y+18.0)
	y = f.field("Street address", left, y, width, 1, "")
	rest := width - float32(2.0*gap)
	f.field("City", left, y, float32(0.4*rest), 1, "")
	f.field("Province or state", left+float32(0.4*rest)+gap, y, float32(0.3*rest), 1, "")
	y = f.field("Postal code", left+float32(0.7*rest)+float32(2.0*gap), y, float32(0.3*rest), 1, "")
	f.field("Email", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")
	f.field("These details apply from *", left, y, half, 1, "")

	// The best way to reach the tenant, three answers in a row beside the date
	f.label("Best way to reach you", left+half+gap, y)
	x := left + half + gap
	for _, answer := range []string{"Email", "Phone", "Mail"} {
		x = pdfjet.NewRadioButton(f.textFont, answer).SetLocation(x, y+21.0).DrawOn(page)[0] + 18.0
	}
	y += 64.0

	// The confirmation, and the signature under it
	pdfjet.NewCheckBox(f.textFont, "I confirm these details and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(page)
	y += 30.0
	f.field("Signature *", left, y, float32(0.6*width)-gap/2.0, 1, "")
	f.field("Date *", left+float32(0.6*width)+gap/2.0, y, float32(0.4*width)-gap/2.0, 1, "")

	// Where the form goes, at the foot of the page
	returnToLine := pdfjet.NewTextLine(f.textFont, returnTo)
	returnToLine.SetFontSize(9.0)
	returnToLine.SetTextColor(color.DimGray)
	returnToLine.SetLocation(left, page.GetHeight()-36.0)
	returnToLine.DrawOn(page)
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
	Example66()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_66 => %4d ms\n", time1-time0)
}
