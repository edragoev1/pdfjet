/**
 * OTF.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

struct FontTable {
    var name: String?
    var offset: Int?
    var length: Int?
}

enum OTFError: Error {
    case format4SubtableNotFound
}

class OTF {
    var fontName: String?
    var fontInfo: String?
    var unitsPerEm: Int?
    // A table the font does not have leaves what it holds at 0, as it is left
    // in the other three ports, and not at a value to be unwrapped.
    var bBoxLLx: Int16? = 0
    var bBoxLLy: Int16? = 0
    var bBoxURx: Int16? = 0
    var bBoxURy: Int16? = 0
    var ascent: Int16? = 0
    var descent: Int16? = 0
    var lineGap: Int16 = 0
    var advanceWidth: [UInt16] = []
    // A font without an OS/2 table does not say which characters it holds,
    // so its character map is read for all of them.
    var firstChar: Int? = 0
    var lastChar: Int? = 0xFFFF
    var capHeight: Int16? = 0
    var hasCapHeight = false
    var indexToLocFormat = 0
    var loca: FontTable?
    var glyf: FontTable?
    var italicAngle: UInt32? = 0
    var underlinePosition: Int16? = 0
    var underlineThickness: Int16? = 0

    var buf = [UInt8]()
    var cff = false
    var fsType = 0          // Of the OS/2 table: what the license allows
    var cffOff: Int?
    var cffLen: Int?
    private var index = 0
    private(set) var numGlyphs = 0  // Of the maxp table, or 0 when the font has none

    private static func fontError(_ what: String) -> PDFjetError {
        return PDFjetError(message: "Invalid font file: \(what).")
    }

    var unicodeToGID = GlyphMap()
    var gpos: FontTable?    // Read when a mark is first drawn, by GPOS

    // Reads the font from a stream of its file.
    convenience init(_ stream: InputStream) throws {
        try self.init(try Content.getFromStream(stream))
    }

    // Reads the font from the bytes of its file, which the caller has read
    // once: reading them again from a stream of them cost a copy and a
    // second read of the whole file (10 October 2026).
    init(_ bytes: [UInt8]) throws {
        buf = bytes

        // Extract OTF metadata
        let version = try readUInt32()

        if version == 0x00010000 ||     // Win OTF
            version == 0x74727565 ||    // Mac TTF
            version == 0x4F54544F {     // CFF OTF
            // We should be able to read this font
        } else {
            throw OTF.fontError("not an OpenType or TrueType font: PDFjet reads .otf and .ttf fonts")
        }

        let numOfTables = try readUInt16()  // numOfTables
        try readUInt16()                    // searchRange
        try readUInt16()                    // entrySelector
        try readUInt16()                    // rangeShift

        var cmapTable: FontTable?
        var hmtxTable: FontTable?
        for _ in 0..<numOfTables {
            var name = [UInt8](repeating: 0, count: 4)
            for i in 0..<4 {
                name[i] = try readByte()
            }
            var table = FontTable()
            table.name = String(bytes: name, encoding: .utf8)
            _ = try readUInt32()
            table.offset = Int(try readUInt32())
            table.length = Int(try readUInt32())

            let k = index   // Save the current index
            if      table.name == "head" { try head(table) }
            else if table.name == "hhea" { try hhea(table) }
            else if table.name == "OS/2" { try OS_2(table) }
            else if table.name == "name" { try n4me(table) }
            else if table.name == "hmtx" { hmtxTable = table }
            else if table.name == "post" { try post(table) }
            else if table.name == "CFF " { try CFF_(table) }
            else if table.name == "GPOS" { gpos = table }
            else if table.name == "maxp" { numGlyphs = tableUInt16(table, 4) ?? 0 }
            else if table.name == "cmap" { cmapTable = table }
            else if table.name == "loca" { loca = table }
            else if table.name == "glyf" { glyf = table }
            index = k       // Restore the index
        }

        // The hmtx table is read after the hhea table, which says how many
        // advance widths it has, whatever their order in the directory: listed
        // first, its widths were left unread, every glyph 0 wide.
        if let hmtxTable = hmtxTable {
            try hmtx(hmtxTable)
        }

        // This table must be processed last
        guard let cmapTable = cmapTable else {
            throw OTF.fontError("no character map")
        }
        try cmap(cmapTable)

        // A font without the cap height of a version 2 OS/2 table has the top
        // of its H, as the glyph is drawn, or else its ascent: the cap height
        // goes into the font descriptor, where a reader fits text in a box
        // with it.
        if !hasCapHeight {
            capHeight = ascent
            if let top = glyphTop(unicodeToGID[0x48]) {     // H
                capHeight = top
            }
        }

        // The sizes of the text are divided by the units per em, and the
        // width of every glyph past the advance widths is that of the last one.
        if unitsPerEm == nil || unitsPerEm! < 16 || unitsPerEm! > 16384 {
            throw OTF.fontError("the units per em")
        }
        if advanceWidth.isEmpty {
            throw OTF.fontError("no advance widths")
        }
        // The name goes into the PDF as the name of the font.
        if fontName == nil || !FontWriter.isFontName(Array(fontName!.utf8)) {
            throw OTF.fontError("the font name")
        }
    }

    private func head(_ table: FontTable) throws {
        self.index = table.offset! + 16
        try readUInt16()                // flags
        unitsPerEm = Int(try readUInt16())
        self.index += 16
        self.bBoxLLx = try readInt16()
        self.bBoxLLy = try readInt16()
        self.bBoxURx = try readInt16()
        self.bBoxURy = try readInt16()
        self.indexToLocFormat = tableUInt16(table, 50) ?? 0
    }

    private func hhea(_ table: FontTable) throws {
        self.index = table.offset! + 4
        self.ascent  = try readInt16()
        self.descent = try readInt16()
        self.lineGap = try readInt16()
        self.index += 24
        self.advanceWidth = [UInt16](repeating: 0, count: Int(try readUInt16()))
    }

    private func OS_2(_ table: FontTable) throws {

        fsType = tableUInt16(table, 8) ?? 0
        index = table.offset! + 64
        firstChar = Int(try readUInt16())
        lastChar  = Int(try readUInt16())
        // sCapHeight is in the table from its version 2 on. Where it would be
        // in a version 0 or 1 table are the bytes after the table, often those
        // of the next table.
        if let version = tableUInt16(table, 0), version >= 2 {
            if let value = tableUInt16(table, 88) {
                capHeight = Int16(truncatingIfNeeded: value)
                hasCapHeight = true
            }
        }
    }

    // Returns the top of the bounding box of the glyph, the yMax of its header
    // in the glyf table, at the offset the loca table gives it. It returns nil
    // for a font with CFF outlines, which has no glyf table, for glyph 0, and
    // for a glyph with no outline, which has no header.
    private func glyphTop(_ gid: Int) -> Int16? {
        guard gid != 0, let loca = loca, let glyf = glyf else {
            return nil
        }
        var start: Int?
        var end: Int?
        if indexToLocFormat == 0 {
            // The short offsets are half the offsets.
            start = tableUInt16(loca, 2*gid).map { 2*$0 }
            end = tableUInt16(loca, 2*gid + 2).map { 2*$0 }
        } else {
            start = tableUInt32(loca, 4*gid)
            end = tableUInt32(loca, 4*gid + 4)
        }
        // The header of a glyph is 10 bytes: the number of contours, then
        // xMin, yMin, xMax and yMax.
        guard let start = start, let end = end, end - start >= 10 else {
            return nil
        }
        return tableUInt16(glyf, start + 8).map { Int16(truncatingIfNeeded: $0) }
    }

    private func n4me(_ table: FontTable) throws {
        self.index = table.offset!
        try readUInt16()            // format
        let count = try readUInt16()
        let stringOffset = try readUInt16()

        var macFontInfo = ""
        var winFontInfo = ""
        for _ in 0..<count {
            let platformID = try readUInt16()
            let encodingID = try readUInt16()
            let languageID = try readUInt16()
            let nameID = try readUInt16()
            let length = Int(try readUInt16())
            let offset = try readUInt16()

            let start = Int(table.offset!) + Int(stringOffset) + Int(offset)
            if start < 0 || length > buf.count - start {
                continue    // A name record outside the font is left out.
            }
            if platformID == 1 && encodingID == 0 && languageID == 0 {
                // Macintosh
                let buffer = buf[start..<(start + length)]
                // The bytes that are not UTF-8 are replaced with U+FFFD, as
                // the other ports replace them; see the Go internal/utf8text.
                let str = String(decoding: buffer, as: UTF8.self)
                if nameID == 6 {
                    fontName = str
                } else {
                    macFontInfo.append(str)
                    macFontInfo.append("\n")
                }
            } else if platformID == 3 && encodingID == 1 && languageID == 0x409 {
                // Windows
                let buffer = buf[start..<(start + length)]
                let str = String(bytes: buffer, encoding: .utf16)
                if nameID == 6 {
                    fontName = str
                } else {
                    if str != nil {
                        winFontInfo.append(str!)
                        winFontInfo.append("\n")
                    }
                }
            }
        }
        fontInfo = winFontInfo != "" ? winFontInfo : macFontInfo
    }

    private func cmap(_ table: FontTable) throws {
        self.index = table.offset!
        let tableOffset = index
        index += 2
        let numRecords = try readUInt16()

        // Process the encoding records: the format 4 subtable of the Windows
        // platform, and its format 12 subtable, which maps the characters past
        // the Basic Multilingual Plane too.
        var format4subtable = false
        var subtableOffset = 0
        var format12Offset = -1
        for _ in 0..<numRecords {
            let platformID = try readUInt16()
            let encodingID = try readUInt16()
            let offset = Int(try readUInt32())
            if platformID == 3 && encodingID == 1 && !format4subtable {
                format4subtable = true
                subtableOffset = offset
            } else if platformID == 3 && encodingID == 10 && format12Offset == -1 {
                format12Offset = offset
            }
        }
        if !format4subtable && format12Offset == -1 {
            throw OTFError.format4SubtableNotFound
        }
        if format4subtable {
            try cmapFormat4(tableOffset + subtableOffset)
        }
        if format12Offset != -1 {
            cmapFormat12(table, format12Offset)
        }
    }

    // Maps the characters of the Basic Multilingual Plane that the format 4
    // subtable left without a glyph, from the format 12 subtable at the offset
    // in the table. IBM Plex Sans TC, as a .ttf, has an empty format 4
    // subtable and maps every character in its format 12 subtable. The groups
    // go up and do not overlap, and a group that goes back is passed over, so
    // that the map is read once at most.
    private func cmapFormat12(_ table: FontTable, _ offset: Int) {
        guard tableUInt16(table, offset) == 12, let numGroups = tableUInt32(table, offset + 12),
                let length = table.length, numGroups <= (length - offset - 16) / 12 else {
            return
        }
        var next = firstChar!
        for i in 0..<numGroups {
            let group = offset + 16 + 12 * i
            guard let first = tableUInt32(table, group), var end = tableUInt32(table, group + 4),
                    let startGlyph = tableUInt32(table, group + 8) else {
                continue    // Past the end of the font
            }
            // The glyphs are counted from the group's first character, even
            // where the characters before next are passed over (the review of
            // 9 October 2026: they were counted from next)
            let start = max(first, next)
            if end > lastChar! {
                end = lastChar!
            }
            if start <= end {
                for ch in start...end {
                    let gid = startGlyph + (ch - first)
                    if gid < 0x10000 && unicodeToGID[ch] == 0 {
                        unicodeToGID.set(ch, gid)
                    }
                }
            }
            if end + 1 > next {
                next = end + 1
            }
        }
    }

    // Reads the format 4 subtable that begins at subtable.
    private func cmapFormat4(_ subtable: Int) throws {
        self.index = subtable

        if try readUInt16() != 4 {
            throw OTF.fontError("the character map is not format 4")
        }
        let tableLen = try readUInt16()
        try readUInt16()    // language
        let segCount = Int(try readUInt16() / 2)

        index += 6          // Skip to the endCount[]
        var endCount = [Int](repeating: 0, count: Int(segCount))
        var i = 0
        while i < segCount {
            endCount[i] = Int(try readUInt16())
            i += 1
        }

        index += 2          // Skip the reservedPad
        var startCount = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            startCount[i] = Int(try readUInt16())
            i += 1
        }

        var idDelta = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idDelta[i] = Int(try readUInt16())
            i += 1
        }

        var idRangeOffset = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idRangeOffset[i] = Int(try readUInt16())
            i += 1
        }

        // The glyph ID array is the rest of the subtable, after its header and
        // the four arrays of the segments. A length that leaves none of it is
        // read as none, not as an array of a negative size.
        var glyphIdLen = (Int(tableLen) - (16 + 8*segCount)) / 2
        if glyphIdLen < 0 {
            glyphIdLen = 0
        }
        var glyphIdArray = [Int](repeating: 0, count: glyphIdLen)
        i = 0
        while i < glyphIdArray.count {
            glyphIdArray[i] = Int(try readUInt16())
            i += 1
        }

        if firstChar! > lastChar! {
            return      // The font says it holds no characters.
        }
        // The segments are in the order of their end codes, so the segment of
        // a character is the first that ends at it or after it, if it starts
        // at it or before it. The characters go up, and so does the segment:
        // each segment has the characters after those of the segments before
        // it, up to its end, and the characters between segments are passed
        // over, not looked at one by one.
        var next = firstChar!   // The first character no segment has had yet
        for seg in 0..<segCount {
            if next > lastChar! {
                break
            }
            if endCount[seg] < next {
                continue
            }
            let first = max(next, startCount[seg])
            let end = min(endCount[seg], lastChar!)
            if first <= end {
                for ch in first...end {
                    var gid = 0
                    var offset = idRangeOffset[seg]
                    if offset == 0 {
                        // Per spec this is unsigned 16-bit modulo arithmetic.
                        // idDelta is read as unsigned here (Int(readUInt16())), so
                        // the sum is always non-negative and % 65536 already gives
                        // the right answer, but use & 0xFFFF to spell out the
                        // intended unsigned-16-bit wraparound explicitly (matches
                        // the other language ports and doesn't rely on that
                        // coincidence).
                        gid = (idDelta[seg] + ch) & 0xFFFF
                    } else {
                        offset /= 2
                        offset -= segCount - seg
                        let i2 = offset + (ch - startCount[seg])
                        if i2 < 0 || i2 >= glyphIdArray.count {
                            // The segment points outside the glyph ID array, so
                            // it gives this character no glyph.
                            continue
                        }
                        gid = glyphIdArray[i2]
                        if gid != 0 {
                            // idDelta[seg] % 65536 alone is a no-op (idDelta is
                            // already < 65536) and never wraps the actual sum, so
                            // a large gid + idDelta could overflow past the valid
                            // 16-bit glyph ID range uncorrected. Wrap the *sum*
                            // instead.
                            gid = (gid + idDelta[seg]) & 0xFFFF
                        }
                    }
                    unicodeToGID.set(ch, gid)
                }
            }
            next = endCount[seg] + 1
        }
    }

    private func hmtx(_ table: FontTable) throws {
        self.index = table.offset!
        for i in 0..<advanceWidth.count {
            advanceWidth[i] = try readUInt16()
            index += 2
        }
    }

    private func post(_ table: FontTable) throws {
        self.index = table.offset!
        _ = try readUInt32()
        self.italicAngle = try readUInt32()
        self.underlinePosition  = try readInt16()
        self.underlineThickness = try readInt16()
    }

    private func CFF_(_ table: FontTable) throws {
        if table.offset! < 0 || table.length! < 0 || table.length! > buf.count - table.offset! {
            throw OTF.fontError("the CFF table is not in the font")
        }
        self.cff = true
        self.cffOff = table.offset!
        self.cffLen = table.length!
    }

    // Returns the 16 bits at the offset, or 0 outside the font data.
    private func uint16(at offset: Int) -> Int {
        if offset < 0 || offset + 2 > buf.count {
            return 0
        }
        return Int(buf[offset]) << 8 | Int(buf[offset + 1])
    }

    // Returns the 16 bits at the offset in the table, or nil when the table or
    // the font ends before them: a table can be shorter than its version says,
    // and the directory can point past the end of the font.
    private func tableUInt16(_ table: FontTable, _ at: Int) -> Int? {
        let offset = table.offset ?? -1
        let length = table.length ?? -1
        if at < 0 || offset < 0 || offset > buf.count || length < 0 ||
                at > length - 2 || at > buf.count - offset - 2 {
            return nil
        }
        return uint16(at: offset + at)
    }

    // Returns the 32 bits at the offset in the table, as tableUInt16 returns
    // 16.
    private func tableUInt32(_ table: FontTable, _ at: Int) -> Int? {
        guard let high = tableUInt16(table, at), let low = tableUInt16(table, at + 2) else {
            return nil
        }
        return high << 16 | low
    }

    private func int16(at offset: Int) -> Int {
        return Int(Int16(truncatingIfNeeded: uint16(at: offset)))
    }

    private func uint32(at offset: Int) -> Int {
        return uint16(at: offset) << 16 | uint16(at: offset + 2)
    }

    // Throws unless the next count bytes of the font are there. The index runs
    // past the end when a table of the directory points outside the font.
    private func need(_ count: Int) throws {
        if index < 0 || count > buf.count - index {
            throw OTF.fontError("the font ends too soon")
        }
    }

    private func readInt16() throws -> Int16 {
        try need(2)
        var val = Int16(buf[index]) << 8
        index += 1
        val |= Int16(buf[index])
        index += 1
        return val
    }

    private func readByte() throws -> UInt8 {
        try need(1)
        let val = buf[index]
        index += 1
        return val
    }

    @discardableResult
    private func readUInt16() throws -> UInt16 {
        try need(2)
        var val = UInt16(buf[index]) << 8
        index += 1
        val |= UInt16(buf[index])
        index += 1
        return val
    }

    @discardableResult
    private func readUInt32() throws -> UInt32 {
        try need(4)
        var val = UInt32(buf[index]) << 24
        index += 1
        val |= UInt32(buf[index]) << 16
        index += 1
        val |= UInt32(buf[index]) << 8
        index += 1
        val |= UInt32(buf[index])
        index += 1
        return val
    }

}   // End of OTF.swift
