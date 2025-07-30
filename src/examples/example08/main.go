package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example08 draws a table.
func Example08() {
	pdf := pdfjet.NewPDFFile("Example_08.pdf")

	// f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	// f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	// f3 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBoldOblique())

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-BoldItalic.ttf.stream")

	f1.SetSize(7.0)
	f2.SetSize(7.0)
	f3.SetSize(7.0)

	image := pdfjet.NewImageFromFile(pdf, "images/TeslaX.png")
	image.ScaleBy(0.20)

	barcode := pdfjet.NewBarcode(pdfjet.CODE128, "Hello, World!")
	barcode.SetModuleLength(0.75)
	// Uncomment the line below if you want to print the text underneath the barcode.
	barcode.SetFont(f1)

	table := pdfjet.NewTableFromFile(f1, f2, "data/Electric_Vehicle_Population_10_Pages.csv")
	table.SetVisibleColumns(1, 2, 3, 4, 5, 6, 7, 9)
	table.GetCellAt(4, 0).SetImage(image)
	table.GetCellAt(5, 0).SetColSpan(8)
	table.GetCellAt(5, 0).SetBarcode(barcode)
	table.SetFontInRow(14, f3)
	table.GetCellAt(20, 0).SetColSpan(6)
	table.GetCellAt(20, 6).SetColSpan(2)
	table.SetColumnWidths()
	table.SetColumnWidth(0, image.GetWidth()+4.0)
	table.SetColumnWidth(3, table.GetColumnWidth(3)+10.0)
	table.SetColumnWidth(5, table.GetColumnWidth(5)+10.0)
	table.RightAlignNumbers()

	table.SetLocationFirstPage(50.0, 100.0)
	table.SetLocation(50.0, 0.0)
	table.SetBottomMargin(15.0)
	table.SetTextColorInRow(12, color.Blue)
	table.SetTextColorInRow(13, color.Red)
	// table.GetCellAt(13, 0).GetTextBox().SetURIAction("http://pdfjet.com")

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait)
	for i := 0; i < len(pages); i++ {
		page := pages[i]
		page.AddFooter(pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+1)+" of "+fmt.Sprint(len(pages))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example08()
	pdfjet.PrintDuration("Example_08", time.Since(start))
}
