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

const club = "Riverbend Community Garden"
const returnTo = "Please bring the form to the garden shed on a Saturday " +
	"morning, or mail it to 85 Mill Road, Elora, ON N0B 1S0."

// The kinds of membership and their fees for a year, in whole dollars, an
// integer
var kinds = []string{"Individual", "Family", "Student"}
var fees = []int64{40, 70, 20}

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

// Example75 draws a membership application of a community garden, to print
// and fill in by hand: the name, the address and how to reach the member, the
// kind of membership with its fee, what they would like to take part in, the
// newsletter and the rules to agree to, and a box to sign in.
//
// This is a form people fill in, not a document with data: the boxes are
// empty. It is drawn as Example_72 draws the job application, with the same
// field helper: the label above its box, the boxes on a grid of columns so
// that the rows line up, 24 points tall for a line of handwriting, and an
// asterisk on the label of a field that is required.
//
// The kinds of membership are radio buttons, one of three, and their fees
// come from the data of the club, so that a change of fee is made in one
// place and the form printed again. The options of a choice are placed one
// after the other, each where the one before it ended, so a longer option
// moves the next one along.
//
// The document is PDF/UA-1: the title and the sections are headings, the
// labels and the options are text, and the boxes are artifacts. The layout
// is that of the membership application template of PDFjet Forms. The
// garden is made up.
func Example75() {
	pdf, err := pdfjet.NewPDFFile("Example_75.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Membership application")
	pdf.SetAuthor(club)
	pdf.SetLanguage("en-CA")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	f.page = pdfjet.NewPage(pdf, letter.Portrait())

	// The title, what to do with the form, and a rule
	f.heading("Membership application", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Join "+club+" for a year. Fill in this "+
		"form and send it back with your first payment.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	// About you
	y = f.heading("About you", structelem.H2, 14.0, y+46.0)
	f.field("First name *", left, y, half, 1, "")
	y = f.field("Last name *", left+half+gap, y, half, 1, "")
	y = f.field("Street address", left, y, width, 1, "")
	// City, province and postal code: 40, 30 and 30 percent of the row
	city := float32(0.4) * (width - 2*gap)
	rest := float32(0.3) * (width - 2*gap)
	f.field("City", left, y, city, 1, "")
	f.field("Province or state", left+city+gap, y, rest, 1, "")
	y = f.field("Postal code", left+city+gap+rest+gap, y, rest, 1, "")
	f.field("Email *", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")

	// The membership: its kind, one of three, with the fee of each
	y = f.heading("Your membership", structelem.H2, 14.0, y+18.0)
	f.label("Membership *", left, y)
	x := left + 96.0
	for i := 0; i < len(kinds); i++ {
		option := kinds[i] + ", $" + strconv.FormatInt(fees[i], 10)
		x = pdfjet.NewRadioButton(f.textFont, option).SetLocation(x, y-1.0).DrawOn(f.page)[0] + 24.0
	}
	y += 30.0
	y = f.field("Start date *", left, y, half, 1, "")
	y = f.field("What would you like to take part in?", left, y, width, 2, "")

	// The newsletter, the rules, and the signature under them
	pdfjet.NewCheckBox(f.textFont, "Send me the monthly newsletter by email").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	pdfjet.NewCheckBox(f.textFont, "I agree to the members' rules and sign below. *").
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
	Example75()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_75 => %4d ms\n", time1-time0)
}
