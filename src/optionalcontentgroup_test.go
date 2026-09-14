// optionalcontentgroup_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

func testLayer(t *testing.T, visible bool) string {
	t.Helper()
	doc := testNewDoc()
	page := NewPage(doc.pdf, testLetterPortrait())
	group := NewOptionalContentGroup(doc.pdf, "Layer").SetVisible(visible).SetPrintable(true)
	group.Add(NewRect(10, 10, 20, 20))
	testAssertXY(t, 30, 30, group.DrawOn(page))
	return string(doc.complete())
}

func TestOptionalContentGroupAHiddenLayerHasTheViewStateOff(t *testing.T) {
	pdf := testLayer(t, false)
	// The default configuration lists the hidden group too, for the viewers
	// that do not apply the usage
	for _, s := range []string{"/OCProperties", "/View << /ViewState /OFF >>", "/Print << /PrintState /ON >>", "/OFF ["} {
		if !strings.Contains(pdf, s) {
			t.Errorf("no %s", s)
		}
	}
}

func TestOptionalContentGroupAVisibleLayerHasTheViewStateOn(t *testing.T) {
	pdf := testLayer(t, true)
	if !strings.Contains(pdf, "/View << /ViewState /ON >>") || strings.Contains(pdf, "/ViewState /OFF") {
		t.Error("wrong view state")
	}
	if strings.Contains(pdf, "/OFF [") {
		t.Error("a visible group is in the /OFF array")
	}
	// Export is off unless SetExportable(true) is called.
	if !strings.Contains(pdf, "/Export << /ExportState /OFF >>") {
		t.Error("wrong export state")
	}
}

func TestOptionalContentGroupAGroupWrapsItsContentInMarkedContentAndIsAPageProperty(t *testing.T) {
	doc := testNewDoc()
	page1 := NewPage(doc.pdf, testLetterPortrait())
	mapGroup := NewOptionalContentGroup(doc.pdf, "Map").SetVisible(true)
	mapGroup.Add(NewRect(10, 10, 20, 20)).DrawOn(page1)
	notes := NewOptionalContentGroup(doc.pdf, "Notes")
	notes.Add(NewLine(0, 0, 10, 10)).DrawOn(page1)
	content1 := testContent(page1)
	if !strings.Contains(content1, "/OC /OC1 BDC\n") || !strings.Contains(content1, "/OC /OC2 BDC\n") ||
		strings.Count(content1, "EMC") != 2 {
		t.Errorf("marked content in %q", content1)
	}
	// The same group on a second page is the same object
	page2 := NewPage(doc.pdf, testLetterPortrait())
	mapGroup.DrawOn(page2)
	if !strings.Contains(testContent(page2), "/OC /OC1 BDC\n") {
		t.Errorf("marked content in %q", testContent(page2))
	}
	file := string(doc.complete())
	if strings.Count(file, "/Type /OCG") != 2 {
		t.Errorf("%d OCG objects", strings.Count(file, "/Type /OCG"))
	}
	for _, s := range []string{"/Properties", "/OC1 ", "/OC2 ", "/OCGs [", "/Order ["} {
		if !strings.Contains(file, s) {
			t.Errorf("no %s", s)
		}
	}
}

func TestOptionalContentGroupClearRemovesTheDrawables(t *testing.T) {
	group := NewOptionalContentGroup(testNewPDF(), "Layer")
	group.Add(NewRect(0, 0, 1, 1)).Add(NewLine(0, 0, 1, 1))
	if len(group.GetComponents()) != 2 {
		t.Errorf("components %d", len(group.GetComponents()))
	}
	if len(group.Clear().GetComponents()) != 0 {
		t.Error("Clear kept components")
	}
	if group.GetName() != "Layer" {
		t.Errorf("name %q", group.GetName())
	}
}

func TestOptionalContentGroupGetComponentsReturnsACopy(t *testing.T) {
	group := NewOptionalContentGroup(testNewPDF(), "Layer")
	group.Add(NewRect(0, 0, 1, 1))
	components := group.GetComponents()
	components[0] = nil
	if group.GetComponents()[0] == nil {
		t.Error("GetComponents returned the live list")
	}
}
