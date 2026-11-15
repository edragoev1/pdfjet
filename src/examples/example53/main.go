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

// item is a line of the invoice. The unit price is in cents, an integer.
type item struct {
	description string
	quantity    int64
	unitPrice   int64
}

func (it item) amount() int64 {
	return it.unitPrice * it.quantity
}

// The data of the invoice. In an application it comes from the database.
const number = "2026-042"

var date = time.Date(2026, time.September, 25, 0, 0, 0, 0, time.UTC)

const paymentDays = 30
const currency = "CAD"
const taxName = "HST" // Ontario's sales tax
const taxPercent = 13 // The rate of the tax, 13%, in percent

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const customer = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4"

var items = []item{
	{"Layout of the annual report, 48 pages", 1, 120000},
	{"Accessible PDF/UA edition", 1, 40000},
	{"Charts redrawn from the spreadsheets", 6, 4500},
	{"Printed copies, saddle stitched", 50, 600},
	{"Courier to Toronto", 1, 3850},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example53 draws an invoice of a small design studio to a customer, on
// Letter paper: the seller and the invoice number at the top, the customer, a
// table of the items, the subtotal, the sales tax and the total, the payment
// terms, and "Page 1 of 1" at the bottom.
//
// The amounts are integer cents, never float32 or float64, so that they add
// up to the cent, and the totals are computed from the items rather than
// typed in. The table of the items goes on to as many pages as it needs, with
// its header row on each, so the same code prints an invoice of three items
// or of three hundred; the totals go under the last row.
//
// An invoice is kept for years by both sides, so the document is archival as
// well as accessible, PDF/A-3a and PDF/UA-1 at once: the title is a heading,
// the items a table that a screen reader reads by its column headers. The
// layout is that of the invoice template of PDFjet Forms. The companies and
// the numbers are made up.
func Example53() {
	pdf, err := pdfjet.NewPDFFile("Example_53.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Invoice " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The totals, from the items. The tax is rounded to the cent once, on
	// the subtotal, as a cash register does.
	var subtotal int64 = 0
	for _, it := range items {
		subtotal += it.amount()
	}
	tax := (subtotal*taxPercent + 50) / 100 // Rounded half up to the cent
	total := subtotal + tax

	// The pages are kept in a list and added to the document at the end,
	// when the footer can say how many there are.
	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin

	// The header: the seller on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Invoice")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	numbers := "No. " + number +
		"\nDate: " + date.Format("2006-01-02") +
		"\nDue: " + date.AddDate(0, 0, paymentDays).Format("2006-01-02")
	pdfjet.NewTextBlock(regular, numbers).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	// A thin rule under the header, decoration only, so an artifact that a
	// screen reader skips
	line := pdfjet.NewLine(left, 148.0, right, 148.0)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The customer
	billTo := pdfjet.NewTextLine(semiBold, "Bill to")
	billTo.SetStructureType(structelem.H2)
	billTo.SetFontSize(12.0)
	billTo.SetLocation(left, 182.0)
	billTo.DrawOn(page)
	pdfjet.NewTextBlock(regular, customer).SetWidth(250.0).SetLocation(left, 192.0).DrawOn(page)

	// The items: a header row, repeated on every page the table goes on to
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Description", "Quantity", "Price", "Amount"))
	for _, it := range items {
		rows = append(rows, row(regular, it.description, strconv.FormatInt(it.quantity, 10),
			money(it.unitPrice), money(it.amount())))
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(right - left)
	table.SetColumnWidthsInPercent(55.0, 15.0, 15.0, 15.0)
	for column := 1; column < 4; column++ {
		table.SetTextAlignmentInColumn(column, alignment.Right)
	}
	// Under the customer on the first page, at the top of the next ones,
	// and clear of the footer on all of them
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(260.0)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The totals, under the last row. When they no longer fit on its page,
	// they go on a page of their own.
	y := end[1] + 22.0
	if y+90.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	rate := strconv.Itoa(taxPercent)
	drawRight(page, regular, 11.0, "Subtotal: "+money(subtotal), right, y)
	drawRight(page, regular, 11.0, taxName+" "+rate+"%: "+money(tax), right, y+15.0)
	drawRight(page, semiBold, 14.0, "Total: "+money(total)+" "+currency, right, y+43.0)

	pdfjet.NewTextBlock(regular, "Payment by bank transfer within "+strconv.Itoa(paymentDays)+
		" days. Thank you for your business.").
		SetWidth(right-left).
		SetLocation(left, y+64.0).
		DrawOn(page)

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
	Example53()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_53 => %4d ms\n", time1-time0)
}
