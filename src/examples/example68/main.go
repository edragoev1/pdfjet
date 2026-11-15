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

const shop = "Harbor Lane Bicycles"
const returnTo = "Leave the card at the counter, or mail it to Harbor Lane " +
	"Bicycles, 72 Harbor Lane, Mystic, CT 06355. Thank you."

// The questions of the rating grid, each rated from 1 to 5
var ratings = []string{
	"Quality of the work or the product",
	"Service: friendly, clear, on time",
	"Value for money",
	"Your visit overall *",
}

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row
const left float32 = 44.0
const width float32 = 524.0
const gap float32 = 12.0
const half float32 = (width - gap) / 2.0

// The rating grid: where its first column of buttons starts, and how far
// apart the columns are
const scaleLeft float32 = left + 250.0
const scaleStep float32 = 54.0

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

// Example68 draws a customer feedback card to print and fill in by hand,
// left on the counter of a bicycle shop: the date of the visit and what it
// was for, a rating of 1 to 5 for the quality, the service, the value and
// the visit overall, whether the customer would recommend the shop, and two
// boxes for comments.
//
// This is a form, not a finished document: the customer fills it in. It is
// drawn as the job application of Example_72 is, a label above each box. The
// ratings are a grid: a row for each question, with five radio buttons
// labeled 1 to 5 in columns of the same width, under a line that says what
// 1 and 5 mean. The columns are placed by a constant, not by the width of
// the labels, so the buttons of all the rows line up, and a thin line
// between the rows, an artifact, keeps the eye on the row.
//
// The document is PDF/UA-1: the title and the sections are headings, the
// questions and the answers are text, and the boxes and the lines are
// artifacts. The layout is that of the customer feedback template of PDFjet
// Forms. The shop is made up.
func Example68() {
	pdf, err := pdfjet.NewPDFFile("Example_68.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Customer feedback")
	pdf.SetAuthor(shop)
	pdf.SetLanguage("en-US")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	f.page = pdfjet.NewPage(pdf, letter.Portrait())
	page := f.page
	textFont := f.textFont

	// The title, a line on what it is for, and a rule
	f.heading("How did we do?", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(textFont, "Two minutes of your time help us do better. "+
		"Every card is read by the owner.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(page)[1] + 12.0
	f.rule(left, y, width)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// Your visit: the date, and what it was for, any of three
	y = f.heading("Your visit", structelem.H2, 14.0, y+46.0)
	const third float32 = (width - 2.0*gap) / 3.0
	f.field("Date of your visit", left, y, third, 1, "")
	x := left + third + gap
	f.label("What did you come for?", x, y)
	for _, reason := range []string{"A purchase", "A repair or a service", "Advice"} {
		x = pdfjet.NewCheckBox(textFont, reason).
			SetBorderColor(box).
			SetLocation(x, y+21.0).
			DrawOn(page)[0] + 12.0
	}
	y += 64.0

	// Your ratings: what 1 and 5 mean, then a row for each question
	y = f.heading("Your ratings", structelem.H2, 14.0, y+18.0)
	pdfjet.NewTextLine(italic, "1 is poor, 5 is excellent.").
		SetLocation(left, y+italic.GetAscent(italic.GetSize())).
		DrawOn(page)
	y += 22.0
	for _, question := range ratings {
		pdfjet.NewTextLine(textFont, question).
			SetLocation(left, y+textFont.GetAscent(textFont.GetSize())).
			DrawOn(page)
		for i := 1; i <= 5; i++ {
			pdfjet.NewRadioButton(textFont, strconv.Itoa(i)).
				SetLocation(scaleLeft+float32(float32(i-1)*scaleStep), y).
				DrawOn(page)
		}
		f.rule(left, y+21.0, width)
		y += 28.0
	}

	// Would you recommend us, in a row
	f.label("Would you recommend us to a friend?", left, y+6.0)
	x = left
	for _, answer := range []string{"Yes, gladly", "Probably", "Not sure", "No"} {
		x = pdfjet.NewRadioButton(textFont, answer).SetLocation(x, y+24.0).DrawOn(page)[0] + 18.0
	}
	y += 56.0

	// In the customer's words
	y = f.field("What did we do well?", left, y, width, 2, "")
	y = f.field("What could we do better?", left, y, width, 2, "")
	pdfjet.NewCheckBox(textFont, "You may quote me, without my name").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(page)
	pdfjet.NewCheckBox(textFont, "Please contact me about my answers").
		SetBorderColor(box).
		SetLocation(left+half+gap, y).
		DrawOn(page)
	y += 30.0
	f.field("Name", left, y, half, 1, "")
	f.field("Email or phone", left+half+gap, y, half, 1, "")

	// Where the card goes, at the foot of the page
	returnToLine := pdfjet.NewTextLine(textFont, returnTo)
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

// rule draws a thin line, decoration only, so an artifact that a screen
// reader skips.
func (f *form) rule(x, y, width float32) {
	line := pdfjet.NewLine(x, y, x+width, y)
	line.SetStrokeColor(rule)
	f.page.AddArtifactBMC()
	line.DrawOn(f.page)
	f.page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example68()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_68 => %4d ms\n", time1-time0)
}
