/**
 * OpenTypeFont.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class OpenTypeFont {
    internal static func register(
            _ pdf: PDF, _ font: Font, _ stream: InputStream) throws {
        let otf = try OTF(stream)
        setData(font, otf)

        // The font is written at complete(), a subset of the glyphs drawn; its
        // pages refer to the number reserved for it.
        if otf.cff {
            Subset.share(pdf, font, Array(otf.buf[otf.cffOff!..<(otf.cffOff! + otf.cffLen!)]),
                    true, otf.fsType & 0x0100 != 0)
        } else {
            Subset.share(pdf, font, otf.buf, false, false)
        }
        font.objNumber = pdf.reserveObjNumber()
        pdf.fonts.append(font)
    }

    // Adds the font to the objects of an existing PDF; its program is written
    // when the objects are added to the PDF, a subset of the glyphs drawn.
    internal static func register(
            _ objects: inout [PDFobj], _ font: Font, _ stream: InputStream) throws {
        let otf = try OTF(stream)
        setData(font, otf)
        let program = Subset.Program()
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
        font.markToMarkOffsets = otf.markToMarkOffsets
        font.markAnchors = otf.markAnchors
        font.baseAnchors = otf.baseAnchors
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

    private static func toHexString(_ code: Int) -> String {
        let str = String(code, radix: 16)
        if str.unicodeScalars.count == 1 {
            return "000" + str
        } else if str.unicodeScalars.count == 2 {
            return "00" + str
        } else if str.unicodeScalars.count == 3 {
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
}   // End of OpenTypeFont.swift
