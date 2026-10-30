// textblock_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"math"
	"strings"
	"testing"
	"time"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

const testTenWords = "one two three four five six seven eight nine ten"

func testMeasureBlock(font *Font, text string) [2]float32 {
	block := NewTextBlock(font, text)
	block.SetLocation(0, 0)
	return block.DrawOn(nil)
}

func TestTextBlockANewlineIsOneEmptyLine(t *testing.T) {
	font := testHelvetica(testNewPDF())
	x := testMeasureBlock(font, "x")
	testAssertXY(t, 500, 13.872, x)
	testAssertXY(t, x[0], x[1], testMeasureBlock(font, "\n"))
	testAssertXY(t, x[0], x[1], testMeasureBlock(font, ""))
}

func TestTextBlockWithoutAHeightTheBlockIsAsTallAsItsText(t *testing.T) {
	block := NewTextBlock(testHelvetica(testNewPDF()), testTenWords).SetWidth(60)
	block.SetLocation(0, 0)
	// Six lines of 13.872 points.
	testAssertXY(t, 60, 83.232, block.DrawOn(nil))
	testNear(t, "height", 83.232, block.GetHeight(), testDelta)
	testNear(t, "width", 60, block.GetWidth(), 0)
}

func TestTextBlockAHeightCutsTheTextThatDoesNotFitAndAlignsTheRest(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	block := NewTextBlock(font, testTenWords).SetSize(60, 30)
	block.SetLocation(0, 0)
	// Two of the six lines fit
	testAssertXY(t, 60, 30, block.DrawOn(nil))
	testNear(t, "height", 30, block.GetHeight(), 0)
	page := NewPage(pdf, testLetterPortrait())
	block.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("...")) || strings.Contains(content, testHex("ten")) {
		t.Errorf("cut text in %q", content)
	}

	// Aligned to the bottom of a block 10 points taller than its two lines,
	// the text sits where a block without a height draws it 10 points lower
	page = NewPage(pdf, testLetterPortrait())
	block.SetSize(60, 2*13.872+10).SetVerticalAlignment(alignment.Bottom).DrawOn(page)
	bottom := testContent(page)
	page = NewPage(pdf, testLetterPortrait())
	lowerBlock := NewTextBlock(font, "one two three four").SetWidth(60)
	lowerBlock.SetLocation(0, 10)
	lowerBlock.DrawOn(page)
	lower := testContent(page)
	start := strings.Index(lower, "1 0 0 1 0 ")
	textMatrix := lower[start : strings.Index(lower, " Tm\n")+4]
	if !strings.Contains(bottom, textMatrix) {
		t.Errorf("%q missing from %q", textMatrix, bottom)
	}
}

func TestTextBlockAWrappedLineIsDrawnWithTheSpaceItWasWrappedAt(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	block := NewTextBlock(font, testTenWords)
	block.SetLocation(0, 0)
	block.SetWidth(60)
	block.DrawOn(page)
	content := testContent(page)
	// Six lines: after each of the first five, the space it was wrapped at,
	// so that a screen reader and a text extractor see the word break; the
	// last line has none.
	space := "[<" + testHex(" ") + ">] TJ\n"
	if n := strings.Count(content, space); n != 5 {
		t.Errorf("%d spaces after the lines, not 5, in %q", n, content)
	}
	if !strings.Contains(content, "[<"+testHex("ten")+">] TJ\nET") {
		t.Errorf("the last line has a space after it in %q", content)
	}
	// Drawn after the line, not in its text: the line is measured without it
	if !strings.Contains(content, "[<"+testHex("one two")+">] TJ\n") {
		t.Errorf("the space is in the text of the line in %q", content)
	}
}

func TestTextBlockPaddingWiderThanTheBlockDrawsOneCharacterALine(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	// The text area was negative, and the word was broken past its end.
	block := NewTextBlock(font, "Hello")
	block.SetLocation(50, 50)
	block.SetWidth(100)
	block.SetPadding(60)
	block.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, testHex("H")) || !strings.Contains(content, testHex("o")) {
		t.Error("the characters of the word are not drawn")
	}
}

