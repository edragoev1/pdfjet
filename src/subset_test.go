// subset_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"encoding/binary"
	"os"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/internal/decompressor"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testSubsetGlyph returns the bytes of the glyph in the glyf table of the
// font, at the offsets its loca table gives.
func testSubsetGlyph(t *testing.T, font []byte, gid int) []byte {
	t.Helper()
	loca := testOpenTypeTable(t, font, "loca")
	glyf := testOpenTypeTable(t, font, "glyf")
	head := testOpenTypeTable(t, font, "head")
	var start, end int
	if binary.BigEndian.Uint16(font[head+50:]) == 1 {
		start = int(binary.BigEndian.Uint32(font[loca+4*gid:]))
		end = int(binary.BigEndian.Uint32(font[loca+4*gid+4:]))
	} else {
		start = 2 * int(binary.BigEndian.Uint16(font[loca+2*gid:]))
		end = 2 * int(binary.BigEndian.Uint16(font[loca+2*gid+2:]))
	}
	return font[glyf+start : glyf+end]
}

// testSubsetPlex is IBM Plex Sans, which has the short offsets of loca; the
// subsets of a font with the long ones read Cascadia Mono or IBM Plex Math.
const testSubsetPlex = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"

// testSubsetUsed returns the glyphs used, as Font records them.
func testSubsetUsed(gids ...int) []bool {
	used := make([]bool, 0x10000)
	for _, gid := range gids {
		used[gid] = true
	}
	return used
}

func TestSubsetKeepsTheGlyphsUsedAndThePartsOfTheComposites(t *testing.T) {
	// In Cascadia Mono, whose loca has the long offsets, as that of Noto
	// Sans, which this test read before 9.1.7, had: Ä (16) is made of A (1)
	// and the dieresis of capitals (3051), and ặ (229) of a (225), the dot
	// below (3029) and the breve (3020); 4,319 glyphs. Noto Sans had Ä (134)
	// of A (36) and the dieresis (106), and ǅ (913) of three.
	ttf := testOpenTypeFontBytes(t, "fonts/CascadiaMono/CascadiaMono-Regular.ttf")
	if format := binary.BigEndian.Uint16(ttf[testOpenTypeTable(t, ttf, "head")+50:]); format != 1 {
		t.Fatalf("loca format %d", format)
	}
	subset, kept, err := subsetTrueType(ttf, testSubsetUsed(16, 229))
	if err != nil {
		t.Fatal(err)
	}
	if len(kept) != 4319 {
		t.Fatalf("%d glyphs, not 4319", len(kept))
	}
	want := map[int]bool{0: true, 16: true, 1: true, 3051: true, 229: true, 225: true, 3029: true, 3020: true}
	for gid, keep := range kept {
		if keep != want[gid] {
			t.Errorf("glyph %d kept: %v", gid, keep)
		}
		glyph := testSubsetGlyph(t, subset, gid)
		if keep {
			// The glyph as it was, padded to four bytes.
			whole := testSubsetGlyph(t, ttf, gid)
			if !bytes.Equal(glyph[:len(whole)], whole) || len(glyph)-len(whole) >= 4 {
				t.Errorf("glyph %d is not copied as it was", gid)
			}
		} else if len(glyph) != 0 {
			t.Errorf("glyph %d is not empty", gid)
		}
	}
	if len(subset) > len(ttf)/4 {
		t.Errorf("the subset is %d bytes of %d", len(subset), len(ttf))
	}
	// The other tables a reader needs as they were; shaping left out, and the
	// cmap and name tables, which a CIDFontType2 does not need.
	for _, name := range []string{"GPOS", "GSUB", "GDEF", "cmap", "name"} {
		if bytes.Contains(subset[:12+16*int(binary.BigEndian.Uint16(subset[4:]))], []byte(name)) {
			t.Errorf("the subset has the %s table", name)
		}
	}
	if post := testOpenTypeTable(t, subset, "post"); binary.BigEndian.Uint32(subset[post:]) != 0x00030000 {
		t.Error("the post table is not of version 3")
	}
	for _, name := range []string{"hmtx", "hhea", "OS/2", "maxp"} {
		entry := testOpenTypeEntry(t, ttf, name)
		length := int(binary.BigEndian.Uint32(ttf[entry+12:]))
		at := testOpenTypeTable(t, ttf, name)
		subsetAt := testOpenTypeTable(t, subset, name)
		if !bytes.Equal(ttf[at:at+length], subset[subsetAt:subsetAt+length]) {
			t.Errorf("the %s table changed", name)
		}
	}
	// The whole font sums to the magic number of its head table.
	if sum := tableChecksum(subset); sum != 0xB1B0AFBA {
		t.Errorf("the font sums to %x", sum)
	}
}

