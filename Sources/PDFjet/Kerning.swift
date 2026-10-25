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

    // A PairPos subtable: the first glyphs it covers, and how far each pair
    // moves the second glyph, in font units, by the pair or by the classes of
    // the pair.
    struct PairSubtable {
        var covered = Set<Int>()
        var pairs: [UInt32: Int16]?         // Format 1, by (first << 16) | second
        var firstClass = [Int: Int]()       // Format 2, the class of each first glyph
        var secondClass = [Int: Int]()      // Format 2, the class of each second glyph
        var secondCount = 0                 // Format 2, the classes of the second glyphs
        var classAdvance = [Int16]()        // Format 2, by first class * secondCount + second class
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

    // Reads the table, the first time it is called, and returns the kerning.
    @discardableResult
    func readTables() -> Kerning {
        readLock.lock()
        defer { readLock.unlock() }
        if !read {
            let table = gposTable
            var reader = Reader(gpos: GPOS.Reader(
                    buf: table.buf, start: table.start, end: table.end, numGlyphs: table.numGlyphs))
            reader.readKernFeature()
            lookups = reader.lookups
            read = true
        }
        return self
    }

    // Returns how far the second glyph moves after the first one, in font
    // units: the advance of the first glyph, made shorter or longer. Each
    // lookup of the kern feature adds its own, from the first of its subtables
    // that covers the first glyph and has the pair, as HarfBuzz applies them.
    // The table was read when the kerning was turned on, so no lock is taken
    // for each pair.
    func pair(_ first: Int, _ second: Int) -> Int {
        let key = UInt32(truncatingIfNeeded: first) << 16 | UInt32(truncatingIfNeeded: second)
        var total = 0
        for subtables in lookups {
            for s in subtables {
                if !s.covered.contains(first) {
                    continue
                }
                if let pairs = s.pairs {
                    if let v = pairs[key] {
                        total += Int(v)
                        break
                    }
                    continue    // A pair set without the second glyph: the next subtable
                }
                let c1 = s.firstClass[first] ?? 0
                let c2 = s.secondClass[second] ?? 0
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
            var pairs = [UInt32: Int16]()
            for (i, first) in firstGlyphs.enumerated() {
                if i >= pairSetCount || gpos.gposWork == 0 {
                    break
                }
                s.covered.insert(first)
                let pairSet = subTable + gpos.uint16(at: subTable + 10 + 2*i)
                let pairCount = min(gpos.uint16(at: pairSet), gpos.gposWork)
                gpos.gposWork -= pairCount
                for j in 0..<pairCount {
                    let record = pairSet + 2 + j*(2 + size1 + size2)
                    let second = gpos.uint16(at: record)
                    let key = UInt32(first) << 16 | UInt32(second)
                    if pairs[key] != nil {
                        continue    // The first record of a pair is the one applied
                    }
                    let v = xAdvance >= 0 ? gpos.int16(at: record + 2 + xAdvance) : 0
                    pairs[key] = Int16(truncatingIfNeeded: v)
                }
            }
            s.pairs = pairs
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
            s.covered.reserveCapacity(firstGlyphs.count)
            for first in firstGlyphs {
                s.covered.insert(first)
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
        mutating func classDef(_ offset: Int) -> [Int: Int] {
            var classes = [Int: Int]()
            let limit = gpos.numGlyphs > 0 ? gpos.numGlyphs : 0x10000
            switch gpos.uint16(at: offset) {
            case 1:
                let start = gpos.uint16(at: offset + 2)
                let count = min(gpos.uint16(at: offset + 4), gpos.gposWork, limit)
                gpos.gposWork -= count
                for i in 0..<count {
                    let glyphClass = gpos.uint16(at: offset + 6 + 2*i)
                    if glyphClass != 0 {
                        classes[start + i] = glyphClass
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
                            classes[glyph] = glyphClass
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
