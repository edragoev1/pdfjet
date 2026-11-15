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

// item is a line of the pro forma invoice. The unit price is in cents, an integer.
type item struct {
	description string
	quantity    int64
	unitPrice   int64
}

func (it item) amount() int64 {
	return it.unitPrice * it.quantity
}

// The data of the pro forma invoice. In an application it comes from the database.
const number = "PF-2026-012"

var date = time.Date(2026, time.November, 2, 0, 0, 0, 0, time.UTC)

const validDays = 30
const currency = "CAD"

const seller = "Northwind Studio"
const sellerAddress = "12 Harbour Street\nPort Elgin, ON N0H 2C0\nhello@example.com\nHST 81234 5678 RT0001"
const billToAddress = "Fabrikam Press\n18 Rue des Lilas\n75011 Paris, France"
const shipToAddress = "Fabrikam Press, warehouse\n4 Avenue de l'Europe\n94200 Ivry-sur-Seine, France"
const customs = "Country of origin: Canada\nWeight: 18 kg, in 2 parcels"

var items = []item{
	{"Art prints, A3, numbered edition", 40, 3500},
	{"Archival portfolio boxes", 4, 4500},
	{"Shipping, insured", 1, 12000},
}

const blue = 0x1f58b0
const rule = 0xdde1e8
const header = 0xe3edf9
const borderColor = 0x4d4d4d

const margin float32 = 48.0
const footerRoom float32 = 80.0 // the bottom margin, clear of "Page i of N"

// Example78 draws a pro forma invoice of a small studio to a customer
// abroad, on Letter paper: the seller, the number and the date until which
// the prices hold, the address to bill and the address to ship to, a table of
// the goods, the country of origin and the weight for customs, the total, and
// the terms.
//
// A pro forma invoice is a finished document that the customer receives, not
// a form. It is sent before the sale: the customer pays it, or takes it to
// the bank or to customs, and the goods are shipped with the real invoice
// once the money is in. So it says what it is not, a tax invoice, and how
// long its prices are good for. Goods exported from Canada carry no sales
// tax, so the total is the sum of the lines.
//
// The amounts are integer cents, never float32 or float64, and the total is
// computed from the goods. The table of the goods goes on to as many pages
// as it needs, with its header row on each; the total and the terms go
// under the last row, and each page says "Page 1 of 1" at the bottom.
//
// Both sides keep the document, so it is archival as well as accessible,
// PDF/A-3a and PDF/UA-1 at once. The layout is that of the pro forma
// invoice template of PDFjet Forms. The companies and the numbers are made
// up.
func Example78() {
	pdf, err := pdfjet.NewPDFFile("Example_78.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Pro forma invoice " + number)
	pdf.SetAuthor(seller)
	pdf.SetLanguage("en-CA")

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	regular.SetSize(11.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	semiBold.SetSize(11.0)

	// The total, from the goods. Exported, they carry no sales tax.
	var total int64 = 0
	for _, it := range items {
		total += it.amount()
	}

	// The pages are kept in a list and added to the document at the end,
	// when the footer can say how many there are.
	pages := make([]*pdfjet.Page, 0)
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	left := margin
	right := page.GetWidth() - margin
	half := (right - left) / 2.0

	// The header: the seller on the left, the title and the numbers on the right
	pdfjet.NewTextLine(semiBold, seller).SetFontSize(16.0).SetLocation(left, 64.0).DrawOn(page)
	pdfjet.NewTextBlock(regular, sellerAddress).SetWidth(250.0).SetLocation(left, 80.0).DrawOn(page)

	title := pdfjet.NewTextLine(semiBold, "Pro forma invoice")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(22.0)
	title.SetTextColor(blue)
	title.SetLocation(right-title.GetWidth(), 70.0)
	title.DrawOn(page)

	validUntil := date.AddDate(0, 0, validDays).Format("2006-01-02")
	numbers := "No. " + number +
		"\nDate: " + date.Format("2006-01-02") +
		"\nValid until: " + validUntil
	pdfjet.NewTextBlock(regular, numbers).
		SetTextAlignment(alignment.Right).
		SetWidth(200.0).
		SetLocation(right-200.0, 94.0).
		DrawOn(page)

	// A thin rule under the header, decoration only, so an artifact that a
	// screen reader skips
	line := pdfjet.NewLine(left, 148.0, right, 148.0)
	line.SetStrokeColor(rule)
	page.AddArtifactBMC()
	line.DrawOn(page)
	page.AddEMC()

	// Who pays, on the left, and where the goods go, on the right
	address(page, semiBold, regular, "Bill to", billToAddress, left, half)
	address(page, semiBold, regular, "Ship to", shipToAddress, left+half, half)

	// The goods: a header row, repeated on every page the table goes on to
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Description", "Quantity", "Price", "Amount"))
	for _, it := range items {
		rows = append(rows, row(regular, it.description, strconv.FormatInt(it.quantity, 10),
			money(it.unitPrice), money(it.amount())))
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
	// Under the addresses on the first page, at the top of the next ones,
	// and clear of the footer on all of them
	table.SetLocation(left, margin)
	table.SetFirstPageTopMargin(260.0)
	table.SetBottomMargin(footerRoom)
	end := table.DrawOnPagesFrom(pdf, page, &pages, letter.Portrait())
	page = pages[len(pages)-1]

	// What customs asks for on the left, the total on the right, and the
	// terms under them, all under the last row. When they no longer fit
	// on its page, they go on a page of their own.
	y := end[1] + 22.0
	if y+110.0 > page.GetHeight()-footerRoom {
		page = pdfjet.NewPageDetached(pdf, letter.Portrait())
		pages = append(pages, page)
		y = 64.0
	}
	totalLine := pdfjet.NewTextLine(semiBold, "Total: "+money(total)+" "+currency)
	totalLine.SetFontSize(14.0)
	totalLine.SetLocation(right-totalLine.GetWidth(), y+14.0)
	totalLine.DrawOn(page)
	y = pdfjet.NewTextBlock(regular, customs).SetWidth(half).SetLocation(left, y).DrawOn(page)[1]

	pdfjet.NewTextBlock(regular, "Not a tax invoice: the goods are shipped when this amount is received, "+
		"by bank transfer, and the invoice is sent with them. Prices are valid until "+
		validUntil+".").
		SetWidth(right-left).
		SetLocation(left, y+16.0).
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

// address draws an address under its heading, in a column at x of the
// width, under the rule of the header.
func address(page *pdfjet.Page, headingFont, font *pdfjet.Font, heading, text string, x, width float32) {
	line := pdfjet.NewTextLine(headingFont, heading)
	line.SetStructureType(structelem.H2)
	line.SetFontSize(12.0)
	line.SetLocation(x, 182.0)
	line.DrawOn(page)
	pdfjet.NewTextBlock(font, text).SetWidth(width-12.0).SetLocation(x, 192.0).DrawOn(page)
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
// thousands, as 1,700.00, whatever the locale of the computer.
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
	Example78()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_78 => %4d ms\n", time1-time0)
}
