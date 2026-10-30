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
        return try TestSupport.fontBytes(path)
    }

    // IBM Plex Sans, which has the short offsets of loca; the subsets of a
    // font with the long ones read Cascadia Mono or IBM Plex Math.
    private let plex = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"

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
        // In Cascadia Mono, whose loca has the long offsets, as that of Noto
        // Sans, which this test read before 9.1.7, had: Ä (16) is made of A
        // (1) and the dieresis of capitals (3051), and ặ (229) of a (225),
        // the dot below (3029) and the breve (3020); 4,319 glyphs. Noto Sans
        // had Ä (134) of A (36) and the dieresis (106), and ǅ (913) of three.
        let ttf = try fontBytes("fonts/CascadiaMono/CascadiaMono-Regular.ttf")
        #expect(u16(ttf, table(ttf, "head") + 50) == 1)
        let (subset, kept) = try Subset.subsetTrueType(ttf, used(16, 229))
        #expect(kept.count == 4319)
        let want: Set<Int> = [0, 16, 1, 3051, 229, 225, 3029, 3020]
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
        // The other tables a reader needs as they were; shaping left out, and
        // the cmap and name tables, which a CIDFontType2 does not need.
        for name in ["GPOS", "GSUB", "GDEF", "cmap", "name"] {
            #expect(entry(subset, name) == -1, "the subset has the \(name) table")
        }
        #expect(u32(subset, table(subset, "post")) == 0x00030000)
        for name in ["hmtx", "hhea", "OS/2", "maxp"] {
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
        // IBM Plex Sans Thai, whose loca has the short offsets, as Noto Sans
        // Thai's had.
        let ttf = try fontBytes("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf")
        #expect(u16(ttf, table(ttf, "head") + 50) == 0)
        let (subset, _) = try Subset.subsetTrueType(ttf, used(5))
        let whole = glyph(ttf, 5)
        #expect(Array(glyph(subset, 5).prefix(whole.count)) == whole)
        #expect(glyph(subset, 6).isEmpty)
    }

    @Test func isRefusedByAFontWhoseLicenseForbidsIt() throws {
        var ttf = try fontBytes(plex)
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

    private let tagged = "/BaseFont /([A-Z]{6}\\+IBMPlexSans)\n"

    @Test func aTrueTypeFontIsEmbeddedAsASubsetUnderATaggedName() throws {
        for path in [plex] {
            let raw = try document(Compliance.PDF_1_7, path, true, "Ä")
            let (groups, _) = try #require(try match(tagged, raw), "\(path)")
            #expect(raw.contains("/FontName /\(groups[1])\n"), "\(path)")
            #expect(try count(tagged, raw) == 2, "\(path)")
            let program = try program(raw)
            // In IBM Plex Sans, Ä (395) is made of A (33) and the dieresis
            // of capitals (971), and B is 34. Noto Sans had Ä (134), A (36),
            // the dieresis (106) and B (37).
            #expect(!glyph(program, 395).isEmpty && !glyph(program, 33).isEmpty, "\(path)")
            #expect(glyph(program, 34).isEmpty, "\(path)")
            // The widths of the glyphs kept: .notdef, A, Ä and the dieresis.
            #expect(try match("/W \\[\n0\\[\\d+ \\]\n33\\[\\d+ \\]\n395\\[\\d+ \\]\n971\\[\\d+ \\]\\]\n", raw) != nil,
                    "\(path): the widths")
            #expect(!raw.contains("/CIDSet"), "\(path)")
        }
    }

    @Test func aFontSetToStayWholeIsEmbeddedWhole() throws {
        let ttf = try fontBytes(plex)
        for path in [plex] {
            let raw = try document(Compliance.PDF_1_7, path, false, "Ä")
            #expect(raw.contains("/BaseFont /IBMPlexSans\n") && !raw.contains("+IBMPlexSans"), "\(path)")
            #expect(try program(raw) == ttf, "\(path)")
        }
    }

    @Test func twoFontsOfOneFileShareOneSubset() throws {
        let raw = try document(Compliance.PDF_1_7, plex, true, "A", "B")
        #expect(try count("/Length1 ", raw) == 1)
        let program = try program(raw)
        // A and B, 33 and 34 in IBM Plex Sans; 36 and 37 in Noto Sans.
        #expect(!glyph(program, 33).isEmpty && !glyph(program, 34).isEmpty)
        // Two Type0 fonts and their CID font.
        #expect(try count(tagged, raw) == 3)
    }

    @Test func aPDFA1HasTheCIDSetOfTheGlyphsKept() throws {
        let raw = try document(Compliance.PDF_A_1B, plex, true, "A")
        let (groups, _) = try #require(try match("/CIDSet (\\d+) 0 R\n", raw), "no CIDSet")
        let at = (raw as NSString).range(of: "\n\(groups[1]) 0 obj\n").location
        #expect(at != NSNotFound)
        let (lengths, end) = try #require(try match("/Length (\\d+)\n>>\nstream\n", raw, from: at))
        let bits = try inflate(raw, end, Int(lengths[1])!)
        // Glyphs 0 and 33, A, of the 1,019 of IBM Plex Sans; Noto Sans had A
        // at 36 of 4,503.
        var want = [UInt8](repeating: 0, count: (1019 + 7) / 8)
        want[0] = 0x80
        want[33 / 8] |= UInt8(0x80 >> (33 % 8))
        #expect(bits == want)
    }

    // Adds the font to the objects of a PDF, draws a word with it on the page,
    // and returns the objects of the PDF written.
    private func subsetInExistingPDF(_ path: String, _ subset: Bool) throws -> [PDFobj] {
        var objects = try TestSupport.read(TestSupport.pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"]))
        let font = try Font(&objects, TestSupport.open(path))
        font.setSubset(subset)
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.addResource(font, &objects)
        page.drawString(font, nil, 12, "Hello", 72, 72)
        page.complete(&objects)
        try memory.pdf.addObjects(objects)
        try memory.pdf.complete()
        return try TestSupport.read(memory.bytes)
    }

    // The objects of a PDF of one empty page.
    private func onePageObjects() throws -> [PDFobj] {
        return try TestSupport.read(TestSupport.pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"]))
    }

    @Test func aFontWhoseObjectsAreNeverAddedIsFreed() throws {
        // The font held its objects, and its Type0 font the font, a cycle that
        // kept the font and its program when the objects were never added to a
        // PDF (the review of 9 October 2026).
        let path = plex
        weak var existing: Font?
        weak var new: Font?
        try {
            var objects = try onePageObjects()
            let font = try Font(&objects, TestSupport.open(path))
            existing = font
            let memory = MemoryPDF()
            let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
            page.addResource(font, &objects)
            page.drawString(font, nil, 12, "Hello", 72, 72)
            page.complete(&objects)
        }()
        try {
            let font = try Font(TestSupport.newPDF(), TestSupport.open(path))
            new = font
        }()
        #expect(existing == nil, "the font of the objects is not freed")
        #expect(new == nil, "the font of the PDF is not freed")
    }

    @Test func aFontAddedToTheObjectsIsWrittenThoughItsCallerDroppedIt() throws {
        // The objects hold the font until they are added, as before.
        var objects = try onePageObjects()
        let memory = MemoryPDF()
        weak var dropped: Font?
        try {
            let font = try Font(&objects, TestSupport.open(plex))
            dropped = font
            let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
            page.addResource(font, &objects)
            page.drawString(font, nil, 12, "Hello", 72, 72)
            page.complete(&objects)
        }()
        #expect(dropped != nil, "the font is freed before its objects are added")
        try memory.pdf.addObjects(objects)
        try memory.pdf.complete()
        let written = try TestSupport.read(memory.bytes)
        let file = try #require(TestSupport.findObject(written, "/Length1"))
        #expect(Int(file.getValue("/Length1")) ?? 0 > 0)
        #expect(try subsetFontName(written).hasSuffix("+IBMPlexSans"))
        objects = []
        #expect(dropped == nil, "the font is not freed once its objects are written")
    }

    // Returns the /BaseFont of the Type0 font.
    private func subsetFontName(_ objects: [PDFobj]) throws -> String {
        let type0 = try #require(objects.first { $0.getValue("/Subtype") == "/Type0" })
        return type0.getValue("/BaseFont")
    }

    @Test func subsetOfATrueTypeFontAddedToAnExistingPDF() throws {
        let path = plex
        let whole = try fontBytes(path).count
        let objects = try subsetInExistingPDF(path, true)
        let name = try subsetFontName(objects)
        #expect(name.range(of: "^/[A-Z]{6}\\+IBMPlexSans$", options: .regularExpression) != nil, "\(name)")
        #expect(TestSupport.findObject(objects, "/FontName")?.getValue("/FontName") == name)
        #expect(TestSupport.findObject(objects, "/CIDToGIDMap")?.getValue("/BaseFont") == name)
        let file = try #require(TestSupport.findObject(objects, "/Length1"))
        let length1 = Int(file.getValue("/Length1")) ?? 0
        #expect(length1 > 0 && length1 < whole, "/Length1 \(length1)")
        #expect(file.getData().count == length1)
        let cmap = try #require(objects.map { TestSupport.latin1($0.getData()) }.first { $0.hasPrefix("/CIDInit") })
        // The codespace range, .notdef and H, e, l, o
        #expect(cmap.components(separatedBy: "> <").count - 1 == 6)
    }

    @Test func subsetOfACFFFontAddedToAnExistingPDF() throws {
        let objects = try subsetInExistingPDF("tests/fonts/IBMPlexSans-Regular.otf", true)
        let name = try subsetFontName(objects)
        #expect(name.range(of: "^/[A-Z]{6}\\+IBMPlexSans$", options: .regularExpression) != nil, "\(name)")
        #expect(TestSupport.findObject(objects, "/FontFile3") != nil)
    }

    @Test func setSubsetFalseKeepsAFontAddedToAnExistingPDFWhole() throws {
        let path = plex
        let objects = try subsetInExistingPDF(path, false)
        #expect(try subsetFontName(objects) == "/IBMPlexSans")
        let length1 = Int(TestSupport.findObject(objects, "/Length1")?.getValue("/Length1") ?? "")
        #expect(try length1 == fontBytes(path).count)
    }

    private func put16(_ font: inout [UInt8], _ at: Int, _ value: Int) {
        font[at] = UInt8(truncatingIfNeeded: value >> 8)
        font[at + 1] = UInt8(truncatingIfNeeded: value)
    }

    private func put32(_ font: inout [UInt8], _ at: Int, _ value: Int) {
        put16(&font, at, value >> 16)
        put16(&font, at + 2, value)
    }

    // Checks that the font, made wrong, is not subset, and so embedded whole,
    // rather than a trap or a subset made of it.
    private func expectRefused(_ ttf: [UInt8], _ used: [Bool], sourceLocation: SourceLocation = #_sourceLocation) {
        #expect(throws: Subset.NotSubset.self, sourceLocation: sourceLocation) {
            _ = try Subset.subsetTrueType(ttf, used)
        }
    }

    @Test func aTableTwiceIsRefused() throws {
        // The gasp table's entry made a second head table of 8 bytes, whose
        // checksum adjustment the subset wrote past its end (the review of
        // 9 October 2026).
        var ttf = try fontBytes(plex)
        let gasp = entry(ttf, "gasp")
        try #require(gasp != -1)
        ttf.replaceSubrange(gasp..<(gasp + 4), with: Array("head".utf8))
        put32(&ttf, gasp + 12, 8)
        expectRefused(ttf, used(36))
    }

    @Test func aLocaFormatOtherThan0Or1IsRefused() throws {
        var ttf = try fontBytes("tests/fonts/IBMPlexMath-Regular.ttf")
        put16(&ttf, table(ttf, "head") + 50, 0xFFFF)
        expectRefused(ttf, used(36))
    }

    @Test func overlappingGlyphsAreRefused() throws {
        // Every even glyph's outline the whole glyf table, so that a subset of
        // them copied it again for each (the review of 9 October 2026: 112 KB
        // made into 384 MB). A subset is never larger than the glyf table and the
        // padding of its glyphs.
        var ttf = try fontBytes(plex)
        let head = table(ttf, "head")
        let loca = table(ttf, "loca")
        let glyfLength = u32(ttf, entry(ttf, "glyf") + 12)
        let numGlyphs = u16(ttf, table(ttf, "maxp") + 4)
        let long = u16(ttf, head + 50) == 1
        var used = [Bool](repeating: false, count: 0x10000)
        for gid in 0...numGlyphs {
            let offset = gid % 2 == 1 ? glyfLength & ~3 : 0
            if long {
                put32(&ttf, loca + 4 * gid, offset)
            } else {
                put16(&ttf, loca + 2 * gid, offset / 2)
            }
            if gid % 2 == 0 && gid < numGlyphs {
                used[gid] = true
            }
        }
        expectRefused(ttf, used)
    }
}
