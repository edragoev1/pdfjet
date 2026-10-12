// opentypefont.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"strconv"
	"strings"
)

func registerOpenTypeFont(pdf *PDF, font *Font, buf []byte) {
	otf := newOpenTypeFont(buf)
	checkEmbedding(otf)
	setOpenTypeFontData(font, otf)

	// The font is written at Complete, a subset of the glyphs drawn; its
	// pages refer to the number reserved for it.
	if otf.cff {
		shareFontProgram(pdf, font, otf.buf[otf.cffOff:otf.cffOff+otf.cffLen], true, otf.fsType&0x0100 != 0, otf.numGlyphs)
	} else {
		shareFontProgram(pdf, font, otf.buf, false, false, otf.numGlyphs)
	}
	font.objNumber = pdf.reserveObjNumber()
	pdf.fonts = append(pdf.fonts, font)
}

// addOpenTypeFontToObjects adds the font to the objects of an existing PDF;
// its program is written when the objects are added to the PDF, a subset of
// the glyphs drawn.
func addOpenTypeFontToObjects(objects *[]*PDFobj, font *Font, buf []byte) {
	otf := newOpenTypeFont(buf)
	checkEmbedding(otf)
	setOpenTypeFontData(font, otf)
	font.program = newFontProgram(otf.numGlyphs)
	if otf.cff {
		font.program.font = otf.buf[otf.cffOff : otf.cffOff+otf.cffLen]
		font.program.cff = true
		font.program.forbidden = otf.fsType&0x0100 != 0
	} else {
		font.program.font = otf.buf
	}
	addFontToObjects(objects, font)
}

// checkEmbedding refuses a font whose license forbids embedding it: the
// fsType of its OS/2 table "Restricted License embedding", 2, and none of
// the bits that allow it, Preview and Print or Editable, as the least
// restrictive of the bits set is the one that applies. It was embedded
// (review of 9 October 2026, "a font whose fsType forbids embedding").
// Bit 1 is tested alone, as bit 0 is reserved, and a font whose fsType allows
// its bitmaps alone, bit 9, is refused too, as PDFjet embeds outlines
// (review of 12 October 2026, R13).
func checkEmbedding(otf *openTypeFont) {
	if otf.fsType&0x0200 != 0 {
		panic("The font " + otf.fontName + " may not be embedded: its license allows its bitmaps alone to be " +
			"embedded, and PDFjet embeds outlines. See \"Choosing a font you may embed\" in PDFjet's README.")
	}
	if otf.fsType&0x0002 != 0 && otf.fsType&0x000C == 0 {
		panic("The font " + otf.fontName + " may not be embedded: its license asks for its maker's " +
			"permission first. See \"Choosing a font you may embed\" in PDFjet's README.")
	}
}

// setOpenTypeFontData gives the font the name, the metrics and the character
// map of the OpenType or TrueType font.
func setOpenTypeFontData(font *Font, otf *openTypeFont) {

	font.name = otf.fontName
	font.firstChar = otf.firstChar
	font.lastChar = otf.lastChar
	font.unicodeToGID = otf.unicodeToGID
	font.unicodeOf = unicodeOfGlyphs(otf.unicodeToGID)
	if otf.gpos != nil {
		font.gpos = newGpos(otf.buf, otf.gpos.offset, otf.gpos.length, otf.numGlyphs)
	}
	font.unitsPerEm = otf.unitsPerEm
	font.bBoxLLx = otf.bBoxLLx
	font.bBoxLLy = otf.bBoxLLy
	font.bBoxURx = otf.bBoxURx
	font.bBoxURy = otf.bBoxURy
	font.fontAscent = otf.ascent
	font.fontDescent = otf.descent
	font.fontLineGap = otf.lineGap
	font.italicAngle = int32(otf.italicAngle)
	font.fontUnderlinePosition = otf.underlinePosition
	font.fontUnderlineThickness = otf.underlineThickness
	font.advanceWidth = otf.advanceWidth
	font.info = otf.fontInfo
	font.capHeight = otf.capHeight
	font.cff = otf.cff
	font.checksum = checksumOf(font)
	font.SetSize(font.size)
}

// toGlyphSpace converts a value in font units to the glyph space units of the
// font descriptor, 1/1000 em, rounded to the nearest integer with halves away
// from zero. The integer arithmetic gives the same value in every port.
func toGlyphSpace(value int16, unitsPerEm int) int {
	magnitude := int(value)
	if magnitude < 0 {
		magnitude = -magnitude
	}
	rounded := (2000*magnitude + unitsPerEm) / (2 * unitsPerEm)
	if value < 0 {
		return -rounded
	}
	return rounded
}

// flagsOf returns the flags of the font descriptor: Nonsymbolic, 32, and
// Italic, 64, for a font whose post table gives it an italic angle.
func flagsOf(italicAngle int32) int {
	if italicAngle != 0 {
		return 32 | 64
	}
	return 32
}

// italicAngleOf returns the italic angle of the font descriptor: the 16.16
// fixed number of the post table in degrees, to two decimals with halves away
// from zero and no trailing zeros. The integer arithmetic gives the same text
// in every port.
func italicAngleOf(angle int32) string {
	magnitude := int64(angle)
	if magnitude < 0 {
		magnitude = -magnitude
	}
	rounded := (200*magnitude + 65536) / (2 * 65536)
	text := strconv.FormatInt(rounded/100, 10)
	if rounded%100 != 0 {
		// The two decimals, as 100 to 199 without the 1, and no trailing zero.
		text += "." + strings.TrimSuffix(strconv.FormatInt(100+rounded%100, 10)[1:], "0")
	}
	if angle < 0 && rounded != 0 {
		return "-" + text
	}
	return text
}
