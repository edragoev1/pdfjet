// review_layout_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The tests of the text, table and Markdown layout, from a review of them.

// testLowestBaseline returns the lowest baseline of the text the page draws,
// in the coordinates of the PDF, which grow upwards from the bottom of the page.
func testLowestBaseline(page *Page) float64 {
	lowest := float64(page.height)
	for _, m := range regexp.MustCompile(`[-0-9.]+ ([-0-9.]+) Td\n`).FindAllStringSubmatch(testContent(page), -1) {
		if y, err := strconv.ParseFloat(m[1], 64); err == nil && y < lowest {
			lowest = y
		}
	}
	return lowest
}

// testBigTableSeq returns the first count rows of testBigTableRows, or more
// rows like them.
func testBigTableSeq(count int) func(yield func([]string) bool) {
	return func(yield func([]string) bool) {
		for i := 0; i < count; i++ {
			if !yield([]string{fmt.Sprintf("n%d", i), fmt.Sprintf("City, %d", i), fmt.Sprintf("%d.5", i)}) {
				return
			}
		}
	}
}

func TestReviewLayoutTextFrameBreaksALongWordInLinearTime(t *testing.T) {
	// The rest of the word was measured whole for every row it was broken
	// into: 100,000 characters took 75 seconds.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	frame := NewTextFrame(font, []string{strings.Repeat("a", 200000)})
	frame.SetLocation(50, 50).(*TextFrame).SetWidth(400)
	start := time.Now()
	frame.DrawOn(NewPageDetached(pdf, letter.Portrait()))
	if took := time.Since(start); took > 3*time.Second {
		t.Errorf("took %v", took)
	}
	if frame.HasMoreText() {
		t.Error("the word is not all drawn")
	}
}

func TestReviewLayoutMarkdownQuotesNestedDeeplyKeepTheTextWide(t *testing.T) {
	// Quotes nested so deeply that the text was narrower than nothing drew one
	// character on each row.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	pages := []*Page{}
	start := time.Now()
	NewMarkdown(font, font, font, font, font).DrawOnPages(pdf, strings.Repeat(">", 20000), &pages, letter.Portrait())
	if took := time.Since(start); took > 3*time.Second {
		t.Errorf("took %v", took)
	}
	if len(pages) > 40 {
		t.Errorf("%d pages for 20,000 characters", len(pages))
	}
}

func TestReviewLayoutARunningSumIsAddedUpInLinearTime(t *testing.T) {
	// The sums were added up again from the first row for every page, with
	// the numbers read again: 40,000 rows took 33 seconds.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	rows := 40000
	data := testRows(font, rows, 2)
	for r := 1; r < rows; r++ {
		data[r][1].SetText("1,234.50")
	}
	table := NewTable().SetTableData(data, 1)
	table.SetNumberOfFooterRows(1)
	table.SetRunningSum(rows-1, 1, 2)
	table.SetLocation(20, 20)
	pages := []*Page{}
	start := time.Now()
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	if took := time.Since(start); took > 5*time.Second {
		t.Errorf("took %v", took)
	}
	// The footer row is the last row, which has no number of its own.
	if !strings.Contains(testContent(pages[len(pages)-1]), testHex("49,377,531.00")) {
		t.Error("the running sum of the last page is not the total")
	}
}

func TestReviewLayoutASumReadsANumberAsRightAlignNumbersDoes(t *testing.T) {
	// A number with a line break or a control character after it is a number
	// to isNumber, which trims them, and was 0 or refused by numberOf.
	for _, text := range []string{"100\n", "100\u0001", " 100 ", "(100)"} {
		value, ok := numberOf(text, 0)
		want := int64(100)
		if strings.Contains(text, "(") {
			want = -100
		}
		if !ok || value != want {
			t.Errorf("%q: want %d, got %d %v", text, want, value, ok)
		}
	}
}

