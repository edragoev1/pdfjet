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
	for _, s := range []string{"/OCProperties", "/View << /ViewState /OFF >>", "/Print << /PrintState /ON >>"} {
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
	// Export is off unless SetExportable(true) is called.
	if !strings.Contains(pdf, "/Export << /ExportState /OFF >>") {
		t.Error("wrong export state")
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
