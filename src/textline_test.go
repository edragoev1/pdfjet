// textline_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestTextLineDrawOnWritesTheTextAsHexAtTheFlippedY(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	line := NewTextLine(testHelvetica(pdf), "Hello (x)")
	line.SetLocation(10, 20)
	xy := line.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "10 772 Td\n") {
		t.Errorf("no text location in %q", content)
	}
	if !strings.Contains(content, "[<"+testHex("Hello (x)")+">] TJ\n") {
		t.Errorf("no text in %q", content)
	}
	testNear(t, "width", 44.664, line.GetWidth(), 0.001)
	testNear(t, "x", 10+line.GetWidth(), xy[0], testDelta)
}

func TestTextLineEmptyTextDrawsNothingAndReturnsTheLocation(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	line := NewTextLine(testHelvetica(pdf), "")
	line.SetLocation(5, 6)
	testAssertXY(t, 5, 6, line.DrawOn(page))
	if len(page.GetContent()) != 0 {
		t.Errorf("content %q", testContent(page))
	}
}

func TestTextLineGetLocationReturnsTheLocation(t *testing.T) {
	line := NewTextLine(testHelvetica(testNewPDF()), "x")
	line.SetLocation(5, 6)
	testAssertXY(t, 5, 6, line.GetLocation())
}

func TestTextLineColorSettersConvertAndCopy(t *testing.T) {
	line := NewTextLine(testHelvetica(testNewPDF()), "x")
	line.SetTextColor(0xFF8000)
	testAssertRGB(t, 1, 128.0/255.0, 0, line.GetTextColor())

	// Go arrays are values, so the setter and the getter copy them.
	rgb := [3]float32{0.1, 0.2, 0.3}
	line.SetTextColorRGB(rgb)
	rgb[0] = 0.9
	color := line.GetTextColor()
	color[1] = 0.9
	testAssertRGB(t, 0.1, 0.2, 0.3, line.GetTextColor())
}

func TestTextLineUnderlineAddsAStrokedLine(t *testing.T) {
	pdf := testNewPDF()
	plain := NewPage(pdf, letter.Portrait())
	line := NewTextLine(testHelvetica(pdf), "Hello")
	line.SetLocation(10, 20)
	line.DrawOn(plain)
	if strings.Contains(testContent(plain), "\nS\n") {
		t.Error("plain text has a stroke")
	}

	underlined := NewPage(pdf, letter.Portrait())
	line = NewTextLine(testHelvetica(pdf), "Hello").SetUnderline(true)
	line.SetLocation(10, 20)
	line.DrawOn(underlined)
	if !strings.Contains(testContent(underlined), "\nS\n") {
		t.Error("underlined text has no stroke")
	}
}

func TestTextLineSetFontChangesTheFallbackFontUnlessAnotherWasSet(t *testing.T) {
	pdf := testNewPDF()
	helvetica := testHelvetica(pdf)
	courier := NewCoreFont(pdf, corefont.Courier())
	line := NewTextLine(helvetica, "x").SetFont(courier)
	if line.GetFallbackFont() != courier {
		t.Error("the fallback font did not change with the font")
	}
	line.SetFallbackFont(helvetica).SetFont(NewCoreFont(pdf, corefont.TimesRoman()))
	if line.GetFallbackFont() != helvetica {
		t.Error("setting the font replaced the fallback font that was set")
	}
}
