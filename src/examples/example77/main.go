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

// entry is a line of the customer's account: an invoice is positive, a
// credit note and a payment negative. The amount is in cents, an integer.
type entry struct {
	date        time.Time
	description string
	amount      int64
}

func newEntry(date, description string, amount int64) entry {
	t, err := time.Parse("2006-01-02", date)
	if err != nil {
		log.Fatal(err)
	}
	return entry{t, description, amount}
}

const sender = "Northwind Studio"
const senderAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"

var date = time.Date(2026, time.November, 9, 0, 0, 0, 0, time.UTC)

const recipient = "Ms. Priya Raman\nContoso Books\n400 King Street West\nToronto, ON M5V 1K4"
const invoice = "2026-042"

var due = time.Date(2026, time.October, 25, 0, 0, 0, 0, time.UTC)

const currency = "CAD"
const account = "01234-567-8901234"

var entries = []entry{
	newEntry("2026-09-25", "Invoice 2026-042, the annual report", 249561),
	newEntry("2026-10-05", "Credit note CN-2026-007, ten copies damaged in delivery", -6780),
	newEntry("2026-10-20", "Your payment, thank you", -100000),
}

const signer = "Dana Whitfield\nDesign Lead, Northwind Studio"

const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const left float32 = 48.0
const width float32 = 516.0 // Letter, 612 wide, less the margins
const gap float32 = 14.0    // Between the parts of the letter

// The dates as the letter writes them, November 9, 2026, and November 9
const longDate = "January 2, 2006"
const monthDay = "January 2"

// Example77 draws a payment reminder, a letter to a customer whose invoice is
// past due: the letterhead, the date, the address, the invoice as the
// subject, a friendly paragraph, a small table of what was invoiced, credited
// and paid, the amount still open, how to pay it, and the signature.
//
// A reminder is a finished document that the customer receives, not a form.
// The letter is placed as the business letter of Example_59 is, each part
// under the one before it, from where DrawOn says the last one ended; the
// table of the account is one more part, and the paragraphs after it move
// down when it has more lines. The amount still open is computed from the
// lines of the account, in integer cents, and is the same figure in the
// table and in the text, as both come from one variable.
//
// The document is PDF/UA-1: the subject is the heading of the letter, the
// account a table that a screen reader reads by its column headers, and the
// rule under the letterhead is an artifact. The layout is that of the
// payment reminder template of PDFjet Forms. The people and the companies
// are made up.
func Example77() {
	pdf, err := pdfjet.NewPDFFile("Example_77.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Payment reminder, invoice " + invoice)
	pdf.SetAuthor(sender)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// What is still open, from the lines of the account
	var open int64 = 0
	for _, e := range entries {
		open += e.amount
	}

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The letterhead, and a thin rule under it, decoration only
	pdfjet.NewTextLine(semiBold, sender).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	y := paragraph(page, regular, senderAddress, 80.0) + gap
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The date, the recipient, and the subject, the heading of the letter
	y = paragraph(page, regular, date.Format(longDate), y+2*gap)
	y = paragraph(page, regular, recipient, y+gap)
	subject := pdfjet.NewTextBlock(semiBold, "Invoice "+invoice+", due "+due.Format(longDate))
	subject.SetFontSize(12.0)
	subject.SetStructureType(structelem.H1)
	subject.SetLocation(left, y+gap)
	subject.SetWidth(width)
	y = subject.DrawOn(page)[1]

	y = paragraph(page, regular, "Dear Ms. Raman,", y+gap)
	y = paragraph(page, regular, "This is a friendly reminder that invoice "+invoice+" of "+
		entries[0].date.Format(monthDay)+", for the layout of your annual report, was due on "+
		due.Format(monthDay)+". The amount still open is "+money(open)+" "+currency+
		", after the credit note and the payment below.", y+gap)

	// The account: what was invoiced, credited and paid, and what is open
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Date", "Description", "Amount"))
	for _, e := range entries {
		rows = append(rows, row(regular, e.date.Format("2006-01-02"), e.description, money(e.amount)))
	}
	rows = append(rows, row(semiBold, "", "Amount open, "+currency, money(open)))

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetNumberOfFooterRows(1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetFooterRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(18.0, 62.0, 20.0)
	table.SetTextAlignmentInColumn(2, alignment.Right)
	table.SetLocation(left, y+gap)
	y = table.DrawOn(page)[1]

	y = paragraph(page, regular, "Invoice 2026-051 of October 28, also on our statement of "+
		"October 31, is not due until November 27 and is not part of this reminder.", y+gap)
	y = paragraph(page, regular, "If you have paid it in the last few days, thank you, and please "+
		"disregard this letter. If anything in the invoice is unclear, just reply to this "+
		"letter and I will be glad to help.", y+gap)
	y = paragraph(page, regular, "You can pay by bank transfer to "+sender+", account "+
		account+", with the invoice number in the message.", y+gap)

	// The closing, room to sign by hand, and the name under it
	y = paragraph(page, regular, "Kind regards,", y+gap)
	paragraph(page, regular, signer, y+3*gap)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// paragraph draws the text wrapped to the width of the page, its top at y,
// and returns the y where it ended, for the next part of the letter.
func paragraph(page *pdfjet.Page, font *pdfjet.Font, text string, y float32) float32 {
	block := pdfjet.NewTextBlock(font, text)
	block.SetLineSpacing(1.25)
	block.SetLocation(left, y)
	block.SetWidth(width)
	return block.DrawOn(page)[1]
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 1,427.81, and a minus sign before a credit, whatever the
// locale of the computer, which might write 1 427,81.
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
	Example77()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_77 => %4d ms\n", time1-time0)
}
