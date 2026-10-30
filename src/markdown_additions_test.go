// markdown_additions_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"reflect"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The additions to Markdown of 27 October 2026: hard line breaks, <br> in a
// table cell, entities, and the bookmarks of the headings of a document that
// is not tagged.

// testAdditionsMarkup returns a markup in Helvetica alone.
func testAdditionsMarkup(pdf *PDF) *Markup {
	font := testHelvetica(pdf)
	return NewMarkup(font, font, font, font, font)
}

// testBreaks returns the breaks of the text lines of the paragraph, in order.
func testBreaks(paragraph *Paragraph) []bool {
	breaks := make([]bool, 0)
	for _, line := range paragraph.lines {
		breaks = append(breaks, line.lineBreaks > 0)
	}
	return breaks
}

func TestMarkdownAdditionsHardLineBreaks(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, c := range []struct {
		text string
		want []bool
	}{
		{"one  \ntwo", []bool{true, false}},
		{"one\\\ntwo", []bool{true, false}},
		{"one<br>two", []bool{true, false}},
		{"one<BR />two", []bool{true, false}},
		{"one<br/>two", []bool{true, false}},
		// A line break after one space, and a <br> in code, are no breaks
		{"one \ntwo", []bool{false}},
		{"`a<br>b`", []bool{false}},
	} {
		if got := testBreaks(markup.Paragraph(c.text)); !reflect.DeepEqual(got, c.want) {
			t.Errorf("%q: breaks %v, want %v", c.text, got, c.want)
		}
	}
	testWant(t, "<br", markup.Paragraph("<br").lines[0].text)
	// Emphasis goes on over a break
	bold := markup.Paragraph("**one<br>two**")
	if len(bold.lines) != 2 || bold.lines[0].lineBreaks != 1 || bold.joinsPrevious(1) {
		t.Errorf("**one<br>two**: %d lines", len(bold.lines))
	}
}

// A hard line break at the end of a text is no break, as in CommonMark, and
// the spaces before it are not in the last text line; Paragraphs strips the
// whitespace at the end of each paragraph.
func TestMarkdownAdditionsAHardLineBreakAtTheEndIsNoBreak(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, text := range []string{"0  \n", "0  \r\n", "0   \n", "0  \n\n", "0  \n  "} {
		paragraph := markup.Paragraph(text)
		if len(paragraph.lines) != 1 || paragraph.lines[0].text != "0" || paragraph.lines[0].lineBreaks != 0 {
			t.Errorf("%q: %d text lines", text, len(paragraph.lines))
		}
		paragraphs := markup.Paragraphs(text)
		if len(paragraphs) != 1 || len(paragraphs[0].lines) != 1 || paragraphs[0].lines[0].text != "0" {
			t.Errorf("%q: %d paragraphs", text, len(paragraphs))
		}
	}
	// In bold, and a break before the last line
	bold := markup.Paragraph("**0**  \n")
	if len(bold.lines) != 1 || bold.lines[0].text != "0" || bold.lines[0].lineBreaks != 0 {
		t.Errorf("**0**: %d text lines", len(bold.lines))
	}
	two := markup.Paragraph("one  \ntwo  \n")
	if got := testBreaks(two); !reflect.DeepEqual(got, []bool{true, false}) || two.lines[1].text != "two" {
		t.Errorf("one two: breaks %v", got)
	}
	// Spaces at the end without a line break are text in Paragraph
	testWant(t, "0  ", markup.Paragraph("0  ").lines[0].text)
	testWant(t, "0", markup.Paragraphs("0  ")[0].lines[0].text)
}

// The spaces before a hard line break are not in the text line before it, as
// in CommonMark.
func TestMarkdownAdditionsTheSpacesBeforeAHardLineBreakAreNotText(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, text := range []string{"one  \ntwo", "one  \r\ntwo", "one   \ntwo", "one\t  \ntwo",
		"**one**  \ntwo", "[one  \ntwo](u)"} {
		paragraph := markup.Paragraph(text)
		if got := testBreaks(paragraph); !reflect.DeepEqual(got, []bool{true, false}) ||
			paragraph.lines[0].text != "one" || paragraph.lines[1].text != "two" {
			t.Errorf("%q: breaks %v", text, got)
		}
	}
}

