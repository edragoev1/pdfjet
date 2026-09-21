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
	"reflect"
	"regexp"
	"slices"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
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

// testDrawSmallBigTable draws a table of the first five rows on one page, after
// the change, and returns the content of the page.
func testDrawSmallBigTable(t *testing.T, pdf *PDF, change func(*BigTable)) string {
	t.Helper()
	font := testHelvetica(pdf)
	table := NewBigTable(pdf, font, font, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(testBigTableRows()[:5]))
	change(table)
	pages := testDrawBigTable(t, table)
	if len(pages) != 1 {
		t.Fatalf("pages: %d", len(pages))
	}
	return testContent(pages[0])
}

func TestBigTableTheShadingAndTheBorderColorsCanBeChangedOrLeftOut(t *testing.T) {
	defaults := testDrawSmallBigTable(t, testNewPDF(), func(*BigTable) {})
	if !strings.Contains(defaults, "0.94 0.94 0.94 rg\n") || !strings.Contains(defaults, "0.69 0.69 0.69 RG\n") {
		t.Error("the default colors are missing")
	}
	if !strings.Contains(defaults, " re\nf\n") { // A shaded row is one rectangle
		t.Error("the shading is not a rectangle")
	}

	colored := testDrawSmallBigTable(t, testNewPDF(), func(table *BigTable) {
		table.SetShadingColor(0xFF0000).SetBorderColorRGB([3]float32{0, 0, 1})
	})
	if !strings.Contains(colored, "1 0 0 rg\n") || !strings.Contains(colored, "0 0 1 RG\n") {
		t.Error("the colors that were set are missing")
	}
	if strings.Contains(colored, "0.94 0.94 0.94 rg") || strings.Contains(colored, "0.69 0.69 0.69 RG") {
		t.Error("the default colors are still drawn")
	}

	plain := testDrawSmallBigTable(t, testNewPDF(), func(table *BigTable) {
		table.SetShadingColor(color.Transparent).SetBorderColor(color.Transparent)
	})
	if strings.Contains(plain, "\nf\n") || strings.Contains(plain, "\nS\n") {
		t.Error("shading or lines are drawn")
	}
	if !strings.Contains(plain, testHex("n4")) {
		t.Error("the last row was not drawn")
	}
}

func TestBigTableThePaddingCanBeSetAfterTheData(t *testing.T) {
	content := testDrawSmallBigTable(t, testNewPDF(), func(table *BigTable) { table.SetPadding(10) })
	if !strings.Contains(content, "BT\n20 ") { // The location is 10, 10
		t.Errorf("content %q", content)
	}
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	NewBigTable(pdf, font, font, letter.Portrait()).SetPadding(-1)
	testRecorded(t, pdf, "The padding cannot be negative.")
}

func TestBigTableTheFooterCanHaveItsOwnTextAndFontOrBeLeftOut(t *testing.T) {
	pdf := testNewPDF()
	big := testHelvetica(pdf)
	big.SetSize(20)
	custom := testDrawSmallBigTable(t, pdf, func(table *BigTable) { table.SetFooter("{page}/{pages}", big) })
	if !strings.Contains(custom, testHex("1/1")) || !strings.Contains(custom, " 20 Tf\n") {
		t.Error("the footer text or font is missing")
	}

	none := testDrawSmallBigTable(t, testNewPDF(), func(table *BigTable) { table.SetFooter("", nil) })
	defaults := testDrawSmallBigTable(t, testNewPDF(), func(*BigTable) {})
	if !strings.HasPrefix(defaults, none) || len(defaults) <= len(none) {
		t.Error("the footer was not left out")
	}
}

