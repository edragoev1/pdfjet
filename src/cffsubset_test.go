// cffsubset_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"fmt"
	"os"
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

const (
	testPlexOTF = "tests/fonts/IBMPlexSans-Regular.otf"
	testHanOTF  = "tests/fonts/SourceHanSansJP-Regular.otf"
)

// testCFF returns the font and its CFF table.
func testCFF(t testing.TB, path string) (*openTypeFont, []byte) {
	t.Helper()
	data, err := os.ReadFile(testRepoPath(t.(*testing.T), path))
	if err != nil {
		t.Fatal(err)
	}
	otf := newOpenTypeFont(data)
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
	return emptiedCFFIndex(cff, index, nil), emptiedCFFIndex(cff, globals, nil), top
}

// testCFFUsed returns the glyphs of the text in the font.
func testCFFUsed(otf *openTypeFont, text string) []bool {
	used := make([]bool, 0x10000)
	for _, ch := range text {
		used[otf.unicodeToGID.get(ch)] = true
	}
	return used
}

// testCFFCharset returns the CIDs the charset of the subset gives its glyphs
// after .notdef, of formats 0, 1 and 2.
func testCFFCharset(t *testing.T, cff []byte, top []cffEntry, glyphs int) []int {
	t.Helper()
	at := cffEntryOf(top, cffCharset)[0]
	format := cff[at]
	at++
	var cids []int
	for len(cids) < glyphs-1 {
		switch format {
		case 0:
			cids = append(cids, int(cff[at])<<8|int(cff[at+1]))
			at += 2
		case 1, 2:
			first := int(cff[at])<<8 | int(cff[at+1])
			left := int(cff[at+2])
			at += 3
			if format == 2 {
				left = left<<8 | int(cff[at])
				at++
			}
			for cid := first; cid <= first+left; cid++ {
				cids = append(cids, cid)
			}
		default:
			t.Fatalf("charset format %d", format)
		}
	}
	return cids
}

func TestCFFSubsetIsACIDFontOfTheGlyphsUsedAlone(t *testing.T) {
	for _, path := range []string{testPlexOTF, testHanOTF} {
		otf, cff := testCFF(t, path)
		used := testCFFUsed(otf, "Hello 日本語")
		subset, kept, err := subsetCFF(cff, used)
		if err != nil {
			t.Fatal(path, err)
		}
		whole, wholeGlobals, _ := testCFFParts(t, cff)
		glyphs, globals, top := testCFFParts(t, subset)
		if len(kept) != len(whole) {
			t.Fatalf("%s: kept is of %d glyphs, not %d", path, len(kept), len(whole))
		}
		// The glyphs kept, .notdef and those used, alone, in their order
		var keptGIDs []int
		for gid := range kept {
			if want := gid == 0 || used[gid]; kept[gid] != want {
				t.Errorf("%s: glyph %d kept: %v", path, gid, kept[gid])
			}
			if kept[gid] && gid > 0 {
				keptGIDs = append(keptGIDs, gid)
			}
		}
		if len(glyphs) != len(keptGIDs)+1 {
			t.Fatalf("%s: %d glyphs, not %d", path, len(glyphs), len(keptGIDs)+1)
		}
		// A CIDFont, whose charset gives each glyph its number in the font
		// as its CID, which the content streams draw it by
		if cffEntryOf(top, cffROS) == nil || cffEntryOf(top, cffFDArray) == nil {
			t.Errorf("%s: the subset is not a CIDFont", path)
		}
		if cffEntryOf(top, cffEncoding) != nil {
			t.Errorf("%s: the subset has an Encoding", path)
		}
		cids := testCFFCharset(t, subset, top, len(glyphs))
		if fmt.Sprint(cids) != fmt.Sprint(keptGIDs) {
			t.Errorf("%s: the CIDs %v, not %v", path, cids, keptGIDs)
		}
		// Each glyph as it was, .notdef first
		if !bytes.Equal(glyphs[0], whole[0]) {
			t.Errorf("%s: .notdef is not as it was", path)
		}
		for i, gid := range keptGIDs {
			if !bytes.Equal(glyphs[i+1], whole[gid]) {
				t.Errorf("%s: glyph %d is not as it was", path, gid)
			}
		}
		// The global subroutines keep their numbers: those called as they
		// were, the others emptied to no bytes
		if len(globals) != len(wholeGlobals) {
			t.Fatalf("%s: %d global subroutines, not %d", path, len(globals), len(wholeGlobals))
		}
		emptied := 0
		for i := range globals {
			if len(globals[i]) == 0 && len(wholeGlobals[i]) > 0 {
				emptied++
			} else if !bytes.Equal(globals[i], wholeGlobals[i]) {
				t.Errorf("%s: global subroutine %d changed", path, i)
			}
		}
		if emptied == 0 {
			t.Errorf("%s: no global subroutine emptied", path)
		}
		if len(subset) > len(cff)/10 {
			t.Errorf("%s: %d bytes of %d", path, len(subset), len(cff))
		}
	}
}

func TestCFFSubsetOfEveryGlyphHasTheIdentityCharset(t *testing.T) {
	// Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
	// numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
	// written whole, each glyph's CID is its number.
	for _, path := range []string{testPlexOTF, testHanOTF} {
		_, cff := testCFF(t, path)
		whole, _, _ := testCFFParts(t, cff)
		subset, kept, err := subsetCFF(cff, nil)
		if err != nil {
			t.Fatal(path, err)
		}
		glyphs, _, top := testCFFParts(t, subset)
		if len(glyphs) != len(whole) || len(kept) != len(whole) {
			t.Fatalf("%s: %d glyphs, not %d", path, len(glyphs), len(whole))
		}
		for i, cid := range testCFFCharset(t, subset, top, len(glyphs)) {
			if cid != i+1 {
				t.Fatalf("%s: glyph %d has the CID %d", path, i+1, cid)
			}
		}
		for gid := range glyphs {
			if !bytes.Equal(glyphs[gid], whole[gid]) {
				t.Fatalf("%s: glyph %d is not as it was", path, gid)
			}
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
	otf := newOpenTypeFont(data)
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
	gid := otf.unicodeToGID.get('Á')
	at := charStrings.objects[gid]
	if charStrings.objects[gid+1]-at < 6 {
		t.Fatalf("the charstring of Á is %d bytes", charStrings.objects[gid+1]-at)
	}
	copy(cff[at:], []byte{139, 139, 204, 247, 86, 14})
	if _, _, err := subsetCFF(cff, testCFFUsed(otf, "Á")); err != errNotSubset {
		t.Errorf("error %v, not errNotSubset", err)
	}
}