func TestSubsetOfAFontWithShortOffsets(t *testing.T) {
	// IBM Plex Sans Thai, whose loca has the short offsets, as Noto Sans
	// Thai's had.
	ttf := testOpenTypeFontBytes(t, "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf")
	head := testOpenTypeTable(t, ttf, "head")
	if binary.BigEndian.Uint16(ttf[head+50:]) != 0 {
		t.Fatal("the font has long offsets")
	}
	gid := 5
	subset, _, err := subsetTrueType(ttf, testSubsetUsed(gid))
	if err != nil {
		t.Fatal(err)
	}
	whole := testSubsetGlyph(t, ttf, gid)
	if glyph := testSubsetGlyph(t, subset, gid); !bytes.Equal(glyph[:len(whole)], whole) {
		t.Error("the glyph is not copied as it was")
	}
	if len(testSubsetGlyph(t, subset, gid+1)) != 0 {
		t.Error("the next glyph is not empty")
	}
}

func TestSubsetIsRefusedByAFontWhoseLicenseForbidsIt(t *testing.T) {
	ttf := testOpenTypeFontBytes(t, testSubsetPlex)
	os2 := testOpenTypeTable(t, ttf, "OS/2")
	if _, _, err := subsetTrueType(testOpenTypeWith(ttf, os2+8, 0x0100), testSubsetUsed(36)); err == nil {
		t.Error("the font was subset")
	}
}

// testSubsetDoc draws the text with each font made by the constructor, from
// the file, and returns the document.
func testSubsetDoc(t *testing.T, level compliance.Compliance, path string, subset bool, texts ...string) string {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level).SetTitle("Test")
	page := NewPage(doc.pdf, letter.Portrait())
	for i, text := range texts {
		font := NewFontFromFile(doc.pdf, testRepoPath(t, path))
		font.SetSubset(subset)
		NewTextLine(font, text).SetLocation(50, float32(50+20*i)).DrawOn(page)
	}
	return string(doc.complete())
}

// testSubsetProgram returns the font program embedded in the document.
func testSubsetProgram(t *testing.T, raw string) []byte {
	t.Helper()
	match := regexp.MustCompile(`/Length1 (\d+)\n(?:/Metadata \d+ 0 R\n)?/Length (\d+)\n>>\nstream\n`).FindStringSubmatchIndex(raw)
	if match == nil {
		t.Fatal("no font program")
	}
	length1, _ := strconv.Atoi(raw[match[2]:match[3]])
	length, _ := strconv.Atoi(raw[match[4]:match[5]])
	program, err := decompressor.Inflate([]byte(raw[match[1] : match[1]+length]))
	if err != nil {
		t.Fatal(err)
	}
	if len(program) != length1 {
		t.Errorf("/Length1 %d, not %d", length1, len(program))
	}
	return program
}

