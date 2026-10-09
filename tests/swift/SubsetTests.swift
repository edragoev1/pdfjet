/**
 * SubsetTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The tests of the subsets of the TrueType fonts.
@Suite struct SubsetTests {
    private func fontBytes(_ path: String) throws -> [UInt8] {
        return [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(path))))
    }

    private func u16(_ font: [UInt8], _ at: Int) -> Int {
        return Int(font[at]) << 8 | Int(font[at + 1])
    }

    private func u32(_ font: [UInt8], _ at: Int) -> Int {
        return Int(font[at]) << 24 | Int(font[at + 1]) << 16 | Int(font[at + 2]) << 8 | Int(font[at + 3])
    }

    // Returns where the directory entry of the table begins, or -1.
    private func entry(_ font: [UInt8], _ name: String) -> Int {
        for i in 0..<u16(font, 4) {
            let entry = 12 + 16 * i
            if TestSupport.latin1(Array(font[entry..<(entry + 4)])) == name {
                return entry
            }
        }
        return -1
    }

    private func table(_ font: [UInt8], _ name: String) -> Int {
        let entry = entry(font, name)
        #expect(entry != -1, "the font has no \(name) table")
        return u32(font, entry + 8)
    }

    // Returns the bytes of the glyph in the glyf table, at the offsets its
    // loca table gives.
    private func glyph(_ font: [UInt8], _ gid: Int) -> [UInt8] {
        let loca = table(font, "loca")
        let glyf = table(font, "glyf")
        var start: Int
        var end: Int
        if u16(font, table(font, "head") + 50) == 1 {
            start = u32(font, loca + 4 * gid)
            end = u32(font, loca + 4 * gid + 4)
        } else {
            start = 2 * u16(font, loca + 2 * gid)
            end = 2 * u16(font, loca + 2 * gid + 2)
        }
        return Array(font[(glyf + start)..<(glyf + end)])
    }

    private func used(_ gids: Int...) -> [Bool] {
        var used = [Bool](repeating: false, count: 0x10000)
        for gid in gids {
            used[gid] = true
        }
        return used
    }

    @Test func keepsTheGlyphsUsedAndThePartsOfTheComposites() throws {
        // In Noto Sans, Ä (134) is made of A (36) and the dieresis (106), and
        // ǅ (913) of D (39), z (93) and the caron (331).
        let ttf = try fontBytes("fonts/NotoSans/NotoSans-Regular.ttf")
        let (subset, kept) = try Subset.subsetTrueType(ttf, used(134, 913))
        #expect(kept.count == 4503)
        let want: Set<Int> = [0, 134, 36, 106, 913, 39, 93, 331]
        for gid in 0..<kept.count {
            #expect(want.contains(gid) == kept[gid], "glyph \(gid)")
            let glyph = glyph(subset, gid)
            if kept[gid] {
                // The glyph as it was, padded to four bytes.
                let whole = self.glyph(ttf, gid)
                #expect(Array(glyph.prefix(whole.count)) == whole && glyph.count - whole.count < 4, "glyph \(gid)")
            } else {
                #expect(glyph.isEmpty, "glyph \(gid)")
            }
        }
        #expect(subset.count <= ttf.count / 4, "the subset is \(subset.count) bytes")
        // The other tables a reader needs as they were; shaping left out.
        for name in ["GPOS", "GSUB", "GDEF"] {
            #expect(entry(subset, name) == -1, "the subset has the \(name) table")
        }
        #expect(u32(subset, table(subset, "post")) == 0x00030000)
        for name in ["cmap", "hmtx", "hhea", "name", "OS/2", "maxp"] {
            let length = u32(ttf, entry(ttf, name) + 12)
            let at = table(ttf, name)
            let subsetAt = table(subset, name)
            #expect(Array(ttf[at..<(at + length)]) == Array(subset[subsetAt..<(subsetAt + length)]),
                    "the \(name) table changed")
        }
        // The whole font sums to the magic number of its head table.
        #expect(Subset.tableChecksum(subset) == 0xB1B0AFBA)
    }

    @Test func subsetsAFontWithShortOffsets() throws {
        let ttf = try fontBytes("fonts/NotoSansThai/NotoSansThai-Regular.ttf")
        #expect(u16(ttf, table(ttf, "head") + 50) == 0)
        let (subset, _) = try Subset.subsetTrueType(ttf, used(5))
        let whole = glyph(ttf, 5)
        #expect(Array(glyph(subset, 5).prefix(whole.count)) == whole)
        #expect(glyph(subset, 6).isEmpty)
    }

    @Test func isRefusedByAFontWhoseLicenseForbidsIt() throws {
        var ttf = try fontBytes("fonts/NotoSans/NotoSans-Regular.ttf")
        let os2 = table(ttf, "OS/2")
        ttf[os2 + 8] = 0x01
        ttf[os2 + 9] = 0x00
        #expect(throws: Subset.NotSubset.self) {
            _ = try Subset.subsetTrueType(ttf, used(36))
        }
    }

    // Draws the texts, each with a font made from the file, and returns the
    // document.
    private func document(_ compliance: Compliance, _ path: String, _ subset: Bool,
            _ texts: String...) throws -> String {
        let memory = MemoryPDF(compliance)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        for (i, text) in texts.enumerated() {
            let font = try Font(memory.pdf, TestSupport.path(path))
            font.setSubset(subset)
            TextLine(font, text).setLocation(50, Float(50 + 20 * i)).drawOn(page)
        }
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    // The groups of the first match of the pattern from the offset, and where
    // the match ends.
    private func match(_ pattern: String, _ raw: String, from: Int = 0) throws -> ([String], Int)? {
        let text = raw as NSString
        let regex = try NSRegularExpression(pattern: pattern)
        guard let m = regex.firstMatch(in: raw, range: NSRange(location: from, length: text.length - from)) else {
            return nil
        }
        var groups = [String]()
        for i in 0..<m.numberOfRanges {
            groups.append(text.substring(with: m.range(at: i)))
        }
        return (groups, m.range.location + m.range.length)
    }

    private func count(_ pattern: String, _ raw: String) throws -> Int {
        return try NSRegularExpression(pattern: pattern)
                .numberOfMatches(in: raw, range: NSRange(location: 0, length: (raw as NSString).length))
    }

    private func inflate(_ raw: String, _ at: Int, _ length: Int) throws -> [UInt8] {
        let bytes = Array(raw.unicodeScalars.map { UInt8($0.value) })
        return try TestSupport.inflate(Array(bytes[at..<(at + length)]))
    }

    // Returns the font program embedded in the document.
    private func program(_ raw: String) throws -> [UInt8] {
        let found = try match("/Length1 (\\d+)\n(?:/Metadata \\d+ 0 R\n)?/Length (\\d+)\n>>\nstream\n", raw)
        let (groups, end) = try #require(found, "no font program")
        let program = try inflate(raw, end, Int(groups[2])!)
        #expect(program.count == Int(groups[1])!, "/Length1")
        return program
    }

    private let tagged = "/BaseFont /([A-Z]{6}\\+NotoSans-Regular)\n"

    @Test func aTrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() throws {
        for path in ["fonts/NotoSans/NotoSans-Regular.ttf"] {
            let raw = try document(Compliance.PDF_1_7, path, true, "Ä")
            let (groups, _) = try #require(try match(tagged, raw), "\(path)")
            #expect(raw.contains("/FontName /\(groups[1])\n"), "\(path)")
            #expect(try count(tagged, raw) == 2, "\(path)")
            let program = try program(raw)
            #expect(!glyph(program, 134).isEmpty && !glyph(program, 36).isEmpty, "\(path)")
            #expect(glyph(program, 37).isEmpty, "\(path)")
            // The widths of the glyphs kept: .notdef, A, the dieresis and Ä.
            #expect(try match("/W \\[\n0\\[\\d+ \\]\n36\\[\\d+ \\]\n106\\[\\d+ \\]\n134\\[\\d+ \\]\\]\n", raw) != nil,
                    "\(path): the widths")
            #expect(!raw.contains("/CIDSet"), "\(path)")
        }
    }

    @Test func aFontSetToStayWholeIsEmbeddedWhole() throws {
        let ttf = try fontBytes("fonts/NotoSans/NotoSans-Regular.ttf")
        for path in ["fonts/NotoSans/NotoSans-Regular.ttf"] {
            let raw = try document(Compliance.PDF_1_7, path, false, "Ä")
            #expect(raw.contains("/BaseFont /NotoSans-Regular\n") && !raw.contains("+NotoSans"), "\(path)")
            #expect(try program(raw) == ttf, "\(path)")
        }
    }

    @Test func aStreamFontIsSubsetAsItsTrueTypeFontIs() throws {
        // The .ttf.stream of JetBrains Mono is its .ttf, compressed: subset or
        // whole, the two embed the same font program.
        for subset in [true, false] {
            #expect(try program(document(Compliance.PDF_1_7, "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf", subset, "Hello")) ==
                    (try program(document(Compliance.PDF_1_7, "tests/data/stream-fonts/JetBrainsMono-Regular.ttf.stream", subset, "Hello"))),
                    "subset \(subset)")
        }
    }

    @Test func twoFontsOfOneFileShareOneSubset() throws {
        let raw = try document(Compliance.PDF_1_7, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A", "B")
        #expect(try count("/Length1 ", raw) == 1)
        let program = try program(raw)
        #expect(!glyph(program, 36).isEmpty && !glyph(program, 37).isEmpty)
        // Two Type0 fonts and their CID font.
        #expect(try count(tagged, raw) == 3)
    }

    @Test func aPDFA1HasTheCIDSetOfTheGlyphsKept() throws {
        let raw = try document(Compliance.PDF_A_1B, "fonts/NotoSans/NotoSans-Regular.ttf", true, "A")
        let (groups, _) = try #require(try match("/CIDSet (\\d+) 0 R\n", raw), "no CIDSet")
        let at = (raw as NSString).range(of: "\n\(groups[1]) 0 obj\n").location
        #expect(at != NSNotFound)
        let (lengths, end) = try #require(try match("/Length (\\d+)\n>>\nstream\n", raw, from: at))
        let bits = try inflate(raw, end, Int(lengths[1])!)
        // Glyphs 0 and 36 of 4503.
        var want = [UInt8](repeating: 0, count: (4503 + 7) / 8)
        want[0] = 0x80
        want[36 / 8] |= UInt8(0x80 >> (36 % 8))
        #expect(bits == want)
    }
}
