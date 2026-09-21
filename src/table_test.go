// table_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"os"
	"path/filepath"
	"reflect"
	"regexp"
	"sort"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
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
	table := NewTable().SetTableData(testRows(testHelvetica(pdf), 5, 3), 1)
	table.SetLocation(20, 20)
	testAssertXY(t, 245, 109.36, table.DrawOn(nil))
	testAssertXY(t, 245, 109.36, table.DrawOn(NewPage(pdf, letter.Portrait())))
}

func TestTableMeasuringFirstStillDrawsEveryRowOnThePage(t *testing.T) {
	pdf := testNewPDF()
	table := NewTable().SetTableData(testRows(testHelvetica(pdf), 60, 1), 1)
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
	table := NewTable().SetTableData(testRows(testHelvetica(pdf), 60, 1), 1)
	table.SetLocation(20, 20)
	pages := make([]*Page, 0)
	testAssertXY(t, 95, 341.696, table.DrawOnPages(pdf, &pages, letter.Portrait()))
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

func TestTableTheFileConstructorReadsQuotedFields(t *testing.T) {
	data := "\"Name\",\"Note\",\"Amount\"\n" +
		"\"Smith, John\",\"said \"\"hi\"\"\",\"1,200\"\n" +
		"Plain,,7\n"
	path := testWriteFile(t, "quoted.csv", []byte(data))
	font := testHelvetica(testNewPDF())
	table := NewTableFromFile(font, font, path)
	if len(table.GetRow(0)) != 3 {
		t.Errorf("row 0 has %d cells", len(table.GetRow(0)))
	}
	for _, want := range []struct {
		row, col int
		text     string
	}{{0, 0, "Name"}, {1, 0, "Smith, John"}, {1, 1, `said "hi"`}, {1, 2, "1,200"},
		{2, 0, "Plain"}, {2, 1, ""}, {2, 2, "7"}} {
		if got := table.GetCellAt(want.row, want.col).GetText(); got != want.text {
			t.Errorf("cell (%d, %d): want %q, got %q", want.row, want.col, want.text, got)
		}
	}
}

func TestTableTheFileConstructorReadsLineBreaksInQuotedFieldsAsSpaces(t *testing.T) {
	data := "Name,Address\r\n" +
		"\"Smith, John\",\"12 Main St\r\nApt 4\"\r\n" +
		"Plain,\"one\n\ntwo\"\n"
	path := testWriteFile(t, "breaks.csv", []byte(data))
	font := testHelvetica(testNewPDF())
	table := NewTableFromFile(font, font, path)
	for _, want := range []struct {
		row, col int
		text     string
	}{{1, 1, "12 Main St Apt 4"}, {2, 0, "Plain"}, {2, 1, "one  two"}} {
		if got := table.GetCellAt(want.row, want.col).GetText(); got != want.text {
			t.Errorf("cell (%d, %d): want %q, got %q", want.row, want.col, want.text, got)
		}
	}
	if n := len(table.GetColumn(0)); n != 3 {
		t.Errorf("rows %d", n)
	}
}

func TestTableARowTallerThanThePageIsDrawnRatherThanAskedForForever(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	var text strings.Builder
	for i := 0; i < 200; i++ {
		fmt.Fprintf(&text, "word%d ", i)
	}
	header := []*Cell{NewCell(font, "header")}
	tall := NewEmptyCell(font)
	tall.SetTextBlock(NewTextBlock(font, text.String())).SetWidth(70)
	table := NewTable().SetTableData([][]*Cell{header, {tall}}, 1)
	table.SetLocation(50, 50)
	table.SetBottomMargin(20)
	if tall.GetHeight(66) <= 792 { // taller than a Letter page
		t.Fatalf("height %f", tall.GetHeight(66))
	}
	pages := make([]*Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait()) // asked for pages forever before
	if len(pages) != 1 {
		t.Fatalf("pages %d", len(pages))
	}
	if table.GetRowsRendered() != -1 {
		t.Errorf("rows rendered %d", table.GetRowsRendered())
	}
	if !strings.Contains(testContent(pages[0]), testHex("word0")) {
		t.Error("the row taller than the page is not drawn")
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
	table := NewTable().SetTableData(testRows(testHelvetica(testNewPDF()), 4, 3), 1)
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
	table := NewTable().SetTableData(data, 1).RightAlignNumbers()
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

func TestTableAnEmptyTableDrawsNothing(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	testAssertXY(t, 20, 30, NewTable().SetLocation(20, 30).DrawOn(page))
	testAssertXY(t, 20, 30, NewTable().SetLocation(20, 30).DrawOn(nil))
	table := NewTable()
	table.SetLocation(20, 30)
	pages := make([]*Page, 0)
	testAssertXY(t, 20, 30, table.DrawOnPages(pdf, &pages, letter.Portrait()))
	if len(pages) != 0 {
		t.Errorf("pages %d", len(pages))
	}
	empty := NewTable().SetTableData(make([][]*Cell, 0), 1)
	empty.AutoAdjustColumnWidths().RightAlignNumbers()
	testAssertXY(t, 0, 0, empty.DrawOn(page))
	if got := testContent(page); got != "" {
		t.Errorf("content %q", got)
	}
}

func TestTableMoreHeaderRowsThanRowsDrawsTheRows(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	expected := NewTable().SetTableData(testRows(font, 2, 1), 1).SetLocation(20, 20).DrawOn(nil)
	xy := NewTable().SetTableData(testRows(font, 2, 1), 5).SetLocation(20, 20).DrawOn(NewPage(pdf, letter.Portrait()))
	testAssertXY(t, expected[0], expected[1], xy)
}

func TestTableInAPDFUADocumentIsTaggedAsATable(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	underlined := NewCell(font, "b")
	underlined.SetUnderline(true)
	data := [][]*Cell{
		{NewCell(font, "Name"), NewCell(font, "Notes")},
		{NewCell(font, "a"), NewCell(font, "a note long enough to wrap to four lines")},
		{NewCell(font, "spanned").SetColSpan(2), NewCell(font, "")},
		{underlined, NewCell(font, "c")},
	}
	table := NewTable().SetTableData(data, 1)
	table.SetLocation(20, 20)
	table.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	raw := string(doc.complete())
	counts := map[string]int{
		"/S /Table\n": 1,
		// The lines of the wrapped note are one row and one cell.
		"/S /TR\n":                        4,
		"/S /TH\n":                        2,
		"/S /TD\n":                        5,
		"/A <</O /Table /Scope /Column>>": 2,
		"/A <</O /Table /ColSpan 2>>":     1,
		// The text of the cells, and not the underline, is in P elements.
		"/S /P\n": 10,
	}
	for text, want := range counts {
		if got := strings.Count(raw, text); got != want {
			t.Errorf("%q is in the PDF %d times, not %d", text, got, want)
		}
	}
	fourKids := regexp.MustCompile(`/S /TD\n[^\n]*\n/K \[\d+ 0 R \d+ 0 R \d+ 0 R \d+ 0 R \]`)
	if n := len(fourKids.FindAllString(raw, -1)); n != 1 {
		t.Errorf("%d cells with the four lines of the note", n)
	}
}

func TestTableHeaderRowsOnTheNextPagesAreArtifacts(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	table := NewTable().SetTableData(testRows(testHelvetica(doc.pdf), 60, 2), 1)
	table.SetLocation(20, 20)
	pages := make([]*Page, 0)
	table.DrawOnPages(doc.pdf, &pages, letter.Portrait())
	if len(pages) != 2 {
		t.Fatalf("%d pages", len(pages))
	}
	content := testContent(pages[1])
	if !strings.HasPrefix(content, "/Artifact BMC\n") {
		t.Errorf("the second page starts with %q", content[:40])
	}
	end := strings.Index(content, "EMC\n")
	if strings.Index(content, testHex("r0c1")) > end || strings.Index(content, "BDC") < end {
		t.Error("the header row on the second page is not an artifact")
	}
	for _, page := range pages {
		doc.pdf.AddPage(page)
	}
	raw := string(doc.complete())
	counts := map[string]int{"/S /Table\n": 1, "/S /TR\n": 60, "/S /TH\n": 2, "/S /TD\n": 118}
	for text, want := range counts {
		if got := strings.Count(raw, text); got != want {
			t.Errorf("%q is in the PDF %d times, not %d", text, got, want)
		}
	}
}

func TestTableIsNotTaggedInADocumentThatIsNotPDFUA(t *testing.T) {
	pdf := testNewPDF()
	table := NewTable().SetTableData(testRows(testHelvetica(pdf), 60, 2), 1)
	table.SetLocation(20, 20)
	pages := make([]*Page, 0)
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	content := testContent(pages[1])
	if strings.Contains(content, "BMC") || strings.Contains(content, "BDC") || strings.Contains(content, "EMC") {
		t.Error("marked content in a document that is not PDF/UA")
	}
}

func TestTableWithAPageLeftOutOfTheDocumentStillHasAStructureTree(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	table := NewTable().SetTableData(testRows(testHelvetica(doc.pdf), 60, 2), 1)
	table.SetLocation(20, 20)
	pages := make([]*Page, 0)
	table.DrawOnPages(doc.pdf, &pages, letter.Portrait())
	doc.pdf.AddPage(pages[1]) // The page with the Table element is left out.
	raw := string(doc.complete())
	if strings.Contains(raw, "/P 0 0 R") || strings.Contains(raw, "/K [0 0 R") || strings.Contains(raw, " 0 0 R ]") {
		t.Error("a structure element refers to one that is not in the document")
	}
}

// testSpanningRows returns a table of rows by columns of cells with every
// border, the cell at 0,0 spanning the rows.
func testSpanningRows(font *Font, rows, columns, rowspan int) [][]*Cell {
	data := make([][]*Cell, 0, rows)
	for r := 0; r < rows; r++ {
		row := make([]*Cell, 0, columns)
		for c := 0; c < columns; c++ {
			text := fmt.Sprintf("r%dc%d", r, c)
			if r == 0 && c == 0 {
				text = "spans"
			} else if r < rowspan && c == 0 {
				text = ""
			}
			cell := NewCell(font, text)
			cell.SetWidth(60)
			cell.SetBorder(border.Top, true)
			cell.SetBorder(border.Bottom, true)
			cell.SetBorder(border.Left, true)
			cell.SetBorder(border.Right, true)
			row = append(row, cell)
		}
		data = append(data, row)
	}
	data[0][0].SetRowSpan(rowspan)
	return data
}

// testRules returns the y of the horizontal rules the page draws, top to bottom.
func testRules(page *Page) []float64 {
	re := regexp.MustCompile(`([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l`)
	seen := map[float64]bool{}
	ys := make([]float64, 0)
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		y1, _ := strconv.ParseFloat(m[2], 64)
		y2, _ := strconv.ParseFloat(m[4], 64)
		if math.Abs(y1-y2) < 0.01 && !seen[y1] {
			seen[y1] = true
			ys = append(ys, y1)
		}
	}
	sort.Sort(sort.Reverse(sort.Float64Slice(ys)))
	return ys
}

func TestTableACellThatSpansRowsIsDrawnOnceOverAllOfThem(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTable().SetTableData(testSpanningRows(font, 3, 2, 2), 0).SetLocation(50, 50).DrawOn(page)
	content := testContent(page)
	// The spanning cell is drawn once, and the cell it covers not at all.
	if strings.Count(content, testHex("spans")) != 1 {
		t.Error("the spanning cell is not drawn once")
	}
	if strings.Contains(content, testHex("r1c0")) {
		t.Error("a cell the span covers was drawn")
	}
	if !strings.Contains(content, testHex("r1c1")) {
		t.Error("the cell beside the span was not drawn")
	}
	// Its left border runs from the top of its row to the bottom of the row
	// under it, which is two of the rows the table draws.
	ys := testRules(page)
	if len(ys) != 4 {
		t.Fatalf("%d rules: %v", len(ys), ys)
	}
	rowHeight := ys[0] - ys[1]
	m := regexp.MustCompile(`50 ([-0-9.]+) m\n50 ([-0-9.]+) l`).FindStringSubmatch(content)
	if m == nil {
		t.Fatal("the left border of the spanning cell was not drawn")
	}
	top, _ := strconv.ParseFloat(m[1], 64)
	bottom, _ := strconv.ParseFloat(m[2], 64)
	if math.Abs((top-bottom)-2*rowHeight) > 0.01 {
		t.Errorf("the spanning cell is %g tall, not %g", top-bottom, 2*rowHeight)
	}
}

func TestTableAPageBreakKeepsTheRowsOfASpanTogether(t *testing.T) {
	// The rows a cell spans go to the next page with it, so that a span is
	// never cut in two.
	for _, rowspan := range []int{1, 2, 3, 4} {
		doc := testNewDoc()
		font := testHelvetica(doc.pdf)
		data := testSpanningRows(font, 60, 2, rowspan)
		data[0][0].SetRowSpan(1)
		data[48][0].SetRowSpan(rowspan).SetText("spans")
		for r := 49; r < 48+rowspan; r++ {
			data[r][0].SetText("")
		}
		table := NewTable().SetTableData(data, 0)
		table.SetLocation(50, 50)
		table.SetBottomMargin(20)
		pages := make([]*Page, 0)
		table.DrawOnPages(doc.pdf, &pages, letter.Portrait())
		contents := make([]string, 0, len(pages))
		for _, page := range pages {
			contents = append(contents, testContent(page))
		}
		spanPage := -1
		for i, content := range contents {
			if strings.Contains(content, testHex("spans")) {
				spanPage = i
			}
		}
		if spanPage < 0 {
			t.Fatalf("row span %d: the spanning cell was not drawn", rowspan)
		}
		for r := 48; r < 48+rowspan; r++ {
			if !strings.Contains(contents[spanPage], testHex(fmt.Sprintf("r%dc1", r))) {
				t.Errorf("row span %d: row %d is not on the page of the span", rowspan, r)
			}
		}
	}
}

func TestTableACellThatSpansRowsSaysSoInAPDFUADocument(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	data := testSpanningRows(font, 3, 2, 2)
	data[0][0].SetColSpan(2)
	data[0][1].SetText("")
	data[1][1].SetText("") // The second row is covered whole.
	NewTable().SetTableData(data, 1).SetLocation(50, 50).
		DrawOn(NewPage(doc.pdf, letter.Portrait()))
	raw := string(doc.complete())
	if strings.Count(raw, "<</O /Table /Scope /Column /ColSpan 2 /RowSpan 2>>") != 1 {
		t.Error("the spanning cell does not say how many rows it spans")
	}
	// A row that a span covers whole holds no cell of its own.
	testWant(t, "2 rows, 1 header cell, 2 cells", fmt.Sprintf("%d rows, %d header cell, %d cells",
		strings.Count(raw, "/S /TR\n"), strings.Count(raw, "/S /TH\n"), strings.Count(raw, "/S /TD\n")))
}

const testLongText = "one two three four five six seven eight nine ten eleven twelve"

// testWrapping returns a one row table of a cell that wraps and a cell that
// does not, each with every border.
func testWrapping(font *Font) [][]*Cell {
	row := make([]*Cell, 0)
	for _, text := range []string{testLongText, "one"} {
		cell := NewCell(font, text)
		cell.SetWidth(60.0)
		cell.SetBorders(true)
		row = append(row, cell)
	}
	return [][]*Cell{row}
}

func TestTableACellWhoseTextWrapsDrawsOneBorderUnderIt(t *testing.T) {
	// The rows a table wraps the text of a cell into are one cell, so the
	// border under it is drawn once, under the last of its lines. Each of
	// those rows kept the borders of the cell, so a cell of seven lines drew
	// seven rules across itself.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	table := NewTable()
	table.SetTableData(testWrapping(font), 0)
	table.SetLocation(50, 50)
	xy := table.DrawOn(page)
	if table.GetRow(0)[0].GetText() == testLongText {
		t.Fatal("the text did not wrap")
	}
	// The table draws two rules: the one over the row and the one under it.
	ys := testRules(page)
	if len(ys) != 2 {
		t.Fatalf("the table drew %d rules, not 2: %v", len(ys), ys)
	}
	if math.Abs(ys[0]-float64(page.height-50.0)) > 0.01 {
		t.Errorf("the rule over the row is at %v, not %v", ys[0], page.height-50.0)
	}
	if math.Abs(ys[1]-float64(page.height-xy[1])) > 0.01 {
		t.Errorf("the rule under the row is at %v, not %v", ys[1], page.height-xy[1])
	}
}

func TestTableTheRowsACellWrapsIntoAreOneCellOfEveryBorderButTheirOwn(t *testing.T) {
	// The left and the right borders are drawn down every row of the wrap,
	// which is what makes the rows one cell; only the top and the bottom of
	// the cell are drawn once.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	table := NewTable()
	table.SetTableData(testWrapping(font), 0)
	table.SetLocation(50, 50)
	xy := table.DrawOn(page)
	// A vertical rule of each row of the wrap, down each of the three edges
	// the two cells have between and beside them.
	re := regexp.MustCompile(`([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l`)
	down := map[string]float64{}
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		if m[1] == m[3] {
			y1, _ := strconv.ParseFloat(m[2], 64)
			y2, _ := strconv.ParseFloat(m[4], 64)
			down[m[1]] += math.Abs(y1 - y2)
		}
	}
	edges := make([]string, 0, len(down))
	for x := range down {
		edges = append(edges, x)
	}
	sort.Strings(edges)
	if want := []string{"110", "170", "50"}; !reflect.DeepEqual(edges, want) {
		t.Fatalf("the table drew rules down %v, not %v", edges, want)
	}
	height := float64(xy[1] - 50.0)
	// The edge between the two cells is the right border of the one and the
	// left border of the other, so it is drawn twice.
	for x, want := range map[string]float64{"50": height, "110": 2 * height, "170": height} {
		if math.Abs(down[x]-want) > 0.01 {
			t.Errorf("the rules down x=%s run %v, not %v", x, down[x], want)
		}
	}
}

// testRulesAcrossTheFirstColumn returns the y of every rule the page draws
// across the first column, in the order they are drawn and with none left out.
func testRulesAcrossTheFirstColumn(page *Page) []float64 {
	re := regexp.MustCompile(`50 ([-0-9.]+) m\n110 ([-0-9.]+) l`)
	ys := make([]float64, 0)
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		if m[1] == m[2] {
			y, _ := strconv.ParseFloat(m[1], 64)
			ys = append(ys, y)
		}
	}
	return ys
}

