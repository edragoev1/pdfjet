// compositetextline_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/scriptposition"
)

func testWater(font *Font, fontSize float32) *CompositeTextLine {
	composite := NewCompositeTextLine(100, 100)
	if fontSize > 0 {
		composite.SetFontSize(fontSize)
	}
	h := NewTextLine(font, "H")
	two := NewTextLine(font, "2")
	two.SetScriptPosition(scriptposition.Subscript)
	o := NewTextLine(font, "O")
	composite.AddComponent(h)
	composite.AddComponent(two)
	composite.AddComponent(o)
	return composite
}

func TestCompositeTextLineASubscriptIsSmallerAndLowerWithoutSettingTheFontSize(t *testing.T) {
	font := testHelvetica(testNewPDF())
	// The font size of the components is the base when the composite text line
	// has none of its own.
	composite := testWater(font, 0)
	two := composite.GetTextLine(1)
	testNear(t, "size", font.GetSize()*composite.GetSubscriptFactor(), two.GetFontSize(), testDelta)
	testNear(t, "baseline", 100+font.GetSize()*composite.GetSubscriptPosition(),
		two.GetLocation()[1], testDelta)
	// The same composite text line with the font size set draws the same.
	withSize := testWater(font, font.GetSize())
	testNear(t, "size", two.GetFontSize(), withSize.GetTextLine(1).GetFontSize(), testDelta)
	testNear(t, "baseline", two.GetLocation()[1], withSize.GetTextLine(1).GetLocation()[1], testDelta)
}

func TestCompositeTextLineTheFontSizeSetAfterTheComponentsWereAddedStillReachesThem(t *testing.T) {
	font := testHelvetica(testNewPDF())
	composite := NewCompositeTextLine(100, 100)
	h := NewTextLine(font, "H")
	two := NewTextLine(font, "2")
	two.SetScriptPosition(scriptposition.Subscript)
	composite.AddComponent(h).AddComponent(two)
	composite.SetFontSize(24)
	testNear(t, "base size", 24, h.GetFontSize(), testDelta)
	testNear(t, "script size", 24*composite.GetSubscriptFactor(), two.GetFontSize(), testDelta)
	testNear(t, "baseline", 100+24*composite.GetSubscriptPosition(), two.GetLocation()[1], testDelta)
}

func TestCompositeTextLineTheHeightIsTheExtentOfWhatIsDrawn(t *testing.T) {
	pdf := testNewPDF()
	big := NewCoreFont(pdf, corefont.Helvetica())
	big.SetSize(24)
	small := NewCoreFont(pdf, corefont.Helvetica())
	small.SetSize(8)
	// The components come from fonts of their own sizes, and the composite
	// text line draws them at 12 points and at the subscript size.
	composite := NewCompositeTextLine(100, 100)
	composite.SetFontSize(12)
	h := NewTextLine(big, "H")
	two := NewTextLine(small, "2")
	two.SetScriptPosition(scriptposition.Subscript)
	composite.AddComponent(h).AddComponent(two)
	minMax := composite.GetMinMaxY()
	testNear(t, "top", 100-big.GetAscent(12), minMax[0], testDelta)
	testNear(t, "bottom", two.GetLocation()[1]+small.GetDescent(two.GetFontSize()),
		minMax[1], testDelta)
	testNear(t, "height", minMax[1]-minMax[0], composite.GetHeight(), testDelta)
}

func TestCompositeTextLineAnEmptyCompositeTextLineMeasuresItsOwnLocation(t *testing.T) {
	composite := NewCompositeTextLine(100, 50)
	testAssertXY(t, 100, 50, composite.DrawOn(nil))
	if composite.GetWidth() != 0 || composite.GetHeight() != 0 {
		t.Errorf("width %f height %f", composite.GetWidth(), composite.GetHeight())
	}
}

func TestCompositeTextLineTheWidthAndTheLocationSurviveASecondSetLocation(t *testing.T) {
	font := testHelvetica(testNewPDF())
	composite := testWater(font, 12)
	width := composite.GetWidth()
	composite.SetLocation(200, 300)
	composite.SetLocation(200, 300)
	testNear(t, "width", width, composite.GetWidth(), testDelta)
	testNear(t, "x", 200, composite.GetTextLine(0).GetLocation()[0], testDelta)
	testNear(t, "y", 300, composite.GetTextLine(0).GetLocation()[1], testDelta)
}