func TestSubsetATrueTypeFontIsEmbeddedAsASubsetUnderATaggedName(t *testing.T) {
	for _, path := range []string{testSubsetPlex} {
		raw := testSubsetDoc(t, compliance.PDF_1_7, path, true, "Ä")
		tagged := regexp.MustCompile(`/BaseFont /[A-Z]{6}\+IBMPlexSans\n`).FindAllString(raw, -1)
		if len(tagged) != 2 || !strings.Contains(raw, "/FontName /"+tagged[0][len("/BaseFont /"):]) {
			t.Errorf("%s: the names %q", path, tagged)
		}
		// In IBM Plex Sans, Ä (395) is made of A (33) and the dieresis of
		// capitals (971), and B is 34. Noto Sans had Ä (134), A (36), the
		// dieresis (106) and B (37).
		program := testSubsetProgram(t, raw)
		if len(testSubsetGlyph(t, program, 395)) == 0 || len(testSubsetGlyph(t, program, 33)) == 0 ||
			len(testSubsetGlyph(t, program, 34)) != 0 {
			t.Errorf("%s: the glyphs kept", path)
		}
		// The widths of the glyphs kept: .notdef, A, Ä and the dieresis.
		if !regexp.MustCompile(`/W \[\n0\[\d+ \]\n33\[\d+ \]\n395\[\d+ \]\n971\[\d+ \]\]\n`).MatchString(raw) {
			t.Errorf("%s: the widths", path)
		}
		if strings.Contains(raw, "/CIDSet") {
			t.Errorf("%s: a CIDSet out of PDF/A-1", path)
		}
	}
}

func TestSubsetAFontSetToStayWholeIsEmbeddedWhole(t *testing.T) {
	ttf := testOpenTypeFontBytes(t, testSubsetPlex)
	for _, path := range []string{testSubsetPlex} {
		raw := testSubsetDoc(t, compliance.PDF_1_7, path, false, "Ä")
		if !strings.Contains(raw, "/BaseFont /IBMPlexSans\n") || strings.Contains(raw, "+IBMPlexSans") {
			t.Errorf("%s: the name", path)
		}
		if !bytes.Equal(testSubsetProgram(t, raw), ttf) {
			t.Errorf("%s: the font is not whole", path)
		}
	}
}

func TestSubsetTwoFontsOfOneFileShareOneSubset(t *testing.T) {
	raw := testSubsetDoc(t, compliance.PDF_1_7, testSubsetPlex, true, "A", "B")
	if n := strings.Count(raw, "/Length1 "); n != 1 {
		t.Fatalf("%d font programs", n)
	}
	program := testSubsetProgram(t, raw)
	// A and B, 33 and 34 in IBM Plex Sans; 36 and 37 in Noto Sans.
	if len(testSubsetGlyph(t, program, 33)) == 0 || len(testSubsetGlyph(t, program, 34)) == 0 {
		t.Error("the glyphs of one of the fonts are missing")
	}
	if n := len(regexp.MustCompile(`/BaseFont /[A-Z]{6}\+IBMPlexSans\n`).FindAllString(raw, -1)); n != 3 {
		t.Errorf("%d tagged names, not 3: two Type0 fonts and their CID font", n)
	}
}

func TestSubsetAPDFA1HasTheCIDSetOfTheGlyphsKept(t *testing.T) {
	raw := testSubsetDoc(t, compliance.PDF_A_1B, testSubsetPlex, true, "A")
	match := regexp.MustCompile(`/CIDSet (\d+) 0 R\n`).FindStringSubmatch(raw)
	if match == nil {
		t.Fatal("no CIDSet")
	}
	at := strings.Index(raw, "\n"+match[1]+" 0 obj\n")
	stream := regexp.MustCompile(`/Length (\d+)\n>>\nstream\n`).FindStringSubmatchIndex(raw[at:])
	length, _ := strconv.Atoi(raw[at+stream[2] : at+stream[3]])
	bits, err := decompressor.Inflate([]byte(raw[at+stream[1] : at+stream[1]+length]))
	if err != nil {
		t.Fatal(err)
	}
	// Glyphs 0 and 33, A, of the 1,019 of IBM Plex Sans; Noto Sans had A at
	// 36 of 4,503.
	want := make([]byte, (1019+7)/8)
	want[0] = 0x80
	want[33/8] |= 0x80 >> (33 % 8)
	if !bytes.Equal(bits, want) {
		t.Error("the CIDSet")
	}
}

