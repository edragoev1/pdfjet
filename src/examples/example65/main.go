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

const organizer = "Lakeside Arts Collective"
const address = "Lakeside Arts Collective, 18 Shore Road, Collingwood, " +
	"ON L9Y 1A1. Please hand the form in at the door."
const event = "Winter concert, Saturday, December 12, 2026"
const changeOfMind = "You can change your mind at any time by writing to " +
	"photos@example.com. We then stop using the photos and videos you appear in, and " +
	"take them off our website and social media within two weeks; copies already " +
	"printed stay as they are."

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row. This form's rows are split two thirds to
// one third.
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const twoThirds float32 = (width - gap) * 2.0 / 3.0
const oneThird float32 = (width - gap) / 3.0

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

// Example65 draws a photo and video consent form to print and sign, handed
// out at the door of an event: how the organizer may use the photos a guest
// appears in, whether the guest may be named, a line for a parent who signs
// for a child, and the name, the date and the signature.
//
// This is a form, not a finished document: the guest fills it in. It is
// drawn as the job application of Example_72 is, a label above each box. The
// organizer prints it for one event, so the event is filled in already: the
// field helper draws a value in its box when it is given one. The answers to
// "How may we use them?" are long, so they are radio buttons one under the
// other, and what changing one's mind means is a paragraph, a TextBlock that
// wraps to the width and returns where it ended. The name and the date share
// a row two thirds to one third, and the child's name is as wide as the
// name, so the boxes line up.
//
// The document is PDF/UA-1: the title and the two sections are headings,
// the labels, the answers and the paragraph are text, and the boxes are
// artifacts. The layout is that of the consent form template of PDFjet
// Forms. The organization and the event are made up.
func Example65() {
	pdf, err := pdfjet.NewPDFFile("Example_65.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Photo and video consent")
	pdf.SetAuthor(organizer)
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

	// The title, what the form is about, and a rule
	f.heading("Photo and video consent", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "During the event we take photos and short "+
		"videos. Tell us how we may use those you appear in.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// The consent: the event, printed in, and the use of the photos
	y = f.heading("Your consent", structelem.H2, 14.0, y+46.0)
	y = f.field("Event", left, y, twoThirds, 1, event)
	y = f.choice("How may we use them? *", left, y,
		"Anywhere: our website, social media and print",
		"Only inside our organization",
		"Not at all")
	y = f.checkBox("You may name me beside a photo of me", y)
	y = f.checkBox("I sign for a child in my care", y)
	y = f.field("Child's name, if you sign for one", left, y+10.0, twoThirds, 1, "")

	change := pdfjet.NewTextBlock(f.textFont, changeOfMind)
	change.SetLineSpacing(1.15)
	change.SetLocation(left, y)
	change.SetWidth(width)
	y = change.DrawOn(page)[1] + 12.0

	// The signature, under the confirmation it signs
	y = f.heading("Your signature", structelem.H2, 14.0, y+18.0)
	f.field("Full name *", left, y, twoThirds, 1, "")
	y = f.field("Date *", left+twoThirds+gap, y, oneThird, 1, "")
	y = f.checkBox("I agree to the above and sign below. *", y)
	f.field("Signature *", left, y+10.0, twoThirds, 1, "")

	// Who the organizer is, at the foot of the page
	organizerLine := pdfjet.NewTextLine(f.textFont, address)
	organizerLine.SetFontSize(9.0)
	organizerLine.SetTextColor(color.DimGray)
	organizerLine.SetLocation(left, page.GetHeight()-36.0)
	organizerLine.DrawOn(page)

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
// button for each answer under it, one a line. Returns where the next row
// starts.
func (f *form) choice(text string, x, y float32, answers ...string) float32 {
	f.label(text, x, y)
	top := y + 18.0
	for _, answer := range answers {
		pdfjet.NewRadioButton(f.textFont, answer).SetLocation(x, top).DrawOn(f.page)
		top += lineHeight
	}
	return top + 10.0
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
	Example65()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_65 => %4d ms\n", time1-time0)
}
