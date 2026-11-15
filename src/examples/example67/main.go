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

// item is a line of the credit note. The unit price is in cents, an integer.
type item struct {
	description string
	quantity    int64
	unitPrice   int64
}

func (it item) amount() int64 {
	return it.unitPrice * it.quantity
}

// The data of the credit note. In an application it comes from the database.
const number = "CN-2026-007"

var date = time.Date(2026, time.October, 5, 0, 0, 0, 0, time.UTC)

const invoice = "2026-042"

var invoiceDate = time.Date(2026, time.September, 25, 0, 0, 0, 0, time.UTC)

const reason = "Ten printed copies were damaged in delivery and returned."
const currency = "CAD"
const taxName = "HST"
const taxPercent = 13 // The rate of the tax, 13%, in percent

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const customer = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4"

var items = []item{
	{"Printed copies returned, damaged in delivery", 10, 600},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example67 draws a credit note of a small design studio to a customer, on
// Letter paper: the studio and the credit note number at the top, the
// customer, the invoice that the credit note corrects and why, a table of
// what is credited, the subtotal, the sales tax and the total credit, and
// what happens to it.
//
// A credit note is a finished document that the customer receives, not a
// form. It takes back part of an invoice, here part of the invoice of
// Example_53, and refers to it by its number and its date, as the tax rules
// ask, rather than changing an invoice that was already sent. The amounts are
// positive: the title and the words "Total credit" say which way they go. The
// sales tax is credited too, at the rate of the invoice, and computed from the
// items the same way.
//
// Both sides keep a credit note with the invoice, so the document is PDF/A-3a
// and PDF/UA-1 at once. The layout is that of the credit note template of
// PDFjet Forms. The companies and the numbers are made up.
func Example67() {
	pdf, err := pdfjet.NewPDFFile("Example_67.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Credit note " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The credit, from the items, the tax rounded once on the subtotal
	var subtotal int64 = 0
	for _, it := range items {
		subtotal += it.amount()
	}
	tax := (subtotal*taxPercent + 50) / 100 // Rounded half up to the cent
	credit := subtotal + tax

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	middle := (left + right) / 2.0

	// The header: the studio on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Credit note")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "No. "+number+"\nDate: "+date.Format("2006-01-02")+"\nFor invoice "+invoice).
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

	// The customer, and the invoice corrected and why: side by side, the
	// taller of the two decides where the table starts
	heading(page, semiBold, "Credit to", left, 182.0)
	y1 := pdfjet.NewTextBlock(regular, customer).SetWidth(middle-left).SetLocation(left, 192.0).
		DrawOn(page)[1]
	heading(page, semiBold, "Invoice "+invoice+" of "+invoiceDate.Format("2006-01-02"), middle, 182.0)
	y2 := pdfjet.NewTextBlock(regular, reason).
		SetWidth(right-middle).SetLocation(middle, 192.0).DrawOn(page)[1]
	y := max(y1, y2) + 24.0

	// What is credited: a header row, repeated on every page
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

	// The amounts and what happens to them under the last row, or on a
	// page of their own when they no longer fit there
	y = end[1] + 22.0
	if y+90.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	rate := strconv.Itoa(taxPercent)
	drawRight(page, regular, 11.0, "Subtotal: "+money(subtotal), right, y)
	drawRight(page, regular, 11.0, taxName+" "+rate+"%: "+money(tax), right, y+15.0)
	drawRight(page, semiBold, 14.0, "Total credit: "+money(credit)+" "+currency,
		right, y+43.0)

	pdfjet.NewTextBlock(regular, "This amount is taken off your next invoice, or refunded to you "+
		"on request. We are sorry for the damaged copies.").
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
	Example67()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_67 => %4d ms\n", time1-time0)
}
