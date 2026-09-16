// cell_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/border"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
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

// testBox is a drawable that is 30 wide and 20 high, and remembers where it
// was placed and how many times it was drawn.
type testBox struct {
	x, y  float32
	draws int
}

func (box *testBox) DrawOn(page *Page) [2]float32 {
	if page != nil {
		box.draws++
	}
	return [2]float32{box.x + 30, box.y + 20}
}

func (box *testBox) SetLocation(x, y float32) Drawable {
	box.x = x
	box.y = y
	return box
}

func TestCellTheLastContentSetterWins(t *testing.T) {
	font := testHelvetica(testNewPDF())
	cell := NewCell(font, "text")
	barcode := NewBarcode(CODE_128, "x")
	cell.SetTextBlock(NewTextBlock(font, "block")).SetBarcode(barcode)
	if cell.GetTextBlock() != nil || cell.GetBarcode() != barcode || cell.GetDrawable() != Drawable(barcode) {
		t.Error("the barcode did not replace the text block")
	}
	box := &testBox{}
	cell.SetDrawable(box)
	if cell.GetBarcode() != nil || cell.GetImage() != nil || cell.GetTextColumn() != nil {
		t.Error("a typed getter returns content the cell no longer holds")
	}
	if cell.GetDrawable() != Drawable(box) || cell.GetText() != "" {
		t.Error("the drawable did not replace the barcode, or the text is not clear")
	}
	if cell.SetImage(nil).GetDrawable() != nil {
		t.Error("a nil image leaves a drawable")
	}
}

func TestCellAnyDrawableIsMeasuredAndAlignedInTheCell(t *testing.T) {
	pdf := testNewPDF()
	box := &testBox{}
	cell := NewEmptyCell(testHelvetica(pdf)).SetDrawable(box)
	testNear(t, "height", 24, cell.GetHeight(100), testDelta) // 20 and the paddings of 2
	page := NewPage(pdf, letter.Portrait())
	cell.drawOn(page, 10, 50, 100, 24)
	testAssertXY(t, 12, 52, [2]float32{box.x, box.y})
	cell.SetTextAlignment(alignment.Center).drawOn(page, 10, 50, 100, 24)
	testAssertXY(t, 45, 52, [2]float32{box.x, box.y})
	cell.SetTextAlignment(alignment.Right).drawOn(page, 10, 50, 100, 24)
	testAssertXY(t, 78, 52, [2]float32{box.x, box.y})
	if box.draws != 3 {
		t.Errorf("draws %d", box.draws)
	}
}

func TestCellTextSetAfterTheDrawableIsDrawnAndMeasuredInstead(t *testing.T) {
	pdf := testNewPDF()
	box := &testBox{}
	cell := NewEmptyCell(testHelvetica(pdf)).SetDrawable(box)
	cell.SetText("x")
	testNear(t, "height", 17.872, cell.GetHeight(100), testDelta)
	cell.drawOn(NewPage(pdf, letter.Portrait()), 10, 50, 100, 24)
	if box.draws != 0 || cell.GetDrawable() != Drawable(box) {
		t.Errorf("draws %d", box.draws)
	}
}

func TestCellATableFitsItsColumnsToAnyDrawable(t *testing.T) {
	font := testHelvetica(testNewPDF())
	data := [][]*Cell{{NewCell(font, "a")}, {NewEmptyCell(font).SetDrawable(&testBox{})}}
	table := NewTable().SetTableData(data, 1).AutoAdjustColumnWidths()
	testNear(t, "width", 34, table.GetColumnWidth(0), testDelta)
}
