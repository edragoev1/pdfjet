// textblockshaped_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"reflect"
	"regexp"
	"strconv"
	"strings"
	"testing"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// TextBlock and TextLine with text such as Hindi or Khmer: a shaper wraps
// and shapes it, and the glyphs it found are drawn as outlines, each line
// tagged with its text; the other paragraphs are drawn as text. Go has no
// shaper of its own: the tests give one that returns fixed lines.

const testComplexDevanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
const testComplexLatin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

// testFixedShaper gives the lines it was made with, and keeps what it was given.
type testFixedShaper struct {
	lines    []ShapedLine
	files    [][]byte
	size     float32
	width    float32
	language string
	calls    int
}

func (shaper *testFixedShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	shaper.files = request.FontFiles
	shaper.size = request.FontSize
	shaper.width = request.Width
	shaper.language = request.Language
	shaper.calls++
	return shaper.lines, nil
}

// testLetterShaper gives each paragraph as many lines as its letters, one
// glyph of the letter each.
type testLetterShaper struct {
	font *Font
}

func (shaper *testLetterShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	var lines []ShapedLine
	for i, r := range request.Text {
		lines = append(lines, ShapedLine{Start: i, End: i + utf8.RuneLen(r), FontIndexes: []int{0},
			Glyphs: []int{shaper.font.unicodeToGID.get(r)}, Positions: []float32{0, 0}, Width: 7})
	}
	return lines, nil
}

func testNumber(t *testing.T, s string) float32 {
	t.Helper()
	v, err := strconv.ParseFloat(s, 32)
	if err != nil {
		t.Fatal(err)
	}
	return float32(v)
}

func TestTextBlockTheShaperGivesTheGlyphsAndTheirPlaces(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	fallback := NewFontFromFile(doc.pdf, testRepoPath(t, testCascadia))
	a := font.unicodeToGID.get('A')
	b := fallback.unicodeToGID.get('B')
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 6, FontIndexes: []int{0, 1}, Glyphs: []int{a, b}, Positions: []float32{0, 0, 7, 0}, Width: 13},
		// A glyph of a font that was not given is not drawn
		{Start: 6, End: 9, FontIndexes: []int{-1}, Glyphs: []int{5}, Positions: []float32{0, 0}},
	}}
	page := NewPage(doc.pdf, letter.Portrait())
	text := NewTextBlock(font, "कखग").SetFallbackFont(fallback).SetFontSize(10).SetLanguage("en").SetShaper(shaper)
	text.SetWidth(120)
	text.SetLocation(50, 100)
	corner := text.DrawOn(page)
	if len(shaper.files) != 2 || shaper.size != 10 || shaper.width != 120 || shaper.language != "en" {
		t.Errorf("the shaper was given %d files, %v, %v, %q", len(shaper.files), shaper.size, shaper.width,
			shaper.language)
	}
	lineHeight := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	testNear(t, "the bottom", 100+2*lineHeight, corner[1], 0.001)
	// The fallback's glyph at the font's size, 10 points, on the first line's baseline
	y := 792 - (100 + font.GetAscent(10))
	content := testContent(page)
	matches := regexp.MustCompile(`q (\S+) 0 0 \S+ (\S+) (\S+) cm /Fm`).FindAllStringSubmatch(content, -1)
	if len(matches) != 2 {
		t.Fatal(content)
	}
	testNear(t, "Plex's scale, 1,000 units an em", 0.01, testNumber(t, matches[0][1]), 1e-6)
	testNear(t, "Cascadia's scale, 2,048", 10.0/2048, testNumber(t, matches[1][1]), 1e-6)
	testNear(t, "A's x", 50, testNumber(t, matches[0][2]), 0.001)
	testNear(t, "B's x", 57, testNumber(t, matches[1][2]), 0.001)
	testNear(t, "A's y", y, testNumber(t, matches[0][3]), 0.006)
	testNear(t, "B's y", y, testNumber(t, matches[1][3]), 0.006)
	// Shaped once however often the block is measured and drawn
	text.DrawOn(nil)
	text.DrawOn(page)
	if shaper.calls != 1 {
		t.Errorf("shaped %d times", shaper.calls)
	}
	doc.complete()
}

