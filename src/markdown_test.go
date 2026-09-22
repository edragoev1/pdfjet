// markdown_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
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
	doc := testNewDoc()
	pdf := doc.pdf
	pdf.SetCompliance(compliance.PDF_UA_1)
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
	drawn := testDrawMarkdown("# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n"+
		"```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next", "")
	if len(drawn.pages) != 1 {
		t.Errorf("%d pages", len(drawn.pages))
	}
	testAssertStructures(t, drawn, "H1", 1, "H2", 1, "L", 1, "LI", 2, "Lbl", 2, "LBody", 2,
		"BlockQuote", 1, "Code", 1, "Table", 1)
}

func TestMarkdownHeadingLevelsSkipNone(t *testing.T) {
	// A text that starts at ### and goes on to ##### is H1, then H2.
	drawn := testDrawMarkdown("### Three\n\n##### Five\n\n# One", "")
	testAssertStructures(t, drawn, "H1", 2, "H2", 1, "H3", 0)
}

func TestMarkdownTheTextFlowsOntoAsManyPagesAsItNeeds(t *testing.T) {
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

func TestMarkdownATableGoesOnOnTheNextPageWithItsHeaderRow(t *testing.T) {
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
	text := "![Tux](linux-logo.png)"
	// With no directory, the image's text is drawn instead.
	drawn := testDrawMarkdown(text, "")
	testAssertStructures(t, drawn, "Figure", 0)
	if !strings.Contains(drawn.text, testHex("Tux")) {
		t.Error("the text of the image is not drawn")
	}
	// From the directory, the image is a figure.
	images := testRepoPath(t, "images")
	drawn = testDrawMarkdown(text, images)
	testAssertStructures(t, drawn, "Figure", 1)
	alt := regexp.MustCompile(`/Alt <([0-9A-Fa-f]+)>`).FindStringSubmatch(drawn.pdf)
	if alt == nil || testUTF16Hex(t, alt[1]) != "Tux" {
		t.Error("the text of the image is not its description")
	}
	// Not above it, not an absolute path, not a URL.
	for _, source := range []string{"../images/linux-logo.png", "/etc/passwd",
		testRepoPath(t, "images/linux-logo.png"), "https://pdfjet.com/logo.png",
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
	drawn := testDrawMarkdown("3. three\n4. four", "")
	if !strings.Contains(drawn.text, testHex("3.")) || !strings.Contains(drawn.text, testHex("4.")) {
		t.Error("the labels are not 3. and 4.")
	}
	if strings.Contains(drawn.text, "<"+testHex("1.")+">") {
		t.Error("a label is 1.")
	}
}

func TestMarkdownHTMLIsDrawnAsText(t *testing.T) {
	drawn := testDrawMarkdown("<b>not bold</b>", "")
	if !strings.Contains(drawn.text, testHex("<b>not")) {
		t.Error("the HTML is not drawn as text")
	}
}

func TestMarkdownLongCodeLinesAreCutInLinearTime(t *testing.T) {
	var line strings.Builder
	for i := 0; i < 200000; i++ {
		line.WriteByte(byte('a' + i%26))
	}
	start := time.Now()
	drawn := testDrawMarkdown("```\n"+line.String()+"\n```", "")
	elapsed := time.Since(start)
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