// FuzzSubsetTrueType checks that a font of any bytes is subset or refused,
// and never makes the subsetter panic.
//
//	go test -run '^$' -fuzz FuzzSubsetTrueType -fuzztime 60s .
func FuzzSubsetTrueType(f *testing.F) {
	for _, path := range fuzzOpenTypeFontSeeds {
		data, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		f.Add(data, uint16(5))
	}
	f.Fuzz(func(t *testing.T, ttf []byte, gid uint16) {
		subset, kept, err := subsetTrueType(ttf, testSubsetUsed(int(gid)))
		if err == nil && (len(kept) == 0 || tableChecksum(subset) != 0xB1B0AFBA) {
			t.Error("a subset without its glyphs or its checksum")
		}
	})
}

// testSubsetInExistingPDF adds the font to the objects of a PDF, draws a word
// with it on the page, and returns the objects of the PDF written.
func testSubsetInExistingPDF(t *testing.T, path string, subset bool) []*PDFobj {
	t.Helper()
	objects := testRead(t, testPDFWithObjects(
		"<< /Type /Catalog /Pages 2 0 R >>",
		"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
		"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"))
	font := NewFontForObjects(&objects, bytes.NewReader(testFontBytes(t, path))).SetSubset(subset)
	doc := testNewDoc()
	page := NewPageFromObject(doc.pdf, doc.pdf.GetPageObjects(objects)[0])
	page.AddFontResource(font, &objects)
	page.DrawString(font, nil, 12, "Hello", 72, 72)
	page.Complete(&objects)
	if err := doc.pdf.AddObjects(objects); err != nil {
		t.Fatal(err)
	}
	return testRead(t, doc.complete())
}

// testSubsetFontName returns the /BaseFont of the Type0 font.
func testSubsetFontName(t *testing.T, objects []*PDFobj) string {
	t.Helper()
	for _, obj := range objects {
		if obj.GetValue("/Subtype") == "/Type0" {
			return obj.GetValue("/BaseFont")
		}
	}
	t.Fatal("no Type0 font")
	return ""
}

func TestSubsetOfATrueTypeFontAddedToAnExistingPDF(t *testing.T) {
	path := testSubsetPlex
	whole := len(testOpenTypeFontBytes(t, path))
	objects := testSubsetInExistingPDF(t, path, true)
	name := testSubsetFontName(t, objects)
	if !regexp.MustCompile(`^/[A-Z]{6}\+IBMPlexSans$`).MatchString(name) {
		t.Errorf("BaseFont %s", name)
	}
	testWant(t, name, testFindObject(objects, "/FontName").GetValue("/FontName"))
	testWant(t, name, testFindObject(objects, "/CIDToGIDMap").GetValue("/BaseFont"))
	file := testFindObject(objects, "/Length1")
	length1, _ := strconv.Atoi(file.GetValue("/Length1"))
	if length1 == 0 || length1 >= whole {
		t.Errorf("/Length1 %d, the whole font %d", length1, whole)
	}
	if got := len(file.GetData()); got != length1 {
		t.Errorf("%d bytes inflated, /Length1 %d", got, length1)
	}
	for _, obj := range objects {
		if cmap := string(obj.GetData()); strings.HasPrefix(cmap, "/CIDInit") {
			// The codespace range, .notdef and H, e, l, o
			if n := strings.Count(cmap, "> <"); n != 6 {
				t.Errorf("the ToUnicode map has %d entries", n)
			}
			return
		}
	}
	t.Error("no ToUnicode map")
}