func TestTextBlockTheUnderlineIsDrawnInTheTextColorAtTheFontThickness(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	page := NewPage(pdf, testLetterPortrait())
	page.SetPenColor(color.Red) // the underline took the pen color
	block := NewTextBlock(font, "Hello")
	block.SetLocation(50, 50)
	block.SetTextColor(color.Blue)
	block.SetUnderline(true)
	block.SetBorderWidth(3) // and the border width
	block.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "0 0 1 RG") {
		t.Error("the underline is not drawn in the text color")
	}
	if strings.Contains(content, "3 w") {
		t.Error("the underline is drawn at the border width")
	}
	if !strings.Contains(content, fmt.Sprintf("%v w", font.GetUnderlineThickness(font.GetSize()))) {
		t.Error("the underline is not as thick as the font says")
	}
}

func TestTextBlockTheCornerRadiusRoundsABackgroundWithoutABorder(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	block := NewTextBlock(testHelvetica(pdf), "Hello")
	block.SetLocation(50, 50)
	block.SetBackgroundColor(color.Yellow)
	block.SetCornerRadius(10)
	block.DrawOn(page)
	// A rounded rectangle draws its corners with the curve operator.
	if !strings.Contains(testContent(page), " c\n") {
		t.Error("the background is not rounded")
	}
}

func TestTextBlockStrikeoutDrawsALineThroughEachLine(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	block := NewTextBlock(testHelvetica(pdf), "one\ntwo").SetStrikeout(true)
	block.SetLocation(0, 0)
	block.DrawOn(page)
	if lines := strings.Count(testContent(page), " l\n"); lines != 2 {
		t.Errorf("%d lines in %q", lines, testContent(page))
	}
}

func TestTextBlockDrawOnAPageWritesEveryWord(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, letter.Portrait())
	block := NewTextBlock(testHelvetica(pdf), testTenWords).SetWidth(60)
	block.SetLocation(0, 0)
	testAssertXY(t, 60, 83.232, block.DrawOn(page))
	content := testContent(page)
	if !strings.Contains(content, testHex("one")) || !strings.Contains(content, testHex("ten")) {
		t.Errorf("words missing from %q", content)
	}
}

func TestTextBlockTransparentLeavesTheTextColorUnchanged(t *testing.T) {
	block := NewTextBlock(testHelvetica(testNewPDF()), "x")
	block.SetTextColor(color.Blue).SetTextColor(color.Transparent)
	testAssertRGB(t, 0, 0, 1, block.GetTextColor())
}

func TestTextBlockSetFontChangesTheFallbackFontUnlessAnotherWasSet(t *testing.T) {
	pdf := testNewPDF()
	helvetica := testHelvetica(pdf)
	courier := NewCoreFont(pdf, corefont.Courier())
	block := NewTextBlock(helvetica, "x").SetFont(courier)
	if block.fallbackFont != courier {
		t.Error("the fallback font did not change with the font")
	}
	block.SetFallbackFont(helvetica).SetFont(NewCoreFont(pdf, corefont.TimesRoman()))
	if block.fallbackFont != helvetica {
		t.Error("setting the font replaced the fallback font that was set")
	}
}

func TestTextBlockIsTaggedAsItsStructureType(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Test")
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
	page := NewPage(pdf, letter.Portrait())
	heading := NewTextBlock(font, "Invoice")
	heading.SetStructureType(structelem.H1)
	heading.SetLocation(50, 50)
	heading.DrawOn(page)
	paragraph := NewTextBlock(font, "Thank you for your order.")
	paragraph.SetLocation(50, 100)
	paragraph.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "/H1 <</MCID 0>>") || !strings.Contains(content, "/P <</MCID 1>>") {
		t.Errorf("the heading and the paragraph are not tagged as H1 and P: %.200q", content)
	}
}

