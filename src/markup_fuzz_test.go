// markup_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"io"
	"strings"
	"testing"
	"time"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// fuzzMarkupRepeated is the length, in characters, that the linear time check
// of FuzzMarkup repeats an input to.
const fuzzMarkupRepeated = 1 << 14

// fuzzMarkupMaxTime is much longer than a text of fuzzMarkupRepeated
// characters takes to be read, which is a few milliseconds. It catches a text
// that takes time much worse than linear; TestMarkupLongInputsAreReadInLinearTime
// reads a text long enough that quadratic time fails it.
const fuzzMarkupMaxTime = time.Second

// The fuzz target of the markup. Any text makes paragraphs that are drawn in a
// text frame and written; see fuzzRun. A text with none of the marks is one
// text line of its text, with each whitespace character a space, and the text
// repeated to fuzzMarkupRepeated characters is read in linear time, as
// TestMarkupLongInputsAreReadInLinearTime checks. The seeds are the texts of
// the unit tests and a few that are hard on the parser; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzMarkup$' -fuzztime 5m
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.
func FuzzMarkup(f *testing.F) {
	for _, text := range []string{
		"Just text, no marks.", "one\ntwo",
		"a **b** c", "a *b* c", "a ***b*** c", "**a *b* c**",
		"Hello, **world**!", "un*believ*able",
		"Call `a*b*c`.", "`` a `b` c ``", "**x`y`z**",
		"See [PDFjet](https://pdfjet.com).", "[a **b**](https://x)", "**[go](u)**",
		"[a] (b)", "[a](b c)", "[a]()", "[a](b", "[[b](u) c](v)",
		"2 * 3 = 6", "**a", "a*", "**a*", "`not code", "[not a link",
		"\\*not italic\\*", "\\[a](b)", "a\\b \\",
		"One **two**\nthree.\n\n  \nFour.\r\n\r\n", " \n\n", "[a](u)",
		"one **two**, three",
		// Hard on the parser: the pattern of the linear time test, marks
		// nested and interleaved, a link in the URL of a link, escapes at
		// the ends, empty spans, whitespace Java counts and does not, text
		// in two bytes, three and four, and bytes that are not UTF-8.
		"[a](b *c ``` [[", "***a**b*c***", "*a **b* c**", "[*a](b*)", "[a](b)(c)",
		"[a]([b](c))", "\\", "*\\", "`\\`", "``", "` `", "****", "[]()", "[](x)",
		"*\u00A0a*", "*\u2007a\u2007*", "a\u2028*b*\u3000c", "\u001C*a*\u001F",
		"**ÄÖ**, `日本` [𝄞](u)", "a\xffb *\xc3* [\xe2\x82](\xf0)",
	} {
		f.Add(text)
	}
	f.Fuzz(func(t *testing.T, text string) {
		fuzzRun(t, len(text), func() {
			pdf := NewPDF(bufio.NewWriter(io.Discard))
			markup, _ := testMarkup(pdf)
			paragraph := markup.Paragraph(text)
			if !strings.ContainsAny(text, "*`[]()\\") {
				fuzzCheckPlainMarkup(t, text, paragraph)
			}
			paragraphs := append(markup.Paragraphs(text), paragraph)
			frame := NewTextFrameFromParagraphs(paragraphs).SetWidth(300)
			frame.SetLocation(10, 10)
			frame.DrawOn(NewPage(pdf, letter.Portrait()))
			if err := pdf.Complete(); err != nil {
				t.Fatal(err)
			}
			if n := utf8.RuneCountInString(text); n > 0 {
				long := strings.Repeat(text, (fuzzMarkupRepeated+n-1)/n)
				start := time.Now()
				markup.Paragraph(long)
				if elapsed := time.Since(start); elapsed > fuzzMarkupMaxTime {
					t.Fatalf("%d characters took %v", utf8.RuneCountInString(long), elapsed)
				}
			}
		})
	})
}

// fuzzCheckPlainMarkup checks the paragraph of a text with none of the marks:
// one text line of the text, as characters, with each whitespace character a
// space, or none when that is only spaces and control characters.
func fuzzCheckPlainMarkup(t *testing.T, text string, paragraph *Paragraph) {
	want := []rune(text)
	for i, ch := range want {
		if isJavaWhitespace(ch) {
			want[i] = ' '
		}
	}
	if trimSpace(string(want)) == "" {
		if len(paragraph.lines) != 0 {
			t.Fatalf("%q: %d text lines, want none", text, len(paragraph.lines))
		}
		return
	}
	if len(paragraph.lines) != 1 || paragraph.lines[0].GetText() != string(want) {
		got := make([]string, 0)
		for _, line := range paragraph.lines {
			got = append(got, line.GetText())
		}
		t.Fatalf("%q: text lines %q, want %q", text, got, string(want))
	}
}
