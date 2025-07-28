package main

import (
	"bufio"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"github.com/edragoev1/pdfjet/src/shape"
)

// Example34 -- TODO:
func Example34() {
	pdf := pdfjet.NewPDFFile("Example_34.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBoldOblique())

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	f3.SetSize(7.0)

	table := pdfjet.NewTable()
	tableData := getData(
		"data/world-communications.txt", "|", pdfjet.TableWith2HeaderRows, f1, f2)

	uri := "https://en.wikipedia.org/wiki/India"
	p1 := pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Circle)
	p1.SetRadius(2.0)
	p1.SetColor(color.DarkOliveGreen)
	p1.SetFillShape(true)
	p1.SetAlignment(align.Right)
	p1.SetURIAction(&uri)
	tableData[4][3].SetPoint(p1)

	uri = "https://en.wikipedia.org/wiki/European_Union"
	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Diamond)
	p1.SetRadius(2.5)
	p1.SetColor(color.Blue)
	p1.SetFillShape(true)
	p1.SetAlignment(align.Right)
	p1.SetURIAction(&uri)
	tableData[5][3].SetPoint(p1)

	uri = "https://en.wikipedia.org/wiki/United_States"
	p1 = pdfjet.NewPoint(0.0, 0.0)
	p1.SetShape(shape.Star)
	p1.SetRadius(3.0)
	p1.SetColor(color.Red)
	p1.SetFillShape(true)
	p1.SetAlignment(align.Right)
	p1.SetURIAction(&uri)
	tableData[6][3].SetPoint(p1)

	table.SetData(tableData, pdfjet.TableWith2HeaderRows)
	table.SetCellBordersWidth(0.0)
	table.SetLocation(70.0, 30.0)
	table.SetTextColorInRow(6, color.Blue)
	table.SetTextColorInRow(39, color.Red)
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
	defer f.Close()

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