// testAppendBrokenWordLinesSlow breaks a word as appendBrokenWordLines did
// before 25 September 2026, measuring the rest of the word before each line:
// the same lines, in time that grows with the square of the word.
func testAppendBrokenWordLinesSlow(textBlock *TextBlock, word string, textAreaWidth float32) ([]string, int) {
	byteIndex := func(runes []rune, runeIndex int) int { return len(string(runes[:runeIndex])) }
	var lines []string
	runes := []rune(word)
	// Each part is measured and made as a part of the whole word, slowly.
	partWidth := func(from, to int) float32 {
		return textBlock.stringWidth(textBlock.part(word, from, to))
	}
	start := 0
	for partWidth(byteIndex(runes, start), len(word)) > textAreaWidth {
		end := nextCharacterBreak(runes, start)
		next := end
		if end < len(runes) {
			next = nextCharacterBreak(runes, end)
		}
		for next < len(runes) &&
			partWidth(byteIndex(runes, start), byteIndex(runes, next)) <= textAreaWidth {
			end = next
			next = nextCharacterBreak(runes, end)
		}
		lines = append(lines, textBlock.part(word, byteIndex(runes, start), byteIndex(runes, end)))
		start = end
	}
	return lines, byteIndex(runes, start)
}

func TestTextBlockAWordIsBrokenAsItWasBeforeTheLinesWereMeasuredAlone(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	for _, c := range []struct {
		word        string
		width       float32
		rightToLeft bool
	}{
		{"pneumonoultramicroscopicsilicovolcanoconiosis", 60, false},
		{"pneumonoultramicroscopicsilicovolcanoconiosis", 6, false}, // One character a line
		{"x", 1, false}, // A single character wider than the line
		{"xy", 1, false},
		{"áéíóú", 7, false},               // No line starts with a combining mark
		{"เกิดเหตุ", 12, false},                // Thai leading vowels
		{strings.Repeat("محمد", 12), 40, true}, // Arabic, right to left
		{"short", 500, false},                  // Fits: no lines
	} {
		block := NewTextBlock(font, c.word).SetRightToLeft(c.rightToLeft)
		wantLines, wantRest := testAppendBrokenWordLinesSlow(block, c.word, c.width)
		gotTextLines, gotRest := block.appendBrokenWordLines(nil, c.word, c.width)
		var got []string
		for _, line := range gotTextLines {
			got = append(got, line.text)
		}
		if fmt.Sprint(got) != fmt.Sprint(wantLines) || gotRest != wantRest {
			t.Errorf("%q at %v: got %q rest %d, want %q rest %d", c.word, c.width, got, gotRest, wantLines, wantRest)
		}
	}
}

func TestTextBlockALongWordIsBrokenInTimeThatGrowsWithTheWord(t *testing.T) {
	font := testHelvetica(testNewPDF())
	word := strings.Repeat("abcdefghij", 10000) // 100,000 characters
	block := NewTextBlock(font, word).SetWidth(100)
	block.SetLocation(0, 0)
	start := time.Now()
	lines := block.getTextLines()
	if took := time.Since(start); took > 3*time.Second {
		t.Errorf("breaking a word of 100,000 characters took %v", took)
	}
	var joined strings.Builder
	for _, line := range lines {
		if w := font.StringWidth(block.fontSize, line.text); w > 100 {
			t.Fatalf("a line is %v wide: %q", w, line.text)
		}
		joined.WriteString(line.text)
	}
	if joined.String() != word {
		t.Errorf("the lines do not join back to the word: %d characters of %d", joined.Len(), len(word))
	}
}

func TestTextBlockALongRightToLeftWordIsBrokenInTimeThatGrowsWithTheWord(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"))
	// 100,000 letters took about 20 minutes
	testLinearTime(t, 5000, func(n int) {
		block := NewTextBlock(font, strings.Repeat("محمد", n)).SetRightToLeft(true).SetWidth(100)
		block.SetLocation(0, 0)
		block.getTextLines()
	})
	block := NewTextBlock(font, strings.Repeat("محمد", 25000)).SetRightToLeft(true).SetWidth(100)
	block.SetLocation(0, 0)
	lines := block.getTextLines()
	letters := 0
	for _, line := range lines {
		if w := font.StringWidth(block.fontSize, line.text); w > 100 {
			t.Fatalf("a line is %v wide: %q", w, line.text)
		}
		letters += len([]rune(line.text))
	}
	if letters != 100000 {
		t.Errorf("the lines have %d letters, not 100,000", letters)
	}
	// The other ports check the same height
	testNear(t, "height", 120006, block.DrawOn(nil)[1], 0.5)
}

