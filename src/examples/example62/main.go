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

// service is a service of a section and its price, in cents before tax.
type service struct {
	name  string
	price int64
}

// section is a kind of work: its heading, and its services with their
// prices.
type section struct {
	title    string
	services []service
}

const seller = "Northwind Studio"

var pricesFrom = time.Date(2026, time.November, 1, 0, 0, 0, 0, time.UTC)

var sections = []section{
	{"Design", []service{
		{"Logo and business card", 45000},
		{"Brochure, up to 8 pages", 90000},
		{"Poster or flyer", 25000},
		{"Design and layout of larger work, per day", 80000}}},
	{"Print", []service{
		{"Business cards, 250", 6000},
		{"Flyers, 500, color, both sides", 18000},
		{"Posters, 11 x 17 in, 25", 9500},
		{"Booklets up to 48 pages, saddle stitched, per copy", 600}}},
	{"Accessible PDFs", []service{
		{"PDF/UA edition of a printed piece, per page", 2500},
		{"Fillable PDF form, per page", 6000}}},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const top float32 = 64.0        // where the next pages start
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example62 draws the price list of a small design studio, on Letter paper:
// a centered title with the date the prices start, then a section for each
// kind of work, a heading and a table of the services and their prices, and
// a note on taxes and quotes at the end.
//
// A price list is a finished document that customers receive, not a form.
// It is several tables, one under the other: each is drawn with
// DrawOnPagesFrom starting where the one before it ended, and goes on to the
// next page when it needs to, with its header row repeated. A heading is not
// left alone at the foot of a page: when there is no room for it and two
// rows under it, the section starts on a new page.
//
// The document is PDF/UA-1: the title is the H1 and the sections H2, so a
// screen reader can jump from one section to the next, and the rule and the
// page numbers are artifacts. The layout is that of the price list template
// of PDFjet Forms. The company and the prices are made up.
func Example62() {
	pdf, err := pdfjet.NewPDFFile("Example_62.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Price list, " + seller)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	bottom := page.GetHeight() - footerRoom

	// The title and the date the prices start, centered, and a rule
	title := pdfjet.NewTextLine(semiBold, "Price list")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(blue)
	title.SetLocation((page.GetWidth()-title.GetWidth())/2.0, 72.0)
	title.DrawOn(page)

	from := pricesFrom.Format("January 2, 2006")
	subtitle := pdfjet.NewTextLine(regular, seller+" · Prices from "+from)
	subtitle.SetFontSize(12.0)
	subtitle.SetLocation((page.GetWidth()-subtitle.GetWidth())/2.0, 96.0)
	subtitle.DrawOn(page)

	line := pdfjet.NewLine(left, 116.0, right, 116.0)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// The sections, each under the one before it
	var y float32 = 116.0
	for _, section := range sections {
		// The heading and the header row and two rows of the table, about
		// 90 points, or the section starts on a new page
		if y+90.0 > bottom {
			page = pdfjet.NewPageDetached(pdf, letter.Portrait())
			pages = append(pages, page)
			y = top - 36.0
		}
		heading := pdfjet.NewTextLine(semiBold, section.title)
		heading.SetStructureType(structelem.H2)
		heading.SetFontSize(14.0)
		heading.SetTextColor(blue)
		heading.SetLocation(left, y+36.0)
		heading.DrawOn(page)

		rows := make([][]*pdfjet.Cell, 0)
		rows = append(rows, row(semiBold, "Service", "Price"))
		for _, service := range section.services {
			rows = append(rows, row(regular, service.name, money(service.price)))
		}
		table := pdfjet.NewTable()
		table.SetTableData(rows, 1)
		table.SetHeaderRowStyle(semiBold, color.Black, header)
		table.SetCellPadding(4.0, 3.0)
		table.SetCellBorderWidth(0.5)
		table.SetCellBorderColor(borderColor)
		table.SetWidth(right - left)
		table.SetColumnWidthsInPercent(70.0, 30.0)
		table.SetTextAlignmentInColumn(1, alignment.Right)
		table.SetLocation(left, top-16.0)
		table.SetFirstPageTopMargin(y + 46.0)
		table.SetBottomMargin(footerRoom)
		y = table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())[1]
		page = pages[len(pages)-1]
	}

	// The note on taxes and quotes, under the last table
	if y+40.0 > bottom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = top - 20.0
	}
	pdfjet.NewTextBlock(regular, "Prices in Canadian dollars, before tax. Ask us for a quote "+
		"for larger work.").
		SetWidth(right-left).
		SetLocation(left, y+20.0).
		DrawOn(page)

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

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cells = append(cells, pdfjet.NewCell(font, text))
	}
	return cells
}

// money returns the amount of cents with two decimals and commas between the
// thousands, as 1,200.00, whatever the locale of the computer.
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
	Example62()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_62 => %4d ms\n", time1-time0)
}
