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

// work is a line of the work: so many days at the day rate.
type work struct {
	description string
	days        int64
}

func (w work) amount() int64 {
	return dayRate * w.days
}

// The data of the quote. In an application it comes from the database.
const number = "Q-2026-017"

var date = time.Date(2026, time.October, 5, 0, 0, 0, 0, time.UTC)

const validDays = 30
const currency = "CAD"
const dayRate = 80000 // In cents, 800.00
const taxName = "HST"
const taxPercent = 13     // The rate of the tax, 13%, in percent
const depositPercent = 30 // The share of the total paid first, 30%, in percent

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const customer = "Tamarack Hill Community Theatre\n27 Mill Street\nOwen Sound, ON N4K 2A1"
const project = "The program book of the 2027 season: its design, " +
	"a layout of 40 pages, and an accessible PDF/UA edition."

var works = []work{
	{"Discovery and a plan of the content", 1},
	{"Design of the layout and the cover", 3},
	{"Layout of 40 pages, two rounds of changes", 4},
	{"Accessible PDF/UA edition, checked page by page", 1},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d
const gray = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"
const gap float32 = 14.0

// Example54 draws a quote of a small design studio to a prospective
// customer, on Letter paper: the studio and the quote number at the top with
// the date it is valid until, who it is for and the project, a table of the
// work in days, the subtotal, the sales tax, the total and the deposit, the
// terms, and lines for the customer to sign the quote and send it back.
//
// A quote is a finished document that the customer receives, not a form.
// The amounts are integer cents, computed from the lines of work, and the
// date it is valid until and the deposit are computed too, so that a change
// of the day rate changes every number that depends on it. The table goes on
// to as many pages as it needs; what follows it moves to a new page when it
// no longer fits. A signed quote is a contract that both sides keep, so the
// document is PDF/A-3a and PDF/UA-1 at once, as the invoice of Example_53
// is. The layout is that of the quote template of PDFjet Forms. The
// companies and the numbers are made up.
func Example54() {
	pdf, err := pdfjet.NewPDFFile("Example_54.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Quote " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The totals, from the work. The tax is rounded to the cent once, on
	// the subtotal, and the deposit is a share of the total with tax.
	var subtotal int64 = 0
	for _, w := range works {
		subtotal += w.amount()
	}
	tax := (subtotal*taxPercent + 50) / 100 // Rounded half up to the cent
	total := subtotal + tax
	deposit := (total*depositPercent + 50) / 100

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin

	// The header: the studio on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Quote")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "No. "+number+"\nDate: "+date.Format("2006-01-02")+
		"\nValid until: "+date.AddDate(0, 0, validDays).Format("2006-01-02")).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	line(page, left, right, 148.0, rule, 1.0)

	// Who the quote is for, and the project, side by side. The taller of
	// the two decides where the next part starts.
	heading(page, semiBold, "Prepared for", left, 182.0)
	middle := (left + right) / 2.0
	y1 := paragraph(page, regular, customer, left, 192.0, middle-left-gap)
	heading(page, semiBold, "Project", middle, 182.0)
	y := max(y1, paragraph(page, regular, project, middle, 192.0, right-middle))

	y = paragraph(page, regular, "Thank you for asking us. Below is what we propose, and "+
		"what it costs. Prices are in Canadian dollars.", left, y+gap, right-left)

	// The work, in a table that goes on to more pages when it needs them
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Work", "Days", "Rate", "Amount"))
	for _, w := range works {
		rows = append(rows, row(regular, w.description, strconv.FormatInt(w.days, 10),
			money(dayRate), money(w.amount())))
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
	table.SetFirstPageTopMargin(y + gap)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The totals, the terms and the signature lines stay together: about
	// 300 points. When they no longer fit under the table, they go on a
	// page of their own.
	y = end[1] + 22.0
	if y+300.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	rate := strconv.Itoa(taxPercent)
	share := strconv.Itoa(depositPercent)
	drawRight(page, regular, 11.0, "Subtotal: "+money(subtotal), right, y)
	drawRight(page, regular, 11.0, taxName+" "+rate+"%: "+money(tax), right, y+15.0)
	drawRight(page, semiBold, 14.0, "Total: "+money(total)+" "+currency, right, y+43.0)
	drawRight(page, regular, 11.0, "Deposit, "+share+"%: "+money(deposit), right, y+62.0)

	y = heading(page, semiBold, "Terms", left, y+100.0)
	y = paragraph(page, regular, "This quote is valid for "+strconv.Itoa(validDays)+" days. A deposit "+
		"of "+share+"% starts the work, and the rest is due on delivery. Two rounds "+
		"of changes are included; more are "+money(dayRate)+" a day. The work takes "+
		"about four weeks from the deposit.", left, y, right-left)

	y = heading(page, semiBold, "Acceptance", left, y+2*gap+6.0)
	y = paragraph(page, regular, "To accept this quote, sign below and send it back to us, "+
		"with the deposit.", left, y, right-left)

	// A line to sign on and one to date, each with its label under it
	dateX := left + float32(0.6*(right-left))
	y += 48.0
	line(page, left, dateX-gap, y, gray, 0.75)
	line(page, dateX, right, y, gray, 0.75)
	pdfjet.NewTextLine(regular, "Name and signature").SetFontSize(10.0).SetTextColor(gray).
		SetLocation(left, y+14.0).DrawOn(page)
	pdfjet.NewTextLine(regular, "Date").SetFontSize(10.0).SetTextColor(gray).
		SetLocation(dateX, y+14.0).DrawOn(page)

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

// heading draws a heading of a part, its baseline at y, and returns where
// the text under it starts.
func heading(page *pdfjet.Page, font *pdfjet.Font, text string, x, y float32) float32 {
	pdfjet.NewTextLine(font, text).SetStructureType(structelem.H2).SetFontSize(12.0).
		SetLocation(x, y).DrawOn(page)
	return y + 10.0
}

// paragraph draws the text wrapped to the width, its top at y, and returns
// the y where it ended.
func paragraph(page *pdfjet.Page, font *pdfjet.Font, text string, x, y, width float32) float32 {
	return pdfjet.NewTextBlock(font, text).SetLineSpacing(1.15).SetWidth(width).
		SetLocation(x, y).DrawOn(page)[1]
}

// line draws a line across, decoration or a line to sign on that its label
// names, so an artifact that a screen reader skips.
func line(page *pdfjet.Page, x1, x2, y float32, c int32, width float32) {
	line := pdfjet.NewLine(x1, y, x2, y)
	line.SetStrokeColor(c)
	line.SetStrokeWidth(width)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()
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
// thousands, as 2,400.00, whatever the locale of the computer.
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
	Example54()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_54 => %4d ms\n", time1-time0)
}
