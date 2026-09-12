/**
 * FontStream1.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class FontStream1 {
    enum StreamError: Error {
        case read
        case write
    }

    static func register(
            _ pdf: PDF,
            _ font: Font,
            _ stream: InputStream) throws {
        stream.open()
        try getFontData(font, stream)
        embedFontFile(pdf, font, stream)
        stream.close()
        addFontDescriptorObject(pdf, font)
        addCIDFontDictionaryObject(pdf, font)
        addToUnicodeCMapObject(pdf, font)

        // Type0 Font Dictionary
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Font\n")
        pdf.append("/Subtype /Type0\n")
        pdf.append("/BaseFont /")
        pdf.append(Array(font.name.utf8))
        pdf.append(Token.newline)
        pdf.append("/Encoding /Identity-H\n")
        pdf.append("/DescendantFonts [")
        pdf.append(font.cidFontDictObjNumber)
        pdf.append(" 0 R]\n")
        pdf.append("/ToUnicode ")
        pdf.append(font.toUnicodeCMapObjNumber)
        pdf.append(" 0 R\n")
        pdf.append(Token.endDictionary)
        pdf.endobj()

        font.objNumber = pdf.getObjNumber()
        pdf.fonts.append(font)
    }

    private static func embedFontFile(
            _ pdf: PDF, _ font: Font, _ stream: InputStream) {
        // Check if the font file is already embedded
        for f in pdf.fonts {
            if f.fileObjNumber != 0 && f.name == font.name {
                font.fileObjNumber = f.fileObjNumber
                return
            }
        }

        let metadataObjNumber = pdf.addMetadataObject(font.info, true)
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Metadata ")
        pdf.append(metadataObjNumber)
        pdf.append(" 0 R\n")
        if font.cff {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")

        var compressed = [UInt8]()
        compressed.reserveCapacity(font.compressedSize!)
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let count = stream.read(&buffer, maxLength: buffer.count)
            if count > 0 {
                compressed.append(contentsOf: buffer[0..<count])
            }
        }
        compressed = pdf.encrypted(compressed)

        pdf.append("/Length ")
        pdf.append(compressed.count)
        pdf.append(Token.newline)

        if !font.cff {
            pdf.append("/Length1 ")
            pdf.append(font.uncompressedSize!)
            pdf.append(Token.newline)
        }

        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(compressed)
        pdf.append(Token.endStream)
        pdf.endobj()

        font.fileObjNumber = pdf.getObjNumber()
    }

    private static func addFontDescriptorObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.fontDescriptorObjNumber != 0 && f.name == font.name {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
                return
            }
        }

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /FontDescriptor\n")
        pdf.append("/FontName /")
        pdf.append(Array(font.name.utf8))
        pdf.append(Token.newline)
        if font.cff {
            pdf.append("/FontFile3 ")
        } else {
            pdf.append("/FontFile2 ")
        }
        pdf.append(font.fileObjNumber)
        pdf.append(" 0 R\n")
        pdf.append("/Flags 32\n")
        pdf.append("/FontBBox [")
        pdf.append(Int32(font.bBoxLLx))
        pdf.append(Token.space)
        pdf.append(Int32(font.bBoxLLy))
        pdf.append(Token.space)
        pdf.append(Int32(font.bBoxURx))
        pdf.append(Token.space)
        pdf.append(Int32(font.bBoxURy))
        pdf.append("]\n")
        pdf.append("/Ascent ")
        pdf.append(Int32(font.fontAscent))
        pdf.append(Token.newline)
        pdf.append("/Descent ")
        pdf.append(Int32(font.fontDescent))
        pdf.append(Token.newline)
        pdf.append("/ItalicAngle 0\n")
        pdf.append("/CapHeight ")
        pdf.append(Int32(font.capHeight))
        pdf.append(Token.newline)
        pdf.append("/StemV 79\n")
        pdf.append(Token.endDictionary)
        pdf.endobj()

        font.fontDescriptorObjNumber = pdf.getObjNumber()
    }

    private static func addToUnicodeCMapObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.toUnicodeCMapObjNumber != 0 && f.name == font.name {
                font.toUnicodeCMapObjNumber = f.toUnicodeCMapObjNumber
                return
            }
        }

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
        var buf = String()
        let unicodeOf = unicodeOfGlyphs(font.unicodeToGID)
        for cid in 0...0xffff {
            let gid = font.unicodeToGID[cid]
            if gid > 0 && unicodeOf[gid] == cid {
                buf.append("<")
                buf.append(toHexString(Int32(gid)))
                buf.append("> <")
                // A presentation form that the Bidi class puts in maps to the letters it stands for.
                if let letters = Bidi.lettersOf(UInt32(cid)) {
                    for letter in letters {
                        buf.append(toHexString(Int32(letter)))
                    }
                } else {
                    buf.append(toHexString(Int32(cid)))
                }
                buf.append(">\n")
                list.append(buf)
                buf = ""
                if list.count == 100 {
                    writeListToBuffer(&list, &sb)
                }
            }
        }
        if list.count > 0 {
            writeListToBuffer(&list, &sb)
        }

        sb.append("endcmap\n")
        sb.append("CMapName currentdict /CMap defineresource pop\n")
        sb.append("end\nend")

        let cmap = pdf.encrypted(Array(sb.utf8))
        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Length ")
        pdf.append(cmap.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(cmap)
        pdf.append(Token.endStream)
        pdf.endobj()

        font.toUnicodeCMapObjNumber = pdf.getObjNumber()
    }

    private static func addCIDFontDictionaryObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.cidFontDictObjNumber != 0 && f.name == font.name {
                font.cidFontDictObjNumber = f.cidFontDictObjNumber
                return
            }
        }

        pdf.newobj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /Font\n")
        if font.cff {
            pdf.append("/Subtype /CIDFontType0\n")
        } else {
            pdf.append("/Subtype /CIDFontType2\n")
        }
        pdf.append("/BaseFont /")
        pdf.append(Array(font.name.utf8))
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
        pdf.append("/DW ")
        pdf.append(Int32(round(k * Float(font.advanceWidth[0]))))
        pdf.append(Token.newline)
        var buffer = String()
        pdf.append("/W [0[\n")
        for i in 0..<font.advanceWidth.count {
            buffer.append(String(UInt16(round(k * Float(font.advanceWidth[i])))))
            buffer.append(" ")
        }
        pdf.append(buffer)
        pdf.append("]]\n")

        pdf.append("/CIDToGIDMap /Identity\n")
        pdf.append(Token.endDictionary)
        pdf.endobj()

        font.cidFontDictObjNumber = pdf.getObjNumber()
    }

    /// Returns the character that each glyph maps to in a ToUnicode CMap. A
    /// glyph can stand for several characters, like the space and the no-break
    /// space, but viewers read a CMap with more than one entry for a glyph
    /// differently. So a glyph maps to the first character that uses it, unless
    /// that is one text seldom has and another character uses the glyph too,
    /// like the modifier letter apostrophe and the right single quotation mark.
    static func unicodeOfGlyphs(_ unicodeToGID: [Int]) -> [Int] {
        var unicode = [Int](repeating: -1, count: 0x10000)
        for cid in 0...0xffff {
            let gid = unicodeToGID[cid]
            if gid > 0 && (unicode[gid] == -1 ||
                    (isSeldomText(unicode[gid]) && !isSeldomText(cid))) {
                unicode[gid] = cid
            }
        }
        return unicode
    }

    // The soft hyphen, the spacing modifier letters, the combining marks, the
    // figure dash, the deprecated angle brackets, the CJK and Kangxi radicals,
    // which CJK fonts draw with the glyphs of the ideographs, and the private
    // use characters.
    private static func isSeldomText(_ ch: Int) -> Bool {
        return ch == 0x00AD ||
                (ch >= 0x02B0 && ch <= 0x036F) ||
                (ch >= 0x1AB0 && ch <= 0x1AFF) ||
                (ch >= 0x1DC0 && ch <= 0x1DFF) ||
                ch == 0x2012 ||
                (ch >= 0x20D0 && ch <= 0x20FF) ||
                (ch >= 0x2329 && ch <= 0x232A) ||
                (ch >= 0x2E80 && ch <= 0x2FDF) ||
                (ch >= 0xE000 && ch <= 0xF8FF) ||
                (ch >= 0xFE20 && ch <= 0xFE2F)
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

    private static func getUInt16(_ stream: InputStream) throws -> UInt16 {
        var buffer = [UInt8](repeating: 0, count: 2)
        if stream.read(&buffer, maxLength: 2) == 2 {
            return (UInt16(buffer[0]) << 8) | UInt16(buffer[1])
        }
        throw StreamError.read
    }

    private static func getInt8(_ stream: InputStream) throws -> Int {
        var buffer = [UInt8](repeating: 0, count: 1)
        if stream.read(&buffer, maxLength: 1) == 1 {
            return Int(buffer[0])
        }
        throw StreamError.read
    }

    private static func getInt24(_ stream: InputStream) throws -> Int {
        var buffer = [UInt8](repeating: 0, count: 3)
        if stream.read(&buffer, maxLength: 3) == 3 {
            return (Int(buffer[0]) << 16) | (Int(buffer[1]) << 8) | Int(buffer[2])
        }
        throw StreamError.read
    }

    private static func getInt32(_ stream: InputStream) throws -> Int32 {
        var buffer = [UInt8](repeating: 0, count: 4)
        if stream.read(&buffer, maxLength: 4) == 4 {
            return (Int32(buffer[0]) << 24) | (Int32(buffer[1]) << 16) |
                    (Int32(buffer[2]) << 8) | Int32(buffer[3])
        }
        throw StreamError.read
    }

    private static func getUInt16(
            _ buffer: [UInt8], _ offset: inout Int) -> UInt16 {
        let value = (UInt16(buffer[offset]) << 8) | UInt16(buffer[offset + 1])
        offset += 2
        return value
    }

    private static func getInt(
            _ buffer: [UInt8], _ offset: inout Int) -> Int {
        let value = (Int(buffer[offset]) << 8) | Int(buffer[offset + 1])
        offset += 2
        return value
    }

    private static func getInt32(
            _ buffer: [UInt8], _ offset: inout Int) -> Int32 {
        let value = (Int32(buffer[offset]) << 24) | (Int32(buffer[offset + 1]) << 16) |
                (Int32(buffer[offset + 2]) << 8) | Int32(buffer[offset + 3])
        offset += 4
        return value
    }

    static func getFontData(_ font: Font, _ stream: InputStream) throws {
        var len = try getInt8(stream)
        var fontName = [UInt8](repeating: 0, count: len)
        if stream.read(&fontName, maxLength: len) == len {
            font.name = String(bytes: fontName, encoding: .utf8)!
        }

        len = try getInt24(stream)
        var fontInfo = [UInt8](repeating: 0, count: len)
        if stream.read(&fontInfo, maxLength: len) == len {
            font.info = String(bytes: fontInfo, encoding: .utf8)!
        }

        let deflatedLength = Int(try getInt32(stream))
        var deflated = [UInt8](repeating: 0, count: deflatedLength)
        if stream.read(&deflated, maxLength: deflatedLength) == deflatedLength {
        }

        var inflated = [UInt8]()
        _ = try Puff(output: &inflated, input: &deflated)

        var offset = 0
        font.unitsPerEm = Int(getInt32(inflated, &offset))
        font.bBoxLLx = Int16(getInt32(inflated, &offset))
        font.bBoxLLy = Int16(getInt32(inflated, &offset))
        font.bBoxURx = Int16(getInt32(inflated, &offset))
        font.bBoxURy = Int16(getInt32(inflated, &offset))
        font.fontAscent = Int16(getInt32(inflated, &offset))
        font.fontDescent = Int16(getInt32(inflated, &offset))
        font.firstChar = Int(getInt32(inflated, &offset))
        font.lastChar = Int(getInt32(inflated, &offset))
        font.capHeight = Int16(getInt32(inflated, &offset))
        font.fontUnderlinePosition = Int16(getInt32(inflated, &offset))
        font.fontUnderlineThickness = Int16(getInt32(inflated, &offset))

        len = Int(getInt32(inflated, &offset))
        font.advanceWidth = [UInt16](repeating: 0, count: len)
        for i in 0..<len {
            font.advanceWidth[i] = getUInt16(inflated, &offset)
        }

        len = Int(getInt32(inflated, &offset))
        font.unicodeToGID = [Int](repeating: 0, count: len)
        for i in 0..<len {
            font.unicodeToGID[i] = getInt(inflated, &offset)
        }

        let flag = UnicodeScalar(try getInt8(stream))
        if flag == UnicodeScalar("Y") {
            font.cff = true
        }

        font.uncompressedSize = Int(try getInt32(stream))
        font.compressedSize = Int(try getInt32(stream))
    }
}   // End of FontStream1.swift
