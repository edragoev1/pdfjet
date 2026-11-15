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

// item is a line of the delivery: how many were ordered, how many are in
// this delivery, and the weight of one, in kilograms.
type item struct {
	description string
	ordered     int
	delivered   int
	kgEach      float64
}

// The data of the delivery. In an application it comes from the database.
const number = "D-2026-064"

var date = time.Date(2026, time.September, 24, 0, 0, 0, 0, time.UTC)

const order = "CB-PO-3310"
const boxes = 2
const carrier = "Harbourline Courier, tracking HC 4471 2290"
const checkDays = 5

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com"
const deliverTo = "Contoso Books\nAttn: Receiving\n400 King Street West\nToronto, ON M5V 1K4"

var items = []item{
	{"Annual report, printed copy, saddle stitched", 50, 50, 0.26},
	{"Display stand for the lobby", 1, 0, 3.8},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d
const gray = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example57 draws a delivery note that goes in the box with the goods, on
// Letter paper: the studio and the delivery note number at the top, where
// the goods go and how they travel, a table of what was ordered and what is
// in this delivery, a note on checking the goods, and a line for the
// receiver to sign.
//
// A delivery note is a finished document that the customer receives, not a
// form; only its last line is filled in, by hand, at the door. It has no
// prices, as it is read by whoever opens the box. The table says for each
// item how many were ordered and how many are in the box, and an item that
// is short says how many are to follow, computed from the two numbers; the
// weight of the shipment is computed from the items too. The table
// goes on to as many pages as it needs, with its header row on each.
//
// The goods are the printed copies of the invoice of Example_53, and a
// display stand that follows them, invoiced when it ships. The document is
// PDF/UA-1: the title is a heading, the items a table, and the rule and the
// line to sign on are artifacts. The layout is that of the delivery note
// template of PDFjet Forms. The companies and the numbers are made up.
func Example57() {
	pdf, err := pdfjet.NewPDFFile("Example_57.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Delivery note " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The weight of the shipment, from the items in it, for the carrier
	kg := 0.0
	for _, it := range items {
		kg += float64(float64(it.delivered) * it.kgEach)
	}

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	middle := (left + right) / 2.0

	// The header: the studio on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Delivery note")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 72.0)
	title.DrawOn(page)

	pdfjet.NewTextBlock(regular, "No. "+number+"\nDate: "+date.Format("2006-01-02")+"\nYour order: "+order).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	line(page, left, right, 148.0, rule, 1.0)

	// Where the goods go, and how they travel: side by side, the taller
	// of the two decides where the table starts
	heading(page, semiBold, "Deliver to", left, 182.0)
	y1 := pdfjet.NewTextBlock(regular, deliverTo).SetWidth(middle-left).
		SetLocation(left, 192.0).DrawOn(page)[1]
	heading(page, semiBold, "Shipment", middle, 182.0)
	shipment := fmt.Sprintf("%d boxes, %.1f kg", boxes, kg) + "\n" + carrier
	y2 := pdfjet.NewTextBlock(regular, shipment).SetWidth(right-middle).
		SetLocation(middle, 192.0).DrawOn(page)[1]
	y := max(y1, y2) + 24.0

	// The items: what was ordered, and what is in the box
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Item", "Ordered", "Delivered"))
	for _, it := range items {
		delivered := strconv.Itoa(it.delivered)
		if it.delivered < it.ordered {
			delivered += " (" + strconv.Itoa(it.ordered-it.delivered) + " to follow)"
		}
		rows = append(rows, row(regular, it.description, strconv.Itoa(it.ordered), delivered))
	}
	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(semiBold, color.Black, header)
	table.SetCellPadding(4.0, 3.0)
	table.SetCellBorderWidth(0.5)
	table.SetCellBorderColor(borderColor)
	table.SetWidth(right - left)
	table.SetColumnWidthsInPercent(60.0, 15.0, 25.0)
	table.SetTextAlignmentInColumn(1, alignment.Right)
	table.SetTextAlignmentInColumn(2, alignment.Right)
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(y)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// The note and the line to sign stay together under the last row, or
	// go on a page of their own when they no longer fit there
	y = end[1] + 22.0
	if y+110.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	y = pdfjet.NewTextBlock(regular, "Please check the goods on arrival and tell us within "+
		strconv.Itoa(checkDays)+" days if anything is missing or damaged.").
		SetLineSpacing(1.15).
		SetWidth(right-left).
		SetLocation(left, y).
		DrawOn(page)[1]

	// Room to sign by hand above the line
	pdfjet.NewTextLine(regular, "Received in good order by (name, signature and date):").
		SetLocation(left, y+30.0).DrawOn(page)
	line(page, left, left+float32(0.6*(right-left)), y+74.0, gray, 0.75)

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

// heading draws the heading of a part, its baseline at y.
func heading(page *pdfjet.Page, font *pdfjet.Font, text string, x, y float32) {
	pdfjet.NewTextLine(font, text).SetStructureType(structelem.H2).SetFontSize(12.0).
		SetLocation(x, y).DrawOn(page)
}

// line draws a line across, the rule under the header or the line to sign
// on, which the text above it names: an artifact that a screen reader skips.
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

func main() {
	time0 := time.Now().UnixMilli()
	Example57()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_57 => %4d ms\n", time1-time0)
}
