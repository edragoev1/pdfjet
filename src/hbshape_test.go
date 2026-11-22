// hbshape_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/json"
	"os"
	"os/exec"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The tests of HBShape need hb-shape, HarfBuzz's program: skipped without it.
func testHBShape(t *testing.T) *HBShape {
	t.Helper()
	if _, err := exec.LookPath("hb-shape"); err != nil {
		t.Skip("hb-shape is not installed")
	}
	t.Chdir("..")
	return NewHBShape()
}

func testUDHR(t *testing.T, code string) []string {
	t.Helper()
	data, err := os.ReadFile("resources/data/udhr/udhr_" + code + ".txt")
	if err != nil {
		t.Fatal(err)
	}
	return strings.Split(strings.TrimSpace(string(data)), "\n")
}

const (
	testNotoDevanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
	testNotoKhmer      = "resources/fonts/world/NotoSansKhmer/NotoSansKhmer-Regular.otf"
)

// A paragraph of one font is the glyphs hb-shape gives for it, at their
// places: the files, the clusters and the scale as they should be.
func TestHBShapeGivesHBShapesGlyphs(t *testing.T) {
	hb := testHBShape(t)
	version, err := hb.Version()
	if err != nil || !strings.Contains(version, "HarfBuzz") {
		t.Fatalf("%q %v", version, err)
	}
	font, _ := os.ReadFile(testNotoDevanagari)
	paragraph := testUDHR(t, "hin")[2]
	lines, err := hb.Shape(ShapingRequest{Text: paragraph, Language: "hi", FontFiles: [][]byte{font},
		FontSize: 10, Width: 1e6})
	if err != nil || len(lines) != 1 {
		t.Fatalf("%d lines, %v", len(lines), err)
	}
	out, err := exec.Command("hb-shape", "--no-glyph-names", "--output-format=json", "--language=hi",
		testNotoDevanagari, paragraph).Output()
	if err != nil {
		t.Fatal(err)
	}
	var want []hbGlyph
	if err := json.Unmarshal(bytes.TrimSpace(out), &want); err != nil {
		t.Fatal(err)
	}
	line := lines[0]
	if len(line.Glyphs) != len(want) {
		t.Fatalf("%d glyphs, hb-shape %d", len(line.Glyphs), len(want))
	}
	var pen float32
	scale := float32(10) / 1000 // Noto Sans Devanagari has 1,000 units an em
	for i, g := range want {
		x := pen + float32(float32(g.Dx)*scale)
		if line.Glyphs[i] != g.G || line.FontIndexes[i] != 0 ||
			line.Positions[2*i] != x || line.Positions[2*i+1] != -float32(g.Dy)*scale {
			t.Fatalf("glyph %d: %d at %v, hb-shape %+v", i, line.Glyphs[i], line.Positions[2*i:2*i+2], g)
		}
		pen += float32(float32(g.Ax) * scale)
	}
	if line.Start != 0 || line.End != len(paragraph) {
		t.Errorf("the line is %d to %d of %d", line.Start, line.End, len(paragraph))
	}
}

// Khmer, without spaces between its words, breaks at the ends of words, as
// ICU's dictionary has them, every line within the width but for one word
// wider than it; Hindi at its spaces.
func TestHBShapeBreaksBetweenWords(t *testing.T) {
	hb := testHBShape(t)
	for _, c := range []struct{ code, lang, font string }{
		{"khm", "km", testNotoKhmer}, {"hin", "hi", testNotoDevanagari},
	} {
		font, _ := os.ReadFile(c.font)
		breaks := 0
		for _, paragraph := range testUDHR(t, c.code) {
			lines, err := hb.Shape(ShapingRequest{Text: paragraph, Language: c.lang, FontFiles: [][]byte{font},
				FontSize: 12, Width: 300})
			if err != nil {
				t.Fatal(err)
			}
			runes := []rune(paragraph)
			ends := map[int]bool{}
			for _, e := range dictionaryWordEnds(runes) {
				ends[e] = true
			}
			for i, line := range lines {
				if i > 0 && line.Start != lines[i-1].End {
					t.Fatalf("%s: line %d starts at %d, the last ended at %d", c.code, i, line.Start, lines[i-1].End)
				}
				if i == len(lines)-1 {
					if line.End != len(paragraph) {
						t.Fatalf("%s: the text ends at %d of %d", c.code, line.End, len(paragraph))
					}
					continue
				}
				breaks++
				at := len([]rune(paragraph[:line.End])) // The rune the next line starts at
				if runes[at-1] != ' ' && !ends[at] {
					t.Errorf("%s: a line breaks inside a word: %q | %q", c.code,
						string(runes[max(0, at-8):at]), string(runes[at:min(len(runes), at+8)]))
				}
				if line.Width > 300 && strings.ContainsAny(strings.TrimSpace(paragraph[line.Start:line.End]), " ") {
					t.Errorf("%s: a line of %v points", c.code, line.Width)
				}
			}
		}
		if breaks < 50 {
			t.Errorf("%s: %d breaks: too few to tell", c.code, breaks)
		}
	}
}

