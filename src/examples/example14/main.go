package main

import (
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/alignment"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
)

// Example14 draws a table and sets the borders of the individual cells.
func Example14() {
	pdf := pdfjet.NewPDFFile("Example_14.pdf")

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f1.SetSize(7.0)

	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f2.SetSize(7.0)

	page := pdfjet.NewPage(pdf, a4.Portrait)

	table := pdfjet.NewTable()
	tableData := make([][]*pdfjet.Cell, 0)
	for i := 0; i < 5; i++ {
		row := make([]*pdfjet.Cell, 0)
		for j := 0; j < 5; j++ {
			var cell *pdfjet.Cell
			if i == 0 {
				cell = pdfjet.NewCell(f1, "")
			} else {
				cell = pdfjet.NewCell(f2, "")
			}
			cell.SetTopBorder(false)
			cell.SetBottomBorder(false)
			cell.SetLeftBorder(false)
			cell.SetRightBorder(false)

			cell.SetTopPadding(10.0)
			cell.SetBottomPadding(10.0)
			cell.SetLeftPadding(10.0)
			cell.SetRightPadding(10.0)

			cell.SetText("Hello " + strconv.Itoa(i) + " " + strconv.Itoa(j))
			if i == 0 {
				cell.SetTopBorder(true)
				cell.SetUnderline(true)
				cell.SetUnderline(false)
			}
			if i == 4 {
				cell.SetBottomBorder(true)
			}
			if j == 0 {
				cell.SetLeftBorder(true)
			}
			if j == 4 {
				cell.SetRightBorder(true)
			}

			if i == 2 && j == 2 {
				cell.SetTopBorder(true)
				cell.SetBottomBorder(true)
				cell.SetLeftBorder(true)
				cell.SetRightBorder(true)

				cell.SetColSpan(3)
				cell.SetBackgroundColor(color.DarkSeaGreen)
				cell.SetLineWidth(1.0)
				cell.SetTextAlignment(alignment.Right)
			}

			row = append(row, cell)
		}
		tableData = append(tableData, row)
	}
	table.SetData(tableData, pdfjet.TableWith0HeaderRows)
	table.SetCellBordersWidth(0.2)
	table.SetLocation(70.0, 30.0)
	table.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example14()
	pdfjet.PrintDuration("Example_14", time.Since(start))
}
