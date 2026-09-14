// textblock_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

const testTenWords = "one two three four five six seven eight nine ten"

func testMeasureBlock(font *Font, text string) [2]float32 {
	block := NewTextBlock(font, text)
	block.SetLocation(0, 0)
	return block.DrawOn(nil)
}

func TestTextBlockANewlineIsOneEmptyLine(t *testing.T) {
	font := testHelvetica(testNewPDF())
	x := testMeasureBlock(font, "x")
	testAssertXY(t, 500, 13.872, x)
	testAssertXY(t, x[0], x[1], testMeasureBlock(font, "\n"))
	testAssertXY(t, x[0], x[1], testMeasureBlock(font, ""))
}

func TestTextBlockWithoutAHeightTheBlockIsAsTallAsItsText(t *testing.T) {
	block := NewTextBlock(testHelvetica(testNewPDF()), testTenWords).SetWidth(60)
	block.SetLocation(0, 0)
	// Six lines of 13.872 points.
	testAssertXY(t, 60, 83.232, block.DrawOn(nil))
	testNear(t, "height", 83.232, block.GetHeight(), testDelta)
	testNear(t, "width", 60, block.GetWidth(), 0)
}

func TestTextBlockAHeightCutsTheTextThatDoesNotFitAndAlignsTheRest(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	block := NewTextBlock(font, testTenWords).SetSize(60, 30)
	block.SetLocation(0, 0)
	// Two of the six lines fit
	testAssertXY(t, 60, 30, block.DrawOn(nil))
	testNear(t, "height", 30, block.GetHeight(), 0)
	page := NewPage(pdf, testLetterPortrait())
	block.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("...")) || strings.Contains(content, testHex("ten")) {
		t.Errorf("cut text in %q", content)
	}

	// Aligned to the bottom of a block 10 points taller than its two lines,
	// the text sits where a block without a height draws it 10 points lower
	page = NewPage(pdf, testLetterPortrait())
	block.SetSize(60, 2*13.872+10).SetVerticalAlignment(alignment.Bottom).DrawOn(page)
	bottom := testContent(page)
	page = NewPage(pdf, testLetterPortrait())
	lowerBlock := NewTextBlock(font, "one two three four").SetWidth(60)
	lowerBlock.SetLocation(0, 10)
	lowerBlock.DrawOn(page)
	lower := testContent(page)
	start := strings.Index(lower, "1 0 0 1 0 ")
	textMatrix := lower[start : strings.Index(lower, " Tm\n")+4]
	if !strings.Contains(bottom, textMatrix) {
		t.Errorf("%q missing from %q", textMatrix, bottom)
	}
}

func TestTextBlockStrikeoutDrawsALineThroughEachLine(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	block := NewTextBlock(testHelvetica(pdf), "one\ntwo").SetStrikeout(true)
	block.SetLocation(0, 0)
	block.DrawOn(page)
	if lines := strings.Count(testContent(page), " l\n"); lines != 2 {
		t.Errorf("%d lines in %q", lines, testContent(page))
	}
}

func TestTextBlockDrawOnAPageWritesEveryWord(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	block := NewTextBlock(testHelvetica(pdf), testTenWords).SetWidth(60)
	block.SetLocation(0, 0)
	testAssertXY(t, 60, 83.232, block.DrawOn(page))
	content := testContent(page)
	if !strings.Contains(content, testHex("one")) || !strings.Contains(content, testHex("ten")) {
		t.Errorf("words missing from %q", content)
	}
}

func TestTextBlockTransparentLeavesTheTextColorUnchanged(t *testing.T) {
	block := NewTextBlock(testHelvetica(testNewPDF()), "x")
	block.SetTextColor(color.Blue).SetTextColor(color.Transparent)
	testAssertRGB(t, 0, 0, 1, block.GetTextColor())
}

func TestTextBlockSetFontChangesTheFallbackFontUnlessAnotherWasSet(t *testing.T) {
	pdf := testNewPDF()
	helvetica := testHelvetica(pdf)
	courier := NewCoreFont(pdf, corefont.Courier())
	block := NewTextBlock(helvetica, "x").SetFont(courier)
	if block.fallbackFont != courier {
		t.Error("the fallback font did not change with the font")
	}
	block.SetFallbackFont(helvetica).SetFont(NewCoreFont(pdf, corefont.TimesRoman()))
	if block.fallbackFont != helvetica {
		t.Error("setting the font replaced the fallback font that was set")
	}
}