// A block of a paragraph of English, one of Hindi and one of Arabic: the
// English and the Arabic drawn as text, the Arabic right to left by itself
// and aligned to the right; the Hindi as glyphs, between them.
func TestTextBlockDrawsEachParagraphAsItNeeds(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	arabic := NewFontFromFile(doc.pdf, testRepoPath(t, "resources/fonts/world/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf"))
	page := NewPage(doc.pdf, letter.Portrait())
	text := NewTextBlock(font, "Hello\nकखग\nمرحبا").SetFallbackFont(arabic).SetFontSize(10).
		SetLanguage("hi").SetShaper(&testLetterShaper{font})
	text.SetWidth(300)
	text.SetLocation(50, 100)
	text.DrawOn(page)
	content := testContent(page)
	doc.complete()
	if strings.Count(content, " cm /Fm") != 3 {
		t.Errorf("not the three Hindi letters drawn as glyphs:\n%s", content)
	}
	for _, letter := range []string{"क", "ख", "ग"} {
		if !strings.Contains(content, "/ActualText <"+toUTF16Hex(letter)+">") {
			t.Errorf("the letter %s not tagged with its text", letter)
		}
	}
	// The Arabic line: as text, at the right of the block
	tms := regexp.MustCompile(`1 0 0 1 (\S+) \S+ Tm`).FindAllStringSubmatch(content, -1)
	if len(tms) != 2 {
		t.Fatalf("%d lines of text:\n%s", len(tms), content)
	}
	if testNumber(t, tms[0][1]) != 50 || testNumber(t, tms[1][1]) <= 300 {
		t.Errorf("English at %s, Arabic at %s", tms[0][1], tms[1][1])
	}
}

func TestTextBlockTheFontFileIsWhatTheFontWasLoadedFrom(t *testing.T) {
	doc := testNewDoc()
	path := testRepoPath(t, testComplexDevanagari)
	data, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	fromBytes := NewFontFromBytes(doc.pdf, data)
	fromPath := NewFontFromFile(doc.pdf, path)
	cascadia, err := os.ReadFile(testRepoPath(t, testCascadia))
	if err != nil {
		t.Fatal(err)
	}
	trueType := NewFont(doc.pdf, bytes.NewReader(cascadia))
	shaper := &testFixedShaper{}
	page := NewPage(doc.pdf, letter.Portrait())
	NewTextBlock(fromBytes, "क").SetFallbackFont(fromPath).SetShaper(shaper).DrawOn(page)
	if &shaper.files[0][0] != &data[0] || !bytes.Equal(shaper.files[1], data) {
		t.Error("the font files are not those the fonts were loaded from")
	}
	// A TrueType font keeps its whole file, from a reader too
	NewTextBlock(trueType, "क").SetShaper(shaper).DrawOn(page)
	if !bytes.Equal(shaper.files[0], cascadia) {
		t.Error("the TrueType font's file")
	}
}

func TestTextBlockWithoutAShaperOrAFontFileItSaysSo(t *testing.T) {
	doc := testNewDoc()
	fromPath := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	NewTextBlock(fromPath, "क").DrawOn(NewPage(doc.pdf, letter.Portrait()))
	if err := doc.pdf.Complete(); err == nil || !strings.Contains(err.Error(), "SetShaper") {
		t.Errorf("no shaper: %v", err)
	}
	doc = testNewDoc()
	otf, err := os.ReadFile(testRepoPath(t, testComplexLatin))
	if err != nil {
		t.Fatal(err)
	}
	fromReader := NewFont(doc.pdf, bytes.NewReader(otf))
	NewTextBlock(fromReader, "क").SetShaper(&testFixedShaper{}).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	if err := doc.pdf.Complete(); err == nil || !strings.Contains(err.Error(), "font files") {
		t.Errorf("an .otf from a reader: %v", err)
	}
}

// testAligned draws two lines: "A A B " of 30 points without its last space,
// then "B", and returns the x of each glyph drawn.
func testAligned(t *testing.T, textAlignment alignment.Alignment) []float32 {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	a := font.unicodeToGID.get('A')
	b := font.unicodeToGID.get('B')
	space := font.unicodeToGID.get(' ')
	text := "क क ख ख" // 15 bytes: the first line the first 12
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 12, FontIndexes: []int{0, 0, 0, 0, 0, 0}, Glyphs: []int{a, space, a, space, b, space},
			Positions: []float32{0, 0, 10, 0, 12, 0, 22, 0, 24, 0, 30, 0}, Width: 30},
		{Start: 12, End: 15, FontIndexes: []int{0}, Glyphs: []int{b}, Positions: []float32{0, 0}, Width: 6},
	}}
	page := NewPage(doc.pdf, letter.Portrait())
	block := NewTextBlock(font, text).SetFontSize(10).SetShaper(shaper).SetTextAlignment(textAlignment)
	block.SetWidth(100)
	block.SetLocation(50, 100)
	block.DrawOn(page)
	var xs []float32
	for _, m := range regexp.MustCompile(`q \S+ 0 0 \S+ (\S+) \S+ cm /Fm`).FindAllStringSubmatch(testContent(page), -1) {
		xs = append(xs, testNumber(t, m[1]))
	}
	return xs
}

