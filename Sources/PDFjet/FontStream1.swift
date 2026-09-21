/**
 * FontStream1.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

class FontStream1 {
    static func register(
            _ pdf: PDF,
            _ font: Font,
            _ stream: InputStream) throws {
        stream.open()
        defer {
            stream.close()
        }
        try getFontData(font, stream)
        try embedFontFile(pdf, font, stream)
        addFontDescriptorObject(pdf, font)
        addCIDFontDictionaryObject(pdf, font)
        addToUnicodeCMapObject(pdf, font)

        // Type0 Font Dictionary
        pdf.newObj()
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
        pdf.endObj()

        font.objNumber = pdf.getObjNumber()
        pdf.fonts.append(font)
    }

    private static func embedFontFile(
            _ pdf: PDF, _ font: Font, _ stream: InputStream) throws {
        // Check if the font file is already embedded
        for f in pdf.fonts {
            if f.fileObjNumber != 0 && f.name == font.name {
                font.fileObjNumber = f.fileObjNumber
                return
            }
        }

        let metadataObjNumber = pdf.addMetadataObject(font.info, true)
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Metadata ")
        pdf.append(metadataObjNumber)
        pdf.append(" 0 R\n")
        if font.cff {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")

        let compressed = pdf.encrypted(try readBytes(stream, font.compressedSize!))

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
        pdf.endObj()

        font.fileObjNumber = pdf.getObjNumber()
    }

    private static func addFontDescriptorObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.fontDescriptorObjNumber != 0 && f.name == font.name {
                font.fontDescriptorObjNumber = f.fontDescriptorObjNumber
                return
            }
        }

        pdf.newObj()
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
        pdf.append("/ItalicAngle 0\n")
        pdf.append("/CapHeight ")
        pdf.append(OpenTypeFont.toGlyphSpace(font.capHeight, font.unitsPerEm))
        pdf.append(Token.newline)
        pdf.append("/StemV 79\n")
        pdf.append(Token.endDictionary)
        pdf.endObj()

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
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Length ")
        pdf.append(cmap.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(cmap)
        pdf.append(Token.endStream)
        pdf.endObj()

        font.toUnicodeCMapObjNumber = pdf.getObjNumber()
    }

    private static func addCIDFontDictionaryObject(_ pdf: PDF, _ font: Font) {
        for f in pdf.fonts {
            if f.cidFontDictObjNumber != 0 && f.name == font.name {
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
        // The width of the glyphs past the /W array: those past the advance
        // widths, which have the width of the last one.
        pdf.append("/DW ")
        pdf.append(Int32(round(k * Float(font.advanceWidth[font.advanceWidth.count - 1]))))
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
        pdf.endObj()

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

    // The most bytes the metrics of a stream font decode to, with its marks
    // compressed in them. IBM Plex Sans JP has 180 KB.
    private static let maxFontMetricsLength = 16 * 1024 * 1024

    private static func fontStreamError(_ what: String) -> PDFjetError {
        return PDFjetError(message: "Invalid font stream: \(what).")
    }

    private static let endOfStream = PDFjetError(message: "Unexpected end of the font stream.")

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

    // Reads the next count bytes. It reads them as they come, so a count that
    // the stream does not have takes no memory; a single read may return fewer
    // bytes than asked for.
    static func readBytes(_ stream: InputStream, _ count: Int) throws -> [UInt8] {
        var bytes = [UInt8]()
        var buffer = [UInt8](repeating: 0, count: 4096)
        while bytes.count < count {
            let read = stream.read(&buffer, maxLength: min(count - bytes.count, buffer.count))
            if read <= 0 {
                throw endOfStream
            }
            bytes.append(contentsOf: buffer[0..<read])
        }
        return bytes
    }

    private static func skipFully(_ stream: InputStream, _ count: Int) throws {
        var buffer = [UInt8](repeating: 0, count: 4096)
        var remaining = count
        while remaining > 0 {
            let read = stream.read(&buffer, maxLength: min(remaining, buffer.count))
            if read <= 0 {
                throw endOfStream
            }
            remaining -= read
        }
    }

    private static func getInt8(_ stream: InputStream) throws -> Int {
        return Int(try readBytes(stream, 1)[0])
    }

    private static func getInt24(_ stream: InputStream) throws -> Int {
        let buffer = try readBytes(stream, 3)
        return (Int(buffer[0]) << 16) | (Int(buffer[1]) << 8) | Int(buffer[2])
    }

    private static func getUInt32(_ stream: InputStream) throws -> Int {
        let buffer = try readBytes(stream, 4)
        return (Int(buffer[0]) << 24) | (Int(buffer[1]) << 16) |
                (Int(buffer[2]) << 8) | Int(buffer[3])
    }

    // Reads the metrics of a stream font from a byte array, after checking
    // that the bytes are there.
    private struct Metrics {
        let data: [UInt8]
        let what: String
        var offset = 0

        init(_ data: [UInt8], _ what: String) {
            self.data = data
            self.what = what
        }

        var remaining: Int {
            return data.count - offset
        }

        // Checks that count entries of size bytes follow.
        func need(_ count: Int, _ size: Int) throws {
            if count < 0 || count > remaining / size {
                throw FontStream1.fontStreamError("\(what) end too soon")
            }
        }

        mutating func getInt32() throws -> Int {
            try need(1, 4)
            let value = Int32(bitPattern: UInt32(data[offset]) << 24 | UInt32(data[offset + 1]) << 16 |
                    UInt32(data[offset + 2]) << 8 | UInt32(data[offset + 3]))
            offset += 4
            return Int(value)
        }

        // Reads the number of entries of size bytes that follow.
        mutating func getCount(_ size: Int) throws -> Int {
            let count = try getInt32()
            try need(count, size)
            return count
        }

        mutating func getUInt16() -> UInt16 {
            let value = UInt16(data[offset]) << 8 | UInt16(data[offset + 1])
            offset += 2
            return value
        }

        mutating func getBytes(_ count: Int) -> [UInt8] {
            let bytes = Array(data[offset..<(offset + count)])
            offset += count
            return bytes
        }
    }

    // Reads where the marks go, which getFontData keeps compressed: for each
    // MarkToBase and MarkToLigature subtable the class and anchor of each mark
    // and the anchors of each letter, and then the offsets of the marks that go
    // on other marks. Done once, the first time a mark is drawn in the font; it
    // throws when the marks cannot be read, as the other ports do.
    static func readMarks(_ font: Font) throws {
        guard let markData = font.markData else {
            return
        }
        font.markData = nil
        let marks = try readMarks(Metrics(try inflate(markData, maxFontMetricsLength), "the marks"))
        font.markAnchors = marks.markAnchors
        font.baseAnchors = marks.baseAnchors
        font.markToMarkOffsets = marks.markToMarkOffsets
    }

    private static func readMarks(_ data: Metrics) throws ->
            (markAnchors: [[Int: [Int]]], baseAnchors: [[Int: [Int]]], markToMarkOffsets: [Int: [Int]]) {
        var data = data
        let subTables = try data.getCount(8)
        var markAnchors = [[Int: [Int]]]()
        var baseAnchors = [[Int: [Int]]]()
        for _ in 0..<subTables {
            var count = try data.getCount(16)
            var marks = [Int: [Int]](minimumCapacity: count)
            for _ in 0..<count {
                let gid = try data.getInt32()
                let markClass = try data.getInt32()
                // The anchors of a letter are 3 ints for each mark class.
                if markClass < 0 || markClass > 0xFFFF {
                    throw fontStreamError("a mark class")
                }
                let x = try data.getInt32()
                marks[gid] = [markClass, x, try data.getInt32()]
            }
            count = try data.getCount(8)
            var bases = [Int: [Int]](minimumCapacity: count)
            for _ in 0..<count {
                let gid = try data.getInt32()
                var anchors = [Int](repeating: 0, count: try data.getCount(4))
                for k in 0..<anchors.count {
                    anchors[k] = try data.getInt32()
                }
                bases[gid] = anchors
            }
            markAnchors.append(marks)
            baseAnchors.append(bases)
        }
        let pairs = try data.getCount(16)
        var markToMarkOffsets = [Int: [Int]](minimumCapacity: pairs)
        for _ in 0..<pairs {
            let other = try data.getInt32()
            let mark = try data.getInt32()
            let dx = try data.getInt32()
            markToMarkOffsets[(other << 16) | mark] = [dx, try data.getInt32()]
        }
        return (markAnchors, baseAnchors, markToMarkOffsets)
    }

    static func getFontData(_ font: Font, _ stream: InputStream) throws {
        let fontName = try readBytes(stream, try getInt8(stream))
        if !isFontName(fontName) {
            throw fontStreamError("the font name")
        }
        font.name = String(decoding: fontName, as: UTF8.self)
        font.info = String(decoding: try readBytes(stream, try getInt24(stream)), as: UTF8.self)

        var metrics = Metrics(try inflate(
                try readBytes(stream, try getUInt32(stream)), maxFontMetricsLength), "the metrics")
        font.unitsPerEm = try metrics.getInt32()
        font.bBoxLLx = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.bBoxLLy = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.bBoxURx = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.bBoxURy = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.fontAscent = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.fontDescent = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.firstChar = try metrics.getInt32()
        font.lastChar = try metrics.getInt32()
        font.capHeight = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.fontUnderlinePosition = Int16(truncatingIfNeeded: try metrics.getInt32())
        font.fontUnderlineThickness = Int16(truncatingIfNeeded: try metrics.getInt32())
        // The range OpenType allows; the sizes of the text are divided by it.
        if font.unitsPerEm < 16 || font.unitsPerEm > 16384 {
            throw fontStreamError("the units per em")
        }
        // A character in the range is looked up in unicodeToGID.
        if font.firstChar < 0 || font.lastChar > 0xFFFF {
            throw fontStreamError("the first or last character")
        }

        var len = try metrics.getCount(2)
        if len == 0 {
            throw fontStreamError("no advance widths")
        }
        font.advanceWidth = [UInt16](repeating: 0, count: len)
        for i in 0..<len {
            font.advanceWidth[i] = metrics.getUInt16()
        }

        len = try metrics.getCount(2)
        if len != 0x10000 {
            throw fontStreamError("the character map")
        }
        font.unicodeToGID = [Int](repeating: 0, count: len)
        for i in 0..<len {
            font.unicodeToGID[i] = Int(metrics.getUInt16())
        }

        // Where the GPOS table of the font puts the marks, compressed on its
        // own after the metrics of a stream that has them. It is kept as it is
        // and read when a mark is drawn in the font; see readMarks. A font with
        // no marks has none, or 0 bytes of them.
        if metrics.remaining > 0 {
            let markData = metrics.getBytes(try metrics.getCount(1))
            if !markData.isEmpty {
                font.markData = markData
            }
        }
        // The line gap of a font that has one follows the marks, where a library
        // that does not read it stops.
        if metrics.remaining > 0 {
            font.fontLineGap = Int16(truncatingIfNeeded: try metrics.getInt32())
        }

        var flag = UnicodeScalar(try getInt8(stream))
        if flag == UnicodeScalar("R") {
            // The tables of an OpenType font that are not in its CFF data,
            // which keep the font whole; they are not embedded.
            try skipFully(stream, try getUInt32(stream))
            flag = UnicodeScalar(try getInt8(stream))
        }
        if flag == UnicodeScalar("Y") {
            font.cff = true
        }

        font.uncompressedSize = try getUInt32(stream)
        font.compressedSize = try getUInt32(stream)
    }
}   // End of FontStream1.swift
