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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/shape"
)

// Example23 draws a table of PDF/A-1a, tagged, over four pages: the life
// expectancy and the children per woman of 217 countries and economies in
// 2023, from the World Bank's World Development Indicators, with a marker on
// three of the rows, each a link, and the longest and the shortest life in
// color.
func Example23() {
	pdf, err := pdfjet.NewPDFFile("Example_23.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_A_1A)
	pdf.SetTitle("Life Expectancy and Children per Woman, 2023")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.BoldItalic)

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	f3.SetSize(7.0)

	table := pdfjet.NewTable()
	// Country|Code|Life expectancy|Children per woman, after a header line
	tableData := getData(
		"resources/data/life-expectancy-and-fertility-2023.txt", "|", 1, f1, f2)

	// A marker on three rows, found by their countries, each a link
	p1 := pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Circle)
	p1.SetRadius(2.0)
	p1.SetStrokeColor(color.DarkOliveGreen)
	p1.SetURIAction("https://data.worldbank.org/country/india")
	tableData[rowOf(tableData, "India")][3].SetMarker(p1, alignment.Right)

	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Diamond)
	p1.SetRadius(2.5)
	p1.SetStrokeColor(color.Blue)
	p1.SetURIAction("https://data.worldbank.org/country/japan")
	tableData[rowOf(tableData, "Japan")][3].SetMarker(p1, alignment.Right)

	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Star)
	p1.SetRadius(3.0)
	p1.SetStrokeColor(color.Red)
	p1.SetURIAction("https://data.worldbank.org/country/united-states")
	tableData[rowOf(tableData, "United States")][3].SetMarker(p1, alignment.Right)

	// The longest life in blue, the shortest in red, and Canada in bold italic
	longest := 1
	shortest := 1
	for i := 1; i < len(tableData); i++ {
		if life(tableData, i) > life(tableData, longest) {
			longest = i
		}
		if life(tableData, i) < life(tableData, shortest) {
			shortest = i
		}
	}

	table.SetTableData(tableData, 1)
	table.SetBottomMargin(26.0)
	// 400 points wide, in the middle of the page
	table.SetLocation(106.0, 30.0)
	table.SetTextColorInRow(longest, color.Blue)
	table.SetTextColorInRow(shortest, color.Firebrick)
	table.SetFontInRow(rowOf(tableData, "Canada"), f3)
	table.SetColumnWidth(0, 170.0)
	table.SetColumnWidth(1, 50.0)
	table.SetColumnWidth(2, 90.0)
	table.SetColumnWidth(3, 90.0)
	table.RightAlignNumbers()

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	for i := 0; i < len(pages); i++ {
		page := pages[i]
		textLine := pdfjet.NewTextLine(f2, "Source: World Bank, World Development Indicators, "+
			"SP.DYN.LE00.IN and SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0. "+
			"Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		page.AddFooter(textLine)
		pdf.AddPage(page)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

// rowOf returns the row of the country, in the first column.
func rowOf(tableData [][]*pdfjet.Cell, country string) int {
	for i, row := range tableData {
		if row[0].GetText() == country {
			return i
		}
	}
	log.Fatal(country + " is not in the table")
	return -1
}

// life returns the life expectancy of the row.
func life(tableData [][]*pdfjet.Cell, row int) float32 {
	value, err := strconv.ParseFloat(tableData[row][2].GetText(), 32)
	if err != nil {
		log.Fatal(err)
	}
	return float32(value)
}

func getData(fileName, delimiter string, numOfHeaderRows int, f1, f2 *pdfjet.Font) [][]*pdfjet.Cell {
	tableData := make([][]*pdfjet.Cell, 0)

	f, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer func(f *os.File) {
		if err := f.Close(); err != nil {
			log.Printf("error closing %s: %v", fileName, err)
		}
	}(f)

	scanner := bufio.NewScanner(f)
	currentRow := 0
	for scanner.Scan() {
		line := scanner.Text()
		row := make([]*pdfjet.Cell, 0)
		var cols []string
		if delimiter == "|" {
			cols = strings.Split(line, "|")
		} else if delimiter == "\t" {
			cols = strings.Split(line, "\t")
		} else {
			log.Fatal("Only pipes and tabs can be used as delimiters")
		}
		// The first five fields: not the two-letter code at the end
		for i := 0; i < min(len(cols), 5); i++ {
			text := strings.TrimSpace(cols[i])
			var cell *pdfjet.Cell
			if currentRow < numOfHeaderRows {
				cell = pdfjet.NewCell(f1, text)
			} else {
				cell = pdfjet.NewCell(f2, text)
			}
			cell.SetTopPadding(2.0)
			cell.SetBottomPadding(2.0)
			cell.SetLeftPadding(2.0)
			if i == 3 {
				cell.SetRightPadding(10.0)
			} else {
				cell.SetRightPadding(2.0)
			}
			row = append(row, cell)
		}
		tableData = append(tableData, row)
		currentRow++
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	appendMissingCells(tableData, f2)

	return tableData
}

func appendMissingCells(tableData [][]*pdfjet.Cell, font *pdfjet.Font) {
	firstRow := tableData[0]
	numOfColumns := len(firstRow)
	for i := 0; i < len(tableData); i++ {
		dataRow := tableData[i]
		dataRowColumns := len(dataRow)
		if dataRowColumns < numOfColumns {
			for j := 0; j < (numOfColumns - dataRowColumns); j++ {
				dataRow = append(dataRow, pdfjet.NewCell(font, ""))
			}
			tableData[i] = dataRow
			dataRow[dataRowColumns-1].SetColSpan((numOfColumns - dataRowColumns) + 1)
		}
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example23()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_23 => %4d ms\n", time1-time0)
}
