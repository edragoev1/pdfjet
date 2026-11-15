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
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const organizer = "Lantern Hill Design School"
const workshop = "Better PDFs: a hands-on workshop, Saturday, January 16, " +
	"2027, 9 a.m. to 4 p.m. Fill in this form and send it back to reserve your place. " +
	"Lunch and coffee are included."
const paymentPage = "https://example.com/workshops/2027-01-16/pay"

// The sessions of the day: the time, the title and the room
var sessions = [][]string{
	{"9 a.m.", "Layouts that print the same everywhere", "A"},
	{"10:30 a.m.", "Forms your clients fill in online", "A"},
	{"1 p.m.", "Barcodes and QR codes", "B"},
	{"2:30 p.m.", "Archival and accessible: PDF/A and PDF/UA", "B"},
}

const blue = 0x1f58b0
const red = 0xb00020
const ruleColor = 0xdde1e8
const box = 0x4d4d4d
const header = 0xe3edf9

// The grid: the margin, the width of the page between the margins, and
// the gap between two fields of a row
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half = (width - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each
// line more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// form holds the page and the fonts of the form.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example84 draws a workshop registration form, to print and fill in by
// hand: who comes, the sessions of the day with a column to tick the ones
// they attend, the lunch and the photographs, and how to pay, with a QR code
// that opens the payment page on a phone.
//
// This is a form people fill in, not a document with data, though it carries
// the program of the day. The fields are drawn as Example_72 draws the job
// application, with the same field helper: the label above its box, the
// boxes on a grid, and an asterisk on the label of a field that is required.
// The sessions are a Table: the organizer prints the time, the title and the
// room, and leaves the last column empty, a cell to tick.
//
// The QR code holds the address of the payment page. It is a figure with a
// description, which a screen reader reads, and the address is in the text
// beside it too, for whoever cannot scan it. It is measured before it is
// placed, with DrawOn of a nil page, so that it lines up with the right
// margin.
//
// The document is PDF/UA-1: the title and the sections are headings, the
// sessions a table read by its column headers, and the boxes are artifacts.
// The layout is that of the workshop sign-up template of PDFjet Forms. The
// organizer, the workshop and the address are made up.
func Example84() {
	pdf, err := pdfjet.NewPDFFile("Example_84.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Workshop registration")
	pdf.SetAuthor(organizer)
	pdf.SetLanguage("en-US")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	f.page = pdfjet.NewPage(pdf, letter.Portrait())
	page := f.page

	// The title, the workshop, and a rule
	f.heading("Workshop registration", structelem.H1, 20.0, 62.0)
	y := f.paragraph(workshop, left, 74.0, width) + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(page)

	// Who comes
	y = f.heading("About you", structelem.H2, 14.0, y+46.0)
	f.field("Full name *", left, y, half, 1, "")
	y = f.field("Email *", left+half+gap, y, half, 1, "")
	f.field("Company", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")

	// The sessions, and an empty column to tick the ones they attend
	y = f.heading("Sessions", structelem.H2, 14.0, y+18.0)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Time", "Session", "Room", "I attend"))
	for _, session := range sessions {
		rows = append(rows, row(f.textFont, session[0], session[1], session[2], ""))
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(5.0, 5.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(box)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(17.0, 58.0, 10.0, 15.0)
	table.SetLocation(left, y)
	y = table.DrawOn(page)[1] + 16.0

	// The lunch and the photographs
	pdfjet.NewCheckBox(f.textFont, "I need a vegetarian lunch").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(page)
	pdfjet.NewCheckBox(f.textFont, "I agree to be photographed during the workshop").
		SetBorderColor(box).
		SetLocation(left, y+20.0).
		DrawOn(page)
	y += 50.0

	// The payment: the text on the left, the QR code of the payment page
	// on the right, its top at the top of the text
	y = f.heading("Payment", structelem.H2, 14.0, y+18.0)
	qr := qrcode.NewQRCode(paymentPage, errorcorrectionlevel.M)
	qr.SetModuleLength(2.0)
	qr.SetAltDescription("QR code of the payment page, " + paymentPage)
	qrSize := qr.DrawOn(nil)[0] // Measured, at 0, 0, so its size
	qr.SetLocation(left+width-qrSize, y)
	qr.DrawOn(page)
	end := f.paragraph("The fee is $120, lunch included. Pay with your phone: scan the code. "+
		"Or open "+paymentPage+" on a computer. Your seat is kept once we have "+
		"your form and your payment.", left, y, width-qrSize-2*gap)
	y = max(end, y+qrSize) + 16.0

	// The signature takes 60% of the width and the date 40%, in float32
	// arithmetic, as the other ports compute it
	var signatureShare float32 = 0.6
	var dateShare float32 = 0.4
	f.field("Signature *", left, y, float32(signatureShare*width)-gap/2.0, 1, "")
	f.field("Date *", left+float32(signatureShare*width)+gap/2.0, y, float32(dateShare*width)-gap/2.0, 1, "")

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// paragraph draws the text wrapped to the width, its top at y, and returns
// where it ended.
func (f *form) paragraph(text string, x, y, width float32) float32 {
	block := pdfjet.NewTextBlock(f.textFont, text)
	block.SetLineSpacing(1.15)
	block.SetLocation(x, y)
	block.SetWidth(width)
	return block.DrawOn(f.page)[1]
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
	Example84()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_84 => %4d ms\n", time1-time0)
}
