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
    var bBoxLLx: Int16?
    var bBoxLLy: Int16?
    var bBoxURx: Int16?
    var bBoxURy: Int16?
    var ascent: Int16?
    var descent: Int16?
    var advanceWidth: [UInt16] = []
    var firstChar: Int?
    var lastChar: Int?
    var capHeight: Int16?
    var postVersion: UInt32?
    var italicAngle: UInt32?
    var underlinePosition: Int16?
    var underlineThickness: Int16?

    var buf = [UInt8]()
    var dos = [UInt8]()
    var cff = false
    private var cffOff: Int?
    private var cffLen: Int?
    private var index = 0

    var unicodeToGID = [Int](repeating: 0, count: 0x10000)
    var markToMarkOffsets: [Int: [Int]]?
    var markAnchors: [[Int: [Int]]]?
    var baseAnchors: [[Int: [Int]]]?

    init(_ stream: InputStream) throws {
        buf = try Content.getFromStream(stream)

        // Extract OTF metadata
        let version = readUInt32()

        if version == 0x00010000 ||     // Win OTF
            version == 0x74727565 ||    // Mac TTF
            version == 0x4F54544F {     // CFF OTF
            // We should be able to read this font
        } else {
            Swift.print("OTF version == \(version) is not supported.")
        }

        let numOfTables = readUInt16()      // numOfTables
        readUInt16()                        // searchRange
        readUInt16()                        // entrySelector
        readUInt16()                        // rangeShift

        var cmapTable: FontTable?
        for _ in 0..<numOfTables {
            var name = [UInt8](repeating: 0, count: 4)
            for i in 0..<4 {
                name[i] = readByte()
            }
            var table = FontTable()
            table.name = String(bytes: name, encoding: .utf8)
            table.checkSum = readUInt32()
            table.offset = Int(readUInt32())
            table.length = Int(readUInt32())

            let k = index   // Save the current index
            if      table.name == "head" { head(table) }
            else if table.name == "hhea" { hhea(table) }
            else if table.name == "OS/2" { OS_2(table) }
            else if table.name == "name" { n4me(table) }
            else if table.name == "hmtx" { hmtx(table) }
            else if table.name == "post" { post(table) }
            else if table.name == "CFF " { CFF_(table) }
            else if table.name == "GPOS" { GPOS(table) }
            else if table.name == "cmap" { cmapTable = table }
            index = k       // Restore the index
        }

        // This table must be processed last
        try cmap(cmapTable!)

        if cff {
            let bufSlice = Array(buf[cffOff!..<(cffOff! + cffLen!)])
            FlateEncode(&dos, bufSlice)
        } else {
            FlateEncode(&dos, buf)
        }
    }

    private func head(_ table: FontTable) {
        self.index = table.offset! + 16
        readUInt16()                    // flags
        unitsPerEm = Int(readUInt16())
        self.index += 16
        self.bBoxLLx = readInt16()
        self.bBoxLLy = readInt16()
        self.bBoxURx = readInt16()
        self.bBoxURy = readInt16()
    }

    private func hhea(_ table: FontTable) {
        self.index = table.offset! + 4
        self.ascent  = readInt16()
        self.descent = readInt16()
        self.index += 26
        self.advanceWidth = [UInt16](repeating: 0, count: Int(readUInt16()))
    }

    private func OS_2(_ table: FontTable) {
        index = table.offset! + 64
        firstChar = Int(readUInt16())
        lastChar  = Int(readUInt16())
        index += 20
        capHeight = readInt16()
    }

    private func n4me(_ table: FontTable) {
        self.index = table.offset!
        readUInt16()                // format
        let count = readUInt16()
        let stringOffset = readUInt16()

        var macFontInfo = ""
        var winFontInfo = ""
        for _ in 0..<count {
            let platformID = readUInt16()
            let encodingID = readUInt16()
            let languageID = readUInt16()
            let nameID = readUInt16()
            let length = readUInt16()
            let offset = readUInt16()

            if platformID == 1 && encodingID == 0 && languageID == 0 {
                // Macintosh
                let index2 = Int(table.offset!) + Int(stringOffset) + Int(offset)
                let buffer = buf[index2..<(index2 + Int(length))]
                let str = String(bytes: buffer, encoding: .utf8)
                if nameID == 6 {
                    fontName = str
                } else {
                    if str != nil {
                        macFontInfo.append(str!)
                        macFontInfo.append("\n")
                    }
                }
            } else if platformID == 3 && encodingID == 1 && languageID == 0x409 {
                // Windows
                let index2 = Int(table.offset!) + Int(stringOffset) + Int(offset)
                let buffer = buf[index2..<(index2 + Int(length))]
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
        let numRecords = readUInt16()

        // Process the encoding records
        var format4subtable = false
        var subtableOffset = 0
        for _ in 0..<numRecords {
            let platformID = readUInt16()
            let encodingID = readUInt16()
            subtableOffset = Int(readUInt32())
            if platformID == 3 && encodingID == 1 {
                format4subtable = true
                break
            }
        }
        if !format4subtable {
            throw OTFError.format4SubtableNotFound
        }

        self.index = tableOffset + subtableOffset

        readUInt16()        // format
        let tableLen = readUInt16()
        readUInt16()        // language
        let segCount = Int(readUInt16() / 2)

        index += 6          // Skip to the endCount[]
        var endCount = [Int](repeating: 0, count: Int(segCount))
        var i = 0
        while i < segCount {
            endCount[i] = Int(readUInt16())
            i += 1
        }

        index += 2          // Skip the reservedPad
        var startCount = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            startCount[i] = Int(readUInt16())
            i += 1
        }

        var idDelta = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idDelta[i] = Int(readUInt16())
            i += 1
        }

        var idRangeOffset = [Int](repeating: 0, count: Int(segCount))
        i = 0
        while i < segCount {
            idRangeOffset[i] = Int(readUInt16())
            i += 1
        }

        var glyphIdArray = [Int](repeating: 0, count: Int((Int(tableLen) - Int(16 + 8*segCount)) / 2))
        i = 0
        while i < glyphIdArray.count {
            glyphIdArray[i] = Int(readUInt16())
            i += 1
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
                    gid = glyphIdArray[offset + (ch - startCount[seg])]
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

    private func hmtx(_ table: FontTable) {
        self.index = table.offset!
        for i in 0..<advanceWidth.count {
            advanceWidth[i] = readUInt16()
            index += 2
        }
    }

    private func post(_ table: FontTable) {
        self.index = table.offset!
        self.postVersion = readUInt32()
        self.italicAngle = readUInt32()
        self.underlinePosition  = readInt16()
        self.underlineThickness = readInt16()
    }

    private func CFF_(_ table: FontTable) {
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
            let lookup = lookupList + uint16(at: lookupList + 2 + 2*i)
            let lookupType = uint16(at: lookup)
            let subTableCount = uint16(at: lookup + 4)
            for j in 0..<subTableCount {
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
        let count = uint16(at: offset + 2)
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
        var glyphs = [Int](repeating: 0, count: size)
        for i in 0..<count {
            let range = offset + 4 + 6*i
            let start = uint16(at: range)
            let end = uint16(at: range + 2)
            let coverageIndex = uint16(at: range + 4)
            if end >= start {
                for glyph in start...end {
                    glyphs[coverageIndex + glyph - start] = glyph
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

    private func readInt16() -> Int16 {
        var val = Int16(buf[index]) << 8
        index += 1
        val |= Int16(buf[index])
        index += 1
        return val
    }

    private func readByte() -> UInt8 {
        let val = buf[index]
        index += 1
        return val
    }

    @discardableResult
    private func readUInt16() -> UInt16 {
        var val = UInt16(buf[index]) << 8
        index += 1
        val |= UInt16(buf[index])
        index += 1
        return val
    }

    @discardableResult
    private func readUInt32() -> UInt32 {
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
