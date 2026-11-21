// complextext_test.go
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

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// ComplexText: a shaper wraps and shapes the text, and the glyphs it found
// are drawn with DrawGlyphs, each line tagged with its text. Go has no
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
}

func (shaper *testFixedShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	shaper.files = request.FontFiles
	shaper.size = request.FontSize
	shaper.width = request.Width
	shaper.language = request.Language
	return shaper.lines, nil
}

func TestComplexTextTheShaperGivesTheGlyphsAndTheirPlaces(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	fallback := NewFontFromFile(doc.pdf, testRepoPath(t, testCascadia))
	a := font.unicodeToGID.get('A')
	b := fallback.unicodeToGID.get('B')
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 2, FontIndexes: []int{0, 1}, Glyphs: []int{a, b}, Positions: []float32{0, 0, 7, 0}, Width: 13},
		// A glyph of a font that was not given is not drawn
		{Start: 2, End: 3, FontIndexes: []int{-1}, Glyphs: []int{5}, Positions: []float32{0, 0}},
	}}
	page := NewPage(doc.pdf, letter.Portrait())
	corner := NewComplexText(font, "ABC").
		SetFallbackFont(fallback).
		SetLanguage("en").
		SetShaper(shaper).
		SetWidth(120).
		SetLocation(50, 100).
		DrawOn(page)
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
	number := func(s string) float32 {
		v, err := strconv.ParseFloat(s, 32)
		if err != nil {
			t.Fatal(err)
		}
		return float32(v)
	}
	testNear(t, "Plex's scale, 1,000 units an em", 0.01, number(matches[0][1]), 1e-6)
	testNear(t, "Cascadia's scale, 2,048", 10.0/2048, number(matches[1][1]), 1e-6)
	testNear(t, "A's x", 50, number(matches[0][2]), 0.001)
	testNear(t, "B's x", 57, number(matches[1][2]), 0.001)
	testNear(t, "A's y", y, number(matches[0][3]), 0.006)
	testNear(t, "B's y", y, number(matches[1][3]), 0.006)
	doc.complete()
}

func TestComplexTextATaggedTextIsAParagraphOfLinesWithTheirText(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	a := font.unicodeToGID.get('A')
	b := font.unicodeToGID.get('B')
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 2, FontIndexes: []int{0, 0}, Glyphs: []int{a, a}, Positions: []float32{0, 0, 7, 0}},
		{Start: 2, End: 4, FontIndexes: []int{0, 0}, Glyphs: []int{b, b}, Positions: []float32{0, 0, 7, 0}},
	}}
	page := NewPage(doc.pdf, letter.Portrait())
	NewComplexText(font, "AABB").SetLanguage("hi").SetShaper(shaper).SetLocation(50, 100).DrawOn(page)
	content := testContent(page)
	if strings.Count(content, "/P <</MCID 0>>\nBDC\n") != 1 {
		t.Error(content)
	}
	for _, line := range []string{"AA", "BB"} {
		if !strings.Contains(content, "/Span <</Lang <6869> /ActualText <"+toUTF16Hex(line)+">>> BDC\n") {
			t.Errorf("the line %s: %s", line, content)
		}
	}
	if strings.Contains(content, "BT") {
		t.Error(content)
	}
	doc.complete()
}

func TestComplexTextTheFontFileIsWhatTheFontWasLoadedFrom(t *testing.T) {
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
	NewComplexText(fromBytes, "x").SetFallbackFont(fromPath).SetShaper(shaper).SetLocation(0, 0).DrawOn(page)
	if &shaper.files[0][0] != &data[0] || !bytes.Equal(shaper.files[1], data) {
		t.Error("the font files are not those the fonts were loaded from")
	}
	// A TrueType font keeps its whole file, from a reader too
	NewComplexText(trueType, "x").SetShaper(shaper).SetLocation(0, 0).DrawOn(page)
	if !bytes.Equal(shaper.files[0], cascadia) {
		t.Error("the TrueType font's file")
	}
}

func TestComplexTextWithoutAShaperOrAFontFileItSaysSo(t *testing.T) {
	doc := testNewDoc()
	fromPath := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	if _, err := NewComplexText(fromPath, "x").GetTextHeight(); err == nil || !strings.Contains(err.Error(), "SetShaper") {
		t.Errorf("no shaper: %v", err)
	}
	otf, err := os.ReadFile(testRepoPath(t, testComplexLatin))
	if err != nil {
		t.Fatal(err)
	}
	fromReader := NewFont(doc.pdf, bytes.NewReader(otf))
	_, err = NewComplexText(fromReader, "x").SetShaper(&testFixedShaper{}).GetTextHeight()
	if err == nil || !strings.Contains(err.Error(), "font files") {
		t.Errorf("an .otf from a reader: %v", err)
	}
	// Drawn, it fails the document
	page := NewPage(doc.pdf, letter.Portrait())
	NewComplexText(fromReader, "x").SetShaper(&testFixedShaper{}).SetLocation(0, 0).DrawOn(page)
	if doc.pdf.Complete() == nil {
		t.Error("the document did not fail")
	}
}

