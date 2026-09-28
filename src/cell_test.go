// cell_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"strings"
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

func TestCellTheUnderlineAndTheStrikeoutAreDrawnInTheTextColor(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	cell := NewCell(testHelvetica(pdf), "Text")
	cell.SetTextColor(color.Red).SetBorderColor(color.Blue).SetBorderWidth(2.0).
		SetBackgroundColor(color.Yellow).SetUnderline(true).SetStrikeout(true)
	cell.drawOn(page, 10, 50, 100, 20)
	content := testContent(page)
	// The text, the underline and the strikeout are red; only the borders are blue.
	if strings.Index(content, "1 0 0 RG") > strings.Index(content, "0 0 1 RG") {
		t.Error("the underline is not drawn in the text color")
	}
	if strings.Count(content, "0 0 1 RG") != 1 {
		t.Error("the border color is written more than once")
	}
	// Each border starts half the pen width back, so that the corners close.
	if !strings.Contains(content, "9 742 m") {
		t.Error("the corners of the borders do not close")
	}
}

func TestCellTheBordersOfACellAreOnePathAndNoBorderWritesNothing(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	NewCell(font, "x").drawOn(page, 10, 50, 100, 20) // the top and left borders
	content := testContent(page)
	if strings.Count(content, " m\n") != 2 {
		t.Error("the borders are not two subpaths")
	}
	if strings.Count(content, "\nS\n") != 1 {
		t.Error("the borders are not stroked once")
	}
	bare := NewPage(pdf, letter.Portrait())
	NewCell(font, "x").SetBorders(false).drawOn(bare, 10, 50, 100, 20)
	content = testContent(bare)
	if strings.Contains(content, "S") || strings.Contains(content, " w") {
		t.Error("a cell without borders writes a path or a pen width")
	}
}

func TestCellTransparentLeavesTheBordersTheColorOfThePen(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	cell.SetBorderColor(color.Blue).SetBorderColor(color.Transparent)
	if cell.GetBorderColor() != nil {
		t.Error("the border color is set")
	}
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

// The four paddings are the four bytes of one uint32, as the comment of Cell
// has them, top in the lowest: a top of 2, a bottom of 3.5, a left of 1.25
// and a right of 0 are 0x00050E08, and setting one leaves the others.
func TestCellTheFourPaddingsAreTheBytesOfOneInteger(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	cell.SetTopPadding(2).SetBottomPadding(3.5).SetLeftPadding(1.25).SetRightPadding(0)
	if cell.padding != 0x00050E08 {
		t.Errorf("the paddings are 0x%08X", cell.padding)
	}
	testNear(t, "top", 2, cell.GetTopPadding(), 0)
	testNear(t, "bottom", 3.5, cell.GetBottomPadding(), 0)
	testNear(t, "left", 1.25, cell.GetLeftPadding(), 0)
	testNear(t, "right", 0, cell.GetRightPadding(), 0)
}

// A padding is kept to the nearest quarter of a point, a half up, between 0
// and 63.75; one below 0 or not a number is 0, and one above is 63.75, as in
// the other ports, whatever the conversion of a float to an int makes of it.
func TestCellAPaddingIsKeptToTheNearestQuarterBetween0And63_75(t *testing.T) {
	cell := NewCell(testHelvetica(testNewPDF()), "x")
	for _, c := range []struct{ set, want float32 }{
		{2.1, 2}, {2.13, 2.25}, {2.125, 2.25}, {63.75, 63.75}, {100, 63.75}, {-5, 0},
		{float32(math.NaN()), 0}, {float32(math.Inf(1)), 63.75}, {float32(math.Inf(-1)), 0}, {1e10, 63.75},
	} {
		cell.SetTopPadding(c.set)
		testNear(t, fmt.Sprint(c.set), c.want, cell.GetTopPadding(), 0)
	}
}
