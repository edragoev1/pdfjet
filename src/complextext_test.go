// complextext_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// ComplexText: a shaper wraps and shapes the text, and the glyphs it found
// are drawn with DrawGlyphs, each line tagged with its text. Go has no
// shaper of its own: the tests give one that returns fixed lines.

const testComplexDevanagari = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
const testComplexLatin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

// testFixedShaper gives the lines it was made with, and keeps what it was given.
type testFixedShaper struct {
	lines    []ShapedLine
	files    [][]byte
	size     float32
	width    float32
	language string
}

func (shaper *testFixedShaper) Shape(text, language string, fontFiles [][]byte, fontSize, width float32) (
	[]ShapedLine, error) {
	shaper.files = fontFiles
	shaper.size = fontSize
	shaper.width = width
	shaper.language = language
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
		{Start: 0, End: 2, Fonts: []int{0, 1}, Glyphs: []int{a, b}, Positions: []float32{0, 0, 7, 0}},
		// A glyph of a font that was not given is not drawn
		{Start: 2, End: 3, Fonts: []int{-1}, Glyphs: []int{5}, Positions: []float32{0, 0}},
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
	lineHeight := font.GetAscent(10) + font.GetDescent(10) + 2.5
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
		{Start: 0, End: 2, Fonts: []int{0, 0}, Glyphs: []int{a, a}, Positions: []float32{0, 0, 7, 0}},
		{Start: 2, End: 4, Fonts: []int{0, 0}, Glyphs: []int{b, b}, Positions: []float32{0, 0, 7, 0}},
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
	if _, err := NewComplexText(fromPath, "x").GetHeight(); err == nil || !strings.Contains(err.Error(), "SetShaper") {
		t.Errorf("no shaper: %v", err)
	}
	otf, err := os.ReadFile(testRepoPath(t, testComplexLatin))
	if err != nil {
		t.Fatal(err)
	}
	fromReader := NewFont(doc.pdf, bytes.NewReader(otf))
	_, err = NewComplexText(fromReader, "x").SetShaper(&testFixedShaper{}).GetHeight()
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
