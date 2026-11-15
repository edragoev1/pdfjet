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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

// The geometry of the sheet, in points
const labelWidth float32 = 189.0  // 2 5/8 inch
const labelHeight float32 = 72.0  // 1 inch
const firstX float32 = 13.5       // 3/16 inch
const firstY float32 = 36.0       // 1/2 inch
const columnPitch float32 = 198.0 // 2 3/4 inch
const rowPitch float32 = 72.0     // 1 inch
const columns = 3
const rows = 10

// Inside a label: the margin kept clear, and the size of a QR code
// module. A QR code of PDFjet is 33 modules a side or more, so 49.5
// points here, which leaves 11 points above and below it.
const padding float32 = 9.0
const module float32 = 1.5

// The orders to ship: the order number, the name, and the address in two lines
var orders = [][]string{
	{"NW-2026-0401", "Maria Alvarez", "218 Cedar Lane", "Springfield, IL 62704"},
	{"NW-2026-0402", "Thomas Becker", "45 Orchard Road, Unit 3", "Burlington, VT 05401"},
	{"NW-2026-0403", "Aiko Tanaka", "1290 Bay View Drive", "Monterey, CA 93940"},
	{"NW-2026-0404", "Samuel Okafor", "77 Willow Street", "Hamilton, ON L8P 4R5"},
	{"NW-2026-0405", "Hannah Lindqvist", "9 Birch Court", "Duluth, MN 55803"},
	{"NW-2026-0406", "Daniel Moreau", "350 Rue des Érables", "Gatineau, QC J8T 3K2"},
	{"NW-2026-0407", "Priya Raman", "400 King Street West", "Toronto, ON M5V 1K4"},
	{"NW-2026-0408", "Ethan Walsh", "61 Harbor Point Road", "Portland, ME 04101"},
	{"NW-2026-0409", "Grace Kim", "1824 Elm Avenue, Apt 5B", "Ann Arbor, MI 48104"},
	{"NW-2026-0410", "Luis Fernández", "502 Mesa Verde Way", "Santa Fe, NM 87501"},
	{"NW-2026-0411", "Olivia Chen", "88 Lakeshore Boulevard", "Kingston, ON K7L 2Z4"},
	{"NW-2026-0412", "Noah Patel", "16 Quarry Hill", "Ithaca, NY 14850"},
	{"NW-2026-0413", "Emma Novak", "735 Prairie Street", "Lincoln, NE 68508"},
	{"NW-2026-0414", "Liam O'Connor", "23 Copper Ridge Trail", "Bozeman, MT 59715"},
	{"NW-2026-0415", "Sofia Rossi", "140 Vine Street", "Napa, CA 94559"},
	{"NW-2026-0416", "Benjamin Ward", "4 Lighthouse Lane", "Halifax, NS B3H 1Y6"},
	{"NW-2026-0417", "Chloe Dubois", "1101 Magnolia Drive", "Savannah, GA 31401"},
	{"NW-2026-0418", "Jacob Nguyen", "59 Riverbend Road", "Boise, ID 83702"},
	{"NW-2026-0419", "Ava Johansson", "312 Aspen Way, Suite 200", "Boulder, CO 80302"},
	{"NW-2026-0420", "Mateo García", "27 Palm Court", "Tucson, AZ 85701"},
	{"NW-2026-0421", "Isabella Brooks", "640 Maple Avenue", "Madison, WI 53703"},
	{"NW-2026-0422", "Lucas Schmidt", "18 Old Mill Road", "Lancaster, PA 17602"},
	{"NW-2026-0423", "Mia Thompson", "905 Sunset Terrace", "Victoria, BC V8V 2L4"},
	{"NW-2026-0424", "Henry Adeyemi", "73 Granite Street", "Concord, NH 03301"},
	{"NW-2026-0425", "Zoe Martin", "211 Pinecrest Drive", "Asheville, NC 28801"},
	{"NW-2026-0426", "Oliver Singh", "1500 Prairie Rose Road", "Regina, SK S4P 3Y2"},
	{"NW-2026-0427", "Lily Andersen", "36 Harbour Street", "Charlottetown, PE C1A 4N3"},
	{"NW-2026-0428", "William Reyes", "820 Canyon Road", "Flagstaff, AZ 86001"},
	{"NW-2026-0429", "Ella Kowalski", "5 Chestnut Square", "Providence, RI 02903"},
	{"NW-2026-0430", "James Mitchell", "447 Bluebonnet Lane", "Austin, TX 78704"},
}

