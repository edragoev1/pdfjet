// astral_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"
)

// The characters past U+FFFF, as the Chinese characters of Hong Kong, 𨋢 the
// lift among them, which IBM Plex Sans TC maps in its format 12 subtable.

func TestGlyphMapPastTheBasicMultilingualPlane(t *testing.T) {
	m := new(glyphMap)
	m.set('A', 3)
	m.set(0x282E2, 7)
	m.setIfAbsent(0x282E2, 9) // Kept
	m.setIfAbsent(0x20021, 5)
	m.set(0x110000, 4) // Not a character
	if m.get('A') != 3 || m.get(0x282E2) != 7 || m.get(0x20021) != 5 || m.get(0x110000) != 0 || m.get(0x20000) != 0 {
		t.Errorf("A %d, U+282E2 %d, U+20021 %d, U+110000 %d, U+20000 %d",
			m.get('A'), m.get(0x282E2), m.get(0x20021), m.get(0x110000), m.get(0x20000))
	}
	var order []rune
	m.each(func(c rune, gid int) { order = append(order, c) })
	if len(order) != 3 || order[0] != 'A' || order[1] != 0x20021 || order[2] != 0x282E2 {
		t.Errorf("each gives %U, not in the order of the characters", order)
	}
	// A font of a million characters past U+FFFF is read no further than
	// maxAstral
	big := new(glyphMap)
	for c := rune(0x10000); c < 0x10000+2*maxAstral; c++ {
		big.setIfAbsent(c, 1)
	}
	if len(big.astral) != maxAstral {
		t.Errorf("%d characters past U+FFFF kept, not %d", len(big.astral), maxAstral)
	}
}

func TestACharacterPastU0xFFFFIsDrawnInTheFallbackFont(t *testing.T) {
	pdf := testNewPDF()
	plex := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
	tc := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.ttf"))
	if !tc.hasGlyph(0x282E2) || plex.hasGlyph(0x282E2) {
		t.Fatalf("TC has 𨋢: %v; Plex Sans has it: %v", tc.hasGlyph(0x282E2), plex.hasGlyph(0x282E2))
	}
	if gid := tc.unicodeToGID.get(0x282E2); gid == 0 || gid == tc.unicodeToGID.get('乘') {
		t.Errorf("𨋢 is glyph %d of TC", gid)
	}
	// Each a Chinese character of TC, one em wide, none the box of Plex Sans
	if w := plex.StringWidthUsingFallbackFont(tc, 1000, "乘𨋢到"); w != 3000 {
		t.Errorf("乘𨋢到 is %v wide in Plex Sans with TC, not 3000", w)
	}
	// Copied out of the PDF as 𨋢: in UTF-16, the two halves of a surrogate
	// pair
	if cmap := toUnicodeCMap(tc, nil); !strings.Contains(cmap, "<d860dee2>") {
		t.Error("the ToUnicode map has no <d860dee2> for 𨋢")
	}
	if unicodeHex(0x282E2) != "d860dee2" || unicodeHex('A') != "0041" {
		t.Errorf("unicodeHex: %s, %s", unicodeHex(0x282E2), unicodeHex('A'))
	}
}
