// textcolumn_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

const testEightWords = "alpha beta gamma delta epsilon zeta eta theta"

func testColumn(font *Font, text string, textAlignment alignment.Alignment) *TextColumn {
	column := NewTextColumn()
	column.SetWidth(200)
	column.SetTextAlignment(textAlignment)
	paragraph := NewParagraph()
	paragraph.Add(NewTextLine(font, text))
	column.AddParagraph(paragraph)
	column.SetLocation(100, 100)
	return column
}

// testPositions returns the x and y of every Td of the content, in order.
func testPositions(content string) [][2]float32 {
	positions := make([][2]float32, 0)
	for _, line := range strings.Split(content, "\n") {
		if strings.HasSuffix(line, " Td") {
			parts := strings.Split(line, " ")
			x, _ := strconv.ParseFloat(parts[0], 32)
			y, _ := strconv.ParseFloat(parts[1], 32)
			positions = append(positions, [2]float32{float32(x), float32(y)})
		}
	}
	return positions
}

func testLastXOfFirstLine(positions [][2]float32) float32 {
	y := positions[0][1]
	var lastX float32
	for _, position := range positions {
		if position[1] == y {
			lastX = position[0]
		}
	}
	return lastX
}

func TestTextColumnAParagraphIsAsTallAsItsTextAndNoTaller(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	// The ascent and the descent of one line, not a whole line of spacing.
	column := testColumn(font, "one short line", alignment.Left)
	testNear(t, "height", font.GetBodyHeight(font.GetSize()),
		column.GetSize().GetHeight(), testDelta)
	// A cell measures the column as it measures its own text.
	withColumn := NewEmptyCell(font)
	inCell := NewTextColumn()
	inCell.SetWidth(200)
	paragraph := NewParagraph()
	paragraph.Add(NewTextLine(font, "one short line"))
	inCell.AddParagraph(paragraph)
	withColumn.SetTextColumn(inCell)
	testNear(t, "cell height", NewCell(font, "one short line").GetHeight(200),
		withColumn.GetHeight(200), testDelta)
}

func TestTextColumnRightAlignedTextReachesTheRightEdge(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	testColumn(font, testEightWords, alignment.Right).DrawOn(page)
	// "zeta" is the last token of the first line; its space is not text.
	lastX := testLastXOfFirstLine(testPositions(testContent(page)))
	testNear(t, "right edge", 300, lastX+font.StringWidth(font.GetSize(), "zeta"), testDelta)
}

func TestTextColumnAJustifiedLineReachesBothEdges(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	testColumn(font, testEightWords+" iota kappa", alignment.Justify).DrawOn(page)
	positions := testPositions(testContent(page))
	testNear(t, "left edge", 100, positions[0][0], testDelta)
	testNear(t, "right edge", 300,
		testLastXOfFirstLine(positions)+font.StringWidth(font.GetSize(), "zeta"), testDelta)
}

func TestTextColumnAWordWiderThanTheColumnLeavesNoBlankLineAboveIt(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	wide := NewTextColumn()
	wide.SetWidth(120)
	paragraph := NewParagraph()
	paragraph.Add(NewTextLine(font, "Supercalifragilisticexpialidocious bbb ccc"))
	wide.AddParagraph(paragraph)
	wide.SetLocation(0, 0)
	// The long word on a line of its own and the two short words on the next.
	testNear(t, "height", 2*font.GetBodyHeight(font.GetSize()),
		wide.GetSize().GetHeight(), testDelta)
	page := NewPage(pdf, letter.Portrait())
	wide.SetLocation(100, 100)
	wide.DrawOn(page)
	positions := testPositions(testContent(page))
	// The first token is drawn on the first line, at the ascent of the font.
	testNear(t, "first baseline", 100+font.GetAscent(font.GetSize()),
		792-positions[0][1], testDelta)
}

func TestTextColumnTheUnderlineOfALineStopsAtItsTextAndRunsThroughIt(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, letter.Portrait())
	underlined := NewTextLine(font, testEightWords)
	underlined.SetUnderline(true)
	column := NewTextColumn()
	column.SetWidth(200)
	paragraph := NewParagraph()
	paragraph.Add(underlined)
	column.AddParagraph(paragraph)
	column.SetLocation(100, 100)
	column.DrawOn(page)
	// The segments of a line follow each other without a gap, and the last one
	// stops at the text rather than after the space that follows it.
	segments := make([][3]float32, 0)
	rows := strings.Split(testContent(page), "\n")
	for i := 0; i < len(rows)-1; i++ {
		if strings.HasSuffix(rows[i], " m") && strings.HasSuffix(rows[i+1], " l") {
			from := strings.Split(rows[i], " ")
			to := strings.Split(rows[i+1], " ")
			x1, _ := strconv.ParseFloat(from[0], 32)
			y1, _ := strconv.ParseFloat(from[1], 32)
			x2, _ := strconv.ParseFloat(to[0], 32)
			segments = append(segments, [3]float32{float32(x1), float32(x2), float32(y1)})
		}
	}
	if len(segments) <= 2 {
		t.Fatalf("segments %d", len(segments))
	}
	for i := 1; i < len(segments); i++ {
		if segments[i][2] == segments[i-1][2] { // the same line
			testNear(t, "segment", segments[i-1][1], segments[i][0], testDelta)
		}
	}
	// The first line ends with "zeta", underlined up to its last character and
	// no further: the space after it is not text.
	var endOfFirstLine float32
	for _, segment := range segments {
		if segment[2] == segments[0][2] {
			endOfFirstLine = segment[1]
		}
	}
	lastTokenX := testLastXOfFirstLine(testPositions(testContent(page)))
	testNear(t, "underline end", lastTokenX+font.StringWidth(font.GetSize(), "zeta"),
		endOfFirstLine, testDelta)
}
