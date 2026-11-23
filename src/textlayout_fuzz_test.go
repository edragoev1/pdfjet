// textlayout_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"os"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
	"testing"
	"unicode"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzzing of the text layout of 9.2.1: TextBlock, TextLine and TextFrame
// with text of every script they draw, shaped or not, and the dictionary
// word breaks of Thai, Lao, Khmer and Burmese. The text is made of pieces
// picked by the fuzzer's bytes, so that it mixes the scripts as documents
// do, and, when the first byte says so, is the fuzzer's own bytes.
//
//	go test -run '^$' -fuzz FuzzTextBlockLayout -fuzztime 10m .

// The pieces a fuzzed text is made of
var fuzzTextPieces = []string{
	"Hello", "PDFjet", "2026", "WhatsApp", " ", " ", "  ", "\n", "\r\n",
	"नमस्ते", "हिन्दी", "क्षत्रिय", "्", "ि", // Hindi, a virama and a vowel sign alone
	"ភាសាខ្មែរ", "ព្រះ", "ៀ", // Khmer, a vowel alone
	"ภาษาไทย", "สวัสดีครับ", "ั", // Thai, a mark alone
	"ພາສາລາວ", "မြန်မာ", // Lao, Burmese
	"مرحبا", "سلام", "שלום", "١٢٣", // Arabic, Persian, Hebrew, Arabic digits
	"中文", "日本語の文章。", "「引用」", "한국어", // Chinese, Japanese, Korean
	"\u200B", "\u200C", "\u200D", "\uFEFF", "\u00AD", // zero widths, a BOM, a soft hyphen
	"😀", "e\u0301", "ﷺ", // an emoji, a combining accent, an Arabic ligature
}

// fuzzText makes the text of the fuzzer's bytes: the pieces they pick, or,
// when the first byte is odd and above 240, the bytes themselves.
func fuzzText(data []byte) string {
	if len(data) > 0 && data[0] > 240 && data[0]%2 == 1 {
		return string(data[1:])
	}
	var b strings.Builder
	for _, c := range data {
		b.WriteString(fuzzTextPieces[int(c)%len(fuzzTextPieces)])
	}
	return b.String()
}

// The font files read once: Noto Sans Devanagari, for the text, and IBM
// Plex Sans, its fallback
var fuzzFontFiles = map[string][]byte{}

func fuzzFont(t *testing.T, pdf *PDF, path string) *Font {
	data, ok := fuzzFontFiles[path]
	if !ok {
		var err error
		data, err = os.ReadFile(filepath.Join("..", path))
		if err != nil {
			t.Fatal(err)
		}
		fuzzFontFiles[path] = data
	}
	return NewFontFromBytes(pdf, data)
}

// fuzzShaper shapes as a real shaper does, roughly: each character a glyph
// of the font, or of the fallback when the font has none, as wide as that
// font has it; the spaces at the end of a line no width; lines broken at
// spaces to the width
type fuzzShaper struct {
	font, fallback *Font
}

func (shaper *fuzzShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	var lines []ShapedLine
	line := ShapedLine{Start: 0}
	var x, spaces float32
	space := shaper.font.StringWidth(request.FontSize, " ")
	for i, r := range request.Text {
		// The bytes of the character in the text: 1 for a byte that is not
		// UTF-8, which range reads as U+FFFD, of 3
		_, size := utf8.DecodeRuneInString(request.Text[i:])
		if r == ' ' {
			spaces += space
			line.End = i + 1
			continue
		}
		index, font := 0, shaper.font
		if font.unicodeToGID.get(r) == 0 && shaper.fallback != nil && len(request.FontFiles) > 1 &&
			shaper.fallback.unicodeToGID.get(r) != 0 {
			index, font = 1, shaper.fallback
		}
		advance := font.StringWidth(request.FontSize, string(r))
		if x+spaces+advance > request.Width && len(line.Glyphs) > 0 && spaces > 0 {
			lines = append(lines, line)
			line = ShapedLine{Start: i}
			x, spaces = 0, 0
		}
		x += spaces
		spaces = 0
		line.FontIndexes = append(line.FontIndexes, index)
		line.Glyphs = append(line.Glyphs, font.unicodeToGID.get(r))
		line.Positions = append(line.Positions, x, 0)
		x += advance
		line.Width = x
		line.End = i + size
	}
	return append(lines, line), nil
}

// fuzzAdvance is the width the fuzzShaper gives the character: in the font,
// or in the fallback when the font has none and the fallback has it.
func fuzzAdvance(font, fallback *Font, size float32, r rune) float32 {
	if font.unicodeToGID.get(r) == 0 && fallback.unicodeToGID.get(r) != 0 {
		font = fallback
	}
	return font.StringWidth(size, string(r))
}

