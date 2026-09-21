// otf_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"io"
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testOpenTypeFontError returns the message that loading the font panics with.
func testOpenTypeFontError(font []byte) string {
	return testPanicMessage(func() {
		NewFont(NewPDF(bufio.NewWriter(io.Discard)), bytes.NewReader(font))
	})
}

// testOpenTypeFontDraws loads the font, draws with it and returns the message
// it panicked with, or "(no panic)" when it drew.
func testOpenTypeFontDraws(font []byte) string {
	return testPanicMessage(func() {
		pdf := NewPDF(bufio.NewWriter(io.Discard))
		f := NewFont(pdf, bytes.NewReader(font))
		NewTextLine(f, "Ab1 กิ่ x").SetLocation(50, 50).DrawOn(NewPage(pdf, letter.Portrait()))
		if err := pdf.Complete(); err != nil {
			panic(err)
		}
	})
}

// testOpenTypeFontBytes returns a font PDFjet ships.
func testOpenTypeFontBytes(t *testing.T, path string) []byte {
	t.Helper()
	font, err := os.ReadFile(testRepoPath(t, path))
	if err != nil {
		t.Fatal(err)
	}
	return font
}

// testOpenTypeEntry returns where the directory entry of the table of the
// name begins: its four letters, then its checksum, offset and length.
func testOpenTypeEntry(t *testing.T, font []byte, name string) int {
	t.Helper()
	for i := 0; i < int(binary.BigEndian.Uint16(font[4:])); i++ {
		entry := 12 + 16*i
		if string(font[entry:entry+4]) == name {
			return entry
		}
	}
	t.Fatalf("the font has no %s table", name)
	return 0
}

// testOpenTypeTable returns where the table of the name begins in the font.
func testOpenTypeTable(t *testing.T, font []byte, name string) int {
	t.Helper()
	return int(binary.BigEndian.Uint32(font[testOpenTypeEntry(t, font, name)+8:]))
}

// testOpenTypeCmap4 returns where the format 4 subtable of the character map
// begins: the one of the Windows platform, which PDFjet reads.
func testOpenTypeCmap4(t *testing.T, font []byte) int {
	t.Helper()
	cmap := testOpenTypeTable(t, font, "cmap")
	for i := 0; i < int(binary.BigEndian.Uint16(font[cmap+2:])); i++ {
		record := cmap + 4 + 8*i
		if binary.BigEndian.Uint16(font[record:]) == 3 && binary.BigEndian.Uint16(font[record+2:]) == 1 {
			return cmap + int(binary.BigEndian.Uint32(font[record+4:]))
		}
	}
	t.Fatal("the font has no character map of the Windows platform")
	return 0
}

// testOpenTypeWith returns the font with the two bytes at the offset changed.
func testOpenTypeWith(font []byte, offset, value int) []byte {
	patched := append([]byte(nil), font...)
	binary.BigEndian.PutUint16(patched[offset:], uint16(value))
	return patched
}

func TestOTFAFontThatEndsWhereATableIsReadIsRefused(t *testing.T) {
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	// The directory, and then the tables it points at, are read from the
	// front of the font, so every one of these ends in the middle of a read.
	for _, length := range []int{4, 5, 12, 13, 100, 1000} {
		testWant(t, "Invalid font file: the font ends too soon.",
			testOpenTypeFontError(font[:length]))
	}
	testWant(t, "(no panic)", testOpenTypeFontDraws(font))
}

func TestOTFAFontWithoutWhatItIsDrawnWithIsRefused(t *testing.T) {
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	otf := testOpenTypeFontBytes(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
	head := testOpenTypeTable(t, ttf, "head")
	hhea := testOpenTypeTable(t, ttf, "hhea")
	cffEntry := testOpenTypeEntry(t, otf, "CFF ")
	cases := []struct {
		want string
		font []byte
	}{
		// The name of the character map table, changed to one PDFjet does
		// not read, so that the font has none.
		{"Invalid font file: no character map.",
			append(append([]byte(nil), ttf[:testOpenTypeEntry(t, ttf, "cmap")]...),
				append([]byte("xmap"), ttf[testOpenTypeEntry(t, ttf, "cmap")+4:]...)...)},
		{"Invalid font file: the units per em.", testOpenTypeWith(ttf, head+18, 0)},
		{"Invalid font file: the units per em.", testOpenTypeWith(ttf, head+18, 15)},
		{"Invalid font file: the units per em.", testOpenTypeWith(ttf, head+18, 16385)},
		{"Invalid font file: no advance widths.", testOpenTypeWith(ttf, hhea+34, 0)},
		{"Invalid font file: the character map is not format 4.",
			testOpenTypeWith(ttf, testOpenTypeCmap4(t, ttf), 6)},
		// The length of the CFF table, past the end of the font.
		{"Invalid font file: the CFF table is not in the font.",
			testOpenTypeWith(otf, cffEntry+12, 0x7FFF)},
	}
	for _, c := range cases {
		testWant(t, c.want, testOpenTypeFontError(c.font))
	}
	testWant(t, "(no panic)", testOpenTypeFontDraws(otf))
}

// testOpenTypeWithout returns the font with the table of the name spelled
// differently, so that PDFjet does not read it and the font has none.
func testOpenTypeWithout(t *testing.T, font []byte, name string) []byte {
	t.Helper()
	patched := append([]byte(nil), font...)
	patched[testOpenTypeEntry(t, font, name)] = 'z'
	return patched
}

func TestOTFAFontWithNoNameOfItsOwnIsRefused(t *testing.T) {
	// The name goes into the PDF as the name of the font, where a name that
	// is not a PDF name would break the syntax, so a font with none is
	// refused as a stream font with none is.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	testWant(t, "Invalid font file: the font name.",
		testOpenTypeFontError(testOpenTypeWithout(t, font, "name")))
}