// A right to left text cut to its height keeps the start of its last line,
// at the right as it is drawn, and its "..." at the left, where the line
// ends in the reading (review of 11 October 2026, #7).
func TestTextBlockARightToLeftTextCutToItsHeightKeepsTheStartOfItsLastLine(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
	block := NewTextBlock(font, "שלום עולם אחד שתיים שלוש ארבע חמש שש שבע שמונה").SetRightToLeft(true)
	block.SetSize(80, 20)
	lines := block.getTextLines()
	ascent, descent := font.GetAscent(block.fontSize), font.GetDescent(block.fontSize)
	leading := (ascent + descent + font.GetLineGap(block.fontSize)) * block.lineSpacing
	fit := block.linesThatFit(lines, leading)
	if len(fit) >= len(lines) {
		t.Fatalf("the text is not cut: %d lines of %d", len(fit), len(lines))
	}
	last := []rune(lines[len(fit)-1].text)
	got := fit[len(fit)-1].text
	if !strings.HasPrefix(got, "...") || !strings.HasSuffix(got, string(last[3:])) {
		t.Errorf("the last line %q, cut from %q, has lost its start", got, string(last))
	}
}

// A character Unicode does not draw without a glyph, as the variation
// selector after ™ in text pasted from a phone, a zero width space or a word
// joiner, takes no width and draws nothing, where it was a box with a width
// (review of 11 October 2026, #9).
func TestTextBlockAnIgnorableCharacterIsNotDrawn(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
	plain := "Brand™ © 2026 word"
	marked := "Brand™️ ©️ 2026​ wo⁠rd"
	if a, b := font.StringWidth(12, plain), font.StringWidth(12, marked); a != b {
		t.Errorf("the text is %v wide with the marks, %v without", b, a)
	}
	draw := func(text string) string {
		page := NewPage(pdf, testLetterPortrait())
		line := NewTextLine(font, text)
		line.SetLocation(10, 10)
		line.DrawOn(page)
		return testContent(page)
	}
	if a, b := draw(plain), draw(marked); a != b {
		t.Errorf("the marks draw something:\n%q\n%q", b, a)
	}
}

// A core font does not draw or measure them either, nor the marks of
// direction, the joiners and the byte order mark: they were spaces, so that
// "©️ 2026" had two (review of 12 October 2026, R14). GetFitChars kerns with
// the next character drawn, not with a soft hyphen, as StringWidth does (R19).
func TestTextBlockACoreFontDrawsNoIgnorableCharacter(t *testing.T) {
	pdf := testNewPDF()
	font := NewCoreFont(pdf, corefont.Helvetica())
	font.SetSize(12)
	plain := "ab © 2026 word"
	marked := "a\uFE0Fb ©\uFE0F 2026\u200B wo\u2060r\u200Dd\u200E\u200C\u200F\uFEFF"
	if a, b := font.StringWidth(12, plain), font.StringWidth(12, marked); a != b {
		t.Errorf("the text is %v wide with the marks, %v without", b, a)
	}
	draw := func(text string) string {
		page := NewPage(pdf, testLetterPortrait())
		line := NewTextLine(font, text)
		line.SetLocation(10, 10)
		line.DrawOn(page)
		return testContent(page)
	}
	if a, b := draw(plain), draw(marked); a != b {
		t.Errorf("the marks draw something:\n%q\n%q", b, a)
	}
	// A hair over the width, as float32 rounds it
	font.SetKernPairs(true)
	width := font.StringWidth(12, "AVAV") + 0.001
	if n := font.GetFitChars("A\u00adVA\u00adV", width); n != 6 {
		t.Errorf("%d characters fit, not 6", n)
	}
}

