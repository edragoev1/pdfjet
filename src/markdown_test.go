// markdown_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"github.com/edragoev1/pdfjet/v9/src/color"
	"regexp"
	"slices"
	"strconv"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testMarkdownDrawn is a Markdown text drawn in a PDF/UA document: its pages
// and the whole PDF.
type testMarkdownDrawn struct {
	pages    []*Page
	pdf      string
	text     string   // The text of the pages, from their content streams
	contents []string // Of each page, before it is written
}

func testDrawMarkdown(text string, imageDirectory string) *testMarkdownDrawn {
	return testDrawMarkdownWith(text, imageDirectory, func(*Markdown) {})
}

// testDrawMarkdownWith draws the Markdown after the change, as an option set
// on it.
func testDrawMarkdownWith(text string, imageDirectory string, change func(*Markdown)) *testMarkdownDrawn {
	doc := testNewDoc()
	pdf := doc.pdf
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	pdf.SetTitle("Markdown")
	regular := NewCoreFont(pdf, corefont.Helvetica())
	bold := NewCoreFont(pdf, corefont.HelveticaBold())
	italic := NewCoreFont(pdf, corefont.HelveticaOblique())
	boldItalic := NewCoreFont(pdf, corefont.HelveticaBoldOblique())
	code := NewCoreFont(pdf, corefont.Courier())
	markdown := NewMarkdown(regular, bold, italic, boldItalic, code)
	if imageDirectory != "" {
		markdown.SetImageDirectory(imageDirectory)
	}
	change(markdown)
	drawn := &testMarkdownDrawn{pages: make([]*Page, 0)}
	markdown.DrawOnPages(pdf, text, &drawn.pages, letter.Portrait())
	var content strings.Builder
	for _, page := range drawn.pages {
		drawn.contents = append(drawn.contents, testContent(page))
		content.WriteString(testContent(page))
	}
	drawn.text = content.String()
	if len(drawn.pages) > 0 {
		pdf.AddPages(drawn.pages)
		drawn.pdf = string(doc.complete())
	}
	return drawn
}

func testCountStructures(pdf, structure string) int {
	return strings.Count(pdf, "/S /"+structure+"\n")
}

func testAssertStructures(t *testing.T, drawn *testMarkdownDrawn, cases ...any) {
	t.Helper()
	for i := 0; i+1 < len(cases); i += 2 {
		structure := cases[i].(string)
		want := cases[i+1].(int)
		if got := testCountStructures(drawn.pdf, structure); got != want {
			t.Errorf("%d %s, want %d", got, structure, want)
		}
	}
}

func TestMarkdownAnEmptyTextNeedsNoPage(t *testing.T) {
	for _, text := range []string{"", "\n  \n"} {
		if n := len(testDrawMarkdown(text, "").pages); n != 0 {
			t.Errorf("%q: %d pages", text, n)
		}
	}
}

func TestMarkdownEveryBlockIsTaggedForPDFUA(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown("# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n"+
		"```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next", "")
	if len(drawn.pages) != 1 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	testAssertStructures(t, drawn, "H1", 1, "H2", 1, "L", 1, "LI", 2, "Lbl", 2, "LBody", 2,
		"BlockQuote", 1, "Code", 1, "Table", 1)
}

func TestMarkdownTasksAreLabeledByTheirBoxes(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown("- [x] done\n- [ ] open\n- plain", "")
	testAssertStructures(t, drawn, "L", 1, "LI", 3, "Lbl", 3, "LBody", 3)
	// The boxes are read as ☑ and ☐, in UTF-16BE, their actual text and their
	// description, the plain item by its bullet
	for _, actualText := range []string{"<feff2611>", "<feff2610>"} {
		if strings.Count(drawn.pdf, "/ActualText "+actualText) != 1 || strings.Count(drawn.pdf, "/Alt "+actualText) != 1 {
			t.Errorf("no /ActualText %s", actualText)
		}
	}
}

