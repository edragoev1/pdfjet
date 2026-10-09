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

        if !otf.cff {
            // A TrueType font is written at complete(), a subset of the glyphs
            // drawn; its pages refer to the number reserved for it.
            Subset.share(pdf, font, otf.buf)
            font.objNumber = pdf.reserveObjNumber()
            pdf.fonts.append(font)
            return
        }
        registerCFF(pdf, font, otf)
    }

    // Adds the font to the objects of an existing PDF, with its font program
    // whole.
    internal static func register(
            _ objects: inout [PDFobj], _ font: Font, _ stream: InputStream) throws {
        let otf = try OTF(stream)
        setData(font, otf)
        font.uncompressedSize = otf.buf.count
        FontObjects.register(&objects, font, otf.compress())
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

    // Writes a font with CFF outlines, which is embedded whole, as it is added.
    private static func registerCFF(_ pdf: PDF, _ font: Font, _ otf: OTF) {
        embedFontFile(pdf, font, otf)
        FontWriter.addFontDescriptorObject(pdf, font, font.name)
        FontWriter.addCIDFontDictionaryObject(pdf, font, font.name, nil)
        FontWriter.addToUnicodeCMapObject(pdf, font, nil)

        // Type0 Font Dictionary
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        pdf.append(otf.fontName!)
        pdf.append(Token.newline)
        pdf.append("/Encoding /Identity-H\n")
        pdf.append("/DescendantFonts [")
        pdf.append(font.cidFontDictObjNumber)
        pdf.append(" 0 R]\n")

        pdf.append("/ToUnicode ")
        pdf.append(font.toUnicodeCMapObjNumber)
        pdf.append(" 0 R\n")

        pdf.append(Token.endDictionary)
        pdf.endObj()

        font.objNumber = pdf.getObjNumber()
        pdf.fonts.append(font)
    }

    private static func embedFontFile(_ pdf: PDF, _ font: Font, _ otf: OTF) {
        // Check if the font file is already embedded
        for f in pdf.fonts {
            if f.fileObjNumber != 0 && f.name == otf.fontName && f.checksum == font.checksum {
                font.fileObjNumber = f.fileObjNumber
                return
            }
        }

        // The metadata is an object of its own, written before the font file,
        // whose dictionary refers to it, as in the other ports.
        let metadataObjNumber = pdf.addMetadataObject(otf.fontInfo!, true)

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        if otf.cff {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")

        if !otf.cff {
            pdf.append("/Length1 ")
            pdf.append(otf.buf.count)   // The uncompressed size
            pdf.append(Token.newline)
        }

        if metadataObjNumber != -1 {
            pdf.append("/Metadata ")
            pdf.append(metadataObjNumber)
            pdf.append(" 0 R\n")
        }

        let compressed = pdf.encrypted(otf.compress())
        pdf.append("/Length ")
        pdf.append(compressed.count)
        pdf.append(Token.newline)

        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(compressed)
        pdf.append(Token.endStream)
        pdf.endObj()

        font.fileObjNumber = pdf.getObjNumber()
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
