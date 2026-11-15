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

const site = "Pinecrest Community Center"

// The areas to inspect, each its name and then its items
var areas = [][]string{
	{"Exits and fire safety",
		"Exits and hallways are clear",
		"Exit signs are lit",
		"Fire extinguishers are in place and charged",
		"Smoke alarms were tested"},
	{"Work areas",
		"Floors are dry and free of trip hazards",
		"Electrical cords are in good shape",
		"Shelves and storage are stable",
		"The first aid kit is stocked"},
}

const blue = 0x1f58b0
const red = 0xb00020
const rule = 0xdde1e8
const box = 0x4d4d4d
const header = 0xe3edf9
const area = 0xf1f3f6

// The grid: the margin, the width of the page between the margins, and the
// gap between two fields of a row
const left float32 = 48.0
const width float32 = 516.0
const gap float32 = 12.0
const half = (width - gap) / 2.0

// A box tall enough for a line of handwriting, and the height of each line
// more
const boxHeight float32 = 24.0
const lineHeight float32 = 20.0

// The side of a box to tick, about the height of the capital letters
const tickSize float32 = 11.0

// Room at the foot of each page for the footer, and the height of the fields
// under the list
const footerRoom float32 = 72.0 // the bottom margin, clear of "Page i of N"
const fieldsHeight float32 = 170.0

// form is the page being drawn on and the fonts of the form.
type form struct {
	page      *pdfjet.Page
	labelFont *pdfjet.Font
	textFont  *pdfjet.Font
}

