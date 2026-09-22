// markup_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"reflect"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
)

// testMarkupFonts are the fonts of a markup, by the style they are for.
type testMarkupFonts struct {
	regular, bold, italic, boldItalic, code *Font
}

// testMarkup returns a markup in Helvetica and Courier for the PDF, and its fonts.
func testMarkup(pdf *PDF) (*Markup, *testMarkupFonts) {
	fonts := &testMarkupFonts{
		regular:    NewCoreFont(pdf, corefont.Helvetica()),
		bold:       NewCoreFont(pdf, corefont.HelveticaBold()),
		italic:     NewCoreFont(pdf, corefont.HelveticaOblique()),
		boldItalic: NewCoreFont(pdf, corefont.HelveticaBoldOblique()),
		code:       NewCoreFont(pdf, corefont.Courier()),
	}
	return NewMarkup(fonts.regular, fonts.bold, fonts.italic, fonts.boldItalic, fonts.code), fonts
}

// testDescribeMarkup returns the text lines of the paragraph, each as its text
// and its font: R, B, I, X for bold italic, or C for code, with the link after
// a space, and a + before the text of one that is joined to the text line
// before it.
func testDescribeMarkup(paragraph *Paragraph, fonts *testMarkupFonts) []string {
	list := make([]string, 0)
	for i, line := range paragraph.lines {
		style := "R"
		switch line.GetFont() {
		case fonts.bold:
			style = "B"
		case fonts.italic:
			style = "I"
		case fonts.boldItalic:
			style = "X"
		case fonts.code:
			style = "C"
		}
		link := ""
		if line.GetURIAction() != "" {
			link = " " + line.GetURIAction()
		}
		joined := ""
		if paragraph.joinsPrevious(i) {
			joined = "+"
		}
		list = append(list, joined+line.GetText()+"|"+style+link)
	}
	return list
}

func testParseMarkup(text string) []string {
	markup, fonts := testMarkup(testNewPDF())
	return testDescribeMarkup(markup.Paragraph(text), fonts)
}

// testAssertMarkup checks the text lines that each text is read into.
func testAssertMarkup(t *testing.T, cases ...[]string) {
	t.Helper()
	for _, c := range cases {
		if got := testParseMarkup(c[0]); !reflect.DeepEqual(got, c[1:]) {
			t.Errorf("%q: want %q, got %q", c[0], c[1:], got)
		}
	}
}

func TestMarkupPlainTextIsOneTextLine(t *testing.T) {
	testAssertMarkup(t,
		[]string{"Just text, no marks.", "Just text, no marks.|R"},
		[]string{"one\ntwo", "one two|R"})
}

func TestMarkupBoldItalicAndBoth(t *testing.T) {
	testAssertMarkup(t,
		[]string{"a **b** c", "a |R", "b|B", " c|R"},
		[]string{"a *b* c", "a |R", "b|I", " c|R"},
		[]string{"a ***b*** c", "a |R", "b|X", " c|R"},
		[]string{"**a *b* c**", "a |B", "b|X", " c|B"})
}

func TestMarkupPunctuationAfterAStyleIsJoinedToTheWord(t *testing.T) {
	testAssertMarkup(t,
		[]string{"Hello, **world**!", "Hello, |R", "world|B", "+!|R"},
		[]string{"un*believ*able", "un|R", "+believ|I", "+able|R"})
}

func TestMarkupCodeKeepsItsTextAsItIs(t *testing.T) {
	testAssertMarkup(t,
		[]string{"Call `a*b*c`.", "Call |R", "a*b*c|C", "+.|R"},
		[]string{"`` a `b` c ``", "a `b` c|C"},
		[]string{"**x`y`z**", "x|B", "+y|C", "+z|B"})
}

func TestMarkupLinks(t *testing.T) {
	testAssertMarkup(t,
		[]string{"See [PDFjet](https://pdfjet.com).", "See |R", "PDFjet|R https://pdfjet.com", "+.|R"},
		[]string{"[a **b**](https://x)", "a |R https://x", "b|B https://x"},
		[]string{"**[go](u)**", "go|B u"})
}