// A Stamp leaves out the marks of direction and the joiners its font has no
// glyph for, as Page does: Hebrew with brackets, reordered for drawing, wrote
// two .notdef glyphs (review of 11 October 2026, #10).
func TestTextBlockAStampLeavesOutTheMarksOfDirection(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
	text := ReorderVisually("שלום (abc) עולם")
	if !strings.ContainsAny(text, "‎‏") {
		t.Fatalf("the text has no marks of direction: %q", text)
	}
	stamp := NewStamp(pdf).SetSize(100, 50)
	stamp.DrawText(font, 12, 0, 12, text)
	content := stamp.buf.String()
	// A mark drawn as a glyph the font lacks is in a span of its own, after
	// the first run of glyphs; this text has no character the font lacks
	if strings.Contains(content, "/ActualText") {
		t.Errorf("the stamp draws a mark as a glyph the font lacks: %q", content)
	}
	start, end := strings.Index(content, "<"), strings.Index(content, ">")
	glyphs := content[start+1 : end]
	for i := 0; i+4 <= len(glyphs); i += 4 {
		if glyphs[i:i+4] == "0000" {
			t.Errorf("the stamp draws .notdef: %q", content)
		}
	}
}

// testSoftHyphenWord is a word of soft hyphens, and testSoftHyphenWidth a
// width that fits its start up to the first of them that a line can break at
// past "Donau", with the hyphen, and no more.
const testSoftHyphenWord = "Donau\u00addampf\u00adschiff\u00adfahrt"

func testSoftHyphenWidth(font *Font) float32 {
	return font.StringWidth(font.size, "Donaudampf-") + 1
}

// A word too wide for its line breaks at a soft hyphen, the line ending with
// a hyphen; it was drawn as a hyphen wherever it was, and broken between its
// characters (review of 11 October 2026, #21).
func TestTextBlockBreaksAWordAtASoftHyphen(t *testing.T) {
	font := testHelvetica(testNewPDF())
	block := NewTextBlock(font, "x "+testSoftHyphenWord)
	block.SetWidth(testSoftHyphenWidth(font) + 2*block.GetPadding())
	var lines []string
	for _, line := range block.getTextLines() {
		lines = append(lines, strings.ReplaceAll(line.text, "\u00ad", ""))
	}
	if len(lines) < 2 || lines[0] != "x Donau-" {
		t.Fatalf("the lines %q", lines)
	}
	for _, line := range lines[:len(lines)-1] {
		if !strings.HasSuffix(line, "-") {
			t.Errorf("the lines %q: %q is not broken at a soft hyphen", lines, line)
		}
	}
	if got := strings.ReplaceAll(strings.Join(lines, ""), "-", ""); got != "x Donaudampfschifffahrt" {
		t.Errorf("the text %q", got)
	}
	// A word that fits draws no hyphen.
	fits := NewTextBlock(font, testSoftHyphenWord).SetWidth(500).getTextLines()
	if len(fits) != 1 || strings.Contains(fits[0].text, "-") {
		t.Errorf("the lines of a word that fits: %d", len(fits))
	}
}

// A word broken between its characters breaks at a soft hyphen where one
// fits, as TextFrame breaks each row: "1234\u00ad5678" was "12", "34", "56",
// "78" with no hyphen, where TextFrame draws "34-", and a line ran across
// the soft hyphen of "aaaaaaaaaa\u00adbbbbbbbbbb" where "aa-" fit (review of
// 12 October 2026, R18).
func TestTextBlockAWordBrokenBetweenCharactersBreaksAtASoftHyphen(t *testing.T) {
	font := testHelvetica(testNewPDF())
	lines := func(text string, width float32) []string {
		block := NewTextBlock(font, text)
		block.SetWidth(width + 2*block.GetPadding())
		var texts []string
		for _, line := range block.getTextLines() {
			texts = append(texts, strings.ReplaceAll(line.text, "\u00ad", ""))
		}
		return texts
	}
	// Two digits and a hyphen fit, three digits do not.
	if got := lines("1234\u00ad5678", font.StringWidth(font.size, "00-")+1); strings.Join(got, "|") != "12|34-|56|78" {
		t.Errorf("the lines %q", got)
	}
	got := lines("aaaaaaaaaa\u00adbbbbbbbbbb", font.StringWidth(font.size, "aaaa")+1)
	if strings.Join(got, "|") != "aaaa|aaaa|aa-|bbbb|bbbb|bb" {
		t.Errorf("the lines %q", got)
	}
}

