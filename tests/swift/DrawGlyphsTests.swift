/**
 * DrawGlyphsTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// drawGlyphs draws glyphs another engine shaped, by their numbers, as real
/// text: a span of actual text for each cluster, each glyph at its place, and
/// the glyphs no character maps to mapped to their cluster's letters in
/// ToUnicode, or to the zero width space.
@Suite struct DrawGlyphsTests {
    // The first glyph of the font no character maps to, like a conjunct
    private func unmappedGlyph(_ font: Font) -> Int {
        for gid in 1..<font.unicodeOf.count where font.unicodeOf[gid] < 0 {
            return gid
        }
        Issue.record("the font maps every glyph")
        return 0
    }

    private func count(_ text: String, _ part: String) -> Int {
        text.components(separatedBy: part).count - 1
    }

    @Test func theGlyphsAreRealTextInATaggedParagraph() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        font.setSize(12)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let a = font.unicodeToGID[0x41]
        let x = unmappedGlyph(font)
        page.addBDC(StructElem.P, "en", nil, nil)
        page.drawGlyphs(font, [a, x], [50, 100, 60, 100], [0, 1], "AXY", "hi")
        page.addEMC()
        let content = TestSupport.content(page)
        // The language around them all, then a span of actual text for each
        // cluster, its glyphs in a TJ array
        #expect(content.contains("/Span <</Lang <" + memory.pdf.toHex("hi") + ">>> BDC\nBT\n"), "\(content)")
        #expect(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("A") + ">>> BDC\n"
                + "1 0 0 1 50 692 Tm\n[<" + String(format: "%04X", a) + ">] TJ\nEMC\n"), "\(content)")
        #expect(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("XY") + ">>> BDC\n"
                + "1 0 0 1 60 692 Tm\n[<" + String(format: "%04X", x) + ">] TJ\nEMC\nET\nEMC\n"), "\(content)")
        #expect(count(content, "BDC\n") == 4, "\(content)")   // The paragraph's, the language's, two spans
        // The unmapped glyph maps to its cluster's letters, "XY"; the mapped
        // one keeps its own
        let map = FontWriter.toUnicodeCMap(font, nil).lowercased()
        #expect(map.contains("<" + String(format: "%04x", x) + "> <00580059>"), "\(map)")
        #expect(!map.contains("<" + String(format: "%04x", a) + "> <0058"), "\(map)")
        #expect(memory.pdf.error == nil)
    }

    @Test func anUnmappedGlyphBesideALetterTheClusterLacksIsTheZeroWidthSpace() throws {
        // As Khmer's ោ, drawn as the glyph of េ and a second part: the letters
        // cannot add up to the cluster's, and MuPDF would copy it twice
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let x = unmappedGlyph(font)
        page.drawGlyphs(font, [font.unicodeToGID[0x42], x], [50, 100, 60, 100], [0, 0], "XY", "en")
        let map = FontWriter.toUnicodeCMap(font, nil).lowercased()
        #expect(map.contains("<" + String(format: "%04x", x) + "> <200b>"), "\(map)")
    }

    @Test func outsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.drawGlyphs(font, [font.unicodeToGID[0x41]], [50, 100], nil, "A", "en")
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/P <</MCID 0>>\nBDC\n/Span <<"), "\(content)")
        #expect(content.hasSuffix("ET\nEMC\nEMC\n"), "\(content)")
        #expect(page.structures.count == 1)
    }

    @Test func anUntaggedPageHasTheSpansOfActualTextAlone() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        page.drawGlyphs(font, [font.unicodeToGID[0x41]], [50, 100], nil, "A", nil)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("BT\n"), "\(content)")
        #expect(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("A") + ">>> BDC\n"), "\(content)")
        #expect(!content.contains("MCID"), "\(content)")
    }

    @Test func misuseIsReported() throws {
        func check(_ draw: (Page, Font, Font, Int) -> Void) throws {
            let memory = MemoryPDF()
            memory.pdf.coreFontsInCompliance = true
            let plex = try TestSupport.plexSans(memory.pdf)
            let core = TestSupport.helvetica(memory.pdf)
            let page = Page(memory.pdf, Letter.PORTRAIT)
            draw(page, plex, core, plex.unicodeToGID[0x41])
            #expect(memory.pdf.error != nil)
        }
        try check { page, _, core, _ in page.drawGlyphs(core, [1], [0, 0], nil, "A", nil) }
        try check { page, plex, _, a in page.drawGlyphs(plex, [a], [0], nil, "A", nil) }
        try check { page, plex, _, _ in page.drawGlyphs(plex, [plex.advanceWidth.count], [0, 0], nil, "A", nil) }
        try check { page, plex, _, a in page.drawGlyphs(plex, [a], [0, 0], [1], "A", nil) }
        try check { page, plex, _, a in page.drawGlyphs(plex, [a], [0, 0], [0, 0], "A", nil) }
    }
}
