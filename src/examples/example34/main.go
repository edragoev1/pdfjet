package main

import (
	"bufio"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example34 -- TODO:
func Example34() {
	pdf := pdfjet.NewPDFFile("Example_34.pdf")
	pdf.SetCompliance(compliance.PDF_A_1B)

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(7.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(7.0)

	f3 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.BoldItalic)
	f3.SetSize(7.0)

	table := pdfjet.NewTable()
	tableData := getData(
		"data/world-communications.txt", "|", pdfjet.TableWith2HeaderRows, f1, f2)

	uri := "https://en.wikipedia.org/wiki/India"
	p1 := pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Circle)
	p1.SetRadius(2.0)
	p1.SetFillColor(color.DarkOliveGreen)
	p1.SetFillShape(true)
	p1.SetAlignment(alignment.Right)
	p1.SetURIAction(uri)
	tableData[4][3].SetPoint(p1)

	uri = "https://en.wikipedia.org/wiki/European_Union"
	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Diamond)
	p1.SetRadius(2.5)
	p1.SetFillColor(color.Blue)
	p1.SetFillShape(true)
	p1.SetAlignment(alignment.Right)
	p1.SetURIAction(uri)
	tableData[5][3].SetPoint(p1)

	uri = "https://en.wikipedia.org/wiki/United_States"
	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Star)
	p1.SetRadius(3.0)
	p1.SetFillColor(color.Red)
	p1.SetFillShape(true)
	p1.SetAlignment(alignment.Right)
	p1.SetURIAction(uri)
	tableData[6][3].SetPoint(p1)

	table.SetData(tableData, pdfjet.TableWith2HeaderRows)
	table.SetCellBordersWidth(0.0)
	table.SetLocation(70.0, 30.0)
	//table.SetTextColorInRow(6, color.Blue)	// TODO
	//table.SetTextColorInRow(39, color.Red)
	table.SetFontInRow(26, f3)
	table.RemoveLineBetweenRows(0, 1)
	table.SetColumnWidths()
	table.SetColumnWidth(0, 50.0)
	table.RightAlignNumbers()

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait)
	for i := 0; i < len(pages); i++ {
		page := pages[i]
		textLine := pdfjet.NewTextLine(f1, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages)))
		page.AddHeader(textLine)
		page.AddFooter(textLine)
		pdf.AddPage(page)
	}

	pdf.Complete()
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
		for i := 0; i < len(cols); i++ {
			text := cols[i] // TODO.trim()
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
			dataRow[dataRowColumns-1].SetColSpan((numOfColumns - dataRowColumns) + 1)
		}
	}
}

func main() {
	start := time.Now()
	Example34()
	pdfjet.PrintDuration("Example_34", time.Since(start))
}
