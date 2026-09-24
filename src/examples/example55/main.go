// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/relationship"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The items of the invoice: the description, the quantity and the unit
// price in cents. The amounts are in cents, so that they add up exactly.
var items = []string{
	"Letterpress business cards, 500",
	"A4 letterhead paper, box of 500",
	"DL envelopes, box of 250",
	"Logo design, hours",
	"Delivery",
}
var quantities = []int64{2, 3, 4, 6, 1}
var prices = []int64{4500, 2850, 1275, 5500, 990}

const vatPercent = 19

const navy = 0x1d3557
const red = 0xe63946
const stripe = 0xf1f4f8

// The description of the invoice for the metadata of the document: the
// file that holds it, its type and its profile, and the extension schema
// that declares these properties, which PDF/A asks of properties of its own.
var facturXMetadata = "<rdf:Description rdf:about=\"\"\n" +
	"    xmlns:fx=\"urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#\">\n" +
	"  <fx:DocumentType>INVOICE</fx:DocumentType>\n" +
	"  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n" +
	"  <fx:Version>1.0</fx:Version>\n" +
	"  <fx:ConformanceLevel>BASIC</fx:ConformanceLevel>\n" +
	"</rdf:Description>\n" +
	"<rdf:Description rdf:about=\"\"\n" +
	"    xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n" +
	"    xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n" +
	"    xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n" +
	"  <pdfaExtension:schemas>\n" +
	"    <rdf:Bag>\n" +
	"      <rdf:li rdf:parseType=\"Resource\">\n" +
	"        <pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>\n" +
	"        <pdfaSchema:namespaceURI>urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#</pdfaSchema:namespaceURI>\n" +
	"        <pdfaSchema:prefix>fx</pdfaSchema:prefix>\n" +
	"        <pdfaSchema:property>\n" +
	"          <rdf:Seq>\n" +
	property("DocumentFileName", "The name of the embedded XML document") +
	property("DocumentType", "The type of the hybrid document, in capital letters") +
	property("Version", "The version of the specification of the XML schema") +
	property("ConformanceLevel", "The profile of the XML document") +
	"          </rdf:Seq>\n" +
	"        </pdfaSchema:property>\n" +
	"      </rdf:li>\n" +
	"    </rdf:Bag>\n" +
	"  </pdfaExtension:schemas>\n" +
	"</rdf:Description>"

func property(name, description string) string {
	return "            <rdf:li rdf:parseType=\"Resource\">\n" +
		"              <pdfaProperty:name>" + name + "</pdfaProperty:name>\n" +
		"              <pdfaProperty:valueType>Text</pdfaProperty:valueType>\n" +
		"              <pdfaProperty:category>external</pdfaProperty:category>\n" +
		"              <pdfaProperty:description>" + description + "</pdfaProperty:description>\n" +
		"            </rdf:li>\n"
}

