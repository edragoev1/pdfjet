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

// item is a line of the receipt. The unit price is in cents, an integer.
type item struct {
	description string
	quantity    int64
	unitPrice   int64
}

func (it item) amount() int64 {
	return it.unitPrice * it.quantity
}

// The data of the receipt. In an application it comes from the database.
const number = "R-2026-118"

var date = time.Date(2026, time.October, 2, 0, 0, 0, 0, time.UTC)

const paidBy = "Credit card ending in 4417"
const invoice = "2026-048"
const currency = "CAD"
const taxName = "HST"
const taxPercent = 13 // The rate of the tax, 13%, in percent

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const customer = "Juniper & Pine Bakery\n9 Goderich Street\nPort Elgin, ON N0H 2C4"

var items = []item{
	{"Logo and business card", 1, 45000},
	{"Business cards, 250", 2, 6000},
	{"Flyers, 500, color, both sides", 1, 18000},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example55 draws a receipt of a small design studio for a payment, on
// Letter paper: the studio and the receipt number at the top, who paid and
// how, a table of what was paid for, the subtotal, the sales tax and the
// amount paid, and a line of thanks.
//
// A receipt is a finished document that the customer receives, not a form.
// It is the invoice of Example_53 turned around: the same item type, the
// same sums in integer cents computed from the items, and the same table
// that goes on to more pages when it needs them, but its total is what was
// paid, and it names the invoice that the payment settles.
//
// The customer keeps a receipt for taxes and warranties for years, so the
// document is PDF/A-3a and PDF/UA-1 at once. The layout is that of the
// receipt template of PDFjet Forms. The companies and the numbers are made
// up.
func Example55() {
	pdf, err := pdfjet.NewPDFFile("Example_55.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Receipt " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The amount paid, from the items, the tax rounded once on the subtotal
	var subtotal int64 = 0
	for _, it := range items {
		subtotal += it.amount()
	}
	tax := (subtotal*taxPercent + 50) / 100 // Rounded half up to the cent
	paid := subtotal + tax

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	middle := (left + right) / 2.0

	// The header: the studio on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Receipt")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "No. "+number+"\nDate: "+date.Format("2006-01-02")).
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

	// Who paid, and how: side by side, the taller of the two decides
	// where the table starts
	heading(page, semiBold, "Received from", left, 182.0)
	y1 := pdfjet.NewTextBlock(regular, customer).SetWidth(middle-left).
		SetLocation(left, 192.0).DrawOn(page)[1]
	heading(page, semiBold, "Paid", middle, 182.0)
	y2 := pdfjet.NewTextBlock(regular, paidBy+" on "+date.Format("2006-01-02")+"\nFor invoice No. "+invoice).
		SetWidth(right-middle).SetLocation(middle, 192.0).DrawOn(page)[1]
	y := max(y1, y2) + 24.0

	// What was paid for: a header row, repeated on every page
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
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(y)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The amounts and the thanks under the last row, or on a page of
	// their own when they no longer fit there
	y = end[1] + 22.0
	if y+90.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	rate := strconv.Itoa(taxPercent)
	drawRight(page, regular, 11.0, "Subtotal: "+money(subtotal), right, y)
	drawRight(page, regular, 11.0, taxName+" "+rate+"%: "+money(tax), right, y+15.0)
	drawRight(page, semiBold, 14.0, "Paid: "+money(paid)+" "+currency, right, y+43.0)

	pdfjet.NewTextBlock(regular, "Thank you for your payment. Keep this receipt for your records.").
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
	Example55()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_55 => %4d ms\n", time1-time0)
}