// A right to left text block breaks a word at a soft hyphen, the hyphen at
// the end of the line, on its left: it was cut between two characters, with
// no hyphen (review of 12 October 2026, R17).
func TestTextBlockARightToLeftWordBreaksAtASoftHyphen(t *testing.T) {
	pdf := testNewPDF()
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
	font.SetSize(12)
	for _, text := range []string{"שלום\u00adעולם\u00adאחד", "א שלום\u00adעולם\u00adאחד"} {
		block := NewTextBlock(font, text)
		block.SetRightToLeft(true)
		block.SetWidth(font.StringWidth(12, "שלום-") + 1 + 2*block.GetPadding())
		var got []string
		for _, line := range block.getTextLines() {
			got = append(got, strings.ReplaceAll(line.text, "\u00ad", ""))
		}
		want := "-םולש|-םלוע|דחא"
		if strings.HasPrefix(text, "א ") {
			want = "א|-םולש|-םלוע|דחא"
		}
		if strings.Join(got, "|") != want {
			t.Errorf("%q: the lines %q", text, got)
		}
	}
}

// A vertical alignment that is not one, as alignment.Right, was taken as Top
// without a word: refused when the block is drawn, as a cell's is, Complete
// failing with the message (GitHub Discussion #5)
func TestATextBlockVerticalAlignmentOfRightIsRefused(t *testing.T) {
	pdf := testNewPDF()
	block := NewTextBlock(testHelvetica(pdf), "text").SetVerticalAlignment(alignment.Right)
	block.SetLocation(50, 50)
	block.DrawOn(nil)
	if pdf.err != nil {
		t.Fatalf("measured: %v", pdf.err)
	}
	block.DrawOn(NewPage(pdf, testLetterPortrait()))
	testRecorded(t, pdf, textBlockValignMessage)
}

// A width of NaN drew a character on each line, and gave NaN as the corner
// (review of 17 October 2026).
func TestATextBlockWidthOfNaNIsRefused(t *testing.T) {
	pdf := testNewPDF()
	block := NewTextBlock(testHelvetica(pdf), "Hello world").SetWidth(float32(math.NaN()))
	block.SetLocation(50, 50)
	block.DrawOn(NewPage(pdf, testLetterPortrait()))
	testRecorded(t, pdf, "The width of a text block is a finite number.")
}

// The keywords and the words are in lower case as Java's toLowerCase of
// Locale.ROOT has them: İ is i and a combining dot, and a sigma that ends a
// word is ς (review of 17 October 2026).
func TestTheLowerCaseIsJavas(t *testing.T) {
	for _, c := range [][2]string{
		{"\u039F\u0394\u039F\u03A3", "\u03BF\u03B4\u03BF\u03C2"},
		{"\u03A3\u0391", "\u03C3\u03B1"},
		{"\u0130STANBUL", "i\u0307stanbul"},
		{"A\u03A3.", "a\u03C2."},
		{"A\u03A3'B", "a\u03C3'b"},
		{"\u03A3", "\u03C3"},
		{"A\u03A3\u0301", "a\u03C2\u0301"},
		{"\u00C0\u03A31", "\u00E0\u03C21"},
		{"\u0391\u03A3\u00AD\u0392", "\u03B1\u03C3\u00AD\u03B2"},
	} {
		if got := lowerAsJava(c[0]); got != c[1] {
			t.Errorf("%q: %q, Java has %q", c[0], got, c[1])
		}
	}
}