// Example85 draws shipping labels, thirty to a sheet: the name and the
// address of each customer, and a QR code of the order number that the
// packer scans to find the order, on the common US sheet of 3 x 10 address
// labels.
//
// The sheet is Letter, 8.5 x 11 inches, with labels of 2 5/8 x 1 inch in 3
// columns and 10 rows. The labels start half an inch from the top and 3/16
// inch from the left; the columns are 2 3/4 inches apart, so there is a gap
// of 1/8 inch between them, and the rows 1 inch apart, with no gap. In
// points, 72 to the inch, that is a label of 189 x 72, the first at 13.5,
// 36, the next one to the right 198 further, the next one down 72 further.
// Print at actual size, not "fit to page", or the labels miss the sheet.
// For A4, sheets of 3 x 8 labels of 70 x 37 mm are common: change the
// constants, in points, mm * 72 / 25.4.
//
// Nothing is drawn closer than 9 points, 1/8 inch, to the edge of a label,
// as printers place the page a little differently each time. A line too
// long for its label is drawn smaller rather than cut off. The QR code has
// room around it, the quiet zone of four modules that scanners need.
//
// The document is PDF/UA-1: each label is text, and each QR code a figure
// that says the order number it carries. The people and the addresses are
// made up.
func Example85() {
	pdf, err := pdfjet.NewPDFFile("Example_85.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Shipping labels")
	pdf.SetLanguage("en-US")

	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)

	// A new sheet every thirty labels, filled row by row
	var page *pdfjet.Page
	for i := 0; i < len(orders); i++ {
		place := i % (columns * rows)
		if place == 0 {
			page = pdfjet.NewPage(pdf, letter.Portrait())
		}
		x := firstX + float32(float32(place%columns)*columnPitch)
		y := firstY + float32(float32(place/columns)*rowPitch)
		drawLabel(page, bold, regular, orders[i], x, y)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// drawLabel draws a label with its top left corner at x, y: the QR code on
// the right, centered from top to bottom, and the name and the address to
// the left of it.
func drawLabel(page *pdfjet.Page, bold, regular *pdfjet.Font, order []string, x, y float32) {
	qr := qrcode.NewQRCode(order[0], errorcorrectionlevel.M)
	qr.SetModuleLength(module)
	qr.SetAltDescription("Order " + order[0])
	qrSize := qr.DrawOn(nil)[0] // Measured, at 0, 0, so its size
	qr.SetLocation(x+labelWidth-padding-qrSize, y+(labelHeight-qrSize)/2.0)
	qr.DrawOn(page)

	// The text ends a quiet zone of four modules before the QR code
	textWidth := labelWidth - 2*padding - qrSize - 4*module
	baseline := y + 25.0
	drawFitted(page, bold, 10.0, order[1], x+padding, baseline, textWidth)
	drawFitted(page, regular, 9.0, order[2], x+padding, baseline+13.0, textWidth)
	drawFitted(page, regular, 9.0, order[3], x+padding, baseline+25.0, textWidth)
}

// drawFitted draws a line of text in the size, or smaller, down to 7 points,
// when it is wider than the room it has.
func drawFitted(page *pdfjet.Page, font *pdfjet.Font, size float32, text string, x, y, width float32) {
	line := pdfjet.NewTextLine(font, text)
	line.SetFontSize(size)
	for line.GetWidth() > width && size > 7.0 {
		size -= 0.5
		line.SetFontSize(size)
	}
	line.SetLocation(x, y)
	line.DrawOn(page)
}

func main() {
	time0 := time.Now().UnixMilli()
	Example85()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_85 => %4d ms\n", time1-time0)
}