// Example55 draws an invoice, as a business sends one: a PDF/A-3 document
// that carries the invoice as data too, in the file factur-x.xml, the way the
// e-invoice standards Factur-X and ZUGFeRD ask, so that accounting software
// reads the amounts instead of someone typing them in again.
//
// The page has the logo of the seller, an SVG file written as a drawing
// program exports one, with a style sheet, groups and shapes; the addresses
// and the dates; a table of the items with a header row, striped rows and the
// totals in its footer rows; and a QR code a banking app scans to pay, in the
// format of the European Payments Council. The document is PDF/A-3a and
// PDF/UA-1 at once, PDF_A_3A_UA_1, so it is kept as PDF/A and tagged for
// screen readers: a heading, paragraphs, a table and a
// figure.
//
// The XML is attached with AddAssociatedFile, with the relationship
// Alternative, since it is the same invoice in another form, and AddMetadata
// says in the metadata of the document which file is the invoice, with the
// extension schema PDF/A asks for properties of its own. The companies, the
// addresses and the numbers are made up.
func Example55() error {
	pdf, err := pdfjet.NewPDFFile("Example_55.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_3A_UA_1)
	pdf.SetTitle("Invoice 2026-0147")
	pdf.SetAuthor("Lindenberg Paper & Print GmbH")
	pdf.SetLanguage("en-US")

	// The invoice as data, which the document carries, and the description
	// of it in the metadata. Files are added before the first page.
	file, err := os.Open("data/invoice/factur-x.xml")
	if err != nil {
		return err
	}
	defer file.Close()
	pdf.AddAssociatedFile(pdfjet.NewEmbeddedFileWithRelationship(
		pdf,
		"factur-x.xml",
		bufio.NewReader(file),
		true,
		"text/xml",
		relationship.Alternative,
		"The invoice as data, in the profile BASIC of Factur-X and ZUGFeRD."))
	pdf.AddMetadata(facturXMetadata)

	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular).SetSize(10.0)
	semiBold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold).SetSize(10.0)

	page := pdfjet.NewPage(pdf, a4.Portrait())
	var left float32 = 50.0
	right := page.GetWidth() - 50.0
	var middle float32 = 345.0

	// The letterhead: the logo, and the address of the seller beside it
	logo, err := pdfjet.NewSVGImageFromFile("data/invoice/logo.svg")
	if err != nil {
		return err
	}
	logo.SetAltDescription("The logo of Lindenberg Paper & Print: a sheet of paper on a navy tile.")
	logo.SetLocation(left, 50.0)
	logo.DrawOn(page)

	var y float32 = 58.0
	line(page, semiBold, "Lindenberg Paper & Print GmbH", middle, y, color.Black)
	for _, text := range []string{"Hafenstraße 12", "20457 Hamburg, Germany", "VAT ID DE123456789"} {
		y += 14.0
		line(page, regular, text, middle, y, color.Gray)
	}

	page.AddArtifactBMC()
	page.SetPenColor(red)
	page.SetPenWidth(1.5)
	page.DrawLine(left, 125.0, right, 125.0)
	page.AddEMC()

	title := pdfjet.NewTextLine(semiBold, "Invoice")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(26.0)
	title.SetTextColor(navy)
	title.SetLocation(left, 172.0)
	title.DrawOn(page)

	// The customer on the left, and the numbers of the invoice on the right
	y = 205.0
	billTo := pdfjet.NewTextLine(regular, "BILL TO")
	billTo.SetFontSize(8.0)
	billTo.SetTextColor(color.Gray)
	billTo.SetLocation(left, y)
	billTo.DrawOn(page)
	line(page, semiBold, "Kranich Design Studio", left, y+16.0, color.Black)
	line(page, regular, "Lindenallee 4", left, y+30.0, color.Black)
	line(page, regular, "50668 Köln, Germany", left, y+44.0, color.Black)

	facts := [][]string{
		{"Invoice number", "2026-0147"},
		{"Invoice date", "23 September 2026"},
		{"Due date", "23 October 2026"},
		{"Customer number", "K-3310"},
	}
	for i := 0; i < len(facts); i++ {
		factY := y + 16.0 + float32(i)*14.0
		line(page, regular, facts[i][0], middle, factY, color.Gray)
		line(page, semiBold, facts[i][1], right-semiBold.StringWidth(semiBold.GetSize(), facts[i][1]), factY, color.Black)
	}

	// The items, and the totals in the footer rows of the table
	var net int64 = 0
	for i := 0; i < len(items); i++ {
		net += quantities[i] * prices[i]
	}
	vat := (net*vatPercent + 50) / 100 // Rounded to the cent
	total := net + vat

	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(semiBold, "Description", "Quantity", "Unit price", "VAT", "Amount (EUR)"))
	for i := 0; i < len(items); i++ {
		rows = append(rows, row(regular, items[i], strconv.FormatInt(quantities[i], 10), money(prices[i]),
			strconv.Itoa(vatPercent)+"%", money(quantities[i]*prices[i])))
	}
	rows = append(rows, totalRow(regular, "Net amount", money(net)))
	rows = append(rows, totalRow(regular, "VAT "+strconv.Itoa(vatPercent)+"%", money(vat)))
	rows = append(rows, totalRow(semiBold, "Total due (EUR)", money(total)))

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetNumberOfFooterRows(3)
	table.SetHeaderRowStyle(semiBold, color.White, navy)
	table.SetAlternateRowColor(stripe)
	table.SetCellBorders(false)
	table.SetWidth(right - left)
	table.SetColumnWidthsInPercent(46.0, 12.0, 16.0, 8.0, 18.0)
	for column := 1; column < 5; column++ {
		table.SetTextAlignmentInColumn(column, alignment.Right)
	}
	table.SetLocation(left, 290.0)
	xy := table.DrawOn(page)

	// A line over the total due, an artifact
	page.AddArtifactBMC()
	page.SetPenColor(navy)
	page.SetPenWidth(1.0)
	page.DrawLine(middle, xy[1]-20.0, right, xy[1]-20.0)
	page.AddEMC()

	// How to pay, and the QR code of the payment, which a banking app reads
	paymentY := xy[1] + 40.0
	heading := pdfjet.NewTextLine(semiBold, "Payment")
	heading.SetStructureType(structelem.H2)
	heading.SetFontSize(13.0)
	heading.SetTextColor(navy)
	heading.SetLocation(left, paymentY)
	heading.DrawOn(page)

	payment := pdfjet.NewTextBlock(regular,
		"Please transfer EUR "+money(total)+" by 23 October 2026 to Lindenberg Paper & Print GmbH, "+
			"IBAN DE89 3704 0044 0532 0130 00, with the reference 2026-0147.\n\n"+
			"Or scan the code with your banking app, which fills in the transfer.")
	payment.SetLineSpacing(1.4)
	payment.SetLocation(left, paymentY+10.0)
	payment.SetWidth(330.0)
	payment.DrawOn(page)

	// An EPC QR code, which banking apps in Europe read as a SEPA credit
	// transfer: the version, the character set, the name, the IBAN, the
	// amount and the reference, one to a line, with error correction M.
	epc := "BCD\n002\n1\nSCT\n\nLindenberg Paper & Print GmbH\nDE89370400440532013000\n" +
		"EUR" + money(total) + "\n\n\nInvoice 2026-0147"
	qr := qrcode.NewQRCode(epc, errorcorrectionlevel.M)
	qr.SetModuleLength(2.0)
	qr.SetLocation(right-90.0, paymentY-8.0)
	qrXY := qr.DrawOn(page)
	caption := pdfjet.NewTextLine(regular, "Scan to pay")
	caption.SetFontSize(8.0)
	caption.SetTextColor(color.Gray)
	caption.SetLocation(right-90.0, qrXY[1]+12.0)
	caption.DrawOn(page)

	// What makes this invoice data as well
	note := pdfjet.NewTextBlock(regular,
		"This invoice is also data. It is a PDF/A-3 document that carries its content as "+
			"factur-x.xml, in the profile BASIC of Factur-X and ZUGFeRD, so that accounting "+
			"software reads the items and the amounts instead of someone typing them in. "+
			"Open the attachments of your PDF viewer to see it.")
	note.SetFontSize(9.0)
	note.SetLineSpacing(1.4)
	note.SetTextColor(navy)
	note.SetBackgroundColor(stripe)
	note.SetCornerRadius(6.0)
	note.SetPadding(12.0)
	note.SetLocation(left, qrXY[1]+45.0)
	note.SetWidth(right - left)
	note.DrawOn(page)

	footer := pdfjet.NewTextLine(regular,
		"Lindenberg Paper & Print GmbH  ·  Hafenstraße 12, 20457 Hamburg  ·  Registered in Hamburg, HRB 000000")
	footer.SetFontSize(8.0)
	footer.SetTextColor(color.Gray)
	footer.SetLocation((page.GetWidth()-footer.GetWidth())/2.0, page.GetHeight()-40.0)
	footer.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}

	return nil
}

