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

// The data of the timesheet. In an application it comes from the time
// tracking: the hours and the work of each day, from Monday on.
const company = "Northwind Studio"
const employee = "Jordan Ellis"
const project = "Contoso Books, the new website"

var weekStart = time.Date(2026, time.November, 9, 0, 0, 0, 0, time.UTC) // A Monday

const regularHours = 4000 // In hundredths of an hour, 40.00

var days = [][]string{
	{"8", "The notes of the interviews at the shop written up for the team"},
	{"8.5", "The site map, first draft, from what the shop's team said: how the orders " +
		"come in today, which titles sell online, and what the catalog needs"},
	{"7.5", "The site map revised; wireframes of the home and title pages"},
	{"9", "Wireframes for phones; the order pages"},
	{"8", "Review with the client by video call, notes for the design phase"},
	{"2.5", "The list of photos of the new titles, for the shoot"},
	{"", ""},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d
const ink = 0x333333

var left float32 = 48.0
var width float32 = 516.0 // Letter, 612 wide, less the margins

// Example61 draws a timesheet of one week, filled in: who worked, the week
// and the project, a table of the seven days with the hours and the work
// done on each, the total and the overtime, and the lines where the employee
// and the manager sign.
//
// This is the timesheet as the manager receives it, a finished document: the
// hours come from a time tracking application, and the total and the
// overtime are computed from them, not typed in. The hours are integers, in
// hundredths of an hour: a tenth of an hour, six minutes, is not exact in a
// float64, and a sum of them can come out as 7.499999. The dates of the days
// are worked out from the first day of the week.
//
// The days are a Table: a header row, a row for each day, and a footer row
// with the total, in the header's style. Table wraps a long description of
// the work in its column and makes the row as tall as it needs; a day of no
// work keeps its row, empty, so that the week reads from Monday to Sunday.
//
// The document is PDF/UA-1: the title is a heading, the days a table that a
// screen reader reads by its column headers, and the rules and the lines to
// sign on are artifacts. The layout is that of the timesheet template of
// PDFjet Forms. The people and the companies are made up.
func Example61() {
	pdf, err := pdfjet.NewPDFFile("Example_61.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Timesheet, " + employee + ", week of " + weekStart.Format("2006-01-02"))
	pdf.SetAuthor(employee)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The total and the overtime, from the hours of the days
	var total int64 = 0
	for _, day := range days {
		if day[0] != "" {
			total += parseHours(day[0])
		}
	}
	overtime := max(total-regularHours, 0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	// The title, the company, and a rule
	title := pdfjet.NewTextLine(semiBold, "Timesheet")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(20.0)
	title.SetTextColor(blue)
	title.SetLocation(left, 64.0)
	title.DrawOn(page)
	companyLine := pdfjet.NewTextLine(semiBold, company)
	companyLine.SetLocation(left+width-companyLine.GetWidth(), 64.0)
	companyLine.DrawOn(page)
	drawRule(page, 80.0)

	// Who, which week, and for what: a label and its value on each line.
	// Go writes the names of the months and the days in English, whatever
	// the locale of the computer.
	const longDate = "January 2, 2006"
	y := detail(page, semiBold, regular, "Employee", employee, 106.0)
	y = detail(page, semiBold, regular, "Week", weekStart.Format(longDate)+
		" to "+weekStart.AddDate(0, 0, 6).Format(longDate), y)
	y = detail(page, semiBold, regular, "Project", project, y)

	// The days: the name, the date and the hours of each, the work done,
	// and the total under them
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Day", "Date", "Hours", "Work done"))
	for i := 0; i < len(days); i++ {
		date := weekStart.AddDate(0, 0, i)
		rows = append(rows, row(regular, date.Format("Monday"), date.Format("Jan 2"),
			hoursOf(days[i][0]), days[i][1]))
	}
	rows = append(rows, row(semiBold, "Total", "", hours(total), ""))

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetNumberOfFooterRows(1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetFooterRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(16.0, 12.0, 10.0, 62.0)
	table.SetTextAlignmentInColumn(2, alignment.Right)
	table.SetLocation(left, y+18.0)
	y = table.DrawOn(page)[1]

	// The regular hours, and the overtime, the hours over them
	y = detail(page, semiBold, regular, "Regular", hours(min(total, regularHours))+" hours", y+28.0)
	y = detail(page, semiBold, regular, "Overtime", hours(overtime)+" hours", y)

	// What the signatures confirm, and the lines to sign on, the
	// employee's and the manager's
	pdfjet.NewTextLine(regular, "I confirm these hours and sign below.").
		SetLocation(left, y+28.0).
		DrawOn(page)
	y += 90.0
	signWidth := float32(0.6*width) - 24.0
	dateX := left + float32(0.6*width)
	signature(page, regular, "Employee, "+employee, left, signWidth, y)
	signature(page, regular, "Date", dateX, float32(0.4*width), y)
	y += 60.0
	signature(page, regular, "Approved by the manager", left, signWidth, y)
	signature(page, regular, "Date", dateX, float32(0.4*width), y)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// detail draws a label and its value beside it, the baseline at y, and
// returns the baseline of the next line.
func detail(page *pdfjet.Page, labelFont, font *pdfjet.Font, label, value string, y float32) float32 {
	pdfjet.NewTextLine(labelFont, label).SetLocation(left, y).DrawOn(page)
	pdfjet.NewTextLine(font, value).SetTextColor(ink).SetLocation(left+80.0, y).DrawOn(page)
	return y + 17.0
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

// parseHours returns the hours, as 8.5, in hundredths of an hour, as 850.
func parseHours(text string) int64 {
	whole, fraction, _ := strings.Cut(text, ".")
	fraction = (fraction + "00")[:2]
	h, err := strconv.ParseInt(whole+fraction, 10, 64)
	if err != nil {
		log.Fatal(err)
	}
	return h
}

// hoursOf returns the hours with two decimals, as 7.50, so that they line up
// in their column, or nothing for a day of no work.
func hoursOf(text string) string {
	if text == "" {
		return ""
	}
	return hours(parseHours(text))
}

// hours returns the hundredths of an hour with two decimals, as 7.50.
func hours(hundredths int64) string {
	return fmt.Sprintf("%d.%02d", hundredths/100, hundredths%100)
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
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example61()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_61 => %4d ms\n", time1-time0)
}
