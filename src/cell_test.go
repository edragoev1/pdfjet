// cell_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
)

func TestCellACellWithoutTextHasNoHeight(t *testing.T) {
	// Java's Cell(font) is NewEmptyCell in Go.
	cell := NewEmptyCell(testHelvetica(testNewPDF()))
	testNear(t, "height", 0, cell.GetHeight(100), 0)
}

func TestCellEmptyTextIsOneLineTallWithThePaddings(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "")
	testNear(t, "top padding", 2, cell.GetTopPadding(), 0)
	testNear(t, "bottom padding", 2, cell.GetBottomPadding(), 0)
	testNear(t, "height", 17.872, cell.GetHeight(100), testDelta)
}

func TestCellTheCellFontSizeSetsTheHeight(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	cell.SetFontSize(24)
	testNear(t, "height", 31.744, cell.GetHeight(100), testDelta)
}

func TestCellSettingATextBlockClearsTheText(t *testing.T) {
	font := testHelvetica(testNewPDF())
	withBlock := NewCell(font, "text").SetTextBlock(NewTextBlock(font, "block"))
	if withBlock.GetText() != "" {
		t.Errorf("text block: text %q", withBlock.GetText())
	}
}

func TestCellANewCellHasTopAndLeftBordersAndSpansOneColumn(t *testing.T) {
	// A table adds the right border of its last column and the bottom border of its last row.
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	if !cell.GetBorder(border.Top) || !cell.GetBorder(border.Left) {
		t.Error("no top or left border")
	}
	if cell.GetBorder(border.Right) || cell.GetBorder(border.Bottom) {
		t.Error("a right or bottom border")
	}
	if cell.GetColSpan() != 1 {
		t.Errorf("colspan %d", cell.GetColSpan())
	}
}

func TestCellSetBorderChangesOneBorderAndKeepsTheColumnSpan(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	cell.SetColSpan(3)
	cell.SetBorder(border.Top, false).SetBorder(border.Bottom, true)
	if cell.GetBorder(border.Top) || !cell.GetBorder(border.Left) || !cell.GetBorder(border.Bottom) {
		t.Error("wrong borders")
	}
	if cell.GetColSpan() != 3 {
		t.Errorf("colspan %d", cell.GetColSpan())
	}
	cell.SetBorders(false)
	if cell.GetBorder(border.Left) || cell.GetBorder(border.Bottom) || cell.GetColSpan() != 3 {
		t.Error("SetBorders(false) kept a border or changed the colspan")
	}
}

func TestCellTransparentLeavesTheTextColorUnchanged(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	cell.SetTextColor(color.Blue).SetTextColor(color.Transparent)
	testAssertRGB(t, 0, 0, 1, cell.GetTextColor())
}

func TestCellSetFontChangesTheFallbackFontUnlessAnotherWasSet(t *testing.T) {
	pdf := testNewPDF()
	helvetica := testHelvetica(pdf)
	courier := NewCoreFont(pdf, corefont.Courier())
	cell := NewCell(helvetica, "x").SetFont(courier)
	if cell.GetFallbackFont() != courier {
		t.Error("the fallback font did not change with the font")
	}
	cell.SetFallbackFont(helvetica).SetFont(NewCoreFont(pdf, corefont.TimesRoman()))
	if cell.GetFallbackFont() != helvetica {
		t.Error("setting the font replaced the fallback font that was set")
	}
}
