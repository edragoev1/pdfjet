// bookmark_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"testing"
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
