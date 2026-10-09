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
	"io"
	"strconv"
	"strings"
)

func registerOpenTypeFont(pdf *PDF, font *Font, reader io.Reader) {
	otf := newOpenTypeFont(reader)
	setOpenTypeFontData(font, otf)

	// The font is written at Complete, a subset of the glyphs drawn; its
	// pages refer to the number reserved for it.
	if otf.cff {
		shareFontProgram(pdf, font, otf.buf[otf.cffOff:otf.cffOff+otf.cffLen], true, otf.fsType&0x0100 != 0)
	} else {
		shareFontProgram(pdf, font, otf.buf, false, false)
	}
	font.objNumber = pdf.reserveObjNumber()
	pdf.fonts = append(pdf.fonts, font)
}

// addOpenTypeFontToObjects adds the font to the objects of an existing PDF;
// its program is written when the objects are added to the PDF, a subset of
// the glyphs drawn.
func addOpenTypeFontToObjects(objects *[]*PDFobj, font *Font, reader io.Reader) {
	otf := newOpenTypeFont(reader)
	setOpenTypeFontData(font, otf)
	font.program = newFontProgram()
	if otf.cff {
		font.program.font = otf.buf[otf.cffOff : otf.cffOff+otf.cffLen]
		font.program.cff = true
		font.program.forbidden = otf.fsType&0x0100 != 0
	} else {
		font.program.font = otf.buf
	}
	addFontToObjects(objects, font)
}

// setOpenTypeFontData gives the font the name, the metrics and the character
// map of the OpenType or TrueType font.
func setOpenTypeFontData(font *Font, otf *openTypeFont) {

	font.name = otf.fontName
	font.firstChar = otf.firstChar
	font.lastChar = otf.lastChar
	font.unicodeToGID = otf.unicodeToGID
	font.markToMarkOffsets = otf.markToMarkOffsets
	font.markAnchors = otf.markAnchors
	font.baseAnchors = otf.baseAnchors
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
