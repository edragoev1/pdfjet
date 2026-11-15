/**
 * CFFSubset.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The subset of a font with CFF outlines, a .otf: a CIDFont of the glyphs the
/// document draws alone, its charset giving each glyph its number in the
/// whole font as its CID, by which the PDF draws it, each glyph written with
/// the subroutines it calls, global and local, in place of its calls, and no
/// subroutines. A glyph that computes a number keeps the subroutines it calls
/// instead, the others emptied to no bytes, keeping their numbers. A
/// name-keyed font becomes a CIDFont of Adobe-Identity-0, with the identity
/// matrix in its one Font DICT and its own, or the default, in its Top DICT.
/// The CFF is written again in a fixed order, with every offset of its Top
/// DICT and of its Font DICTs in the five-byte form, so that an offset of any
/// value has the same size: the header, the Name, Top DICT, String and Global
/// Subr INDEXes, the charset, the FDSelect, the new CharStrings INDEX, the
/// FDArray, and each Private DICT with its local subroutines, if any.
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
    static let fontMatrix = 12 << 8 | 7

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
        var seac = false    // An accented letter drawn from two others

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
                        // would have to be kept too: the font is embedded whole
                        // (the review of 9 October 2026: the letter drew blank)
                        seac = true
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

    /// Writes charstrings again with each subroutine they call, local or
    /// global, in place of its call (Technical Note #5177): the number of the
    /// subroutine taken off the end, the bytes of the subroutine, its own
    /// calls in place too, without its return. The charstring written draws
    /// the glyph as the operators of the one it was written from do, run with
    /// its subroutines, one after another. A charstring that computes a
    /// number, by an arithmetic or storage operator, cannot be written so: it
    /// fails.
    final class Inliner {
        let cff: [UInt8]
        let global: Index
        var local: Index?
        var out = [UInt8]()     // The charstrings written, one after another
        var numbers = [Int](repeating: 0, count: 48)    // Where each operand on the stack starts in out
        var stack = 0           // The operands on the stack, of the 48 of Type 2 at most
        var stems = 0
        var work = 0
        var ended = false       // By endchar
        var failed = false
        var seac = false

        init(_ cff: [UInt8], _ global: Index, _ capacity: Int) {
            self.cff = cff
            self.global = global
            out.reserveCapacity(capacity)
        }

        /// Returns the charstring, from and to in the CFF table, written
        /// again with its subroutines in place, after the charstrings written
        /// before it in out, which holds them all.
        func glyph(_ from: Int, _ to: Int) -> [UInt8] {
            let start = out.count
            stack = 0
            stems = 0
            ended = false
            run(from, to, 0)
            return Array(out[start...])
        }

        /// Writes the charstring, from and to in the CFF table, and the
        /// subroutines it calls in place. Its bytes are written in runs, from
        /// one call of a subroutine to the next.
        func run(_ start: Int, _ end: Int, _ depth: Int) {
            if depth > 10 {     // Type 2 nests subroutines 10 deep at most.
                failed = true
                return
            }
            var from = start    // The bytes of the charstring from here on are not written yet.
            var i = start
            while i < end {
                let b0 = Int(cff[i])
                if b0 >= 28 && b0 != 29 && b0 != 30 && b0 != 31 {  // A number
                    var n = 1
                    if b0 == 28 {
                        n = 3
                    } else if b0 == 255 {
                        n = 5
                    } else if b0 >= 247 {
                        n = 2
                    }
                    if i + n > end || stack == numbers.count {
                        failed = true
                        return
                    }
                    numbers[stack] = out.count + i - from
                    stack += 1
                    i += n
                    continue
                }
                var n = 1   // The bytes of the operator
                switch b0 {
                case 1, 3, 18, 23:  // The stem hints
                    stems += stack / 2
                case 19, 20:        // hintmask and cntrmask, and their mask
                    stems += stack / 2
                    n += (stems + 7) / 8
                case 10, 29:        // callsubr and callgsubr
                    if stack == 0 {
                        failed = true
                        return
                    }
                    out.append(contentsOf: cff[from..<i])
                    stack -= 1
                    let at = numbers[stack]
                    let last = CFFSubset.number(out, at)
                    out.removeSubrange(at...)
                    guard let subrs = (b0 == 29) ? global : local else {
                        failed = true
                        return
                    }
                    let number = last + CFFSubset.bias(subrs.count)
                    if number < 0 || number >= subrs.count {
                        failed = true
                        return
                    }
                    let subrFrom = subrs.objects[number]
                    let subrTo = subrs.objects[number + 1]
                    work += subrTo - subrFrom
                    if work > CFFSubset.maxWork {
                        failed = true
                        return
                    }
                    run(subrFrom, subrTo, depth + 1)
                    if failed || ended {
                        return
                    }
                    i += 1
                    from = i
                    continue
                case 11:            // return
                    out.append(contentsOf: cff[from..<i])
                    return
                case 14:            // endchar
                    if stack >= 4 {
                        // seac, an accented letter drawn from two others: the
                        // font is embedded whole
                        seac = true
                        failed = true
                        return
                    }
                    out.append(contentsOf: cff[from..<(i + 1)])
                    ended = true
                    return
                case 12:
                    n = 2
                    if i + 1 < end {
                        switch cff[i + 1] {
                        case 3, 4, 5, 9, 10, 11, 12, 14, 15, 18, 20, 21, 22, 23, 24, 26, 27, 28, 29, 30:
                            // The arithmetic and storage operators, which
                            // leave numbers on the stack
                            failed = true
                            return
                        default:
                            break
                        }
                    }
                default:
                    break
                }
                if i + n > end {
                    failed = true
                    return
                }
                // An operator, which takes its operands off the stack
                stack = 0
                i += n
            }
            out.append(contentsOf: cff[from..<i])
        }
    }

    /// Returns the integer part of the number of a charstring at the offset
    /// of the bytes.
    static func number(_ b: [UInt8], _ at: Int) -> Int {
        let b0 = Int(b[at])
        if b0 == 28 {
            return Int(Int16(bitPattern: UInt16(b[at + 1]) << 8 | UInt16(b[at + 2])))
        } else if b0 >= 32 && b0 <= 246 {
            return b0 - 139
        } else if b0 >= 247 && b0 <= 250 {
            return (b0 - 247) * 256 + Int(b[at + 1]) + 108
        } else if b0 >= 251 && b0 <= 254 {
            return -(b0 - 251) * 256 - Int(b[at + 1]) - 108
        }
        return Int(Int32(bitPattern: UInt32(b[at + 1]) << 24 | UInt32(b[at + 2]) << 16 |
                UInt32(b[at + 3]) << 8 | UInt32(b[at + 4]))) >> 16
    }

    /// The FontMatrix entry [1 0 0 1 0 0].
    static let identityMatrix: [UInt8] = [140, 139, 139, 140, 139, 139, 12, 7]

    /// The FontMatrix entry [0.001 0 0 0.001 0 0], the default of a Top DICT,
    /// its reals written as 1E-3.
    static let thousandthMatrix: [UInt8] = [30, 0x1c, 0x3f, 139, 139, 30, 0x1c, 0x3f, 139, 139, 12, 7]

    // Returns the objects of the INDEX, those not used emptied to no bytes,
    // which keeps their numbers and makes their offsets the same as the next
    // one's, or every one for used nil.
    static func emptied(_ cff: [UInt8], _ index: Index, _ used: [Bool]?) -> [[UInt8]] {
        var objects = [[UInt8]]()
        objects.reserveCapacity(index.count)
        for i in 0..<index.count {
            if used == nil || used![i] {
                objects.append(Array(cff[index.objects[i]..<index.objects[i + 1]]))
            } else {
                objects.append([])
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

    /// Returns the CFF table of the glyphs used and glyph 0, or of every glyph
    /// for used nil, and the glyphs kept, by their numbers in the font. The
    /// subset is a CIDFont of the glyphs kept alone, in the order of their
    /// numbers, its charset giving each its number in the font as its CID: the
    /// PDF draws a glyph of a CIDFontType0C program by its CID, which the
    /// charset maps to the glyph (ISO 32000-2, 9.7.4.2; Technical Note #5176,
    /// section 18, "Subset CIDFonts will generally need to use a more complex
    /// charset table representing a non-identity mapping"), so the content
    /// streams, the widths and the ToUnicode map stay those of the whole font.
    /// Each glyph kept is written with the subroutines it calls in place of
    /// its calls, and the subset has no subroutines; a glyph that computes a
    /// number, which cannot be written so, keeps the subroutines called, the
    /// others emptied to no bytes, keeping their numbers. The whole font
    /// keeps its subroutines. The Font DICTs no glyph kept uses are left out.
    /// Every INDEX has the least offSize its offsets need; the offsets of the
    /// Top DICT and of the Font DICTs are in the five-byte form.
    ///
    /// A name-keyed font is written as a CIDFont of the ordering
    /// Adobe-Identity-0 whose one Font DICT has its Private DICT and the
    /// FontMatrix [1 0 0 1 0 0], and its Top DICT the font's FontMatrix, its
    /// own, as Cascadia Mono's of 2048 units to the em, or the default,
    /// [0.001 0 0 0.001 0 0], written: the glyph space of a CIDFont is that of
    /// the FontMatrix of the glyph's Font DICT concatenated with the Top
    /// DICT's (PostScript Language Reference, third edition, 5.11.3, Table
    /// 5.14), the Top DICT's interpreting the coordinates of the CIDFont as a
    /// whole, as its FontBBox. Both are written, as each has a default that a
    /// reader may take otherwise: a Font DICT of no FontMatrix is taken as the
    /// identity, though Technical Note #5176 gives a FontMatrix no default but
    /// [0.001 0 0 0.001 0 0] (Table 9), and a CIDFont whose Top DICT has none
    /// has its Font DICTs' matrices taken as the whole ones (the PostScript
    /// Language Reference, and MuPDF, Poppler, PDFium and pdf.js). A
    /// CID-keyed font's matrices are kept as they are. A table that cannot be
    /// read throws NotSubset.
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

        let cidKeyed = entryOf(top, ros) != nil
        var fdSelectBlock: [UInt8]?
        if cidKeyed {
            let count = entryOf(top, cidCount)
            if let count = count, count.count != 1 || count[0] < numGlyphs {
                throw Subset.NotSubset()
            }
            if count == nil && numGlyphs > 8720 {   // The default CIDCount
                throw Subset.NotSubset()
            }
            guard let at = entryOf(top, fdSelect), at.count == 1 else {
                throw Subset.NotSubset()
            }
            fdSelectBlock = try block(cff, at[0], try fdSelectLength(cff, at[0], numGlyphs))
        }

        // The Font DICTs of a CID-keyed font, each with its Private DICT, or
        // the Private DICT of a name-keyed one, which becomes its one Font
        // DICT's.
        var privates = [PrivateDict]()
        var fontDicts = [[Entry]]()
        if cidKeyed {
            guard let at = entryOf(top, fdArray), at.count == 1 else {
                throw Subset.NotSubset()
            }
            let fdIndex = try readIndex(cff, at[0])
            for i in 0..<fdIndex.count {
                let entries = try readDict(cff, fdIndex.objects[i], fdIndex.objects[i + 1])
                guard let p = entryOf(entries, privateOp), p.count == 2 else {
                    throw Subset.NotSubset()
                }
                let dict = try readPrivate(cff, p[0], p[1])
                fontDicts.append(entries)
                privates.append(dict)
            }
        } else {
            guard let p = entryOf(top, privateOp), p.count == 2 else {
                throw Subset.NotSubset()
            }
            let dict = try readPrivate(cff, p[0], p[1])
            // Its one Font DICT has the identity matrix, written, and its Top
            // DICT its FontMatrix, its own or the default, written.
            fontDicts = [[Entry(op: fontMatrix, raw: identityMatrix, integers: nil),
                    Entry(op: privateOp, raw: [], integers: nil)]]
            privates.append(dict)
        }

        // The glyphs kept, by their numbers in the font, which are their CIDs.
        var kept = [Bool](repeating: false, count: numGlyphs)
        var keptGIDs = [Int]()
        for gid in 0..<numGlyphs {
            if used == nil || gid == 0 || (gid < used!.count && used![gid]) {
                kept[gid] = true
                keptGIDs.append(gid)
            }
        }
        var glyphs = [[UInt8]]()
        glyphs.reserveCapacity(keptGIDs.count)
        for gid in keptGIDs {
            glyphs.append(Array(cff[glyphIndex.objects[gid]..<glyphIndex.objects[gid + 1]]))
        }

        // The subroutines the glyphs kept call, the others emptied. A font
        // whose charstrings cannot be followed keeps them all.
        let fdOfGID = try fdOf(fdSelectBlock, numGlyphs, privates.count)
        let usage = Usage(cff, globalSubrs, [Bool](repeating: false, count: globalSubrs.count),
                privates.map { $0.subrs.map { [Bool](repeating: false, count: $0.count) } })
        if used == nil {
            usage.failed = true     // Every glyph and subroutine kept
        }
        // The glyphs of a subset written with the subroutines they call in
        // place of their calls, and no subroutines then, or, when that cannot
        // be done, with the subroutines they call.
        var glyphsInlined = false
        if used != nil {
            var size = 0
            for glyph in glyphs {
                size += glyph.count
            }
            let inliner = Inliner(cff, globalSubrs, 3 * size)
            var inlinedGlyphs = [[UInt8]]()
            inlinedGlyphs.reserveCapacity(keptGIDs.count)
            var i = 0
            while i < keptGIDs.count && !inliner.failed {
                let gid = keptGIDs[i]
                inliner.local = privates[fdOfGID?[gid] ?? 0].subrs
                inlinedGlyphs.append(inliner.glyph(glyphIndex.objects[gid], glyphIndex.objects[gid + 1]))
                i += 1
            }
            if inliner.seac {
                throw Subset.NotSubset()
            }
            if !inliner.failed {
                // No subroutine used, global or local, as the usage has them.
                glyphs = inlinedGlyphs
                glyphsInlined = true
            }
        }
        var i = 0
        while i < keptGIDs.count && !usage.failed && !glyphsInlined {
            let gid = keptGIDs[i]
            let fd = fdOfGID?[gid] ?? 0
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
            i += 1
        }
        var usedG: [Bool]? = usage.usedG
        var usedL = usage.usedL
        if usage.failed {
            if usage.work > maxWork || usage.seac {
                throw Subset.NotSubset()
            }
            usedG = nil     // Kept whole
            usedL = usedL.map { _ in nil }
        }

        // The Font DICTs the glyphs kept use, in their order in the font, and
        // the new number of each.
        var usedFD = [Bool](repeating: false, count: privates.count)
        for gid in keptGIDs {
            usedFD[fdOfGID?[gid] ?? 0] = true
        }
        var newFD = [Int](repeating: 0, count: privates.count)
        var fds = [Int]()
        for (fd, isUsed) in usedFD.enumerated() where isUsed {
            newFD[fd] = fds.count
            fds.append(fd)
        }
        let fdOfGlyph = keptGIDs.map { gid in fdOfGID.map { newFD[$0[gid]] } ?? 0 }

        // The blocks: the Name INDEX again; the String INDEX of the strings the
        // Top DICT and the Font DICTs kept name, in the order they name them, a
        // name-keyed font's with those of its ROS, Adobe and Identity, after
        // them, and not the names of its glyphs, which a CIDFont has not; the
        // charset of the CIDs; the FDSelect; the CharStrings of the glyphs kept.
        let nameIndex = index(emptied(cff, names, nil))
        let sids = Strings(emptied(cff, strings, nil))
        var entries = [Entry]()
        for entry in top {
            switch entry.op {
            case charset, encoding, charStrings, privateOp, fdArray, fdSelect:
                continue
            default:
                entries.append(try sids.renumber(entry))
            }
        }
        var keptFontDicts = [[Entry]]()
        for fd in fds {
            keptFontDicts.append(try fontDicts[fd].map { try sids.renumber($0) })
        }
        if !cidKeyed {
            let sid = 391 + sids.kept.count
            sids.kept.append(Array("Adobe".utf8))
            sids.kept.append(Array("Identity".utf8))
            var rosRaw = [UInt8]()
            appendInteger(&rosRaw, sid)
            appendInteger(&rosRaw, sid + 1)
            appendInteger(&rosRaw, 0)
            rosRaw.append(contentsOf: [12, 30])
            entries.insert(Entry(op: ros, raw: rosRaw, integers: nil), at: 0)
            if !hasEntry(top, fontMatrix) {
                entries.append(Entry(op: fontMatrix, raw: thousandthMatrix, integers: nil))
            }
            var countRaw = [UInt8]()
            appendInteger(&countRaw, numGlyphs)
            countRaw.append(contentsOf: [12, 34])
            entries.append(Entry(op: cidCount, raw: countRaw, integers: nil))
        }
        let stringIndex = index(sids.kept)
        for op in [charset, fdSelect, charStrings, fdArray] {
            entries.append(Entry(op: op, raw: [], integers: nil))
        }
        let charsetBlock = charsetOf(Array(keptGIDs[1...]))
        let newFDSelect = fdSelectOf(fdOfGlyph)
        let newCharStrings = index(glyphs)
        // An INDEX of subroutines none of which is called is left out, global
        // ones as an empty INDEX, local ones with their Subrs operator: one of
        // objects of no bytes alone is not read by FreeType
        var globals = emptied(cff, globalSubrs, usedG)
        if let usedG = usedG, !usedG.contains(true) {
            globals = []
        }
        let newGlobalSubrs = index(globals)

        // Each Private DICT of a Font DICT kept is written again with its
        // local subroutines right after it, at its size, whatever they were
        // before.
        var blocks = [[UInt8]]()
        var dictSizes = [Int]()
        for fd in fds {
            var p = privates[fd]
            if p.subrs != nil, let usedLocal = usedL[fd], !usedLocal.contains(true) {
                p = PrivateDict(entries: p.entries.filter { $0.op != subrsOp }, subrs: nil)
            }
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
                appendIndex(&b, emptied(cff, s, usedL[fd]))
            }
            blocks.append(b)
        }

        // The layout. The Top DICT and the Font DICTs have the same size
        // whatever their offsets, so they are written once with none to know
        // where the blocks after them go, and once more with their offsets.
        func topOffsets(_ charsetAt: Int, _ fdSelectAt: Int, _ charStringsAt: Int, _ fdArrayAt: Int) -> [Int: Int] {
            return [charset: charsetAt, fdSelect: fdSelectAt, charStrings: charStringsAt, fdArray: fdArrayAt]
        }
        func fontDictOf(_ i: Int, _ privateAt: Int) -> [UInt8] {
            return writeDict(keptFontDicts[i], [privateOp: privateAt], dictSizes[i])
        }
        let topSize = writeDict(entries, topOffsets(0, 0, 0, 0), 0).count
        let topIndexSize = index([[UInt8](repeating: 0, count: topSize)]).count
        var at = headerSize + nameIndex.count + topIndexSize + stringIndex.count + newGlobalSubrs.count
        let charsetAt = at
        at += charsetBlock.count
        let fdSelectAt = at
        at += newFDSelect.count
        let newCharStringsAt = at
        at += newCharStrings.count
        let fdArrayAt = at
        at += index(fds.indices.map { fontDictOf($0, 0) }).count
        var privateAts = [Int]()
        for b in blocks {
            privateAts.append(at)
            at += b.count
        }
        let fdArrayBytes = index(fds.indices.map { fontDictOf($0, privateAts[$0]) })
        let newTop = writeDict(entries, topOffsets(charsetAt, fdSelectAt, newCharStringsAt, fdArrayAt), 0)

        var out = [UInt8]()
        out.reserveCapacity(at)
        out.append(contentsOf: cff[0..<headerSize])
        out.append(contentsOf: nameIndex)
        appendIndex(&out, [newTop])
        out.append(contentsOf: stringIndex)
        out.append(contentsOf: newGlobalSubrs)
        out.append(contentsOf: charsetBlock)
        out.append(contentsOf: newFDSelect)
        out.append(contentsOf: newCharStrings)
        out.append(contentsOf: fdArrayBytes)
        for b in blocks {
            out.append(contentsOf: b)
        }
        if out.count != at {
            throw Subset.NotSubset()
        }
        return (out, kept)
    }

    // The operators of the Top DICT and of the Font DICTs whose first operands
    // are SIDs, and how many: version, Notice, FullName, FamilyName, Weight,
    // Copyright, PostScript, BaseFontName, ROS and FontName.
    static let sidOperands: [Int: Int] = [0: 1, 1: 1, 2: 1, 3: 1, 4: 1, 12 << 8: 1, 12 << 8 | 21: 1,
            12 << 8 | 22: 1, ros: 2, 12 << 8 | 38: 1]

    /// Gives the strings of the font that the entries name new SIDs, in the
    /// order they are first named, after the standard strings.
    final class Strings {
        let old: [[UInt8]]      // The String INDEX of the font
        var kept = [[UInt8]]()  // The strings named, in the order of their new SIDs
        var sids = [Int: Int]() // The new SID of each SID of the font named

        init(_ old: [[UInt8]]) {
            self.old = old
        }

        // Returns the entry with its SIDs of the font's strings renumbered,
        // its operands written again in their shortest form, or as it was when
        // it names none.
        func renumber(_ entry: Entry) throws -> Entry {
            guard let n = CFFSubset.sidOperands[entry.op] else {
                return entry
            }
            let operands = entry.integers ?? []
            if operands.count < n {
                throw Subset.NotSubset()
            }
            var raw = [UInt8]()
            var integers = [Int]()
            integers.reserveCapacity(operands.count)
            for (i, operand) in operands.enumerated() {
                var v = operand
                if i < n && v >= 391 {
                    if v - 391 >= old.count {
                        throw Subset.NotSubset()
                    }
                    if let sid = sids[v] {
                        v = sid
                    } else {
                        let sid = 391 + kept.count
                        sids[v] = sid
                        kept.append(old[v - 391])
                        v = sid
                    }
                }
                integers.append(v)
                CFFSubset.appendInteger(&raw, v)
            }
            if entry.op > 0xFF {
                raw.append(12)
                raw.append(UInt8(entry.op & 0xFF))
            } else {
                raw.append(UInt8(entry.op))
            }
            return Entry(op: entry.op, raw: raw, integers: integers)
        }
    }

    // Tells whether the DICT has an entry of the operator, of any operands.
    static func hasEntry(_ entries: [Entry], _ op: Int) -> Bool {
        return entries.contains { $0.op == op }
    }

    // Appends the integer of a DICT in its shortest form.
    static func appendInteger(_ out: inout [UInt8], _ value: Int) {
        if value >= -107 && value <= 107 {
            out.append(UInt8(value + 139))
        } else if value >= 108 && value <= 1131 {
            let v = value - 108
            out.append(UInt8((v >> 8) + 247))
            out.append(UInt8(truncatingIfNeeded: v))
        } else if value >= -1131 && value <= -108 {
            let v = -value - 108
            out.append(UInt8((v >> 8) + 251))
            out.append(UInt8(truncatingIfNeeded: v))
        } else if value >= -32768 && value <= 32767 {
            out.append(28)
            out.append(UInt8(truncatingIfNeeded: value >> 8))
            out.append(UInt8(truncatingIfNeeded: value))
        } else {
            appendOffset(&out, value)
        }
    }

    // Returns the runs of consecutive CIDs, each its first and the number
    // after it, nLeft, of at most max.
    static func ranges(_ cids: [Int], _ max: Int) -> [(Int, Int)] {
        var ranges = [(Int, Int)]()
        var i = 0
        while i < cids.count {
            var j = i
            while j + 1 < cids.count && cids[j + 1] == cids[j] + 1 && j + 1 - i <= max {
                j += 1
            }
            ranges.append((cids[i], j - i))
            i = j + 1
        }
        return ranges
    }

    // Returns the charset of the CIDs of the glyphs after .notdef, in the
    // format of the three that is the shortest, the first of those as short.
    static func charsetOf(_ cids: [Int]) -> [UInt8] {
        let ranges1 = ranges(cids, 0xFF)
        let ranges2 = ranges(cids, 0xFFFF)
        let size0 = 1 + 2 * cids.count
        let size1 = 1 + 3 * ranges1.count
        let size2 = 1 + 4 * ranges2.count
        var out = [UInt8]()
        if size0 <= size1 && size0 <= size2 {
            out.append(0)
            for cid in cids {
                out.append(UInt8(truncatingIfNeeded: cid >> 8))
                out.append(UInt8(truncatingIfNeeded: cid))
            }
        } else if size1 <= size2 {
            out.append(1)
            for (first, left) in ranges1 {
                out.append(UInt8(truncatingIfNeeded: first >> 8))
                out.append(UInt8(truncatingIfNeeded: first))
                out.append(UInt8(truncatingIfNeeded: left))
            }
        } else {
            out.append(2)
            for (first, left) in ranges2 {
                out.append(UInt8(truncatingIfNeeded: first >> 8))
                out.append(UInt8(truncatingIfNeeded: first))
                out.append(UInt8(truncatingIfNeeded: left >> 8))
                out.append(UInt8(truncatingIfNeeded: left))
            }
        }
        return out
    }

    // Returns the FDSelect of the Font DICT of each glyph, in format 3,
    // ranges, when that is shorter than format 0, a byte for each.
    static func fdSelectOf(_ fdOf: [Int]) -> [UInt8] {
        var firsts = [Int]()
        for (gid, fd) in fdOf.enumerated() where gid == 0 || fd != fdOf[gid - 1] {
            firsts.append(gid)
        }
        if 5 + 3 * firsts.count >= 1 + fdOf.count {
            var out: [UInt8] = [0]
            for fd in fdOf {
                out.append(UInt8(truncatingIfNeeded: fd))
            }
            return out
        }
        var out: [UInt8] = [3, UInt8(truncatingIfNeeded: firsts.count >> 8), UInt8(truncatingIfNeeded: firsts.count)]
        for first in firsts {
            out.append(UInt8(truncatingIfNeeded: first >> 8))
            out.append(UInt8(truncatingIfNeeded: first))
            out.append(UInt8(truncatingIfNeeded: fdOf[first]))
        }
        out.append(UInt8(truncatingIfNeeded: fdOf.count >> 8))
        out.append(UInt8(truncatingIfNeeded: fdOf.count))
        return out
    }
}