// color.Transparent leaves the color of a done task's box green: it filled
// the box with the color last set, black (review of 15 October 2026).
func TestMarkdownATransparentCheckColorKeepsTheGreen(t *testing.T) {
	font := testPlexSans(testNewPDF())
	markdown := NewMarkdown(font, font, font, font, font).SetCheckColor(color.Transparent)
	if markdown.checkColor != 0x1A7F37 {
		t.Errorf("the check color is %06X", markdown.checkColor)
	}
}

func TestMarkdownHeadingLevelsSkipNone(t *testing.T) {
	testAllowCoreFonts(t)
	// A text that starts at ### and goes on to ##### is H1, then H2.
	drawn := testDrawMarkdown("### Three\n\n##### Five\n\n# One", "")
	testAssertStructures(t, drawn, "H1", 2, "H2", 1, "H3", 0)
}

func TestMarkdownTheTextFlowsOntoAsManyPagesAsItNeeds(t *testing.T) {
	testAllowCoreFonts(t)
	var text strings.Builder
	text.WriteString("# A long text\n\n")
	for i := 0; i < 150; i++ {
		text.WriteString("Paragraph p" + strconv.Itoa(1000+i) +
			" has words enough to take a line or two of the page.\n\n")
	}
	drawn := testDrawMarkdown(text.String(), "")
	if len(drawn.pages) < 3 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	for i := 0; i < 150; i++ {
		word := "p" + strconv.Itoa(1000+i)
		if n := strings.Count(drawn.text, testHex(word)); n != 1 {
			t.Errorf("%s is drawn %d times", word, n)
		}
	}
	// No text is drawn under the bottom margin of 72 points.
	for _, m := range regexp.MustCompile(`[-0-9.]+ ([-0-9.]+) Td\n`).FindAllStringSubmatch(drawn.text, -1) {
		y, _ := strconv.ParseFloat(m[1], 32)
		if y < 72 {
			t.Errorf("a baseline at y = %s", m[1])
		}
	}
}

func TestMarkdownAListOverPagesIsOneList(t *testing.T) {
	testAllowCoreFonts(t)
	var text strings.Builder
	for i := 0; i < 80; i++ {
		text.WriteString("- item " + strconv.Itoa(i) + "\n")
	}
	drawn := testDrawMarkdown(text.String(), "")
	if len(drawn.pages) < 2 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	testAssertStructures(t, drawn, "L", 1, "LI", 80)
}

// testFontBefore returns the font selected last before the text is drawn: the
// Tf of its line.
func testFontBefore(t *testing.T, content, text string) string {
	t.Helper()
	at := strings.Index(content, "<"+testHex(text)+">")
	if at < 0 {
		t.Fatalf("%s is not drawn", text)
	}
	fonts := regexp.MustCompile(`/F\d+ [\d.]+ Tf`).FindAllString(content[:at], -1)
	if len(fonts) == 0 {
		return ""
	}
	return fonts[len(fonts)-1]
}

// A whole cell in bold is drawn in the bold font, as the header is, so that a
// table can mark a value; a cell of mixed emphasis stays regular.
func TestMarkdownATableCellInOneEmphasisIsDrawnInItsFont(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown("| N | V |\n|---|---:|\n| a | 15 |\n| b | **25** |\n| c | **7** 8 |", "")
	content := drawn.contents[0]
	header := testFontBefore(t, content, "V")
	if got := testFontBefore(t, content, "25"); got != header {
		t.Errorf("the bold cell is in %q, the header in %q", got, header)
	}
	plain := testFontBefore(t, content, "15")
	if plain == header {
		t.Errorf("the plain cell is in the header's font %q", plain)
	}
	if got := testFontBefore(t, content, "7 8"); got != plain {
		t.Errorf("the cell of mixed emphasis is in %q, not the regular %q", got, plain)
	}
}

