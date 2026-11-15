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

// entry is a line of the statement: a charge, such as an invoice, or a
// credit, such as a payment or a credit note. One of the two is zero. The
// amounts are in cents, integers.
type entry struct {
	date        time.Time
	description string
	charge      int64
	credit      int64
}

// day returns the date, at midnight UTC.
func day(year int, month time.Month, d int) time.Time {
	return time.Date(year, month, d, 0, 0, 0, 0, time.UTC)
}

// The data of the statement. In an application it comes from the books.
var periodStart = day(2026, time.September, 1)
var date = day(2026, time.October, 31)

const paymentDays = 30
const currency = "CAD"
const balanceForward = 0 // In cents, 0.00

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const customer = "Contoso Books\n400 King Street West\nToronto, ON M5V 1K4"

var entries = []entry{
	{day(2026, time.September, 25), "Invoice 2026-042, annual report", 249561, 0},
	{day(2026, time.October, 5), "Credit note CN-2026-007", 0, 6780},
	{day(2026, time.October, 20), "Payment, thank you", 0, 100000},
	{day(2026, time.October, 28), "Invoice 2026-051, display stand", 45200, 0},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example56 draws a statement of account of a small design studio to a
// customer, on Letter paper: the studio, the date and the period at the top,
// the customer, a table of the invoices, credit notes and payments of the
// period with the balance after each, the balance due, and when to pay it.
//
// A statement is a finished document that the customer receives, not a
// form. The balance of each line is computed from the one before it, the
// charges added and the credits taken off, starting from the balance brought
// forward from the period before; the balance due is the balance of the last
// line. The amounts are integer cents, so that they add up to the cent over
// any number of lines, and the table goes on to as many pages as it needs,
// with its header row on each.
//
// The lines are those of Example_53, the invoice, and Example_67, the credit
// note against it, with a payment and a later invoice. Both sides keep a
// statement with their books, so the document is PDF/A-3a and PDF/UA-1 at
// once. The layout is that of the statement of account template of PDFjet
// Forms. The companies and the numbers are made up.
func Example56() {
	pdf, err := pdfjet.NewPDFFile("Example_56.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Statement of account, " + iso(date))
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin

	// The header: the studio on the left, the title and the dates on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Statement")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "Date: "+iso(date)+"\nPeriod: "+iso(periodStart)+" to "+iso(date)).
		SetTextAlignment(alignment.Right).
		SetWidth(250.0).
		SetLocation(right-250.0, 94.0).
		DrawOn(page)

	// A thin rule under the header, decoration only, so an artifact
	line := pdfjet.NewLine(left, 148.0, right, 148.0)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The customer
	pdfjet.NewTextLine(semiBold, "Account").SetStructureType(structelem.H2).SetFontSize(12.0).
		SetLocation(left, 182.0).DrawOn(page)
	y := pdfjet.NewTextBlock(regular, customer).SetWidth(250.0).SetLocation(left, 192.0).
		DrawOn(page)[1] + 24.0

	// The lines, each with the balance after it. The first line is the
	// balance brought forward, so that the column adds up from the top.
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Date", "Description", "Charges", "Credits", "Balance"))
	var balance int64 = balanceForward
	rows = append(rows, row(regular, iso(periodStart), "Balance forward", "", "", money(balance)))
	for _, e := range entries {
		balance = balance + e.charge - e.credit
		rows = append(rows, row(regular, iso(e.date), e.description,
			blankIfZero(e.charge), blankIfZero(e.credit), money(balance)))
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(right - left)
	table.SetColumnWidthsInPercent(16.0, 36.0, 16.0, 16.0, 16.0)
	for column := 2; column < 5; column++ {
		table.SetTextAlignmentInColumn(column, alignment.Right)
	}
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(y)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The balance due and when to pay it, under the last row, or on a
	// page of their own when they no longer fit there
	y = end[1] + 30.0
	if y+70.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	due := pdfjet.NewTextLine(semiBold, "Balance due: "+money(balance)+" "+currency)
	due.SetFontSize(14.0)
	due.SetLocation(right-due.GetWidth(), y)
	due.DrawOn(page)

	pdfjet.NewTextBlock(regular, "Please pay the balance by "+iso(date.AddDate(0, 0, paymentDays))+
		", by bank transfer. If a payment has crossed with this statement, thank you, "+
		"and please disregard it.").
		SetLineSpacing(1.15).
		SetWidth(right-left).
		SetLocation(left, y+22.0).
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

// iso returns the date as 2026-10-31.
func iso(t time.Time) string {
	return t.Format("2006-01-02")
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// blankIfZero: a line has a charge or a credit, and the other column is left
// empty rather than 0.00, so that the eye finds the amounts.
func blankIfZero(cents int64) string {
	if cents == 0 {
		return ""
	}
	return money(cents)
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 2,495.61, whatever the locale of the computer.
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
	Example56()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_56 => %4d ms\n", time1-time0)
}
