package main

import (
	"bufio"
	"fmt"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example99 --
func Example99() {
	pdf := pdfjet.NewPDFFile("Example_99.pdf", 0)

	// f1 := NewFontFromFile(pdf, CoreFont.HELVETICA_BOLD)
	// f2 := NewFontFromFile(pdf, CoreFont.HELVETICA)
	// f1 := pdfjet.NewFontFromFile(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream")
	// f2 := pdfjet.NewFontFromFile(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream")
	// f1 := pdfjet.NewFontFromFile(pdf, "fonts/Andika/Andika-Bold.ttf.stream")
	// f2 := pdfjet.NewFontFromFile(pdf, "fonts/Andika/Andika-Regular.ttf.stream")
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/SourceCodePro/SourceCodePro-SemiBold.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream")
	// f1.SetSize(9.0)
	// f2.SetSize(9.0)
	f1.SetSize(7.0)
	f2.SetSize(7.0)

	L := 0
	R := 1
	// widths := []int{15, 15, 18, 7, 12, 12, 15, 15, 25}
	// align := []int{L, L, L, L, R, R, L, L, L}

	table := pdfjet.NewBigTable(pdf, f1, f2, letter.Portrait)
	table.SetLocation(20.0, 15.0)
	table.SetBottomMargin(15.0)
	table.SetColumnWidths([]int{80, 80, 35, 60, 60, 75, 110, 90})
	table.SetTextAlignment([]int{L, L, L, R, R, L, L, L})
	table.SetColumnSpacing(2.0)
	table.SetDrawVerticalLines(false)
	// table.SetHeaderRowColor(color.Darkolivegreen)

	readFile, err := os.Open("../datasets/Electric_Vehicle_Population_Data.csv")
	if err != nil {
		fmt.Println(err)
	}
	fileScanner := bufio.NewScanner(readFile)
	fileScanner.Split(bufio.ScanLines)
	for fileScanner.Scan() {
		line := fileScanner.Text()
		fields := strings.Split(line, ",")

		// textLine := table.GetTextLine(fields, widths, align)
		// table.Add(textLine)
		// if strings.Contains(textLine, "FORD") {
		// 	table.DrawRow(color.Blue)
		// } else if strings.Contains(textLine, "VOLKSWAGEN") {
		// 	table.DrawRow(color.Red)
		// } else {
		// 	table.DrawRow(color.Black)
		// }

		table.Add(fields[0])
		table.Add(fields[2])
		table.Add(fields[3])
		table.Add(fields[4])
		table.Add(fields[5])
		table.Add(fields[6])
		table.Add(fields[7])
		if strings.HasPrefix(fields[8], "B") {
			table.Add("BEV")
		} else if strings.HasPrefix(fields[8], "P") {
			table.Add("PHEV")
		} else {
			table.Add(fields[8])
		}
		if fields[6] == "FORD" {
			table.DrawRow(color.Blue)
		} else if fields[6] == "VOLKSWAGEN" {
			table.DrawRow(color.Red)
		} else {
			table.DrawRow(color.Black)
		}
	}
	readFile.Close()

	pages := table.GetPages()
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1, "Page "+fmt.Sprint(i+1)+" of "+fmt.Sprint((len(pages)))))
		pdf.AddPage(page)
	}

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example99()
	pdfjet.PrintDuration("Example_73", time.Since(start))
}