// A cell that starts with a check mark is green, as a table of features marks
// what each has; a header and other cells are not.
func TestMarkdownATableCellThatStartsWithACheckMarkIsInTheCheckColor(t *testing.T) {
	testAllowCoreFonts(t)
	green := testFillOf(0x1A7F37)
	red := testFillOf(0xFF0000)
	table := "| Feature | \u2713 |\n|---|---|\n| Tables | \u2713 |\n| SVG | \u2713 with a module |\n| Charts | no |"
	content := testDrawMarkdown(table, "").contents[0]
	if n := strings.Count(content, green); n != 2 {
		t.Errorf("%d cells are green, not the two that start with a check mark", n)
	}
	if strings.Contains(testDrawMarkdown("| Feature | A |\n|---|---|\n| Tables | yes |", "").contents[0], green) {
		t.Error("a cell with no check mark is green")
	}
	// The color is the check color, which colors the task boxes too.
	content = testDrawMarkdownWith(table, "", func(markdown *Markdown) {
		markdown.SetCheckColor(0xFF0000)
	}).contents[0]
	if !strings.Contains(content, red) || strings.Contains(content, green) {
		t.Error("the cells are not in the check color that was set")
	}
}

// testFillOf returns how a fill color is written: the content of a page with
// only that color set.
func testFillOf(c int32) string {
	page := NewPageDetached(testNewPDF(), letter.Portrait())
	page.SetBrushColor(c)
	return testContent(page)
}

// testFillerAndTable returns the text of k short paragraphs, p0, p1 and so on,
// then the lead-in, then a table of the rows, the cells of row r named r1, r2
// and so on: words of no letters that the core fonts kern, so that each is
// drawn whole.
func testFillerAndTable(k int, leadIn string, rows int) string {
	var text strings.Builder
	for i := 0; i < k; i++ {
		text.WriteString("p" + strconv.Itoa(i) + "\n\n")
	}
	if leadIn != "" {
		text.WriteString(leadIn + "\n\n")
	}
	text.WriteString("| N | V |\n|---|---|\n")
	for r := 1; r <= rows; r++ {
		text.WriteString("| r" + strconv.Itoa(r) + " | v" + strconv.Itoa(r) + " |\n")
	}
	return text.String()
}

// testMarkdownPagesOf returns the pages, counted from 0, that draw the text.
func testMarkdownPagesOf(drawn *testMarkdownDrawn, text string) []int {
	pages := []int{}
	for i, content := range drawn.contents {
		if strings.Contains(content, testHex(text)) {
			pages = append(pages, i)
		}
	}
	return pages
}

// Whatever the text before it, the table of eight rows is on one page, its
// first row and its last; at some length of the text it is moved to the next
// page, which shows the rule at work.
func TestMarkdownATableThatFitsOnAPageIsNotSplit(t *testing.T) {
	testAllowCoreFonts(t)
	moved := false
	for k := 0; k <= 60; k++ {
		drawn := testDrawMarkdown(testFillerAndTable(k, "", 8), "")
		first, last := testMarkdownPagesOf(drawn, "r1"), testMarkdownPagesOf(drawn, "r8")
		if !slices.Equal(first, last) {
			t.Fatalf("%d paragraphs: the table is split, %v and %v", k, first, last)
		}
		lastFiller := 0
		if k > 0 {
			lastFiller = testMarkdownPagesOf(drawn, "p"+strconv.Itoa(k-1))[0]
		}
		moved = moved || first[0] > lastFiller
	}
	if !moved {
		t.Error("the table never moved to the next page")
	}
}

func TestMarkdownAParagraphThatEndsInAColonStaysWithItsTable(t *testing.T) {
	testAllowCoreFonts(t)
	moved := false
	for k := 0; k <= 60; k++ {
		drawn := testDrawMarkdown(testFillerAndTable(k, "555:", 8), "")
		leadIn := testMarkdownPagesOf(drawn, "555:")
		if !slices.Equal(leadIn, testMarkdownPagesOf(drawn, "r1")) || !slices.Equal(leadIn, testMarkdownPagesOf(drawn, "r8")) {
			t.Fatalf("%d paragraphs: the lead-in or the table is apart", k)
		}
		lastFiller := 0
		if k > 0 {
			lastFiller = testMarkdownPagesOf(drawn, "p"+strconv.Itoa(k-1))[0]
		}
		moved = moved || leadIn[0] > lastFiller
	}
	if !moved {
		t.Error("the lead-in never moved with its table")
	}
}