func TestSubsetOfACFFFontAddedToAnExistingPDF(t *testing.T) {
	objects := testSubsetInExistingPDF(t, "tests/fonts/IBMPlexSans-Regular.otf", true)
	name := testSubsetFontName(t, objects)
	if !regexp.MustCompile(`^/[A-Z]{6}\+IBMPlexSans$`).MatchString(name) {
		t.Errorf("BaseFont %s", name)
	}
	if testFindObject(objects, "/FontFile3") == nil {
		t.Error("no /FontFile3")
	}
}

func TestSetSubsetFalseKeepsAFontAddedToAnExistingPDFWhole(t *testing.T) {
	path := testSubsetPlex
	objects := testSubsetInExistingPDF(t, path, false)
	testWant(t, "/IBMPlexSans", testSubsetFontName(t, objects))
	length1, _ := strconv.Atoi(testFindObject(objects, "/Length1").GetValue("/Length1"))
	if whole := len(testOpenTypeFontBytes(t, path)); length1 != whole {
		t.Errorf("/Length1 %d, the whole font %d", length1, whole)
	}
}

// testSubsetRefused checks that the font, made wrong, is not subset, and so
// embedded whole, rather than a panic or a subset made of it.
func testSubsetRefused(t *testing.T, ttf []byte, used []bool) {
	t.Helper()
	if _, _, err := subsetTrueType(ttf, used); err != errNotSubset {
		t.Errorf("error %v, not errNotSubset", err)
	}
}

func TestSubsetATableTwiceIsRefused(t *testing.T) {
	// The gasp table's entry made a second head table of 8 bytes, whose
	// checksum adjustment the subset wrote past its end (the review of
	// 9 October 2026).
	ttf := testOpenTypeFontBytes(t, testSubsetPlex)
	gasp := testOpenTypeEntry(t, ttf, "gasp")
	copy(ttf[gasp:], "head")
	binary.BigEndian.PutUint32(ttf[gasp+12:], 8)
	testSubsetRefused(t, ttf, testSubsetUsed(36))
}

func TestSubsetALocaFormatOtherThan0Or1IsRefused(t *testing.T) {
	ttf := testOpenTypeFontBytes(t, "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf")
	binary.BigEndian.PutUint16(ttf[testOpenTypeTable(t, ttf, "head")+50:], 0xFFFF)
	testSubsetRefused(t, ttf, testSubsetUsed(36))
}

func TestSubsetOverlappingGlyphsAreRefused(t *testing.T) {
	// Every even glyph's outline the whole glyf table, so that a subset of
	// them copied it again for each (the review of 9 October 2026: 112 KB
	// made into 384 MB). A subset is never larger than the glyf table and the
	// padding of its glyphs.
	ttf := testOpenTypeFontBytes(t, testSubsetPlex)
	head := testOpenTypeTable(t, ttf, "head")
	loca := testOpenTypeTable(t, ttf, "loca")
	glyfLength := int(binary.BigEndian.Uint32(ttf[testOpenTypeEntry(t, ttf, "glyf")+12:]))
	numGlyphs := int(binary.BigEndian.Uint16(ttf[testOpenTypeTable(t, ttf, "maxp")+4:]))
	long := binary.BigEndian.Uint16(ttf[head+50:]) == 1
	used := make([]bool, 0x10000)
	for gid := 0; gid <= numGlyphs; gid++ {
		offset := 0
		if gid%2 == 1 {
			offset = glyfLength &^ 3
		}
		if long {
			binary.BigEndian.PutUint32(ttf[loca+4*gid:], uint32(offset))
		} else {
			binary.BigEndian.PutUint16(ttf[loca+2*gid:], uint16(offset/2))
		}
		if gid%2 == 0 && gid < numGlyphs {
			used[gid] = true
		}
	}
	testSubsetRefused(t, ttf, used)
}
