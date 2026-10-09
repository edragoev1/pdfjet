// fontwriter.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

// The objects of an embedded font that every font writes, whatever its
// outlines: its descriptor, its CID font with the widths of its glyphs, and
// its ToUnicode map.

import (
	"encoding/hex"
	"math"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/token"
)

// addFontDescriptorObject writes the font descriptor with the name given,
// which is the font's own, or that of a subset.
func addFontDescriptorObject(pdf *PDF, font *Font, fontName string) {
	for _, f := range pdf.fonts {
		if f.fontDescriptorObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
			font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
			return
		}
	}

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /FontDescriptor\n")
	pdf.appendString("/FontName /")
	pdf.appendString(fontName)
	pdf.appendString("\n")
	if font.cff {
		pdf.appendString("/FontFile3 ")
	} else {
		pdf.appendString("/FontFile2 ")
	}
	pdf.appendInteger(font.fileObjNumber)
	pdf.appendString(" 0 R\n")
	pdf.appendString("/Flags ")
	pdf.appendInteger(flagsOf(font.italicAngle))
	pdf.appendString("\n")
	pdf.appendString("/FontBBox [")
	pdf.appendInteger(toGlyphSpace(font.bBoxLLx, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxLLy, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxURx, font.unitsPerEm))
	pdf.appendString(" ")
	pdf.appendInteger(toGlyphSpace(font.bBoxURy, font.unitsPerEm))
	pdf.appendString("]\n")
	pdf.appendString("/Ascent ")
	pdf.appendInteger(toGlyphSpace(font.fontAscent, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/Descent ")
	pdf.appendInteger(toGlyphSpace(font.fontDescent, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/ItalicAngle ")
	pdf.appendString(italicAngleOf(font.italicAngle))
	pdf.appendString("\n")
	pdf.appendString("/CapHeight ")
	pdf.appendInteger(toGlyphSpace(font.capHeight, font.unitsPerEm))
	pdf.appendString("\n")
	pdf.appendString("/StemV 79\n")
	if font.cidSetObjNumber != 0 {
		pdf.appendString("/CIDSet ")
		pdf.appendInteger(font.cidSetObjNumber)
		pdf.appendString(" 0 R\n")
	}
	pdf.appendString(">>\n")
	pdf.endObj()

	font.fontDescriptorObjNumber = pdf.getObjNumber()
}

// addToUnicodeCMapObject writes the ToUnicode map of the font: of every glyph
// that has a character, or only of the glyphs kept, for a subset.
func addToUnicodeCMapObject(pdf *PDF, font *Font, kept []bool) {
	for _, f := range pdf.fonts {
		if f.toUnicodeCMapObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
			font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber
			return
		}
	}

	var sb strings.Builder
	sb.WriteString("/CIDInit /ProcSet findresource begin\n")
	sb.WriteString("12 dict begin\n")
	sb.WriteString("begincmap\n")
	sb.WriteString("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n")
	sb.WriteString("/CMapName /Adobe-Identity def\n")
	sb.WriteString("/CMapType 2 def\n")

	sb.WriteString("1 begincodespacerange\n")
	sb.WriteString("<0000> <FFFF>\n")
	sb.WriteString("endcodespacerange\n")

	list := make([]string, 0)
	// A character the font does not contain is drawn with the .notdef glyph.
	// PDF/UA requires every glyph to map to Unicode, so map it to the
	// replacement character.
	list = append(list, "<0000> <FFFD>\n")
	var buf strings.Builder
	unicodeOf := unicodeOfGlyphs(font.unicodeToGID)
	for cid := 0; cid <= 0xffff; cid++ {
		gid := font.unicodeToGID[cid]
		if gid > 0 && unicodeOf[gid] == cid && (kept == nil || (gid < len(kept) && kept[gid])) {
			buf.WriteString("<")
			buf.WriteString(toHexString(gid))
			buf.WriteString("> <")
			// A presentation form that the Bidi class puts in maps to the letters it stands for.
			if letters := lettersOf(rune(cid)); letters != nil {
				for _, letter := range letters {
					buf.WriteString(toHexString(int(letter)))
				}
			} else {
				buf.WriteString(toHexString(cid))
			}
			buf.WriteString(">\n")
			list = append(list, buf.String())
			buf.Reset()
			if len(list) == 100 {
				writeListTo(&sb, list)
				list = nil
			}
		}
	}
	if len(list) > 0 {
		writeListTo(&sb, list)
		list = nil
	}

	sb.WriteString("endcmap\n")
	sb.WriteString("CMapName currentdict /CMap defineresource pop\n")
	sb.WriteString("end\nend")

	pdf.addCompressedStream([]byte(sb.String()))
	font.toUnicodeCMapObjNumber = pdf.getObjNumber()
}

// addCompressedStream writes a stream object of the data, compressed.
func (pdf *PDF) addCompressedStream(data []byte) {
	compressed := deflateFontProgram(data)
	if pdf.encryption != nil {
		compressed = pdf.encryption.encrypt(compressed)
	}
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Length ")
	pdf.appendInteger(len(compressed))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(compressed)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
}

// addCIDFontDictionaryObject writes the CID font with the name given,
// which is the font's own, or that of a subset, whose widths are those of the
// glyphs it keeps.
func addCIDFontDictionaryObject(pdf *PDF, font *Font, baseFont string, kept []bool) {
	for _, f := range pdf.fonts {
		if f.cidFontDictObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
			font.cidFontDictObjNumber = f.cidFontDictObjNumber
			return
		}
	}

	pdf.newObj()
	pdf.appendByteArray(token.BeginDictionary)
	pdf.appendString("/Type /Font\n")
	if font.cff {
		pdf.appendString("/Subtype /CIDFontType0\n")
	} else {
		pdf.appendString("/Subtype /CIDFontType2\n")
	}
	pdf.appendString("/BaseFont /")
	pdf.appendString(baseFont)
	pdf.appendByte(token.Newline)

	registry := []byte("Adobe")
	ordering := []byte("Identity")
	if pdf.encryption != nil {
		registry = pdf.encryption.encrypt(registry)
		ordering = pdf.encryption.encrypt(ordering)
	}
	pdf.appendString("/CIDSystemInfo <</Registry <")
	pdf.appendString(hex.EncodeToString(registry))
	pdf.appendString("> /Ordering <")
	pdf.appendString(hex.EncodeToString(ordering))
	pdf.appendString("> /Supplement 0>>\n")

	pdf.appendString("/FontDescriptor ")
	pdf.appendInteger(font.fontDescriptorObjNumber)
	pdf.appendByteArray(token.ObjRef)

	k := float32(1000.0) / float32(font.unitsPerEm)

	// The width of the glyphs past the /W array: those past the advance
	// widths, which have the width of the last one.
	pdf.appendString("/DW ")
	pdf.appendInteger(int(math.Round(float64(float32(k * float32(font.advanceWidth[len(font.advanceWidth)-1]))))))
	pdf.appendString("\n")

	if kept == nil {
		pdf.appendString("/W [0[\n")
		for _, width := range font.advanceWidth {
			pdf.appendInteger(int(math.Round(float64(float32(k * float32(width))))))
			pdf.appendString(" ")
		}
		pdf.appendString("]]\n")
	} else {
		// Each run of kept glyphs: its first glyph and its widths.
		pdf.appendString("/W [")
		for gid := 0; gid < len(kept) && gid < len(font.advanceWidth); gid++ {
			if !kept[gid] {
				continue
			}
			pdf.appendString("\n")
			pdf.appendInteger(gid)
			pdf.appendString("[")
			for ; gid < len(kept) && gid < len(font.advanceWidth) && kept[gid]; gid++ {
				pdf.appendInteger(int(math.Round(float64(float32(k * float32(font.advanceWidth[gid]))))))
				pdf.appendString(" ")
			}
			pdf.appendString("]")
		}
		pdf.appendString("]\n")
	}

	pdf.appendString("/CIDToGIDMap /Identity\n")
	pdf.appendByteArray(token.EndDictionary)
	pdf.endObj()

	font.cidFontDictObjNumber = pdf.getObjNumber()
}

// unicodeOfGlyphs returns the character that each glyph maps to in a ToUnicode
// CMap. A glyph can stand for several characters, like the space and the
// no-break space, but viewers read a CMap with more than one entry for a glyph
// differently. So a glyph maps to the first character that uses it, unless
// that is one text seldom has and another character uses the glyph too, like
// the modifier letter apostrophe and the right single quotation mark.
func unicodeOfGlyphs(unicodeToGID []int) []int {
	chars := make([]int, 0x10000)
	for i := range chars {
		chars[i] = -1
	}
	for cid := 0; cid <= 0xffff; cid++ {
		gid := unicodeToGID[cid]
		if gid > 0 && (chars[gid] == -1 ||
			(isSeldomText(chars[gid]) && !isSeldomText(cid))) {
			chars[gid] = cid
		}
	}
	return chars
}

// isSeldomText returns true for the soft hyphen, the micro sign, the spacing
// modifier letters, the combining marks, the figure dash, the ohm, kelvin and
// angstrom signs, the deprecated angle brackets, the CJK and Kangxi radicals,
// which CJK fonts draw with the glyphs of the ideographs, and the private use
// characters. The micro sign and the three signs are the letters Unicode
// makes them, mu, omega, K and A with a ring, which a font often draws them
// with: such a glyph is copied as the letter, as Greek text needs it.
func isSeldomText(ch int) bool {
	return ch == 0x00AD ||
		ch == 0x00B5 ||
		(ch >= 0x02B0 && ch <= 0x036F) ||
		(ch >= 0x1AB0 && ch <= 0x1AFF) ||
		(ch >= 0x1DC0 && ch <= 0x1DFF) ||
		ch == 0x2012 ||
		(ch >= 0x20D0 && ch <= 0x20FF) ||
		ch == 0x2126 || ch == 0x212A || ch == 0x212B ||
		(ch >= 0x2329 && ch <= 0x232A) ||
		(ch >= 0x2E80 && ch <= 0x2FDF) ||
		(ch >= 0xE000 && ch <= 0xF8FF) ||
		(ch >= 0xFE20 && ch <= 0xFE2F)
}

func writeListTo(sb *strings.Builder, list []string) {
	sb.WriteString(strconv.Itoa(len(list)))
	sb.WriteString(" beginbfchar\n")
	for _, s := range list {
		sb.WriteString(s)
	}
	sb.WriteString("endbfchar\n")
}

// isFontName returns true if the name can be written as a PDF name as it is:
// printable ASCII, and none of the characters that end a name or start an
// escape.
func isFontName(name []byte) bool {
	if len(name) == 0 {
		return false
	}
	for _, b := range name {
		if b < 0x21 || b > 0x7E || strings.IndexByte("()<>[]{}/%#", b) != -1 {
			return false
		}
	}
	return true
}