// A heading over the lead-in goes with it and its table: the two went to the
// next page and left the heading at the bottom of this one (the report of the
// fair test, 26 October 2026).
func TestMarkdownAHeadingStaysWithALeadInAndItsTable(t *testing.T) {
	testAllowCoreFonts(t)
	moved := false
	for k := 0; k <= 60; k++ {
		drawn := testDrawMarkdown(testFillerAndTable(k, "## H7\n\n555:", 8), "")
		heading := testMarkdownPagesOf(drawn, "H7")
		if !slices.Equal(heading, testMarkdownPagesOf(drawn, "555:")) {
			t.Fatalf("%d paragraphs: the heading is apart from its lead-in", k)
		}
		if !slices.Equal(heading, testMarkdownPagesOf(drawn, "r8")) {
			t.Fatalf("%d paragraphs: the heading is apart from its table", k)
		}
		lastFiller := 0
		if k > 0 {
			lastFiller = testMarkdownPagesOf(drawn, "p"+strconv.Itoa(k-1))[0]
		}
		moved = moved || heading[0] > lastFiller
	}
	if !moved {
		t.Error("the heading never moved with its table")
	}
}

func TestMarkdownATableLongerThanAPageStillStartsWhereItIs(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown(testFillerAndTable(3, "", 120), "")
	if !slices.Equal(testMarkdownPagesOf(drawn, "p2"), []int{0}) || testMarkdownPagesOf(drawn, "r1")[0] != 0 {
		t.Error("the long table does not start after the text")
	}
	if testMarkdownPagesOf(drawn, "r120")[0] == 0 {
		t.Error("the long table does not go on to the next pages")
	}
}

// The right-aligned header of the last column is drawn at the right end of the
// table: near the text's right margin, 72 pt from the edge of the page, only
// when the tables are as wide as the text.
func TestMarkdownATableIsAsWideAsTheTextWhenSet(t *testing.T) {
	testAllowCoreFonts(t)
	text := "| N | V |\n|---|---:|\n| a | 15 |"
	narrow := testPositionOf(t, testDrawMarkdown(text, "").contents[0], "V")[0]
	wide := testPositionOf(t, testDrawMarkdownWith(text, "", func(markdown *Markdown) {
		markdown.SetFullWidthTables(true)
	}).contents[0], "V")[0]
	if narrow >= 200 {
		t.Errorf("a table is as wide as its text: V at %v", narrow)
	}
	if wide <= 612-72-30 {
		t.Errorf("a table is as wide as the text: V at %v", wide)
	}
}

func TestMarkdownATableGoesOnOnTheNextPageWithItsHeaderRow(t *testing.T) {
	testAllowCoreFonts(t)
	// Letters of the core fonts that are kerned are drawn apart, so the
	// header is one letter a column.
	var text strings.Builder
	text.WriteString("Some text first.\n\n| N | S |\n|---:|---:|\n")
	for i := 1; i <= 120; i++ {
		text.WriteString("| " + strconv.Itoa(i) + " | " + strconv.Itoa(i*i) + " |\n")
	}
	drawn := testDrawMarkdown(text.String(), "")
	if len(drawn.pages) < 2 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	testAssertStructures(t, drawn, "Table", 1)
	for i, content := range drawn.contents {
		if !strings.Contains(content, "<"+testHex("S")+">") {
			t.Errorf("the header row is not on page %d", i+1)
		}
	}
	if !strings.Contains(drawn.text, testHex("14400")) {
		t.Error("the last row is not drawn")
	}
}

