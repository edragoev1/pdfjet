// bookmark_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

func TestBookmarkTheRootHasNoTitleAndNoDestination(t *testing.T) {
	// Java returns null; the Go getters return an empty string.
	root := NewBookmark(testNewPDF())
	if root.GetTitle() != "" || root.GetDestinationName() != "" {
		t.Errorf("title %q, destination %q", root.GetTitle(), root.GetDestinationName())
	}
}

func TestBookmarkATitleHasItsWhitespaceCollapsed(t *testing.T) {
	pdf := testNewPDF()
	root := NewBookmark(pdf)
	page := NewPage(pdf, testLetterPortrait())
	child := root.AddBookmark(page, NewTitle(testHelvetica(pdf), "Chapter\t one\n  intro", 10, 10))
	if child.GetTitle() != "Chapter one intro" {
		t.Errorf("title %q", child.GetTitle())
	}
	if child.GetDestinationName() == "" {
		t.Error("no destination")
	}
	if child.GetParent() != root {
		t.Error("wrong parent")
	}
}

func testOutlineItem(t *testing.T, objects []*PDFobj, title string) *PDFobj {
	t.Helper()
	for _, obj := range objects {
		value := obj.GetValue("/Title")
		if value != "" && obj.GetValue("/Producer") == "" && testUTF16Hex(t, value) == title {
			return obj
		}
	}
	t.Fatalf("no outline item %q", title)
	return nil
}

func TestBookmarkNestedBookmarksMakeATreeThatReadersNeedNotRepair(t *testing.T) {
	doc := testNewDoc()
	font := testHelvetica(doc.pdf)
	page := NewPage(doc.pdf, testLetterPortrait())
	root := NewBookmark(doc.pdf)
	root.AddBookmark(page, NewTitle(font, "A", 10, 10))
	b := root.AddBookmark(page, NewTitle(font, "B", 10, 30))
	b.AddBookmark(page, NewTitle(font, "B1", 10, 50))
	b2 := b.AddBookmark(page, NewTitle(font, "B2", 10, 70))
	b2.AddBookmark(page, NewTitle(font, "B2a", 10, 90))
	root.AddBookmark(page, NewTitle(font, "C", 10, 110))

	objects := testRead(t, doc.complete())
	var outlines *PDFobj
	for _, obj := range objects {
		if obj.GetValue("/Type") == "/Outlines" {
			outlines = obj
		}
	}
	if outlines == nil {
		t.Fatal("no outline dictionary")
	}
	number := func(title string) string {
		return strconv.Itoa(testOutlineItem(t, objects, title).GetNumber())
	}
	value := func(title, key string) string {
		return testOutlineItem(t, objects, title).GetValue(key)
	}
	root0 := strconv.Itoa(outlines.GetNumber())
	// The outline dictionary has the items of the first level.
	testWant(t, number("A"), outlines.GetValue("/First"))
	testWant(t, number("C"), outlines.GetValue("/Last"))
	testWant(t, "3", outlines.GetValue("/Count"))
	testWant(t, root0, value("A", "/Parent"))
	testWant(t, root0, value("C", "/Parent"))

	// A nested item has the item above it as its parent, and a closed item
	// counts the items that opening it shows.
	testWant(t, "-2", value("B", "/Count"))
	testWant(t, number("B1"), value("B", "/First"))
	testWant(t, number("B2"), value("B", "/Last"))
	testWant(t, number("B"), value("B1", "/Parent"))
	testWant(t, number("B"), value("B2", "/Parent"))
	testWant(t, number("B2"), value("B1", "/Next"))
	testWant(t, number("B1"), value("B2", "/Prev"))
	testWant(t, "-1", value("B2", "/Count"))
	testWant(t, number("B2"), value("B2a", "/Parent"))
	testWant(t, "", value("B2a", "/Count"))
}

func TestBookmarkATitleIsATextStringThatEveryReaderDecodes(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, testLetterPortrait())
	root := NewBookmark(doc.pdf)
	root.AddBookmark(page, NewTitle(testHelvetica(doc.pdf), "\u00dcbersicht \u2013 r\u00e9sum\u00e9", 10, 10))
	obj := testOutlineItem(t, testRead(t, doc.complete()), "\u00dcbersicht \u2013 r\u00e9sum\u00e9")
	if title := strings.ToLower(obj.GetValue("/Title")); !strings.HasPrefix(title, "<feff00dc") {
		t.Errorf("title %s", title)
	}
}

