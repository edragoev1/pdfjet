// markdown_fuzz_test.go
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

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// fuzzMarkdownRepeated is the length, in characters, that the linear time
// check of FuzzMarkdown repeats an input to.
const fuzzMarkdownRepeated = 1 << 14

// fuzzMarkdownMaxTime is much longer than a text of fuzzMarkdownRepeated
// characters takes to be read, which is a few milliseconds. It catches a text
// that takes time much worse than linear;
// TestMarkdownParserLongInputsAreReadQuickly reads a text long enough that
// quadratic time fails it.
const fuzzMarkdownMaxTime = time.Second

// The fuzz target of Markdown. Any text is read into blocks and drawn on as
// many pages as it needs in a PDF/UA document, which is written; see fuzzRun.
// The text repeated to fuzzMarkdownRepeated characters is read in
// fuzzMarkdownMaxTime, as the blocks of a text are read in linear time; it is
// not drawn, since a text of that length can need hundreds of pages, of a
// column an inch wide in quotes nested as deep as they nest, and they are as
// much work in every port. The seeds are the texts of the unit tests and a few
// that are hard on the parser and on the drawing; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzMarkdown$' -fuzztime 5m
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.
func FuzzMarkdown(f *testing.F) {
	for _, text := range []string{
		"# Title\n### Three\n## Closed ##", "#NoSpace\n\n####### Seven", "Big\nTitle\n===\n\nSmall\n---",
		"one\ntwo\n\n  \nthree", "a\r\n\r\nb", "", "\n \n",
		"```java\nint x = 1;\n  y();\n```", "~~~~\na\n```\nb\n~~~~", "```\nunclosed",
		"text\n\n    code\n\n      more\n\n", "text\n    continued",
		"a\n\n***\nb\n\n- - -\n___", "-- not",
		"> a\n> b", "> a\nlazy", "> # T\n> > deep", "> a\n\nb",
		"- a\n- b", "3. x\n4. y", "- a\n+ b", "- a\nlazy", "- a\n  - b\n- c", "- a\n\n- b",
		"- a\n\n  more", "- a\n\nafter", "The year\n2026. was good", "Steps:\n1. go",
		"| A | B |\n|---|--:|\n| 1 | 2 |\n| 3 |", "a | b\n:-:|---\nx\\|y | z", "a | b\n--- | --- | ---",
		"![A map](images/map.png)", "See ![a](b.png) here", "![a](b c.png)",
		"\tx", "-\ta\n\n\t\tb",
		"> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n",
		"# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n" +
			"```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next",
		"### Three\n\n##### Five\n\n# One", "<b>not bold</b>",
		"![Not read](../images/linux-logo.png)", "![x](/etc/passwd)", "![x](https://pdfjet.com/logo.png)",
		// Hard on the parser and the drawing: quotes and lists nested deeper
		// than markdownMaxDepth, so that the text is narrower than nothing, a
		// table of many columns and rows, fences never closed, tabs, carriage
		// returns alone, text in two bytes, three and four, and bytes that are
		// not UTF-8.
		strings.Repeat(">", 40) + " deep\n" + strings.Repeat(">", 40) + "     code 😀😀😀",
		strings.Repeat("> ", 40) + "```\n😀😀😀😀\n```",
		func() string {
			var b strings.Builder
			for i := 0; i < 40; i++ {
				b.WriteString(strings.Repeat("  ", i) + "- item\n")
			}
			b.WriteString(strings.Repeat("  ", 40) + "    😀 code\n")
			return b.String()
		}(),
		"|" + strings.Repeat(" a |", 60) + "\n|" + strings.Repeat("---|", 60) + "\n" +
			strings.Repeat("|"+strings.Repeat(" wordwordword |", 60)+"\n", 30),
		"```\n" + strings.Repeat("x", 500) + "\n" + strings.Repeat("😀", 200),
		// Code in 31 quotes, one column wide, which a character of two UTF-16
		// code units is wider than.
		strings.Repeat("> ", 31) + "```\n" + strings.Repeat("> ", 31) + "😀x😀\n" + strings.Repeat("> ", 31) + "```",
		"~~~~\n~~~\n```", "\t\t\t- \t> \t1.\t```", "a\rb\r\rc\r> d\r- e",
		"# **ÄÖ** *日本*\n\n`𝄞` [link](u)\n\n| 日本 | 😀 |\n|:-:|--:|\n| é | \\| |",
		"a\xffb\n\n- \xc3\n> \xe2\x82\n```\xf0\n\xff\t\xff",
		"1234567890. x\n123456789. y\n0. z", "- \n- \n-", "> \n>\n> >", "***\n---\n___\n* * *",
		"a\n===\nb\n---\n- c\n---", "![](x)", "![a]()", "![a](b)(c)",
	} {
		f.Add(text)
	}
	f.Fuzz(func(t *testing.T, text string) {
		fuzzRun(t, len(text), func() {
			fuzzDrawMarkdown(t, text)
			if n := utf8.RuneCountInString(text); n > 0 {
				long := strings.Repeat(text, (fuzzMarkdownRepeated+n-1)/n)
				start := time.Now()
				markdownParser{}.parse(long)
				if elapsed := time.Since(start); elapsed > fuzzMarkdownMaxTime {
					t.Fatalf("%d characters took %v", utf8.RuneCountInString(long), elapsed)
				}
			}
		})
	})
}

// fuzzDrawMarkdown draws the text in a PDF/UA document and writes it.
func fuzzDrawMarkdown(t *testing.T, text string) {
	pdf := NewPDF(bufio.NewWriter(io.Discard))
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Markdown")
	markdown := NewMarkdown(NewCoreFont(pdf, corefont.Helvetica()), NewCoreFont(pdf, corefont.HelveticaBold()),
		NewCoreFont(pdf, corefont.HelveticaOblique()), NewCoreFont(pdf, corefont.HelveticaBoldOblique()),
		NewCoreFont(pdf, corefont.Courier()))
	pages := make([]*Page, 0)
	markdown.DrawOnPages(pdf, text, &pages, letter.Portrait())
	if len(pages) == 0 {
		if trimSpace(text) != "" {
			t.Fatalf("%q: no pages", text)
		}
		return // A text with no blocks needs no page.
	}
	pdf.AddPages(pages)
	if err := pdf.Complete(); err != nil {
		t.Fatal(err)
	}
}
