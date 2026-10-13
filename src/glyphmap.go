// glyphmap.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import "slices"

// glyphMap maps the characters of the Basic Multilingual Plane to the glyphs
// of a font, as its cmap table does, in two levels: 256 blocks of 256
// characters, a block made only where the font has a character in it. A
// font of 1,000 characters in a few blocks takes some kilobytes, where an
// array of an int for each of the 65,536 characters took 512 KB for every
// font of every document; a lookup is two reads of an array still.
type glyphMap struct {
	blocks [256]*[256]int32
}

// get returns the glyph of the character, or 0 for one the font does not
// have.
func (m *glyphMap) get(c rune) int {
	if m == nil || c < 0 || c > 0xFFFF {
		return 0
	}
	block := m.blocks[c>>8]
	if block == nil {
		return 0
	}
	return int(block[c&0xFF])
}

// set sets the glyph of the character, which is in the Basic Multilingual
// Plane.
func (m *glyphMap) set(c rune, gid int) {
	block := m.blocks[c>>8]
	if block == nil {
		if gid == 0 {
			return
		}
		block = new([256]int32)
		m.blocks[c>>8] = block
	}
	block[c&0xFF] = int32(gid)
}

// setIfAbsent sets the glyph of the character, which is in the Basic
// Multilingual Plane, unless it has one: as get and then set, with one lookup
// of its block, for the format 12 subtable of a font, which maps again the
// characters its format 4 subtable mapped (12 October 2026).
func (m *glyphMap) setIfAbsent(c rune, gid int) {
	block := m.blocks[c>>8]
	if block == nil {
		if gid == 0 {
			return
		}
		block = new([256]int32)
		m.blocks[c>>8] = block
	}
	if block[c&0xFF] == 0 {
		block[c&0xFF] = int32(gid)
	}
}

// each calls the function for each character that has a glyph, in the order
// of the characters.
func (m *glyphMap) each(fn func(c rune, gid int)) {
	for b, block := range m.blocks {
		if block == nil {
			continue
		}
		for i, gid := range block {
			if gid != 0 {
				fn(rune(b<<8|i), int(gid))
			}
		}
	}
}

// unicodeOfGlyphs returns the character of each glyph of the map, as the
// ToUnicode map writes it, indexed by glyph number, -1 for a glyph of no
// character: the first of those that share a glyph, in the order of the
// characters, but one that is seldom text, as the soft hyphen or a
// combining mark, gives way to one that is not. It is made once, when the
// font is read, where the ToUnicode map walked the 65,536 characters twice
// for every font of every document.
func unicodeOfGlyphs(m *glyphMap) []int32 {
	var chars []int32
	m.each(func(c rune, gid int) {
		if gid >= len(chars) {
			n := len(chars)
			chars = slices.Grow(chars, gid+1-n)[:gid+1]
			for i := n; i <= gid; i++ {
				chars[i] = -1
			}
		}
		if chars[gid] == -1 || (isSeldomText(int(chars[gid])) && !isSeldomText(int(c))) {
			chars[gid] = int32(c)
		}
	})
	return chars
}