func TestReviewLayoutARowSpanCountsTheRowsOfTheTable(t *testing.T) {
	// The span of a cell over a row that wraps to four lines was written as
	// the five rows of the drawing, and not as the two rows of the table.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	data := [][]*Cell{
		{NewCell(font, "H1"), NewCell(font, "H2")},
		{NewCell(font, "span").SetRowSpan(2), NewCell(font, "a note long enough to wrap to four lines")},
		{NewCell(font, ""), NewCell(font, "x")},
		{NewCell(font, "y"), NewCell(font, "z")},
	}
	table := NewTable().SetTableData(data, 1)
	table.SetColumnWidth(0, 60).SetColumnWidth(1, 60)
	table.SetLocation(20, 20)
	table.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	raw := string(doc.complete())
	testWant(t, "[/RowSpan 2]", fmt.Sprint(regexp.MustCompile(`/RowSpan \d+`).FindAllString(raw, -1)))
	testWant(t, "4", strconv.Itoa(strings.Count(raw, "/S /TR\n")))
}

func TestReviewLayoutARowThatDoesNotFitUnderAHeadingGoesToTheNextPage(t *testing.T) {
	// The first row of the first page was drawn where it was, past the bottom
	// of the page, when it did not fit under what was above the table.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	first := NewPageDetached(pdf, letter.Portrait())
	data := testRows(font, 5, 1)
	data[1][0].SetText("one two three four five six seven eight nine ten eleven twelve")
	table := NewTable().SetTableData(data, 1)
	table.SetColumnWidth(0, 60)
	table.SetLocation(20, 20)
	table.SetBottomMargin(20)
	table.SetFirstPageTopMargin(720)
	pages := []*Page{first}
	table.DrawOnPagesFrom(pdf, first, &pages, letter.Portrait())
	if strings.Contains(testContent(first), testHex("one")) {
		t.Error("the row is drawn on the first page")
	}
	if len(pages) != 2 || !strings.Contains(testContent(pages[1]), testHex("twelve")) {
		t.Errorf("%d pages, and the row is not on the second", len(pages))
	}
	for i, page := range pages {
		if lowest := testLowestBaseline(page); lowest < 20 {
			t.Errorf("page %d draws text at %v, under the bottom margin", i, lowest)
		}
	}
}

func TestReviewLayoutARowSpanTallerThanAPageIsCutBetweenItsRows(t *testing.T) {
	// A cell that spans rows taller than a page drew them all on one page,
	// past its bottom.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	data := testRows(font, 80, 2)
	data[3][0].SetRowSpan(70)
	table := NewTable().SetTableData(data, 1)
	table.SetLocation(20, 20)
	table.SetBottomMargin(20)
	pages := []*Page{}
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	if len(pages) < 2 {
		t.Fatalf("%d pages", len(pages))
	}
	drawn := 0
	for i, page := range pages {
		if lowest := testLowestBaseline(page); lowest < 20 {
			t.Errorf("page %d draws text at %v, under the bottom margin", i, lowest)
		}
		content := testContent(page)
		for r := 1; r < 80; r++ {
			drawn += strings.Count(content, "<"+testHex(fmt.Sprintf("r%dc1", r))+">")
		}
	}
	testWant(t, "79 rows", fmt.Sprintf("%d rows", drawn))
	// The spanning cell draws its text once, and its box on each page.
	count := 0
	for _, page := range pages {
		count += strings.Count(testContent(page), "<"+testHex("r3c0")+">")
	}
	testWant(t, "1", strconv.Itoa(count))
}

func TestReviewLayoutATableWithNoRowsLeftEndsWhereItStarts(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	table := NewTable().SetTableData(testRows(font, 3, 2), 1)
	table.SetLocation(20, 30)
	pages := []*Page{}
	table.DrawOnPages(pdf, &pages, letter.Portrait())
	xy := table.DrawOnPages(pdf, &pages, letter.Portrait())
	testWant(t, "1 page", fmt.Sprintf("%d page", len(pages)))
	testNear(t, "x", 20+table.GetWidth(), xy[0], testDelta)
	testNear(t, "y", 30, xy[1], testDelta)
}