// line draws a line of text in the font and the color.
func line(page *pdfjet.Page, font *pdfjet.Font, text string, x, y float32, c int32) {
	textLine := pdfjet.NewTextLine(font, text)
	textLine.SetTextColor(c)
	textLine.SetLocation(x, y)
	textLine.DrawOn(page)
}

// row makes a row of the table, a cell in the font for each text.
func row(font *pdfjet.Font, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for _, text := range texts {
		cell := pdfjet.NewCell(font, text)
		cell.SetPadding(6.0)
		cells = append(cells, cell)
	}
	return cells
}

// totalRow makes a row of the totals: its label over the first four columns,
// right aligned, and the amount. The cells the label spans are there and empty.
func totalRow(font *pdfjet.Font, label, amount string) []*pdfjet.Cell {
	cells := row(font, label, "", "", "", amount)
	cells[0].SetColSpan(4)
	cells[0].SetTextAlignment(alignment.Right)
	return cells
}

// money returns the amount of cents as euros and cents, with a comma between
// the thousands.
func money(cents int64) string {
	euros := strconv.FormatInt(cents/100, 10)
	var sb strings.Builder
	for i := 0; i < len(euros); i++ {
		if i > 0 && (len(euros)-i)%3 == 0 {
			sb.WriteByte(',')
		}
		sb.WriteByte(euros[i])
	}
	rest := cents % 100
	if rest < 10 {
		sb.WriteString(".0")
	} else {
		sb.WriteString(".")
	}
	sb.WriteString(strconv.FormatInt(rest, 10))
	return sb.String()
}

func main() {
	time0 := time.Now().UnixMilli()
	err := Example55()
	if err != nil {
		log.Fatal(err)
	}
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_55 => %4d ms\n", time1-time0)
}
