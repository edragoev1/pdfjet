// kerning_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testKerningSerif is IBM Plex Serif, whose kern lookups have LookupFlag 8,
// as those of Noto Sans, which these tests read before 9.1.7, have; the
// lookups of IBM Plex Sans have none. testKerningExtension is IBM Plex Sans
// JP, whose kern feature has Extension lookups of its pairs, as Source Serif
// 4, which PDFjet carried before, has.
const (
	testKerningSerif     = "fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf"
	testKerningExtension = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"
)

// testKernedFont opens a font of the repository, or one PDFjet carries, with
// its kerning on or off.
func testKernedFont(t *testing.T, pdf *PDF, path string, kerned bool) *Font {
	t.Helper()
	font := NewFont(pdf, bytes.NewReader(testFontBytes(t, path)))
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
		"tests/fonts/IBMPlexSans-Regular.otf",
		testKerningSerif,
		testKerningExtension,
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

var drawnCoreText = regexp.MustCompile(`\[([^\]]*)\] TJ`)
var drawnCorePiece = regexp.MustCompile(`<([0-9A-F]*)>|(-?[0-9.]+)`)

// testCoreDrawnWidth returns how wide the text drawn in a core font is as the
// page draws it, at the size: the widths of its characters, with the moves of
// the TJ arrays before and between them, from the content of the page. The
// end of its last character is then where StringWidth says the text ends.
func testCoreDrawnWidth(font *Font, content string, size float32) float32 {
	var units float32
	for _, tj := range drawnCoreText.FindAllStringSubmatch(content, -1) {
		for _, m := range drawnCorePiece.FindAllStringSubmatch(tj[1], -1) {
			if m[2] != "" {
				v, _ := strconv.ParseFloat(m[2], 32)
				units -= float32(v)
				continue
			}
			for i := 0; i+2 <= len(m[1]); i += 2 {
				code, _ := strconv.ParseInt(m[1][i:i+2], 16, 32)
				units += float32(font.metrics[code-32][1])
			}
		}
	}
	return units * size / 1000
}

// Colored text in a core font kerns the pairs across its words and its color
// changes, as StringWidth measures them: Helvetica kerns "Y." (KPX Y period),
// and a red "Y" before the period was drawn without it (the review of
// 28 October 2026). A color changes only between words, so a pair inside a
// word, as "AV", is never split by one.
func TestKerningOfColoredCoreFontTextIsDrawnAsItIsMeasured(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	font.SetKernPairs(true)
	colors := map[string]int32{"Y": 0xFF0000, "To": 0x0000FF}
	for _, text := range []string{
		"Y.",
		"AVATAR Y. To \"Wally\", LT: P.V. said.",
		"T\u00adoo, Y\u00ad.",
	} {
		page := NewPage(pdf, letter.Portrait())
		start := len(page.buf)
		page.drawColoredString(font, text, [3]float32{}, colors)
		content := string(page.buf[start:])
		testNear(t, text, font.StringWidth(12, text), testCoreDrawnWidth(font, content, 12), 0.001)
	}
	// "Y." kerns by 140 units, the period moved as the TJ array it starts
	page := NewPage(pdf, letter.Portrait())
	start := len(page.buf)
	page.drawColoredString(font, "Y.", [3]float32{}, colors)
	if content := string(page.buf[start:]); !strings.Contains(content, "[140<2E>] TJ") {
		t.Errorf("the period is not kerned:\n%s", content)
	}
}

// Text in a core font that is not colored is drawn as before the kerning of
// colored text across its words: one TJ array, kerned inside it.
func TestKerningOfCoreFontTextNotColoredIsUnchanged(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	font.SetKernPairs(true)
	page := NewPage(pdf, letter.Portrait())
	start := len(page.buf)
	page.DrawStringUsingFontSize(font, 12, "AV Y. To", 50, 50)
	want := "BT\n/F1 12 Tf\n50 742 Td\n0 0 0 rg\n[<41>70<5620>90<59>140<2E>60<20>50<54>120<6F>] TJ\nET\n"
	if content := string(page.buf[start:]); content != want {
		t.Errorf("want %q, got %q", want, content)
	}
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
	// IBM Plex Sans Hebrew kerns שלום עולם by 20 units without its marks, as
	// Noto Sans Hebrew, which this test read before 9.1.7, kerned it by 10.
	for _, path := range []string{
		"fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf",
		"fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf",
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
	k := newKerning(buf, &fontTable{offset: 0, length: len(buf)}, 10)
	if v := k.read(nil).pair(1, 2); v != 0 {
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
		{"tests/fonts/IBMPlexSans-Regular.otf", "AVATAR To Wally", 75.14, -302},
		// IBM Plex Serif, from its hmtx and kern lookups (Noto Sans, before
		// 9.1.7: 75.83 and -310, 56.79 and -150, 24.62 and -80)
		{testKerningSerif, "AVATAR To Wally", 80.59, -315},
		{testKerningSerif, "Тъй ТАТЯНА", 61.57, -147},
		{testKerningSerif, "ΑΥΤΟ", 26.40, -48},
	} {
		font := testKernedFont(t, pdf, c.path, true)
		testNear(t, c.path+": "+c.text, c.kerned, font.StringWidth(10, c.text), 0.005)
		testNear(t, c.path+": "+c.text+" in font units", c.unitsOf, float32(font.textKerning(c.text)), 0)
	}
}

func TestKerningOfAClassOutOfRangeIsTheNextSubtables(t *testing.T) {
	k := new(kerning)
	k.read(nil)
	k.lookups = [][]pairSubtable{{
		{covered: []uint64{1 << 1}, secondClass: []uint16{0, 0, 5}, secondCount: 2, classAdvance: make([]int16, 2)},
		{covered: []uint64{1 << 1}, pairSets: []int32{0, 1}, seconds: [][]int32{{2}}, values: [][]int16{{-40}}},
	}}
	if v := k.pair(1, 2); v != -40 {
		t.Errorf("the pair kerns %d, not the -40 of the next subtable", v)
	}
}

func TestKerningPairSetsOutOfOrderKeepTheFirstRecordOfAPair(t *testing.T) {
	seconds, values := sortedPairs([]int32{9, 3, 9, 5}, []int16{-10, -20, -30, -40})
	want := []int32{3, 5, 9}
	wantValues := []int16{-20, -40, -10}
	for i := range want {
		if i >= len(seconds) || seconds[i] != want[i] || values[i] != wantValues[i] {
			t.Fatalf("%v %v, not %v %v", seconds, values, want, wantValues)
		}
	}
	if len(seconds) != len(want) {
		t.Errorf("%v, not %v", seconds, want)
	}
}

// testKernTable returns a GPOS table whose kern feature has the lookups, all
// one lookup of the subtables, all one PairPos subtable of the format, which
// moves the glyph after itself by -50: the pair of the glyph, or the pair of
// its class 1.
func testKernTable(lookups, subtables, format, glyph int) []byte {
	var b []byte
	put := func(values ...int) {
		for _, v := range values {
			b = append(b, byte(v>>8), byte(v))
		}
	}
	put(1, 0, 10, 30, 42+2*lookups)
	put(1, 0x4446, 0x4C54, 8) // DFLT
	put(4, 0, 0, 0xFFFF, 1, 0)
	put(1, 0x6B65, 0x726E, 8) // kern
	put(0, lookups)
	for i := 0; i < lookups; i++ {
		put(i)
	}
	put(lookups)
	for i := 0; i < lookups; i++ {
		put(2 + 2*lookups)
	}
	put(2, 0, subtables)
	for i := 0; i < subtables; i++ {
		put(6 + 2*subtables)
	}
	if format == 1 {
		put(1, 12, 4, 0, 1, 18) // The coverage at 12, the pair set at 18
		put(1, 1, glyph)
		put(1, glyph, -50)
	} else {
		put(2, 24, 4, 0, 30, 30, 2, 2) // The coverage at 24, the classes at 30
		put(0, 0, 0, -50)
		put(1, 1, glyph)
		put(1, glyph, 1, 1)
	}
	return b
}

// testKernRoom returns the room the arrays of the kerning take, in words of
// 64 bits.
func testKernRoom(k *kerning) int {
	room := 0
	for _, subtables := range k.lookups {
		for _, s := range subtables {
			room += len(s.covered) + (len(s.pairSets)+1)/2 + (len(s.firstClass)+3)/4 + (len(s.secondClass)+3)/4
		}
	}
	return room
}

// A table of a few kilobytes whose subtables named glyph 65535 again and again
// made gigabytes of arrays by glyph; the room they make is work, and a
// subtable a lookup names again is read once (the review of 28 October 2026).
func TestKerningOfATableThatNamesGlyph65535AgainIsBounded(t *testing.T) {
	for _, c := range []struct {
		name                       string
		lookups, subtables, format int
	}{
		{"a subtable named again", 1, 400, 1},
		{"pairs in many lookups", 400, 1, 1},
		{"classes in many lookups", 400, 1, 2},
	} {
		buf := testKernTable(c.lookups, c.subtables, c.format, 65535)
		k := newKerning(buf, &fontTable{offset: 0, length: len(buf)}, 0)
		if room := testKernRoom(k.read(nil)); room > maxGposWork {
			t.Errorf("%s: %d words, more than the %d of the work", c.name, room, maxGposWork)
		}
	}
	buf := testKernTable(1, 400, 1, 65535)
	k := newKerning(buf, &fontTable{offset: 0, length: len(buf)}, 0)
	if v := k.read(nil).pair(65535, 65535); v != -50 {
		t.Errorf("the subtable named again kerns %d, not -50", v)
	}
	// A glyph past the glyphs of the font is left out; a glyph of the font
	// kerns as before.
	for _, format := range []int{1, 2} {
		buf := testKernTable(1, 1, format, 65535)
		k := newKerning(buf, &fontTable{offset: 0, length: len(buf)}, 1000)
		if room := testKernRoom(k.read(nil)); room > 0 || k.pair(65535, 65535) != 0 {
			t.Errorf("format %d: %d words for a glyph the font does not have", format, room)
		}
		buf = testKernTable(1, 1, format, 7)
		k = newKerning(buf, &fontTable{offset: 0, length: len(buf)}, 1000)
		if v := k.read(nil).pair(7, 7); v != -50 {
			t.Errorf("format %d: glyph 7 kerns %d, not -50", format, v)
		}
	}
}
