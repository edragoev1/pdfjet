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

const studio = "Northwind Studio"
const studioAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"

var date = time.Date(2026, time.October, 19, 0, 0, 0, 0, time.UTC)

const validDays = 30

const project = "A new website for Contoso Books"
const preparedFor = "Priya Raman\nContoso Books"
const preparedBy = "Dana Whitfield\nDesign Lead, " + studio
const goal = "A website that shows the new titles each season, lets readers order " +
	"from any phone, and that the shop's team can keep up to date without help."

// The phases: the name, what the customer gets, and the weeks it takes
var phases = [][]string{
	{"Discovery", "Interviews, a site map, the look in two directions", "2"},
	{"Design", "The pages designed for computers and phones", "3"},
	{"Build", "The site, the catalog and ordering, tested", "4"},
	{"Launch", "Training for your team, a month of support", "1"},
}

// price is a part of the price, in cents, in Canadian dollars before the
// sales tax.
type price struct {
	item  string
	cents int64
}

// The price, in two parts
var prices = []price{
	{"Discovery and design", 480000},
	{"Build and launch", 720000},
}

const currency = "CAD"

const blue = 0x1f58b0
const ruleColor = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const left float32 = 48.0
const width float32 = 516.0 // Letter, 612 wide, less the margins
const right = left + width

// proposal holds the page and the fonts the parts of the proposal are drawn with.
type proposal struct {
	page     *pdfjet.Page
	regular  *pdfjet.Font
	semiBold *pdfjet.Font
}

