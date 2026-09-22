// markdownparser_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"encoding/hex"
	"os"
	"strconv"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// testDescribeMarkdown returns the blocks as text: H1(text), P(text),
// CODE(text), RULE, IMG(alt|source), QUOTE[...], UL[...] or OL3[...] with its
// start, * after a list that is loose, I[...] for an item, and
// TABLE(a,b;c,d|L,C) with its rows and alignments.
func testDescribeMarkdown(blocks []*markdownBlock) string {
	var buf strings.Builder
	for _, block := range blocks {
		if buf.Len() > 0 {
			buf.WriteByte(' ')
		}
		switch block.kind {
		case markdownHeading:
			buf.WriteString("H" + strconv.Itoa(block.level) + "(" + block.text + ")")
		case markdownParagraph:
			buf.WriteString("P(" + block.text + ")")
		case markdownCode:
			buf.WriteString("CODE(" + block.text + ")")
		case markdownRule:
			buf.WriteString("RULE")
		case markdownImage:
			buf.WriteString("IMG(" + block.text + "|" + block.source + ")")
		case markdownQuote:
			buf.WriteString("QUOTE[" + testDescribeMarkdown(block.children) + "]")
		case markdownList:
			if block.ordered {
				buf.WriteString("OL" + strconv.Itoa(block.start))
			} else {
				buf.WriteString("UL")
			}
			if block.loose {
				buf.WriteString("*")
			}
			buf.WriteString("[" + testDescribeMarkdown(block.children) + "]")
		case markdownItem:
			buf.WriteString("I[" + testDescribeMarkdown(block.children) + "]")
		case markdownTable:
			var rows strings.Builder
			for _, row := range block.rows {
				if rows.Len() > 0 {
					rows.WriteByte(';')
				}
				rows.WriteString(strings.Join(row, ","))
			}
			var alignments strings.Builder
			for _, a := range block.alignments {
				switch a {
				case alignment.Left:
					alignments.WriteByte('L')
				case alignment.Center:
					alignments.WriteByte('C')
				case alignment.Right:
					alignments.WriteByte('R')
				}
			}
			buf.WriteString("TABLE(" + rows.String() + "|" + alignments.String() + ")")
		}
	}
	return buf.String()
}

func testParseMarkdown(text string) string {
	return testDescribeMarkdown(markdownParser{}.parse(text))
}

// testAssertMarkdown checks the blocks that each text is read into, as pairs
// of the description and the text.
func testAssertMarkdown(t *testing.T, cases ...string) {
	t.Helper()
	for i := 0; i+1 < len(cases); i += 2 {
		if got := testParseMarkdown(cases[i+1]); got != cases[i] {
			t.Errorf("%q: want %q, got %q", cases[i+1], cases[i], got)
		}
	}
}

func TestMarkdownParserHeadingsOfHashesAndOfUnderlines(t *testing.T) {
	testAssertMarkdown(t,
		"H1(Title) H3(Three) H2(Closed)", "# Title\n### Three\n## Closed ##",
		"P(#NoSpace) P(####### Seven)", "#NoSpace\n\n####### Seven",
		"H1(Big\nTitle) H2(Small)", "Big\nTitle\n===\n\nSmall\n---")
}

func TestMarkdownParserParagraphsAreSeparatedByEmptyLines(t *testing.T) {
	testAssertMarkdown(t,
		"P(one\ntwo) P(three)", "one\ntwo\n\n  \nthree",
		"P(a) P(b)", "a\r\n\r\nb",
		"", "",
		"", "\n \n")
}

func TestMarkdownParserFencedAndIndentedCode(t *testing.T) {
	testAssertMarkdown(t,
		"CODE(int x = 1;\n  y();)", "```java\nint x = 1;\n  y();\n```",
		"CODE(a\n```\nb)", "~~~~\na\n```\nb\n~~~~",
		"CODE(unclosed)", "```\nunclosed",
		"P(text) CODE(code\n\n  more)", "text\n\n    code\n\n      more\n\n",
		// A paragraph goes on over an indented line.
		"P(text\ncontinued)", "text\n    continued")
}

func TestMarkdownParserThematicBreaks(t *testing.T) {
	testAssertMarkdown(t,
		"P(a) RULE P(b) RULE RULE", "a\n\n***\nb\n\n- - -\n___",
		"P(-- not)", "-- not")
}

func TestMarkdownParserQuotesNestAndGoOnWithoutTheirMark(t *testing.T) {
	testAssertMarkdown(t,
		"QUOTE[P(a\nb)]", "> a\n> b",
		"QUOTE[P(a\nlazy)]", "> a\nlazy",
		"QUOTE[H1(T) QUOTE[P(deep)]]", "> # T\n> > deep",
		"QUOTE[P(a)] P(b)", "> a\n\nb")
}