func TestOTFAFontWithoutTheTablesItNeedsNoneOfStillDraws(t *testing.T) {
	// Without OS/2 the font says it holds no characters and maps none of
	// them; without post it has no underline; without GPOS its marks are not
	// placed. None of the three stops it from drawing.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	for _, name := range []string{"OS/2", "post", "GPOS"} {
		testWant(t, "(no panic)", testOpenTypeFontDraws(testOpenTypeWithout(t, font, name)))
	}
}

func TestOTFANameRecordOutsideTheFontIsLeftOut(t *testing.T) {
	// The offset of the first name record, past the end of the font. The
	// font keeps its other records and still draws.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	name := testOpenTypeTable(t, font, "name")
	testWant(t, "(no panic)", testOpenTypeFontDraws(testOpenTypeWith(font, name+16, 0xFFFF)))
}

func TestOTFASegmentOutsideTheGlyphIDArrayGivesNoGlyph(t *testing.T) {
	// The length of the format 4 subtable, cut to its header and the four
	// arrays of its segments, so that its glyph ID array holds nothing and
	// every segment that is read through it points outside.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	subtable := testOpenTypeCmap4(t, font)
	segCount := int(binary.BigEndian.Uint16(font[subtable+6:]))
	testWant(t, "(no panic)",
		testOpenTypeFontDraws(testOpenTypeWith(font, subtable+2, 16+4*segCount)))
}

func TestOTFAGposTableIsNotReadPastTheWorkAFontNeeds(t *testing.T) {
	// The lookups of the GPOS table, and the subtables of its first lookup,
	// changed to the most a font can say it has. Read to the end it says,
	// the font takes every byte of memory there is and never loads; read to
	// the work a font needs, it loads at once.
	font := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	gpos := testOpenTypeTable(t, font, "GPOS")
	lookupList := gpos + int(binary.BigEndian.Uint16(font[gpos+8:]))
	lookup := lookupList + int(binary.BigEndian.Uint16(font[lookupList+2:]))
	patched := testOpenTypeWith(font, lookupList, 0xFFFF)
	patched = testOpenTypeWith(patched, lookup+4, 0xFFFF)
	testWant(t, "(no panic)", testOpenTypeFontDraws(patched))
}

// testOpenTypeCapHeight returns the cap height PDFjet reads of the font.
func testOpenTypeCapHeight(font []byte) int {
	return int(newOpenTypeFont(bytes.NewReader(font)).capHeight)
}

// testOpenTypeLength returns the font with the length of the table of the
// name in its directory changed.
func testOpenTypeLength(t *testing.T, font []byte, name string, length int) []byte {
	t.Helper()
	patched := append([]byte(nil), font...)
	binary.BigEndian.PutUint32(patched[testOpenTypeEntry(t, font, name)+12:], uint32(length))
	return patched
}

func TestOTFTheCapHeightIsReadOnlyFromAnOS2TableThatHasIt(t *testing.T) {
	// The cap height of both fonts, sCapHeight in OS/2 and the top of the H,
	// is 714, so sCapHeight is changed to 999 to tell the two apart. Noto
	// Sans Thai has the short offsets of loca, and Noto Sans the long ones.
	for _, path := range []string{"fonts/NotoSansThai/NotoSansThai-Regular.ttf",
		"fonts/NotoSans/NotoSans-Regular.ttf"} {
		font := testOpenTypeFontBytes(t, path)
		os2 := testOpenTypeTable(t, font, "OS/2")
		font = testOpenTypeWith(font, os2+88, 999)
		if got := testOpenTypeCapHeight(font); got != 999 {
			t.Errorf("%s, version 4: %d", path, got)
		}
		// A version 1 table ends before sCapHeight: the 999 is not its own.
		if got := testOpenTypeCapHeight(testOpenTypeWith(font, os2, 1)); got != 714 {
			t.Errorf("%s, version 1: %d, not the top of the H", path, got)
		}
		// Nor is it of a version 2 table that ends before it.
		if got := testOpenTypeCapHeight(testOpenTypeLength(t, font, "OS/2", 88)); got != 714 {
			t.Errorf("%s, a table of 88 bytes: %d, not the top of the H", path, got)
		}
	}
}

func TestOTFAFontWithoutTheCapHeightOrTheOutlineOfAnHHasItsAscent(t *testing.T) {
	// IBM Plex Sans has CFF outlines, and so no glyf table to find the top
	// of the H in; its ascent is 1025. Noto Sans Thai without its loca
	// table has no offset for its H; its ascent is 1061.
	otf := testOpenTypeFontBytes(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.otf")
	if got := testOpenTypeCapHeight(testOpenTypeWith(otf, testOpenTypeTable(t, otf, "OS/2"), 1)); got != 1025 {
		t.Errorf("CFF outlines: %d", got)
	}
	ttf := testOpenTypeFontBytes(t, "fonts/NotoSansThai/NotoSansThai-Regular.ttf")
	ttf = testOpenTypeWith(ttf, testOpenTypeTable(t, ttf, "OS/2"), 1)
	if got := testOpenTypeCapHeight(testOpenTypeWithout(t, ttf, "loca")); got != 1061 {
		t.Errorf("no loca table: %d", got)
	}
	// A loca table that ends before the offsets of the H, and a glyf table
	// that ends before the header of the H.
	if got := testOpenTypeCapHeight(testOpenTypeLength(t, ttf, "loca", 4)); got != 1061 {
		t.Errorf("a loca table of 4 bytes: %d", got)
	}
	if got := testOpenTypeCapHeight(testOpenTypeLength(t, ttf, "glyf", 4)); got != 1061 {
		t.Errorf("a glyf table of 4 bytes: %d", got)
	}
}
