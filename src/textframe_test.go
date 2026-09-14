// textframe_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"testing"
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
	line := testHelvetica(testNewPDF()).GetBodyHeight()
	testNear(t, "default", 2*line, testParagraphDistance(-1), testDelta) // one empty line
	testNear(t, "gap 0", line, testParagraphDistance(0), testDelta)
	testNear(t, "gap 10", line+10, testParagraphDistance(10), testDelta)
}
