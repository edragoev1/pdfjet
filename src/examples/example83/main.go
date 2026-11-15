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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const organization = "Harbourview Food Bank"
const event = "Holiday food drive, December 5 to 19, 2026"
const intro = "Thank you for offering your time. Write your name and how " +
	"to reach you, and tick the times you are free: the mornings, afternoons and " +
	"evenings are those of weekdays. Under \"Help with\", write what you would like " +
	"to do: sorting, the front desk, driving, or anything. Our coordinator calls you " +
	"within a week."

// The rows of the sheet, as many as fit on the page. More go on to the
// next page.
const rowCount = 13

const blue = 0x1f58b0
const ruleColor = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const left float32 = 48.0
const top float32 = 48.0
const width float32 = 696.0     // 792 less two margins of 48
const footerRoom float32 = 56.0 // the bottom margin, clear of "Page i of N"

// Example83 draws a volunteer sign-up sheet, to print and put on a
// clipboard at the front desk: the event at the top, and a table of numbered
// empty rows in which each volunteer writes their name and how to reach
// them, ticks the times they are free, and says what they would like to help
// with.
//
// This is a form people fill in, not a document with data. The template of
// PDFjet Forms is a form for one volunteer, filled in on screen; on paper
// the same questions go on one sheet for many people, a row for each, which
// is how a sign-up is done at a table or a door. So the form is a Table
// whose rows are empty: they are padded to about the height of a box, room
// enough for handwriting, and the columns for the times are narrow, for a
// tick. The page is Letter landscape, for the width of the columns.
//
// The table is drawn with DrawOnPagesFrom(pdf, page, &pages,
// letter.Landscape()), so a sheet of more rows than a page holds goes on to
// the next pages, with its header row on each, and every page says "Page i
// of N". The pages are kept in a list and added to the document at the end,
// when their number is known.
//
// The document is PDF/UA-1: the title is a heading, and the sheet a table
// that a screen reader reads by its column headers. The layout follows the
// volunteer sign-up template of PDFjet Forms. The food bank is made up.
func Example83() {
	pdf, err := pdfjet.NewPDFFile("Example_83.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Volunteer sign-up")
	pdf.SetAuthor(organization)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(10.0)

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Landscape())
	pages = append(pages, page)

	// The title and the event on one line, what to write, and a rule
	title := pdfjet.NewTextLine(semiBold, "Volunteer sign-up")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(20.0)
	title.SetTextColor(blue)
	title.SetLocation(left, 62.0)
	title.DrawOn(page)
	eventLine := pdfjet.NewTextLine(regular, organization+" · "+event)
	eventLine.SetLocation(left+width-eventLine.GetWidth(), 62.0)
	eventLine.DrawOn(page)

	introBlock := pdfjet.NewTextBlock(regular, intro)
	introBlock.SetLineSpacing(1.15)
	introBlock.SetLocation(left, 74.0)
	introBlock.SetWidth(width)
	y := introBlock.DrawOn(page)[1] + 12.0
	rule := pdfjet.NewLine(left, y, left+width, y)
	rule.SetStrokeColor(ruleColor)
	page.AddArtifactBMC()
	rule.DrawOn(page)
	page.AddEMC()

	// The sheet: a header row, and numbered empty rows to write in
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "No.", "Name", "Phone or email",
		"Mornings", "Afternoons", "Evenings", "Weekends", "Help with"))
	for i := 1; i <= rowCount; i++ {
		rows = append(rows, row(regular, strconv.Itoa(i), "", "", "", "", "", "", ""))
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	// The rows about as tall as a box of a form, the header row less so
	table.SetCellPadding(5.0, 7.0)
	for _, cell := range table.GetRow(0) {
		cell.SetTopPadding(4.0)
		cell.SetBottomPadding(4.0)
	}
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(5.0, 22.0, 22.0, 9.0, 9.0, 9.0, 9.0, 15.0)
	table.SetTextAlignmentInColumn(0, alignment.Right)
	table.SetTextColorInColumn(0, color.DimGray)
	table.SetLocation(left, top)
	table.SetFirstPageTopMargin(y + 16.0)
	table.SetBottomMargin(footerRoom)
	table.DrawOnPagesFrom(pdf, page, &pages, letter.Landscape())

	// The footer, on every page, now that their number is known
	for i := 0; i < len(pages); i++ {
		p := pages[i]
		footer := pdfjet.NewTextLine(regular, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		footer.SetFontSize(10.0)
		footer.SetLocation(left+width-footer.GetWidth(), p.GetHeight()-30.0)
		p.AddArtifactBMC()
		footer.DrawOn(p)
		p.AddEMC()
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
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
	Example83()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_83 => %4d ms\n", time1-time0)
}
