// bigtable_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"iter"
	"os"
	"path/filepath"
	"slices"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

var testBigTableHeader = []string{"Name", "City", "Total"}

// testBigTableRows returns a hundred rows, with a delimiter inside a quoted
// field, and a short row near the end, which is skipped.
func testBigTableRows() [][]string {
	rows := make([][]string, 0)
	for i := 0; i < 100; i++ {
		if i == 90 {
			rows = append(rows, []string{"short"})
		}
		rows = append(rows, []string{fmt.Sprintf("n%d", i), fmt.Sprintf("City, %d", i), fmt.Sprintf("%d.5", i)})
	}
	return rows
}

func testDrawBigTable(t *testing.T, table *BigTable) []*Page {
	t.Helper()
	table.SetLocation(10, 10)
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	return table.GetPages()
}

// testBigTableFile writes the rows as a delimited file, with the fields that
// hold a comma quoted, and returns its path.
func testBigTableFile(t *testing.T) string {
	t.Helper()
	var csv strings.Builder
	csv.WriteString("Name,City,Total\n")
	for _, row := range testBigTableRows() {
		if len(row) == 1 {
			csv.WriteString(row[0] + "\n")
		} else {
			csv.WriteString(row[0] + ",\"" + row[1] + "\"," + row[2] + "\n")
		}
	}
	path := filepath.Join(t.TempDir(), "rows.csv")
	if err := os.WriteFile(path, []byte(csv.String()), 0o644); err != nil {
		t.Fatal(err)
	}
	return path
}

func TestBigTableRowsFromMemoryDrawWhatTheSameFileDraws(t *testing.T) {
	path := testBigTableFile(t)

	pdf1 := testNewPDF()
	font1 := testHelvetica(pdf1)
	fromFile, err := NewBigTable(pdf1, font1, font1, letter.Portrait()).SetNumberOfColumns(3).SetTableData(path, ",")
	if err != nil {
		t.Fatal(err)
	}
	filePages := testDrawBigTable(t, fromFile)

	pdf2 := testNewPDF()
	font2 := testHelvetica(pdf2)
	memoryPages := testDrawBigTable(t, NewBigTable(pdf2, font2, font2, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(testBigTableRows())))

	// A page releases its content when it is written, so the last page is
	// compared, which also shows the column widths of the first pass.
	if len(filePages) != 2 || len(memoryPages) != 2 {
		t.Fatalf("pages: %d from the file, %d from memory", len(filePages), len(memoryPages))
	}
	last := testContent(memoryPages[1])
	if testContent(filePages[1]) != last {
		t.Error("the last pages differ")
	}
	if !strings.Contains(last, testHex("Name")) || !strings.Contains(last, testHex("n99")) {
		t.Error("the last page lacks the header or the last row")
	}
	if strings.Contains(last, testHex("short")) {
		t.Error("the short row was drawn")
	}
}

func TestBigTableChosenColumnsAreDrawnInTheirOrder(t *testing.T) {
	path := testBigTableFile(t)
	rows := testBigTableRows()
	rows = slices.Insert(rows, 95, []string{"n95b", "x"}) // No third field, so it is skipped

	pdf1 := testNewPDF()
	font1 := testHelvetica(pdf1)
	fromFile, err := NewBigTable(pdf1, font1, font1, letter.Portrait()).SetColumns(2, 0).SetTableData(path, ",")
	if err != nil {
		t.Fatal(err)
	}
	filePages := testDrawBigTable(t, fromFile)
	pdf2 := testNewPDF()
	font2 := testHelvetica(pdf2)
	memoryPages := testDrawBigTable(t, NewBigTable(pdf2, font2, font2, letter.Portrait()).
		SetColumns(2, 0).SetTableRows(testBigTableHeader, slices.Values(rows)))

	if len(memoryPages) != 2 {
		t.Fatalf("pages: %d", len(memoryPages))
	}
	last := testContent(memoryPages[1])
	if testContent(filePages[1]) != last {
		t.Error("the last pages differ")
	}
	if strings.Index(last, testHex("Total")) > strings.Index(last, testHex("Name")) {
		t.Error("the columns are not in their order")
	}
	if !strings.Contains(last, testHex("n99")) {
		t.Error("the last row was not drawn")
	}
	if strings.Contains(last, testHex("City")) || strings.Contains(last, testHex("n95b")) {
		t.Error("a column or a row that is not drawn was drawn")
	}
}

func TestBigTableANegativeColumnIndexIsRefused(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	NewBigTable(pdf, font, font, letter.Portrait()).SetColumns(1, -1)
	testRecorded(t, pdf, "A column index cannot be negative.")
}

func TestBigTableTheRowsAreReadTwice(t *testing.T) {
	rows := testBigTableRows()
	opened := 0
	var source iter.Seq[[]string] = func(yield func([]string) bool) {
		opened++
		for _, row := range rows {
			if !yield(row) {
				return
			}
		}
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	testDrawBigTable(t, NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(3).SetTableRows(testBigTableHeader, source))
	if opened != 2 {
		t.Errorf("opened %d times", opened)
	}
}

func TestBigTableAHeaderWithFewerFieldsThanColumnsIsRefused(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(4)
	table.SetTableRows(testBigTableHeader, slices.Values(testBigTableRows()))
	testRecorded(t, pdf, "The header does not have a field for every column.")
	if err := table.Complete(); err != nil || len(table.GetPages()) != 0 {
		t.Errorf("Complete: %v, %d pages", err, len(table.GetPages()))
	}
}
