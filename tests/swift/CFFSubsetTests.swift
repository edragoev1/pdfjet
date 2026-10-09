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
    static let plex = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf"
    static let han = "fonts/SourceHanSansJP/SourceHanSansJP-Regular.otf"

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
        return (CFFSubset.emptied(cff, charStrings, nil, 0), CFFSubset.emptied(cff, globals, nil, 0), top)
    }

    private func used(_ otf: OTF, _ text: String) -> [Bool] {
        var used = [Bool](repeating: false, count: 0x10000)
        for scalar in text.unicodeScalars {
            used[otf.unicodeToGID[Int(scalar.value)]] = true
        }
        return used
    }

    @Test func keepsTheCharstringsUsedAndEmptiesTheRest() throws {
        for path in [CFFSubsetTests.plex, CFFSubsetTests.han] {
            let font = try otf(path)
            let table = cff(font)
            let used = used(font, "Hello 日本語")
            let (subset, kept) = try CFFSubset.subset(table, used)
            let (whole, wholeGlobals, _) = try parts(table)
            let (glyphs, globals, _) = try parts(subset)
            #expect(glyphs.count == whole.count, "\(path)")
            for gid in 0..<glyphs.count {
                #expect(kept[gid] == (gid == 0 || used[gid]), "\(path) glyph \(gid)")
                #expect(glyphs[gid] == (kept[gid] ? whole[gid] : [14]), "\(path) glyph \(gid)")
            }
            var emptied = 0
            for i in 0..<globals.count {
                if globals[i] == [11] && wholeGlobals[i] != [11] {
                    emptied += 1
                } else {
                    #expect(globals[i] == wholeGlobals[i], "\(path) global subroutine \(i)")
                }
            }
            #expect(emptied > 0, "\(path)")
            #expect(subset.count <= table.count / 3, "\(path): \(subset.count) bytes")
        }
    }

    @Test func aCIDKeyedFontHasTheIdentityCharset() throws {
        // Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
        // numbers, and a PDF looks the glyphs of a CID-keyed font up by CID.
        let table = cff(try otf(CFFSubsetTests.han))
        for used in [nil, [Bool](repeating: false, count: 0x10000)] {
            let (subset, _) = try CFFSubset.subset(table, used)
            let (glyphs, _, top) = try parts(subset)
            let at = CFFSubset.entryOf(top, CFFSubset.charset)![0]
            let n = glyphs.count - 2
            #expect(Array(subset[at..<(at + 5)]) == [2, 0, 1, UInt8(n >> 8), UInt8(n & 0xFF)])
        }
        let (whole, _, _) = try parts(table)
        let (same, _, _) = try parts(try CFFSubset.subset(table, nil).0)
        #expect(same == whole)
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
}
