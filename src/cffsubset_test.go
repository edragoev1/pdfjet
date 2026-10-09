// cffsubset_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

const (
	testPlexOTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf"
	testHanOTF  = "fonts/Test/SourceHanSansJP-Regular.otf"
)

// testCFF returns the font and its CFF table.
func testCFF(t testing.TB, path string) (*openTypeFont, []byte) {
	t.Helper()
	data, err := os.ReadFile(testRepoPath(t.(*testing.T), path))
	if err != nil {
		t.Fatal(err)
	}
	otf := newOpenTypeFont(bytes.NewReader(data))
	return otf, otf.buf[otf.cffOff : otf.cffOff+otf.cffLen]
}

// testCFFParts returns the charstrings, the global subroutines and the Top
// DICT of a CFF table.
func testCFFParts(t *testing.T, cff []byte) (charStrings, globalSubrs [][]byte, top []cffEntry) {
	t.Helper()
	names, err := readCFFIndex(cff, int(cff[2]))
	if err != nil {
		t.Fatal(err)
	}
	topDicts, _ := readCFFIndex(cff, names.end)
	stringsIndex, _ := readCFFIndex(cff, topDicts.end)
	globals, err := readCFFIndex(cff, stringsIndex.end)
	if err != nil {
		t.Fatal(err)
	}
	top, err = readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	if err != nil {
		t.Fatal(err)
	}
	index, err := readCFFIndex(cff, cffEntryOf(top, cffCharStrings)[0])
	if err != nil {
		t.Fatal(err)
	}
	return emptiedCFFIndex(cff, index, nil, 0), emptiedCFFIndex(cff, globals, nil, 0), top
}

// testCFFUsed returns the glyphs of the text in the font.
func testCFFUsed(otf *openTypeFont, text string) []bool {
	used := make([]bool, 0x10000)
	for _, ch := range text {
		used[otf.unicodeToGID[ch]] = true
	}
	return used
}

func TestCFFSubsetKeepsTheCharstringsUsedAndEmptiesTheRest(t *testing.T) {
	for _, path := range []string{testPlexOTF, testHanOTF} {
		otf, cff := testCFF(t, path)
		used := testCFFUsed(otf, "Hello 日本語")
		subset, kept, err := subsetCFF(cff, used)
		if err != nil {
			t.Fatal(path, err)
		}
		whole, wholeGlobals, _ := testCFFParts(t, cff)
		glyphs, globals, _ := testCFFParts(t, subset)
		if len(glyphs) != len(whole) || len(kept) != len(whole) {
			t.Fatalf("%s: %d glyphs, not %d", path, len(glyphs), len(whole))
		}
		for gid := range glyphs {
			if want := gid == 0 || used[gid]; kept[gid] != want {
				t.Errorf("%s: glyph %d kept: %v", path, gid, kept[gid])
			}
			if kept[gid] && !bytes.Equal(glyphs[gid], whole[gid]) {
				t.Errorf("%s: glyph %d is not as it was", path, gid)
			}
			if !kept[gid] && !bytes.Equal(glyphs[gid], []byte{14}) {
				t.Errorf("%s: glyph %d is not endchar", path, gid)
			}
		}
		// The global subroutines are kept as they were, or emptied to return.
		emptied := 0
		for i := range globals {
			if bytes.Equal(globals[i], []byte{11}) && !bytes.Equal(wholeGlobals[i], []byte{11}) {
				emptied++
			} else if !bytes.Equal(globals[i], wholeGlobals[i]) {
				t.Errorf("%s: global subroutine %d changed", path, i)
			}
		}
		if emptied == 0 {
			t.Errorf("%s: no global subroutine emptied", path)
		}
		if len(subset) > len(cff)/3 {
			t.Errorf("%s: %d bytes of %d", path, len(subset), len(cff))
		}
	}
}

func TestCFFSubsetOfACIDKeyedFontHasTheIdentityCharset(t *testing.T) {
	// Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
	// numbers, and a PDF looks the glyphs of a CID-keyed font up by CID.
	_, cff := testCFF(t, testHanOTF)
	for _, used := range [][]bool{nil, make([]bool, 0x10000)} {
		subset, _, err := subsetCFF(cff, used)
		if err != nil {
			t.Fatal(err)
		}
		glyphs, _, top := testCFFParts(t, subset)
		at := cffEntryOf(top, cffCharset)[0]
		n := len(glyphs) - 2
		if want := []byte{2, 0, 1, byte(n >> 8), byte(n)}; !bytes.Equal(subset[at:at+5], want) {
			t.Errorf("used %v: the charset % x", used != nil, subset[at:at+5])
		}
	}
	// Kept whole, every charstring is as it was.
	whole, _, _ := testCFFParts(t, cff)
	subset, _, _ := subsetCFF(cff, nil)
	glyphs, _, _ := testCFFParts(t, subset)
	for gid := range glyphs {
		if !bytes.Equal(glyphs[gid], whole[gid]) {
			t.Fatalf("glyph %d is not as it was", gid)
		}
	}
}