// TextBlock draws Hindi with an English word, in two fonts, and Khmer,
// shaped by hb-shape, into a PDF that completes.
func TestHBShapeDrawsTextBlocks(t *testing.T) {
	hb := testHBShape(t)
	doc := testNewDoc()
	hindi := NewFontFromFile(doc.pdf, testNotoDevanagari)
	khmer := NewFontFromFile(doc.pdf, testNotoKhmer)
	latin := NewFontFromFile(doc.pdf, "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
	for _, c := range []struct {
		font *Font
		text string
		lang string
	}{
		{hindi, testUDHR(t, "hin")[2] + " PDFjet", "hi"},
		{khmer, testUDHR(t, "khm")[2], "km"},
	} {
		c.font.SetSize(12)
		page := NewPage(doc.pdf, letter.Portrait())
		block := NewTextBlock(c.font, c.text).SetFallbackFont(latin).SetLanguage(c.lang).SetShaper(hb)
		block.SetWidth(300)
		block.SetLocation(50, 50)
		xy := block.DrawOn(page)
		if xy[1] <= 50+3*12 {
			t.Errorf("%s: drawn down to %v", c.lang, xy[1])
		}
		if !strings.Contains(testContent(page), "/Fm") {
			t.Errorf("%s: no glyph drawn", c.lang)
		}
	}
	if len(doc.complete()) < 1000 {
		t.Error("the PDF is empty")
	}
}

// Without a shaper set, TextBlock does not run hb-shape, even where it is
// installed: it fails, saying what to import, so that a program shapes the
// same wherever it runs, whatever is installed there
func TestTextBlockWithoutAShaperRunsNoHBShape(t *testing.T) {
	testHBShape(t)
	doc := testNewDoc()
	khmer := NewFontFromFile(doc.pdf, testNotoKhmer)
	khmer.SetSize(12)
	block := NewTextBlock(khmer, testUDHR(t, "khm")[2]).SetLanguage("km")
	block.SetWidth(300)
	block.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	err := doc.pdf.Complete()
	if err == nil || !strings.Contains(err.Error(), `import _ "github.com/edragoev1/pdfjet/harfbuzz/v9"`) ||
		!strings.Contains(err.Error(), "SetShaper") || !strings.Contains(err.Error(), "NewHBShape") {
		t.Errorf("%v", err)
	}
}

// HBShape set, without hb-shape, the PDF fails, saying what to install, and
// what to import instead
func TestHBShapeWithoutHBShapeSaysWhatToDo(t *testing.T) {
	t.Chdir("..")
	t.Setenv("PATH", t.TempDir())
	doc := testNewDoc()
	khmer := NewFontFromFile(doc.pdf, testNotoKhmer)
	khmer.SetSize(12)
	block := NewTextBlock(khmer, "ភាសាខ្មែរ").SetShaper(NewHBShape())
	block.SetWidth(300)
	block.DrawOn(NewPage(doc.pdf, letter.Portrait()))
	err := doc.pdf.Complete()
	if err == nil || !strings.Contains(err.Error(), "apt install libharfbuzz-bin") ||
		!strings.Contains(err.Error(), `import _ "github.com/edragoev1/pdfjet/harfbuzz/v9"`) {
		t.Errorf("%v", err)
	}
}
