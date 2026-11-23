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
	"github.com/edragoev1/pdfjet/v9/src/corefont"
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

// A TextLine of text such as Hindi turned: its glyphs turned around its
// start as the same line of English is, the one rotation for both.
func TestTextLineTurnsItsGlyphsAsItsText(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	font.SetSize(10)
	SetDefaultShaper(&testLetterShaper{font})
	defer SetDefaultShaper(nil)
	latin := NewFontFromFile(doc.pdf, testRepoPath(t, testPlexOTF))
	latin.SetSize(10)
	for _, degrees := range []int{30, 90, -45} {
		english := NewPage(doc.pdf, letter.Portrait())
		text := NewTextLine(latin, "Hello")
		text.SetLocation(50, 100)
		text.SetTextRotation(degrees)
		text.DrawOn(english)
		englishContent := testContent(english)
		hindi := NewPage(doc.pdf, letter.Portrait())
		line := NewTextLine(font, "कखग")
		line.SetLocation(50, 100)
		line.SetTextRotation(degrees)
		line.DrawOn(hindi)
		tm := regexp.MustCompile(`(\S+) (\S+) (\S+) (\S+) \S+ \S+ Tm`).FindStringSubmatch(englishContent)
		content := testContent(hindi)
		cm := regexp.MustCompile(`q\n1 0 0 1 50 692 cm\n(\S+) (\S+) (\S+) (\S+) 0 0 cm\n1 0 0 1 -50 -692 cm\n`).
			FindStringSubmatch(content)
		if tm == nil || cm == nil {
			t.Fatalf("%d degrees: no text matrix or no turn:\n%s\n%s", degrees, englishContent, content)
		}
		for i := 1; i <= 4; i++ {
			a, _ := strconv.ParseFloat(tm[i], 64)
			b, _ := strconv.ParseFloat(cm[i], 64)
			testNear(t, "the turn", float32(a), float32(b), 1e-5)
		}
		if strings.Count(content, " cm /Fm") != 3 {
			t.Errorf("%d degrees: not its three glyphs:\n%s", degrees, content)
		}
	}
	// Not turned: no turn written
	page := NewPage(doc.pdf, letter.Portrait())
	line := NewTextLine(font, "कखग")
	line.SetLocation(50, 100)
	line.DrawOn(page)
	if strings.Contains(testContent(page), "1 0 0 1 -50 -692 cm") {
		t.Errorf("turned at 0 degrees:\n%s", testContent(page))
	}
	doc.complete()
}

// testCountingShaper shapes as testLetterShaper does, and counts its calls
type testCountingShaper struct {
	testLetterShaper
	calls int
	files [][]byte
}

func (shaper *testCountingShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	shaper.calls++
	shaper.files = request.FontFiles
	lines, err := shaper.testLetterShaper.Shape(request)
	// A space as wide as the font's, and the spaces at the end no width, as a
	// real shaper leaves the white space at the end of a line out of its width
	for i := range lines {
		if strings.TrimSpace(request.Text[lines[i].Start:]) == "" {
			lines[i].Width = 0
		} else if request.Text[lines[i].Start:lines[i].End] == " " {
			lines[i].Width = shaper.font.StringWidth(request.FontSize, " ")
		}
	}
	return lines, err
}

// A TextFrame of Hindi words places them by their shaped widths, the
// widths they are drawn at, each word shaped once however often measured
func TestTextFrameMeasuresShapedWords(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	font.SetSize(10)
	shaper := &testCountingShaper{testLetterShaper: testLetterShaper{font}}
	SetDefaultShaper(shaper)
	defer SetDefaultShaper(nil)
	paragraph := NewParagraph().Add(NewTextLine(font, "कख गघ ङच"))
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetWidth(300)
	frame.SetLocation(10, 10)
	frame.SetHeight(500)
	page := NewPage(doc.pdf, letter.Portrait())
	frame.DrawOn(page)
	var xs []float32
	for _, m := range regexp.MustCompile(`q \S+ 0 0 \S+ (\S+) \S+ cm /Fm`).FindAllStringSubmatch(testContent(page), -1) {
		xs = append(xs, testNumber(t, m[1]))
	}
	if len(xs) != 6 {
		t.Fatalf("%d glyphs: %v\n%s", len(xs), xs, testContent(page))
	}
	// Each word 14 points, and the space after it the font's
	space := font.StringWidth(10, " ")
	want := []float32{10, 17, 24 + space, 31 + space, 38 + 2*space, 45 + 2*space}
	for i := range want {
		testNear(t, "a glyph's x", want[i], xs[i], 0.01)
	}
	if shaper.calls > 8 {
		t.Errorf("shaped %d times: each word once, and the pieces drawn", shaper.calls)
	}
	// In a frame 43 points wide, two words and their space, 30.6 points,
	// fit, and the third goes to the next row: measured with the spaces as
	// none, all three fitted, 47.2 points drawn past the frame
	narrow := NewTextFrameFromParagraphs([]*Paragraph{NewParagraph().Add(NewTextLine(font, "कख गघ ङच"))})
	narrow.SetWidth(43)
	narrow.SetLocation(10, 10)
	narrow.SetHeight(500)
	page = NewPage(doc.pdf, letter.Portrait())
	narrow.DrawOn(page)
	rows := map[string]bool{}
	for _, m := range regexp.MustCompile(`q \S+ 0 0 \S+ (\S+) (\S+) cm /Fm`).FindAllStringSubmatch(testContent(page), -1) {
		rows[m[2]] = true
		if testNumber(t, m[1])+7 > 53.01 {
			t.Errorf("a glyph at %s, past the frame:\n%s", m[1], testContent(page))
		}
	}
	if len(rows) != 2 {
		t.Errorf("%d rows, not 2:\n%s", len(rows), testContent(page))
	}
	doc.complete()
}