func TestCompositeTextLineAFormulaSubscriptsTheAtomCounts(t *testing.T) {
	font := testHelvetica(testNewPDF())
	water := NewCompositeTextLine(0, 0)
	water.AddFormula(font, "H2O")
	if water.GetNumberOfTextLines() != 3 {
		t.Fatalf("components %d", water.GetNumberOfTextLines())
	}
	if water.GetTextLine(0).GetText() != "H" || water.GetTextLine(1).GetText() != "2" ||
		water.GetTextLine(2).GetText() != "O" {
		t.Error("the formula is not split into its runs")
	}
	if water.GetTextLine(1).GetScriptPosition() != scriptposition.Subscript ||
		water.GetTextLine(0).GetScriptPosition() != scriptposition.Normal {
		t.Error("the atom count is not a subscript")
	}
	if water.GetTextLine(1).GetFontSize() >= water.GetTextLine(0).GetFontSize() {
		t.Error("the subscript is not smaller than the base")
	}

	glucose := NewCompositeTextLine(0, 0)
	glucose.AddFormula(font, "C6H12O6")
	if glucose.GetNumberOfTextLines() != 6 || glucose.GetTextLine(3).GetText() != "12" ||
		glucose.GetTextLine(3).GetScriptPosition() != scriptposition.Subscript {
		t.Error("C6H12O6 is not split into six runs with 12 as a subscript")
	}

	// A digit that begins the formula counts the molecules, not the atoms.
	coefficient := NewCompositeTextLine(0, 0)
	coefficient.AddFormula(font, "2H2O")
	if coefficient.GetTextLine(0).GetText() != "2H" ||
		coefficient.GetTextLine(0).GetScriptPosition() != scriptposition.Normal ||
		coefficient.GetTextLine(1).GetScriptPosition() != scriptposition.Subscript {
		t.Error("the leading coefficient is not drawn on the baseline")
	}

	// The digits of a group in brackets follow the bracket.
	lime := NewCompositeTextLine(0, 0)
	lime.AddFormula(font, "Ca(OH)2")
	if lime.GetTextLine(0).GetText() != "Ca(OH)" ||
		lime.GetTextLine(1).GetScriptPosition() != scriptposition.Subscript {
		t.Error("the count after a bracket is not a subscript")
	}
}

func TestCompositeTextLineAFormulaSuperscriptsTheChargeAfterACircumflex(t *testing.T) {
	font := testHelvetica(testNewPDF())
	calcium := NewCompositeTextLine(0, 0)
	calcium.AddFormula(font, "Ca^2+")
	if calcium.GetNumberOfTextLines() != 2 || calcium.GetTextLine(0).GetText() != "Ca" ||
		calcium.GetTextLine(1).GetText() != "2+" ||
		calcium.GetTextLine(1).GetScriptPosition() != scriptposition.Superscript {
		t.Error("the charge is not a superscript")
	}
	if calcium.GetTextLine(1).GetLocation()[1] >= calcium.GetTextLine(0).GetLocation()[1] {
		t.Error("the superscript is not above the baseline")
	}

	sulfate := NewCompositeTextLine(0, 0)
	sulfate.AddFormula(font, "SO4^2-")
	if sulfate.GetNumberOfTextLines() != 3 ||
		sulfate.GetTextLine(1).GetScriptPosition() != scriptposition.Subscript ||
		sulfate.GetTextLine(2).GetScriptPosition() != scriptposition.Superscript {
		t.Error("SO4^2- is not a subscript and a superscript")
	}

	// Nothing at all is one component of nothing.
	empty := NewCompositeTextLine(0, 0)
	empty.AddFormula(font, "")
	if empty.GetNumberOfTextLines() != 0 {
		t.Errorf("components %d", empty.GetNumberOfTextLines())
	}
}

func TestCompositeTextLineACellDrawsACompositeTextLineWithoutAnyText(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	cell := NewEmptyCell(font)
	composite := NewCompositeTextLine(0, 0)
	composite.AddFormula(font, "H2O")
	cell.SetCompositeTextLine(composite)
	// The cell is as tall as the taller of its font and the composite.
	expected := font.GetBodyHeight(font.GetSize())
	if composite.GetHeight() > expected {
		expected = composite.GetHeight()
	}
	expected += cell.GetTopPadding() + cell.GetBottomPadding()
	testNear(t, "cell height", expected, cell.GetHeight(100), testDelta)
	cell.drawOn(page, 50, 50, 100, cell.GetHeight(100))
	content := testContent(page)
	if !strings.Contains(content, testHex("H")) || !strings.Contains(content, testHex("2")) ||
		!strings.Contains(content, testHex("O")) {
		t.Error("the cell does not draw the composite text line")
	}
}