func TestMarkdownImagesAreReadOnlyFromTheImageDirectory(t *testing.T) {
	testAllowCoreFonts(t)
	text := "![Circles](circles.png)"
	// With no directory, the image's text is drawn instead.
	drawn := testDrawMarkdown(text, "")
	testAssertStructures(t, drawn, "Figure", 0)
	if !strings.Contains(drawn.text, testHex("Circles")) {
		t.Error("the text of the image is not drawn")
	}
	// From the directory, the image is a figure.
	images := testRepoPath(t, "images")
	drawn = testDrawMarkdown(text, images)
	testAssertStructures(t, drawn, "Figure", 1)
	alt := regexp.MustCompile(`/Alt <([0-9A-Fa-f]+)>`).FindStringSubmatch(drawn.pdf)
	if alt == nil || testUTF16Hex(t, alt[1]) != "Circles" {
		t.Error("the text of the image is not its description")
	}
	// Not above it, not an absolute path, not a URL.
	for _, source := range []string{"../images/circles.png", "/etc/passwd",
		testRepoPath(t, "images/circles.png"), "https://pdfjet.com/logo.png",
		"missing.png"} {
		drawn = testDrawMarkdown("![Not read]("+source+")", images)
		if n := testCountStructures(drawn.pdf, "Figure"); n != 0 {
			t.Errorf("%s: %d figures", source, n)
		}
		if !strings.Contains(drawn.text, testHex("Not read")) {
			t.Errorf("%s: the text is not drawn", source)
		}
	}
}

func TestMarkdownCodeKeepsItsLinesAndGoesOnOnTheNextPage(t *testing.T) {
	testAllowCoreFonts(t)
	var text strings.Builder
	text.WriteString("```\n")
	for i := 0; i < 90; i++ {
		text.WriteString("  line " + strconv.Itoa(i) + " *not emphasis*\n")
	}
	text.WriteString("```\n")
	drawn := testDrawMarkdown(text.String(), "")
	if len(drawn.pages) < 2 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	testAssertStructures(t, drawn, "Code", 1)
	for _, line := range []string{"  line 0 *not emphasis*", "  line 89 *not emphasis*"} {
		if !strings.Contains(drawn.text, testHex(line)) {
			t.Errorf("%q is not drawn", line)
		}
	}
}

func TestMarkdownNumberedListsStartAtTheirFirstNumber(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown("3. three\n4. four", "")
	if !strings.Contains(drawn.text, testHex("3.")) || !strings.Contains(drawn.text, testHex("4.")) {
		t.Error("the labels are not 3. and 4.")
	}
	if strings.Contains(drawn.text, "<"+testHex("1.")+">") {
		t.Error("a label is 1.")
	}
}

func TestMarkdownHTMLIsDrawnAsText(t *testing.T) {
	testAllowCoreFonts(t)
	drawn := testDrawMarkdown("<b>not bold</b>", "")
	if !strings.Contains(drawn.text, testHex("<b>not")) {
		t.Error("the HTML is not drawn as text")
	}
}

// A table of more than 64 columns, and tables of more than 100,000 cells,
// make the text an error: the first was drawn as a paragraph of pipes, and a
// header of 64 columns over rows of "|" made 64 cells of a row of 2 bytes,
// 895 MB from 64 KB (review of 12 October 2026, R33).
func TestMarkdownATableTooWideOrTooLargeIsAnError(t *testing.T) {
	testAllowCoreFonts(t)
	draw := func(text string) error {
		pdf := testNewPDF()
		font := NewCoreFont(pdf, corefont.Helvetica())
		pages := make([]*Page, 0)
		NewMarkdown(font, font, font, font, font).DrawOnPages(pdf, text, &pages, letter.Portrait())
		return pdf.err
	}
	table := func(columns, rows int) string {
		return strings.Repeat("| a ", columns) + "|\n" + strings.Repeat("| - ", columns) + "|\n" +
			strings.Repeat("|\n", rows)
	}
	if err := draw(table(64, 2)); err != nil {
		t.Errorf("64 columns: %v", err)
	}
	if err := draw(table(65, 2)); err == nil || !strings.Contains(err.Error(), "more than 64 columns") {
		t.Errorf("65 columns: %v", err)
	}
	if err := draw(table(64, 1600)); err == nil || !strings.Contains(err.Error(), "more than 100000 cells") {
		t.Errorf("102,464 cells: %v", err)
	}
	// A paragraph of pipes that is no table, with no row of delimiters, is
	// text, as before
	if err := draw(strings.Repeat("| a ", 70) + "|\n"); err != nil {
		t.Errorf("a line of pipes: %v", err)
	}
}