// A backslash at the end of a text is a backslash, and no hard line break, as
// in CommonMark; in the middle it is a break, as before.
func TestMarkdownAdditionsABackslashAtTheEndIsText(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, text := range []string{"0\\\n", "0\\\r\n", "0\\\n  ", "0\\\n\n"} {
		paragraph := markup.Paragraph(text)
		if len(paragraph.lines) != 1 || paragraph.lines[0].text != "0\\" || paragraph.lines[0].lineBreaks != 0 {
			t.Errorf("%q: %d text lines", text, len(paragraph.lines))
		}
		paragraphs := markup.Paragraphs(text)
		if len(paragraphs) != 1 || len(paragraphs[0].lines) != 1 || paragraphs[0].lines[0].text != "0\\" {
			t.Errorf("%q: %d paragraphs", text, len(paragraphs))
		}
	}
	two := markup.Paragraph("one\\\ntwo\\\n")
	if got := testBreaks(two); !reflect.DeepEqual(got, []bool{true, false}) || two.lines[1].text != "two\\" {
		t.Errorf("one two: breaks %v", got)
	}
}

// The spaces and tabs at the start of the line after a hard line break are not
// in the text, as in CommonMark; a space written &#32; is, and so are the
// spaces after a <br>, which is no line ending.
func TestMarkdownAdditionsTheSpacesAfterAHardLineBreakAreNotText(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, text := range []string{"one  \n  two", "one  \r\n  two", "one   \n\t two", "one\\\n  two",
		"one\\\r\n  two", "**one**  \n  two", "[one  \n  two](u)"} {
		paragraph := markup.Paragraph(text)
		if got := testBreaks(paragraph); !reflect.DeepEqual(got, []bool{true, false}) ||
			paragraph.lines[0].text != "one" || paragraph.lines[1].text != "two" {
			t.Errorf("%q: breaks %v", text, got)
		}
		paragraphs := markup.Paragraphs(text)
		if len(paragraphs) != 1 || len(paragraphs[0].lines) != 2 || paragraphs[0].lines[1].text != "two" {
			t.Errorf("%q: %d paragraphs", text, len(paragraphs))
		}
	}
	testWant(t, " two", markup.Paragraph("one  \n&#32;two").lines[1].text)
	testWant(t, "  two", markup.Paragraph("one<br>  two").lines[1].text)
}

// The spaces and tabs at the start of the line after a soft line break are not
// in the text, as in CommonMark: the line break is one space; a space written
// &#32; is text.
func TestMarkdownAdditionsTheSpacesAfterASoftLineBreakAreNotText(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	for _, text := range []string{"one\n  two", "one\r\n  two", "one\n\t two", "one\ntwo", "one\r\ntwo"} {
		paragraph := markup.Paragraph(text)
		if len(paragraph.lines) != 1 || paragraph.lines[0].text != "one two" || paragraph.lines[0].lineBreaks != 0 {
			t.Errorf("%q: %d text lines", text, len(paragraph.lines))
		}
		paragraphs := markup.Paragraphs(text)
		if len(paragraphs) != 1 || len(paragraphs[0].lines) != 1 || paragraphs[0].lines[0].text != "one two" {
			t.Errorf("%q: %d paragraphs", text, len(paragraphs))
		}
	}
	testWant(t, "one  two", markup.Paragraph("one\n&#32;two").lines[0].text)
}

func TestMarkdownAdditionsATextFrameStartsARowAtABreak(t *testing.T) {
	testAllowCoreFonts(t)
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	markup := testAdditionsMarkup(pdf)
	frame := NewTextFrameFromParagraphs([]*Paragraph{markup.Paragraph("p1<br>p2 p3")})
	frame.SetLocation(50, 50)
	frame.SetWidth(400).DrawOn(page)
	content := testContent(page)
	first := testPositionOf(t, content, "p1")
	second := testPositionOf(t, content, "p2")
	testNear(t, "x", first[0], second[0], testDelta)
	if !(second[1] < first[1]) { // Below it, y up
		t.Error(content)
	}
}

func TestMarkdownAdditionsEntitiesAreTheirCharacters(t *testing.T) {
	markup := testAdditionsMarkup(testNewPDF())
	testWant(t, "Fish & Chips — — — ©   < >", trimSpace(markup.Paragraph(
		"Fish &amp; Chips &mdash; &#8212; &#x2014; &copy; &nbsp; &lt; &gt;").lines[0].text))
	// Not an entity: an unknown name, no ;, no character, an escaped &
	testWant(t, "&bogus; &amp &#0; &#xD800; &#12345678; &amp;", trimSpace(markup.Paragraph(
		"&bogus; &amp &#0; &#xD800; &#12345678; \\&amp;").lines[0].text))
	// &lt;br&gt; is the text <br>, not a break
	paragraph := markup.Paragraph("a &lt;br&gt; b")
	if got := testBreaks(paragraph); !reflect.DeepEqual(got, []bool{false}) {
		t.Errorf("breaks %v", got)
	}
	testWant(t, "a <br> b", trimSpace(paragraph.lines[0].text))
	// In code an entity is its text
	testWant(t, "&amp;", markup.Paragraph("`&amp;`").lines[0].text)
	// The ; within ten characters of the &, the longest number of each kind
	testWant(t, "\U0010FFFF \U0010FFFF", markup.Paragraph("&#1114111; &#x10FFFF;").lines[0].text)
}

