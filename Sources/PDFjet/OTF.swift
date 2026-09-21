/**
 * OTF.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

struct FontTable {
    var name: String?
    var checkSum: UInt32?
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
    var firstChar: Int? = 0
    var lastChar: Int? = 0
    var capHeight: Int16? = 0
    var postVersion: UInt32? = 0
    var italicAngle: UInt32? = 0
    var underlinePosition: Int16? = 0
    var underlineThickness: Int16? = 0

    var buf = [UInt8]()
    var dos = [UInt8]()
    var cff = false
    private var cffOff: Int?
    private var cffLen: Int?
    private var index = 0
    private var gposWork = maxGposWork

    // The most work the GPOS table of a font is read with, counted in the
    // glyphs of the coverage tables it names and in the pairs its mark
    // lookups place: each mark against each letter or mark it goes on. The
    // lookups, the subtables and the ranges of a coverage table all say their
    // own count, so a table that claims more than a font holds is stopped
    // here rather than read to an end it does not have. Of the 252 fonts
    // PDFjet ships the most this takes is 62,954, in Noto Sans.
    private static let maxGposWork = 1 << 20

    private static func fontError(_ what: String) -> PDFjetError {
        return PDFjetError(message: "Invalid font file: \(what).")
    }

    var unicodeToGID = [Int](repeating: 0, count: 0x10000)
    var markToMarkOffsets: [Int: [Int]]?
    var markAnchors: [[Int: [Int]]]?
    var baseAnchors: [[Int: [Int]]]?

    init(_ stream: InputStream) throws {
        buf = try Content.getFromStream(stream)

        // Extract OTF metadata
        let version = try readUInt32()

        if version == 0x00010000 ||     // Win OTF
            version == 0x74727565 ||    // Mac TTF
            version == 0x4F54544F {     // CFF OTF
            // We should be able to read this font
        } else {
            throw PDFjetError(message: "OTF version == \(version) is not supported.")
        }

        let numOfTables = try readUInt16()  // numOfTables
        try readUInt16()                    // searchRange
        try readUInt16()                    // entrySelector
        try readUInt16()                    // rangeShift

        var cmapTable: FontTable?
        for _ in 0..<numOfTables {
            var name = [UInt8](repeating: 0, count: 4)
            for i in 0..<4 {
                name[i] = try readByte()
            }
            var table = FontTable()
            table.name = String(bytes: name, encoding: .utf8)
            table.checkSum = try readUInt32()
            table.offset = Int(try readUInt32())
            table.length = Int(try readUInt32())

            let k = index   // Save the current index
            if      table.name == "head" { try head(table) }
            else if table.name == "hhea" { try hhea(table) }
            else if table.name == "OS/2" { try OS_2(table) }
            else if table.name == "name" { try n4me(table) }
            else if table.name == "hmtx" { try hmtx(table) }
            else if table.name == "post" { try post(table) }
            else if table.name == "CFF " { try CFF_(table) }
            else if table.name == "GPOS" { GPOS(table) }
            else if table.name == "cmap" { cmapTable = table }
            index = k       // Restore the index
        }

        // This table must be processed last
        guard let cmapTable = cmapTable else {
            throw OTF.fontError("no character map")
        }
        try cmap(cmapTable)

        // The sizes of the text are divided by the units per em, and the
        // width of every glyph past the advance widths is that of the last one.
        if unitsPerEm == nil || unitsPerEm! < 16 || unitsPerEm! > 16384 {
            throw OTF.fontError("the units per em")
        }
        if advanceWidth.isEmpty {
            throw OTF.fontError("no advance widths")
        }
        // The name goes into the PDF as the name of the font, as the name of
        // a stream font does.
        if fontName == nil || !FontStream1.isFontName(Array(fontName!.utf8)) {
            throw OTF.fontError("the font name")
        }

        if cff {
            let bufSlice = Array(buf[cffOff!..<(cffOff! + cffLen!)])
            FlateEncode(&dos, bufSlice)
        } else {
            FlateEncode(&dos, buf)
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
        index = table.offset! + 64
        firstChar = Int(try readUInt16())
        lastChar  = Int(try readUInt16())
        index += 20
        capHeight = try readInt16()
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

        // Process the encoding records
        var format4subtable = false
        var subtableOffset = 0
        for _ in 0..<numRecords {
            let platformID = try readUInt16()
            let encodingID = try readUInt16()
            subtableOffset = Int(try readUInt32())
            if platformID == 3 && encodingID == 1 {
                format4subtable = true
                break
            }
        }
        if !format4subtable {
            throw OTFError.format4SubtableNotFound
        }

        self.index = tableOffset + subtableOffset

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
        for ch in firstChar!...lastChar! {
            let seg = getSegmentFor(ch, startCount, endCount, Int(segCount))
            if seg != -1 {
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
                unicodeToGID[ch] = gid
            }
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
        self.postVersion = try readUInt32()
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

    private func getSegmentFor(
            _ ch: Int,
            _ startCount: [Int],
            _ endCount: [Int],
            _ segCount: Int) -> Int {
        var segment = -1
        for i in 0..<segCount {
            if ch <= endCount[i] && ch >= startCount[i] {
                segment = i
                break
            }
        }
        return segment
    }

    // Reads where the marks go from the GPOS table: the marks on letters and
    // ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
    // MarkToLigature lookups, and the marks that attach to other marks, like a
    // Thai tone mark above an upper vowel, from its MarkToMark lookups.
    private func GPOS(_ table: FontTable) {
        markToMarkOffsets = [Int: [Int]]()
        markAnchors = [[Int: [Int]]]()
        baseAnchors = [[Int: [Int]]]()
        let lookupList = table.offset! + uint16(at: table.offset! + 8)
        let lookupCount = uint16(at: lookupList)
        for i in 0..<lookupCount {
            if gposWork <= 0 {
                break
            }
            let lookup = lookupList + uint16(at: lookupList + 2 + 2*i)
            let lookupType = uint16(at: lookup)
            let subTableCount = uint16(at: lookup + 4)
            for j in 0..<subTableCount {
                if gposWork <= 0 {
                    break
                }
                gposWork -= 1
                var subTable = lookup + uint16(at: lookup + 6 + 2*j)
                var type = lookupType
                if type == 9 {  // An extension lookup holds the subtable
                    type = uint16(at: subTable + 2)
                    subTable += uint32(at: subTable + 4)
                }
                if type == 4 && uint16(at: subTable) == 1 {
                    markToBase(subTable, false)
                } else if type == 5 && uint16(at: subTable) == 1 {
                    markToBase(subTable, true)
                } else if type == 6 && uint16(at: subTable) == 1 {
                    markToMark(subTable)
                }
            }
        }
    }

    // Keeps the anchors of a MarkToBase or MarkToLigature subtable, by glyph
    // ID: the class and anchor of each mark, and an anchor of each letter for
    // each class of marks, with 1 before an anchor that is there and 0 before
    // one that is not. A ligature has anchors for each of the letters it joins,
    // and keeps those of its first letter, like the lam of a lam-alef ligature.
    private func markToBase(_ subTable: Int, _ ligature: Bool) {
        let markGlyphs = coverage(subTable + uint16(at: subTable + 2))
        let baseGlyphs = coverage(subTable + uint16(at: subTable + 4))
        let classCount = uint16(at: subTable + 6)
        let markArray = subTable + uint16(at: subTable + 8)
        let baseArray = subTable + uint16(at: subTable + 10)
        var marks = [Int: [Int]]()
        for (m, glyph) in markGlyphs.enumerated() {
            let anchor = markArray + uint16(at: markArray + 4 + 4*m)
            marks[glyph] = [
                    uint16(at: markArray + 2 + 4*m), int16(at: anchor + 2), int16(at: anchor + 4)]
        }
        var bases = [Int: [Int]]()
        for (b, glyph) in baseGlyphs.enumerated() {
            if gposWork < classCount {
                break
            }
            gposWork -= classCount
            // The anchor offsets are from the base array, or from the ligature
            // attach table of a ligature.
            var table = baseArray
            var record = baseArray + 2 + 2*b*classCount
            if ligature {
                table = baseArray + uint16(at: baseArray + 2 + 2*b)
                record = table + 2
                if uint16(at: table) == 0 {     // No letters
                    continue
                }
            }
            var anchors = [Int](repeating: 0, count: 3*classCount)
            for c in 0..<classCount {
                let anchorOffset = uint16(at: record + 2*c)
                if anchorOffset != 0 {
                    anchors[3*c] = 1
                    anchors[3*c + 1] = int16(at: table + anchorOffset + 2)
                    anchors[3*c + 2] = int16(at: table + anchorOffset + 4)
                }
            }
            bases[glyph] = anchors
        }
        markAnchors!.append(marks)
        baseAnchors!.append(bases)
    }

    // Keeps the offset of each mark from the mark it attaches to, in font
    // units, by the glyph IDs of the two marks. The first lookup that has a
    // pair of marks places them.
    private func markToMark(_ subTable: Int) {
        let mark1Glyphs = coverage(subTable + uint16(at: subTable + 2))
        let mark2Glyphs = coverage(subTable + uint16(at: subTable + 4))
        let classCount = uint16(at: subTable + 6)
        let mark1Array = subTable + uint16(at: subTable + 8)
        let mark2Array = subTable + uint16(at: subTable + 10)
        for (m1, glyph1) in mark1Glyphs.enumerated() {
            if gposWork < mark2Glyphs.count {
                break
            }
            gposWork -= mark2Glyphs.count
            let markClass = uint16(at: mark1Array + 2 + 4*m1)
            let anchor1 = mark1Array + uint16(at: mark1Array + 4 + 4*m1)
            for (m2, glyph2) in mark2Glyphs.enumerated() {
                let anchorOffset = uint16(at: mark2Array + 2 + 2*(m2*classCount + markClass))
                if anchorOffset == 0 {
                    continue
                }
                let anchor2 = mark2Array + anchorOffset
                let key = (glyph2 << 16) | glyph1
                if markToMarkOffsets![key] == nil {
                    markToMarkOffsets![key] = [
                            int16(at: anchor2 + 2) - int16(at: anchor1 + 2),
                            int16(at: anchor2 + 4) - int16(at: anchor1 + 4)]
                }
            }
        }
    }

    // Returns the glyph IDs of a coverage table, in the order of their coverage indexes.
    private func coverage(_ offset: Int) -> [Int] {
        let format = uint16(at: offset)
        var count = uint16(at: offset + 2)
        if count > gposWork {
            count = gposWork
        }
        gposWork -= count
        if format == 1 {
            var glyphs = [Int](repeating: 0, count: count)
            for i in 0..<count {
                glyphs[i] = uint16(at: offset + 4 + 2*i)
            }
            return glyphs
        }
        // Format 2 has ranges of glyphs, each with the coverage index of its first glyph.
        var size = 0
        for i in 0..<count {
            let range = offset + 4 + 6*i
            let start = uint16(at: range)
            let end = uint16(at: range + 2)
            if end >= start {
                size = max(size, uint16(at: range + 4) + end - start + 1)
            }
        }
        // A coverage table lists each glyph once, and a glyph ID is 16 bits.
        if size > 0x10000 {
            size = 0x10000
        }
        var glyphs = [Int](repeating: 0, count: size)
        for i in 0..<count {
            if gposWork <= 0 {
                break
            }
            let range = offset + 4 + 6*i
            let start = uint16(at: range)
            let end = uint16(at: range + 2)
            let coverageIndex = uint16(at: range + 4)
            if end >= start {
                for glyph in start...end {
                    if gposWork <= 0 {
                        break
                    }
                    gposWork -= 1
                    let index = coverageIndex + glyph - start
                    if index < size {
                        glyphs[index] = glyph
                    }
                }
            }
        }
        return glyphs
    }

    // The GPOS table is read at offsets from its subtables. A value outside
    // the font data is read as 0, so a broken table cannot stop the font from
    // loading.
    private func uint16(at offset: Int) -> Int {
        if offset < 0 || offset + 2 > buf.count {
            return 0
        }
        return Int(buf[offset]) << 8 | Int(buf[offset + 1])
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