// fuzzWidestCharacter returns the width of the widest part of the text that
// a TextFrame cannot cut, a letter with the marks that stay with it, as
// nextCharacterBreakIn finds them: what may overflow a frame narrower than it.
func fuzzWidestCharacter(text string, font, fallback *Font, size float32) float32 {
	var widest float32
	for i := 0; i < len(text); {
		next := nextCharacterBreakIn(text, i)
		var w float32
		for _, r := range text[i:next] {
			w += fuzzAdvance(font, fallback, size, r)
		}
		widest = max(widest, w)
		i = next
	}
	return widest
}

// The places of what a page draws: the x of each text line and each glyph
var fuzzPlaces = regexp.MustCompile(`(?m)^1 0 0 1 (\S+) \S+ Tm$|q \S+ \S+ \S+ \S+ (\S+) \S+ cm /Fm`)

// fuzzCheckPlaces fails when the page draws text or a glyph to the left of
// the block, or past its right side by more than the slack: the widest
// letter with its marks, in a TextFrame, which breaks a word too long for
// it; anything in a TextBlock, where a word a shaper cannot break overflows
// to the right
func fuzzCheckPlaces(t *testing.T, content string, left, right, slack float32) {
	t.Helper()
	for _, m := range fuzzPlaces.FindAllStringSubmatch(content, -1) {
		v := m[1] + m[2]
		x, err := strconv.ParseFloat(v, 64)
		if err != nil || math.IsNaN(x) {
			t.Fatalf("a place of %q:\n%s", v, content)
		}
		if float32(x) < left-0.01 || float32(x) > right+slack {
			t.Fatalf("drawn at %v, outside %v to %v:\n%s", x, left, right, content)
		}
	}
}

// A TextBlock of any text, any width, size and alignment: measured and drawn
// to the same corner, every line and glyph within the block, the document
// complete with no error, as the fonts have files and the shaper answers.
func FuzzTextBlockLayout(f *testing.F) {
	f.Add([]byte{0, 4, 9, 4, 20, 4, 24, 4, 31}, uint16(200), uint8(12), uint8(0))
	f.Add([]byte{9, 7, 9, 7, 9, 7, 9, 7, 9}, uint16(30), uint8(10), uint8(2))
	f.Add([]byte{24, 4, 25, 8, 9}, uint16(100), uint8(14), uint8(1))
	f.Add([]byte{30, 31, 32, 3, 33}, uint16(1), uint8(40), uint8(3))
	f.Fuzz(func(t *testing.T, data []byte, width uint16, size uint8, align uint8) {
		if len(data) > 400 {
			return
		}
		text := fuzzText(data)
		doc := testNewDoc()
		font := fuzzFont(t, doc.pdf, testComplexDevanagari)
		fallback := fuzzFont(t, doc.pdf, testPlexOTF)
		w := float32(width%600) + 1
		block := NewTextBlock(font, text).SetFallbackFont(fallback).SetFontSize(float32(size%48) + 1).
			SetShaper(&fuzzShaper{font, fallback}).
			SetTextAlignment([]alignment.Alignment{alignment.Left, alignment.Center, alignment.Right, alignment.Left}[align%4])
		block.SetWidth(w)
		block.SetLocation(20, 20)
		measured := block.DrawOn(nil)
		page := NewPage(doc.pdf, letter.Portrait())
		drawn := block.DrawOn(page)
		if measured != drawn {
			t.Fatalf("measured %v, drawn %v", measured, drawn)
		}
		if doc.pdf.err != nil {
			t.Fatalf("the document failed: %v", doc.pdf.err)
		}
		fuzzCheckPlaces(t, testContent(page), 20, 20+w, float32(math.Inf(1)))
		doc.complete()
	})
}

