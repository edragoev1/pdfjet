// textframe_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
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

// testNovel returns a frame of 200 paragraphs of a few lines each, the first
// word of each its number, p000 to p199, which no other word begins with.
func testNovel(font *Font) *TextFrame {
	paragraphs := make([]string, 0, 200)
	for i := 0; i < 200; i++ {
		var text strings.Builder
		fmt.Fprintf(&text, "p%03d", i)
		for j := 0; j < 30+i%17; j++ {
			fmt.Fprintf(&text, " word%d", j)
		}
		paragraphs = append(paragraphs, text.String())
	}
	tf := NewTextFrame(font, paragraphs)
	tf.SetLocation(72, 72)
	tf.SetWidth(468)
	return tf
}

// testPagesOf returns the page each paragraph starts on, by its number.
func testPagesOf(t *testing.T, pages []*Page) []int {
	pageOf := make([]int, 200)
	for i := range pageOf {
		pageOf[i] = -1
	}
	for p, page := range pages {
		content := testContent(page)
		for i := 0; i < 200; i++ {
			if strings.Contains(content, testHex(fmt.Sprintf("p%03d", i))) {
				if pageOf[i] != -1 {
					t.Errorf("paragraph %d starts on two pages", i)
				}
				pageOf[i] = p
			}
		}
	}
	return pageOf
}

func TestTextFrameAFrameFlowsOntoAsManyPagesAsTheTextNeeds(t *testing.T) {
	pdf := testNewPDF()
	tf := testNovel(testHelvetica(pdf))
	pages := make([]*Page, 0)
	tf.DrawOnPages(pdf, &pages, letter.Portrait())
	if len(pages) <= 5 {
		t.Fatalf("%d pages", len(pages))
	}
	if tf.HasMoreText() {
		t.Error("text is left")
	}
	// Every paragraph is drawn once, in order, and every page has text.
	pageOf := testPagesOf(t, pages)
	for i := 0; i < 200; i++ {
		if pageOf[i] < 0 {
			t.Errorf("paragraph %d is not drawn", i)
		} else if i > 0 && pageOf[i] < pageOf[i-1] {
			t.Errorf("paragraph %d is out of order", i)
		}
	}
	if pageOf[199] != len(pages)-1 {
		t.Errorf("the last paragraph is on page %d of %d", pageOf[199], len(pages))
	}
	// The text keeps the margin of its location at the bottom too: no
	// baseline under 72 points from the bottom of the page.
	td := regexp.MustCompile(`[-0-9.]+ ([-0-9.]+) Td\n`)
	baselines := 0
	for _, page := range pages {
		for _, m := range td.FindAllStringSubmatch(testContent(page), -1) {
			if y, _ := strconv.ParseFloat(m[1], 32); y < 72 {
				t.Errorf("a baseline at y = %s", m[1])
			}
			baselines++
		}
	}
	if baselines <= 100 {
		t.Errorf("%d baselines", baselines)
	}
	// The frame has no height of its own, as before.
	testNear(t, "the height", 0, tf.GetHeight(), 0)
}

func TestTextFrameAFrameWithAHeightHasItOnEveryPage(t *testing.T) {
	pdf := testNewPDF()
	tall := make([]*Page, 0)
	testNovel(testHelvetica(pdf)).DrawOnPages(pdf, &tall, letter.Portrait())
	tf := testNovel(testHelvetica(pdf))
	tf.SetHeight(300)
	pages := make([]*Page, 0)
	xy := tf.DrawOnPages(pdf, &pages, letter.Portrait())
	if len(pages) <= len(tall) {
		t.Errorf("%d pages, not more than %d", len(pages), len(tall))
	}
	testNear(t, "the height", 300, tf.GetHeight(), 0)
	testAssertXY(t, 540, 372, xy)
	// An empty frame needs no page.
	none := make([]*Page, 0)
	empty := NewTextFrameFromParagraphs(make([]*Paragraph, 0))
	empty.SetLocation(10, 20)
	testAssertXY(t, 10, 20, empty.DrawOnPages(pdf, &none, letter.Portrait()))
	if len(none) != 0 {
		t.Errorf("%d pages", len(none))
	}
}

// testJoinedParagraph returns a paragraph of text lines in the font, each
// added with Add, or with AddJoined when it starts with "+", which is not part
// of its text.
func testJoinedParagraph(font *Font, texts ...string) *Paragraph {
	paragraph := NewParagraph()
	for _, text := range texts {
		if strings.HasPrefix(text, "+") {
			paragraph.AddJoined(NewTextLine(font, text[1:]))
		} else {
			paragraph.Add(NewTextLine(font, text))
		}
	}
	return paragraph
}

