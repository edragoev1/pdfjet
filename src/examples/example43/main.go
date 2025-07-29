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

	fileName := "data/Electric_Vehicle_Population_Data.csv"
	// fileName := "data/Electric_Vehicle_Population_550.csv"

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream")
	f1.SetSize(9.0)

	f2 := pdfjet.NewFontFromFile(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
	f2.SetSize(9.0)

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Landscape)
	widths := table.GetColumnWidths(fileName)
	widths[8] = 60.0 // Override the calculated width
	widths[9] = 70.0 // Override the calculated width
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
		drawRow(table, fields, headerRow)
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

func drawRow(table *pdfjet.BigTable, fields []string, headerRow bool) {
	row := make([]string, 0)
	for i := 0; i < 10; i++ {
		field := fields[i]
		switch i {
		case 8:
			if headerRow {
				row = append(row, "Vehicle Type")
			} else {
				switch field[0] {
				case 'B':
					row = append(row, "BEV")
				case 'P':
					row = append(row, "PHEV")
				default:
					row = append(row, fields[8])
				}
			}
		case 9:
			if headerRow {
				row = append(row, "Green Vehicle")
			} else {
				switch field[0] {
				case 'C':
					row = append(row, "YES")
				case 'N':
					row = append(row, "NO")
				default:
					row = append(row, "UNKNOWN")
				}
			}
		default:
			row = append(row, field)
		}
	}
	switch fields[6] {
	case "TOYOTA":
		table.DrawRow(row, color.Red)
	case "JEEP":
		table.DrawRow(row, color.Green)
	case "FORD":
		table.DrawRow(row, color.Blue)
	default:
		table.DrawRow(row, color.Black)
	}
}

func main() {
	start := time.Now()
	Example43()
	pdfjet.PrintDuration("Example_43", time.Since(start))
}
