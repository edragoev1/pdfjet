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
	"github.com/edragoev1/pdfjet/v9/src/CascadiaMono"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSerif"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// product is a product: its code, name, description, the price in cents,
// the number of colors it comes in, and a picture, "" for none.
type product struct {
	code        string
	name        string
	description string
	price       int64
	colors      int
	picture     string
	alt         string
}

const studio = "Northwind Studio"
const title = "Catalog 2026"
const orderURL = "https://example.com/order"

// The pictures and the logo are SVG files, drawn as vectors, sharp at any size
const images = "resources/images/catalog/"

var products = []product{
	{"NB-A5-L", "Lined notebook, A5", "160 pages of lined paper, 100 g/m², " +
		"a navy linen cover.", 2400, 4, "notebook-lined.svg", "A navy notebook with a cream label"},
	{"NB-A5-D", "Dot grid notebook, A5", "160 pages with a 5 mm dot grid, " +
		"for notes and sketches.", 2400, 4, "notebook-dots.svg", "An open notebook of dotted pages"},
	{"MG-350-B", "Stoneware mug", "350 ml, thrown by hand; dishwasher safe.",
		3200, 4, "mug.svg", "A brown stoneware mug"},
	{"NB-A4-S", "Sketchbook, A4", "80 sheets of 160 g/m² paper for pencil, " +
		"ink and washes.", 3600, 2, "sketchbook.svg",
		"A sketchbook with a kraft cover and a spiral binding"},
	{"MG-250-E", "Espresso cups, two", "90 ml each, in a teal glaze, with their " +
		"saucers.", 3800, 3, "espresso-cups.svg", "Two small teal cups on saucers"},
	{"BW-180-S", "Serving bowl, 18 cm", "Wide and shallow, for salads and fruit; " +
		"oven safe.", 5800, 2, "serving-bowl.svg", "A wide sand-colored bowl with a teal rim"},
	{"NB-A6-P", "Pocket notebook, A6, three", "", 2800, 4, "", ""},
	{"GC-050", "Gift card", "", 5000, 0, "", ""},
}

// The colors the covers and the glazes come in, as on the swatches
var colorNames = []string{"Navy", "Teal", "Sand", "Clay"}
var colors = []int32{0x1d3557, 0x0c727d, 0xf4eee4, 0x6e4a2e}

const navy = 0x1d3557
const ink = 0x333333
const sand = 0xf4eee4
const clay = 0x6e4a2e
const ruleColor = 0xdde1e8

const left float32 = 40.0
const width float32 = 532.0 // Letter, 612 wide, less the margins
const gap float32 = 12.0
const perRow = 3
const column float32 = (width - (perRow-1)*gap) / perRow

// catalog holds the fonts the parts of the catalog are drawn in.
type catalog struct {
	sans        *pdfjet.Font
	sansBold    *pdfjet.Font
	serif       *pdfjet.Font
	serifItalic *pdfjet.Font
	mono        *pdfjet.Font
}

