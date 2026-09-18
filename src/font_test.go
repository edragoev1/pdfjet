// font_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/cjkfont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestFontCoreFontWidthsComeFromTheAfmMetrics(t *testing.T) {
	font := testHelvetica(testNewPDF())
	if font.GetName() != "Helvetica" {
		t.Errorf("name %q", font.GetName())
	}
	testNear(t, "size", 12, font.GetSize(), 0)
	// H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
	testNear(t, "Hello at 12", 27.336, font.StringWidth(12, "Hello"), 0.001)
	font.SetSize(24)
	testNear(t, "Hello at 24", 54.672, font.StringWidth(font.GetSize(), "Hello"), 0.001)
}

func TestFontTheWidthOfNoTextIsZero(t *testing.T) {
	// A Go string cannot be nil; the empty string is no text.
	testNear(t, "width", 0, testHelvetica(testNewPDF()).StringWidth(12, ""), 0)
}

func TestFontKerningPairsNarrowTheText(t *testing.T) {
	font := testHelvetica(testNewPDF())
	testNear(t, "without kerning", 16.008, font.StringWidth(12, "AV"), 0.001)
	font.SetKernPairs(true)
	// KPX A V -70
	testNear(t, "with kerning", 15.168, font.StringWidth(12, "AV"), 0.001)
}

func TestFontCoreFontVerticalMetrics(t *testing.T) {
	font := testHelvetica(testNewPDF())
	testNear(t, "ascent", 11.172, font.GetAscent(12), 0.001)
	testNear(t, "descent", 2.7, font.GetDescent(12), 0.001)
	testNear(t, "body height", 13.872, font.GetBodyHeight(12), 0.001)
}

func TestFontGetFitCharsCountsTheCharactersThatFit(t *testing.T) {
	if got := testHelvetica(testNewPDF()).GetFitChars("Hello world", 30); got != 5 {
		t.Errorf("got %d", got)
	}
}

func TestFontEveryCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce(t *testing.T) {
	font := NewCJKFont(testNewPDF(), cjkfont.AdobeMingStdLight)
	testNear(t, "two characters", 20, font.StringWidth(10, "日本"), 0)
	testNear(t, "one supplementary character", 10, font.StringWidth(10, "𠀋"), 0)
}

func TestFontReadsAStreamFont(t *testing.T) {
	path := testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
	file, err := os.Open(path)
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	defer file.Close()
	font := NewFont(testNewPDF(), file)
	if font.GetName() != "IBMPlexSans" {
		t.Errorf("name %q", font.GetName())
	}
	testNear(t, "Hello at 12", 28.32, font.StringWidth(12, "Hello"), 0.001)
}

// testEmbeddedFontFile returns the embedded font file of a PDF that draws a
// line of text in the font.
func testEmbeddedFontFile(t *testing.T, fontStream []byte) []byte {
	t.Helper()
	doc := testNewDoc()
	font := NewFont(doc.pdf, bytes.NewReader(fontStream))
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, testLetterPortrait()))
	for _, obj := range testRead(t, doc.complete()) {
		if obj.GetValue("/Subtype") == "/CIDFontType0C" {
			return obj.GetData()
		}
	}
	return nil
}

