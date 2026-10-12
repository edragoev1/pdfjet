/**
 * GPOS.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// Where the marks go, from the GPOS table of a font: the marks on letters and
// ligatures, like Hebrew and Arabic vowel marks, from its MarkToBase and
// MarkToLigature lookups, and the marks that attach to other marks, like a
// Thai tone mark above an upper vowel, from its MarkToMark lookups.
//
// The table is read the first time a mark is drawn, not when the font is
// loaded: most documents draw no mark, and reading every anchor of a font of
// many scripts, as Noto Sans, took most of the time of a page drawn in it
// and much of what it allocated.
final class GPOS {
    // The most work the GPOS table of a font is read with, counted in the
    // glyphs of the coverage tables it names, the glyphs their indexes make
    // room for, the marks and the letters its mark lookups keep and the pairs
    // they place: each mark against each letter or mark it goes on. The
    // lookups, the subtables, the ranges of a coverage table and its indexes
    // all say their own count, and subtables can share one another, so a
    // table that claims more than a font holds is stopped here rather than
    // read to an end it does not have. Of the 252 fonts PDFjet ships the most
    // this takes is 76,168, in Noto Sans.
    private static let maxGposWork = 1 << 20

    private let buf: [UInt8]    // The font data the table is in
    private let start: Int      // The table, from start to end of buf
    private let end: Int
    private let numGlyphs: Int  // Of the maxp table, or 0 when the font has none
    private var read = false

    // The offsets of the marks that attach to other marks, in font units, by
    // the glyph IDs of the two marks.
    private(set) var markToMarkOffsets = [Int: [Int]]()
    // The anchors of the marks, by glyph ID, for each MarkToBase and
    // MarkToLigature lookup subtable: the class and anchor of each mark.
    private(set) var markAnchors = [[Int: [Int]]]()
    // The anchors of the letters and ligatures that the marks go on, by glyph
    // ID, for each subtable in markAnchors: 1 and an anchor, or 0 and no
    // anchor, for each class of marks.
    private(set) var baseAnchors = [[Int: [Int]]]()

    // The GPOS table from start to end of the font data, which is kept until
    // the table is read. A value outside the table is read as 0, so a broken
    // table cannot stop the font from loading, or a mark from being drawn.
    private init(_ buf: [UInt8], _ start: Int, _ end: Int, _ numGlyphs: Int) {
        self.buf = buf
        self.start = start
        self.end = end
        self.numGlyphs = numGlyphs
    }

    // Returns the GPOS table of the font data: the font data itself, which
    // the font keeps, or a copy of the table when the font keeps only a part
    // of it, as a CFF font keeps its CFF table.
    static func of(_ buf: [UInt8], _ offset: Int, _ length: Int, _ numGlyphs: Int, _ copy: Bool) -> GPOS {
        let start = max(min(offset, buf.count), 0)
        let end = length < 0 ? start : min(start + length, buf.count)
        if copy {
            return GPOS(Array(buf[start..<end]), 0, end - start, numGlyphs)
        }
        return GPOS(buf, start, end, numGlyphs)
    }

    // Reads the table, the first time it is called, and returns it.
    func readTable() -> GPOS {
        if !read {
            read = true
            var reader = Reader(buf: buf, start: start, end: end, numGlyphs: numGlyphs)
            reader.lookups()
            markToMarkOffsets = reader.markToMarkOffsets
            markAnchors = reader.markAnchors
            baseAnchors = reader.baseAnchors
        }
        return self
    }

    // Reads the table: a struct, whose values Swift reads and changes
    // directly, where the properties of a class cost it more.
    private struct Reader {
        let buf: [UInt8]
        let start: Int
        let end: Int
        let numGlyphs: Int
        var gposWork = GPOS.maxGposWork
        var markToMarkOffsets = [Int: [Int]]()
        var markAnchors = [[Int: [Int]]]()
        var baseAnchors = [[Int: [Int]]]()

        mutating func lookups() {
            let lookupList = start + uint16(at: start + 8)
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
        mutating func markToBase(_ subTable: Int, _ ligature: Bool) {
            let markGlyphs = coverage(subTable + uint16(at: subTable + 2))
            let baseGlyphs = coverage(subTable + uint16(at: subTable + 4))
            let classCount = uint16(at: subTable + 6)
            let markArray = subTable + uint16(at: subTable + 8)
            let baseArray = subTable + uint16(at: subTable + 10)
            var marks = [Int: [Int]]()
            for (m, glyph) in markGlyphs.enumerated() {
                if gposWork == 0 {
                    break
                }
                gposWork -= 1
                let anchor = markArray + uint16(at: markArray + 4 + 4*m)
                marks[glyph] = [
                        uint16(at: markArray + 2 + 4*m), int16(at: anchor + 2), int16(at: anchor + 4)]
            }
            var bases = [Int: [Int]]()
            for (b, glyph) in baseGlyphs.enumerated() {
                // A letter is work of its own, whatever the classes of its marks.
                let work = max(classCount, 1)
                if gposWork < work {
                    break
                }
                gposWork -= work
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
            markAnchors.append(marks)
            baseAnchors.append(bases)
        }

        // Keeps the offset of each mark from the mark it attaches to, in font
        // units, by the glyph IDs of the two marks. The first lookup that has a
        // pair of marks places them.
        mutating func markToMark(_ subTable: Int) {
            let mark1Glyphs = coverage(subTable + uint16(at: subTable + 2))
            let mark2Glyphs = coverage(subTable + uint16(at: subTable + 4))
            let classCount = uint16(at: subTable + 6)
            let mark1Array = subTable + uint16(at: subTable + 8)
            let mark2Array = subTable + uint16(at: subTable + 10)
            for (m1, glyph1) in mark1Glyphs.enumerated() {
                let work = max(mark2Glyphs.count, 1)
                if gposWork < work {
                    break
                }
                gposWork -= work
                let markClass = uint16(at: mark1Array + 2 + 4*m1)
                let anchor1 = mark1Array + uint16(at: mark1Array + 4 + 4*m1)
                for (m2, glyph2) in mark2Glyphs.enumerated() {
                    let anchorOffset = uint16(at: mark2Array + 2 + 2*(m2*classCount + markClass))
                    if anchorOffset == 0 {
                        continue
                    }
                    let anchor2 = mark2Array + anchorOffset
                    let key = (glyph2 << 16) | glyph1
                    if markToMarkOffsets[key] == nil {
                        markToMarkOffsets[key] = [
                                int16(at: anchor2 + 2) - int16(at: anchor1 + 2),
                                int16(at: anchor2 + 4) - int16(at: anchor1 + 4)]
                    }
                }
            }
        }

        // Returns the glyph IDs of a coverage table, in the order of their coverage indexes.
        mutating func coverage(_ offset: Int) -> [Int] {
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
            // A coverage table lists each glyph of the font once at most, and the
            // room its indexes make is counted as work, as each glyph of it is
            // gone through by the lookup it is of.
            let limit = numGlyphs > 0 ? numGlyphs : 0x10000     // A glyph ID is 16 bits.
            size = min(size, limit, gposWork)
            gposWork -= size
            var glyphs = [Int](repeating: 0, count: size)
            for i in 0..<count {
                if gposWork <= 0 {
                    break
                }
                let range = offset + 4 + 6*i
                let start = uint16(at: range)
                let end = uint16(at: range + 2)
                let coverageIndex = uint16(at: range + 4)
                // The glyphs of the range past the room of the indexes are left out.
                let last = min(end, start + size - 1 - coverageIndex)
                if last >= start {
                    for glyph in start...last {
                        if gposWork <= 0 {
                            break
                        }
                        gposWork -= 1
                        glyphs[coverageIndex + glyph - start] = glyph
                    }
                }
            }
            return glyphs
        }

        // Returns the 16 bits at the offset, or 0 outside the table.
        func uint16(at offset: Int) -> Int {
            if offset < start || offset > end - 2 {
                return 0
            }
            return Int(buf[offset]) << 8 | Int(buf[offset + 1])
        }

        func int16(at offset: Int) -> Int {
            return Int(Int16(truncatingIfNeeded: uint16(at: offset)))
        }

        func uint32(at offset: Int) -> Int {
            return uint16(at: offset) << 16 | uint16(at: offset + 2)
        }
    }
}
