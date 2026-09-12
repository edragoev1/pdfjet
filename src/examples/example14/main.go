package main

import (
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/a4"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
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
			cell.SetBorders(false)

			cell.SetTopPadding(10.0)
			cell.SetBottomPadding(10.0)
			cell.SetLeftPadding(10.0)
			cell.SetRightPadding(10.0)

			cell.SetText("Hello " + strconv.Itoa(i) + " " + strconv.Itoa(j))
			if i == 0 {
				cell.SetBorder(border.Top, true)
				cell.SetUnderline(true)
				cell.SetUnderline(false)
			}
			if i == 4 {
				cell.SetBorder(border.Bottom, true)
			}
			if j == 0 {
				cell.SetBorder(border.Left, true)
			}
			if j == 4 {
				cell.SetBorder(border.Right, true)
			}

			if i == 2 && j == 2 {
				cell.SetBorder(border.Top, true)
				cell.SetBorder(border.Bottom, true)
				cell.SetBorder(border.Left, true)
				cell.SetBorder(border.Right, true)

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
	time0 := time.Now().UnixMilli()
	Example14()
	time1 := time.Now().UnixMilli()
	pdfjet.PrintDuration("Example_14", time0, time1)
}
