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
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
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

func TestTextColumnAParagraphIsOneStructureElementOfTheTypeItIsGiven(t *testing.T) {
	// The words of a paragraph are drawn one at a time, and each was an
	// element of its own, so a reader read every word as a paragraph.
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1)
	doc.pdf.SetTitle("Title")
	font := testHelvetica(doc.pdf)
	column := NewTextColumn()
	column.SetWidth(200)
	column.SetLocation(100, 100)
	column.AddParagraph(NewParagraph().
		SetStructureType(structelem.H1).Add(NewTextLine(font, testEightWords)))
	column.AddParagraph(NewParagraph().Add(NewTextLine(font, testEightWords)))
	page := NewPage(doc.pdf, letter.Portrait())
	column.DrawOn(page)
	content := testContent(page)
	// The eight words of each paragraph are its marked contents.
	if got := strings.Count(content, "/H1 <</MCID"); got != 8 {
		t.Errorf("the heading has %d marked contents, not 8", got)
	}
	if got := strings.Count(content, "/P <</MCID"); got != 8 {
		t.Errorf("the paragraph has %d marked contents, not 8", got)
	}
	raw := string(doc.complete())
	counts := map[string]int{
		"/S /H1\n": 1,
		"/S /P\n":  1,
	}
	for text, want := range counts {
		if got := strings.Count(raw, text); got != want {
			t.Errorf("%q is in the PDF %d times, not %d", text, got, want)
		}
	}
	if !strings.Contains(raw, "/K [0 1 2 3 4 5 6 7]") {
		t.Error("the heading does not hold the marked contents of its words")
	}
}

// testDrawJoinedColumn draws a column of the joined paragraph and returns the
// content of the page.
func testDrawJoinedColumn(width float32, textAlignment alignment.Alignment, texts ...string) string {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	column := NewTextColumn()
	column.SetWidth(width)
	column.SetTextAlignment(textAlignment)
	column.AddParagraph(testJoinedParagraph(font, texts...))
	column.SetLocation(10, 10)
	page := NewPage(pdf, letter.Portrait())
	column.DrawOn(page)
	return testContent(page)
}

func TestTextColumnAJoinedTextLineHasNoSpaceBeforeIt(t *testing.T) {
	font := testHelvetica(testNewPDF())
	content := testDrawJoinedColumn(300, alignment.Left, "one", "+,", "two")
	one := testPositionOf(t, content, "one")
	comma := testPositionOf(t, content, ",")
	two := testPositionOf(t, content, "two")
	testNear(t, "the comma", one[0]+font.StringWidth(font.GetSize(), "one"), comma[0], testDelta)
	testNear(t, "two", comma[0]+font.StringWidth(font.GetSize(), ", "), two[0], testDelta)
}

func TestTextColumnALineDoesNotBreakInsideAJoinedWord(t *testing.T) {
	font := testHelvetica(testNewPDF())
	width := font.StringWidth(font.GetSize(), "aaa bbb") + 1
	content := testDrawJoinedColumn(width, alignment.Left, "aaa bbb", "+ccc")
	aaa := testPositionOf(t, content, "aaa")
	bbb := testPositionOf(t, content, "bbb")
	ccc := testPositionOf(t, content, "ccc")
	if !(bbb[1] < aaa[1]) {
		t.Errorf("bbb is not on the second line: %v, %v", aaa, bbb)
	}
	testNear(t, "bbb", 10, bbb[0], testDelta)
	testNear(t, "the line of ccc", bbb[1], ccc[1], testDelta)
	testNear(t, "ccc", bbb[0]+font.StringWidth(font.GetSize(), "bbb"), ccc[0], testDelta)
}

func TestTextColumnAJoinedWordWiderThanTheColumnBreaksWhereItIsJoined(t *testing.T) {
	font := testHelvetica(testNewPDF())
	content := testDrawJoinedColumn(font.StringWidth(font.GetSize(), "abc")+1, alignment.Left, "abc", "+def")
	abc := testPositionOf(t, content, "abc")
	def := testPositionOf(t, content, "def")
	if !(def[1] < abc[1]) {
		t.Errorf("def is not on the second line: %v, %v", abc, def)
	}
	testNear(t, "def", 10, def[0], testDelta)
}

func TestTextColumnASpaceWhereTheyMeetKeepsJoinedTextLinesApart(t *testing.T) {
	if testDrawJoinedColumn(300, alignment.Left, "one", "two") !=
		testDrawJoinedColumn(300, alignment.Left, "one", "+ two") {
		t.Error("a space at the start of the joined text line")
	}
}

func TestTextColumnAJustifiedLineDoesNotWidenAJoin(t *testing.T) {
	font := testHelvetica(testNewPDF())
	content := testDrawJoinedColumn(200, alignment.Justify,
		"one two three", "+,", "four five six seven eight nine ten eleven twelve")
	three := testPositionOf(t, content, "three")
	comma := testPositionOf(t, content, ",")
	four := testPositionOf(t, content, "four")
	testNear(t, "the line of four", three[1], four[1], testDelta)
	testNear(t, "the comma", three[0]+font.StringWidth(font.GetSize(), "three"), comma[0], testDelta)
	if !(four[0] > comma[0]+font.StringWidth(font.GetSize(), ", ")+1) {
		t.Errorf("the line is not justified: %v, %v", comma, four)
	}
}

func TestTextColumnTheSpaceBetweenTwoTextLinesIsTheNarrowerOfTheirSpaces(t *testing.T) {
	testCheckTheNarrowerSpaceIsUsed(t, true)
}

func TestTextColumnAMovedSpaceDoesNotStartALine(t *testing.T) {
	testCheckAMovedSpaceDoesNotStartARow(t, true)
}

func TestTextColumnAJustifiedLineWidensAMovedSpace(t *testing.T) {
	testCheckAJustifiedRowWidensAMovedSpace(t, true)
}

func TestTextColumnALinkEndsBeforeTheSpaceAfterIt(t *testing.T) {
	testCheckALinkEndsBeforeTheSpaceAfterIt(t, true)
}
