/**
 * FontWriter.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The objects of an embedded font that every font writes, whatever its
/// outlines: its descriptor, its CID font with the widths of its glyphs, and
/// its ToUnicode map.
class FontWriter {
    // Writes the font descriptor with the name given, which is the font's own,
    // or that of a subset.
    static func addFontDescriptorObject(_ pdf: PDF, _ font: Font, _ fontName: String) {
        for f in pdf.fonts {
            if f.fontDescriptorObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
                return
            }
        }

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(Array(fontName.utf8))
        pdf.append(Token.newline)
        if font.cff {
            pdf.append("/FontFile3 ")
        } else {
            pdf.append("/FontFile2 ")
        }
        pdf.append(font.fileObjNumber)
        pdf.append(" 0 R\n")
        pdf.append("/Flags ")
        pdf.append(OpenTypeFont.flagsOf(font.italicAngle))
        pdf.append(Token.newline)
        pdf.append("/FontBBox [")
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxLLx, font.unitsPerEm))
        pdf.append(Token.space)
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxLLy, font.unitsPerEm))
        pdf.append(Token.space)
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxURx, font.unitsPerEm))
        pdf.append(Token.space)
        pdf.append(OpenTypeFont.toGlyphSpace(font.bBoxURy, font.unitsPerEm))
        pdf.append("]\n")
        pdf.append("/Ascent ")
        pdf.append(OpenTypeFont.toGlyphSpace(font.fontAscent, font.unitsPerEm))
        pdf.append(Token.newline)
        pdf.append("/Descent ")
        pdf.append(OpenTypeFont.toGlyphSpace(font.fontDescent, font.unitsPerEm))
        pdf.append(Token.newline)
        pdf.append("/ItalicAngle ")
        pdf.append(OpenTypeFont.italicAngleOf(font.italicAngle))
        pdf.append(Token.newline)
        pdf.append("/CapHeight ")
        pdf.append(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm))
        pdf.append(Token.newline)
        pdf.append("/StemV 79\n")
        if font.cidSetObjNumber != 0 {
            pdf.append("/CIDSet ")
            pdf.append(font.cidSetObjNumber)
            pdf.append(" 0 R\n")
        }
        pdf.append(Token.endDictionary)
        pdf.endObj()

        font.fontDescriptorObjNumber = pdf.getObjNumber()
    }

    // Writes the ToUnicode map of the font: of every glyph that has a
    // character, or only of the glyphs kept, for a subset.
    static func addToUnicodeCMapObject(_ pdf: PDF, _ font: Font, _ kept: [Bool]?) {
        for f in pdf.fonts {
            if f.toUnicodeCMapObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber
                return
            }
        }

        addCompressedStream(pdf, Array(toUnicodeCMap(font, kept).utf8))

        font.toUnicodeCMapObjNumber = pdf.getObjNumber()
    }

    // Returns the ToUnicode map of the font: of every glyph that has a
    // character, or only of the glyphs kept, for a subset.
    static func toUnicodeCMap(_ font: Font, _ kept: [Bool]?) -> String {
        var sb = String()
        sb.append("/CIDInit /ProcSet findresource begin\n")
        sb.append("12 dict begin\n")
        sb.append("begincmap\n")
        sb.append("/CIDSystemInfo <</Registry (Adobe) /Ordering (Identity) /Supplement 0>> def\n")
        sb.append("/CMapName /Adobe-Identity def\n")
        sb.append("/CMapType 2 def\n")

        sb.append("1 begincodespacerange\n")
        sb.append("<0000> <FFFF>\n")
        sb.append("endcodespacerange\n")

        var list = Array<String>()
        // A character the font does not contain is drawn with the .notdef
        // glyph. PDF/UA requires every glyph to map to Unicode, so map it to
        // the replacement character.
        list.append("<0000> <FFFD>\n")
        // The glyphs of the map, those of the subset or all of them, each with
        // its character, the one drawn with it where the document drew one of
        // several, in the order of the characters, as the map was written when
        // it walked them
        let unicodeOf = font.unicodeOf
        let chars = font.program?.chars
        var entries = [(c: Int, gid: Int)]()
        if unicodeOf.count > 1 {
            for gid in 1..<unicodeOf.count {
                if let kept = kept, gid >= kept.count || !kept[gid] {
                    continue
                }
                var c = Int(unicodeOf[gid])
                if let drawn = chars?.first(gid), drawn >= 0 {
                    c = drawn
                }
                if c >= 0 {
                    entries.append((c, gid))
                }
            }
        }
        entries.sort { $0.c < $1.c }
        var buf = String()
        for entry in entries {
            buf.append("<")
            buf.append(toHexString(Int32(entry.gid)))
            buf.append("> <")
            // A presentation form that the Bidi class puts in maps to the letters it stands for.
            if let letters = Bidi.lettersOf(UInt32(entry.c)) {
                for letter in letters {
                    buf.append(unicodeHex(Int(letter)))
                }
            } else {
                buf.append(unicodeHex(entry.c))
            }
            buf.append(">\n")
            list.append(buf)
            buf = ""
            if list.count == 100 {
                writeListToBuffer(&list, &sb)
            }
        }
        if list.count > 0 {
            writeListToBuffer(&list, &sb)
        }

        sb.append("endcmap\n")
        sb.append("CMapName currentdict /CMap defineresource pop\n")
        sb.append("end\nend")

        return sb
    }

    // Returns the /W array of the font: the widths of all its glyphs, or of
    // each run of the glyphs kept, for a subset, its first glyph and its
    // widths. A width is an Int32, as /DW is: in a font of few units per em
    // it is past the 65,535 of a UInt16 (the review of 9 October 2026: it
    // trapped).
    static func widthsArray(_ font: Font, _ kept: [Bool]?) -> String {
        var k: Float = 1.0
        if font.unitsPerEm != 1000 {
            k = Float(1000.0) / Float(font.unitsPerEm)
        }
        var buffer = String()
        guard let kept = kept else {
            buffer.append("[0[\n")
            for i in 0..<font.advanceWidth.count {
                buffer.append(String(Int32((k * Float(font.advanceWidth[i])).rounded())))
                buffer.append(" ")
            }
            buffer.append("]]")
            return buffer
        }
        buffer.append("[")
        let count = min(kept.count, font.advanceWidth.count)
        var gid = 0
        while gid < count {
            if !kept[gid] {
                gid += 1
                continue
            }
            buffer.append("\n")
            buffer.append(String(gid))
            buffer.append("[")
            while gid < count && kept[gid] {
                buffer.append(String(Int32((k * Float(font.advanceWidth[gid])).rounded())))
                buffer.append(" ")
                gid += 1
            }
            buffer.append("]")
        }
        buffer.append("]")
        return buffer
    }

    // Writes a stream object of the data, compressed.
    static func addCompressedStream(_ pdf: PDF, _ data: [UInt8]) {
        var compressed = [UInt8]()
        FlateEncode(&compressed, data, pdf.flateMemory)
        compressed = pdf.encrypted(compressed)
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Filter /FlateDecode\n")
        pdf.append("/Length ")
        pdf.append(compressed.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(compressed)
        pdf.append(Token.endStream)
        pdf.endObj()
    }

    // Writes the CID font with the name given, which is the font's own, or
    // that of a subset, whose widths are those of the glyphs it keeps.
    static func addCIDFontDictionaryObject(_ pdf: PDF, _ font: Font, _ baseFont: String, _ kept: [Bool]?) {
        for f in pdf.fonts {
            if f.cidFontDictObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber
                return
            }
        }

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Font\n")
        if font.cff {
            pdf.append("/Subtype /CIDFontType0\n")
        } else {
            pdf.append("/Subtype /CIDFontType2\n")
        }
        pdf.append("/BaseFont /")
        pdf.append(Array(baseFont.utf8))
        pdf.append(Token.newline)
        pdf.append("/CIDSystemInfo <</Registry <")
        pdf.append(pdf.toHexString("Adobe"))
        pdf.append("> /Ordering <")
        pdf.append(pdf.toHexString("Identity"))
        pdf.append("> /Supplement 0>>\n")
        pdf.append("/FontDescriptor ")
        pdf.append(font.fontDescriptorObjNumber)
        pdf.append(" 0 R\n")

        var k: Float = 1.0
        if font.unitsPerEm != 1000 {
            k = Float(1000.0) / Float(font.unitsPerEm)
        }
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        pdf.append("/DW ")
        pdf.append(Int32((k * Float(font.advanceWidth[font.advanceWidth.count - 1])).rounded()))
        pdf.append(Token.newline)
        pdf.append("/W ")
        pdf.append(widthsArray(font, kept))
        pdf.append(Token.newline)

        pdf.append("/CIDToGIDMap /Identity\n")
        pdf.append(Token.endDictionary)
        pdf.endObj()

        font.cidFontDictObjNumber = pdf.getObjNumber()
    }

    /// Returns the character that each glyph maps to in a ToUnicode CMap,
    /// indexed by glyph number, -1 for a glyph of no character. A glyph can
    /// stand for several characters, like the space and the no-break space,
    /// but viewers read a CMap with more than one entry for a glyph
    /// differently. So a glyph maps to the first character that uses it,
    /// unless that is one text seldom has and another character uses the
    /// glyph too, like the modifier letter apostrophe and the right single
    /// quotation mark. It is made once, when the font is read, where the
    /// ToUnicode map walked the 65,536 characters twice for every font of
    /// every document.
    static func unicodeOfGlyphs(_ unicodeToGID: GlyphMap) -> [Int32] {
        var unicode = [Int32]()
        unicodeToGID.forEach { cid, gid in
            if gid >= unicode.count {
                unicode.append(contentsOf: repeatElement(Int32(-1), count: gid + 1 - unicode.count))
            }
            if unicode[gid] == -1 ||
                    (isSeldomText(Int(unicode[gid])) && !isSeldomText(cid)) {
                unicode[gid] = Int32(cid)
            }
        }
        return unicode
    }

    // The soft hyphen, the micro sign, the spacing modifier letters, the
    // combining marks, the figure dash, the ohm, kelvin and angstrom signs, the
    // deprecated angle brackets, the CJK and Kangxi radicals, which CJK fonts
    // draw with the glyphs of the ideographs, and the private use characters.
    // The micro sign and the three signs are the letters Unicode makes them,
    // mu, omega, K and A with a ring, which a font often draws them with: such
    // a glyph is copied as the letter, as Greek text needs it.
    private static func isSeldomText(_ ch: Int) -> Bool {
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

    // Returns the character as the ToUnicode map writes it, in UTF-16: four
    // hexadecimal digits, or eight, the two halves of a surrogate pair, for a
    // character past U+FFFF.
    static func unicodeHex(_ c: Int) -> String {
        if c <= 0xFFFF {
            return toHexString(Int32(c))
        }
        let v = c - 0x10000
        return toHexString(Int32(0xD800 + (v >> 10))) + toHexString(Int32(0xDC00 + (v & 0x3FF)))
    }

    private static func toHexString(_ code: Int32) -> String {
        let str = String(code, radix: 16)
        if str.count == 1 {
            return "000" + str
        } else if str.count == 2 {
            return "00" + str
        } else if str.count == 3 {
            return "0" + str
        }
        return str
    }

    private static func writeListToBuffer(
            _ list: inout [String], _ sb: inout String) {
        sb.append(String(list.count))
        sb.append(" beginbfchar\n")
        for str in list {
            sb.append(str)
        }
        sb.append("endbfchar\n")
        list.removeAll()
    }

    // Returns true if the name can be written as a PDF name as it is:
    // printable ASCII, and none of the characters that end a name or start an
    // escape.
    static func isFontName(_ name: [UInt8]) -> Bool {
        if name.isEmpty {
            return false
        }
        for b in name {
            if b < 0x21 || b > 0x7E || Array("()<>[]{}/%#".utf8).contains(b) {
                return false
            }
        }
        return true
    }
}   // End of FontWriter.swift