func TestBigTableLineBreaksInFieldsAreDrawnAsSpaces(t *testing.T) {
	path := filepath.Join(t.TempDir(), "breaks.csv")
	data := "Name,City,Total\n\"n\n0\",\"City\r\n0\",1\nn1,City 1,2\n"
	if err := os.WriteFile(path, []byte(data), 0o644); err != nil {
		t.Fatal(err)
	}
	pdf1 := testNewPDF()
	font1 := testHelvetica(pdf1)
	table1, err := NewBigTable(pdf1, font1, font1, letter.Portrait()).SetNumberOfColumns(3).SetTableData(path, ",")
	if err != nil {
		t.Fatal(err)
	}
	fromFile := testDrawBigTable(t, table1)

	pdf2 := testNewPDF()
	font2 := testHelvetica(pdf2)
	rows := [][]string{{"n\r0", "City\r\n0", "1"}, {"n1", "City 1", "2"}}
	fromMemory := testDrawBigTable(t, NewBigTable(pdf2, font2, font2, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(rows)))

	pdf3 := testNewPDF()
	font3 := testHelvetica(pdf3)
	spaces := [][]string{{"n 0", "City 0", "1"}, {"n1", "City 1", "2"}}
	withSpaces := testDrawBigTable(t, NewBigTable(pdf3, font3, font3, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(spaces)))

	if len(fromFile) != 1 {
		t.Fatalf("pages %d", len(fromFile))
	}
	if testContent(fromFile[0]) != testContent(withSpaces[0]) || testContent(fromMemory[0]) != testContent(withSpaces[0]) {
		t.Error("a line break is not drawn as a space")
	}
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

func TestBigTableIsTaggedAsATableInAPDFUADocument(t *testing.T) {
	// The table is one Table element over all its pages: a TR for each row,
	// and a TH for each header field the first time the header is drawn or a
	// TD for each field of a row, each holding the text of its cell.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	table := NewBigTable(doc.pdf, font, font, letter.Portrait()).SetNumberOfColumns(3).
		SetTableRows(testBigTableHeader, slices.Values(testBigTableRows()))
	pages := testDrawBigTable(t, table)
	if len(pages) != 2 {
		t.Fatalf("%d pages", len(pages))
	}
	// The header that repeats on the second page is an artifact, and the
	// shading and the lines of every page are artifacts too.
	content := testContent(pages[1])
	if strings.Index(content, testHex("Name")) > strings.Index(content, "BDC\n") {
		t.Error("the header row on the second page is not an artifact")
	}
	if strings.Index(content, "/Artifact BMC\n") > strings.Index(content, testHex("Name")) {
		t.Error("the header row on the second page is drawn before any artifact begins")
	}
	raw := string(doc.complete())
	counts := map[string]int{
		"/S /Table\n": 1,
		// The 100 rows of the data, and the header row of the first page.
		"/S /TR\n": 101,
		"/S /TH\n": 3,
		"/S /TD\n": 300,
		// A cell holds the text itself and has no paragraph under it.
		"/S /P\n":                         0,
		"/A <</O /Table /Scope /Column>>": 3,
	}
	for text, want := range counts {
		if got := strings.Count(raw, text); got != want {
			t.Errorf("%q is in the PDF %d times, not %d", text, got, want)
		}
	}
}

func TestBigTableIsNotTaggedInADocumentThatIsNotPDFUA(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	testDrawBigTable(t, NewBigTable(doc.pdf, font, font, letter.Portrait()).
		SetNumberOfColumns(3).SetTableRows(testBigTableHeader, slices.Values(testBigTableRows())))
	raw := string(doc.complete())
	for _, text := range []string{"/S /Table\n", "/S /TR\n", "BDC\n", "/Artifact BMC\n"} {
		if strings.Contains(raw, text) {
			t.Errorf("a document that is not PDF/UA has %q", text)
		}
	}
}

// testTextPositions returns the x coordinate and the length of the text the
// page draws, in the order it draws it.
func testTextPositions(page *Page) [][2]float64 {
	var out [][2]float64
	re := regexp.MustCompile(`([-0-9.]+) [-0-9.]+ Td\n(?:/F\d+ [0-9.]+ Tf\n)?\[<([0-9A-Fa-f]*)>\] TJ`)
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		x, err := strconv.ParseFloat(m[1], 64)
		if err != nil {
			continue
		}
		out = append(out, [2]float64{x, float64(len(m[2]) / 2)})
	}
	return out
}

func TestBigTableTheAlignmentOfAColumnTheTableDoesNotHaveIsRefused(t *testing.T) {
	// The alignment of a column was written into the slice of them whatever
	// the column was, and there is no slice before SetTableRows.
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	table := NewBigTable(doc.pdf, font, font, letter.Portrait()).SetNumberOfColumns(2)
	table.SetTextAlignment(0, alignment.Right)
	table.SetTableRows([]string{"A", "B"}, slices.Values([][]string{{"a", "b"}}))
	table.SetTextAlignment(1, alignment.Right)
	table.SetTextAlignment(2, alignment.Right)
	err := doc.pdf.Complete()
	if err == nil {
		t.Fatal("a table with no column 0 or 2 was completed")
	}
	testWant(t, "The PDF was not completed because of an earlier error: "+
		"The table has no column 0: set the alignment of a column after SetTableData.",
		err.Error())
}

func TestBigTableAColumnIsAsWideAsTheFontOfEachRowDrawsIt(t *testing.T) {
	// The columns were measured with the header font whatever font a row was
	// drawn with, so a body font wider than the header font ran over the
	// column on its right.
	doc := testNewDoc()
	f1 := NewCoreFont(doc.pdf, corefont.HelveticaBold()).SetSize(8)
	f2 := NewCoreFont(doc.pdf, corefont.Helvetica()).SetSize(14)
	table := NewBigTable(doc.pdf, f1, f2, letter.Portrait())
	table.SetNumberOfColumns(2)
	table.SetTableRows([]string{"A", "B"}, slices.Values([][]string{{"wwww", "xx"}}))
	table.SetLocation(50, 50)
	table.SetFooter("", nil)
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	positions := testTextPositions(table.GetPages()[0])
	if len(positions) != 4 {
		t.Fatalf("%d strings drawn", len(positions))
	}
	// The header "A" ends before "B" starts, and so does the row under it.
	if float64(f1.StringWidth(f1.size, "A"))+positions[0][0] > positions[1][0] {
		t.Error("the header runs into the next column")
	}
	if width := float64(f2.StringWidth(f2.size, "wwww")); positions[2][0]+width > positions[3][0] {
		t.Errorf("the row runs into the next column: %g + %g > %g",
			positions[2][0], width, positions[3][0])
	}
}

