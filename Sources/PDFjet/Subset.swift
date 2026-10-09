/**
 * Subset.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// The subsets of the embedded fonts, .ttf and .otf files, embedded at
/// complete() with the outlines of the glyphs the document did not draw
/// emptied. The glyph numbers stay, so the widths, the character map and the
/// ToUnicode map are those of the whole font.
class Subset {
    /// The font program of a TrueType font, or the CFF table of an OpenType
    /// font with CFF outlines, kept until complete(). Fonts read from one file
    /// share one program, and the glyphs any of them drew.
    final class Program {
        var font = [UInt8]()        // The TrueType font, or the CFF table
        var cff = false
        var forbidden = false       // The fsType of a CFF font's OS/2 table forbids subsetting
        // The glyphs drawn, by glyph number, written in four hexadecimal digits.
        var used = [Bool](repeating: false, count: 0x10000)
        var whole = false           // Set by setSubset(false): the font is embedded whole

        init() {
            used[0] = true          // .notdef, drawn for a character the font lacks
        }
    }

    /// The font cannot be subset, and is embedded whole.
    struct NotSubset: Error {
    }

    // The tables a subset keeps: those a PDF reader draws a TrueType font with
    // (ISO 32000-1, 9.9), and the cmap, OS/2, name and post tables, which some
    // readers and checkers look at. The tables of shaping and of vertical text,
    // which PDFjet reads from the font and the PDF does not need, are left
    // out, and so is the DSIG table, the signature of the whole font.
    private static let subsetTables: Set<String> = [
        "head", "hhea", "hmtx", "maxp", "loca", "glyf",
        "cvt ", "fpgm", "prep", "gasp",
        "cmap", "OS/2", "name", "post"
    ]

    /// Gives the font the program of a font the PDF already has from the same
    /// file, if any, or else a new one of the font given.
    static func share(_ pdf: PDF, _ font: Font, _ data: [UInt8], _ cff: Bool, _ forbidden: Bool) {
        for f in pdf.fonts {
            if f.program != nil && f.name == font.name && f.checksum == font.checksum {
                font.program = f.program
                return
            }
        }
        let program = Program()
        program.font = data
        program.cff = cff
        program.forbidden = forbidden
        font.program = program
    }

    /// Writes the TrueType fonts at complete(), when every glyph they draw is
    /// known: each font program, its descriptor, its CID font and its
    /// ToUnicode map once, and the Type0 font of each Font under the number its
    /// pages refer to.
    static func addTrueTypeFonts(_ pdf: PDF) {
        for font in pdf.fonts {
            if font.program == nil {
                continue
            }
            let kept = embedProgram(pdf, font)
            addCIDSetObject(pdf, font, kept)
            FontWriter.addFontDescriptorObject(pdf, font, font.baseFont)
            FontWriter.addCIDFontDictionaryObject(pdf, font, font.baseFont, kept)
            FontWriter.addToUnicodeCMapObject(pdf, font, kept)

            pdf.newObj(font.objNumber)
            pdf.append(Token.beginDictionary)
            pdf.append("/Type /Font\n")
            pdf.append("/Subtype /Type0\n")
            pdf.append("/BaseFont /")
            pdf.append(Array(font.baseFont.utf8))
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
        }
        // The programs are no longer needed.
        for font in pdf.fonts {
            font.program = nil
        }
    }

    /// Writes the font program, a subset unless it is to be whole, sets the
    /// name of the font, with the tag of a subset, and returns the glyphs the
    /// subset keeps, or nil for a whole font. A font of the same program
    /// embedded before is not written again: its name and object numbers are
    /// used.
    private static func embedProgram(_ pdf: PDF, _ font: Font) -> [Bool]? {
        for f in pdf.fonts {
            if f === font {
                break
            }
            if f.fileObjNumber != 0 && f.name == font.name && f.checksum == font.checksum {
                font.fileObjNumber = f.fileObjNumber
                font.cidSetObjNumber = f.cidSetObjNumber
                font.baseFont = f.baseFont
                return f.kept
            }
        }

        let program = font.program!
        font.baseFont = font.name
        var data = program.font
        var compressed = [UInt8]()
        var length = data.count
        let subset = (program.whole || program.forbidden) ? nil :
                (program.cff ? try? CFFSubset.subset(data, program.used) : try? subsetTrueType(data, program.used))
        if let (subset, kept) = subset {
            FlateEncode(&compressed, subset)
            length = subset.count
            font.kept = kept
            font.baseFont = subsetTag(font.checksum, kept) + "+" + font.name
        } else {    // Whole
            if program.cff, let (whole, _) = try? CFFSubset.subset(data, nil) {
                // Written again for the identity charset of a CID-keyed font.
                data = whole
            }
            FlateEncode(&compressed, data)
        }

        let metadataObjNumber = pdf.addMetadataObject(font.info, true)
        let encrypted = pdf.encrypted(compressed)
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        if program.cff {
            pdf.append("/Subtype /CIDFontType0C\n")
        }
        pdf.append("/Filter /FlateDecode\n")
        if !program.cff {
            pdf.append("/Length1 ")
            pdf.append(length)
            pdf.append(Token.newline)
        }
        if metadataObjNumber != -1 {
            pdf.append("/Metadata ")
            pdf.append(metadataObjNumber)
            pdf.append(" 0 R\n")
        }
        pdf.append("/Length ")
        pdf.append(encrypted.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(encrypted)
        pdf.append(Token.endStream)
        pdf.endObj()
        font.fileObjNumber = pdf.getObjNumber()
        return font.kept
    }

    /// Writes, for PDF/A-1, which asks it of a subset, the CIDSet of the glyphs
    /// the subset keeps: a bit for each glyph number, the first in the high bit
    /// of the first byte.
    private static func addCIDSetObject(_ pdf: PDF, _ font: Font, _ kept: [Bool]?) {
        guard let kept = kept, font.cidSetObjNumber == 0,
                pdf.compliance == Compliance.PDF_A_1A || pdf.compliance == Compliance.PDF_A_1B else {
            return
        }
        var bits = [UInt8](repeating: 0, count: (kept.count + 7) / 8)
        for gid in 0..<kept.count where kept[gid] {
            bits[gid / 8] |= UInt8(0x80 >> (gid % 8))
        }
        FontWriter.addCompressedStream(pdf, bits)
        font.cidSetObjNumber = pdf.getObjNumber()
    }

    /// Returns the six capitals that begin the name of a subset, from the font
    /// and the glyphs it keeps, so that another subset of the font has another
    /// tag, and the same document the same one, in the four ports.
    static func subsetTag(_ checksum: UInt64, _ kept: [Bool]) -> String {
        var hash = checksum
        for gid in 0..<kept.count where kept[gid] {
            hash = Font.fold(hash, gid)
        }
        var tag = String()
        for _ in 0..<6 {
            tag.append(Character(Unicode.Scalar(UInt8(65 + hash % 26))))
            hash /= 26
        }
        return tag
    }

    private static func u16(_ ttf: [UInt8], _ at: Int) throws -> Int {
        if at < 0 || at + 2 > ttf.count {
            throw NotSubset()
        }
        return Int(ttf[at]) << 8 | Int(ttf[at + 1])
    }

    private static func u32(_ ttf: [UInt8], _ at: Int) throws -> Int {
        if at < 0 || at + 4 > ttf.count {
            throw NotSubset()
        }
        return Int(ttf[at]) << 24 | Int(ttf[at + 1]) << 16 | Int(ttf[at + 2]) << 8 | Int(ttf[at + 3])
    }

    /// A table of the font: its tag, offset and length.
    private struct Table {
        let tag: String
        let offset: Int
        let length: Int
    }

    /// Returns the TrueType font with the outlines of the glyphs not used
    /// emptied, and the glyphs kept: those used, glyph 0 and the parts of the
    /// composite glyphs kept. Every glyph keeps its number, so the tables
    /// kept, subsetTables, are copied as they are, but for the glyf and loca
    /// tables made again, the head table's checksum adjustment, and the post
    /// table without the names of the glyphs. A font that forbids subsetting
    /// in the fsType of its OS/2 table, or whose tables cannot be read, throws
    /// NotSubset, and is embedded whole.
    static func subsetTrueType(_ ttf: [UInt8], _ used: [Bool]) throws -> ([UInt8], [Bool]) {
        let numTables = try u16(ttf, 4)
        var tables = [Table]()
        var head: Table?, loca: Table?, glyf: Table?, maxp: Table?, os2: Table?
        for i in 0..<numTables {
            let entry = 12 + 16 * i
            let offset = try u32(ttf, entry + 8)
            let length = try u32(ttf, entry + 12)
            if offset + length > ttf.count {
                throw NotSubset()
            }
            let tag = String(decoding: ttf[entry..<(entry + 4)].map { $0 & 0x7F }, as: UTF8.self)
            let table = Table(tag: tag, offset: offset, length: length)
            tables.append(table)
            switch tag {
            case "head": head = table
            case "loca": loca = table
            case "glyf": glyf = table
            case "maxp": maxp = table
            case "OS/2": os2 = table
            default: break
            }
        }
        guard let head = head, let maxp = maxp, let loca = loca, let glyf = glyf,
                head.length >= 54, maxp.length >= 6, loca.length > 0, glyf.length > 0 else {
            throw NotSubset()
        }
        if let os2 = os2, os2.length >= 10, try u16(ttf, os2.offset + 8) & 0x0100 != 0 {
            throw NotSubset()   // No subsetting
        }
        let numGlyphs = try u16(ttf, maxp.offset + 4)
        let longOffsets = try u16(ttf, head.offset + 50)
        if numGlyphs == 0 || loca.length < (numGlyphs + 1) * (2 + 2 * longOffsets) {
            throw NotSubset()
        }
        var starts = [Int](repeating: 0, count: numGlyphs)
        var ends = [Int](repeating: 0, count: numGlyphs)
        for gid in 0..<numGlyphs {
            var start: Int
            var end: Int
            if longOffsets == 1 {
                start = try u32(ttf, loca.offset + 4 * gid)
                end = try u32(ttf, loca.offset + 4 * gid + 4)
            } else {
                start = try 2 * u16(ttf, loca.offset + 2 * gid)
                end = try 2 * u16(ttf, loca.offset + 2 * gid + 2)
            }
            starts[gid] = glyf.offset + start
            ends[gid] = glyf.offset + end
            if start > end || end > glyf.length {
                starts[gid] = -1    // Read only if kept
            }
        }

        // The glyphs kept, with the parts of the composite glyphs.
        var keep = [Bool](repeating: false, count: numGlyphs)
        var stack = [Int]()
        for gid in 0..<min(numGlyphs, used.count) where used[gid] || gid == 0 {
            keep[gid] = true
            stack.append(gid)
        }
        while let gid = stack.popLast() {
            let start = starts[gid]
            let end = ends[gid]
            if start < 0 {
                throw NotSubset()
            }
            if end - start < 10 {
                continue    // No outline
            }
            if try u16(ttf, start) < 0x8000 {
                continue    // A simple glyph
            }
            var at = start + 10
            while true {
                let flags = try u16(ttf, at)
                let component = try u16(ttf, at + 2)
                if at + 4 > end || component >= numGlyphs {
                    throw NotSubset()
                }
                if !keep[component] {
                    keep[component] = true
                    stack.append(component)
                }
                at += 4
                at += (flags & 0x0001) != 0 ? 4 : 2     // ARG_1_AND_2_ARE_WORDS
                if (flags & 0x0008) != 0 {              // WE_HAVE_A_SCALE
                    at += 2
                } else if (flags & 0x0040) != 0 {       // WE_HAVE_AN_X_AND_Y_SCALE
                    at += 4
                } else if (flags & 0x0080) != 0 {       // WE_HAVE_A_TWO_BY_TWO
                    at += 8
                }
                if (flags & 0x0020) == 0 {              // MORE_COMPONENTS
                    break
                }
            }
        }

        // The glyf and loca tables, the kept glyphs copied, each padded to a
        // multiple of four bytes, the others empty.
        var newGlyf = [UInt8]()
        var newLoca = [UInt8]()
        newLoca.reserveCapacity((numGlyphs + 1) * (2 + 2 * longOffsets))
        func appendOffset(_ offset: Int) {
            if longOffsets == 1 {
                newLoca.append(contentsOf: [UInt8(truncatingIfNeeded: offset >> 24), UInt8(truncatingIfNeeded: offset >> 16),
                        UInt8(truncatingIfNeeded: offset >> 8), UInt8(truncatingIfNeeded: offset)])
            } else {
                newLoca.append(contentsOf: [UInt8(truncatingIfNeeded: offset >> 9), UInt8(truncatingIfNeeded: offset >> 1)])
            }
        }
        for gid in 0..<numGlyphs {
            appendOffset(newGlyf.count)
            if keep[gid] {
                newGlyf.append(contentsOf: ttf[starts[gid]..<ends[gid]])
                while newGlyf.count % 4 != 0 {
                    newGlyf.append(0)
                }
            }
        }
        appendOffset(newGlyf.count)
        if longOffsets == 0 && newGlyf.count > 0x1FFFE {
            throw NotSubset()
        }

        // The font again, its tables in the order of their tags.
        tables.sort { Array($0.tag.utf8).lexicographicallyPrecedes(Array($1.tag.utf8)) }
        var tags = [String]()
        var data = [[UInt8]]()
        for t in tables {
            if t.tag == "glyf" {
                data.append(newGlyf)
            } else if t.tag == "loca" {
                data.append(newLoca)
            } else if t.tag == "head" {
                var h = Array(ttf[t.offset..<(t.offset + t.length)])
                h[8] = 0; h[9] = 0; h[10] = 0; h[11] = 0    // checkSumAdjustment, set below
                data.append(h)
            } else if t.tag == "post" {
                if t.length < 32 {
                    continue
                }
                // Version 3, without the names of the glyphs.
                var p = Array(ttf[t.offset..<(t.offset + 32)])
                p[0] = 0; p[1] = 3; p[2] = 0; p[3] = 0
                data.append(p)
            } else if subsetTables.contains(t.tag) {
                data.append(Array(ttf[t.offset..<(t.offset + t.length)]))
            } else {
                continue
            }
            tags.append(t.tag)
        }
        let count = data.count
        var searchRange = 1
        var entrySelector = 0
        while searchRange * 2 <= count {
            searchRange *= 2
            entrySelector += 1
        }
        var out = [UInt8]()
        out.reserveCapacity(12 + 16 * count + data.reduce(0) { $0 + (($1.count + 3) & ~3) })
        func be16(_ v: Int) {
            out.append(UInt8(truncatingIfNeeded: v >> 8))
            out.append(UInt8(truncatingIfNeeded: v))
        }
        func be32(_ v: UInt32) {
            out.append(UInt8(truncatingIfNeeded: v >> 24))
            out.append(UInt8(truncatingIfNeeded: v >> 16))
            out.append(UInt8(truncatingIfNeeded: v >> 8))
            out.append(UInt8(truncatingIfNeeded: v))
        }
        out.append(contentsOf: ttf[0..<4])
        be16(count)
        be16(16 * searchRange)
        be16(entrySelector)
        be16(16 * count - 16 * searchRange)
        var offset = 12 + 16 * count
        var headAt = 0
        for i in 0..<count {
            out.append(contentsOf: Array(tags[i].utf8))
            be32(tableChecksum(data[i]))
            be32(UInt32(offset))
            be32(UInt32(data[i].count))
            if tags[i] == "head" {
                headAt = offset
            }
            offset += (data[i].count + 3) & ~3
        }
        for d in data {
            out.append(contentsOf: d)
            while out.count % 4 != 0 {
                out.append(0)
            }
        }
        let adjustment = UInt32(0xB1B0AFBA) &- tableChecksum(out)
        out[headAt + 8] = UInt8(truncatingIfNeeded: adjustment >> 24)
        out[headAt + 9] = UInt8(truncatingIfNeeded: adjustment >> 16)
        out[headAt + 10] = UInt8(truncatingIfNeeded: adjustment >> 8)
        out[headAt + 11] = UInt8(truncatingIfNeeded: adjustment)
        return (out, keep)
    }

    /// Returns the sum of the big-endian 32-bit words of the data, the last one
    /// padded with zeros.
    static func tableChecksum(_ data: [UInt8]) -> UInt32 {
        var sum: UInt32 = 0
        var i = 0
        while i < data.count {
            var word: UInt32 = 0
            for j in 0..<4 {
                word <<= 8
                if i + j < data.count {
                    word |= UInt32(data[i + j])
                }
            }
            sum = sum &+ word
            i += 4
        }
        return sum
    }
}