func TestMarkdownParserBulletAndNumberedLists(t *testing.T) {
	testAssertMarkdown(t,
		"UL[I[P(a)] I[P(b)]]", "- a\n- b",
		"OL3[I[P(x)] I[P(y)]]", "3. x\n4. y",
		"UL[I[P(a)]] UL[I[P(b)]]", "- a\n+ b",
		"UL[I[P(a\nlazy)]]", "- a\nlazy",
		"UL[I[P(a) UL[I[P(b)]]] I[P(c)]]", "- a\n  - b\n- c",
		"UL*[I[P(a)] I[P(b)]]", "- a\n\n- b",
		"UL*[I[P(a) P(more)]]", "- a\n\n  more",
		"UL[I[P(a)]] P(after)", "- a\n\nafter")
}

func TestMarkdownParserANumberedListInterruptsAParagraphOnlyWhenItStartsAtOne(t *testing.T) {
	testAssertMarkdown(t,
		"P(The year\n2026. was good)", "The year\n2026. was good",
		"P(Steps:) OL1[I[P(go)]]", "Steps:\n1. go")
}

func TestMarkdownParserTablesOfPipes(t *testing.T) {
	testAssertMarkdown(t,
		"TABLE(A,B;1,2;3,|LR)", "| A | B |\n|---|--:|\n| 1 | 2 |\n| 3 |",
		"TABLE(a,b;x\\|y,z|CL)", "a | b\n:-:|---\nx\\|y | z",
		// The header and the delimiter row need the same number of cells.
		"P(a | b\n--- | --- | ---)", "a | b\n--- | --- | ---")
}

func TestMarkdownParserAnImageAloneInItsParagraph(t *testing.T) {
	testAssertMarkdown(t,
		"IMG(A map|images/map.png)", "![A map](images/map.png)",
		"P(See ![a](b.png) here)", "See ![a](b.png) here",
		"P(![a](b c.png))", "![a](b c.png)")
}

func TestMarkdownParserTabsAreSpacesToTheNextStopOfFour(t *testing.T) {
	testAssertMarkdown(t,
		"CODE(x)", "\tx",
		"UL*[I[P(a) CODE(b)]]", "-\ta\n\n\t\tb")
}

func TestMarkdownParserContainersNestAtMostMaxDepthLevels(t *testing.T) {
	text := strings.Repeat(">", 1000) + " deep"
	tree := testParseMarkdown(text)
	quotes := strings.Count(tree, "QUOTE[")
	if quotes != markdownMaxDepth {
		t.Errorf("%d quotes, want %d", quotes, markdownMaxDepth)
	}
}

func TestMarkdownParserLongInputsAreReadQuickly(t *testing.T) {
	text := strings.Repeat("> - a | b\n>   ---|---\n>   ```\n\n    x\n- [ ]\n", 20000)
	start := time.Now()
	blocks := markdownParser{}.parse(text)
	elapsed := time.Since(start)
	if len(blocks) == 0 {
		t.Error("no blocks")
	}
	if elapsed > 3*time.Second {
		t.Errorf("%v", elapsed)
	}
}

// TestMarkdownParserDescribesAFileOfInputs writes the description of each
// input of a file, for a comparison with the Java port's: when
// PDFJET_MARKDOWN_IN names a file of inputs, a line of hexadecimal UTF-8 each,
// it writes to the file PDFJET_MARKDOWN_OUT names the description of each, as
// testDescribeMarkdown makes it, a line of hexadecimal UTF-8 each.
func TestMarkdownParserDescribesAFileOfInputs(t *testing.T) {
	inName := os.Getenv("PDFJET_MARKDOWN_IN")
	if inName == "" {
		t.Skip("PDFJET_MARKDOWN_IN is not set")
	}
	in, err := os.Open(inName)
	if err != nil {
		t.Fatal(err)
	}
	defer in.Close()
	out, err := os.Create(os.Getenv("PDFJET_MARKDOWN_OUT"))
	if err != nil {
		t.Fatal(err)
	}
	defer out.Close()
	writer := bufio.NewWriter(out)
	scanner := bufio.NewScanner(in)
	scanner.Buffer(make([]byte, 1<<20), 1<<26)
	for scanner.Scan() {
		text, err := hex.DecodeString(scanner.Text())
		if err != nil {
			t.Fatal(err)
		}
		writer.WriteString(hex.EncodeToString([]byte(testParseMarkdown(string(text)))) + "\n")
	}
	if err := scanner.Err(); err != nil {
		t.Fatal(err)
	}
	if err := writer.Flush(); err != nil {
		t.Fatal(err)
	}
}
