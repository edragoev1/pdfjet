package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/imagetype"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example08 draws a table.
func Example08() {
	file, err := os.Create("Example_08.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()

	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBoldOblique())

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	f3.SetSize(7.0)

	/*
		f1 := new Font(pdf,
				getClass().getResourceAsStream("fonts/OpenSans/OpenSans-Bold.ttf.stream"),
				Font.STREAM)
		f2 := new Font(pdf,
				getClass().getResourceAsStream("fonts/OpenSans/OpenSans-Regular.ttf.stream"),
				Font.STREAM)
		f3 := new Font(pdf,
				getClass().getResourceAsStream("fonts/OpenSans/OpenSans-BoldItalic.ttf.stream"),
				Font.STREAM)
	*/

	f, err := os.Open("images/fruit.jpg")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()

	reader := bufio.NewReader(f)
	image1 := pdfjet.NewImage(pdf, reader, imagetype.JPG)
	image1.ScaleBy(0.20)

	barCode := pdfjet.NewBarCode(pdfjet.CODE128, "Hello, World!")
	barCode.SetModuleLength(0.75)
	// Uncomment the line below if you want to print the text underneath the barcode.
	// barCode.SetFont(f1);

	table := pdfjet.NewTable()
	tableData := getData(
		"data/world-communications.txt", "|", pdfjet.TableWith2HeaderRows, f1, f2, image1, barCode)
	table.SetData(tableData, pdfjet.TableWith2HeaderRows)
	table.RemoveLineBetweenRows(0, 1)
	table.SetLocation(100.0, 0.0)
	table.SetRightMargin(20.0)
	table.SetBottomMargin(0.0)
	table.SetCellBordersWidth(0.0)
	table.SetTextColorInRow(12, color.Blue)
	table.SetTextColorInRow(13, color.Red)
	table.GetCellAt(13, 0).GetTextBlock().SetURIAction("http://pdfjet.com")
	table.SetFontInRow(14, f3)
	table.GetCellAt(21, 0).SetColSpan(6)
	table.GetCellAt(21, 6).SetColSpan(2)

	// Set the column widths manually:
	// table.SetColumnWidth(0, 70f);
	// table.SetColumnWidth(1, 50f);
	// table.SetColumnWidth(2, 70f);
	// table.SetColumnWidth(3, 70f);
	// table.SetColumnWidth(4, 70f);
	// table.SetColumnWidth(5, 70f);
	// table.SetColumnWidth(6, 50f);
	// table.SetColumnWidth(7, 50f);

	// Auto adjust the column widths to be just wide enough to fit the text without truncation.
	// Columns with colspan > 1 will not be adjusted.
	// table.AutoAdjustColumnWidths();

	// Auto adjust the column widths in a way that allows the table to fit perfectly on the page.
	// Columns with colspan > 1 will not be adjusted.
	table.FitToPage(letter.Portrait)

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait)
	for i := 0; i < len(pages); i++ {
		page := pages[i]
		// page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + len(pages)))
		pdf.AddPage(page)
	}
	pdf.Complete()
}

func getTextData(fileName, delimiter string) [][]string {
	tableTextData := make([][]string, 0)

	f, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()

	scanner := bufio.NewScanner(f)
	for scanner.Scan() {
		line := scanner.Text()
		var cols []string
		if delimiter == "|" {
			cols = strings.Split(line, "|")
		} else if delimiter == "\t" {
			cols = strings.Split(line, "\t")
		} else {
			log.Fatal("Only pipes and tabs can be used as delimiters")
		}
		tableTextData = append(tableTextData, cols)
	}

	return tableTextData
}

func getData(
	fileName string,
	delimiter string,
	numOfHeaderRows int,
	f1 *pdfjet.Font,
	f2 *pdfjet.Font,
	image *pdfjet.Image,
	barCode *pdfjet.BarCode) [][]*pdfjet.Cell {
	tableData := make([][]*pdfjet.Cell, 0)

	tableTextData := getTextData(fileName, delimiter)
	currentRow := 0
	for _, rowData := range tableTextData {
		row := make([]*pdfjet.Cell, 0)
		for i := 0; i < len(rowData); i++ {
			text := strings.TrimSpace(rowData[i])
			var cell *pdfjet.Cell
			if currentRow < numOfHeaderRows {
				cell = pdfjet.NewCell(f1, text)
			} else {
				cell = pdfjet.NewCell(f2, "")
				if i == 0 && currentRow == 5 {
					cell.SetImage(image)
				}
				if i == 0 && currentRow == 6 {
					cell.SetBarcode(barCode)
					cell.SetTextAlignment(align.Center)
					cell.SetColSpan(8)
				} else {
					textBlock := pdfjet.NewTextBlock(f2, text)
					if i == 0 {
						textBlock.SetTextAlignment(align.Left)
					} else {
						textBlock.SetTextAlignment(align.Right)
					}
					cell.SetTextBlock(textBlock)
				}
			}
			row = append(row, cell)
		}
		tableData = append(tableData, row)
		currentRow++
	}

	return tableData
}

func main() {
	start := time.Now()
	Example08()
	elapsed := time.Since(start)
	fmt.Printf("Example_08 => %dµs\n", elapsed.Microseconds())
}
