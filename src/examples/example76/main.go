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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// item is an item of the price list: its name and its price, in cents, an
// integer
type item struct {
	name  string
	price int64
}

const shop = "Tidewater Print Co."
const returnTo = "Mail the form to Tidewater Print Co., 210 Front Street, " +
	"Beaufort, NC 28516, or scan it to orders@example.com."
const tax = "Sales tax 8%"

var items = []item{
	{"T-shirt, cotton, printed on the front", 2000},
	{"Hoodie, printed on the back", 4500},
	{"Cap, embroidered", 1500},
	{"Tote bag, canvas", 1200},
}

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d
const header = 0xe3edf9

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

// Example76 draws an order form of a small shop, to print and fill in by
// hand: who orders, a table of the items with their prices and empty cells
// for the size, the quantity and the amount, lines for the subtotal, the
// sales tax and the total, where to deliver, and a box to sign in.
//
// This is a form people fill in, not a document with data. The shop prints
// what it knows, the items and their prices, from amounts in integer cents
// written the same whatever the locale, and leaves the rest empty. The rows
// of the table are about as tall as a box, to write a number in by hand; the
// subtotal, the tax and the total are rows of the same table whose label
// spans the first four columns, so that their empty amounts line up under
// those of the items. The other fields are drawn with the field helper of
// Example_72.
//
// The document is PDF/UA-1: the title and the sections are headings, the
// items a table that a screen reader reads by its column headers, and the
// boxes are artifacts. The layout is that of the order form template of
// PDFjet Forms. The shop is made up.
func Example76() {
	pdf, err := pdfjet.NewPDFFile("Example_76.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Order form")
	pdf.SetAuthor(shop)
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

	// The title, what happens to the order, and a rule
	f.heading("Order form", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Write how many of each item you want, and "+
		"the amounts. Sign the form and send it to us: we confirm every order by "+
		"email within one working day.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	// Your details
	y = f.heading("Your details", structelem.H2, 14.0, y+46.0)
	f.field("Name *", left, y, half, 1, "")
	y = f.field("Email *", left+half+gap, y, half, 1, "")
	f.field("Company", left, y, half, 1, "")
	y = f.field("Phone", left+half+gap, y, half, 1, "")

	// The order: the items with their prices, and empty cells to write in
	y = f.heading("Your order", structelem.H2, 14.0, y+18.0)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Item", "Price", "Size, color", "Qty", "Amount"))
	for _, it := range items {
		rows = append(rows, row(f.textFont, it.name, money(it.price), "", "", ""))
	}
	rows = append(rows, totalRow(f.textFont, "Subtotal"))
	rows = append(rows, totalRow(f.textFont, tax))
	rows = append(rows, totalRow(semiBold, "Total"))

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	// Rows about as tall as a box, to write in
	table.SetCellPadding(5.0, 6.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(box)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(42.0, 13.0, 20.0, 10.0, 15.0)
	table.SetTextAlignmentInColumn(1, alignment.Right)
	table.SetLocation(left, y)
	y = table.DrawOn(f.page)[1] + 12.0

	// Where to deliver: the address beside the date
	y = f.heading("Delivery", structelem.H2, 14.0, y+18.0)
	wide := float32(0.6)*width - gap/2.0
	f.field("Delivery date", left+wide+gap, y, width-wide-gap, 1, "")
	y = f.field("Delivery address *", left, y, wide, 3, "")

	// The confirmation, and the signature under it
	pdfjet.NewCheckBox(f.textFont, "I confirm this order and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	y += 30.0
	f.field("Signature *", left, y, wide, 1, "")
	f.field("Date", left+wide+gap, y, width-wide-gap, 1, "")

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

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// totalRow makes a row of the totals: the label across the first four
// columns, on the right, and an empty amount. The cells under the span stay
// in the row.
func totalRow(font *pdfjet.Font, label string) []*pdfjet.Cell {
	cells := row(font, label, "", "", "", "")
	cells[0].SetColSpan(4)
	cells[0].SetTextAlignment(alignment.Right)
	return cells
}

// money returns the price in cents with two decimals, as 45.00, whatever
// the locale of the computer, which might write 45,00.
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
	Example76()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_76 => %4d ms\n", time1-time0)
}
