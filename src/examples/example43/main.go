package main

import (
	"bufio"
	"fmt"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example43 --
func Example43() {
	pdf := pdfjet.NewPDFFile("Example_43.pdf")
	pdf.SetCompliance(compliance.PDF_UA)

	// f1 := NewFontFromFile(pdf, CoreFont.HELVETICA_BOLD)
	// f2 := NewFontFromFile(pdf, CoreFont.HELVETICA)
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream")
	f1.SetSize(8.0)
	f2.SetSize(8.0)

	fileName := "data/Electric_Vehicle_Population_Data.csv"
	// fileName :=  "data/Electric_Vehicle_Population_1000.csv"

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Landscape)
	widths := table.GetColumnWidths(fileName)
	widths[8] = 70.0 // Override the calculated width
	widths[9] = 99.0 // Override the calculated width
	table.SetColumnSpacing(7.0)
	table.SetLocation(20.0, 15.0)
	table.SetBottomMargin(15.0)
	table.SetColumnWidths(widths)

	readFile, err := os.Open(fileName)
	if err != nil {
		fmt.Println(err)
	}
	fileScanner := bufio.NewScanner(readFile)
	fileScanner.Split(bufio.ScanLines)
	headerRow := true
	for fileScanner.Scan() {
		line := fileScanner.Text()
		fields := strings.Split(line, ",")
		// Optional step:
		fields = selectAndProcessFields(table, fields, headerRow)
		if fields[6] == "TOYOTA" {
			table.DrawRow(fields, color.Red)
		} else if fields[6] == "JEEP" {
			table.DrawRow(fields, color.Green)
		} else if fields[6] == "FORD" {
			table.DrawRow(fields, color.Blue)
		} else {
			table.DrawRow(fields, color.Black)
		}
		headerRow = false
	}
	table.Complete()
	readFile.Close()

	pages := table.GetPages()
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+1)+" of "+fmt.Sprint((len(pages)))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func selectAndProcessFields(table *pdfjet.BigTable, fields []string, headerRow bool) []string {
	row := make([]string, 0)
	for i := 0; i < 10; i++ {
		field := fields[i]
		if i == 8 {
			if field[0] == 'B' {
				row = append(row, "BEV")
			} else if field[0] == 'P' {
				row = append(row, "PHEV")
			} else {
				row = append(row, fields[8])
			}
		} else if i == 9 {
			if headerRow {
				row = append(row, "Clean Alternative Fuel Vehicle")
			} else {
				if field[0] == 'C' {
					row = append(row, "YES")
				} else if field[0] == 'N' {
					row = append(row, "NO")
				} else {
					row = append(row, "UNKNOWN")
				}
			}
		} else {
			row = append(row, field)
		}
	}
	return row
}

func main() {
	start := time.Now()
	Example43()
	pdfjet.PrintDuration("Example_43", time.Since(start))
}