// Example80 draws a proposal of a design studio to a customer, on Letter
// paper: the studio and the date, the project, who it is for and who wrote
// it, the goal, a table of the phases of the work with their weeks, a table
// of the price, the total, and how to go ahead.
//
// A proposal is a finished document that the customer receives, not a form.
// It is read from top to bottom, so each part is placed under the one before
// it, from where DrawOn says the last one ended: a longer goal or one more
// phase moves everything below it down. Each section is a heading and what
// follows it, drawn by two small helpers. The weeks and the price are
// computed from the tables' rows, the price in integer cents, so the totals
// cannot disagree with the lines above them.
//
// The document is PDF/UA-1: the project is the heading of the document and
// each section a heading under it, so that a screen reader can go from one
// to the next; the plan and the price are tables that it reads by their
// column headers. The layout is that of the proposal template of PDFjet
// Forms. The people and the companies are made up.
func Example80() {
	pdf, err := pdfjet.NewPDFFile("Example_80.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Proposal: " + project)
	pdf.SetAuthor(studio)
	pdf.SetLanguage("en-CA")

	p := &proposal{}
	p.regular = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	p.regular.SetSize(11.0)
	p.semiBold = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	p.semiBold.SetSize(11.0)

	// The weeks and the price, from the rows of the tables. The customer
	// pays half when the work starts, the rest at the launch.
	weeks := 0
	for _, phase := range phases {
		n, err := strconv.Atoi(phase[2])
		if err != nil {
			log.Fatal(err)
		}
		weeks += n
	}
	var total int64 = 0
	for _, price := range prices {
		total += price.cents
	}
	half := total / 2

	p.page = pdfjet.NewPage(pdf, letter.Portrait())
	page := p.page

	// The header: the studio on the left, what the document is and its
	// dates on the right
	pdfjet.NewTextLine(p.semiBold, studio).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(p.regular, studioAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)
	kind := pdfjet.NewTextLine(p.semiBold, "Proposal")
	kind.SetFontSize(26.0)
	kind.SetTextColor(blue)
	kind.SetLocation(right-kind.GetWidth(), 72.0)
	kind.DrawOn(page)
	pdfjet.NewTextBlock(p.regular, "Date: "+date.Format("2006-01-02")+
		"\nValid until: "+date.AddDate(0, 0, validDays).Format("2006-01-02")).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	// A thin rule under the header, decoration only, so an artifact
	rule := pdfjet.NewLine(left, 148.0, right, 148.0)
	rule.SetStrokeColor(ruleColor)
	page.AddArtifactBMC()
	rule.DrawOn(page)
	page.AddEMC()

	// The project, the heading of the document, and the two sides of it
	y := p.heading(project, structelem.H1, 18.0, color.Black, 186.0)
	pdfjet.NewTextLine(p.semiBold, "Prepared for").SetLocation(left, y+18.0).DrawOn(page)
	pdfjet.NewTextLine(p.semiBold, "Prepared by").SetLocation(left+width/2.0, y+18.0).DrawOn(page)
	pdfjet.NewTextBlock(p.regular, preparedFor).SetWidth(width/2.0).SetLocation(left, y+26.0).DrawOn(page)
	y = pdfjet.NewTextBlock(p.regular, preparedBy).
		SetWidth(width/2.0).
		SetLocation(left+width/2.0, y+26.0).
		DrawOn(page)[1]

	// The goal
	y = p.heading("The goal", structelem.H2, 14.0, blue, y+30.0)
	y = p.paragraph(goal, y)

	// The plan: the phases, and the weeks in all under them
	y = p.heading("The plan", structelem.H2, 14.0, blue, y+26.0)
	plan := make([][]*pdfjet.Cell, 0)
	plan = append(plan, row(p.semiBold, "Phase", "What you get", "Weeks"))
	for _, phase := range phases {
		plan = append(plan, row(p.regular, phase...))
	}
	plan = append(plan, row(p.semiBold, "In all", "", strconv.Itoa(weeks)))
	y = p.table(plan, y, 30.0, 55.0, 15.0)

	// The price: the parts, and the total under them
	y = p.heading("The price", structelem.H2, 14.0, blue, y+26.0)
	priceRows := make([][]*pdfjet.Cell, 0)
	priceRows = append(priceRows, row(p.semiBold, "Item", "Price"))
	for _, price := range prices {
		priceRows = append(priceRows, row(p.regular, price.item, money(price.cents)))
	}
	priceRows = append(priceRows, row(p.semiBold, "Total, "+currency+", plus HST", money(total)))
	y = p.table(priceRows, y, 70.0, 30.0)

	// How to go ahead
	p.paragraph("Half, "+money(half)+" "+currency+", is invoiced when the work starts, "+
		"and half at the launch. To go ahead, reply to this proposal, and we will set a "+
		"date for the first interview.", y+18.0)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// heading draws a heading in the size and the color, its top at y, and
// returns where the text under it starts.
func (p *proposal) heading(text string, level structelem.StructElem, size float32, c int32, y float32) float32 {
	heading := pdfjet.NewTextLine(p.semiBold, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(c)
	heading.SetLocation(left, y+heading.GetAscent())
	heading.DrawOn(p.page)
	return y + heading.GetAscent() + 8.0
}

// paragraph draws a paragraph across the page, its top at y, and returns
// where it ended.
func (p *proposal) paragraph(text string, y float32) float32 {
	block := pdfjet.NewTextBlock(p.regular, text)
	block.SetLineSpacing(1.15)
	block.SetLocation(left, y)
	block.SetWidth(width)
	return block.DrawOn(p.page)[1]
}

// table draws a table across the page, its top at y: a header row, the rows,
// and a footer row of totals in the header's style, the last column aligned
// on the right. Returns where it ended.
func (p *proposal) table(rows [][]*pdfjet.Cell, y float32, percents ...float32) float32 {
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetNumberOfFooterRows(1)
	table.SetHeaderRowStyle(p.semiBold, color.Black, header)
	table.SetFooterRowStyle(p.semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(percents...)
	table.SetTextAlignmentInColumn(len(percents)-1, alignment.Right)
	table.SetLocation(left, y)
	return table.DrawOn(p.page)[1]
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
// thousands, as 12,000.00, whatever the locale of the computer.
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
	Example80()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_80 => %4d ms\n", time1-time0)
}