// A TextFrame of paragraphs of any text, drawn frame after frame: it ends,
// within a page for each character, as a frame holds at least one line and
// a line at least one character, every word within the frame, the document
// complete with no error.
func FuzzTextFrameLayout(f *testing.F) {
	f.Add([]byte{0, 4, 9, 4, 20, 4, 24, 4, 31}, uint16(200), uint8(12))
	f.Add([]byte{9, 4, 9, 4, 9, 4, 9, 4, 9, 4, 9}, uint16(40), uint8(10))
	f.Add([]byte{0, 7, 9, 7, 24, 8, 30}, uint16(1), uint8(30))
	f.Fuzz(func(t *testing.T, data []byte, width uint16, size uint8) {
		if len(data) > 300 {
			return
		}
		doc := testNewDoc()
		font := fuzzFont(t, doc.pdf, testComplexDevanagari)
		fallback := fuzzFont(t, doc.pdf, testPlexOTF)
		font.SetSize(float32(size%36) + 1)
		fallback.SetSize(font.size)
		SetDefaultShaper(&fuzzShaper{font, fallback})
		defer SetDefaultShaper(nil)
		var paragraphs []*Paragraph
		text := fuzzText(data)
		slack := max(1.5*font.size, fuzzWidestCharacter(text, font, fallback, font.size)+0.01)
		for _, part := range strings.Split(text, "\n") {
			line := NewTextLine(font, part)
			line.SetFallbackFont(fallback)
			paragraphs = append(paragraphs, NewParagraph().Add(line))
		}
		w := float32(width%500) + 1
		frame := NewTextFrameFromParagraphs(paragraphs).SetWidth(w)
		frame.SetHeight(300)
		// A frame of a point wide at size 36 holds 6 lines of one character
		// each, fewer than the 8 of the piece "WhatsApp": a page for each byte
		// of data ended a frame that ends (fuzzing, 23 November 2026)
		limit := 2 + utf8.RuneCountInString(text)
		for pages := 0; ; pages++ {
			if pages > limit {
				t.Fatalf("%d pages and more text: the frame does not end", pages)
			}
			page := NewPage(doc.pdf, letter.Portrait())
			frame.SetLocation(20, 20)
			frame.DrawOn(page)
			if doc.pdf.err != nil {
				t.Fatalf("the document failed: %v", doc.pdf.err)
			}
			fuzzCheckPlaces(t, testContent(page), 20, 20+w, slack)
			if !frame.HasMoreText() {
				break
			}
		}
		doc.complete()
	})
}

// The word ends of Thai, Lao, Khmer and Burmese by ICU's dictionaries: in
// order, within the text, never before a mark that stays with the letter
// before it; and the zero width spaces of thaiWordBreaks take nothing out.
func FuzzDictionaryWordEnds(f *testing.F) {
	f.Add("ภาษาไทยเป็นภาษาราชการ")
	f.Add("ភាសាខ្មែរជាភាសាផ្លូវការ")
	f.Add("ພາສາລາວເປັນພາສາ မြန်မာဘာသာ")
	f.Add("ั\u0E31\u0E31ไทย\u200Bไทย")
	f.Fuzz(func(t *testing.T, text string) {
		if len(text) > 2000 || !utf8.ValidString(text) {
			return
		}
		runes := []rune(text)
		last := 0
		for _, end := range dictionaryWordEnds(runes) {
			if end <= last || end > len(runes) {
				t.Fatalf("an end at %d after %d, of %d", end, last, len(runes))
			}
			if end < len(runes) && unicode.Is(unicode.Mn, runes[end]) {
				t.Fatalf("an end before the mark %U, at %d of %q", runes[end], end, text)
			}
			last = end
		}
		broken := thaiWordBreaks(text)
		if strings.ReplaceAll(broken, "\u200B", "") != strings.ReplaceAll(text, "\u200B", "") {
			t.Fatalf("thaiWordBreaks changed the text: %q into %q", text, broken)
		}
	})
}

// A shaper that answers anything: lines whose glyphs, fonts, places and
// parts of the text are the fuzzer's. The document fails, or draws them,
// and nothing panics.
type fuzzWildShaper struct {
	data []byte
}

func (shaper *fuzzWildShaper) Shape(request ShapingRequest) ([]ShapedLine, error) {
	d := shaper.data
	at := 0
	next := func() int {
		if at >= len(d) {
			return 0
		}
		at++
		return int(int8(d[at-1]))
	}
	var lines []ShapedLine
	for n := next() & 3; n >= 0; n-- {
		count := next() & 7
		line := ShapedLine{Start: next(), End: next() * 3, Width: float32(next())}
		for i := 0; i < count; i++ {
			line.FontIndexes = append(line.FontIndexes, next()%3)
			line.Glyphs = append(line.Glyphs, next()*37)
			line.Positions = append(line.Positions, float32(next()), float32(next()))
		}
		if next()%5 == 0 && len(line.Positions) > 0 {
			line.Positions = line.Positions[1:] // One place short
		}
		lines = append(lines, line)
	}
	return lines, nil
}

func FuzzShapedLines(f *testing.F) {
	f.Add([]byte{1, 2, 0, 3, 10, 0, 5, 0, 0, 1, 7, 2, 0})
	f.Add([]byte{3, 7, 255, 100, 200, 9, 9, 9, 9, 9, 9, 9, 9})
	f.Fuzz(func(t *testing.T, data []byte) {
		doc := testNewDoc()
		font := fuzzFont(t, doc.pdf, testComplexDevanagari)
		page := NewPage(doc.pdf, letter.Portrait())
		block := NewTextBlock(font, "नमस्ते दुनिया").SetShaper(&fuzzWildShaper{data})
		block.SetWidth(200)
		block.DrawOn(nil)
		block.DrawOn(page)
		SetDefaultShaper(&fuzzWildShaper{data})
		defer SetDefaultShaper(nil)
		line := NewTextLine(font, "नमस्ते")
		line.GetWidth()
		line.DrawOn(page)
	})
}