func TestReviewLayoutAColumnSpanIsWithinTheRow(t *testing.T) {
	// A column span of 0 hung the drawing, and one past the end of the row
	// ran past the cells of the row.
	for _, colspan := range []int{0, -3, 5} {
		pdf := testNewPDF()
		font := testHelvetica(pdf)
		data := testRows(font, 3, 3)
		for _, row := range data {
			for _, cell := range row {
				cell.SetBorders(true)
			}
		}
		data[1][1].SetColSpan(colspan)
		table := NewTable().SetTableData(data, 1)
		table.SetLocation(20, 20)
		table.DrawOn(NewPage(pdf, letter.Portrait()))
		if colspan < 1 && data[1][1].GetColSpan() != 1 {
			t.Errorf("column span %d: %d", colspan, data[1][1].GetColSpan())
		}
	}
}

func TestReviewLayoutAListItemGoesOnInTheNextFrameWithoutItsLabel(t *testing.T) {
	// The label of an item was drawn again, as a new item, in every frame the
	// item went on into.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	item := NewParagraph().Add(NewTextLine(font, strings.Repeat("word ", 400)))
	item.SetListLabel(NewTextLine(font, "LABEL"), 15)
	frame := NewTextFrameFromParagraphs([]*Paragraph{item})
	frame.SetLocation(50, 50).(*TextFrame).SetWidth(200).SetHeight(200)
	pages := []*Page{}
	frame.DrawOnPages(doc.pdf, &pages, letter.Portrait())
	if len(pages) < 2 {
		t.Fatalf("%d pages", len(pages))
	}
	labels := 0
	for _, page := range pages {
		labels += strings.Count(testContent(page), testHex("LABEL"))
	}
	testWant(t, "1 label", fmt.Sprintf("%d label", labels))
	doc.pdf.AddPages(pages)
	raw := string(doc.complete())
	testWant(t, "1 Lbl", fmt.Sprintf("%d Lbl", strings.Count(raw, "/S /Lbl\n")))
	testWant(t, fmt.Sprintf("%d LBody", len(pages)), fmt.Sprintf("%d LBody", strings.Count(raw, "/S /LBody\n")))
}

func TestReviewLayoutAHeadingThatGoesOnInTheNextFrameIsOneBookmark(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	heading := NewParagraph().Add(NewTextLine(font, strings.Repeat("heading ", 200)))
	heading.SetStructureType(structelem.H1)
	frame := NewTextFrameFromParagraphs([]*Paragraph{heading})
	frame.SetLocation(50, 50).(*TextFrame).SetWidth(200).SetHeight(100)
	pages := []*Page{}
	frame.DrawOnPages(doc.pdf, &pages, letter.Portrait())
	if len(pages) < 2 {
		t.Fatalf("%d pages", len(pages))
	}
	testWant(t, "1 heading", fmt.Sprintf("%d heading", len(doc.pdf.headings)))
}

func TestReviewLayoutATextColumnDrawsTheLabelOfAListItem(t *testing.T) {
	// The label that Paragraph.SetListLabel sets was left out by TextColumn.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	column := NewTextColumn()
	column.SetLocation(50, 50)
	column.SetWidth(300)
	for _, text := range []string{"first item", "second item"} {
		item := NewParagraph().Add(NewTextLine(font, text))
		item.SetListLabel(NewTextLine(font, "LABEL"), 15)
		column.AddParagraph(item)
	}
	column.AddParagraph(NewParagraph().Add(NewTextLine(font, "after the list")))
	column.DrawOn(page)
	content := testContent(page)
	testWant(t, "2 labels", fmt.Sprintf("%d labels", strings.Count(content, testHex("LABEL"))))
	label := testPositionOf(t, content, "LABEL")
	text := testPositionOf(t, content, "first")
	testNear(t, "label x", 35, label[0], testDelta)
	testNear(t, "label y", text[1], label[1], testDelta)
	raw := string(doc.complete())
	testWant(t, "1 L, 2 LI, 2 Lbl, 2 LBody, 3 P", fmt.Sprintf("%d L, %d LI, %d Lbl, %d LBody, %d P",
		strings.Count(raw, "/S /L\n"), strings.Count(raw, "/S /LI\n"), strings.Count(raw, "/S /Lbl\n"),
		strings.Count(raw, "/S /LBody\n"), strings.Count(raw, "/S /P\n")))
}