// testDrawJoined draws a frame of the joined paragraph and returns the content
// of the page. With no alignment the paragraph has none of its own.
func testDrawJoined(width float32, textAlignment *alignment.Alignment, texts ...string) string {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	paragraph := testJoinedParagraph(font, texts...)
	if textAlignment != nil {
		paragraph.SetTextAlignment(*textAlignment)
	}
	page := NewPage(pdf, letter.Portrait())
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetWidth(width)
	frame.SetLocation(10, 10)
	frame.DrawOn(page)
	return testContent(page)
}

func TestTextFrameAJoinedTextLineHasNoSpaceBeforeIt(t *testing.T) {
	font := testHelvetica(testNewPDF())
	content := testDrawJoined(300, nil, "one", "+,", "two")
	one := testPositionOf(t, content, "one")
	comma := testPositionOf(t, content, ",")
	two := testPositionOf(t, content, "two")
	testNear(t, "the comma", one[0]+font.StringWidth(font.GetSize(), "one"), comma[0], testDelta)
	testNear(t, "two", comma[0]+font.StringWidth(font.GetSize(), ", "), two[0], testDelta)
	testNear(t, "the row", one[1], two[1], testDelta)
}

func TestTextFrameARowDoesNotBreakInsideAJoinedWord(t *testing.T) {
	font := testHelvetica(testNewPDF())
	// "aaa bbb" fits in the row, and "aaa bbbccc" does not, so bbb goes on the
	// next row with the ccc joined to it.
	width := font.StringWidth(font.GetSize(), "aaa bbb") + 1
	content := testDrawJoined(width, nil, "aaa bbb", "+ccc")
	aaa := testPositionOf(t, content, "aaa")
	bbb := testPositionOf(t, content, "bbb")
	ccc := testPositionOf(t, content, "ccc")
	if !(bbb[1] < aaa[1]) {
		t.Errorf("bbb is not on the second row: %v, %v", aaa, bbb)
	}
	testNear(t, "bbb", 10, bbb[0], testDelta)
	testNear(t, "the row of ccc", bbb[1], ccc[1], testDelta)
	testNear(t, "ccc", bbb[0]+font.StringWidth(font.GetSize(), "bbb"), ccc[0], testDelta)
}

func TestTextFrameAJoinedWordWiderThanTheFrameBreaksWhereItIsJoined(t *testing.T) {
	font := testHelvetica(testNewPDF())
	content := testDrawJoined(font.StringWidth(font.GetSize(), "abc")+1, nil, "abc", "+def")
	abc := testPositionOf(t, content, "abc")
	def := testPositionOf(t, content, "def")
	if !(def[1] < abc[1]) {
		t.Errorf("def is not on the second row: %v, %v", abc, def)
	}
	testNear(t, "def", 10, def[0], testDelta)
}

func TestTextFrameASpaceWhereTheyMeetKeepsJoinedTextLinesApart(t *testing.T) {
	if testDrawJoined(300, nil, "one", "two") != testDrawJoined(300, nil, "one", "+ two") {
		t.Error("a space at the start of the joined text line")
	}
	if testDrawJoined(300, nil, "one ", "two") != testDrawJoined(300, nil, "one ", "+two") {
		t.Error("a space at the end of the text line before")
	}
}

func TestTextFrameAJustifiedRowDoesNotWidenAJoin(t *testing.T) {
	font := testHelvetica(testNewPDF())
	justify := alignment.Justify
	content := testDrawJoined(200, &justify,
		"one two three", "+,", "four five six seven eight nine ten eleven twelve")
	three := testPositionOf(t, content, "three")
	comma := testPositionOf(t, content, ",")
	four := testPositionOf(t, content, "four")
	testNear(t, "the row of four", three[1], four[1], testDelta)
	testNear(t, "the comma", three[0]+font.StringWidth(font.GetSize(), "three"), comma[0], testDelta)
	// The spaces are widened: four is further than one space after the comma.
	if !(four[0] > comma[0]+font.StringWidth(font.GetSize(), ", ")+1) {
		t.Errorf("the row is not justified: %v, %v", comma, four)
	}
}

