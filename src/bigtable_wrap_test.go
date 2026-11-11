// bigtable_wrap_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"iter"
	"math/rand"
	"slices"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testWrapTable draws the rows in a table of the header's columns, with
// wrapping on or off, and returns its pages.
func testWrapTable(t *testing.T, pdf *PDF, font *Font, wrap bool, header []string, rows [][]string,
	set func(*BigTable)) []*Page {
	t.Helper()
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(len(header))
	table.SetTableRows(header, slices.Values(rows))
	table.SetWrapText(wrap)
	if set != nil {
		set(table)
	}
	table.SetLocation(20, 20)
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	return table.GetPages()
}

// testWrapWords returns a text of n words of the seed's lengths.
func testWrapWords(r *rand.Rand, n int) string {
	words := make([]string, n)
	for i := range words {
		words[i] = strings.Repeat(string(rune('a'+r.Intn(26))), 1+r.Intn(9))
	}
	return strings.Join(words, " ")
}

// A table whose columns fit its page draws the same with wrapping on as off,
// byte for byte: nothing wraps, and the rows and pages are where they were.
func TestBigTableWrapOnATableThatFitsDrawsTheSameAsOff(t *testing.T) {
	draw := func(wrap bool) []string {
		pdf := testNewPDF()
		pages := testWrapTable(t, pdf, testHelvetica(pdf), wrap, testBigTableHeader, testBigTableRows(), nil)
		content := make([]string, len(pages))
		for i, page := range pages {
			content[i] = testContent(page)
		}
		return content
	}
	off, on := draw(false), draw(true)
	if !slices.Equal(off, on) {
		t.Fatalf("wrapping a table that fits changed it: %d pages off, %d on", len(off), len(on))
	}
}

// A field too long for its column wraps onto more lines, its words whole and
// in their order, no line wider than the column, and nothing cut.
func TestBigTableWrapALongFieldWrapsWholeWithinItsColumn(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	text := "The quick brown fox jumps over the lazy dog, again and again, until the page " +
		"is far too narrow for the whole of the sentence on one line of the table."
	rows := [][]string{{"1", text, "2.5"}}
	pages := testWrapTable(t, pdf, font, true, []string{"No", "Description", "Total"}, rows, nil)
	drawn := testDrawnText(pages[0])
	// The header, then the row's three fields, the description on lines
	if len(drawn) < 6 {
		t.Fatalf("the page drew %q", drawn)
	}
	lines := drawn[4 : len(drawn)-2] // Before the total and the footer
	if strings.Join(lines, " ") != text {
		t.Errorf("the lines are not the text:\n%q\n%q", strings.Join(lines, " "), text)
	}
	for _, line := range lines {
		if strings.HasSuffix(line, bigTableEllipsis) {
			t.Errorf("a line was cut: %q", line)
		}
	}
	room := letter.Portrait().GetWidth() - 40
	for _, line := range lines {
		if width := font.StringWidth(font.size, line); width > room {
			t.Errorf("a line is wider than the table: %q %v", line, width)
		}
	}
}

// A word longer than its column is broken between its characters, the
// pieces in their order.
func TestBigTableWrapAWordLongerThanItsColumnIsBroken(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	word := strings.Repeat("abcdefghij", 12)
	lines := bigTableWrap(word, font, 60)
	if len(lines) < 2 || strings.Join(lines, "") != word {
		t.Fatalf("the word broke as %q", lines)
	}
	for _, line := range lines {
		if font.StringWidth(font.size, line) > 60 {
			t.Errorf("a piece is wider than the column: %q", line)
		}
	}
	// A column too narrow for one character still draws one on each line
	if narrow := bigTableWrap("abc", font, 0.5); !slices.Equal(narrow, []string{"a", "b", "c"}) {
		t.Errorf("a column too narrow drew %q", narrow)
	}
	// Spaces are where lines break, not text: an empty field is one line
	if empty := bigTableWrap("  ", font, 60); !slices.Equal(empty, []string{""}) {
		t.Errorf("a field of spaces drew %q", empty)
	}
}

// The narrow columns keep their widths, and the wide ones share the rest of
// the page equally.
func TestBigTableWrapTheNarrowColumnsKeepTheirWidths(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	long := strings.Repeat("word ", 60)
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(4)
	table.SetTableRows([]string{"No", "A", "B", "Total"}, slices.Values([][]string{{"1", long, long, "12.50"}}))
	table.SetWrapText(true)
	table.SetLocation(20, 20)
	natural := slices.Clone(table.widths)
	if table.textWidths[0] != natural[0] || table.textWidths[3] != natural[3] {
		t.Errorf("a narrow column lost its width: %v of %v", table.textWidths, natural)
	}
	if table.textWidths[1] != table.textWidths[2] || table.textWidths[1] >= natural[1] {
		t.Errorf("the wide columns do not share the rest equally: %v", table.textWidths)
	}
	if right := table.vertLines[4]; right > letter.Portrait().GetWidth()-20+0.01 {
		t.Errorf("the table is wider than its page: %v", right)
	}
}