// Example71 draws a monthly safety inspection checklist to print and take on
// a walk through a building: who inspects, when and where, then for each
// area a row for each item to check, with a box to tick when it is in order,
// a box when it needs fixing, and room for a note; under the list, the
// problems found, the date of the next inspection, and the signature.
//
// This is a form, not a finished document: the inspector fills it in. The
// fields above and below the list are drawn as in the job application of
// Example_72, a label above each box. The list is a Table: the items come
// from an array, each area is a row that spans the four columns, and each
// box to tick is a square, a Rect placed in its cell with SetDrawable. The
// table goes on to as many pages as it needs, with its header row on each,
// so a building with sixty items prints on two pages with the same code; the
// fields under it follow the last row, or go to a page of their own when
// they no longer fit.
//
// The document is PDF/UA-1: the title is a heading, the list is a table that
// a screen reader reads by its column headers, and the boxes, of the fields
// and to tick, are artifacts. The layout is that of the inspection checklist
// template of PDFjet Forms. The building is made up.
func Example71() {
	pdf, err := pdfjet.NewPDFFile("Example_71.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Monthly safety inspection")
	pdf.SetAuthor(site)
	pdf.SetLanguage("en-US")

	f := &form{}
	f.textFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f.textFont.SetSize(11.0)
	f.labelFont = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f.labelFont.SetSize(10.0)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	italic.SetSize(10.0)

	// The pages are kept in a list and added to the document at the end,
	// when the footer can say how many there are.
	pages := make([]*pdfjet.Page, 0)
	f.page = pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, f.page)

	// The title, how to use the list, and a rule
	f.heading("Monthly safety inspection", structelem.H1, 20.0, 62.0)
	intro := pdfjet.NewTextBlock(f.textFont, "Walk through each area, tick what is in "+
		"order, and note anything that needs fixing.")
	intro.SetLineSpacing(1.15)
	intro.SetLocation(left, 74.0)
	intro.SetWidth(width)
	y := intro.DrawOn(f.page)[1] + 12.0
	f.rule(y)

	pdfjet.NewTextLine(italic, "Fields marked * are required.").
		SetTextColor(red).
		SetLocation(left, y+20.0).
		DrawOn(f.page)

	// Where, who and when, a row of a half and two quarters. The list is
	// printed for one building, so its name is filled in already.
	y += 40.0
	quarter := (half - gap) / 2.0
	f.field("Location *", left, y, half, 1, site)
	f.field("Inspected by *", left+half+gap, y, quarter, 1, "")
	y = f.field("Date *", left+half+gap+quarter+gap, y, quarter, 1, "")

	// The list: a header row, then each area and its items. The row of an
	// area is kept on the page of its first item.
	rows := make([][]*pdfjet.Cell, 0)
	areaRows := make([]int, 0)
	rows = append(rows, row(pdfjet.NewCell(f.labelFont, "Item"), pdfjet.NewCell(f.labelFont, "OK"),
		pdfjet.NewCell(f.labelFont, "To fix"), pdfjet.NewCell(f.labelFont, "Notes")))
	for _, a := range areas {
		// The name of the area across the row; a cell that spans columns is
		// followed by empty ones that it covers
		name := pdfjet.NewCell(f.labelFont, a[0]).SetColSpan(4).SetBackgroundColor(area)
		areaRows = append(areaRows, len(rows))
		rows = append(rows, row(name, pdfjet.NewEmptyCell(f.textFont),
			pdfjet.NewEmptyCell(f.textFont), pdfjet.NewEmptyCell(f.textFont)))
		for i := 1; i < len(a); i++ {
			rows = append(rows, row(pdfjet.NewCell(f.textFont, a[i]), tick(f.textFont), tick(f.textFont),
				pdfjet.NewEmptyCell(f.textFont)))
		}
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(f.labelFont, color.Black, header)
	table.SetCellPadding(5.0, 6.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(box)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(50.0, 9.0, 9.0, 32.0)
	table.SetTextAlignmentInColumn(1, alignment.Center)
	table.SetTextAlignmentInColumn(2, alignment.Center)
	for _, index := range areaRows {
		table.KeepRowWithNext(index)
	}
	// Under the fields on the first page, at the top of the next ones, and
	// clear of the footer on all of them
	table.SetLocation(left, 48.0)
	table.SetFirstPageTopMargin(y + 6.0)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, f.page, &pages, letter.Portrait())
	f.page = pages[len(pages)-1]

	// The fields under the last row. When they no longer fit on its page,
	// they go on a page of their own.
	y = end[1] + 18.0
	if y+fieldsHeight > f.page.GetHeight()-footerRoom {
		f.page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, f.page)
		y = 48.0
	}
	y = f.field("Problems found, and who will fix them", left, y, width, 3, "")
	pdfjet.NewCheckBox(f.textFont, "I confirm this inspection and sign below. *").
		SetBorderColor(box).
		SetLocation(left, y).
		DrawOn(f.page)
	y += 30.0
	f.field("Inspector's signature *", left, y, half, 1, "")
	f.field("Date *", left+half+gap, y, quarter, 1, "")
	f.field("Next inspection", left+half+gap+quarter+gap, y, quarter, 1, "")

	// The footer, on every page, now that their number is known
	for i, p := range pages {
		footer := pdfjet.NewTextLine(f.textFont, site+", monthly safety inspection, page "+
			strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		footer.SetFontSize(9.0)
		footer.SetTextColor(color.DimGray)
		footer.SetLocation(left, p.GetHeight()-36.0)
		p.AddArtifactBMC()
		footer.DrawOn(p)
		p.AddEMC()
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// tick returns a cell with a box to tick: a square, placed in the cell with
// SetDrawable and centered as the column is. A Rect is drawn as an artifact;
// the column header says what a tick in it means.
func tick(font *pdfjet.Font) *pdfjet.Cell {
	rect := pdfjet.NewRect(0.0, 0.0, tickSize, tickSize)
	rect.SetBorderColor(box)
	rect.SetBorderWidth(0.75)
	return pdfjet.NewEmptyCell(font).SetDrawable(rect)
}

// row makes a row of the table, of the cells.
func row(cells ...*pdfjet.Cell) []*pdfjet.Cell {
	return append([]*pdfjet.Cell{}, cells...)
}

// heading draws a heading in blue, its baseline at y, and returns where the
// field under it starts.
func (f *form) heading(text string, level structelem.StructElem, size, y float32) float32 {
	heading := pdfjet.NewTextLine(f.labelFont, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(blue)
	heading.SetLocation(left, y)
	heading.DrawOn(f.page)
	return y + 16.0
}

// label draws the label of a field, its top at y.
func (f *form) label(text string, x, y float32) {
	label := pdfjet.NewTextLine(f.labelFont, text)
	label.SetLocation(x, y+f.labelFont.GetAscent(f.labelFont.GetSize()))
	label.DrawOn(f.page)
}

// field draws a field: its label at y and the box under it, of room for the
// lines of handwriting, with the value in it when there is one, a value that
// is not "". Returns where the next row starts.
func (f *form) field(text string, x, y, w float32, lines int, value string) float32 {
	f.label(text, x, y)
	top := y + 16.0
	height := boxHeight + float32(float32(lines-1)*lineHeight)
	rect := pdfjet.NewRect(x, top, w, height)
	rect.SetBorderColor(box)
	rect.SetBorderWidth(0.5)
	rect.DrawOn(f.page)
	if value != "" {
		line := pdfjet.NewTextLine(f.textFont, value)
		line.SetTextColor(blue)
		line.SetLocation(x+6.0, top+16.0)
		line.DrawOn(f.page)
	}
	return top + height + 12.0
}

// rule draws a thin line across the page, decoration only, so an artifact
// that a screen reader skips.
func (f *form) rule(y float32) {
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(rule)
	f.page.AddArtifactBMC()
	line.DrawOn(f.page)
	f.page.AddEMC()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example71()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_71 => %4d ms\n", time1-time0)
}