func TestReviewLayoutATextFrameInTheBottomHalfOfThePageNeedsAHeight(t *testing.T) {
	// The height the frame took on each page was less than nothing, so it
	// drew all of its text on the first page, past the bottom.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	frame := NewTextFrame(font, []string{strings.Repeat("word ", 1000)})
	frame.SetLocation(50, 500).(*TextFrame).SetWidth(200)
	pages := []*Page{}
	frame.DrawOnPages(pdf, &pages, letter.Portrait())
	testWant(t, "0 pages", fmt.Sprintf("%d pages", len(pages)))
	testRecorded(t, pdf, "The text frame has no height and is in the bottom half of the page: "+
		"set its height, or put it higher on the page.")
}

func TestReviewLayoutARecordOfManyLinesIsReadInLinearTime(t *testing.T) {
	// Every line closes a quoted field and opens the next, and the record was
	// split again at each line.
	var text strings.Builder
	text.WriteString(`"a`)
	for i := 0; i < 5000; i++ {
		text.WriteString("\n" + strings.Repeat("x", 1000) + `","`)
	}
	text.WriteString("\nend\"")
	start := time.Now()
	fields := testFirstRecord(text.String())
	if took := time.Since(start); took > 3*time.Second {
		t.Errorf("took %v", took)
	}
	testWant(t, "5001 fields", fmt.Sprintf("%d fields", len(fields)))
	testWant(t, "a "+strings.Repeat("x", 1000), fields[0])
	testWant(t, " end", fields[5000])
	// A quote after the delimiter opens a field, and one inside a field that
	// does not start with one is text.
	testWant(t, `[a b c"d e f]`, fmt.Sprint(testFirstRecord("\"a\nb\",c\"d,\"e\nf\"")))
}

