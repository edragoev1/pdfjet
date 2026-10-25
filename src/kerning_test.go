// kerning_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testKernedFont opens a font of the repository, with its kerning on or off.
func testKernedFont(t *testing.T, pdf *PDF, path string, kerned bool) *Font {
	t.Helper()
	file, err := os.Open(testRepoPath(t, path))
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	defer file.Close()
	font := NewFont(pdf, file)
	font.SetKernPairs(kerned)
	return font
}

var drawnText = regexp.MustCompile(`<([0-9A-F]*)> Tj|\[(-?[0-9.]+)\] TJ`)

// testDrawnWidth returns how wide the text is as the page draws it, at the
// size: the advances of its glyphs, with the moves of the TJ arrays between
// them, from the content of the page.
func testDrawnWidth(font *Font, content string, size float32) float32 {
	var units float32
	var moves float32
	for _, m := range drawnText.FindAllStringSubmatch(content, -1) {
		if m[2] != "" {
			v, _ := strconv.ParseFloat(m[2], 32)
			moves += float32(v)
			continue
		}
		for i := 0; i+4 <= len(m[1]); i += 4 {
			gid, _ := strconv.ParseInt(m[1][i:i+4], 16, 32)
			units += float32(font.glyphAdvance(int(gid)))
		}
	}
	return units*size/float32(font.unitsPerEm) - moves*size/1000
}

// testDraw draws the text with the font on a page of its own, and returns
// what the page wrote for it.
func testDraw(font *Font, text string) string {
	page := NewPage(font.pdf, letter.Portrait())
	start := len(page.buf)
	page.DrawString(font, nil, 10, text, 50, 50)
	return string(page.buf[start:])
}

