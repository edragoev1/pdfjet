// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const landlord = "Cedar Row Rentals"
const returnToText = "Please return both pages to Cedar Row Rentals, " +
	"40 Woolwich Street, Guelph, ON N1H 3V1, or scan them to rent@example.com."

// The property the form is printed for
const property = "Unit 4, 118 Elm Street, Guelph, ON N1H 3T2"
const rent int64 = 185000 // in cents

const ink = 0x333333    // the headings
const maroon = 0x800000 // the answers printed on the form
const red = 0xb00020
const ruleColor = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, the
// gap between two fields of a row, and the widths of two and three fields
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half = (width - gap) / 2.0
const third = (width - 2*gap) / 3.0

// A box tall enough for a line of handwriting, and the height of each
// line more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

const pages = 2

// form holds the page being drawn and the fonts of the form.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example81 draws a rental application, two pages to print and fill in by
// hand: the property, the applicant and where they live now, who will live
// in the property, their work and income, two references, and the agreement
// to sign, with "Page 1 of 2" at the bottom of each page.
//
// This is a form people fill in, not a document with data. It is drawn as
// Example_72 draws the job application, with the same field helper: the
// label above its box, the boxes on a grid of two or three columns, and an
// asterisk on the label of a field that is required. The choices, as the
// length of the lease or the pets, are radio buttons under their label.
//
// The landlord prints the form for one property, so its address and its
// rent are filled in already, in the dark red of the answers of the
// template, apart from the black of the form; the rent is in integer cents,
// written with commas between the thousands whatever the locale. The second
// page starts with a section: a form is broken between its sections, never
// inside one.
//
// The document is PDF/UA-1: the title and the sections are headings, the
// labels and the options are text, and the boxes and the page numbers are
// artifacts. The layout is that of the rental application template of
// PDFjet Forms. The landlord and the address are made up.
func Example81() {
	pdf, err := pdfjet.NewPDFFile("Example_81.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Rental application")
	pdf.SetAuthor(landlord)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	// Page 1: the title, the property and the applicant
	f.page = pdfjet.NewPage(pdf, letter.Portrait())
	f.heading("Rental application", structelem.H1, 20.0, 62.0)
	y := f.paragraph("Thank you for your interest in the property. Fill in both pages, "+
		"sign at the end and send the form back to us; we answer within three "+
		"working days.", 74.0) + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	y = f.heading("The property", structelem.H2, 14.0, y+46.0)
	y = f.field("Address of the property *", left, y, width, 1, property)
	f.field("Move-in date *", left, y, half, 1, "")
	y = f.field("Monthly rent", left+half+gap, y, half, 1, "$"+money(rent))
	y = f.choice("Length of the lease", left, y, "12 months", "6 months", "Month to month")

	y = f.heading("About you", structelem.H2, 14.0, y+18.0)
	f.field("First name *", left, y, half, 1, "")
	y = f.field("Last name *", left+half+gap, y, half, 1, "")
	f.field("Email *", left, y, half, 1, "")
	y = f.field("Phone *", left+half+gap, y, half, 1, "")
	y = f.field("Current address *", left, y, width, 2, "")
	y = f.choice("How long have you lived there?", left, y,
		"Less than a year", "1 to 3 years", "More than 3 years")
	f.field("Current landlord's name", left, y, half, 1, "")
	f.field("Current landlord's phone", left+half+gap, y, half, 1, "")
	f.footer(1)

	// Page 2: the household, the income, the references and the agreement
	f.page = pdfjet.NewPage(pdf, letter.Portrait())
	y = f.heading("Who will live there", structelem.H2, 14.0, 62.0)
	f.field("Adults *", left, y, third, 1, "")
	y = f.field("Children", left+third+gap, y, third, 1, "")
	y = f.choice("Pets", left, y, "No pets", "Cat", "Dog", "Other")
	y = f.field("About your pets, if any", left, y, width, 1, "")

	y = f.heading("Work and income", structelem.H2, 14.0, y+18.0)
	f.field("Employer", left, y, half, 1, "")
	y = f.field("Position", left+half+gap, y, half, 1, "")
	f.field("Years there", left, y, third, 1, "")
	f.field("Monthly income", left+third+gap, y, third, 1, "")
	y = f.field("Employer's phone", left+2*(third+gap), y, third, 1, "")

	y = f.heading("References", structelem.H2, 14.0, y+18.0)
	y = f.paragraph("Two people who know you, not related to you: a former landlord, "+
		"an employer or a colleague.", y) + 10.0
	for _, which := range []string{"First", "Second"} {
		f.field(which+" reference", left, y, third, 1, "")
		f.field("Phone", left+third+gap, y, third, 1, "")
		y = f.field("How they know you", left+2*(third+gap), y, third, 1, "")
	}

	y = f.heading("Agreement", structelem.H2, 14.0, y+18.0)
	y = f.paragraph("I confirm that what I have written in this application is true, and "+
		"I allow you to contact my landlord, my employer and my references to "+
		"check it. Sending this application does not commit either of us to a "+
		"lease.", y) + 12.0
	pdfjet.NewCheckBox(f.textFont, "I have read and agree to the above and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	y += 30.0
	// The signature takes 60% of the width and the date 40%, in float32
	// arithmetic, as the other ports compute it
	var signatureShare float32 = 0.6
	var dateShare float32 = 0.4
	f.field("Signature *", left, y, float32(signatureShare*width)-gap/2.0, 1, "")
	f.field("Date *", left+float32(signatureShare*width)+gap/2.0, y, float32(dateShare*width)-gap/2.0, 1, "")

	returnTo := pdfjet.NewTextLine(f.textFont, returnToText)
	returnTo.SetFontSize(9.0)
	returnTo.SetTextColor(color.DimGray)
	returnTo.SetLocation(left, f.page.GetHeight()-52.0)
	returnTo.DrawOn(f.page)
	f.footer(2)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// heading draws a heading in gray, its baseline at y, and returns where the
// field under it starts.
func (f *form) heading(text string, level structelem.StructElem, size, y float32) float32 {
	heading := pdfjet.NewTextLine(f.labelFont, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(ink)
	heading.SetLocation(left, y)
	heading.DrawOn(f.page)
	return y + 16.0
}

// paragraph draws the text wrapped to the width of the page, its top at y,
// and returns where it ended.
func (f *form) paragraph(text string, y float32) float32 {
	block := pdfjet.NewTextBlock(f.textFont, text)
	block.SetLineSpacing(1.15)
	block.SetLocation(left, y)
	block.SetWidth(width)
	return block.DrawOn(f.page)[1]
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
		line.SetTextColor(maroon)
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

// footer draws the page number at the foot of the page, as an artifact: it
// is not part of the form, and a screen reader skips it.
func (f *form) footer(number int) {
	footer := pdfjet.NewTextLine(f.textFont, "Page "+strconv.Itoa(number)+" of "+strconv.Itoa(pages))
	footer.SetFontSize(10.0)
	footer.SetLocation(left+width-footer.GetWidth(), f.page.GetHeight()-36.0)
	f.page.AddArtifactBMC()
	footer.DrawOn(f.page)
	f.page.AddEMC()
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 1,850.00, whatever the locale of the computer.
func money(cents int64) string {
	sign := ""
	if cents < 0 {
		sign = "-"
		cents = -cents
	}
	dollars := strconv.FormatInt(cents/100, 10)
	var sb strings.Builder
	sb.WriteString(sign)
	for i := 0; i < len(dollars); i++ {
		if i > 0 && (len(dollars)-i)%3 == 0 {
			sb.WriteByte(',')
		}
		sb.WriteByte(dollars[i])
	}
	fmt.Fprintf(&sb, ".%02d", cents%100)
	return sb.String()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example81()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_81 => %4d ms\n", time1-time0)
}
