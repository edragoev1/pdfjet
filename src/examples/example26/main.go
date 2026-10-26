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

// The countries named on the chart, by their codes
var named = map[string]bool{"BRA": true, "CAN": true, "IND": true, "JPN": true, "NER": true, "NGA": true, "USA": true}

// Example26 draws a SimpleScatterChart of the life expectancy and the
// children per woman of 217 countries and economies, from the World Bank's
// data of 2023: a gray dot for each, seven of them named, and the trend line,
// with a table of the named ones under the chart.
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

	// Country|Code|Life expectancy|Children per woman, after a header line
	lines := content.LinesOfTextFile("data/life-expectancy-and-fertility-2023.txt")
	total := len(lines) - 1

	page := pdfjet.NewPage(pdf, letter.Portrait())

	title := pdfjet.NewTextLine(f1, "Life Expectancy and Children per Woman")
	title.SetStructureType(structelem.H1).SetFontSize(20.0)
	title.SetLocation(50.0, 70.0)
	title.DrawOn(page)
	intro := []*pdfjet.Paragraph{pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f2,
		"Where women have fewer children, people live longer: the "+strconv.Itoa(total)+
			" countries and economies of the World Bank's data of 2023, each a gray dot, "+
			"seven of them named, and the trend line through them, with the named ones in the table "+
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
	for _, line := range lines[1:] {
		field := strings.Split(line, "|")
		life, _ := strconv.ParseFloat(field[2], 32)
		children, _ := strconv.ParseFloat(field[3], 32)
		if named[field[1]] {
			chart.AddNamed(field[0], float32(children), float32(life)) // A navy dot, named
			data = append(data, row(f2, false, field[0], fmt.Sprintf("%.2f", float32(children)),
				fmt.Sprintf("%.1f years", float32(life))))
		} else {
			chart.Add(float32(children), float32(life)) // A gray dot
		}
	}
	chart.SetSize(512.0, 340.0)
	chart.SetLocation(50.0, y)
	y = chart.DrawOn(page)[1] + 30.0

	// The countries named on the chart
	heading := pdfjet.NewTextLine(f1, "The countries named on the chart")
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