func TestMarkdownLongCodeLinesAreCutInLinearTime(t *testing.T) {
	testAllowCoreFonts(t)
	var line strings.Builder
	for i := 0; i < 200000; i++ {
		line.WriteByte(byte('a' + i%26))
	}
	var drawn *testMarkdownDrawn
	elapsed := testShortest(5*time.Second, func() { drawn = testDrawMarkdown("```\n"+line.String()+"\n```", "") })
	if len(drawn.pages) <= 1 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	if elapsed > 5*time.Second {
		t.Errorf("%v", elapsed)
	}
}

// A run of headings is laid out in linear time: each heading measured the
// headings after it to the end of the run, and 5,000 took 40 seconds (the
// review of 28 October 2026).
func TestMarkdownARunOfHeadingsIsLaidOutInLinearTime(t *testing.T) {
	testAllowCoreFonts(t)
	text := strings.Repeat("# h\n", 5000)
	var drawn *testMarkdownDrawn
	elapsed := testShortest(5*time.Second, func() { drawn = testDrawMarkdown(text, "") })
	if len(drawn.pages) <= 1 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	if elapsed > 5*time.Second {
		t.Errorf("%v", elapsed)
	}
}

func TestMarkdownACodeLineOfOneColumnKeepsItsSurrogatePairs(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	code := NewCoreFont(pdf, corefont.Courier())
	// A width that one character of the code fills.
	markdown := NewMarkdown(font, font, font, font, code).SetMargins(300, 72, 300, 72)
	pages := make([]*Page, 0)
	markdown.DrawOnPages(pdf, "```\n\U0001F600x\U0001F600\n```", &pages, letter.Portrait())
	if len(pages) != 1 {
		t.Errorf("%d pages", len(pages))
	}
}

// The review of the Markdown of 26 October 2026. The first block of a quote
// or a list item keeps the room it needs, as any other block: a short table
// there is not split, and a lead-in there keeps its table. A short table after
// a lead-in stays with it, and a heading with a table right under it.
func TestMarkdownBlocksKeepTheRoomTheyNeed(t *testing.T) {
	testAllowCoreFonts(t) // What the document tags, in a core font
	for k := 0; k <= 60; k++ {
		filler := testFillerAndTable(k, "", 0)
		filler = filler[:strings.Index(filler, "| N |")]
		drawn := testDrawMarkdown(filler+"> | N | V |\n> |---|---|\n> | r1 | v1 |\n> | r2 | v2 |\n> | r3 | v3 |\n", "")
		if !slices.Equal(testMarkdownPagesOf(drawn, "r1"), testMarkdownPagesOf(drawn, "r3")) {
			t.Fatalf("%d paragraphs: the table in the quote is split", k)
		}
		drawn = testDrawMarkdown(filler+"- Sizes:\n\n  | N | V |\n  |---|---|\n  | r1 | v1 |\n", "")
		if !slices.Equal(testMarkdownPagesOf(drawn, "Sizes:"), testMarkdownPagesOf(drawn, "r1")) {
			t.Fatalf("%d paragraphs: the item's lead-in is apart", k)
		}
		drawn = testDrawMarkdown(testFillerAndTable(k, "555:", 1), "")
		if !slices.Equal(testMarkdownPagesOf(drawn, "555:"), testMarkdownPagesOf(drawn, "r1")) {
			t.Fatalf("%d paragraphs: the short table left its lead-in", k)
		}
		drawn = testDrawMarkdown(filler+"## H7\n\n"+testFillerAndTable(0, "", 8), "")
		if !slices.Equal(testMarkdownPagesOf(drawn, "H7"), testMarkdownPagesOf(drawn, "r1")) {
			t.Fatalf("%d paragraphs: the heading is apart from its table", k)
		}
	}
}

