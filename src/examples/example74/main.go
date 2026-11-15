// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The data of the meeting. In an application it comes from the notes taken
// during it.
const team = "Northwind Studio"

var date = time.Date(2026, time.November, 12, 0, 0, 0, 0, time.UTC)

const timeAndPlace = "10:00 to 11:00\nMeeting room 2, and by video call"
const people = "Present: Dana, Sam, Jordan · Absent: Lee · Notes: Sam"

var nextMeeting = time.Date(2026, time.November, 26, 0, 0, 0, 0, time.UTC)

// What was said, by topic of the agenda
var notes = [][]string{
	{"Contoso Books website", "Jordan wrote up the interviews at the shop, and the site " +
		"map and the wireframes for computers and phones are done. Contoso Books " +
		"reviews them with us tomorrow; the design phase starts after that."},
	{"New clients", "Two new clients this month, both from the website. A third has " +
		"asked for a quote for a catalog of 32 pages. Lee was away, so Dana calls " +
		"the two new clients in his place."},
	{"Holidays", "The studio is closed from December 24 to January 2. Work due in that " +
		"week is moved to the week before; Sam tells the clients."},
}

// The actions agreed: what, who, and by when
var actions = [][]string{
	{"Send the notes of the review to the team", "Jordan", "2026-11-16"},
	{"Send the quote for the catalog", "Dana", "2026-11-14"},
	{"Call the two new clients", "Dana", "2026-11-19"},
	{"Tell clients of the holiday closing", "Sam", "2026-12-01"},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const left float32 = 48.0
const width float32 = 516.0
const top float32 = 62.0

// The text ends this far from the bottom of the page, above the footer
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// The date as the minutes write it, Thursday, November 12, 2026
const longDate = "Monday, January 2, 2006"

// minutes is the document, its pages, the page being drawn on and the fonts.
type minutes struct {
	pdf      *pdfjet.PDF
	pages    []*pdfjet.Page
	page     *pdfjet.Page
	regular  *pdfjet.Font
	semiBold *pdfjet.Font
}

// Example74 draws the minutes of a team meeting, sent to the team after it:
// when and where it was, who was there and who was not, what was said on
// each topic of the agenda, the actions agreed, with who does each and by
// when, and the date of the next meeting, with "Page 1 of 1" at the bottom.
//
// This is a document people receive, not a form. It is the record of the
// meeting of Example_73, whose agenda it follows topic by topic. Minutes have
// no fixed length, so each part goes on a new page when it no longer fits on
// the one it is on: a paragraph is measured first, with DrawOn of a nil page,
// which returns where it would end without drawing it. The table of the
// actions goes on to as many pages as it needs, its header row on each. The
// pages are kept in a list and added to the document at the end, when the
// footer can say how many there are.
//
// The document is PDF/UA-1: the title, the sections and the topics are
// headings, and the page numbers are artifacts that a screen reader skips.
// The layout is that of the meeting minutes template of PDFjet Forms. The
// studio and the people are made up.
func Example74() {
	pdf, err := pdfjet.NewPDFFile("Example_74.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Team meeting, " + date.Format("2006-01-02") + ": minutes")
	pdf.SetAuthor(team)
	pdf.SetLanguage("en-CA")

	m := &minutes{pdf: pdf, pages: make([]*pdfjet.Page, 0)}
	m.regular = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	m.regular.SetSize(11.0)
	m.semiBold = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	m.semiBold.SetSize(11.0)

	m.newPage()

	// The title, when and where, and a rule under them
	y := m.heading("Team meeting: minutes", structelem.H1, 18.0, top)
	y = m.paragraph(date.Format(longDate)+" · "+timeAndPlace, y) + 12.0
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	m.page.AddArtifactBMC()
	line.DrawOn(m.page)
	m.page.AddEMC()

	y = m.paragraph(people, y+12.0)

	// What was said: a heading for each topic and the notes under it
	y = m.heading("What was said", structelem.H2, 14.0, y+30.0)
	for _, note := range notes {
		y = m.heading(note[0], structelem.H3, 11.0, y+18.0)
		y = m.paragraph(note[1], y)
	}

	// The actions, a table that goes on to the next pages when it must
	y = m.heading("Actions", structelem.H2, 14.0, y+30.0)
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(m.semiBold, "Action", "Who", "By"))
	for _, action := range actions {
		rows = append(rows, row(m.regular, action...))
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(m.semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(60.0, 20.0, 20.0)
	table.SetLocation(left, top)
	table.SetFirstPageTopMargin(y)
	table.SetBottomMargin(footerRoom)
	y = table.DrawOnPagesFrom(pdf, m.page, &m.pages, letter.Portrait())[1]
	m.page = m.pages[len(m.pages)-1]

	m.paragraph("Next meeting: "+nextMeeting.Format(longDate)+", at 10:00.", y+18.0)

	// The footer, on every page, now that their number is known
	for i, p := range m.pages {
		footer := pdfjet.NewTextLine(m.regular, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(m.pages)))
		footer.SetFontSize(10.0)
		footer.SetLocation(left+width-footer.GetWidth(), m.page.GetHeight()-52.0)
		p.AddArtifactBMC()
		footer.DrawOn(p)
		p.AddEMC()
	}
	pdf.AddPages(m.pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// newPage starts a page, kept in the list until the end.
func (m *minutes) newPage() {
	m.page = pdfjet.NewPageDetached(m.pdf, letter.Portrait())
	m.pages = append(m.pages, m.page)
}

// heading draws a heading in blue, its baseline at y, and returns where the
// text under it starts. A heading with no room for a line under it goes on
// to a new page, so that it is never left alone at the bottom.
func (m *minutes) heading(text string, level structelem.StructElem, size, y float32) float32 {
	if y+40.0 > m.page.GetHeight()-footerRoom {
		m.newPage()
		y = top
	}
	heading := pdfjet.NewTextLine(m.semiBold, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(blue)
	heading.SetLocation(left, y)
	heading.DrawOn(m.page)
	return y + 8.0
}

// paragraph draws the text wrapped to the width of the page, its top at y,
// on a new page when it does not fit, and returns where it ended.
func (m *minutes) paragraph(text string, y float32) float32 {
	block := pdfjet.NewTextBlock(m.regular, text)
	block.SetLineSpacing(1.15)
	block.SetWidth(width)
	block.SetLocation(left, y)
	if block.DrawOn(nil)[1] > m.page.GetHeight()-footerRoom {
		m.newPage()
		block.SetLocation(left, top)
	}
	return block.DrawOn(m.page)[1]
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

func main() {
	time0 := time.Now().UnixMilli()
	Example74()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_74 => %4d ms\n", time1-time0)
}
