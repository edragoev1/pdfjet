/**
 * CFFSubsetTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The tests of the subsets of the fonts with CFF outlines.
@Suite struct CFFSubsetTests {
    static let plex = "tests/fonts/IBMPlexSans-Regular.otf"
    static let han = "tests/fonts/SourceHanSansJP-Regular.otf"

    private func otf(_ path: String) throws -> OTF {
        return try OTF(InputStream(data: try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(path)))))
    }

    private func cff(_ otf: OTF) -> [UInt8] {
        return Array(otf.buf[otf.cffOff!..<(otf.cffOff! + otf.cffLen!)])
    }

    // The charstrings, the global subroutines and the Top DICT of a CFF table.
    private func parts(_ cff: [UInt8]) throws -> ([[UInt8]], [[UInt8]], [CFFSubset.Entry]) {
        let names = try CFFSubset.readIndex(cff, Int(cff[2]))
        let tops = try CFFSubset.readIndex(cff, names.end)
        let strings = try CFFSubset.readIndex(cff, tops.end)
        let globals = try CFFSubset.readIndex(cff, strings.end)
        let top = try CFFSubset.readDict(cff, tops.objects[0], tops.objects[1])
        let charStrings = try CFFSubset.readIndex(cff, CFFSubset.entryOf(top, CFFSubset.charStrings)![0])
        return (CFFSubset.emptied(cff, charStrings, nil), CFFSubset.emptied(cff, globals, nil), top)
    }

    private func used(_ otf: OTF, _ text: String) -> [Bool] {
        var used = [Bool](repeating: false, count: 0x10000)
        for scalar in text.unicodeScalars {
            used[otf.unicodeToGID[Int(scalar.value)]] = true
        }
        return used
    }

    // The CIDs the charset of the subset gives its glyphs after .notdef, of
    // formats 0, 1 and 2.
    private func charsetCIDs(_ cff: [UInt8], _ top: [CFFSubset.Entry], _ glyphs: Int) -> [Int] {
        var at = CFFSubset.entryOf(top, CFFSubset.charset)![0]
        let format = cff[at]
        at += 1
        var cids = [Int]()
        while cids.count < glyphs - 1 {
            switch format {
            case 0:
                cids.append(Int(cff[at]) << 8 | Int(cff[at + 1]))
                at += 2
            case 1, 2:
                let first = Int(cff[at]) << 8 | Int(cff[at + 1])
                var left = Int(cff[at + 2])
                at += 3
                if format == 2 {
                    left = left << 8 | Int(cff[at])
                    at += 1
                }
                cids.append(contentsOf: first...(first + left))
            default:
                Issue.record("charset format \(format)")
                return cids
            }
        }
        return cids
    }

    @Test func isACIDFontOfTheGlyphsUsedAlone() throws {
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let font = try otf(path)
            let table = cff(font)
            let used = used(font, "Hello 日本語")
            let (subset, kept) = try CFFSubset.subset(table, used)
            let (whole, wholeGlobals, _) = try parts(table)
            let (glyphs, globals, top) = try parts(subset)
            try #require(kept.count == whole.count, "\(path): kept is of \(kept.count) glyphs")
            // The glyphs kept, .notdef and those used, alone, in their order
            var keptGIDs = [Int]()
            for gid in 0..<kept.count {
                #expect(kept[gid] == (gid == 0 || used[gid]), "\(path) glyph \(gid)")
                if kept[gid] && gid > 0 {
                    keptGIDs.append(gid)
                }
            }
            try #require(glyphs.count == keptGIDs.count + 1, "\(path): \(glyphs.count) glyphs")
            // A CIDFont, whose charset gives each glyph its number in the font
            // as its CID, which the content streams draw it by
            #expect(CFFSubset.entryOf(top, CFFSubset.ros) != nil && CFFSubset.entryOf(top, CFFSubset.fdArray) != nil,
                    "\(path): the subset is not a CIDFont")
            #expect(CFFSubset.entryOf(top, CFFSubset.encoding) == nil, "\(path): the subset has an Encoding")
            #expect(charsetCIDs(subset, top, glyphs.count) == keptGIDs, "\(path): the CIDs")
            // Each glyph as it was, .notdef first
            #expect(glyphs[0] == whole[0], "\(path): .notdef is not as it was")
            for (i, gid) in keptGIDs.enumerated() {
                #expect(glyphs[i + 1] == whole[gid], "\(path) glyph \(gid)")
            }
            // The global subroutines keep their numbers: those called as they
            // were, the others emptied to no bytes
            try #require(globals.count == wholeGlobals.count, "\(path): \(globals.count) global subroutines")
            var emptied = 0
            for i in 0..<globals.count {
                if globals[i].isEmpty && !wholeGlobals[i].isEmpty {
                    emptied += 1
                } else {
                    #expect(globals[i] == wholeGlobals[i], "\(path) global subroutine \(i)")
                }
            }
            #expect(emptied > 0, "\(path)")
            #expect(subset.count <= table.count / 10, "\(path): \(subset.count) bytes of \(table.count)")
        }
    }

    @Test func ofEveryGlyphHasTheIdentityCharset() throws {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
        // written whole, each glyph's CID is its number.
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let table = cff(try otf(path))
            let (whole, _, _) = try parts(table)
            let (subset, kept) = try CFFSubset.subset(table, nil)
            let (glyphs, _, top) = try parts(subset)
            try #require(glyphs.count == whole.count && kept.count == whole.count, "\(path): \(glyphs.count) glyphs")
            #expect(charsetCIDs(subset, top, glyphs.count) == Array(1..<glyphs.count), "\(path): the CIDs")
            #expect(glyphs == whole, "\(path)")
        }
    }

    private func document(_ compliance: Compliance, _ path: String, _ subset: Bool, _ text: String) throws -> String {
        let memory = MemoryPDF(compliance)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.path(path))
        font.setSubset(subset)
        TextLine(font, text).setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    private func tagged(_ raw: String) -> Bool {
        return raw.range(of: "/FontName /[A-Z]{6}\\+", options: .regularExpression) != nil
    }

    @Test func aFontWithCFFOutlinesIsEmbeddedAsASubset() throws {
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let raw = try document(Compliance.PDF_A_1B, path, true, "Hello 日本語")
            #expect(tagged(raw), "\(path)")
            #expect(raw.contains("/Subtype /CIDFontType0C\n") && raw.contains("/FontFile3 "), "\(path)")
            #expect(!raw.contains("/Length1 "), "\(path)")
            #expect(raw.contains("/CIDSet "), "\(path)")
        }
        let raw = try document(Compliance.PDF_1_7, CFFSubsetTests.plex, false, "Hello")
        #expect(!tagged(raw) && raw.contains("/FontFile3 "))
    }

    @Test func aFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole() throws {
        var data = [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(CFFSubsetTests.plex))))
        let tables = Int(data[4]) << 8 | Int(data[5])
        for i in 0..<tables {
            let entry = 12 + 16 * i
            if TestSupport.latin1(Array(data[entry..<(entry + 4)])) == "OS/2" {
                let at = Int(data[entry + 8]) << 24 | Int(data[entry + 9]) << 16 |
                        Int(data[entry + 10]) << 8 | Int(data[entry + 11])
                data[at + 8] = 0x01
                data[at + 9] = 0x00
            }
        }
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, InputStream(data: Data(data)))
        TextLine(font, "Hello").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        #expect(!tagged(TestSupport.latin1(memory.bytes)))
    }

    @Test func anAccentedLetterDrawnWithSeacIsRefused() throws {
        // Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
        // it is drawn from were emptied, and it drew blank (the review of
        // 9 October 2026). The font is embedded whole.
        let font = try otf(CFFSubsetTests.plex)
        var table = cff(font)
        let names = try CFFSubset.readIndex(table, Int(table[2]))
        let tops = try CFFSubset.readIndex(table, names.end)
        let top = try CFFSubset.readDict(table, tops.objects[0], tops.objects[1])
        let charStrings = try CFFSubset.readIndex(table, CFFSubset.entryOf(top, CFFSubset.charStrings)![0])
        let gid = font.unicodeToGID[Int(("Á" as Unicode.Scalar).value)]
        let at = charStrings.objects[gid]
        try #require(charStrings.objects[gid + 1] - at >= 6, "the charstring of Á is too short")
        table.replaceSubrange(at..<(at + 6), with: [139, 139, 204, 247, 86, 14])
        #expect(throws: Subset.NotSubset.self) {
            _ = try CFFSubset.subset(table, used(font, "Á"))
        }
    }
}
