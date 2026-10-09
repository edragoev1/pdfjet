// textline_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestTextLineTheLinkOfAWordDrawnWithItsSpaceEndsAtTheWord(t *testing.T) {
	// A word of a TextColumn, or of a justified TextFrame row, is drawn with
	// the space after it, and its link reached one space past the word. The
	// box now holds the text that shows.
	words := []string{"Click", "here", "for", "the", "whole", "story", "of", "it"}
	text := strings.Join(words, " ")
	check := func(what string, page *Page) {
		t.Helper()
		if len(page.annots) != len(words) {
			t.Fatalf("%s: %d links, not %d", what, len(page.annots), len(words))
		}
		font := testHelvetica(page.pdf)
		for i, annot := range page.annots {
			want := NewTextLine(font, words[i]).GetWidth()
			testNear(t, what+" "+words[i], want, annot.x2-annot.x1, testDelta)
		}
	}

	page := testNewPage()
	font := testHelvetica(page.pdf)
	column := NewTextColumn()
	column.SetWidth(120)
	column.SetLocation(50, 50)
	column.AddParagraph(NewParagraph().Add(NewTextLine(font, text).SetURIAction("https://pdfjet.com")))
	column.DrawOn(page)
	check("TextColumn", page)

	// Rows of two words, justified and so drawn a word at a time, and a last
	// row of one, drawn as it is.
	words = []string{"word", "word", "word", "word", "word"}
	page = testNewPage()
	font = testHelvetica(page.pdf)
	paragraph := NewParagraph().SetTextAlignment(alignment.Justify)
	paragraph.Add(NewTextLine(font, strings.Join(words, " ")).SetURIAction("https://pdfjet.com"))
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).
		SetWidth(NewTextLine(font, "word word").GetWidth() + 2)
	frame.SetLocation(50, 50)
	frame.DrawOn(page)
	check("justified TextFrame", page)

	// A text line of its own keeps the spaces it is given out of its box too.
	page = testNewPage()
	font = testHelvetica(page.pdf)
	line := NewTextLine(font, "  link  ").SetURIAction("https://pdfjet.com")
	line.SetLocation(100, 100)
	line.DrawOn(page)
	testNear(t, "left", 100+NewTextLine(font, "  ").GetWidth(), page.annots[0].x1, testDelta)
	testNear(t, "width", NewTextLine(font, "link").GetWidth(), page.annots[0].x2-page.annots[0].x1, testDelta)
}

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

func TestTextLineTheUnderlineAndTheStrikeoutOfTaggedTextAreArtifacts(t *testing.T) {
	// The line is decoration: an element of its own, described as "Underlined
	// text: " and the text, is read after the text again.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	doc.pdf.SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	line := NewTextLine(testHelvetica(doc.pdf), "Hello")
	line.SetUnderline(true)
	line.SetStrikeout(true)
	line.SetLocation(10, 20)
	line.DrawOn(page)
	if got := strings.Count(testContent(page), "/Artifact BMC\n"); got != 2 {
		t.Errorf("the two lines are %d artifacts", got)
	}
	raw := string(doc.complete())
	if got := strings.Count(raw, "/S /P\n"); got != 1 {
		t.Errorf("the text line is %d elements, not one", got)
	}
	if strings.Contains(raw, "/Alt ") {
		t.Error("the lines describe themselves")
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