func TestReviewLayoutABigTableReturnsTheErrorOfAQuoteThatIsNotClosed(t *testing.T) {
	// The quote that is not closed panicked, and the table returns an error.
	path := filepath.Join(t.TempDir(), "open.csv")
	if err := os.WriteFile(path, []byte("A,B\n1,\"2\n3,4\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	_, err := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(2).SetTableData(path, ",")
	if err == nil || err.Error() != "A quoted field is not closed by the end of the data file: 1,\"2\n3,4" {
		t.Errorf("error: %v", err)
	}
}

func TestReviewLayoutTheLinesOfADataFileEndAtACarriageReturnToo(t *testing.T) {
	// A carriage return alone ended a line in Java and C#, and not in Go and
	// Swift.
	path := filepath.Join(t.TempDir(), "cr.csv")
	if err := os.WriteFile(path, []byte("A,B\r1,2\r\n3,4\n5,6\r"), 0o644); err != nil {
		t.Fatal(err)
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	table := NewTableFromFile(font, font, path)
	testWant(t, "4 rows", fmt.Sprintf("%d rows", len(table.tableData)))
	testWant(t, "5 6", table.tableData[3][0].text+" "+table.tableData[3][1].text)
	var rows []string
	err := scanDataFile(path, ",", func(fields []string) bool {
		rows = append(rows, strings.Join(fields, " "))
		return true
	})
	if err != nil {
		t.Fatal(err)
	}
	testWant(t, "[A B 1 2 3 4 5 6]", fmt.Sprint(rows))
}

func TestReviewLayoutABigTableThatDoesNotFitOnItsFirstPageStartsOnTheNext(t *testing.T) {
	// The header and the first row were drawn wherever the first page had
	// them start, even under its bottom margin.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	first := NewPage(doc.pdf, letter.Portrait())
	table := NewBigTable(doc.pdf, font, font, letter.Portrait()).SetNumberOfColumns(3).
		SetTableRows(testBigTableHeader, testBigTableSeq(5))
	table.SetFirstPage(first, first.height-15)
	pages := testDrawBigTable(t, table)
	if len(pages) != 1 || pages[0] == first {
		t.Fatalf("%d pages, starting on the first", len(pages))
	}
	if strings.Contains(testContent(first), testHex(testBigTableHeader[0])) {
		t.Error("the header is drawn on the first page")
	}
	if !strings.Contains(testContent(pages[0]), testHex("Page 1 of 1")) {
		t.Error("the page is not page 1 of 1")
	}
}

func TestReviewLayoutABigTableCountsItsFirstPageAtItsOwnHeight(t *testing.T) {
	// The pages were counted as if the first were of the size of the next.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	first := NewPage(doc.pdf, letter.Landscape())
	table := NewBigTable(doc.pdf, font, font, letter.Portrait()).SetNumberOfColumns(3).
		SetTableRows(testBigTableHeader, testBigTableSeq(200))
	table.SetFirstPage(first, 100)
	pages := testDrawBigTable(t, table)
	want := fmt.Sprintf("Page %d of %d", len(pages), len(pages))
	if !strings.Contains(testContent(pages[len(pages)-1]), testHex(want)) {
		t.Errorf("the last page is not %q", want)
	}
}

func TestReviewLayoutATextBlockWithALeadingOfZeroDrawsItsLines(t *testing.T) {
	// The lines that fit were the height divided by a leading of 0.
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	block := NewTextBlock(font, "one two three four five six seven eight nine ten")
	block.SetWidth(40).SetHeight(30).SetLineSpacing(0)
	lines, _, _, _ := block.layout()
	if len(lines) < 5 {
		t.Errorf("%d lines", len(lines))
	}
	testWant(t, "2147483647 0 -2147483648 3", fmt.Sprintf("%d %d %d %d", saturatingInt(1/zeroFloat),
		saturatingInt(zeroFloat/zeroFloat), saturatingInt(-1/zeroFloat), saturatingInt(3.9)))
}

// zeroFloat is 0, as a variable, so that 0/0 is not a constant expression.
var zeroFloat float32

func TestReviewLayoutMarkdownCodeInAFontOfSizeZeroIsDrawn(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	code := NewCoreFont(pdf, corefont.Courier()).SetSize(0)
	pages := []*Page{}
	NewMarkdown(font, font, font, font, code).DrawOnPages(pdf, "```\none\ntwo\n```", &pages, letter.Portrait())
	testWant(t, "1 page", fmt.Sprintf("%d page", len(pages)))
}

func TestReviewLayoutMarkdownReadsTheSourceOfAnImageTheSameWayInEveryPort(t *testing.T) {
	images := testRepoPath(t, "images")
	markdown := NewMarkdown(nil, nil, nil, nil, nil).SetImageDirectory(images)
	for source, read := range map[string]bool{
		"linux-logo.png":             true,
		"./linux-logo.png":           true,
		".//linux-logo.png":          true,
		"linux-logo.png/":            false,
		"linux-logo.png/.":           false,
		"../images/linux-logo.png":   false,
		"x/../linux-logo.png":        false,
		"":                           false,
		".":                          false,
		"\u0301/../linux-logo.png":   false,
		"/\u0301linux-logo.png":      false,
		"c:linux-logo.png":           false,
		"linux-logo.png\\..\\x.png":  false,
		"%2E%2E/images/linux-logo.p": false,
	} {
		if got := markdown.imagePath(source) != ""; got != read {
			t.Errorf("%q: read %v", source, got)
		}
	}
	// An empty directory is the working directory.
	t.Chdir(images)
	if NewMarkdown(nil, nil, nil, nil, nil).SetImageDirectory("").imagePath("linux-logo.png") == "" {
		t.Error("the image is not read from the working directory")
	}
}
