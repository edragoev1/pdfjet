// textframe_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// testParagraphDistance draws two paragraphs of one line and returns how far
// below the first the second starts. A negative gap leaves the default.
func testParagraphDistance(gap float32) float32 {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	first := NewParagraph().Add(NewTextLine(font, "one"))
	second := NewParagraph().Add(NewTextLine(font, "two"))
	frame := NewTextFrameFromParagraphs([]*Paragraph{first, second}).SetWidth(300)
	frame.SetLocation(10, 10)
	if gap >= 0 {
		frame.SetParagraphGap(gap)
	}
	frame.DrawOn(NewPage(pdf, testLetterPortrait()))
	return second.GetY1() - first.GetY1()
}

func TestTextFrameTheGapIsAddedToTheLineSoParagraphsNeverOverlap(t *testing.T) {
	helvetica := testHelvetica(testNewPDF())
	line := helvetica.GetBodyHeight(helvetica.GetSize())
	testNear(t, "default", 2*line, testParagraphDistance(-1), testDelta) // one empty line
	testNear(t, "gap 0", line, testParagraphDistance(0), testDelta)
	testNear(t, "gap 10", line+10, testParagraphDistance(10), testDelta)
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