func TestKerningNarrowsTheKernedPairsOfAnOpenTypeFont(t *testing.T) {
	pdf := testNewPDF()
	kerned := testKernedFont(t, pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true)
	plain := testKernedFont(t, pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", false)
	for _, pair := range []string{"AV", "To", "Ty", "LT"} {
		if !(kerned.StringWidth(10, pair) < plain.StringWidth(10, pair)) {
			t.Errorf("%s: kerned %v, not kerned %v", pair, kerned.StringWidth(10, pair), plain.StringWidth(10, pair))
		}
	}
	if kerned.StringWidth(10, "HH") != plain.StringWidth(10, "HH") {
		t.Errorf("HH kerned")
	}
}

func TestKerningIsDrawnAsItIsMeasured(t *testing.T) {
	pdf := testNewPDF()
	for _, path := range []string{
		"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf",
		"fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
		"fonts/NotoSans/NotoSans-Regular.ttf",
	} {
		font := testKernedFont(t, pdf, path, true)
		for _, text := range []string{
			"AVATAR. To Wally, Tyler and LT: \"Yes,\" P.V. said.",
			"Обява: Тъй, Уважаеми Г-н Петров, ТАТЯНА.",
			"Ταύτα, ΑΥΤΟ ΤΟ ΒΑΓΟΝΙ.",
			"A\u00a0V, then A V again",
			"soft\u00adhy\u200bphen, T\ufeffo",
		} {
			drawn := testDrawnWidth(font, testDraw(font, text), 10)
			testNear(t, path+": "+text, font.StringWidth(10, text), drawn, 0.01)
		}
	}
}

func TestKerningOfColoredTextIsDrawnAsItIsMeasured(t *testing.T) {
	pdf := testNewPDF()
	font := testKernedFont(t, pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true)
	text := "AVATAR To \"Wally\" LT."
	page := NewPage(pdf, letter.Portrait())
	start := len(page.buf)
	page.drawColoredString(font, text, [3]float32{}, map[string]int32{"To": 0xFF0000})
	testNear(t, "colored", font.StringWidth(12, text), testDrawnWidth(font, string(page.buf[start:]), 12), 0.01)
	// A word with a mark, drawn on the shaping path, kerns with the last
	// glyph of the word before it, as StringWidth measures it
	shaped := "A V. To Te\u0301 LT"
	start = len(page.buf)
	page.drawColoredString(font, shaped, [3]float32{}, map[string]int32{"To": 0xFF0000})
	testNear(t, "colored, shaped", font.StringWidth(12, shaped), testDrawnWidth(font, string(page.buf[start:]), 12), 0.01)
}

func TestKerningIsOffUnlessAskedFor(t *testing.T) {
	pdf := testNewPDF()
	font := testKernedFont(t, pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", false)
	if content := testDraw(font, "AVATAR To"); strings.Contains(content, "] TJ") {
		t.Errorf("a font not kerned moved its glyphs:\n%s", content)
	}
}

func TestKerningLeavesMarksAndRightToLeftTextAlone(t *testing.T) {
	pdf := testNewPDF()
	for _, path := range []string{
		"fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf",
		"fonts/NotoSansArabic/NotoSansArabic-Regular.ttf",
	} {
		kerned := testKernedFont(t, pdf, path, true)
		plain := testKernedFont(t, pdf, path, false)
		for _, text := range []string{"שָׁלוֹם עוֹלָם", "مرحبا بالعالم"} {
			if kerned.StringWidth(10, text) != plain.StringWidth(10, text) {
				t.Errorf("%s: %q kerned", path, text)
			}
		}
	}
}

func TestKerningFitsWhatIsMeasured(t *testing.T) {
	pdf := testNewPDF()
	font := testKernedFont(t, pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", true)
	text := "AVATAR To Wally"
	// GetFitChars measures at the font's own size
	width := font.StringWidth(font.GetSize(), text)
	if n := font.GetFitChars(text, width+0.001); n != len([]rune(text)) {
		t.Errorf("%d of %d characters fit in their own width", n, len([]rune(text)))
	}
	if n := font.GetFitChars(text, width-0.1); n == len([]rune(text)) {
		t.Errorf("all the characters fit in less than their width")
	}
	// A pair kerned wider, "T after a quotation mark, takes its room only
	// with its second character: every start of the text fits in its width
	wider := "\"Tea\" &co"
	runes := []rune(wider)
	for n := 1; n <= len(runes); n++ {
		width := font.StringWidth(font.GetSize(), string(runes[:n]))
		if fit := font.GetFitChars(wider, width+0.001); fit != n {
			t.Errorf("%q: %d characters fit in the width of %d", wider, fit, n)
		}
	}
}

func TestKerningOfABrokenTableIsNone(t *testing.T) {
	buf := []byte{0, 1, 0, 0, 0, 10, 0, 10, 0, 10, 0xFF, 0xFF, 0xFF, 0xFF}
	k := newKerning(buf, &fontTable{offset: 0, length: len(buf)}, &fontTable{offset: 4, length: 900}, 10)
	if v := k.pair(1, 2); v != 0 {
		t.Errorf("a broken table kerns: %d", v)
	}
}

// The same widths in the four ports, at 10 points: what each port measures,
// and so wraps and aligns, is the same to the hundredth of a point.
func TestKerningWidthsAreThoseOfTheOtherPorts(t *testing.T) {
	pdf := testNewPDF()
	for _, c := range []struct {
		path, text      string
		kerned, unitsOf float32
	}{
		{"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "AVATAR To Wally", 75.14, -302},
		{"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "Тъй ТАТЯНА", 55.94, -165},
		{"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf", "ΑΥΤΟ", 24.44, -70},
		{"fonts/IBMPlexSans/IBMPlexSans-Regular.otf", "AVATAR To Wally", 75.14, -302},
		{"fonts/NotoSans/NotoSans-Regular.ttf", "AVATAR To Wally", 75.83, -310},
		{"fonts/NotoSans/NotoSans-Regular.ttf", "Тъй ТАТЯНА", 56.79, -150},
		{"fonts/NotoSans/NotoSans-Regular.ttf", "ΑΥΤΟ", 24.62, -80},
	} {
		font := testKernedFont(t, pdf, c.path, true)
		testNear(t, c.path+": "+c.text, c.kerned, font.StringWidth(10, c.text), 0.005)
		testNear(t, c.path+": "+c.text+" in font units", c.unitsOf, float32(font.textKerning(c.text)), 0)
	}
}

func TestKerningOfAClassOutOfRangeIsTheNextSubtables(t *testing.T) {
	k := new(kerning)
	k.read()
	k.lookups = [][]pairSubtable{{
		{covered: map[int]bool{1: true}, firstClass: map[int]int{}, secondClass: map[int]int{2: 5}, secondCount: 2, classAdvance: make([]int16, 2)},
		{covered: map[int]bool{1: true}, pairs: map[uint32]int16{1<<16 | 2: -40}},
	}}
	if v := k.pair(1, 2); v != -40 {
		t.Errorf("the pair kerns %d, not the -40 of the next subtable", v)
	}
}
