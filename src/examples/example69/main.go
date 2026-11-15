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

// expense is an expense, as on its receipt. The amount is in cents, an
// integer.
type expense struct {
	date        time.Time
	category    string
	description string
	amount      int64
}

func newExpense(date, category, description string, amount int64) expense {
	d, err := time.Parse("2006-01-02", date)
	if err != nil {
		log.Fatal(err)
	}
	return expense{d, category, description, amount}
}

// The data of the report. In an application it comes from the database.
const company = "Northwind Studio"
const employee = "Jordan Ellis"
const purpose = "Discovery interviews at Contoso Books, Toronto"

var from = time.Date(2026, time.November, 2, 0, 0, 0, 0, time.UTC)
var to = time.Date(2026, time.November, 4, 0, 0, 0, 0, time.UTC)

const currency = "CAD"
const payBackBy = "Bank transfer"

var expenses = []expense{
	newExpense("2026-11-02", "Travel", "Bus, Port Elgin to Kitchener, return", 6400),
	newExpense("2026-11-02", "Travel", "Train, Kitchener to Toronto Union, return", 8640),
	newExpense("2026-11-02", "Lodging", "Hotel, two nights, taxes included", 41230),
	newExpense("2026-11-02", "Meals", "Dinner", 3875),
	newExpense("2026-11-03", "Meals", "Lunch with the shop's team, four people", 12620),
	newExpense("2026-11-03", "Supplies", "Sticky notes and markers for the workshop", 2345),
	newExpense("2026-11-03", "Meals", "Dinner", 4110),
	newExpense("2026-11-04", "Travel", "Subway fares, three days", 1980),
	newExpense("2026-11-04", "Meals", "Lunch at the station", 1685),
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d
const ink = 0x333333

// The grid. The width is a variable, not a constant, so that its products
// are rounded to float32 as they are computed.
const left float32 = 48.0

var width float32 = 516.0 // Letter, 612 wide, less the margins
var right = left + width

const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example69 draws an expense report of a business trip, filled in: who paid,
// what for and when, a table of the expenses, the total of each category and
// the total to pay back, how to pay it, and the lines where the employee and
// the manager sign.
//
// This is the report as the manager receives it, a finished document: the
// expenses come from an application where each receipt was entered, and the
// totals are computed from them, not typed in. The amounts are integer
// cents, never float32 or float64, so that they add up to the cent. The
// totals of the categories are kept in a map, with the categories in a slice
// in the order they first appear, so the summary reads in the order of the
// trip.
//
// The table of the expenses goes on to as many pages as it needs, with its
// header row on each, as a month of receipts can take more than one page;
// the totals go under its last row, or on a page of their own when they no
// longer fit, and each page says "Page 1 of 2" at the bottom.
//
// The document is PDF/UA-1: the title is a heading, the expenses a table that
// a screen reader reads by its column headers, and the rules and the lines to
// sign on are artifacts. The layout is that of the expense report template of
// PDFjet Forms. The people and the companies are made up.
func Example69() {
	pdf, err := pdfjet.NewPDFFile("Example_69.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Expense report, " + employee + ", " + from.Format("2006-01-02"))
	pdf.SetAuthor(employee)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The total of each category, and of all, from the expenses
	categories := make([]string, 0)
	sums := make(map[string]int64)
	var total int64 = 0
	for _, expense := range expenses {
		if _, ok := sums[expense.category]; !ok {
			categories = append(categories, expense.category)
		}
		sums[expense.category] += expense.amount
		total += expense.amount
	}

	// The pages are kept in a list and added to the document at the end,
	// when the footer can say how many there are.
	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)

	// The title, the company, and a rule
	title := pdfjet.NewTextLine(semiBold, "Expense report")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(20.0)
	title.SetTextColor(blue)
	title.SetLocation(left, 64.0)
	title.DrawOn(page)
	companyLine := pdfjet.NewTextLine(semiBold, company)
	companyLine.SetLocation(right-companyLine.GetWidth(), 64.0)
	companyLine.DrawOn(page)
	drawRule(page, 80.0)

	// Who, what for, and when: a label and its value on each line
	const longDate = "January 2, 2006"
	y := detail(page, semiBold, regular, "Employee", employee, 106.0)
	y = detail(page, semiBold, regular, "Purpose", purpose, y)
	detail(page, semiBold, regular, "Dates", from.Format(longDate)+" to "+to.Format(longDate), y)

	// The expenses: a header row, repeated on every page the table goes on to
	const shortDate = "Jan 2"
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Date", "Category", "What it was", "Amount"))
	for _, expense := range expenses {
		rows = append(rows, row(regular, expense.date.Format(shortDate), expense.category,
			expense.description, money(expense.amount)))
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(12.0, 16.0, 56.0, 16.0)
	table.SetTextAlignmentInColumn(3, alignment.Right)
	// Under the details on the first page, at the top of the next ones, and
	// clear of the footer on all of them
	table.SetLocation(left, 48.0)
	table.SetFirstPageTopMargin(172.0)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The totals and the signatures, under the last row. When they no
	// longer fit on its page, they go on a page of their own.
	y = end[1] + 24.0
	height := float32(17.0*float32(len(categories))) + 190.0
	if y+height > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	for _, category := range categories {
		drawRight(page, regular, 11.0, category+": "+money(sums[category]), right, y)
		y += 17.0
	}
	drawRight(page, semiBold, 14.0, "To pay back: "+money(total)+" "+currency, right, y+12.0)
	detail(page, semiBold, regular, "Pay back by", payBackBy, y+12.0)

	// What the signatures confirm, and the lines to sign on, the employee's
	// and the manager's
	y += 46.0
	pdfjet.NewTextLine(regular, "I confirm these expenses were for the business and sign below.").
		SetLocation(left, y).
		DrawOn(page)
	y += 52.0
	signWidth := float32(0.6*width) - 24.0
	dateX := left + float32(0.6*width)
	signature(page, regular, "Employee, "+employee, left, signWidth, y)
	signature(page, regular, "Date", dateX, float32(0.4*width), y)
	y += 52.0
	signature(page, regular, "Approved by the manager", left, signWidth, y)
	signature(page, regular, "Date", dateX, float32(0.4*width), y)

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

// detail draws a label and its value beside it, the baseline at y, and
// returns the baseline of the next line.
func detail(page *pdfjet.Page, labelFont, font *pdfjet.Font, label, value string, y float32) float32 {
	pdfjet.NewTextLine(labelFont, label).SetLocation(left, y).DrawOn(page)
	pdfjet.NewTextLine(font, value).SetTextColor(ink).SetLocation(left+90.0, y).DrawOn(page)
	return y + 17.0
}

// drawRight draws a line of text in the font and the size, its baseline at
// y and its end at x, as the amounts line up on their right.
func drawRight(page *pdfjet.Page, font *pdfjet.Font, size float32, text string, x, y float32) {
	line := pdfjet.NewTextLine(font, text)
	line.SetFontSize(size)
	line.SetLocation(x-line.GetWidth(), y)
	line.DrawOn(page)
}

// signature draws a line to sign on at y, of the width, with what it is for
// under it. The line is an artifact; its caption is text.
func signature(page *pdfjet.Page, font *pdfjet.Font, caption string, x, width, y float32) {
	line := pdfjet.NewLine(x, y, x+width, y)
	line.SetStrokeWidth(0.75)
	line.SetStrokeColor(ink)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()
	pdfjet.NewTextLine(font, caption).
		SetFontSize(9.0).
		SetTextColor(color.DimGray).
		SetLocation(x, y+13.0).
		DrawOn(page)
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// drawRule draws a thin line across the page, decoration only, so an
// artifact that a screen reader skips.
func drawRule(page *pdfjet.Page, y float32) {
	line := pdfjet.NewLine(left, y, right, y)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 1,034.85, whatever the locale of the computer.
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
	Example69()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_69 => %4d ms\n", time1-time0)
}
