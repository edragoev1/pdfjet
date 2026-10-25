/**
 * Kerning.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// How far apart pairs of glyphs go, from the font: the PairPos lookups of the
// kern feature of its GPOS table, pairs of glyphs (format 1) and pairs of
// classes of glyphs (format 2). Font.setKernPairs(true) turns it on; off,
// which it is unless asked for, nothing is read and nothing is slower. The
// kern table of an older TrueType font with no GPOS table is not read: none of
// the fonts PDFjet ships, nor any of a Linux system's, has one without GPOS,
// and such a font draws its text unkerned.
//
// The table is read when the font's kerning is turned on, once, under a
// lock, so that text is measured with it from any number of threads, and
// within the same limit of work as the marks of the GPOS table, so that a
// table that claims more than a font holds is stopped rather than read to an
// end it does not have.
final class Kerning {
    private let gposTable: GPOS     // The GPOS table, read with its reader
    private var read = false        // Set last, once the table is read
    private let readLock = NSLock()
    // The kern feature's lookups, in their order
    private var lookups = [[PairSubtable]]()
    // Made with the lookups, for the font: the glyphs kerned, not a mark, a
    // right to left letter or a glyph of no character, and the pairs of the
    // printable ASCII characters, looked up once, by asciiIndex
    private var kernable = [UInt64]()
    private var asciiIndex = [Int8]()   // By glyph, the ASCII character - 0x20 drawn with it, or -1
    private var asciiKern = [Int16]()

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair. Each pair drawn of a kerned font is looked up in them, so they
    // are arrays by glyph number, not dictionaries: a map lookup for each
    // subtable that a pair goes through made kerned text twice as slow in
    // Java, and four times in Go (25 October 2026).
    struct PairSubtable {
        var covered = [UInt64]()            // A bit for each first glyph covered
        var pairSets: [Int32]?              // Format 1, by first glyph: its pair set + 1, or 0
        var seconds = [[Int32]]()           // Format 1, the second glyphs of each pair set, in order
        var values = [[Int16]]()            // Format 1, the moves of the pairs of each pair set
        var firstClass = [UInt16]()         // Format 2, the class of each first glyph, 0 past the end
        var secondClass = [UInt16]()        // Format 2, the class of each second glyph, 0 past the end
        var secondCount = 0                 // Format 2, the classes of the second glyphs
        var classAdvance = [Int16]()        // Format 2, by first class * secondCount + second class

        @inline(__always)
        func covers(_ gid: Int) -> Bool {
            return Kerning.hasBit(covered, gid)
        }

        mutating func cover(_ gid: Int) {
            while gid >> 6 >= covered.count {
                covered.append(0)
            }
            covered[gid >> 6] |= 1 << UInt64(gid & 63)
        }

        // Returns the move of the pair, or nil when the pair set of the first
        // glyph has not the second, found by halves in its second glyphs,
        // which are in order.
        func pairValue(_ first: Int, _ second: Int) -> Int? {
            guard let pairSets = pairSets, first < pairSets.count, pairSets[first] != 0 else {
                return nil
            }
            let set = Int(pairSets[first]) - 1
            let glyphs = seconds[set]
            var lo = 0
            var hi = glyphs.count
            while lo < hi {
                let mid = (lo + hi) >> 1
                if Int(glyphs[mid]) < second {
                    lo = mid + 1
                } else {
                    hi = mid
                }
            }
            if lo < glyphs.count && Int(glyphs[lo]) == second {
                return Int(values[set][lo])
            }
            return nil
        }
    }

    @inline(__always)
    static func hasBit(_ bits: [UInt64], _ i: Int) -> Bool {
        return i >= 0 && i >> 6 < bits.count && bits[i >> 6] & (1 << UInt64(i & 63)) != 0
    }

    private init(_ gposTable: GPOS) {
        self.gposTable = gposTable
    }

    // Returns the kerning of the font, from its GPOS table, which the font has
    // already; nil when the font has none.
    static func of(_ gpos: GPOS?) -> Kerning? {
        guard let gpos = gpos else {
            return nil
        }
        return Kerning(gpos)
    }

    // Reads the table, the first time it is called, and makes the glyphs
    // kerned and the ASCII pairs of the font, when there is one; returns the
    // kerning.
    @discardableResult
    func readTables(_ font: Font?) -> Kerning {
        readLock.lock()
        defer { readLock.unlock() }
        if !read {
            let table = gposTable
            var reader = Reader(gpos: GPOS.Reader(
                    buf: table.buf, start: table.start, end: table.end, numGlyphs: table.numGlyphs))
            reader.readKernFeature()
            lookups = reader.lookups
            if let font = font {
                prepare(font)
            }
            read = true
        }
        return self
    }

    // Makes the glyphs the font kerns and the pairs of its printable ASCII
    // characters, so that most pairs drawn are an array read.
    private func prepare(_ font: Font) {
        let unicodeOf = font.unicodeOf
        var kernable = [UInt64](repeating: 0, count: (unicodeOf.count + 63) / 64)
        if unicodeOf.count > 1 {
            for gid in 1..<unicodeOf.count {
                let c = Int(unicodeOf[gid])
                if c >= 0 && !Page.isMark(c) && !Page.isRightToLeft(c) {
                    kernable[gid >> 6] |= 1 << UInt64(gid & 63)
                }
            }
        }
        var asciiIndex = [Int8](repeating: -1, count: unicodeOf.count)
        for i in 0..<95 {
            let gid = Int(font.asciiGID[i])
            if gid != 0 && gid < asciiIndex.count && asciiIndex[gid] < 0 {
                asciiIndex[gid] = Int8(i)
            }
        }
        var asciiKern = [Int16](repeating: 0, count: 95*95)
        for i in 0..<95 {
            let first = Int(font.asciiGID[i])
            for j in 0..<95 {
                let second = Int(font.asciiGID[j])
                if first != 0 && second != 0 && Kerning.hasBit(kernable, first) && Kerning.hasBit(kernable, second) {
                    asciiKern[i*95 + j] = Int16(truncatingIfNeeded: pair(first, second))
                }
            }
        }
        self.kernable = kernable
        self.asciiIndex = asciiIndex
        self.asciiKern = asciiKern
    }

    // Returns how far the second glyph moves after the first, in font units:
    // 0 unless both are kerned, from the ASCII pairs when both are printable
    // ASCII characters. The table was read when the kerning was turned on, so
    // no lock is taken for each pair.
    func glyphPair(_ first: Int, _ second: Int) -> Int {
        if first <= 0 || second <= 0 || !Kerning.hasBit(kernable, first) || !Kerning.hasBit(kernable, second) {
            return 0
        }
        if first < asciiIndex.count && second < asciiIndex.count {
            let a = Int(asciiIndex[first])
            let b = Int(asciiIndex[second])
            if a >= 0 && b >= 0 {
                return Int(asciiKern[a*95 + b])
            }
        }
        return pair(first, second)
    }

    // Returns how far the second glyph moves after the first one, in font
    // units: the advance of the first glyph, made shorter or longer. Each
    // lookup of the kern feature adds its own, from the first of its subtables
    // that covers the first glyph and has the pair, as HarfBuzz applies them.
    func pair(_ first: Int, _ second: Int) -> Int {
        if first < 0 || second < 0 {
            return 0
        }
        var total = 0
        for subtables in lookups {
            for s in subtables {
                if !s.covers(first) {
                    continue
                }
                if s.pairSets != nil {
                    if let v = s.pairValue(first, second) {
                        total += v
                        break
                    }
                    continue    // A pair set without the second glyph: the next subtable
                }
                let c1 = first < s.firstClass.count ? Int(s.firstClass[first]) : 0
                let c2 = second < s.secondClass.count ? Int(s.secondClass[second]) : 0
                if c2 >= s.secondCount || c1*s.secondCount + c2 >= s.classAdvance.count {
                    continue    // A class the subtable has no values for: the next subtable
                }
                total += Int(s.classAdvance[c1*s.secondCount + c2])
                break
            }
        }
        return total
    }

    // Reads the table: a struct, as GPOS.Reader is, whose values Swift reads
    // and changes directly.
    private struct Reader {
        var gpos: GPOS.Reader
        var lookups = [[PairSubtable]]()

        // Reads the lookups of the kern feature of the GPOS table, from the
        // default language of every script that has the feature, each lookup
        // once and in the order of the lookup list. The kern features of
        // other languages are left out, as HarfBuzz leaves them out of text of
        // no language.
        mutating func readKernFeature() {
            if gpos.end - gpos.start < 10 {
                return
            }
            let featureList = gpos.start + gpos.uint16(at: gpos.start + 6)
            let lookupList = gpos.start + gpos.uint16(at: gpos.start + 8)
            let featureCount = gpos.uint16(at: featureList)
            let lookupCount = gpos.uint16(at: lookupList)
            let defaults = defaultFeatures(featureCount)
            var kern = [Bool](repeating: false, count: lookupCount)
            var i = 0
            while i < featureCount && gpos.gposWork > 0 {
                gpos.gposWork -= 1
                let record = featureList + 2 + 6*i
                i += 1
                if !defaults[i - 1] {
                    continue
                }
                if gpos.uint16(at: record) != 0x6B65 || gpos.uint16(at: record + 2) != 0x726E {   // "kern"
                    continue
                }
                let feature = featureList + gpos.uint16(at: record + 4)
                let indexCount = min(gpos.uint16(at: feature + 2), gpos.gposWork)
                gpos.gposWork -= indexCount
                for j in 0..<indexCount {
                    let index = gpos.uint16(at: feature + 4 + 2*j)
                    if index < lookupCount {
                        kern[index] = true
                    }
                }
            }
            i = 0
            while i < lookupCount && gpos.gposWork > 0 {
                defer { i += 1 }
                if !kern[i] {
                    continue
                }
                let lookup = lookupList + gpos.uint16(at: lookupList + 2 + 2*i)
                let lookupType = gpos.uint16(at: lookup)
                let subTableCount = gpos.uint16(at: lookup + 4)
                var subtables = [PairSubtable]()
                var j = 0
                while j < subTableCount && gpos.gposWork > 0 {
                    gpos.gposWork -= 1
                    var subTable = lookup + gpos.uint16(at: lookup + 6 + 2*j)
                    j += 1
                    var subTableType = lookupType
                    if subTableType == 9 {
                        // An extension lookup holds the subtable at an offset of
                        // 32 bits, read unsigned, as the marks' lookups read it.
                        subTableType = gpos.uint16(at: subTable + 2)
                        let offset32 = Int64(gpos.uint16(at: subTable + 4)) << 16 |
                                Int64(gpos.uint16(at: subTable + 6))
                        if offset32 >= Int64(gpos.end - subTable) {
                            continue
                        }
                        subTable += Int(offset32)
                    }
                    if subTableType != 2 {
                        continue
                    }
                    switch gpos.uint16(at: subTable) {
                    case 1:
                        subtables.append(pairsSubtable(subTable))
                    case 2:
                        subtables.append(classesSubtable(subTable))
                    default:
                        break
                    }
                }
                if !subtables.isEmpty {
                    lookups.append(subtables)
                }
            }
        }

        // Returns the features of the default languages of the scripts of
        // the GPOS table, by their index in the feature list.
        mutating func defaultFeatures(_ featureCount: Int) -> [Bool] {
            var features = [Bool](repeating: false, count: featureCount)
            let scriptList = gpos.start + gpos.uint16(at: gpos.start + 4)
            let scriptCount = gpos.uint16(at: scriptList)
            var i = 0
            while i < scriptCount && gpos.gposWork > 0 {
                gpos.gposWork -= 1
                let script = scriptList + gpos.uint16(at: scriptList + 2 + 6*i + 4)
                i += 1
                let defaultLangSys = gpos.uint16(at: script)
                if defaultLangSys == 0 {
                    continue
                }
                let langSys = script + defaultLangSys
                let required = gpos.uint16(at: langSys + 2)
                if required < featureCount {
                    features[required] = true
                }
                let indexCount = min(gpos.uint16(at: langSys + 4), gpos.gposWork)
                gpos.gposWork -= indexCount
                for j in 0..<indexCount {
                    let index = gpos.uint16(at: langSys + 6 + 2*j)
                    if index < featureCount {
                        features[index] = true
                    }
                }
            }
            return features
        }

        // Returns the size of a value record of the format, in bytes, and the
        // offset of its XAdvance in it, or -1 when it has none.
        static func valueSize(_ format: Int) -> (size: Int, xAdvance: Int) {
            var size = 0
            var xAdvance = 0
            for bit in 0..<8 where format & (1 << bit) != 0 {
                if bit == 2 {
                    xAdvance = size
                }
                size += 2
            }
            if format & 0x0004 == 0 {
                xAdvance = -1
            }
            return (size, xAdvance)
        }

        // Reads a PairPos subtable of format 1: for each first glyph it
        // covers, a set of second glyphs, each with how far it moves.
        mutating func pairsSubtable(_ subTable: Int) -> PairSubtable {
            let firstGlyphs = gpos.coverage(subTable + gpos.uint16(at: subTable + 2))
            let (size1, xAdvance) = Reader.valueSize(gpos.uint16(at: subTable + 4))
            let size2 = Reader.valueSize(gpos.uint16(at: subTable + 6)).size
            let pairSetCount = gpos.uint16(at: subTable + 8)
            var s = PairSubtable()
            let maxFirst = firstGlyphs.prefix(pairSetCount).max() ?? -1
            var pairSets = [Int32](repeating: 0, count: maxFirst + 1)
            for (i, first) in firstGlyphs.enumerated() {
                if i >= pairSetCount || gpos.gposWork == 0 {
                    break
                }
                s.cover(first)
                let pairSet = subTable + gpos.uint16(at: subTable + 10 + 2*i)
                let pairCount = min(gpos.uint16(at: pairSet), gpos.gposWork)
                gpos.gposWork -= pairCount
                // Each pair, by its second glyph and then its record, the first
                // record of a pair kept, as it is the one applied
                var records = [Int]()
                records.reserveCapacity(pairCount)
                for j in 0..<pairCount {
                    let record = pairSet + 2 + j*(2 + size1 + size2)
                    let v = xAdvance >= 0 ? gpos.int16(at: record + 2 + xAdvance) : 0
                    records.append(gpos.uint16(at: record) << 40 | j << 16 | (v & 0xFFFF))
                }
                records.sort()
                var setSeconds = [Int32]()
                var setValues = [Int16]()
                for r in records {
                    let second = Int32(r >> 40)
                    if let last = setSeconds.last, last == second {
                        continue    // The first record of a pair is the one applied
                    }
                    setSeconds.append(second)
                    setValues.append(Int16(truncatingIfNeeded: r & 0xFFFF))
                }
                if pairSets[first] == 0 {   // A glyph covered twice: its first pair set
                    s.seconds.append(setSeconds)
                    s.values.append(setValues)
                    pairSets[first] = Int32(s.seconds.count)
                }
            }
            s.pairSets = pairSets
            return s
        }

        // Reads a PairPos subtable of format 2: the first glyphs it covers, the
        // class of each first and second glyph, and how far each pair of
        // classes moves the second glyph.
        mutating func classesSubtable(_ subTable: Int) -> PairSubtable {
            let firstGlyphs = gpos.coverage(subTable + gpos.uint16(at: subTable + 2))
            let (size1, xAdvance) = Reader.valueSize(gpos.uint16(at: subTable + 4))
            let size2 = Reader.valueSize(gpos.uint16(at: subTable + 6)).size
            let firstClasses = gpos.uint16(at: subTable + 12)
            let secondClasses = gpos.uint16(at: subTable + 14)
            var s = PairSubtable()
            s.secondCount = secondClasses
            for first in firstGlyphs {
                s.cover(first)
            }
            s.firstClass = classDef(subTable + gpos.uint16(at: subTable + 8))
            s.secondClass = classDef(subTable + gpos.uint16(at: subTable + 10))
            let count = firstClasses * secondClasses
            if count > gpos.gposWork {
                return PairSubtable()   // Covers nothing
            }
            gpos.gposWork -= count
            s.classAdvance = [Int16](repeating: 0, count: count)
            if xAdvance >= 0 {
                let record = subTable + 16
                for c in 0..<count {
                    s.classAdvance[c] = Int16(truncatingIfNeeded:
                            gpos.int16(at: record + c*(size1 + size2) + xAdvance))
                }
            }
            return s
        }

        // Reads a class definition table: the class of each glyph it gives
        // one, every other glyph of class 0.
        mutating func classDef(_ offset: Int) -> [UInt16] {
            var classes = [UInt16]()
            func set(_ glyph: Int, _ glyphClass: Int) {
                while glyph >= classes.count {
                    classes.append(0)
                }
                classes[glyph] = UInt16(truncatingIfNeeded: glyphClass)
            }
            let limit = gpos.numGlyphs > 0 ? gpos.numGlyphs : 0x10000
            switch gpos.uint16(at: offset) {
            case 1:
                let start = gpos.uint16(at: offset + 2)
                let count = min(gpos.uint16(at: offset + 4), gpos.gposWork, limit)
                gpos.gposWork -= count
                for i in 0..<count where start + i < limit {
                    let glyphClass = gpos.uint16(at: offset + 6 + 2*i)
                    if glyphClass != 0 {
                        set(start + i, glyphClass)
                    }
                }
            case 2:
                let rangeCount = min(gpos.uint16(at: offset + 2), gpos.gposWork)
                gpos.gposWork -= rangeCount
                var i = 0
                while i < rangeCount && gpos.gposWork > 0 {
                    let record = offset + 4 + 6*i
                    i += 1
                    let start = gpos.uint16(at: record)
                    let end = min(gpos.uint16(at: record + 2), limit - 1)
                    let glyphClass = gpos.uint16(at: record + 4)
                    var glyph = start
                    while glyph <= end && gpos.gposWork > 0 {
                        gpos.gposWork -= 1
                        if glyphClass != 0 {
                            set(glyph, glyphClass)
                        }
                        glyph += 1
                    }
                }
            default:
                break
            }
            return classes
        }
    }
}
