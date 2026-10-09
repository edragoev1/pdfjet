/**
 * CFFSubset.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The subset of a font with CFF outlines, a .otf: the CFF table with the
/// charstrings of the glyphs the document does not draw emptied, and the
/// subroutines, global and local, that the glyphs kept do not call. Every glyph
/// and every subroutine keeps its number, so the glyphs kept are drawn by the
/// same bytes as in the whole font. The CFF is written again in a fixed order,
/// with every offset of its Top DICT, of the Font DICTs of a CID-keyed font and
/// of its Private DICTs in the five-byte form, so that an offset of any value
/// has the same size: the header, the Name, Top DICT, String and Global Subr
/// INDEXes, the charset, the Encoding and the FDSelect, the new CharStrings
/// INDEX, the FDArray, and each Private DICT with its local subroutines after
/// it.
class CFFSubset {
    // The operators whose operands are offsets in the CFF table.
    static let charset = 15
    static let encoding = 16
    static let charStrings = 17
    static let privateOp = 18
    static let subrsOp = 19
    static let fdArray = 12 << 8 | 36
    static let fdSelect = 12 << 8 | 37
    static let ros = 12 << 8 | 30
    static let cidCount = 12 << 8 | 34

    // The most bytes of charstrings the subroutines are looked for in, which a
    // font of nested calls that come back to the same subroutines could
    // otherwise make take as long as it likes.
    static let maxWork = 1 << 24

    /// An INDEX: where it starts and ends, and the offsets of its objects.
    struct Index {
        let start: Int
        let end: Int
        let objects: [Int]  // count+1 offsets, the last one the end of the data

        var count: Int { objects.count - 1 }
    }

    /// An entry of a DICT: its operator, its bytes, its integer operands.
    struct Entry {
        let op: Int             // 12 << 8 | b1 for an escaped one
        let raw: [UInt8]
        let integers: [Int]?    // nil when an operand is a real
    }

    /// A Private DICT, its entries and the INDEX of its subroutines or nil.
    struct PrivateDict {
        let entries: [Entry]
        let subrs: Index?
    }

    static func readIndex(_ cff: [UInt8], _ at: Int) throws -> Index {
        if at < 0 || at + 2 > cff.count {
            throw Subset.NotSubset()
        }
        let count = Int(cff[at]) << 8 | Int(cff[at + 1])
        if count == 0 {
            return Index(start: at, end: at + 2, objects: [at + 2])
        }
        if at + 3 > cff.count {
            throw Subset.NotSubset()
        }
        let offSize = Int(cff[at + 2])
        if offSize < 1 || offSize > 4 || at + 3 + (count + 1) * offSize > cff.count {
            throw Subset.NotSubset()
        }
        let data = at + 3 + (count + 1) * offSize - 1   // The offsets count from 1.
        var objects = [Int](repeating: 0, count: count + 1)
        for i in 0...count {
            var offset = 0
            for j in 0..<offSize {
                offset = offset << 8 | Int(cff[at + 3 + i * offSize + j])
            }
            if offset < 1 || data + offset > cff.count || (i > 0 && data + offset < objects[i - 1]) {
                throw Subset.NotSubset()
            }
            objects[i] = data + offset
        }
        return Index(start: at, end: objects[count], objects: objects)
    }

    static func appendIndex(_ out: inout [UInt8], _ objects: [[UInt8]]) {
        out.append(UInt8(truncatingIfNeeded: objects.count >> 8))
        out.append(UInt8(truncatingIfNeeded: objects.count))
        if objects.isEmpty {
            return
        }
        var size = 1
        for object in objects {
            size += object.count
        }
        var offSize = 1
        while offSize < 4 && size >= 1 << (8 * offSize) {
            offSize += 1
        }
        out.append(UInt8(offSize))
        var offset = 1
        for i in 0...objects.count {
            for j in stride(from: offSize - 1, through: 0, by: -1) {
                out.append(UInt8(truncatingIfNeeded: offset >> (8 * j)))
            }
            if i < objects.count {
                offset += objects[i].count
            }
        }
        for object in objects {
            out.append(contentsOf: object)
        }
    }

    static func index(_ objects: [[UInt8]]) -> [UInt8] {
        var out = [UInt8]()
        appendIndex(&out, objects)
        return out
    }

    static func readDict(_ cff: [UInt8], _ from: Int, _ to: Int) throws -> [Entry] {
        var entries = [Entry]()
        var start = from
        var integers = [Int]()
        var isInteger = true
        var i = from
        while i < to {
            let b0 = Int(cff[i])
            if b0 <= 21 {   // An operator
                var op = b0
                i += 1
                if b0 == 12 {
                    if i >= to {
                        throw Subset.NotSubset()
                    }
                    op = 12 << 8 | Int(cff[i])
                    i += 1
                }
                entries.append(Entry(op: op, raw: Array(cff[start..<i]), integers: isInteger ? integers : nil))
                start = i
                integers = []
                isInteger = true
            } else if b0 == 28 {
                if i + 3 > to {
                    throw Subset.NotSubset()
                }
                integers.append(Int(Int16(bitPattern: UInt16(cff[i + 1]) << 8 | UInt16(cff[i + 2]))))
                i += 3
            } else if b0 == 29 {
                if i + 5 > to {
                    throw Subset.NotSubset()
                }
                integers.append(Int(Int32(bitPattern: UInt32(cff[i + 1]) << 24 | UInt32(cff[i + 2]) << 16 |
                        UInt32(cff[i + 3]) << 8 | UInt32(cff[i + 4]))))
                i += 5
            } else if b0 == 30 {    // A real, in nibbles up to one of 0xF
                isInteger = false
                i += 1
                while i < to && cff[i] & 0x0F != 0x0F && cff[i] & 0xF0 != 0xF0 {
                    i += 1
                }
                if i >= to {
                    throw Subset.NotSubset()
                }
                i += 1
            } else if b0 >= 32 && b0 <= 246 {
                integers.append(b0 - 139)
                i += 1
            } else if b0 >= 247 && b0 <= 250 {
                if i + 2 > to {
                    throw Subset.NotSubset()
                }
                integers.append((b0 - 247) * 256 + Int(cff[i + 1]) + 108)
                i += 2
            } else if b0 >= 251 && b0 <= 254 {
                if i + 2 > to {
                    throw Subset.NotSubset()
                }
                integers.append(-(b0 - 251) * 256 - Int(cff[i + 1]) - 108)
                i += 2
            } else {
                throw Subset.NotSubset()
            }
        }
        if start != to {
            throw Subset.NotSubset()    // Operands without an operator
        }
        return entries
    }

    static func entryOf(_ entries: [Entry], _ op: Int) -> [Int]? {
        for entry in entries where entry.op == op {
            return entry.integers
        }
        return nil
    }

    static func appendOffset(_ out: inout [UInt8], _ value: Int) {
        out.append(29)
        out.append(UInt8(truncatingIfNeeded: value >> 24))
        out.append(UInt8(truncatingIfNeeded: value >> 16))
        out.append(UInt8(truncatingIfNeeded: value >> 8))
        out.append(UInt8(truncatingIfNeeded: value))
    }

    // Writes the entries again, the operands of the operators in offsets in
    // the five-byte form: the offset, or for Private its size and its offset.
    static func writeDict(_ entries: [Entry], _ offsets: [Int: Int], _ privateSize: Int) -> [UInt8] {
        var out = [UInt8]()
        for entry in entries {
            guard let offset = offsets[entry.op] else {
                out.append(contentsOf: entry.raw)
                continue
            }
            if entry.op == privateOp {
                appendOffset(&out, privateSize)
            }
            appendOffset(&out, offset)
            if entry.op > 0xFF {
                out.append(12)
                out.append(UInt8(entry.op & 0xFF))
            } else {
                out.append(UInt8(entry.op))
            }
        }
        return out
    }

    static func charsetLength(_ cff: [UInt8], _ at: Int, _ numGlyphs: Int) throws -> Int {
        if at < 0 || at >= cff.count {
            throw Subset.NotSubset()
        }
        let format = Int(cff[at])
        if format == 0 {
            return 1 + 2 * (numGlyphs - 1)
        }
        if format == 1 || format == 2 {
            let size = format   // The size of nLeft: 1 byte in format 1, 2 in format 2
            var covered = 1     // .notdef is not in it
            var i = at + 1
            while covered < numGlyphs {
                if i + 2 + size > cff.count {
                    throw Subset.NotSubset()
                }
                var nLeft = Int(cff[i + 2])
                if size == 2 {
                    nLeft = nLeft << 8 | Int(cff[i + 3])
                }
                covered += nLeft + 1
                i += 2 + size
            }
            return i - at
        }
        throw Subset.NotSubset()
    }

    static func encodingLength(_ cff: [UInt8], _ at: Int) throws -> Int {
        if at < 0 || at + 2 > cff.count {
            throw Subset.NotSubset()
        }
        var length: Int
        switch cff[at] & 0x7F {
        case 0:
            length = 2 + Int(cff[at + 1])
        case 1:
            length = 2 + 2 * Int(cff[at + 1])
        default:
            throw Subset.NotSubset()
        }
        if cff[at] & 0x80 != 0 {    // Supplements
            if at + length >= cff.count {
                throw Subset.NotSubset()
            }
            length += 1 + 3 * Int(cff[at + length])
        }
        return length
    }

    static func fdSelectLength(_ cff: [UInt8], _ at: Int, _ numGlyphs: Int) throws -> Int {
        if at < 0 || at >= cff.count {
            throw Subset.NotSubset()
        }
        if cff[at] == 0 {
            return 1 + numGlyphs
        }
        if cff[at] == 3 {
            if at + 3 > cff.count {
                throw Subset.NotSubset()
            }
            let ranges = Int(cff[at + 1]) << 8 | Int(cff[at + 2])
            return 1 + 2 + 3 * ranges + 2
        }
        throw Subset.NotSubset()
    }

    static func block(_ cff: [UInt8], _ at: Int, _ length: Int) throws -> [UInt8] {
        if at < 0 || length < 0 || at + length > cff.count {
            throw Subset.NotSubset()
        }
        return Array(cff[at..<(at + length)])
    }

    static func readPrivate(_ cff: [UInt8], _ size: Int, _ at: Int) throws -> PrivateDict {
        _ = try block(cff, at, size)
        let entries = try readDict(cff, at, at + size)
        guard let subrs = entryOf(entries, subrsOp) else {
            return PrivateDict(entries: entries, subrs: nil)
        }
        if subrs.count != 1 {
            throw Subset.NotSubset()
        }
        return PrivateDict(entries: entries, subrs: try readIndex(cff, at + subrs[0]))
    }

    static func bias(_ count: Int) -> Int {
        if count < 1240 {
            return 107
        } else if count < 33900 {
            return 1131
        }
        return 32768
    }

    /// Finds the subroutines that the charstrings of the glyphs kept call,
    /// directly or through other subroutines.
    final class Usage {
        let cff: [UInt8]
        let global: Index
        var usedG: [Bool]
        var local: Index?
        var fd = -1         // The Font DICT of the local subroutines
        var usedL: [[Bool]?]
        var stack = 0       // The operands on the stack
        var last = 0        // The last of them, a subroutine number when one is called
        var stems = 0
        var work = 0
        var failed = false
        var stopped = false // By endchar

        init(_ cff: [UInt8], _ global: Index, _ usedG: [Bool], _ usedL: [[Bool]?]) {
            self.cff = cff
            self.global = global
            self.usedG = usedG
            self.usedL = usedL
        }

        func run(_ from: Int, _ to: Int, _ depth: Int) {
            if depth > 10 {     // Type 2 nests subroutines 10 deep at most.
                failed = true
                return
            }
            work += to - from
            if work > CFFSubset.maxWork {
                failed = true
                return
            }
            var i = from
            while i < to && !failed && !stopped {
                let b0 = Int(cff[i])
                if b0 == 28 {
                    if i + 3 > to {
                        failed = true
                        return
                    }
                    last = Int(Int16(bitPattern: UInt16(cff[i + 1]) << 8 | UInt16(cff[i + 2])))
                    stack += 1
                    i += 3
                } else if b0 >= 32 && b0 <= 246 {
                    last = b0 - 139
                    stack += 1
                    i += 1
                } else if b0 >= 247 && b0 <= 250 {
                    if i + 2 > to {
                        failed = true
                        return
                    }
                    last = (b0 - 247) * 256 + Int(cff[i + 1]) + 108
                    stack += 1
                    i += 2
                } else if b0 >= 251 && b0 <= 254 {
                    if i + 2 > to {
                        failed = true
                        return
                    }
                    last = -(b0 - 251) * 256 - Int(cff[i + 1]) - 108
                    stack += 1
                    i += 2
                } else if b0 == 255 {   // A 16.16 fixed number
                    if i + 5 > to {
                        failed = true
                        return
                    }
                    last = Int(Int32(bitPattern: UInt32(cff[i + 1]) << 24 | UInt32(cff[i + 2]) << 16 |
                            UInt32(cff[i + 3]) << 8 | UInt32(cff[i + 4]))) >> 16
                    stack += 1
                    i += 5
                } else if b0 == 1 || b0 == 3 || b0 == 18 || b0 == 23 {  // The stem hints
                    stems += stack / 2
                    stack = 0
                    i += 1
                } else if b0 == 19 || b0 == 20 {    // hintmask and cntrmask, and their mask
                    stems += stack / 2
                    stack = 0
                    i += 1 + (stems + 7) / 8
                } else if b0 == 10 || b0 == 29 {    // callsubr and callgsubr
                    if stack == 0 {
                        failed = true
                        return
                    }
                    stack -= 1
                    guard let subrs = (b0 == 29) ? global : local else {
                        failed = true
                        return
                    }
                    let number = last + CFFSubset.bias(subrs.count)
                    if number < 0 || number >= subrs.count {
                        failed = true
                        return
                    }
                    if b0 == 29 {
                        usedG[number] = true
                    } else {
                        usedL[fd]![number] = true
                    }
                    run(subrs.objects[number], subrs.objects[number + 1], depth + 1)
                    i += 1
                } else if b0 == 11 {    // return
                    return
                } else if b0 == 14 {    // endchar
                    if stack >= 4 {
                        // seac, an accented letter drawn from two others, which
                        // would have to be kept too.
                        failed = true
                    }
                    stopped = true
                    return
                } else if b0 == 12 {    // An escaped operator
                    stack = 0
                    i += 2
                } else {
                    stack = 0
                    i += 1
                }
            }
        }
    }

    // Returns the objects of the INDEX, those not used emptied to the
    // charstring of the byte given.
    static func emptied(_ cff: [UInt8], _ index: Index, _ used: [Bool]?, _ empty: UInt8) -> [[UInt8]] {
        var objects = [[UInt8]]()
        objects.reserveCapacity(index.count)
        for i in 0..<index.count {
            if used == nil || used![i] {
                objects.append(Array(cff[index.objects[i]..<index.objects[i + 1]]))
            } else {
                objects.append([empty])
            }
        }
        return objects
    }

    // Returns the Font DICT of each glyph, from the FDSelect of a CID-keyed
    // font, or nil for a name-keyed one.
    static func fdOf(_ fdSelect: [UInt8]?, _ numGlyphs: Int, _ fds: Int) throws -> [Int]? {
        guard let fdSelect = fdSelect else {
            return nil
        }
        var fdOf = [Int](repeating: 0, count: numGlyphs)
        if fdSelect[0] == 0 {
            for gid in 0..<numGlyphs {
                fdOf[gid] = Int(fdSelect[1 + gid])
            }
        } else if fdSelect[0] == 3 {
            let ranges = Int(fdSelect[1]) << 8 | Int(fdSelect[2])
            for r in 0..<ranges {
                let first = Int(fdSelect[3 + 3 * r]) << 8 | Int(fdSelect[4 + 3 * r])
                // The first of the next range, or the sentinel
                let next = Int(fdSelect[6 + 3 * r]) << 8 | Int(fdSelect[7 + 3 * r])
                let fd = Int(fdSelect[5 + 3 * r])
                if first > next || next > numGlyphs {
                    throw Subset.NotSubset()
                }
                for gid in first..<next {
                    fdOf[gid] = fd
                }
            }
        }
        for fd in fdOf where fd >= fds {
            throw Subset.NotSubset()
        }
        return fdOf
    }

    /// Returns the CFF table with the charstrings of the glyphs not used
    /// emptied, and the subroutines they do not call, and the glyphs kept:
    /// those used and glyph 0, or every glyph for used nil, which writes the
    /// table again with nothing left out, for the identity charset of a
    /// CID-keyed font. A table that cannot be read throws NotSubset.
    static func subset(_ cff: [UInt8], _ used: [Bool]?) throws -> ([UInt8], [Bool]) {
        if cff.count < 4 || cff[0] != 1 {
            throw Subset.NotSubset()    // Not CFF version 1
        }
        let headerSize = Int(cff[2])
        let names = try readIndex(cff, headerSize)
        let topDicts = try readIndex(cff, names.end)
        if topDicts.count != 1 {    // One font
            throw Subset.NotSubset()
        }
        let strings = try readIndex(cff, topDicts.end)
        let globalSubrs = try readIndex(cff, strings.end)
        let top = try readDict(cff, topDicts.objects[0], topDicts.objects[1])
        guard let charStringsAt = entryOf(top, charStrings), charStringsAt.count == 1 else {
            throw Subset.NotSubset()
        }
        let glyphIndex = try readIndex(cff, charStringsAt[0])
        let numGlyphs = glyphIndex.count
        if numGlyphs == 0 {
            throw Subset.NotSubset()
        }

        // The blocks copied as they are, but the charset of a CID-keyed font,
        // which maps each glyph to its CID: it is made the identity, so that
        // the glyph numbers PDFjet writes are the CIDs of the glyphs, as they
        // are in a name-keyed font.
        var charsetBlock: [UInt8]?
        var encodingBlock: [UInt8]?
        var fdSelectBlock: [UInt8]?
        if entryOf(top, ros) != nil {
            let count = entryOf(top, cidCount)
            if let count = count, count.count != 1 || count[0] < numGlyphs {
                throw Subset.NotSubset()
            }
            if count == nil && numGlyphs > 8720 {   // The default CIDCount
                throw Subset.NotSubset()
            }
            charsetBlock = [0]  // Format 0, for .notdef alone
            if numGlyphs > 1 {
                // Format 2, one range: CIDs 1 to numGlyphs-1.
                charsetBlock = [2, 0, 1, UInt8(truncatingIfNeeded: (numGlyphs - 2) >> 8),
                        UInt8(truncatingIfNeeded: numGlyphs - 2)]
            }
        } else if let at = entryOf(top, charset), at.count == 1 && at[0] > 2 {
            charsetBlock = try block(cff, at[0], try charsetLength(cff, at[0], numGlyphs))
        }
        if let at = entryOf(top, encoding), at.count == 1 && at[0] > 1 {
            encodingBlock = try block(cff, at[0], try encodingLength(cff, at[0]))
        }
        if let at = entryOf(top, fdSelect) {
            if at.count != 1 {
                throw Subset.NotSubset()
            }
            fdSelectBlock = try block(cff, at[0], try fdSelectLength(cff, at[0], numGlyphs))
        }

        // The Private DICT of a name-keyed font, or the Font DICTs of a
        // CID-keyed one, each with its Private DICT.
        var privates = [PrivateDict]()
        var fontDicts: [[Entry]]?
        if let at = entryOf(top, fdArray) {
            if at.count != 1 || fdSelectBlock == nil {
                throw Subset.NotSubset()
            }
            let fdIndex = try readIndex(cff, at[0])
            var dicts = [[Entry]]()
            for i in 0..<fdIndex.count {
                let entries = try readDict(cff, fdIndex.objects[i], fdIndex.objects[i + 1])
                guard let p = entryOf(entries, privateOp), p.count == 2 else {
                    throw Subset.NotSubset()
                }
                dicts.append(entries)
                privates.append(try readPrivate(cff, p[0], p[1]))
            }
            fontDicts = dicts
        } else if let p = entryOf(top, privateOp) {
            if p.count != 2 {
                throw Subset.NotSubset()
            }
            privates.append(try readPrivate(cff, p[0], p[1]))
        }

        // The charstrings, of the glyphs kept as they were, of the others
        // endchar.
        var keep = [Bool](repeating: false, count: numGlyphs)
        var glyphs = [[UInt8]]()
        glyphs.reserveCapacity(numGlyphs)
        for gid in 0..<numGlyphs {
            if used == nil || gid == 0 || (gid < used!.count && used![gid]) {
                keep[gid] = true
                glyphs.append(Array(cff[glyphIndex.objects[gid]..<glyphIndex.objects[gid + 1]]))
            } else {
                glyphs.append([14])
            }
        }
        let newCharStrings = index(glyphs)

        // The subroutines the glyphs kept call, the others emptied to return.
        // A font whose charstrings cannot be followed keeps them all.
        let fdOfGlyph = try fdOf(fdSelectBlock, numGlyphs, privates.count)
        let usage = Usage(cff, globalSubrs, [Bool](repeating: false, count: globalSubrs.count),
                privates.map { $0.subrs.map { [Bool](repeating: false, count: $0.count) } })
        if used == nil {
            usage.failed = true     // Every glyph and subroutine kept
        }
        var gid = 0
        while gid < numGlyphs && !usage.failed {
            if keep[gid] {
                let fd = fdOfGlyph?[gid] ?? 0
                usage.local = nil
                usage.fd = -1
                if fd < privates.count, let subrs = privates[fd].subrs {
                    usage.local = subrs
                    usage.fd = fd
                }
                usage.stack = 0
                usage.stems = 0
                usage.stopped = false
                usage.run(glyphIndex.objects[gid], glyphIndex.objects[gid + 1], 0)
            }
            gid += 1
        }
        var usedG: [Bool]? = usage.usedG
        var usedL = usage.usedL
        if usage.failed {
            if usage.work > maxWork {
                throw Subset.NotSubset()
            }
            usedG = nil     // Kept whole
            usedL = usedL.map { _ in nil }
        }
        let newGlobalSubrs = index(emptied(cff, globalSubrs, usedG, 11))

        // Each Private DICT is written again with its local subroutines right
        // after it, at its size, whatever they were before.
        var blocks = [[UInt8]]()
        var dictSizes = [Int]()
        for (i, p) in privates.enumerated() {
            var subrs = [Int: Int]()
            if p.subrs != nil {
                subrs[subrsOp] = 0
            }
            let size = writeDict(p.entries, subrs, 0).count
            dictSizes.append(size)
            if p.subrs != nil {
                subrs[subrsOp] = size
            }
            var b = writeDict(p.entries, subrs, 0)
            if let s = p.subrs {
                appendIndex(&b, emptied(cff, s, usedL[i], 11))
            }
            blocks.append(b)
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        let namePrivate = (fontDicts == nil && privates.count == 1)
        func topOffsets(_ charsetAt: Int, _ encodingAt: Int, _ fdSelectAt: Int, _ charStringsAt: Int,
                _ fdArrayAt: Int, _ privateAt: Int) -> [Int: Int] {
            var offsets = [charStrings: charStringsAt]
            if charsetBlock != nil {
                offsets[charset] = charsetAt
            }
            if encodingBlock != nil {
                offsets[encoding] = encodingAt
            }
            if fdSelectBlock != nil {
                offsets[fdSelect] = fdSelectAt
                offsets[fdArray] = fdArrayAt
            } else if namePrivate {
                offsets[privateOp] = privateAt
            }
            return offsets
        }
        let topSize = writeDict(top, topOffsets(0, 0, 0, 0, 0, 0), 0).count
        var at = headerSize + (names.end - names.start) + index([[UInt8](repeating: 0, count: topSize)]).count +
                (strings.end - strings.start) + newGlobalSubrs.count
        let charsetAt = at
        at += charsetBlock?.count ?? 0
        let encodingAt = at
        at += encodingBlock?.count ?? 0
        let fdSelectAt = at
        at += fdSelectBlock?.count ?? 0
        let newCharStringsAt = at
        at += newCharStrings.count
        let fdArrayAt = at
        if let fontDicts = fontDicts {
            at += index(fontDicts.indices.map { writeDict(fontDicts[$0], [privateOp: 0], dictSizes[$0]) }).count
        }
        var privateAts = [Int]()
        for b in blocks {
            privateAts.append(at)
            at += b.count
        }
        var fdArrayBytes = [UInt8]()
        if let fontDicts = fontDicts {
            fdArrayBytes = index(fontDicts.indices.map {
                writeDict(fontDicts[$0], [privateOp: privateAts[$0]], dictSizes[$0])
            })
        }
        let newTop = writeDict(top, topOffsets(charsetAt, encodingAt, fdSelectAt, newCharStringsAt, fdArrayAt,
                namePrivate ? privateAts[0] : 0), namePrivate ? dictSizes[0] : 0)

        var out = [UInt8]()
        out.reserveCapacity(at)
        out.append(contentsOf: cff[0..<headerSize])
        out.append(contentsOf: cff[names.start..<names.end])
        appendIndex(&out, [newTop])
        out.append(contentsOf: cff[strings.start..<strings.end])
        out.append(contentsOf: newGlobalSubrs)
        out.append(contentsOf: charsetBlock ?? [])
        out.append(contentsOf: encodingBlock ?? [])
        out.append(contentsOf: fdSelectBlock ?? [])
        out.append(contentsOf: newCharStrings)
        out.append(contentsOf: fdArrayBytes)
        for b in blocks {
            out.append(contentsOf: b)
        }
        if out.count != at {
            throw Subset.NotSubset()
        }
        return (out, keep)
    }
}
