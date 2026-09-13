// textblock_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

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

func TestTextBlockWrappedTextMakesTheBlockTallerThanItsSetHeight(t *testing.T) {
	block := NewTextBlock(testHelvetica(testNewPDF()), testTenWords).SetSize(60, 10)
	block.SetLocation(0, 0)
	// Six lines of 13.872 points.
	testAssertXY(t, 60, 83.232, block.DrawOn(nil))
	testNear(t, "height", 83.232, block.GetHeight(), testDelta)
	testNear(t, "width", 60, block.GetWidth(), 0)
}

func TestTextBlockDrawOnAPageWritesEveryWord(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	block := NewTextBlock(testHelvetica(pdf), testTenWords).SetSize(60, 10)
	block.SetLocation(0, 0)
	testAssertXY(t, 60, 83.232, block.DrawOn(page))
	content := testContent(page)
	if !strings.Contains(content, testHex("one")) || !strings.Contains(content, testHex("ten")) {
		t.Errorf("words missing from %q", content)
	}
}