func TestMarkdownAdditionsABrInATableCellBreaksItsLine(t *testing.T) {
	testAllowCoreFonts(t)
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	pages := make([]*Page, 0)
	NewMarkdown(font, font, font, font, font).
		DrawOnPages(pdf, "| r0 |\n|---|\n| r1<br>r2 |", &pages, letter.Portrait())
	content := testContent(pages[0])
	first := testPositionOf(t, content, "r1")
	second := testPositionOf(t, content, "r2")
	testNear(t, "x", first[0], second[0], testDelta)
	if !(second[1] < first[1]) { // Below it, y up
		t.Error(content)
	}
	if strings.Contains(content, testHex("<br>")) {
		t.Error(content)
	}
}

func TestMarkdownAdditionsTheParagraphOfAMarkdownTextKeepsItsBreaks(t *testing.T) {
	testAllowCoreFonts(t)
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	pages := make([]*Page, 0)
	NewMarkdown(font, font, font, font, font).
		DrawOnPages(pdf, "p1  \np2\\\np3\np4", &pages, letter.Portrait())
	content := testContent(pages[0])
	y1 := testPositionOf(t, content, "p1")[1]
	y2 := testPositionOf(t, content, "p2")[1]
	y3 := testPositionOf(t, content, "p3")[1]
	if !(y2 < y1 && y3 < y2) { // Each below the one before it, y up
		t.Error(content)
	}
	if !strings.Contains(content, testHex("p4")) || strings.Contains(content, testHex("\\")) {
		t.Error(content)
	}
}

// testMarkdownOutline returns the titles of the outline items of the PDF.
func testMarkdownOutline(t *testing.T, pdf []byte) []string {
	t.Helper()
	titles := make([]string, 0)
	for _, obj := range testRead(t, pdf) {
		value := obj.GetValue("/Title")
		if value != "" && obj.GetValue("/Producer") == "" {
			titles = append(titles, testUTF16Hex(t, value))
		}
	}
	return titles
}

// testMarkdownPDFOf returns the PDF of the text drawn as Markdown, in a
// document that is not tagged.
func testMarkdownPDFOf(t *testing.T, text string) []byte {
	testAllowCoreFonts(t)
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	pages := make([]*Page, 0)
	NewMarkdown(font, font, font, font, font).DrawOnPages(doc.pdf, text, &pages, letter.Portrait())
	doc.pdf.AddPages(pages)
	return doc.complete()
}

func TestMarkdownAdditionsTheHeadingsOfAnUntaggedDocumentAreItsBookmarks(t *testing.T) {
	pdf := testMarkdownPDFOf(t, "# One\n\ntext\n\n## Two\n\nmore")
	if !strings.Contains(string(pdf), "/Outlines") {
		t.Fatal("no outline")
	}
	if got := testMarkdownOutline(t, pdf); !reflect.DeepEqual(got, []string{"One", "Two"}) {
		t.Errorf("bookmarks %q", got)
	}
	if strings.Contains(string(testMarkdownPDFOf(t, "text alone")), "/Outlines") {
		t.Error("a text without headings has an outline")
	}
}

func TestMarkdownAdditionsADocumentWithBookmarksOfItsOwnKeepsThem(t *testing.T) {
	testAllowCoreFonts(t)
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	pages := make([]*Page, 0)
	NewMarkdown(font, font, font, font, font).DrawOnPages(doc.pdf, "# One", &pages, letter.Portrait())
	toc := NewBookmark(doc.pdf)
	title := NewTitle(font, "Mine", 50, 100)
	toc.AddBookmark(pages[0], title)
	title.DrawOn(pages[0])
	doc.pdf.AddPages(pages)
	if got := testMarkdownOutline(t, doc.complete()); !reflect.DeepEqual(got, []string{"Mine"}) {
		t.Errorf("bookmarks %q, want their own alone", got)
	}
}