func TestMarkupALinkNeedsItsBracketsAParenthesisAndAURLWithoutSpaces(t *testing.T) {
	testAssertMarkup(t,
		[]string{"[a] (b)", "[a] (b)|R"},
		[]string{"[a](b c)", "[a](b c)|R"},
		[]string{"[a]()", "[a]()|R"},
		[]string{"[a](b", "[a](b|R"},
		// A link is not in a link.
		[]string{"[[b](u) c](v)", "[b](u) c|R v"})
}

func TestMarkupMarksWithNoMatchAreText(t *testing.T) {
	testAssertMarkup(t,
		[]string{"2 * 3 = 6", "2 * 3 = 6|R"},
		[]string{"**a", "**a|R"},
		[]string{"a*", "a*|R"},
		[]string{"**a*", "*|R", "+a|I"},
		[]string{"`not code", "`not code|R"},
		[]string{"[not a link", "[not a link|R"})
}

func TestMarkupABackslashMakesAMarkText(t *testing.T) {
	testAssertMarkup(t,
		[]string{"\\*not italic\\*", "*not italic*|R"},
		[]string{"\\[a](b)", "[a](b)|R"},
		[]string{"a\\b \\", "a\\b \\|R"})
}

func TestMarkupParagraphsAreSeparatedByEmptyLines(t *testing.T) {
	markup, fonts := testMarkup(testNewPDF())
	paragraphs := markup.Paragraphs("One **two**\nthree.\n\n  \nFour.\r\n\r\n")
	if len(paragraphs) != 2 {
		t.Fatalf("%d paragraphs", len(paragraphs))
	}
	if got, want := testDescribeMarkup(paragraphs[0], fonts), []string{"One |R", "two|B", " three. |R"}; !reflect.DeepEqual(got, want) {
		t.Errorf("want %q, got %q", want, got)
	}
	if got, want := testDescribeMarkup(paragraphs[1], fonts), []string{"Four. |R"}; !reflect.DeepEqual(got, want) {
		t.Errorf("want %q, got %q", want, got)
	}
	if n := len(markup.Paragraphs(" \n\n")); n != 0 {
		t.Errorf("%d paragraphs of no words", n)
	}
}

func TestMarkupALinkIsColoredAndUnderlined(t *testing.T) {
	markup, _ := testMarkup(testNewPDF())
	line := markup.SetLinkColor(color.Red).Paragraph("[a](u)").lines[0]
	if !line.GetUnderline() {
		t.Error("the link is not underlined")
	}
	testAssertRGB(t, 1, 0, 0, line.GetTextColor())
}

func TestMarkupLongInputsAreReadInLinearTime(t *testing.T) {
	// Unmatched brackets, backticks of every length and runs of * would each
	// take quadratic time with a naive search.
	var text strings.Builder
	for i := 0; i < 20000; i++ {
		text.WriteString("[a](b *c `")
		if i%50 == 0 {
			text.WriteString("``")
		}
		text.WriteString(" [[")
	}
	markup, _ := testMarkup(testNewPDF())
	start := time.Now()
	paragraph := markup.Paragraph(text.String())
	elapsed := time.Since(start)
	if len(paragraph.lines) == 0 {
		t.Error("no text lines")
	}
	if elapsed > 2*time.Second {
		t.Errorf("%v", elapsed)
	}
}

func TestMarkupTheParagraphDrawsWithNoSpaceBeforeJoinedPunctuation(t *testing.T) {
	pdf := testNewPDF()
	markup, fonts := testMarkup(pdf)
	page := NewPage(pdf, testLetterPortrait())
	frame := NewTextFrameFromParagraphs([]*Paragraph{markup.Paragraph("one **two**, three")}).SetWidth(300)
	frame.SetLocation(10, 10)
	frame.DrawOn(page)
	content := testContent(page)
	two := testPositionOf(t, content, "two")
	comma := testPositionOf(t, content, ",")
	testNear(t, "x", two[0]+fonts.bold.StringWidth(fonts.bold.GetSize(), "two"), comma[0], testDelta)
}