// Example79 draws a product catalog of two pages, on Letter paper: the logo
// and the title, a grid of products with a picture, a name, a description,
// the code and the price of each, the colors the products come in, the price
// list, how to order, and a QR code of the shop online.
//
// A catalog is a finished document that customers receive, not a form. The
// products are one list, and both the grid and the price list are made from
// it, so a price is changed in one place; the products without a picture are
// in the price list only. The grid places three products to a row, each in a
// column of the same width, and starts the next row under the tallest
// product of the row, as a longer description pushes it down. The codes and
// the prices are in Cascadia Mono, a monospace font, so that the characters
// of the codes line up from row to row of the price list.
//
// The pictures here are small drawings, SVG files in
// resources/images/catalog that SVGImage draws as vectors, sharp at any
// size, scaled to the width of a column. A real catalog has photos:
// pdfjet.NewImageFromFile(pdf, "photo.jpg") in place of SVGImage, with the
// same SetAltDescription. The logo is an SVG, as logos usually are.
//
// The document is PDF/UA-1: the title, the sections and the products are
// headings of three levels, every picture and the QR code is a figure that
// says what it shows, the price list is a table, and the running header of
// each page and the color swatches are artifacts. The layout is that of the
// product catalog template of PDFjet Forms. The studio and its products are
// made up.
func Example79() error {
	pdf, err := pdfjet.NewPDFFile("Example_79.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle(title)
	pdf.SetAuthor(studio)
	pdf.SetLanguage("en-CA")

	c := &catalog{}
	c.sans = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	c.sans.SetSize(10.0)
	c.sansBold = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	c.sansBold.SetSize(10.0) // The headings set their own size
	c.serif = pdfjet.NewFontFromFile(pdf, IBMPlexSerif.Regular)
	c.serif.SetSize(11.0)
	c.serifItalic = pdfjet.NewFontFromFile(pdf, IBMPlexSerif.Italic)
	c.serifItalic.SetSize(13.0)
	c.mono = pdfjet.NewFontFromFile(pdf, CascadiaMono.Regular)
	c.mono.SetSize(10.0)

	pages := make([]*pdfjet.Page, 0)

	// Page 1: the logo, the title and a line about the studio, then the
	// products with a picture, three to a row
	page := pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	logo, err := pdfjet.NewSVGImageFromFile(images + "northwind-logo.svg")
	if err != nil {
		return err
	}
	logo.SetAltDescription(studio)
	logo.SetLocation(left, 52.0)
	logo.DrawOn(page)
	x := left + width/4.0 + gap
	c.heading(page, title, structelem.H1, 26.0, x, 50.0)
	tagline := pdfjet.NewTextBlock(c.serifItalic,
		"Paper goods and ceramics, made by hand in our studio by the lake.")
	tagline.SetTextColor(ink)
	tagline.SetLocation(x, 86.0)
	tagline.SetWidth(left + width - x)
	y := tagline.DrawOn(page)[1] + 18.0
	rule(page, y)

	y = c.heading(page, "Notebooks and mugs", structelem.H2, 16.0, left, y+20.0)
	y = c.paragraph(page, "Every notebook is sewn by hand and opens flat, on paper heavy enough for "+
		"ink. Our mugs are thrown on the wheel and glazed in small batches, so no two are "+
		"quite alike. Each item has its code and its price in Canadian dollars.", left, y, width)

	col := 0
	rowTop := y + 18.0
	rowEnd := rowTop
	for _, product := range products {
		if product.picture == "" {
			continue
		}
		end, err := c.drawProduct(page, product, left+float32(float32(col)*(column+gap)), rowTop)
		if err != nil {
			return err
		}
		rowEnd = max(rowEnd, end)
		col++
		if col == perRow {
			col = 0
			rowTop = rowEnd + 22.0
		}
	}

	// Page 2: the colors, the price list of all the products, and how to order
	page = pdfjet.NewPageDetached(pdf, letter.Portrait())
	pages = append(pages, page)
	y = c.heading(page, "Colors", structelem.H2, 16.0, left, 52.0)
	y = c.paragraph(page, "Covers and glazes come in four colors; the price list says how many each "+
		"item has. Name the color with the code when you order.", left, y, width)
	y = c.drawSwatches(page, y+14.0)

	y = c.heading(page, "Price list", structelem.H2, 16.0, left, y+30.0)
	y = c.drawPriceList(page, y+4.0)

	// How to order on the left, the QR code of the shop online on the right
	qrX := left + 0.75*width + gap
	top := y + 30.0
	end := c.heading(page, "Ordering", structelem.H3, 12.0, left, top)
	c.paragraph(page, "Prices in Canadian dollars, taxes extra. Orders over $150 ship free within "+
		"Canada. Write to orders@example.com with the codes and colors you would like, and "+
		"we send an invoice within a day.", left, end, 0.75*width-gap)
	end = c.heading(page, "Order online", structelem.H3, 12.0, qrX, top)
	qr := qrcode.NewQRCode(orderURL, errorcorrectionlevel.M)
	qr.SetModuleLength(2.5)
	qr.SetAltDescription("QR code of " + orderURL)
	qr.SetLocation(qrX, end+4.0)
	qr.DrawOn(page)

	// The running header of each page, now that their number is known
	for i := 0; i < len(pages); i++ {
		c.drawHeader(pages[i], i+1, len(pages))
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
	return nil
}

// drawProduct draws a product in its column, its top at y: the picture, the
// name, a heading under the section, the description, and the code and the
// price. Returns where it ended.
func (c *catalog) drawProduct(page *pdfjet.Page, product product, x, y float32) (float32, error) {
	// The drawings are 300 wide, scaled to the width of the column
	picture, err := pdfjet.NewSVGImageFromFile(images + product.picture)
	if err != nil {
		return 0, err
	}
	picture.ScaleBy(column / picture.GetWidth())
	picture.SetAltDescription(product.alt)
	picture.SetLocation(x, y)
	y = picture.DrawOn(page)[1]
	y = c.heading(page, product.name, structelem.H3, 12.0, x, y+14.0)
	y = c.paragraph(page, product.description, x, y, column)
	// The code padded to ten characters, so the prices line up from
	// product to product in the monospace font
	text := fmt.Sprintf("%-10s $%s", product.code, money(product.price))
	price := pdfjet.NewTextLine(c.mono, text)
	price.SetTextColor(ink)
	price.SetLocation(x, y+18.0)
	price.DrawOn(page)
	return y + 18.0, nil
}

// drawSwatches draws a swatch of each color with its name under it, in four
// columns, its top at y, and returns where the names end. The swatches are
// decoration; the names say the colors.
func (c *catalog) drawSwatches(page *pdfjet.Page, y float32) float32 {
	w := (width - 3*gap) / 4.0
	for i := 0; i < len(colors); i++ {
		x := left + float32(float32(i)*(w+gap))
		swatch := pdfjet.NewRect(x, y, w, 40.0)
		swatch.SetCornerRadius(6.0)
		swatch.SetFillColor(colors[i])
		// The sand swatch is near the color of the paper, so it has a border
		if colors[i] == sand {
			swatch.SetBorderColor(clay)
		} else {
			swatch.SetBorderColor(colors[i])
		}
		swatch.DrawOn(page)
		name := pdfjet.NewTextLine(c.sans, colorNames[i])
		name.SetLocation(x+(w-name.GetWidth())/2.0, y+58.0)
		name.DrawOn(page)
	}
	return y + 58.0
}

// drawPriceList draws the price list of all the products, its top at y, and
// returns where it ended: the header in white on navy, the rows on sand, no
// borders, the codes and the prices in the monospace font.
func (c *catalog) drawPriceList(page *pdfjet.Page, y float32) float32 {
	rows := make([][]*pdfjet.Cell, 0)
	rows = append(rows, row(c.sansBold, "Code", "Item", "Colors", "Price"))
	for _, product := range products {
		count := ""
		if product.colors != 0 {
			count = strconv.Itoa(product.colors)
		}
		rows = append(rows, row(c.mono, product.code, product.name, count, "$"+money(product.price)))
	}
	for i := 1; i < len(rows); i++ {
		for _, cell := range rows[i] {
			cell.SetBackgroundColor(sand)
		}
	}

	table := pdfjet.NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(c.sansBold, color.White, navy)
	table.SetCellBorders(false)
	table.SetCellPadding(4.0, 3.0)
	table.SetWidth(width)
	table.SetColumnWidthsInPercent(20.0, 50.0, 15.0, 15.0)
	table.SetTextAlignmentInColumn(2, alignment.Center)
	table.SetTextAlignmentInColumn(3, alignment.Right)
	table.SetLocation(left, y)
	return table.DrawOn(page)[1]
}

// drawHeader draws the studio and the catalog on the left and the page
// number on the right, above the content, in small gray type. It repeats on
// every page, so it is an artifact.
func (c *catalog) drawHeader(page *pdfjet.Page, number, count int) {
	leftLine := pdfjet.NewTextLine(c.sans, studio+" · "+title)
	leftLine.SetFontSize(9.0)
	leftLine.SetTextColor(color.DimGray)
	leftLine.SetLocation(left, 26.0)
	rightLine := pdfjet.NewTextLine(c.sans, "Page "+strconv.Itoa(number)+" of "+strconv.Itoa(count))
	rightLine.SetFontSize(9.0)
	rightLine.SetTextColor(color.DimGray)
	rightLine.SetLocation(left+width-rightLine.GetWidth(), 26.0)
	page.AddArtifactBMC()
	leftLine.DrawOn(page)
	rightLine.DrawOn(page)
	page.AddEMC()
}

// heading draws a heading in navy, its top at y, and returns where the text
// under it starts.
func (c *catalog) heading(page *pdfjet.Page, text string, level structelem.StructElem, size, x, y float32) float32 {
	heading := pdfjet.NewTextLine(c.sansBold, text)
	heading.SetStructureType(level)
	heading.SetFontSize(size)
	heading.SetTextColor(navy)
	heading.SetLocation(x, y+heading.GetAscent())
	heading.DrawOn(page)
	return y + heading.GetAscent() + 10.0
}

// paragraph draws a paragraph in the serif font, its top at y, of the width,
// and returns where it ended.
func (c *catalog) paragraph(page *pdfjet.Page, text string, x, y, width float32) float32 {
	block := pdfjet.NewTextBlock(c.serif, text)
	block.SetTextColor(ink)
	block.SetLineSpacing(1.15)
	block.SetLocation(x, y)
	block.SetWidth(width)
	return block.DrawOn(page)[1]
}

// rule draws a thin line across the page, decoration only, so an artifact
// that a screen reader skips.
func rule(page *pdfjet.Page, y float32) {
	line := pdfjet.NewLine(left, y, left+width, y)
	line.SetStrokeColor(ruleColor)
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
	err := Example79()
	if err != nil {
		log.Fatal(err)
	}
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_79 => %4d ms\n", time1-time0)
}
