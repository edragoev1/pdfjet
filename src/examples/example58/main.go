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

// item is a line of the order: the vendor's code of the item, so that the
// vendor picks the right one, and the price the vendor quoted, in cents.
type item struct {
	description string
	code        string
	quantity    int64
	unitPrice   int64
}

func (it item) amount() int64 {
	return it.unitPrice * it.quantity
}

// The data of the order. In an application it comes from the database.
const number = "2026-017"

var date = time.Date(2026, time.October, 5, 0, 0, 0, 0, time.UTC)

const deliveryDays = 15
const paymentDays = 30
const currency = "CAD"
const taxName = "HST"
const taxPercent = 13 // The rate of the tax, 13%, in percent
const approvedBy = "Dana Whitfield, Design Lead"

const buyer = "Northwind Studio"
const buyerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0"
const vendor = "Harbour Paper Supply\n88 Dock Road\nOwen Sound, ON N4K 3T2"

var items = []item{
	{"Recycled paper, letter, 500 sheets", "PP-2201", 20, 850},
	{"Kraft envelopes, 9 x 12 in", "EN-0912", 200, 35},
	{"Shipping boxes, medium", "BX-0400", 50, 120},
	{"Packing tape, 48 mm x 50 m", "TP-4850", 12, 395},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example58 draws a purchase order of a small design studio to its
// supplier, on Letter paper: the studio and the order number at the top with
// the date to deliver by, the vendor and where to ship, a table of the items
// with the vendor's codes, the subtotal, the sales tax and the total, the
// terms, and who approved the order.
//
// Here the studio is the buyer: the layout of its invoice of Example_53,
// the parties the other way around. A purchase order is a finished document
// that the vendor receives, not a form. The amounts are integer cents,
// computed from the items, and the table goes on to as many pages as it
// needs.
//
// The vendor's invoice is checked against the purchase order, and both are
// kept for the books, so the document is PDF/A-3a and PDF/UA-1 at once. The
// layout is that of the purchase order template of PDFjet Forms. The
// companies, the people and the numbers are made up.
func Example58() {
	pdf, err := pdfjet.NewPDFFile("Example_58.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Purchase order " + number)
	pdf.SetAuthor(buyer)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The totals, from the items, the tax rounded once on the subtotal
	var subtotal int64 = 0
	for _, it := range items {
		subtotal += it.amount()
	}
	tax := (subtotal*taxPercent + 50) / 100 // Rounded half up to the cent
	total := subtotal + tax

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	middle := (left + right) / 2.0

	// The header: the studio on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, buyer).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, buyerAddress+"\nhello@example.com").
		SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Purchase order")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "PO No. "+number+"\nDate: "+date.Format("2006-01-02")+
		"\nDeliver by: "+date.AddDate(0, 0, deliveryDays).Format("2006-01-02")).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	// A thin rule under the header, decoration only, so an artifact
	line := pdfjet.NewLine(left, 148.0, right, 148.0)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The vendor, and where to ship: side by side, the taller of the two
	// decides where the table starts
	heading(page, semiBold, "Vendor", left, 182.0)
	y1 := pdfjet.NewTextBlock(regular, vendor).SetWidth(middle-left).
		SetLocation(left, 192.0).DrawOn(page)[1]
	heading(page, semiBold, "Ship to", middle, 182.0)
	y2 := pdfjet.NewTextBlock(regular, buyer+"\n"+buyerAddress).SetWidth(right-middle).
		SetLocation(middle, 192.0).DrawOn(page)[1]
	y := max(y1, y2) + 24.0

	// The items: a header row, repeated on every page
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Item", "Code", "Quantity", "Price", "Amount"))
	for _, it := range items {
		rows = append(rows, row(regular, it.description, it.code, strconv.FormatInt(it.quantity, 10),
			money(it.unitPrice), money(it.amount())))
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(right - left)
	table.SetColumnWidthsInPercent(45.0, 15.0, 12.0, 13.0, 15.0)
	for column := 2; column < 5; column++ {
		table.SetTextAlignmentInColumn(column, alignment.Right)
	}
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(y)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The totals, the terms and the approval under the last row, or on a
	// page of their own when they no longer fit there
	y = end[1] + 22.0
	if y+130.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	rate := strconv.Itoa(taxPercent)
	drawRight(page, regular, 11.0, "Subtotal: "+money(subtotal), right, y)
	drawRight(page, regular, 11.0, taxName+" "+rate+"%: "+money(tax), right, y+15.0)
	drawRight(page, semiBold, 14.0, "Total: "+money(total)+" "+currency, right, y+43.0)

	y = pdfjet.NewTextBlock(regular, "Please quote the PO number on your invoice and packing slip. "+
		"Payment within "+strconv.Itoa(paymentDays)+" days of delivery.").
		SetLineSpacing(1.15).
		SetWidth(right-left).
		SetLocation(left, y+64.0).
		DrawOn(page)[1]
	pdfjet.NewTextLine(regular, "Approved by: "+approvedBy).
		SetLocation(left, y+28.0).DrawOn(page)

	// The footer, on every page, now that their number is known
	for i := 0; i < len(pages); i++ {
		footer := pdfjet.NewTextLine(regular, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		footer.SetFontSize(10.0)
		footer.SetLocation(right-footer.GetWidth(), page.GetHeight()-52.0)
		pages[i].AddArtifactBMC()
		footer.DrawOn(pages[i])
		pages[i].AddEMC()
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// heading draws the heading of a part, its baseline at y.
func heading(page *pdfjet.Page, font *pdfjet.Font, text string, x, y float32) {
	pdfjet.NewTextLine(font, text).SetStructureType(structelem.H2).SetFontSize(12.0).
		SetLocation(x, y).DrawOn(page)
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// drawRight draws a line of text in the font and the size, its baseline at
// y and its end at x, as the amounts line up on their right.
func drawRight(page *pdfjet.Page, font *pdfjet.Font, size float32, text string, x, y float32) {
	line := pdfjet.NewTextLine(font, text)
	line.SetFontSize(size)
	line.SetLocation(x-line.GetWidth(), y)
	line.DrawOn(page)
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 1,200.00, whatever the locale of the computer.
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
	Example58()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_58 => %4d ms\n", time1-time0)
}