// testCFFDoc draws the text in the font and returns the document.
func testCFFDoc(t *testing.T, level compliance.Compliance, path string, subset bool, text string) string {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level).SetTitle("Test")
	page := NewPage(doc.pdf, testLetterPortrait())
	font := NewFontFromFile(doc.pdf, testRepoPath(t, path))
	font.SetSubset(subset)
	NewTextLine(font, text).SetLocation(50, 50).DrawOn(page)
	return string(doc.complete())
}

func TestCFFAFontWithCFFOutlinesIsEmbeddedAsASubset(t *testing.T) {
	for _, path := range []string{testPlexOTF, testHanOTF} {
		raw := testCFFDoc(t, compliance.PDF_A_1B, path, true, "Hello 日本語")
		if !regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) {
			t.Errorf("%s: no subset tag", path)
		}
		if !strings.Contains(raw, "/Subtype /CIDFontType0C\n") || !strings.Contains(raw, "/FontFile3 ") {
			t.Errorf("%s: not embedded as CFF", path)
		}
		if strings.Contains(raw, "/Length1 ") {
			t.Errorf("%s: a /Length1 for CFF", path)
		}
		if !strings.Contains(raw, "/CIDSet ") {
			t.Errorf("%s: no CIDSet in PDF/A-1", path)
		}
	}
	// Kept whole, it has no tag.
	raw := testCFFDoc(t, compliance.PDF_1_7, testPlexOTF, false, "Hello")
	if regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) || !strings.Contains(raw, "/FontFile3 ") {
		t.Error("the font kept whole")
	}
}

func TestCFFAFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole(t *testing.T) {
	data, err := os.ReadFile(testRepoPath(t, testPlexOTF))
	if err != nil {
		t.Fatal(err)
	}
	os2 := testOpenTypeTable(t, data, "OS/2")
	data = testOpenTypeWith(data, os2+8, 0x0100)
	doc := testNewDoc()
	font := NewFont(doc.pdf, bytes.NewReader(data))
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, testLetterPortrait()))
	if raw := string(doc.complete()); regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) {
		t.Error("the font was subset")
	}
}

// FuzzSubsetCFF checks that a CFF table of any bytes is subset or refused, and
// never makes the subsetter panic.
//
//	go test -run '^$' -fuzz FuzzSubsetCFF -fuzztime 60s .
func FuzzSubsetCFF(f *testing.F) {
	data, err := os.ReadFile("../" + testPlexOTF)
	if err != nil {
		f.Fatal(err)
	}
	otf := newOpenTypeFont(bytes.NewReader(data))
	f.Add(otf.buf[otf.cffOff:otf.cffOff+otf.cffLen], uint16(36))
	f.Fuzz(func(t *testing.T, cff []byte, gid uint16) {
		used := make([]bool, 0x10000)
		used[gid] = true
		_, _, _ = subsetCFF(cff, used)
		_, _, _ = subsetCFF(cff, nil)
	})
}

func TestCFFSubsetOfAnAccentedLetterDrawnWithSeacIsRefused(t *testing.T) {
	// Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
	// it is drawn from were emptied, and it drew blank (the review of
	// 9 October 2026). The font is embedded whole.
	otf, cff := testCFF(t, testPlexOTF)
	names, _ := readCFFIndex(cff, int(cff[2]))
	topDicts, _ := readCFFIndex(cff, names.end)
	top, _ := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	charStrings, err := readCFFIndex(cff, cffEntryOf(top, cffCharStrings)[0])
	if err != nil {
		t.Fatal(err)
	}
	gid := otf.unicodeToGID['Á']
	at := charStrings.objects[gid]
	if charStrings.objects[gid+1]-at < 6 {
		t.Fatalf("the charstring of Á is %d bytes", charStrings.objects[gid+1]-at)
	}
	copy(cff[at:], []byte{139, 139, 204, 247, 86, 14})
	if _, _, err := subsetCFF(cff, testCFFUsed(otf, "Á")); err != errNotSubset {
		t.Errorf("error %v, not errNotSubset", err)
	}
}