// testDrawMixed draws two text lines, the first in Courier, whose space is
// wide, and the second in Helvetica, or the other way round when courierFirst
// is false.
func testDrawMixed(width float32, textAlignment *alignment.Alignment, courierFirst bool,
	first, second string, column bool) string {
	pdf := testNewPDF()
	courier := NewCoreFont(pdf, corefont.Courier())
	helvetica := testHelvetica(pdf)
	firstFont, secondFont := courier, helvetica
	if !courierFirst {
		firstFont, secondFont = helvetica, courier
	}
	paragraph := NewParagraph().
		Add(NewTextLine(firstFont, first)).
		Add(NewTextLine(secondFont, second))
	if textAlignment != nil {
		paragraph.SetTextAlignment(*textAlignment)
	}
	page := NewPage(pdf, letter.Portrait())
	if column {
		textColumn := NewTextColumn()
		textColumn.SetWidth(width)
		if textAlignment == nil {
			textColumn.SetTextAlignment(alignment.Left)
		} else {
			textColumn.SetTextAlignment(*textAlignment)
		}
		textColumn.AddParagraph(paragraph)
		textColumn.SetLocation(10, 10)
		textColumn.DrawOn(page)
	} else {
		frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetWidth(width)
		frame.SetLocation(10, 10)
		frame.DrawOn(page)
	}
	return testContent(page)
}

func testCheckTheNarrowerSpaceIsUsed(t *testing.T, column bool) {
	t.Helper()
	courier := NewCoreFont(testNewPDF(), corefont.Courier())
	helvetica := testHelvetica(testNewPDF())
	// Code, then text: the space is Helvetica's, at the start of the text.
	content := testDrawMixed(300, nil, true, "x", "and more", column)
	x := testPositionOf(t, content, "x")
	if !strings.Contains(content, "<"+testHex("x")+">") {
		t.Errorf("x has a space after it:\n%s", content)
	}
	testNear(t, "the space before and", x[0]+courier.StringWidth(courier.GetSize(), "x"),
		testPositionOf(t, content, " and")[0], testDelta)
	// Text, then code: the space is still Helvetica's, after the text.
	content = testDrawMixed(300, nil, false, "use", "x", column)
	testNear(t, "x", testPositionOf(t, content, "use")[0]+helvetica.StringWidth(helvetica.GetSize(), "use "),
		testPositionOf(t, content, "x")[0], testDelta)
}

func testCheckAMovedSpaceDoesNotStartARow(t *testing.T, column bool) {
	t.Helper()
	courier := NewCoreFont(testNewPDF(), corefont.Courier())
	content := testDrawMixed(courier.StringWidth(courier.GetSize(), "aaa")+5, nil, true, "aaa", "bbb", column)
	aaa := testPositionOf(t, content, "aaa")
	bbb := testPositionOf(t, content, "bbb")
	if !(bbb[1] < aaa[1]) {
		t.Errorf("bbb is not on the second row: %v, %v", aaa, bbb)
	}
	testNear(t, "bbb", 10, bbb[0], testDelta)
	if strings.Contains(content, "<"+testHex(" bbb")) {
		t.Errorf("bbb has a space before it:\n%s", content)
	}
}

func testCheckAJustifiedRowWidensAMovedSpace(t *testing.T, column bool) {
	t.Helper()
	courier := NewCoreFont(testNewPDF(), corefont.Courier())
	helvetica := testHelvetica(testNewPDF())
	justify := alignment.Justify
	content := testDrawMixed(150, &justify, true, "x",
		"one two three four five six seven eight nine ten eleven twelve", column)
	x := testPositionOf(t, content, "x")
	oneText := "one"
	if column {
		oneText = " one"
	}
	one := testPositionOf(t, content, oneText)
	two := testPositionOf(t, content, "two")
	// Where one and two start; a text column draws the space before one with it.
	oneStart := one[0]
	if column {
		oneStart = one[0] + helvetica.StringWidth(helvetica.GetSize(), " ")
	}
	testNear(t, "the row of one", x[1], one[1], testDelta)
	// The space before one, which Courier's text line left to it, is as wide
	// as the space after it: both are widened alike.
	before := oneStart - (x[0] + courier.StringWidth(courier.GetSize(), "x"))
	after := two[0] - (oneStart + helvetica.StringWidth(helvetica.GetSize(), "one"))
	if !(before > helvetica.StringWidth(helvetica.GetSize(), " ")+0.1) {
		t.Errorf("the row is not justified: %v", before)
	}
	testNear(t, "the space before one", after, before, testDelta)
}

func TestTextFrameTheSpaceBetweenTwoTextLinesIsTheNarrowerOfTheirSpaces(t *testing.T) {
	testCheckTheNarrowerSpaceIsUsed(t, false)
}

func TestTextFrameAMovedSpaceDoesNotStartARow(t *testing.T) {
	testCheckAMovedSpaceDoesNotStartARow(t, false)
}

func TestTextFrameAJustifiedRowWidensAMovedSpace(t *testing.T) {
	testCheckAJustifiedRowWidensAMovedSpace(t, false)
}
