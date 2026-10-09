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

// testSubsetUsed returns the glyphs used, as Font records them.
func testSubsetUsed(gids ...int) []bool {
	used := make([]bool, 0x10000)
	for _, gid := range gids {
		used[gid] = true
	}
	return used
}

func TestSubsetKeepsTheGlyphsUsedAndThePartsOfTheComposites(t *testing.T) {
	// In Noto Sans, Ä (134) is made of A (36) and the dieresis (106), and ǅ
	// (913) of D (39), z (93) and the caron (331).
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSans/NotoSans-Regular.ttf")
	subset, kept, err := subsetTrueType(ttf, testSubsetUsed(134, 913))
	if err != nil {
		t.Fatal(err)
	}
	if len(kept) != 4503 {
		t.Fatalf("%d glyphs, not 4503", len(kept))
	}
	want := map[int]bool{0: true, 134: true, 36: true, 106: true, 913: true, 39: true, 93: true, 331: true}
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
	// The other tables a reader needs as they were; shaping left out.
	for _, name := range []string{"GPOS", "GSUB", "GDEF"} {
		if bytes.Contains(subset[:12+16*int(binary.BigEndian.Uint16(subset[4:]))], []byte(name)) {
			t.Errorf("the subset has the %s table", name)
		}
	}
	if post := testOpenTypeTable(t, subset, "post"); binary.BigEndian.Uint32(subset[post:]) != 0x00030000 {
		t.Error("the post table is not of version 3")
	}
	for _, name := range []string{"cmap", "hmtx", "hhea", "name", "OS/2", "maxp"} {
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
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	head := testOpenTypeTable(t, ttf, "head")
	if binary.BigEndian.Uint16(ttf[head+50:]) != 0 {
		t.Skip("the font has long offsets")
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
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSans/NotoSans-Regular.ttf")
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
	for _, path := range []string{"fonts/NotoSans/NotoSans-Regular.ttf", "fonts/NotoSans/NotoSans-Regular.ttf.stream"} {
		raw := testSubsetDoc(t, compliance.PDF_1_7, path, true, "Ä")
		tagged := regexp.MustCompile(`/BaseFont /[A-Z]{6}\+NotoSans-Regular\n`).FindAllString(raw, -1)
		if len(tagged) != 2 || !strings.Contains(raw, "/FontName /"+tagged[0][len("/BaseFont /"):]) {
			t.Errorf("%s: the names %q", path, tagged)
		}
		program := testSubsetProgram(t, raw)
		if len(testSubsetGlyph(t, program, 134)) == 0 || len(testSubsetGlyph(t, program, 36)) == 0 ||
			len(testSubsetGlyph(t, program, 37)) != 0 {
			t.Errorf("%s: the glyphs kept", path)
		}
		// The widths of the glyphs kept: .notdef, A, the dieresis and Ä.
		if !regexp.MustCompile(`/W \[\n0\[\d+ \]\n36\[\d+ \]\n106\[\d+ \]\n134\[\d+ \]\]\n`).MatchString(raw) {
			t.Errorf("%s: the widths", path)
		}
		if strings.Contains(raw, "/CIDSet") {
			t.Errorf("%s: a CIDSet out of PDF/A-1", path)
		}
	}
}

func TestSubsetAFontSetToStayWholeIsEmbeddedWhole(t *testing.T) {
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSans/NotoSans-Regular.ttf")
	for _, path := range []string{"fonts/NotoSans/NotoSans-Regular.ttf", "fonts/NotoSans/NotoSans-Regular.ttf.stream"} {
		raw := testSubsetDoc(t, compliance.PDF_1_7, path, false, "Ä")
		if !strings.Contains(raw, "/BaseFont /NotoSans-Regular\n") || strings.Contains(raw, "+NotoSans") {
			t.Errorf("%s: the name", path)
		}
		if !bytes.Equal(testSubsetProgram(t, raw), ttf) {
			t.Errorf("%s: the font is not whole", path)
		}
	}
}

func TestSubsetTwoFontsOfOneFileShareOneSubset(t *testing.T) {
	raw := testSubsetDoc(t, compliance.PDF_1_7, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A", "B")
	if n := strings.Count(raw, "/Length1 "); n != 1 {
		t.Fatalf("%d font programs", n)
	}
	program := testSubsetProgram(t, raw)
	if len(testSubsetGlyph(t, program, 36)) == 0 || len(testSubsetGlyph(t, program, 37)) == 0 {
		t.Error("the glyphs of one of the fonts are missing")
	}
	if n := len(regexp.MustCompile(`/BaseFont /[A-Z]{6}\+NotoSans-Regular\n`).FindAllString(raw, -1)); n != 3 {
		t.Errorf("%d tagged names, not 3: two Type0 fonts and their CID font", n)
	}
}

func TestSubsetAPDFA1HasTheCIDSetOfTheGlyphsKept(t *testing.T) {
	raw := testSubsetDoc(t, compliance.PDF_A_1B, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A")
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
	// Glyphs 0 and 36 of 4503.
	want := make([]byte, (4503+7)/8)
	want[0] = 0x80
	want[36/8] |= 0x80 >> (36 % 8)
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