// testAligned draws two lines: "A A B " of 30 points without its last space,
// then "B", and returns the x of each glyph drawn.
func testAligned(t *testing.T, textAlignment alignment.Alignment) []float32 {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	a := font.unicodeToGID.get('A')
	b := font.unicodeToGID.get('B')
	space := font.unicodeToGID.get(' ')
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 6, FontIndexes: []int{0, 0, 0, 0, 0, 0}, Glyphs: []int{a, space, a, space, b, space},
			Positions: []float32{0, 0, 10, 0, 12, 0, 22, 0, 24, 0, 30, 0}, Width: 30},
		{Start: 6, End: 7, FontIndexes: []int{0}, Glyphs: []int{b}, Positions: []float32{0, 0}, Width: 6},
	}}
	page := NewPage(doc.pdf, letter.Portrait())
	NewComplexText(font, "A A B B").SetShaper(shaper).SetTextAlignment(textAlignment).
		SetWidth(100).SetLocation(50, 100).DrawOn(page)
	var xs []float32
	for _, m := range regexp.MustCompile(`q \S+ 0 0 \S+ (\S+) \S+ cm /Fm`).FindAllStringSubmatch(testContent(page), -1) {
		v, err := strconv.ParseFloat(m[1], 32)
		if err != nil {
			t.Fatal(err)
		}
		xs = append(xs, float32(v))
	}
	return xs
}

func TestComplexTextTheLinesAreAligned(t *testing.T) {
	// The glyphs drawn: A, A, B of the first line, B of the second; justified,
	// the 70 points left over go to the two spaces between the words, and the
	// last line is left as it is
	for _, c := range []struct {
		alignment alignment.Alignment
		want      []float32
	}{
		{alignment.Left, []float32{50, 62, 74, 50}},
		{alignment.Center, []float32{85, 97, 109, 97}},
		{alignment.Right, []float32{120, 132, 144, 144}},
		{alignment.Justify, []float32{50, 97, 144, 50}},
	} {
		if got := testAligned(t, c.alignment); !reflect.DeepEqual(got, c.want) {
			t.Errorf("alignment %d: %v, not %v", c.alignment, got, c.want)
		}
	}
}

func TestComplexTextTheLineSpacingIsAMultipleOfTheBodyHeightAsTextBlocks(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	a := font.unicodeToGID.get('A')
	shaper := &testFixedShaper{lines: []ShapedLine{
		{Start: 0, End: 1, FontIndexes: []int{0}, Glyphs: []int{a}, Positions: []float32{0, 0}, Width: 7},
		{Start: 1, End: 2, FontIndexes: []int{0}, Glyphs: []int{a}, Positions: []float32{0, 0}, Width: 7},
	}}
	text := NewComplexText(font, "AA").SetShaper(shaper)
	body := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	height, _ := text.GetTextHeight()
	testNear(t, "at 1.0", 2*body, height, 0.001)
	height, _ = text.SetLineSpacing(1.5).GetTextHeight()
	testNear(t, "at 1.5", 3*body, height, 0.001)
}

// testLetterShaper gives each paragraph as many lines as its letters, one
// glyph of the letter each.
type testLetterShaper struct {
	font *Font
}

func (shaper *testLetterShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	var lines []ShapedLine
	for i, r := range request.Text {
		lines = append(lines, ShapedLine{Start: i, End: i + 1, FontIndexes: []int{0},
			Glyphs: []int{shaper.font.unicodeToGID.get(r)}, Positions: []float32{0, 0}, Width: 7})
	}
	return lines, nil
}

func TestComplexTextParagraphsFlowFromPageToPageCutWhereTheHeightEnds(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	line := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	text := NewComplexTextFromList(font, []string{"AAA", "BBB"}).
		SetShaper(&testLetterShaper{font}).
		SetLanguage("en").
		SetParagraphGap(5).
		SetHeight(4*line + 5)
	height, _ := text.GetTextHeight()
	testNear(t, "the height of all", 6*line+5, height, 0.001)
	// Three lines of the first paragraph, the gap, one of the second
	first := NewPage(doc.pdf, letter.Portrait())
	corner := text.SetLocation(50, 100).DrawOn(first)
	testNear(t, "the bottom on the first page", 100+4*line+5, corner[1], 0.001)
	if more, _ := text.HasMoreText(); !more {
		t.Error("no more text after the first page")
	}
	height, _ = text.GetTextHeight()
	testNear(t, "the height left", 2*line, height, 0.001)
	content := testContent(first)
	if strings.Count(content, " cm /Fm") != 4 || strings.Count(content, "/P <<") != 2 {
		t.Error(content)
	}
	// The rest of the second paragraph, from the top of the next page
	second := NewPage(doc.pdf, letter.Portrait())
	corner = text.SetLocation(50, 50).DrawOn(second)
	testNear(t, "the bottom on the second page", 50+2*line, corner[1], 0.001)
	if more, _ := text.HasMoreText(); more {
		t.Error("more text after the second page")
	}
	if strings.Count(testContent(second), " cm /Fm") != 2 {
		t.Error(testContent(second))
	}
	doc.complete()
}

