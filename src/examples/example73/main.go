// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// topic is a topic of the meeting: what it is, who leads it, and how long it
// takes
type topic struct {
	title   string
	leader  string
	minutes int
}

// The data of the meeting. In an application it comes from the calendar.
const team = "Northwind Studio"

var date = time.Date(2026, time.November, 12, 0, 0, 0, 0, time.UTC)
var start = time.Date(2026, time.November, 12, 10, 0, 0, 0, time.UTC)

const place = "Meeting room 2, or by video call"
const people = "Attending: Dana, Sam, Jordan, Lee · Notes: Sam"

var topics = []topic{
	{"Welcome and the notes of the last meeting", "Dana", 5},
	{"Contoso Books website: the wireframes", "Jordan", 20},
	{"New clients this month", "Lee", 20},
	{"Holiday schedule", "Sam", 10},
	{"Anything else, and the next meeting", "All", 5},
}

// The actions agreed at the last meeting: what, who, and by when
var openActions = [][]string{
	{"Write up the interviews at Contoso Books", "Jordan", "2026-11-09"},
	{"Call the two new clients", "Lee", "2026-11-19"},
	{"Post the holiday schedule", "Sam", "2026-11-13"},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const left float32 = 48.0
const width float32 = 516.0

// clock returns a time of day as the team writes it, 10:05, the hour without
// a leading zero.
func clock(t time.Time) string {
	return fmt.Sprintf("%d:%02d", t.Hour(), t.Minute())
}

// agenda is the page being drawn on and the fonts of the agenda.
type agenda struct {
	page     *pdfjet.Page
	regular  *pdfjet.Font
	semiBold *pdfjet.Font
}

// Example73 draws the agenda of a team meeting, sent to the people attending
// before it: when and where it is, who comes and who takes the notes, the
// topics with the time each one starts and who leads it, and the actions from
// the last meeting, to be checked at the start.
//
// This is a document people receive, not a form: every part has its data.
// The topics are given with the minutes each one takes, and the start time of
// each, and the end of the meeting, are worked out from them, so a topic that
// is added or made longer moves the times after it. The two tables are placed
// one under the other from where DrawOn says the one before ended.
//
// The document is PDF/UA-1: the title and the two sections are headings, and
// the tables are read by a screen reader by their column headers. The layout
// is that of the meeting agenda template of PDFjet Forms, and Example_74 is
// the minutes of the same meeting. The studio and the people are made up.
func Example73() {
	pdf, err := pdfjet.NewPDFFile("Example_73.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Team meeting, " + date.Format("2006-01-02"))
	pdf.SetAuthor(team)
	pdf.SetLanguage("en-CA")

	a := &agenda{}
	a.regular = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	a.regular.SetSize(11.0)
	a.semiBold = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	a.semiBold.SetSize(11.0)

	a.page = pdfjet.NewPage(pdf, letter.Portrait())

	// The topics, each starting when the one before it ends. The meeting
	// ends when the last one does.
	topicRows := make([][]*pdfjet.Cell, 0)
	topicRows = append(topicRows, row(a.semiBold, "Time", "Topic", "Who"))
	t := start
	for _, topic := range topics {
		topicRows = append(topicRows, row(a.regular, clock(t), topic.title, topic.leader))
		t = t.Add(time.Duration(topic.minutes) * time.Minute)
	}
	end := t

	// The title, when and where, and a rule under them
	y := a.heading("Team meeting", structelem.H1, 18.0, 62.0)
	when := date.Format("Monday, January 2, 2006") +
		" · " + clock(start) + " to " + clock(end) + "\n" + place
	y = a.paragraph(when, y) + 12.0
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	a.page.AddArtifactBMC()
	line.DrawOn(a.page)
	a.page.AddEMC()

	// Who comes, and who takes the notes
	y = a.paragraph(people, y+12.0)

	// The agenda
	y = a.heading("Agenda", structelem.H2, 14.0, y+30.0)
	y = a.table(topicRows, y, 15.0, 60.0, 25.0)

	// The actions of the last meeting, checked at its start
	actions := make([][]*pdfjet.Cell, 0)
	actions = append(actions, row(a.semiBold, "Action", "Who", "By"))
	for _, action := range openActions {
		actions = append(actions, row(a.regular, action...))
	}
	y = a.heading("Actions from the last meeting", structelem.H2, 14.0, y+30.0)
	a.table(actions, y, 60.0, 20.0, 20.0)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// heading draws a heading in blue, its baseline at y, and returns where the
// part under it starts.
func (a *agenda) heading(text string, level structelem.StructElem, size, y float32) float32 {
	heading := pdfjet.NewTextLine(a.semiBold, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(blue)
	heading.SetLocation(left, y)
	heading.DrawOn(a.page)
	return y + 10.0
}

// paragraph draws the text wrapped to the width of the page, its top at y,
// and returns where it ended.
func (a *agenda) paragraph(text string, y float32) float32 {
	block := pdfjet.NewTextBlock(a.regular, text)
	block.SetLineSpacing(1.15)
	block.SetLocation(left, y)
	block.SetWidth(width)
	return block.DrawOn(a.page)[1]
}

// table draws a table across the page, its first row the header, the columns
// in percent of the width, and returns where it ended.
func (a *agenda) table(rows [][]*pdfjet.Cell, y float32, percents ...float32) float32 {
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(a.semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(percents...)
	table.SetLocation(left, y)
	return table.DrawOn(a.page)[1]
}

// row makes a row of a table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

func main() {
	time0 := time.Now().UnixMilli()
	Example73()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_73 => %4d ms\n", time1-time0)
}
