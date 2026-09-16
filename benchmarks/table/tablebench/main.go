// Table at the scale the TODO asks about: the 9 columns of the Electric
// Vehicle Population CSV built as Cell objects and drawn with Table on as
// many Letter pages as they need.
//
// The widths are narrow enough that some columns wrap, so the benchmark
// measures the wrapping as well as the drawing; the rows of the file are
// repeated until the table has the number of rows asked for.
//
// Usage: tablebench bench|cold <rows>
//        tablebench sample <rows> <file>
//
// Run from the root of the repository, as the paths are relative to it.
package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"sort"
	"strconv"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

const csvFile = "data/Electric_Vehicle_Population_10_Pages.csv"
const semibold = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf.stream"
const regular = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"
const columns = 9

var widths = [columns]float32{68.0, 58.0, 58.0, 26.0, 44.0, 34.0, 52.0, 60.0, 152.0}

// countingWriter counts the bytes written, so that the output costs no memory
// or disk.
type countingWriter struct {
	n int
}

func (w *countingWriter) Write(b []byte) (int, error) {
	w.n += len(b)
	return len(b), nil
}

// readFields returns the first columns fields of every line of the CSV, the
// header first.
func readFields() [][]string {
	data, err := os.ReadFile(csvFile)
	if err != nil {
		log.Fatal(err)
	}
	var lines [][]string
	for _, line := range strings.Split(strings.ReplaceAll(string(data), "\r\n", "\n"), "\n") {
		fields := strings.Split(line, ",")
		if len(fields) < columns {
			continue
		}
		lines = append(lines, fields[:columns])
	}
	return lines
}

func tableData(fields [][]string, rows int, f1, f2 *pdfjet.Font) [][]*pdfjet.Cell {
	data := make([][]*pdfjet.Cell, 0, rows+1)
	for r := 0; r <= rows; r++ {
		// Row 0 is the header; the data rows repeat the file from line 1.
		values := fields[0]
		if r > 0 {
			values = fields[1+(r-1)%(len(fields)-1)]
		}
		row := make([]*pdfjet.Cell, 0, columns)
		for c := 0; c < columns; c++ {
			font := f2
			if r == 0 {
				font = f1
			}
			cell := pdfjet.NewCell(font, values[c])
			cell.SetWidth(widths[c])
			if c == 4 {
				cell.SetTextAlignment(alignment.Right)
			} else {
				cell.SetTextAlignment(alignment.Left)
			}
			row = append(row, cell)
		}
		data = append(data, row)
	}
	return data
}

func document(fields [][]string, rows int, w *bufio.Writer) int {
	pdf := pdfjet.NewPDF(w)
	f1 := pdfjet.NewFontFromFile(pdf, semibold)
	f1.SetSize(8.0)
	f2 := pdfjet.NewFontFromFile(pdf, regular)
	f2.SetSize(8.0)

	table := pdfjet.NewTable()
	table.SetTableData(tableData(fields, rows, f1, f2), 1)
	table.SetLocation(20.0, 20.0)
	table.SetBottomMargin(20.0)

	pages := make([]*pdfjet.Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	for _, page := range pages {
		pdf.AddPage(page)
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
	return len(pages)
}

func run(fields [][]string, rows int) (int, int) {
	sink := &countingWriter{}
	w := bufio.NewWriter(sink)
	pages := document(fields, rows, w)
	w.Flush()
	return pages, sink.n
}

func main() {
	start := time.Now()
	mode := os.Args[1]
	rows, err := strconv.Atoi(os.Args[2])
	if err != nil {
		log.Fatal(err)
	}
	fields := readFields()
	if mode == "cold" {
		pages, bytes := run(fields, rows)
		fmt.Printf("go cold %d rows: %d ms, %d pages, %d bytes\n",
			rows, time.Since(start).Milliseconds(), pages, bytes)
		return
	}
	if mode == "sample" {
		file, err := os.Create(os.Args[3])
		if err != nil {
			log.Fatal(err)
		}
		w := bufio.NewWriter(file)
		document(fields, rows, w)
		w.Flush()
		file.Close()
		return
	}
	for i := 0; i < 2; i++ {
		run(fields, rows)
	}
	ms := make([]int, 7)
	pages, size := 0, 0
	for i := range ms {
		t := time.Now()
		pages, size = run(fields, rows)
		ms[i] = int(time.Since(t).Milliseconds())
	}
	sort.Ints(ms)
	fmt.Printf("go %d rows: median %d ms (min %d, max %d, %d runs), %d pages, %d bytes\n",
		rows, ms[len(ms)/2], ms[0], ms[len(ms)-1], len(ms), pages, size)
}
