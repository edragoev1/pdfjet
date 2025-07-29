package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/border"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example38 draws the Canadian flag using a Path object that contains both lines
// and curve segments. Every curve segment must have exactly 2 control points.
func Example38() {
	pdf := pdfjet.NewPDFFile("Example_38.pdf")
	font := pdfjet.NewCoreFont(pdf, corefont.Courier())
	page := pdfjet.NewPage(pdf, letter.Landscape)

	page.SetBrushColor(color.Black)
	page.FillRect(100, 100, 100, 100)

	table := pdfjet.NewTable()
	table.SetData(createTableData(font), pdfjet.TableWith0HeaderRows)
	table.SetLocation(50.0, 50.0)
	table.SetBottomMargin(10.0)
	table.DrawOn(page)

	pdf.Complete()
}

/**
 * This will return a 10x10 matrix. The HTML-Like table will be like:
 * <table border="solid">
 * <tr>
 * <td colspan="2" rowspan="2">2x2</td>
 * <td colspan="2">2x1</td>
 * <td colspan="2">2x1</td>
 * <td colspan="2">2x1</td>
 * <td colspan="2">2x1</td>
 * </tr>
 * <tr>
 * <td colspan="2" rowspan="2">2x2</td>
 * <td>1x1</td>
 * <td colspan="5">5x1</td>
 * </tr>
 * <tr>
 * <td rowspan="2">1x2</td>
 * <td>1x1</td>
 * <td colspan="2" rowspan="2">2x2</td>
 * <td rowspan="2">1x2</td>
 * <td colspan="3">3x1</td>
 * </tr>
 * <tr>
 * <td>1x1</td>
 * <td rowspan="3">1x3</td>
 * <td>1x1</td>
 * <td colspan="2">2x1</td>
 * <td rowspan="2">1x2</td>
 * </tr>
 * <tr>
 * <td rowspan="2">1x2</td>
 * <td>1x1</td>
 * <td colspan="2">2x1</td>
 * <td colspan="4" rowspan="4">4x4</td>
 * </tr>
 * <tr>
 * <td>1x1</td>
 * <td rowspan="3">1x3</td>
 * <td rowspan="3">1x3</td>
 * <td rowspan="3">1x3</td>
 * </tr>
 * <tr>
 * <td rowspan="2">1x2</td>
 * <td>1x1</td>
 * <td rowspan="4">1x4</td>
 * </tr>
 * <tr>
 * <td>1x1</td>
 * </tr>
 * <tr>
 * <td rowspan="2">1x2</td>
 * <td>1x1</td>
 * <td colspan="2">2x1</td>
 * <td colspan="2" rowspan="2">2x2</td>
 * <td rowspan="2">1x2</td>
 * <td>1x1</td>
 * <td>1x1</td>
 * </tr>
 * <tr>
 * <td>1x1</td>
 * <td>1x1</td>
 * <td>1x1</td>
 * <td>1x1</td>
 * <td>1x1</td>
 * </tr>
 * </table>
 *
 * @return
 * @throws Exception
 */
func createTableData(font *pdfjet.Font) [][]*pdfjet.Cell {
	rows := make([][]*pdfjet.Cell, 0)

	for i := 0; i < 10; i++ {
		row := make([]*pdfjet.Cell, 0)
		if i == 0 {
			row = append(row, getCell(font, 2, "2x2", true, false))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, false))
		} else if i == 1 {
			row = append(row, getCell(font, 2, "^", false, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 2, "2x2", true, false))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 5, "5x1", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "", true, true))
		} else if i == 2 {
			row = append(row, getCell(font, 1, "1x2", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 2, "^", false, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 2, "2x2", true, false))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 3, "3x1", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
		} else if i == 3 {
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "1x3", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 2, "^", false, true))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, false))
			row = append(row, getCell(font, 1, "1x2", true, false))
		} else if i == 4 {
			row = append(row, getCell(font, 1, "1x2", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "^", false, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 4, "4x4", true, false))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "^", false, true))
		} else if i == 5 {
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x3", true, false))
			row = append(row, getCell(font, 1, "1x3", true, false))
			row = append(row, getCell(font, 4, "^", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "1x3", true, false))
		} else if i == 6 {
			row = append(row, getCell(font, 1, "1x2", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "1x4", true, false))
			row = append(row, getCell(font, 1, "^", false, false))
			row = append(row, getCell(font, 1, "^", false, false))
			row = append(row, getCell(font, 4, "^", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "", false, false))
			row = append(row, getCell(font, 1, "^", false, false))
		} else if i == 7 {
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "^", false, false))
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 4, "^", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "^", false, true))
		} else if i == 8 {
			row = append(row, getCell(font, 1, "1x2", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "^", false, false))
			row = append(row, getCell(font, 2, "2x1", true, true))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 2, "2x2", true, false))
			row = append(row, getCell(font, 1, "", true, true))
			row = append(row, getCell(font, 1, "1x2", true, false))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
		} else if i == 9 {
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 2, "^", false, true))
			row = append(row, getCell(font, 1, "", false, true))
			row = append(row, getCell(font, 1, "^", false, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
			row = append(row, getCell(font, 1, "1x1", true, true))
		}
		rows = append(rows, row)
	}

	return rows
}

func getCell(
	font *pdfjet.Font,
	colSpan int,
	text string,
	topBorder, bottomBorder bool) *pdfjet.Cell {
	cell := pdfjet.NewCell(font, text)
	cell.SetColSpan(colSpan)
	cell.SetWidth(50.0)
	cell.SetBorder(border.Top, topBorder)
	cell.SetBorder(border.Bottom, bottomBorder)
	cell.SetTextAlignment(align.Center)
	cell.SetBgColor(color.LightBlue)
	cell.SetLineWidth(0.5)
	return cell
}

func main() {
	start := time.Now()
	Example38()
	pdfjet.PrintDuration("Example_38", time.Since(start))
}
