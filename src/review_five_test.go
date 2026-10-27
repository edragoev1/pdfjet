// review_five_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"reflect"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The findings of the fresh-eyes review of the charts, Markdown and the
// barcodes, of 27 October 2026.

// testReviewFiveMarkup returns a markup in Helvetica alone.
func testReviewFiveMarkup(pdf *PDF) *Markup {
	font := testHelvetica(pdf)
	return NewMarkup(font, font, font, font, font)
}

// testJoinFields returns the words of the text, one space between them.
func testJoinFields(text string) string {
	return strings.Join(strings.Fields(text), " ")
}

func TestReviewFiveTheTextOfAParagraphHasNoSpaceWhereItsRunsAreJoined(t *testing.T) {
	markup := testReviewFiveMarkup(testNewPDF())
	testWant(t, "Version 9.1.5: notes", testJoinFields(markup.Paragraph("Version **9.1.5**: notes").text()))
	testWant(t, "The Tables", testJoinFields(markup.Paragraph("The `Table`s").text()))
	testWant(t, "one two", testJoinFields(markup.Paragraph("one<br>two").text()))
}

func TestReviewFiveTheBookmarkOfAHeadingHasItsText(t *testing.T) {
	pdf := testMarkdownPDFOf(t, "# Version **9.1.5**: notes")
	if got := testMarkdownOutline(t, pdf); !reflect.DeepEqual(got, []string{"Version 9.1.5: notes"}) {
		t.Errorf("the title: %q", got)
	}
}

func TestReviewFiveABackslashAtTheEndOfAParagraphIsKept(t *testing.T) {
	markup := testReviewFiveMarkup(testNewPDF())
	paragraphs := markup.Paragraphs("Save it to C:\\Temp\\")
	testWant(t, "Save it to C:\\Temp\\", strings.TrimSpace(paragraphs[0].text()))
}

func TestReviewFiveTheAltTextOfAnImageIsItsText(t *testing.T) {
	testAllowCoreFonts(t)
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	pages := make([]*Page, 0)
	// No image directory: the text is drawn in the image's place
	NewMarkdown(font, font, font, font, font).
		DrawOnPages(pdf, "![Fish &amp; Chips \\*](fish.png)", &pages, letter.Portrait())
	content := testContent(pages[0])
	if !strings.Contains(content, testHex("Fish & Chips *")) {
		t.Error(content)
	}
}

// testReviewFiveBarChart returns the content of a bar chart over a range of
// 0 to 100 with a line series of the values.
func testReviewFiveBarChart(pdf *PDF, line []float32) string {
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	chart := NewBarChart(font, font).SetSize(300, 200).
		SetCategories("a", "b").SetValueAxisRange(0, 100)
	chart.SetLocation(50, 50)
	chart.AddSeries("", []float32{50, 60})
	chart.AddLineSeries("target", line)
	chart.DrawOn(page)
	return testContent(page)
}

func TestReviewFiveALineValueOutsideTheRangeIsNotDrawnAtTheEdge(t *testing.T) {
	pdf := testNewPDF()
	if content := testReviewFiveBarChart(pdf, []float32{150, 80}); !strings.Contains(content, "W\nn\n") {
		t.Error("not clipped:\n" + content)
	}
	// In range, no clip
	if content := testReviewFiveBarChart(pdf, []float32{90, 80}); strings.Contains(content, "W\nn\n") {
		t.Error("clipped:\n" + content)
	}
}

func TestReviewFiveAHeadingOnAPageThatIsNotAddedHasNoBookmark(t *testing.T) {
	testAllowCoreFonts(t)
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	var text strings.Builder
	text.WriteString("# One\n\n")
	for i := 0; i < 80; i++ {
		text.WriteString("p" + strconv.Itoa(i) + "\n\n")
	}
	text.WriteString("# Two")
	pages := make([]*Page, 0)
	NewMarkdown(font, font, font, font, font).DrawOnPages(doc.pdf, text.String(), &pages, letter.Portrait())
	if len(pages) <= 1 {
		t.Fatalf("%d pages", len(pages))
	}
	doc.pdf.AddPage(pages[0]) // The page of One only
	out := doc.complete()     // It failed: "Two" was on a page that is not added
	if got := testMarkdownOutline(t, out); !reflect.DeepEqual(got, []string{"One"}) {
		t.Errorf("the bookmarks: %q", got)
	}
}

