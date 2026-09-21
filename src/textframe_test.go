// textframe_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testParagraphDistance draws two paragraphs of one line and returns how far
// below the first the second starts. With no gap the default is left.
func testParagraphDistance(gap ...float32) float32 {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	first := NewParagraph().Add(NewTextLine(font, "one"))
	second := NewParagraph().Add(NewTextLine(font, "two"))
	frame := NewTextFrameFromParagraphs([]*Paragraph{first, second}).SetWidth(300)
	frame.SetLocation(10, 10)
	if len(gap) > 0 {
		frame.SetParagraphGap(gap[0])
	}
	frame.DrawOn(NewPage(pdf, testLetterPortrait()))
	return second.GetY1() - first.GetY1()
}

func TestTextFrameTheGapIsAddedToTheLineSoParagraphsNeverOverlap(t *testing.T) {
	helvetica := testHelvetica(testNewPDF())
	line := helvetica.GetBodyHeight(helvetica.GetSize())
	testNear(t, "default", 2*line, testParagraphDistance(), testDelta) // one empty line
	testNear(t, "gap 0", line, testParagraphDistance(0), testDelta)
	testNear(t, "gap 10", line+10, testParagraphDistance(10), testDelta)
}

func TestTextFrameANegativeGapIsTakenAsZero(t *testing.T) {
	testNear(t, "gap -5", testParagraphDistance(0), testParagraphDistance(-5), testDelta)
}

func TestTextFrameTheDefaultGapIsAnEmptyLineOfTheNextParagraph(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	heading := NewParagraph().Add(NewTextLine(font, "Heading").SetFontSize(24))
	body := NewParagraph().Add(NewTextLine(font, "body"))
	frame := NewTextFrameFromParagraphs([]*Paragraph{heading, body}).SetWidth(300)
	frame.SetLocation(10, 10)
	frame.DrawOn(NewPage(pdf, testLetterPortrait()))
	// The heading, then one empty line in the size of the body text
	want := font.GetBodyHeight(24) + font.GetBodyHeight(font.GetSize())
	testNear(t, "distance", want, body.GetY1()-heading.GetY1(), testDelta)
}

// testDrawAligned draws one paragraph with the alignment in a frame 200 wide
// at x 10, and returns it.
func testDrawAligned(textAlignment alignment.Alignment, text string) *Paragraph {
	pdf := testNewPDF()
	paragraph := NewParagraph().Add(NewTextLine(testHelvetica(pdf), text))
	paragraph.SetTextAlignment(textAlignment)
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetWidth(200)
	frame.SetLocation(10, 10)
	frame.DrawOn(NewPage(pdf, testLetterPortrait()))
	return paragraph
}

// testTextHeight draws one paragraph in a frame of the width at x 0, and
// returns how far down its text reaches.
func testTextHeight(text string, width float32) float32 {
	pdf := testNewPDF()
	paragraph := NewParagraph().Add(NewTextLine(testHelvetica(pdf), text))
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetWidth(width)
	frame.SetLocation(0, 10)
	frame.DrawOn(NewPage(pdf, testLetterPortrait()))
	return paragraph.GetY2() - paragraph.GetY1()
}

func TestTextFrameARowTakesTheWordsThatFitWithoutTheSpaceAfterThem(t *testing.T) {
	font := testHelvetica(testNewPDF())
	oneRow := testTextHeight("one two", 300)
	width := font.StringWidth(font.GetSize(), "one ") + font.StringWidth(font.GetSize(), "two")
	testNear(t, "one row", oneRow, testTextHeight("one two", width), testDelta)
	if testTextHeight("one two", width-0.1) <= oneRow {
		t.Error("the text is not two rows")
	}
	// A word as wide as the frame is not broken.
	testNear(t, "a whole word", oneRow, testTextHeight("Hello", font.StringWidth(font.GetSize(), "Hello")), testDelta)
}

func TestTextFrameARightAlignedParagraphEndsAtTheRightEdge(t *testing.T) {
	font := testHelvetica(testNewPDF())
	paragraph := testDrawAligned(alignment.Right, "Hello")
	// The text ends at the right edge; the space after it is past the edge.
	testNear(t, "text x", 210-font.StringWidth(font.GetSize(), "Hello"), paragraph.GetTextX(), testDelta)
	testNear(t, "x2", 210+font.StringWidth(font.GetSize(), " "), paragraph.GetX2(), testDelta)
}

func TestTextFrameACenteredParagraphHasTheSameSpaceOnBothSides(t *testing.T) {
	font := testHelvetica(testNewPDF())
	paragraph := testDrawAligned(alignment.Center, "Hello")
	testNear(t, "text x", 10+(200-font.StringWidth(font.GetSize(), "Hello"))/2, paragraph.GetTextX(), testDelta)
}

func TestTextFrameAJustifiedParagraphLeavesItsLastRowAsItIs(t *testing.T) {
	text := "one two three four five six seven eight nine ten eleven twelve thirteen"
	left := testDrawAligned(alignment.Left, text)
	justified := testDrawAligned(alignment.Justify, text)
	if left.GetY2()-left.GetY1() <= 20 {
		t.Error("the text is not more than one row")
	}
	testNear(t, "y2", left.GetY2(), justified.GetY2(), testDelta)
	testNear(t, "x2", left.GetX2(), justified.GetX2(), testDelta)
}

func TestTextFrameParagraphsWithALabelAreAList(t *testing.T) {
	// The label of an item is drawn where the item begins, so that it reads
	// before the text of the item and not after all of the text.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	paragraphs := make([]*Paragraph, 0)
	for i, text := range []string{"alpha beta", "gamma delta"} {
		paragraphs = append(paragraphs, NewParagraph().
			Add(NewTextLine(font, text)).
			SetListLabel(NewTextLine(font, strconv.Itoa(i+1)+"."), 15))
	}
	// A paragraph with no label ends the list.
	paragraphs = append(paragraphs, NewParagraph().Add(NewTextLine(font, "epsilon")))
	frame := NewTextFrameFromParagraphs(paragraphs)
	frame.SetLocation(70, 50)
	frame.SetWidth(300)
	page := NewPage(doc.pdf, letter.Portrait())
	frame.DrawOn(page)
	content := testContent(page)
	// The label of an item is drawn before the text of the item.
	if strings.Index(content, testHex("1.")) > strings.Index(content, testHex("alpha")) {
		t.Error("the label of the first item is drawn after its text")
	}
	raw := string(doc.complete())
	counts := map[string]int{
		"/S /L\n": 1, "/S /LI\n": 2, "/S /Lbl\n": 2, "/S /LBody\n": 2,
	}
	for text, want := range counts {
		if got := strings.Count(raw, text); got != want {
			t.Errorf("%q is in the PDF %d times, not %d", text, got, want)
		}
	}
}