func TestTextBlockTheShapedLinesAreAligned(t *testing.T) {
	// The glyphs drawn: A, A, B of the first line, B of the second
	for _, c := range []struct {
		alignment alignment.Alignment
		want      []float32
	}{
		{alignment.Left, []float32{50, 62, 74, 50}},
		{alignment.Center, []float32{85, 97, 109, 97}},
		{alignment.Right, []float32{120, 132, 144, 144}},
	} {
		if got := testAligned(t, c.alignment); !reflect.DeepEqual(got, c.want) {
			t.Errorf("alignment %d: %v, not %v", c.alignment, got, c.want)
		}
	}
}

func TestTextBlockShapedLinesAreSpacedAsItsText(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	body := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	text := NewTextBlock(font, "कख").SetFontSize(10).SetShaper(&testLetterShaper{font})
	text.SetWidth(200)
	testNear(t, "at 1.0", 2*body, text.DrawOn(nil)[1], 0.001)
	text.SetLineSpacing(1.5)
	testNear(t, "at 1.5", 3*body, text.DrawOn(nil)[1], 0.001)
	// With a height, the lines that fit, as DrawOn draws them, the glyphs not
	// cut with "..."
	text.SetLineSpacing(1).SetHeight(1.5 * body)
	page := NewPage(doc.pdf, letter.Portrait())
	text.DrawOn(page)
	if n := strings.Count(testContent(page), " cm /Fm"); n != 1 {
		t.Errorf("%d glyphs drawn in the height of a line", n)
	}
}

// Text such as Hindi in Table cells: each cell as tall as its text wrapped
// to the column, the text drawn whole each time, as the header row is on
// each page.
func TestTextBlockShapedInTableCellsWrapsAndRepeatsTheHeader(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	line := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	shaper := &testLetterShaper{font}
	hindi := func(text string) *TextBlock {
		return NewTextBlock(font, text).SetFontSize(10).SetShaper(shaper).SetLanguage("hi")
	}
	data := [][]*Cell{{NewCell(font, "").SetTextBlock(hindi("क")), NewCell(font, "").SetTextBlock(hindi("ख"))}}
	for r := 0; r < 40; r++ {
		data = append(data, []*Cell{NewCell(font, "Row "+strconv.Itoa(r+1)), NewCell(font, "").SetTextBlock(hindi("कखग"))})
	}
	table := NewTable().SetTableData(data, 1)
	table.SetLocation(50, 50)
	table.SetColumnWidth(0, 100).SetColumnWidth(1, 250)
	// A cell is as tall as its lines, three here, and its padding
	cell := data[1][1]
	testNear(t, "the cell's height", 3*line+cell.GetTopPadding()+cell.GetBottomPadding(), cell.GetHeight(250), 0.01)
	var pages []*Page
	table.DrawOnPages(doc.pdf, &pages, letter.Portrait())
	if len(pages) < 2 {
		t.Fatalf("the table on %d page", len(pages))
	}
	first := testContent(pages[0])
	for _, word := range []string{"क", "ख"} {
		if !strings.Contains(first, "/ActualText <"+toUTF16Hex(word)+">") {
			t.Errorf("the header's %s on the first page", word)
		}
	}
	for i := 1; i < len(pages); i++ {
		if strings.Count(testContent(pages[i]), " cm /Fm") < 2 {
			t.Errorf("page %d: no glyphs", i+1)
		}
	}
}

// TextLine with text such as Hindi: shaped as one line, drawn as glyphs,
// tagged with its text, its width the glyphs'.
func TestTextLineShapesTextSuchAsHindi(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	font.SetSize(10)
	SetDefaultShaper(&testLetterShaper{font})
	defer SetDefaultShaper(nil)
	line := NewTextLine(font, "कखग")
	line.SetLocation(50, 100)
	testNear(t, "its width", 21, line.GetWidth(), 0.001)
	page := NewPage(doc.pdf, letter.Portrait())
	line.DrawOn(page)
	content := testContent(page)
	if strings.Count(content, " cm /Fm") != 3 || !strings.Contains(content, "/ActualText <"+toUTF16Hex("कखग")+">") {
		t.Errorf("not drawn as glyphs with its text:\n%s", content)
	}
	if strings.Contains(content, "Tj") {
		t.Errorf("drawn as text too:\n%s", content)
	}
	doc.complete()
}