// A TextBlock or a TextLine given another font, fallback font or language
// shapes its text again: the glyphs of one font are not drawn in another
func TestShapedTextIsShapedAgainInAnotherFont(t *testing.T) {
	doc := testNewDoc()
	regular := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	bold := NewFontFromFile(doc.pdf, testRepoPath(t, testPlexOTF))
	shaper := &testCountingShaper{testLetterShaper: testLetterShaper{regular}}
	block := NewTextBlock(regular, "कख").SetShaper(shaper)
	block.SetWidth(200)
	block.DrawOn(nil)
	block.DrawOn(nil)
	if shaper.calls != 1 {
		t.Fatalf("shaped %d times in one font", shaper.calls)
	}
	block.SetFont(bold)
	block.DrawOn(nil)
	block.SetLanguage("mr")
	block.DrawOn(nil)
	if shaper.calls != 3 {
		t.Errorf("shaped %d times: not again in another font and language", shaper.calls)
	}
	SetDefaultShaper(shaper)
	defer SetDefaultShaper(nil)
	line := NewTextLine(regular, "कख")
	line.GetWidth()
	line.GetWidth()
	line.SetFont(bold)
	line.GetWidth()
	if shaper.calls != 5 {
		t.Errorf("a TextLine shaped %d times in all, not 5", shaper.calls)
	}
}

// A fallback font the text engine cannot read, as a core font, is left out
// of the shaping: Hindi with a Latin core fallback is drawn
func TestAFallbackFontWithoutAFileIsLeftOut(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	shaper := &testCountingShaper{testLetterShaper: testLetterShaper{font}}
	page := NewPage(doc.pdf, letter.Portrait())
	block := NewTextBlock(font, "कख").SetShaper(shaper)
	block.SetFallbackFont(NewCoreFont(doc.pdf, corefont.Helvetica()))
	block.SetWidth(200)
	block.DrawOn(page)
	if len(shaper.files) != 1 || strings.Count(testContent(page), " cm /Fm") != 2 {
		t.Errorf("%d font files, not 1:\n%s", len(shaper.files), testContent(page))
	}
	doc.complete()
}

// In an untagged document, the outlines of shaped text carry its text, as
// marked content of their own, which a reader may copy and search
func TestUntaggedShapedTextCarriesItsText(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
	page := NewPage(doc.pdf, letter.Portrait())
	block := NewTextBlock(font, "कख").SetShaper(&testLetterShaper{font})
	block.SetWidth(200)
	block.DrawOn(page)
	if !strings.Contains(testContent(page), "/ActualText <"+toUTF16Hex("क")+">>> BDC") {
		t.Errorf("no actual text:\n%s", testContent(page))
	}
	doc.complete()
}

// testOneLineShaper shapes the text as one line, each character a glyph 7
// points wide: a word that cannot be broken
type testOneLineShaper struct {
	font *Font
}

func (shaper *testOneLineShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	line := ShapedLine{End: len(request.Text)}
	for _, r := range request.Text {
		line.FontIndexes = append(line.FontIndexes, 0)
		line.Glyphs = append(line.Glyphs, shaper.font.unicodeToGID.get(r))
		line.Positions = append(line.Positions, line.Width, 0)
		line.Width += 7
	}
	return []ShapedLine{line}, nil
}

// A line wider than its block, as a word a shaper cannot break, starts at
// the block's left side when aligned to the right or the center, and
// overflows to the right: it was drawn left of the block (found by
// FuzzTextBlockLayout)
func TestALineWiderThanItsBlockStartsAtItsLeft(t *testing.T) {
	for _, align := range []alignment.Alignment{alignment.Right, alignment.Center} {
		doc := testNewDoc()
		font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexDevanagari))
		page := NewPage(doc.pdf, letter.Portrait())
		block := NewTextBlock(font, "कखगघङच").SetShaper(&testOneLineShaper{font}).SetTextAlignment(align)
		block.SetWidth(31)
		block.SetLocation(20, 20)
		block.DrawOn(page)
		m := regexp.MustCompile(`q \S+ 0 0 \S+ (\S+) \S+ cm /Fm`).FindStringSubmatch(testContent(page))
		if m == nil || testNumber(t, m[1]) != 20 {
			t.Errorf("%v: not at the block's left side:\n%s", align, testContent(page))
		}
		doc.complete()
	}
}
