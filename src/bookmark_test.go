// bookmark_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "testing"

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
