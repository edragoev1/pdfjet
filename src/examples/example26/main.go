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
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The seven with the fewest children, by their codes, the fewest first
var fewest = []string{"MAC", "KOR", "HKG", "PRI", "SGP", "UKR", "CHN"}

// The World Bank's regions, a series each, in the order of its list
var regions = []string{
	"East Asia & Pacific",
	"Europe & Central Asia",
	"Latin America & Caribbean",
	"Middle East, North Africa, Afghanistan & Pakistan",
	"North America",
	"South Asia",
	"Sub-Saharan Africa",
}

// Example26 draws a SimpleScatterChart of the life expectancy and the
// children per woman of 217 countries and economies, from the World Bank's
// data of 2023: a series for each of the World Bank's seven regions, in its
// color and shape, each point a link to its row
// of a table on pdfjet.com, with its life expectancy and children per woman,
// described by its figures, and the trend line, with a table of the seven with the fewest children
// under the chart.
func Example26() {
	pdf, err := pdfjet.NewPDFFile("Example_26.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Life Expectancy and Children per Woman")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f1.SetSize(12.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(9.0)

	// Country|Code|Life expectancy|Children per woman|Region|ISO2, after a header line
	lines := content.LinesOfTextFile("resources/data/life-expectancy-and-fertility-2023.txt")
	total := len(lines) - 1

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f1, "Life Expectancy and Children per Woman")
	title.SetStructureType(structelem.H1).SetFontSize(20.0)
	title.SetLocation(50.0, 70.0)
	title.DrawOn(page)
	intro := []*pdfjet.Paragraph{pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f2,
		"Where women have fewer children, people live longer: the "+strconv.Itoa(total)+
			" countries and economies of the World Bank's data of 2023, in a color and "+
			"a shape for each of its seven regions, each point a link to its row of a table on pdfjet.com, with its life expectancy and children per woman, and the trend line "+
			"through them, with the seven with the fewest children in the table "+
			"under the chart.").SetFontSize(10.0))}
	frame := pdfjet.NewTextFrameFromParagraphs(intro).SetWidth(512.0)
	frame.SetLocation(50.0, 84.0)
	frame.DrawOn(page)
	y := frame.GetTextBottom() + 18.0

	chart := pdfjet.NewSimpleScatterChart(f1, f2).
		SetTitle("Life expectancy and children per woman").
		SetSubtitle(strconv.Itoa(total) + " countries and economies, 2023").
		SetXAxisTitle("Children per woman").
		SetYAxisTitle("Life expectancy at birth, years").
		SetTrendLine(true)
	data := [][]*pdfjet.Cell{row(f1, true, "Country", "Children per woman", "Life expectancy")}
	for _, region := range regions {
		chart.AddSeries(region)
		for _, line := range lines[1:] {
			field := strings.Split(line, "|")
			if field[4] != region {
				continue
			}
			life, _ := strconv.ParseFloat(field[2], 32)
			children, _ := strconv.ParseFloat(field[3], 32)
			uri := "https://pdfjet.com/countries.html#" + field[5]
			// The outline of its shape, a link described by its figures
			name := field[0] + ": " + decimals(field[2], 1) + " years, " + decimals(field[3], 2) + " children per woman"
			chart.AddLinkedDot(name, float32(children), float32(life), uri)
		}
	}
	for _, code := range fewest {
		for _, line := range lines[1:] {
			field := strings.Split(line, "|")
			if field[1] != code {
				continue
			}
			// The figures as the file writes them, two decimals and one,
			// not rounded again, so that every port writes them alike
			data = append(data, row(f2, false, field[0], decimals(field[3], 2), decimals(field[2], 1)+" years"))
		}
	}
	chart.SetSize(512.0, 340.0)
	chart.SetLocation(50.0, y)
	y = chart.DrawOn(page)[1] + 30.0

	// The seven with the fewest children
	heading := pdfjet.NewTextLine(f1, "The seven with the fewest children")
	heading.SetStructureType(structelem.H2).SetFontSize(14.0)
	heading.SetLocation(50.0, y)
	heading.DrawOn(page)
	table := pdfjet.NewTable()
	table.SetTableData(data, 1)
	table.SetColumnWidth(0, 232.0)
	table.SetColumnWidth(1, 140.0)
	table.SetColumnWidth(2, 140.0)
	table.SetLocation(50.0, y+10.0)
	y = table.DrawOn(page)[1] + 16.0

	source := pdfjet.NewTextLine(f2, "Source: World Bank, World Development Indicators, SP.DYN.LE00.IN and "+
		"SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0.")
	source.SetFontSize(8.0).SetTextColor(0x5F6873)
	source.SetLocation(50.0, y)
	source.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// decimals returns the number of the text with the decimals, zeros added:
// 1.5 is 1.50.
func decimals(number string, count int) string {
	point := strings.IndexByte(number, '.')
	var sb strings.Builder
	sb.WriteString(number)
	have := 0
	if point < 0 {
		sb.WriteString(".")
	} else {
		have = len(number) - point - 1
	}
	for i := have; i < count; i++ {
		sb.WriteByte('0')
	}
	return sb.String()
}

// row returns a row of cells of the font at 9 points, one for each text, in
// the look of the chart: a light band behind the header row, a thin gray line
// under each row, and the numbers, the cells after the first, aligned right.
func row(font *pdfjet.Font, header bool, texts ...string) []*pdfjet.Cell {
	cells := make([]*pdfjet.Cell, 0)
	for i, text := range texts {
		cell := pdfjet.NewCell(font, text).SetFontSize(9.0)
		cell.SetTopPadding(5.0).SetBottomPadding(5.0).SetLeftPadding(6.0).SetRightPadding(6.0)
		cell.SetBorders(false).SetBorder(border.Bottom, true)
		cell.SetBorderWidth(0.5).SetBorderColor(0xD5D9DE)
		if header {
			cell.SetBackgroundColor(0xF1F3F6)
		}
		if i > 0 {
			cell.SetTextAlignment(alignment.Right)
		}
		cells = append(cells, cell)
	}
	return cells
}

func main() {
	time0 := time.Now().UnixMilli()
	Example26()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_26 => %4d ms\n", time1-time0)
}
