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

	if !otf.cff {
		// A TrueType font is written at Complete, a subset of the glyphs
		// drawn; its pages refer to the number reserved for it.
		shareTrueTypeProgram(pdf, font, otf.buf)
		font.objNumber = pdf.reserveObjNumber()
		pdf.fonts = append(pdf.fonts, font)
		return
	}
	registerCFFFont(pdf, font, otf)
}

// addOpenTypeFontToObjects adds the font to the objects of an existing PDF,
// with its font program whole.
func addOpenTypeFontToObjects(objects *[]*PDFobj, font *Font, reader io.Reader) {
	otf := newOpenTypeFont(reader)
	setOpenTypeFontData(font, otf)
	font.uncompressedSize = len(otf.buf)
	addFontToObjects(objects, font, otf.compress())
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

// registerCFFFont writes a font with CFF outlines, which is embedded whole, as
// it is added.
func registerCFFFont(pdf *PDF, font *Font, otf *openTypeFont) {
	embedOpenTypeFontFile(pdf, font, otf)
	addFontDescriptorObject(pdf, font, font.name)
	addCIDFontDictionaryObject(pdf, font, font.name, nil)
	addToUnicodeCMapObject(pdf, font, nil)

	// Type0 Font Dictionary
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /Font\n")
	pdf.appendString("/Subtype /Type0\n")
	pdf.appendString("/BaseFont /")
	pdf.appendString(otf.fontName)
	pdf.appendString("\n")
	pdf.appendString("/Encoding /Identity-H\n")
	pdf.appendString("/DescendantFonts [")
	pdf.appendInteger(font.cidFontDictObjNumber)
	pdf.appendString(" 0 R]\n")
	pdf.appendString("/ToUnicode ")
	pdf.appendInteger(font.toUnicodeCMapObjNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString(">>\n")
	pdf.endObj()

	font.objNumber = pdf.getObjNumber()
	pdf.fonts = append(pdf.fonts, font)
}

func embedOpenTypeFontFile(pdf *PDF, font *Font, otf *openTypeFont) {
	// Check if the font file is already embedded
	for _, f := range pdf.fonts {
		if f.fileObjNumber != 0 && f.name == otf.fontName && f.checksum == font.checksum {
			font.fileObjNumber = f.fileObjNumber
			return
		}
	}

	metadataObjNumber := pdf.addMetadataObject(otf.fontInfo, true)

	pdf.newObj()
	pdf.appendString("<<\n")
	if otf.cff {
		pdf.appendString("/Subtype /CIDFontType0C\n")
	}
	pdf.appendString("/Filter /FlateDecode\n")

	if !otf.cff {
		pdf.appendString("/Length1 ")
		pdf.appendInteger(len(otf.buf)) // The uncompressed size
		pdf.appendString("\n")
	}

	if metadataObjNumber != 0 {
		pdf.appendString("/Metadata ")
		pdf.appendInteger(metadataObjNumber)
		pdf.appendString(" 0 R\n")
	}

	buf := otf.compress()
	if pdf.encryption != nil {
		buf = pdf.encryption.encrypt(buf)
	}

	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf))
	pdf.appendString("\n")

	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf)
	pdf.appendString("\nendstream\n")
	pdf.endObj()

	font.fileObjNumber = pdf.getObjNumber()
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