func TestTableACellThatWrapsAndSpansRowsDrawsItsBorderUnderTheWholeSpan(t *testing.T) {
	// A cell that spans rows is drawn over all of them at once, so its bottom
	// border is drawn under the whole span and not at the end of the rows its
	// own text wraps into, which is where a cell that spans no rows draws it.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	data := testSpanningRows(font, 3, 2, 2)
	data[0][0].SetText(testLongText)
	page := NewPage(doc.pdf, letter.Portrait())
	table := NewTable()
	table.SetTableData(data, 0)
	table.SetLocation(50, 50)
	xy := table.DrawOn(page)
	if table.GetRow(0)[0].GetText() == testLongText {
		t.Fatal("the text did not wrap")
	}
	// The rule over the span, the one under it, the one over the row below it,
	// which is the same line drawn by that row, and the one under the table.
	// The rows the text wrapped into draw none of their own.
	ys := testRulesAcrossTheFirstColumn(page)
	if len(ys) != 4 {
		t.Fatalf("the table drew %d rules across the first column, not 4: %v", len(ys), ys)
	}
	if math.Abs(ys[0]-float64(page.height-50.0)) > 0.01 {
		t.Errorf("the rule over the span is at %v, not %v", ys[0], page.height-50.0)
	}
	if math.Abs(ys[1]-ys[2]) > 0.01 {
		t.Errorf("the span ends at %v and the row under it starts at %v", ys[1], ys[2])
	}
	if !(ys[1] < ys[0] && ys[1] > ys[3]) {
		t.Errorf("the rule under the span, %v, is not between %v and %v", ys[1], ys[0], ys[3])
	}
	if math.Abs(ys[3]-float64(page.height-xy[1])) > 0.01 {
		t.Errorf("the rule under the table is at %v, not %v", ys[3], page.height-xy[1])
	}
}