func TestFontAnOpenTypeStreamFontKeepsItsOtherTablesAndEmbedsOnlyItsCFFData(t *testing.T) {
	whole, err := os.ReadFile(testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	// The name, the info and the metrics come first, then 'R' with the
	// length of the other tables of the font, and then the CFF data.
	i := 1 + int(whole[0])
	i += 3 + (int(whole[i])<<16 | int(whole[i+1])<<8 | int(whole[i+2]))
	i += 4 + int(binary.BigEndian.Uint32(whole[i:]))
	if whole[i] != 'R' {
		t.Fatalf("flag %q", whole[i])
	}
	length := int(binary.BigEndian.Uint32(whole[i+1:]))
	// The same stream without the tables, as streams were written before.
	cffOnly := append(append([]byte{}, whole[:i]...), whole[i+5+length:]...)
	if cffOnly[i] != 'Y' {
		t.Fatalf("flag %q", cffOnly[i])
	}

	embedded := testEmbeddedFontFile(t, whole)
	if len(embedded) == 0 {
		t.Fatal("no embedded font file")
	}
	if !bytes.Equal(embedded, testEmbeddedFontFile(t, cffOnly)) {
		t.Error("the embedded font files differ")
	}
}

// testThaiContent returns the content of a page with a line of Thai in the
// font: po pla, the upper vowel sara ii on it and the tone mark mai ek above
// the vowel.
func testThaiContent(t *testing.T, path string) string {
	t.Helper()
	file, err := os.Open(testRepoPath(t, path))
	if err != nil {
		t.Skip("the fonts directory is not here")
	}
	defer file.Close()
	pdf := testNewPDF()
	font := NewFont(pdf, file)
	page := NewPage(pdf, testLetterPortrait())
	NewTextLine(font, "\u0E1B\u0E35\u0E48").SetLocation(50, 50).DrawOn(page)
	return testContent(page)
}

func TestFontAStreamFontPlacesTheMarksAsTheOpenTypeFontDoes(t *testing.T) {
	otf := testThaiContent(t, "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf")
	if stream := testThaiContent(t, "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf.stream"); stream != otf {
		t.Errorf("stream %q\notf %q", stream, otf)
	}
}

func TestFontTheLineGapOfAFontSpacesTheLinesOfATextBlock(t *testing.T) {
	pdf := testNewPDF()
	open := func(path string) *Font {
		file, err := os.Open(testRepoPath(t, path))
		if err != nil {
			t.Skip("the fonts directory is not here")
		}
		defer file.Close()
		return NewFont(pdf, file)
	}
	jp := open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream")
	testNear(t, "the line gap of the stream", 10, jp.GetLineGap(10), 0.001)
	testNear(t, "the line gap of the .otf", 10, open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf").GetLineGap(10), 0.001)
	testNear(t, "the line gap of IBM Plex Sans", 0, open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream").GetLineGap(10), 0)
	// The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
	jp.SetSize(10)
	block := NewTextBlock(jp, "日本\n日本")
	block.SetLocation(0, 0)
	testAssertXY(t, 500, 40, block.DrawOn(nil))
}

func TestFontACoreFontDrawsTheWinAnsiCharactersFrom128To159(t *testing.T) {
	pdf := testNewPDF()
	font := testHelvetica(pdf)
	// ’ is 146 in WinAnsi and 222 units wide, where a space is 278.
	testNear(t, "the width of ’", 2.22, font.StringWidth(10, "\u2019"), 0.001)
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(font, "Don\u2019t \u20ac5 \u2014 \u201cHi\u201d").SetLocation(10, 20).DrawOn(page)
	if content := strings.ToLower(testContent(page)); !strings.Contains(content, "<446f6e927420803520972093486994>") {
		t.Errorf("the text is not in WinAnsi: %q", content)
	}
}

func TestFontASoftHyphenIsAHyphenAndANoBreakSpaceIsASpace(t *testing.T) {
	font := testHelvetica(testNewPDF())
	font.SetKernPairs(true)
	testNear(t, "a soft hyphen", font.StringWidth(10, "T-"), font.StringWidth(10, "T\u00ad"), 0)
	testNear(t, "a no-break space", font.StringWidth(10, ". "), font.StringWidth(10, ".\u00a0"), 0)
	// KPX period space -60
	testNear(t, "a kerned no-break space", 4.96, font.StringWidth(10, ".\u00a0"), 0.001)
}

func TestFontAFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor(t *testing.T) {
	pdf := testNewPDF()
	open := func(path string) *Font {
		file, err := os.Open(testRepoPath(t, path))
		if err != nil {
			t.Skip("the fonts directory is not here")
		}
		defer file.Close()
		return NewFont(pdf, file)
	}
	latin := open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
	jp := open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream")
	helvetica := testHelvetica(pdf)
	// The Latin letters after the Japanese ones are in the font again.
	testNear(t, "Latin after Japanese", latin.StringWidth(10, "abc")+jp.StringWidth(10, "\u65e5\u672c")+latin.StringWidth(10, "def"),
		latin.StringWidthUsingFallbackFont(jp, 10, "abc\u65e5\u672cdef"), 0.001)
	// A core font has a fallback font too.
	testNear(t, "a core font", helvetica.StringWidth(10, "Tokyo ")+jp.StringWidth(10, "\u6771\u4eac"),
		helvetica.StringWidthUsingFallbackFont(jp, 10, "Tokyo \u6771\u4eac"), 0.001)
	// A character that neither font has stays in the font.
	testNear(t, "a character neither font has", latin.StringWidth(10, "x\u0e01y"),
		latin.StringWidthUsingFallbackFont(jp, 10, "x\u0e01y"), 0)
	// A combining mark stays with the character before it, in one run of one font.
	page := NewPage(pdf, letter.Portrait())
	NewTextLine(latin, "\u65e5\u0301").SetFallbackFont(jp).SetLocation(10, 20).DrawOn(page)
	if n := strings.Count(testContent(page), " Tf\n"); n != 1 {
		t.Errorf("%d fonts: %q", n, testContent(page))
	}
}
