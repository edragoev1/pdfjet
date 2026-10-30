/**
 * AstralTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The characters past U+FFFF, as the Chinese characters of Hong Kong, 𨋢 the
/// lift among them, which IBM Plex Sans TC maps in its format 12 subtable.
@Suite struct AstralTests {
    @Test func theGlyphMapGoesPastTheBasicMultilingualPlane() {
        var m = GlyphMap()
        m.set(0x41, 3)
        m.set(0x282E2, 7)
        m.setIfAbsent(0x282E2, 9)   // Kept
        m.setIfAbsent(0x20021, 5)
        m.set(0x110000, 4)          // Not a character
        #expect(m[0x41] == 3)
        #expect(m[0x282E2] == 7)
        #expect(m[0x20021] == 5)
        #expect(m[0x110000] == 0)
        #expect(m[0x20000] == 0)
        var order = [Int]()
        m.forEach { c, _ in order.append(c) }
        #expect(order == [0x41, 0x20021, 0x282E2])
        // The glyphs drawn for several characters, those past U+FFFF too
        m.set(0x20022, 7)
        #expect(m.sharedGlyphs()[7])
        #expect(!m.sharedGlyphs()[5])
        // A font of a million characters past U+FFFF is read no further than
        // maxAstral
        var big = GlyphMap()
        for c in 0x10000..<(0x10000 + 2 * GlyphMap.maxAstral) {
            big.setIfAbsent(c, 1)
        }
        #expect(big.astral.count == GlyphMap.maxAstral)
    }

    @Test func aCharacterPastU0xFFFFIsDrawnInTheFallbackFont() throws {
        let pdf = TestSupport.newPDF()
        let plex = try Font(pdf, TestSupport.path("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        // IBM Plex Sans TC, the .otf PDFjet carries, maps 𨋢 in its format 12
        // subtable, as its .ttf, which this test read before 9.1.7, does.
        let tc = try Font(pdf, TestSupport.path("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"))
        #expect(tc.hasGlyph(0x282E2), "TC has 𨋢")
        #expect(!plex.hasGlyph(0x282E2), "Plex Sans has 𨋢")
        let gid = tc.unicodeToGID[0x282E2]
        #expect(gid != 0 && gid != tc.unicodeToGID[0x4E58], "𨋢 is glyph \(gid)")
        // Each a Chinese character of TC, one em wide, none the box of Plex Sans
        #expect(plex.stringWidth(tc, 1000, "乘𨋢到") == 3000)
        // Copied out of the PDF as 𨋢: in UTF-16, the two halves of a surrogate
        // pair
        #expect(FontWriter.toUnicodeCMap(tc, nil).contains("<d860dee2>"))
        #expect(FontWriter.unicodeHex(0x282E2) == "d860dee2")
        #expect(FontWriter.unicodeHex(0x41) == "0041")
    }
}