// The footer of every page says how many pages there are, however the rows
// wrap: the pages are counted as they are drawn.
func TestBigTableWrapThePagesAreCountedAsTheyAreDrawn(t *testing.T) {
	for seed := int64(1); seed <= 20; seed++ {
		r := rand.New(rand.NewSource(seed))
		rows := make([][]string, 40+r.Intn(120))
		for i := range rows {
			rows[i] = []string{fmt.Sprint(i), testWrapWords(r, r.Intn(60)), testWrapWords(r, r.Intn(12))}
		}
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		pages := testWrapTable(t, pdf, font, true, []string{"No", "Notes", "Name"}, rows,
			func(table *BigTable) { table.SetVerticalPadding(float32(r.Intn(4))) })
		last := fmt.Sprintf("Page %d of %d", len(pages), len(pages))
		if !strings.Contains(testContent(pages[len(pages)-1]), testHex(last)) {
			t.Errorf("seed %d: %d pages, and the last does not say %q", seed, len(pages), last)
		}
	}
}

// A row that does not fit what is left of a page starts the next page whole.
func TestBigTableWrapARowThatDoesNotFitStartsTheNextPage(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	rows := make([][]string, 0)
	for i := 0; i < 50; i++ {
		rows = append(rows, []string{fmt.Sprint(i), "short"})
	}
	tall := strings.Repeat("several words of a long note ", 40)
	rows = append(rows, []string{"tall", tall})
	pages := testWrapTable(t, pdf, font, true, []string{"No", "Notes"}, rows, nil)
	if len(pages) < 2 {
		t.Fatalf("%d pages", len(pages))
	}
	first := strings.Join(testDrawnText(pages[0]), "|")
	if strings.Contains(first, "|tall|") {
		t.Errorf("the tall row began on the first page")
	}
	if drawn := strings.Join(testDrawnText(pages[len(pages)-1]), " "); !strings.Contains(drawn, "tall") {
		t.Errorf("the tall row is not on the last page")
	}
}

// A row taller than a whole page is cut at the lines that fit it, its last
// line ending in the mark, and the cell of a PDF/UA document keeps the whole
// of its text.
func TestBigTableWrapARowTallerThanAPageIsCutAndItsCellKeepsTheText(t *testing.T) {
	testAllowCoreFonts(t)
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := NewCoreFont(doc.pdf, corefont.Helvetica())
	font.SetSize(12)
	text := strings.TrimSpace(strings.Repeat("a note that runs on and on ", 400))
	pages := testWrapTable(t, doc.pdf, font, true, []string{"No", "Notes"}, [][]string{{"1", text}}, nil)
	if len(pages) != 1 {
		t.Fatalf("%d pages", len(pages))
	}
	drawn := testDrawnText(pages[0])
	if !strings.HasSuffix(drawn[len(drawn)-2], bigTableEllipsis) {
		t.Errorf("the last line drawn does not end in the mark: %q", drawn[len(drawn)-2])
	}
	raw := string(doc.complete())
	var alt strings.Builder
	alt.WriteString("feff")
	for _, r := range text {
		alt.WriteString(fmt.Sprintf("%04x", r))
	}
	if !strings.Contains(raw, "/Alt <"+alt.String()+">") {
		t.Error("the cell does not keep the whole of the text")
	}
}

// A header too long for its column wraps too, and the rows start under it.
func TestBigTableWrapTheHeaderWrapsToo(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	header := []string{"No", "A heading of many words for a narrow column of short values"}
	rows := [][]string{{"1", "x"}}
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(2)
	table.SetTableRows(header, slices.Values(rows)).SetWrapText(true).SetColumnWidthsInPercent(80, 20)
	table.SetLocation(20, 20)
	if table.headerCount < 2 {
		t.Fatalf("the header did not wrap: %q", table.headerLines)
	}
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	drawn := testDrawnText(table.GetPages()[0])
	if drawn[len(drawn)-2] != "x" {
		t.Errorf("the page drew %q", drawn)
	}
}

// Wrapping reads the rows once more, to count the pages: three times in all,
// and none of them kept.
func TestBigTableWrapReadsTheRowsThreeTimes(t *testing.T) {
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
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(3).
		SetTableRows(testBigTableHeader, source).SetWrapText(true)
	testDrawBigTable(t, table)
	if opened != 3 {
		t.Errorf("opened %d times", opened)
	}
}

// Wrapping turned off again draws the table as it was never on.
func TestBigTableWrapTurnedOffAgainIsAsNeverOn(t *testing.T) {
	long := strings.Repeat("word ", 80)
	draw := func(set func(*BigTable)) string {
		pdf := testNewPDF()
		pages := testWrapTable(t, pdf, testHelvetica(pdf), false, []string{"No", "Notes"},
			[][]string{{"1", long}, {"2", "short"}}, set)
		return testContent(pages[0])
	}
	never := draw(nil)
	again := draw(func(table *BigTable) { table.SetWrapText(true).SetWrapText(false) })
	if never != again {
		t.Error("turned on and off, the table is not drawn as before")
	}
}
