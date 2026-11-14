// link_action_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The links to one address share one action, which holds the address: each
// link carried it, and twenty links to an address of 20,000 characters added
// about 800 KB. Each link keeps its own description.
func TestLinksToOneAddressShareOneAction(t *testing.T) {
	address := "https://pdfjet.com/" + strings.Repeat("a", 300)
	doc := testNewDoc()
	font := testPlexSans(doc.pdf)
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextLine(font, "One").SetURIAction(address).SetLocation(50, 100).DrawOn(page)
	NewTextLine(font, "Two").SetURIAction(address).SetLocation(50, 120).DrawOn(page)
	NewTextLine(font, "Site").SetURIAction("https://pdfjet.com").SetLocation(50, 140).DrawOn(page)
	raw := string(doc.complete())
	if n := strings.Count(raw, "/Subtype /Link"); n != 3 {
		t.Errorf("%d links", n)
	}
	if n := strings.Count(raw, "/S /URI\n"); n != 2 {
		t.Errorf("%d actions", n)
	}
	links := regexp.MustCompile(`/A (\d+) 0 R\n`).FindAllStringSubmatch(raw, -1)
	actions := map[string]bool{}
	for _, link := range links {
		actions[link[1]] = true
	}
	if len(links) != 3 || len(actions) != 2 {
		t.Errorf("%d links to %d actions", len(links), len(actions))
	}
}