func TestReviewFiveAnEmptyCode128IsRefused(t *testing.T) {
	defer func() {
		if recover() == nil {
			t.Error("an empty Code 128 is drawn")
		}
	}()
	NewBarcode(CODE_128, "")
}

func TestReviewFiveTwoBreaksLeaveAnEmptyRow(t *testing.T) {
	testAllowCoreFonts(t)
	pdf := testNewPDF()
	markup := testReviewFiveMarkup(pdf)
	if got := markup.Paragraph("p1<br><br>p2").lines[0].lineBreaks; got != 2 {
		t.Errorf("%d breaks", got)
	}
	var gaps [2]float32
	for k, text := range []string{"p1<br>p2", "p1<br><br>p2"} {
		page := NewPage(pdf, letter.Portrait())
		frame := NewTextFrameFromParagraphs([]*Paragraph{markup.Paragraph(text)}).SetWidth(400)
		frame.SetLocation(50, 50)
		frame.DrawOn(page)
		content := testContent(page)
		gaps[k] = testPositionOf(t, content, "p1")[1] - testPositionOf(t, content, "p2")[1]
	}
	// To the 0.01 of the positions written
	if math.Abs(float64(2*gaps[0]-gaps[1])) > 0.05 {
		t.Errorf("gaps %v", gaps)
	}
}

func TestReviewFiveAPipeInCodeInATableCell(t *testing.T) {
	testWant(t, "TABLE(a;`x|y`|L)", testParseMarkdown("| a |\n|---|\n| `x\\|y` |"))
}

func TestReviewFiveHardBreaksOfCRLFText(t *testing.T) {
	markup := testReviewFiveMarkup(testNewPDF())
	for text, want := range map[string]int{"one  \r\ntwo": 1, "one\\\r\ntwo": 1, "one \r\ntwo": 0} {
		if got := markup.Paragraph(text).lines[0].lineBreaks; got != want {
			t.Errorf("%q: %d breaks, want %d", text, got, want)
		}
	}
}

func TestReviewFiveEntitiesInTheAddressOfALink(t *testing.T) {
	markup := testReviewFiveMarkup(testNewPDF())
	testWant(t, "https://x/?a=1&b=2", markup.Paragraph("[a](https://x/?a=1&amp;b=2)").lines[0].GetURIAction())
}

func TestReviewFiveAControlCharacterOfAnEntityIsASpace(t *testing.T) {
	markup := testReviewFiveMarkup(testNewPDF())
	testWant(t, "a b c d", markup.Paragraph("a&#10;b&#9;c&#x7;d").lines[0].text)
}

func TestReviewFiveTheTrendIsOfThePointsDrawn(t *testing.T) {
	font := testHelvetica(testNewPDF())
	chart := NewSimpleScatterChart(font, font).SetTitle("T").
		Add(1, 1).Add(2, 2).Add(3, 3).Add(4, -1000).SetTrendLine(true).SetYAxisRange(0, 10)
	testWant(t, "T: 3 points; the trend rises.", chart.getAltDescription())
}

func TestTheIgnorableCharactersAtTheEdgesOfTheGapsSkipped(t *testing.T) {
	chars := []rune{0x034F, 0x0350, 0x061B, 0x061C, 0x3164, 0x3165, 0xFDFF, 0xFE00}
	ignorable := []bool{true, false, false, true, true, false, false, true}
	for i, ch := range chars {
		if isIgnorable(ch) != ignorable[i] {
			t.Errorf("%X: %v", ch, isIgnorable(ch))
		}
	}
}