// A heading of two lines is kept whole on its page, with a line of the text
// after it; it was kept by its first line.
func TestMarkdownAHeadingOfTwoLinesIsKeptWhole(t *testing.T) {
	testAllowCoreFonts(t) // What the document tags, in a core font
	heading := "# Alpha" + strings.Repeat(" word", 12) + " Omega\n\nThe text after it.\n"
	for k := 0; k <= 60; k++ {
		filler := testFillerAndTable(k, "", 0)
		drawn := testDrawMarkdown(filler[:strings.Index(filler, "| N |")]+heading, "")
		if !slices.Equal(testMarkdownPagesOf(drawn, "Alpha"), testMarkdownPagesOf(drawn, "Omega")) {
			t.Fatalf("%d paragraphs: the heading is split", k)
		}
		if !slices.Equal(testMarkdownPagesOf(drawn, "Omega"), testMarkdownPagesOf(drawn, "The text after it.")) {
			t.Fatalf("%d paragraphs: it ends a page", k)
		}
	}
}

// An empty heading takes no level: the "##" after it is the H1. A table
// interrupts a paragraph, as in GitHub's Markdown. An empty block of code is
// drawn as nothing.
func TestMarkdownAnEmptyHeadingATableAfterALineAndEmptyCode(t *testing.T) {
	testAllowCoreFonts(t) // What the document tags, in a core font
	testAssertStructures(t, testDrawMarkdown("#\n\n## Intro\n\nText.\n", ""), "H1", 1, "H2", 0)
	// Nor does one whose text draws nothing: it took the H1 that was not
	// drawn (the review of 28 October 2026)
	testAssertStructures(t, testDrawMarkdown("# <br>\n### Intro\n", ""), "H1", 1, "H2", 0)
	testAssertStructures(t, testDrawMarkdown("# &#32;\n### Intro\n", ""), "H1", 1, "H2", 0)
	testAssertStructures(t, testDrawMarkdown("Sizes:\n| a | b |\n|---|---|\n| 1 | 2 |\n", ""), "Table", 1)
	testAssertStructures(t, testDrawMarkdown("Text.\n\n```\n```\n", ""), "Code", 0)
}

// A table split inside a quote has the quote's bar on every page it is on:
// the bar stopped where it started, and the pages between had none.
func TestMarkdownASplitTableInAQuoteHasItsBarOnEveryPage(t *testing.T) {
	testAllowCoreFonts(t) // What the document tags, in a core font
	var text strings.Builder
	text.WriteString("> | N | V |\n> |---|---|\n")
	for r := 1; r <= 160; r++ {
		text.WriteString("> | r" + strconv.Itoa(r) + " | v" + strconv.Itoa(r) + " |\n")
	}
	drawn := testDrawMarkdown(text.String(), "")
	if len(drawn.contents) < 3 {
		t.Fatalf("%d pages", len(drawn.contents))
	}
	for i, content := range drawn.contents {
		if !strings.Contains(content, "0.69 0.71 0.75 RG") {
			t.Errorf("no bar on page %d", i)
		}
	}
}

// A lead-in is trimmed as Java's String.trim() trims, of the characters up to
// U+0020 only: one that ends in a colon and a no-break space leads into no
// table, in the four ports alike.
func TestMarkdownALeadInIsTrimmedAsInJava(t *testing.T) {
	table := &markdownBlock{kind: markdownTable}
	if !markdownLeadsIntoTable(&markdownBlock{kind: markdownParagraph, text: "Sizes: \t"}, table) {
		t.Error("a colon and spaces leads into no table")
	}
	if !markdownLeadsIntoTable(&markdownBlock{kind: markdownParagraph, text: "大小："}, table) {
		t.Error("the full-width colon leads into no table")
	}
	if markdownLeadsIntoTable(&markdownBlock{kind: markdownParagraph, text: "Sizes: "}, table) {
		t.Error("a colon and a no-break space leads into the table")
	}
}
