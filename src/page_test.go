// page_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

func TestPageANewPageTracksTheDefaultGraphicsState(t *testing.T) {
	page := testNewPage()
	testNear(t, "pen width", 1, page.GetPenWidth(), 0)
	testAssertRGB(t, 0, 0, 0, page.GetPenColor())
	testAssertRGB(t, 0, 0, 0, page.GetBrushColor())
}

func TestPageCmykSettersWriteCmykAndTrackTheRgbOfTheStandard(t *testing.T) {
	page := testNewPage()
	page.SetPenColorCMYK(0, 1, 1, 0)
	page.SetBrushColorCMYK(0, 0, 0, 1)
	if got := testContent(page); got != "0 1 1 0 K\n0 0 0 1 k\n" {
		t.Errorf("content %q", got)
	}
	testAssertRGB(t, 1, 0, 0, page.GetPenColor())
	testAssertRGB(t, 0, 0, 0, page.GetBrushColor())
}

func TestPageRestoreGraphicsStateRestoresTheTrackedState(t *testing.T) {
	page := testNewPage()
	page.SetPenColor(0xFF0000)
	page.SaveGraphicsState()
	page.SetPenWidth(3)
	page.SetPenColor(0x00FF00)
	page.RestoreGraphicsState()
	testNear(t, "pen width", 1, page.GetPenWidth(), 0)
	testAssertRGB(t, 1, 0, 0, page.GetPenColor())
	if content := testContent(page); !strings.HasSuffix(content, "q\n3 w\n0 1 0 RG\nQ\n") {
		t.Errorf("content %q", content)
	}
}

func TestPageGettersReturnCopies(t *testing.T) {
	page := testNewPage()
	pen := page.GetPenColor()
	pen[0] = 1
	testAssertRGB(t, 0, 0, 0, page.GetPenColor())
	page.DrawLine(0, 0, 10, 10)
	page.GetContent()[0] = 'X'
	if testContent(page)[0] == 'X' {
		t.Error("GetContent returns the live buffer")
	}
}

func TestPageDrawLineWritesAStrokedPathWithTheYFlipped(t *testing.T) {
	page := testNewPage()
	page.DrawLine(10, 20, 30, 40)
	if content := testContent(page); !strings.Contains(content, "10 772 m\n30 752 l\nS\n") {
		t.Errorf("content %q", content)
	}
}
