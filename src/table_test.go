// table_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func testRows(font *Font, count, columns int) [][]*Cell {
	data := make([][]*Cell, 0)
	for r := 0; r < count; r++ {
		row := make([]*Cell, 0)
		for c := 0; c < columns; c++ {
			text := fmt.Sprintf("r%dc%d", r, c)
			if columns == 1 {
				text = fmt.Sprintf("row%d", r)
			}
			row = append(row, NewCell(font, text))
		}
		data = append(data, row)
	}
	return data
}

func TestTableMeasuringAndDrawingReturnTheSameCorner(t *testing.T) {
	pdf := testNewPDF()
	table := NewTable().SetData(testRows(testHelvetica(pdf), 5, 3), 1)
	table.SetLocation(20, 20)
	testAssertXY(t, 20, 109.36, table.DrawOn(nil))
	testAssertXY(t, 20, 109.36, table.DrawOn(NewPage(pdf, letter.Portrait())))
}

func TestTableMeasuringFirstStillDrawsEveryRowOnThePage(t *testing.T) {
	pdf := testNewPDF()
	table := NewTable().SetData(testRows(testHelvetica(pdf), 60, 1), 1)
	table.SetLocation(20, 20)
	table.DrawOn(nil)
	page := NewPage(pdf, letter.Portrait())
	table.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("row0")) || !strings.Contains(content, testHex("row1")) {
		t.Error("the first rows were not drawn")
	}
	if got := table.GetRowsRendered(); got != 42 {
		t.Errorf("rows rendered %d", got)
	}
}

func TestTableHeaderRowsRepeatOnEveryPage(t *testing.T) {
	pdf := testNewPDF()
	table := NewTable().SetData(testRows(testHelvetica(pdf), 60, 1), 1)
	table.SetLocation(20, 20)
	pages := make([]*Page, 0)
	testAssertXY(t, 20, 341.696, table.DrawOnPages(pdf, &pages, letter.Portrait()))
	if len(pages) != 2 {
		t.Fatalf("pages %d", len(pages))
	}
	first := testContent(pages[0])
	second := testContent(pages[1])
	if !strings.Contains(first, testHex("row0")) || !strings.Contains(second, testHex("row0")) {
		t.Error("the header row is not on every page")
	}
	if !strings.Contains(first, testHex("row1")) || strings.Contains(second, testHex("row1")) {
		t.Error("row1 is not on the first page only")
	}
	if strings.Contains(first, testHex("row59")) || !strings.Contains(second, testHex("row59")) {
		t.Error("row59 is not on the second page only")
	}
}

func TestTableTheFileConstructorDropsAByteOrderMarkAndPadsShortRows(t *testing.T) {
	path := filepath.Join(t.TempDir(), "table.txt")
	if err := os.WriteFile(path, []byte("\uFEFFa|b|c\n1||\n2\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	font := testHelvetica(testNewPDF())
	table := NewTableFromFile(font, font, path)
	checks := []struct {
		row, col int
		text     string
	}{{0, 0, "a"}, {0, 2, "c"}, {1, 0, "1"}, {1, 2, ""}, {2, 2, ""}}
	for _, c := range checks {
		if got := table.GetCellAt(c.row, c.col).GetText(); got != c.text {
			t.Errorf("cell %d,%d: %q", c.row, c.col, got)
		}
	}
	if len(table.GetRow(1)) != 3 || len(table.GetRow(2)) != 3 || len(table.GetColumn(0)) != 3 {
		t.Error("the rows are not padded")
	}
}

func TestTableGetCellAtGetRowAndGetColumnAgree(t *testing.T) {
	table := NewTable().SetData(testRows(testHelvetica(testNewPDF()), 4, 3), 1)
	if table.GetCellAt(2, 1) != table.GetRow(2)[1] || table.GetCellAt(2, 1) != table.GetColumn(1)[2] {
		t.Error("different cells")
	}
	if got := table.GetCellAt(2, 1).GetText(); got != "r2c1" {
		t.Errorf("text %q", got)
	}
}

func TestTableRightAlignNumbersRightAlignsOnlyNumbers(t *testing.T) {
	font := testHelvetica(testNewPDF())
	data := make([][]*Cell, 0)
	for _, text := range []string{"header", "-1.5e3", "12a", "+7", "3."} {
		data = append(data, []*Cell{NewCell(font, text)})
	}
	table := NewTable().SetData(data, 1).RightAlignNumbers()
	want := []alignment.Alignment{alignment.Right, alignment.Left, alignment.Right, alignment.Right}
	for i, w := range want {
		if got := table.GetCellAt(i+1, 0).GetTextAlignment(); got != w {
			t.Errorf("row %d: %v", i+1, got)
		}
	}
	if table.GetCellAt(0, 0).GetTextAlignment() == alignment.Right {
		t.Error("the header is right aligned")
	}
}

func TestTableAnEmptyTableHasNoWidth(t *testing.T) {
	testNear(t, "width", 0, NewTable().GetWidth(), 0)
}