// testTooWideBigTable returns a table of one column of the given text, drawn
// on a letter page in a font wide enough that the text may not fit it.
func testTooWideBigTable(t *testing.T, pdf *PDF, font *Font, text string) *BigTable {
	t.Helper()
	table := NewBigTable(pdf, font, font, letter.Portrait()).SetNumberOfColumns(1)
	table.SetTableRows([]string{"Header"}, slices.Values([][]string{{text}}))
	table.SetLocation(0, 20)
	table.SetFooter("", nil)
	if err := table.Complete(); err != nil {
		t.Fatal(err)
	}
	return table
}

// testDrawnText returns the strings the page draws, in the order they are
// drawn. A core font draws a character as the one byte of its code.
func testDrawnText(page *Page) []string {
	re := regexp.MustCompile(`\[<([0-9A-Fa-f]*)>\] TJ`)
	out := make([]string, 0)
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		text := make([]byte, 0, len(m[1])/2)
		for i := 0; i+1 < len(m[1]); i += 2 {
			b, _ := strconv.ParseUint(m[1][i:i+2], 16, 8)
			text = append(text, byte(b))
		}
		out = append(out, string(text))
	}
	return out
}

func TestBigTableATableTooWideForItsPageIsCutBackToItAndSaysSo(t *testing.T) {
	// The columns of a table are as wide as their widest field, which can come
	// to more than the page holds; the last of them were drawn off the right
	// edge, where they are lost. They are cut back to the page now, and a
	// field that was cut ends in " ..." to say it was.
	doc := testNewDoc()
	font := NewCoreFont(doc.pdf, corefont.Helvetica())
	font.SetSize(24.0)
	text := "A string that is far too long to fit across the width of a letter page"
	table := testTooWideBigTable(t, doc.pdf, font, text)
	page := table.GetPages()[0]
	drawn := testDrawnText(page)
	if len(drawn) != 2 {
		t.Fatalf("the page drew %v", drawn)
	}
	if !strings.HasSuffix(drawn[1], " ...") {
		t.Fatalf("the field does not say it was cut: %q", drawn[1])
	}
	if !strings.HasPrefix(text, strings.TrimSuffix(drawn[1], " ...")) {
		t.Errorf("the text drawn is not the start of the text: %q", drawn[1])
	}
	// What is drawn fits the page, which the whole of the text does not.
	if font.StringWidth(font.size, text) <= letter.Portrait().GetWidth() {
		t.Fatal("the text fits the page")
	}
	if width := font.StringWidth(font.size, drawn[1]); width > letter.Portrait().GetWidth() {
		t.Errorf("the text drawn does not fit the page: %v", width)
	}
	// And the vertical lines of the table are on the page with it.
	re := regexp.MustCompile(`([-0-9.]+) [-0-9.]+ m`)
	for _, m := range re.FindAllStringSubmatch(testContent(page), -1) {
		x, _ := strconv.ParseFloat(m[1], 64)
		if x > float64(letter.Portrait().GetWidth()) {
			t.Errorf("a line of the table is off the page at x=%v", x)
		}
	}
}

func TestBigTableATableThatFitsItsPageIsNotCut(t *testing.T) {
	// Nothing is measured or cut when the columns fit, and no field of a table
	// that fits ends in the mark of one that was cut.
	doc := testNewDoc()
	font := NewCoreFont(doc.pdf, corefont.Helvetica())
	font.SetSize(24.0)
	text := "Short enough"
	table := testTooWideBigTable(t, doc.pdf, font, text)
	if drawn := testDrawnText(table.GetPages()[0]); !reflect.DeepEqual(drawn, []string{"Header", text}) {
		t.Errorf("the page drew %v", drawn)
	}
}

func TestBigTableTheCellOfACutFieldKeepsTheWholeOfItsText(t *testing.T) {
	// A field the page was too narrow for is drawn cut, but a reader is read
	// the whole of it: the cell of the structure tree keeps it.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := NewCoreFont(doc.pdf, corefont.Helvetica())
	font.SetSize(24.0)
	text := "A string that is far too long to fit across the width of a letter page"
	table := testTooWideBigTable(t, doc.pdf, font, text)
	// The content of a page is written out and let go by Complete().
	drawn := testDrawnText(table.GetPages()[0])
	raw := string(doc.complete())
	// An Alt is written as the UTF-16 of the text, after a byte order mark.
	var alt strings.Builder
	alt.WriteString("feff")
	for _, r := range text {
		alt.WriteString(fmt.Sprintf("%04x", r))
	}
	if !strings.Contains(raw, "/Alt <"+alt.String()+">") {
		t.Error("the cell does not keep the whole of the text")
	}
	if !strings.HasSuffix(drawn[1], " ...") || drawn[1] == text {
		t.Errorf("the field was not drawn cut: %q", drawn[1])
	}
}