// testHeadingsDoc returns a document with the headings, as text lines of
// their structure types, H1 on the first page and the others on the second.
func testHeadingsDoc(t *testing.T, tagged bool, headings ...[2]string) *testDoc {
	t.Helper()
	doc := testNewDoc()
	if tagged {
		doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
		doc.pdf.SetTitle("Title")
	}
	font := testTrueTypeFont(t, doc.pdf)
	var page *Page
	for i, h := range headings {
		if page == nil || h[0] == "H1" && i > 0 {
			page = NewPage(doc.pdf, testLetterPortrait())
		}
		NewTextLine(font, h[1]).SetStructureType(structelem.StructElem(h[0])).
			SetLocation(70, float32(100+40*i)).DrawOn(page)
	}
	return doc
}

// A tagged document with headings and no bookmarks of its own has the
// bookmarks of its headings, each under the heading of a higher level before
// it, and each at the top of its heading on its page, as PAC asks.
func TestBookmarkATaggedDocumentHasTheBookmarksOfItsHeadings(t *testing.T) {
	doc := testHeadingsDoc(t, true,
		[2]string{"H1", "Intro"}, [2]string{"H2", "What  it is"}, [2]string{"H3", "In short"},
		[2]string{"H2", "Why"}, [2]string{"H1", "Use"})
	objects := testRead(t, doc.complete())
	number := func(title string) string {
		return strconv.Itoa(testOutlineItem(t, objects, title).GetNumber())
	}
	value := func(title, key string) string {
		return testOutlineItem(t, objects, title).GetValue(key)
	}
	var outlines *PDFobj
	for _, obj := range objects {
		if obj.GetValue("/Type") == "/Outlines" {
			outlines = obj
		}
	}
	if outlines == nil {
		t.Fatal("no outline dictionary")
	}
	testWant(t, number("Intro"), outlines.GetValue("/First"))
	testWant(t, number("Use"), outlines.GetValue("/Last"))
	// The whitespace of a title is collapsed, as AddBookmark does
	testWant(t, number("Intro"), value("What it is", "/Parent"))
	testWant(t, number("What it is"), value("In short", "/Parent"))
	testWant(t, number("Intro"), value("Why", "/Parent"))
	testWant(t, number("Why"), value("What it is", "/Next"))
	// The destination: the page of the heading, and the top of its text
	for _, title := range []string{"Intro", "Use"} {
		dest := testOutlineItem(t, objects, title).GetValue("/Dest")
		if !strings.Contains(dest, "/XYZ") {
			t.Errorf("%s goes to %q", title, dest)
		}
	}
	intro := testOutlineItem(t, objects, "Intro").GetValue("/Dest")
	use := testOutlineItem(t, objects, "Use").GetValue("/Dest")
	if strings.Fields(intro)[1] == strings.Fields(use)[1] {
		t.Errorf("Intro and Use, on two pages, go to one: %q, %q", intro, use)
	}
	// 792 - (100 - 12): the top of a line of 12 points at 100
	if !strings.Contains(intro, "/XYZ 0 704 0") {
		t.Errorf("Intro goes to %q", intro)
	}
}

// A document that is not tagged has no headings, and one with bookmarks of
// its own keeps them.
func TestBookmarkOfHeadingsOnlyInATaggedDocumentWithoutBookmarks(t *testing.T) {
	untagged := testHeadingsDoc(t, false, [2]string{"H1", "Intro"})
	if strings.Contains(string(untagged.complete()), "/Outlines") {
		t.Error("a document that is not tagged has bookmarks of its headings")
	}
	own := testHeadingsDoc(t, true, [2]string{"H1", "Intro"})
	page := NewPage(own.pdf, testLetterPortrait())
	NewBookmark(own.pdf).AddBookmark(page, NewTitle(testTrueTypeFont(t, own.pdf), "Mine", 10, 10))
	objects := testRead(t, own.complete())
	testOutlineItem(t, objects, "Mine")
	for _, obj := range objects {
		if value := obj.GetValue("/Title"); value != "" && obj.GetValue("/Producer") == "" && testUTF16Hex(t, value) == "Intro" {
			t.Error("the bookmarks of the document were replaced by those of its headings")
		}
	}
}
