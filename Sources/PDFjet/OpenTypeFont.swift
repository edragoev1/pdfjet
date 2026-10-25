/**
 * OpenTypeFont.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class OpenTypeFont {
    internal static func register(
            _ pdf: PDF, _ font: Font, _ bytes: [UInt8]) throws {
        let otf = try OTF(bytes)
        try checkEmbedding(otf)
        setData(font, otf)

        // The font is written at complete(), a subset of the glyphs drawn; its
        // pages refer to the number reserved for it.
        if otf.cff {
            Subset.share(pdf, font, Array(otf.buf[otf.cffOff!..<(otf.cffOff! + otf.cffLen!)]),
                    true, otf.fsType & 0x0100 != 0, otf.numGlyphs)
        } else {
            Subset.share(pdf, font, otf.buf, false, false, otf.numGlyphs)
        }
        font.objNumber = pdf.reserveObjNumber()
        pdf.fonts.append(font)
    }

    // Adds the font to the objects of an existing PDF; its program is written
    // when the objects are added to the PDF, a subset of the glyphs drawn.
    internal static func register(
            _ objects: inout [PDFobj], _ font: Font, _ bytes: [UInt8]) throws {
        let otf = try OTF(bytes)
        try checkEmbedding(otf)
        setData(font, otf)
        let program = Subset.Program(otf.numGlyphs)
        if otf.cff {
            program.font = Array(otf.buf[otf.cffOff!..<(otf.cffOff! + otf.cffLen!)])
            program.cff = true
            program.forbidden = otf.fsType & 0x0100 != 0
        } else {
            program.font = otf.buf
        }
        font.program = program
        FontObjects.register(&objects, font)
    }

    // Refuses a font whose license forbids embedding it: the fsType of its
    // OS/2 table "Restricted License embedding", 2, and none of the bits that
    // allow it, Preview and Print or Editable, as the least restrictive of the
    // bits set is the one that applies. It was embedded (review of 9 October
    // 2026, "a font whose fsType forbids embedding").
    // Bit 1 is tested alone, as bit 0 is reserved, and a font whose fsType allows
    // its bitmaps alone, bit 9, is refused too, as PDFjet embeds outlines
    // (review of 12 October 2026, R13).
    private static func checkEmbedding(_ otf: OTF) throws {
        if otf.fsType & 0x0200 != 0 {
            throw PDFjetError(message: "The font \(otf.fontName ?? "") may not be embedded: " +
                    "its license allows its bitmaps alone to be embedded, and PDFjet embeds outlines. " +
                    "See \"Choosing a font you may embed\" in PDFjet's README.")
        }
        if otf.fsType & 0x0002 != 0 && otf.fsType & 0x000C == 0 {
            throw PDFjetError(message: "The font \(otf.fontName ?? "") may not be embedded: " +
                    "its license asks for its maker's permission first. " +
                    "See \"Choosing a font you may embed\" in PDFjet's README.")
        }
    }

    // Gives the font the name, the metrics and the character map of the
    // OpenType or TrueType font.
    private static func setData(_ font: Font, _ otf: OTF) {
        font.name = otf.fontName!
        font.firstChar = otf.firstChar!
        font.lastChar = otf.lastChar!
        font.unitsPerEm = otf.unitsPerEm!
        font.bBoxLLx = otf.bBoxLLx!
        font.bBoxLLy = otf.bBoxLLy!
        font.bBoxURx = otf.bBoxURx!
        font.bBoxURy = otf.bBoxURy!
        font.advanceWidth = otf.advanceWidth
        font.unicodeToGID = otf.unicodeToGID
        font.setASCIIGlyphs()
        font.unicodeOf = FontWriter.unicodeOfGlyphs(otf.unicodeToGID)
        if let table = otf.gpos {
            // A CFF font keeps its CFF table alone, so a copy of the GPOS table.
            font.gpos = GPOS.of(otf.buf, table.offset ?? 0, table.length ?? -1, otf.numGlyphs, otf.cff)
        }
        font.kernTables = Kerning.of(font.gpos)
        font.fontAscent = otf.ascent!
        font.fontDescent = otf.descent!
        font.fontLineGap = otf.lineGap
        font.italicAngle = Int32(bitPattern: otf.italicAngle!)
        font.fontUnderlinePosition = otf.underlinePosition!
        font.fontUnderlineThickness = otf.underlineThickness!
        font.info = otf.fontInfo!
        font.capHeight = otf.capHeight!
        font.cff = otf.cff
        font.checksum = Font.checksumOf(font)
        font.setSize(font.size)
    }

    /// Converts a value in font units to the glyph space units of the font
    /// descriptor, 1/1000 em, rounded to the nearest integer with halves away
    /// from zero. The integer arithmetic gives the same value in every port.
    static func toGlyphSpace(_ value: Int16, _ unitsPerEm: Int) -> Int32 {
        let rounded = (2000 * abs(Int(value)) + unitsPerEm) / (2 * unitsPerEm)
        return Int32((value < 0) ? -rounded : rounded)
    }

    /// Returns the flags of the font descriptor: Nonsymbolic, 32, and Italic,
    /// 64, for a font whose post table gives it an italic angle.
    static func flagsOf(_ italicAngle: Int32) -> Int {
        return (italicAngle != 0) ? 32 | 64 : 32
    }

    /// Returns the italic angle of the font descriptor: the 16.16 fixed number
    /// of the post table in degrees, to two decimals with halves away from
    /// zero and no trailing zeros. The integer arithmetic gives the same text
    /// in every port.
    static func italicAngleOf(_ angle: Int32) -> String {
        let rounded = (200 * abs(Int64(angle)) + 65536) / (2 * 65536)
        var text = String(rounded / 100)
        if rounded % 100 != 0 {
            // The two decimals, as 100 to 199 without the 1, and no trailing zero.
            var decimals = String(String(100 + rounded % 100).dropFirst())
            if decimals.hasSuffix("0") {
                decimals.removeLast()
            }
            text += "." + decimals
        }
        return (angle < 0 && rounded != 0) ? "-" + text : text
    }
}   // End of OpenTypeFont.swift
