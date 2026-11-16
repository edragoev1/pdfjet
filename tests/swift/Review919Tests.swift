/**
 * Review919Tests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The faults found by the review of 9.1.9 (16 November 2026), each kept from
/// coming back.
@Suite struct Review919Tests {
    private static let cascadia = "tests/fonts/CascadiaMono-Regular.ttf"

    private func put32(_ data: inout [UInt8], _ at: Int, _ value: Int) {
        data[at] = UInt8(truncatingIfNeeded: value >> 24)
        data[at + 1] = UInt8(truncatingIfNeeded: value >> 16)
        data[at + 2] = UInt8(truncatingIfNeeded: value >> 8)
        data[at + 3] = UInt8(truncatingIfNeeded: value)
    }

    // The outline of Cascadia Mono with the glyphs given made the bytes
    // given, written after its glyf table.
    private func outlineWithGlyphs(_ glyphs: [Int: [UInt8]]) throws -> GlyphOutline {
        let cascadia = try Font(TestSupport.newPDF(), TestSupport.open(Review919Tests.cascadia))
        let o = try GlyphOutline(cascadia.program!.font, false)
        try #require(o.longOffsets, "short loca offsets")
        var data = o.data
        for (gid, g) in glyphs {
            while (data.count - o.glyfOffset) % 4 != 0 {
                data.append(0)
            }
            let start = data.count - o.glyfOffset
            data += g
            // The glyph after is not one of those given, so its start is kept
            put32(&data, o.locaOffset + 4 * gid, start)
            put32(&data, o.locaOffset + 4 * gid + 4, start + g.count)
        }
        o.data = data
        o.glyfLength = data.count - o.glyfOffset
        return o
    }

    // A composite glyph of the parts, each placed at 0 0.
    private func compositeGlyph(_ parts: [Int]) -> [UInt8] {
        var glyph: [UInt8] = [0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0]  // -1 contours, the box
        for (i, part) in parts.enumerated() {
            var flags = 0x0002  // ARGS_ARE_XY_VALUES, the arguments bytes
            if i < parts.count - 1 {
                flags |= 0x0020 // MORE_COMPONENTS
            }
            glyph += [UInt8(flags >> 8), UInt8(flags & 0xFF), UInt8(part >> 8), UInt8(part & 0xFF), 0, 0]
        }
        return glyph
    }

    private func contours(_ operators: String) -> Int {
        return operators.components(separatedBy: " m\n").count - 1
    }

    // A composite glyph of four parts, each of four parts, 16 deep, down to an
    // O, was 4^16 Os read; the work of a glyph is bounded.
    @Test func aTrueTypeCompositeGlyphOfManyPartsIsRefusedQuickly() throws {
        let cascadia = try Font(TestSupport.newPDF(), TestSupport.open(Review919Tests.cascadia))
        let o = cascadia.unicodeToGID[0x4F]
        // Every other glyph of the last 40, so that the glyph after each keeps
        // its start, and none is the O
        var glyphs = [Int: [UInt8]]()
        var below = o
        let first = cascadia.advanceWidth.count - 40
        var gid = first
        for _ in 0..<16 {
            glyphs[gid] = compositeGlyph([below, below, below, below])
            below = gid
            gid += 2
        }
        let outline = try outlineWithGlyphs(glyphs)
        // Two levels are drawn, as a font may have them
        let path = try outline.outline(first + 2)
        #expect(contours(path.operators) == 32)
        // Within 10 seconds, the shortest of up to three runs: about one in a
        // debug build alone, more with the other tests; 4^16 Os took minutes
        let elapsed = shortestTime(.seconds(10)) {
            #expect(throws: PDFjetError.self) {
                _ = try outline.outline(below)
            }
        }
        #expect(elapsed < .seconds(10), "\(elapsed)")
    }

    // The outline of IBM Plex Sans with the charstring of glyph 1 made the
    // bytes given.
    private func cffOutlineWithCharString(_ cs: [UInt8]) throws -> GlyphOutline {
        let plex = try TestSupport.plexSans(TestSupport.newPDF())
        let o = try GlyphOutline(plex.program!.font, true)
        var objects = o.charStrings.objects
        objects[1] = o.data.count
        objects[2] = o.data.count + cs.count
        o.data += cs
        o.charStrings = CFFSubset.Index(start: o.charStrings.start, end: o.charStrings.end, objects: objects)
        return o
    }

    // A charstring's arithmetic made a point of 1.15e18 units, written as a
    // number of no meaning, or one Swift trapped on; a point past the bound is
    // a damaged glyph.
    @Test func aCFFGlyphOfAPointFarOutIsRefused() throws {
        // 32767 dup mul dup mul 0 rmoveto 0 1 rlineto endchar
        let cs: [UInt8] = [0x1C, 0x7F, 0xFF, 0x0C, 0x1B, 0x0C, 0x18, 0x0C, 0x1B, 0x0C, 0x18,
                0x8B, 0x15, 0x8B, 0x8C, 0x05, 0x0E]
        let outline = try cffOutlineWithCharString(cs)
        #expect(throws: PDFjetError.self) {
            _ = try outline.outline(1)
        }
    }

    // 2^31 as 32767 1 add 65536 mul, with 65536 as 32767 2 mul 2 add
    private static let big: [UInt8] = [0x1C, 0x7F, 0xFF, 0x8C, 0x0C, 0x0A, 0x1C, 0x7F, 0xFF, 0x8D, 0x0C, 0x18,
            0x8D, 0x0C, 0x0A, 0x0C, 0x18]

    // roll and index take their operands as numbers before integers, so that a
    // count of 2^31 is refused in the four ports alike.
    @Test func aCFFRollOrIndexOfAnOperandTooLargeIsRefused() throws {
        let ops: [(String, [UInt8])] = [
            ("roll", [0x8C, 0x8D, 0x8E, 0x8E] + Review919Tests.big + [0x0C, 0x1E]),
            ("index", [0x8C] + Review919Tests.big + [0x0C, 0x1D]),
        ]
        for (name, op) in ops {
            let cs = op + [0x8B, 0x8B, 0x15, 0x0E]  // 0 0 rmoveto endchar
            let outline = try cffOutlineWithCharString(cs)
            #expect(throws: PDFjetError.self, "\(name)") {
                _ = try outline.outline(1)
            }
        }
    }

    // index of a number below 0, however far, is index 0, as in the other
    // ports: Swift refused one past -1e9.
    @Test func aCFFIndexOfALargeNegativeOperandIsTheTop() throws {
        // 1 -2^31 index rmoveto endchar: 1 1 rmoveto
        let cs: [UInt8] = [0x8C] + Review919Tests.big + [0x0C, 0x0E, 0x0C, 0x1D, 0x15, 0x0E]
        let path = try cffOutlineWithCharString(cs).outline(1)
        #expect(path.operators.isEmpty)
    }

    // A position of a glyph not finite, or far off the page, is an error, not a
    // number of no meaning in the content.
    @Test func drawGlyphsAPositionNotFiniteIsAnError() throws {
        for x: Float in [Float.infinity, Float.nan, 2e6] {
            let memory = MemoryPDF()
            let font = try TestSupport.plexSans(memory.pdf)
            let page = Page(memory.pdf, Letter.PORTRAIT)
            page.drawGlyphs(font, [36], [x, 100], "A", nil)
            #expect(memory.pdf.error?.contains("positions between") == true, "\(x): \(memory.pdf.error ?? "")")
        }
    }

    // The matrix of a glyph at a font size too large for an Int64 in
    // hundred-millionths is written, not trapped on.
    @Test func glyphNumbersTooLargeDoNotTrap() {
        #expect(GlyphOutline.decimals(1e12, 7) == "922337203685.4775807")
        #expect(GlyphOutline.decimals(-Double.infinity, 7) == "-922337203685.4775807")
        #expect(GlyphOutline.decimals(Double.nan, 2) == "0")
        #expect(Page.glyphMatrix([0.001, 0, 0, 0.001, 0, 0], 1e30, 0, 0).hasPrefix("922337203685.4775807 0 0 "))
    }

    // The header of a BigTable whose header and rows have one font took the
    // rows' fallback font: the header has its own, and the rows theirs.
    @Test func bigTableOfOneFontGivesTheHeaderItsOwnFallbackFont() throws {
        let pdf = TestSupport.newPDF()
        let plex = try TestSupport.plexSans(pdf)
        let kr = try Font(pdf, IBMPlexSansKR.Regular)
        var bt = BigTable(pdf, plex, plex, Letter.PORTRAIT).setFallbackFonts(kr, nil)
        #expect(bt.measureOf(true).fallback === kr && bt.measureOf(false).fallback == nil,
                "the header's fallback font is not its own")
        bt = BigTable(pdf, plex, plex, Letter.PORTRAIT).setFallbackFonts(nil, kr)
        #expect(bt.measureOf(true).fallback == nil && bt.measureOf(false).fallback === kr,
                "the rows' fallback font is not theirs")
    }

    // Two lines that meet end to end, or edge to edge, may paint one pixel,
    // the hairline of width 0 most of all: they are stroked in the order of
    // the cells, or the color on top of that pixel changed when the borders
    // were joined.
    @Test func tableLinesThatMeetOverlap() {
        let black = TableLines.Segment(true, 50, 332.94, 350.82, 0, 0, 0, 0, 0)
        let blue = TableLines.Segment(true, 50, 350.82, 368.69, 0, 0, 1, 0, 0)
        #expect(black.overlaps(blue) && blue.overlaps(black), "hairlines that meet end to end do not overlap")
        let red = TableLines.Segment(false, 100, 50, 148, 1, 0, 0, 3, 0)
        let hairline = TableLines.Segment(true, 148, 100, 120, 0, 0, 0, 0, 0)
        #expect(red.overlaps(hairline), "a hairline at the end of a wide line does not overlap it")
        let far = TableLines.Segment(true, 160, 100, 120, 0, 0, 0, 0, 0)
        #expect(!red.overlaps(far), "lines 10 points apart overlap")
    }

    // A heading, a rule and a table: the rule is not a paragraph to measure,
    // and Java and C# threw on its text, which is none.
    @Test func markdownHeadingRuleAndTableAreDrawn() throws {
        let md = "# " + String(repeating: "word ", count: 1500) + "\n\n---\n\n| a | b |\n|---|---|\n| 1 | 2 |\n"
        let drawn = try MarkdownTests.draw(md, nil)
        #expect(drawn.pages.count >= 2 && drawn.text.contains("a"), "\(drawn.pages.count) pages")
    }

    // Drawing on a page of an existing PDF that has no content: the content
    // restored a graphics state it never saved, "Q" first.
    @Test func drawingOnAnEmptyPageOfAnExistingPDFIsBalanced() throws {
        var objects = try TestSupport.read(TestSupport.pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>"]))
        let memory = MemoryPDF()
        let page = Page(memory.pdf, memory.pdf.getPageObjects(from: objects)[0])
        page.drawRect(10, 10, 20, 20)
        page.complete(&objects)
        for obj in objects {
            if let stream = obj.stream {
                let content = TestSupport.latin1(stream)
                if content.contains(" l\n") {
                    #expect(content.hasPrefix("q\nQ\n"), "the content starts \(content.prefix(20))")
                    return
                }
            }
        }
        Issue.record("no content drawn")
    }

    // Two links to URIs that differ in their bytes, but not as Swift's
    // strings, é and e with its accent: two actions, as in the other ports.
    @Test func linksToCanonicallyEquivalentURIsAreTwo() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        _ = TextLine(font, "One").setURIAction("https://pdfjet.com/caf\u{E9}").setLocation(50, 100).drawOn(page)
        _ = TextLine(font, "Two").setURIAction("https://pdfjet.com/cafe\u{301}").setLocation(50, 120).drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/S /URI\n").count - 1 == 2)
    }
}