func TestComplexTextAHeightTooSmallForALineStillDrawsOneEachTime(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	text := NewComplexText(font, "AB").SetShaper(&testLetterShaper{font}).SetHeight(1)
	pages := 0
	for more, _ := text.HasMoreText(); more; more, _ = text.HasMoreText() {
		text.SetLocation(0, 0).DrawOn(NewPage(doc.pdf, letter.Portrait()))
		pages++
	}
	if pages != 2 {
		t.Errorf("%d pages", pages)
	}
}

func TestComplexTextGetHeightIsTheHeightSetAndTheGapIsTextFramesEmptyLine(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	text := NewComplexTextFromList(font, []string{"A", "B"}).SetShaper(&testLetterShaper{font})
	if text.GetHeight() != 0 || text.SetHeight(500).GetHeight() != 500 {
		t.Error("GetHeight is not the height set")
	}
	// Two lines, one empty line between them, as a TextFrame's default
	line := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	gap := font.GetAscent(10) + font.GetDescent(10) + float32(2*font.GetLineGap(10))
	height, _ := text.GetTextHeight()
	testNear(t, "two lines and the gap", float32(2*line)+gap, height, 0.001)
}

// Without a page, DrawOn measures: the corner DrawOn would return, the text
// left as it was, so that a Table cell can measure it.
func TestComplexTextDrawOnWithoutAPageMeasuresAndKeepsTheText(t *testing.T) {
	doc := testNewDoc()
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	line := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	text := NewComplexText(font, "ABCD").SetShaper(&testLetterShaper{font}).SetWidth(150)
	text.SetLocation(50, 100)
	first := text.DrawOn(nil)
	second := text.DrawOn(nil)
	testNear(t, "the right edge", 200, first[0], 0.001)
	testNear(t, "the bottom", 100+4*line, first[1], 0.001)
	if first != second {
		t.Errorf("measured twice: %v, then %v", first, second)
	}
	if more, _ := text.HasMoreText(); !more {
		t.Error("the text used up by measuring it")
	}
	// With a height, the lines that fit, as DrawOn draws them
	text.SetHeight(2.5 * line)
	testNear(t, "the lines that fit", 100+2*line, text.DrawOn(nil)[1], 0.001)
	page := NewPage(doc.pdf, letter.Portrait())
	testNear(t, "the lines drawn", 100+2*line, text.DrawOn(page)[1], 0.001)
	doc.complete()
}

// Text such as Hindi in Table cells: each cell as tall as its text wrapped
// to the column, the text drawn whole each time, as the header row is on
// each page; the caller's text where it was.
func TestComplexTextInTableCellsWrapsAndRepeatsTheHeader(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	font := NewFontFromFile(doc.pdf, testRepoPath(t, testComplexLatin))
	font.SetSize(10)
	line := font.GetAscent(10) + font.GetDescent(10) + font.GetLineGap(10)
	shaper := &testLetterShaper{font}
	data := [][]*Cell{{
		NewCell(font, "").SetDrawable(NewComplexText(font, "X").SetShaper(shaper).SetLanguage("hi")),
		NewCell(font, "").SetDrawable(NewComplexText(font, "Z").SetShaper(shaper).SetLanguage("hi")),
	}}
	kept := NewComplexText(font, "ABC").SetShaper(shaper).SetLanguage("hi")
	kept.SetLocation(7, 9)
	for r := 0; r < 40; r++ {
		text := kept
		if r > 0 {
			text = NewComplexText(font, "ABC").SetShaper(shaper).SetLanguage("hi")
		}
		data = append(data, []*Cell{NewCell(font, "Row "+strconv.Itoa(r+1)), NewCell(font, "").SetDrawable(text)})
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
	// The header row: its words as text on the first page; on the next pages
	// drawn again, as an artifact, as PDF/UA has a repeated header
	first := testContent(pages[0])
	for _, word := range []string{"X", "Z"} {
		if strings.Count(first, "/ActualText <"+toUTF16Hex(word)+">") != 1 {
			t.Errorf("the header's %s on the first page:\n%s", word, first)
		}
	}
	for i := 1; i < len(pages); i++ {
		content := testContent(pages[i])
		start := strings.Index(content, "/Artifact BMC\n")
		if start < 0 {
			t.Fatalf("page %d has no header", i+1)
		}
		drawn := content[start : start+strings.Index(content[start:], "EMC\n")]
		if strings.Count(drawn, " cm /Fm") < 2 {
			t.Errorf("page %d: its header's glyphs not drawn:\n%s", i+1, drawn)
		}
	}
	// The caller's text kept where it was, and whole
	corner := kept.DrawOn(nil)
	testNear(t, "the caller's x", 7+kept.width, corner[0], 0.001)
	testNear(t, "the caller's y", 9+3*line, corner[1], 0.001)
}
