/**
 * OTFTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

// The OpenType and TrueType fonts that are not valid, as the Go fuzz targets
// of the font loaders found them: each throws with a message, and none reads
// past the font, allocates what the font does not have or traps.
@Suite struct OTFTests {
    private let thai = "fonts/NotoSansThai/NotoSansThai-Regular.ttf"
    private let plex = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf"

    private func font(_ path: String) -> [UInt8] {
        return [UInt8](FileManager.default.contents(atPath: TestSupport.path(path))!)
    }

    // The message that loading the font throws with.
    private func error(_ font: [UInt8]) -> String {
        do {
            _ = try Font(TestSupport.newPDF(), InputStream(data: Data(font)))
        } catch {
            return "\(error)"
        }
        return "(no error)"
    }

    // Loads the font and draws with it.
    private func draws(_ font: [UInt8]) throws {
        let pdf = TestSupport.newPDF()
        let f = try Font(pdf, InputStream(data: Data(font)))
        TextLine(f, "Ab1 กิ่ x").setLocation(50.0, 50.0).drawOn(Page(pdf, Letter.PORTRAIT))
        try pdf.complete()
    }

    private func uint16(_ font: [UInt8], _ offset: Int) -> Int {
        return Int(font[offset]) << 8 | Int(font[offset + 1])
    }

    private func uint32(_ font: [UInt8], _ offset: Int) -> Int {
        return uint16(font, offset) << 16 | uint16(font, offset + 2)
    }

    // Where the directory entry of the table of the name begins: its four
    // letters, then its checksum, offset and length.
    private func entry(_ font: [UInt8], _ name: String) -> Int {
        for i in 0..<uint16(font, 4) {
            let entry = 12 + 16*i
            if String(decoding: font[entry..<(entry + 4)], as: UTF8.self) == name {
                return entry
            }
        }
        return -1
    }

    // Where the table of the name begins in the font.
    private func table(_ font: [UInt8], _ name: String) -> Int {
        return uint32(font, entry(font, name) + 8)
    }

    // Where the format 4 subtable of the character map begins: the one of the
    // Windows platform, which PDFjet reads.
    private func cmap4(_ font: [UInt8]) -> Int {
        let cmap = table(font, "cmap")
        for i in 0..<uint16(font, cmap + 2) {
            let record = cmap + 4 + 8*i
            if uint16(font, record) == 3 && uint16(font, record + 2) == 1 {
                return cmap + uint32(font, record + 4)
            }
        }
        return -1
    }

    // The font with the two bytes at the offset changed.
    private func with(_ font: [UInt8], _ offset: Int, _ value: Int) -> [UInt8] {
        var patched = font
        patched[offset] = UInt8((value >> 8) & 0xFF)
        patched[offset + 1] = UInt8(value & 0xFF)
        return patched
    }

    @Test func aFontThatEndsWhereATableIsReadIsRefused() throws {
        let data = font(thai)
        // The directory, and then the tables it points at, are read from the
        // front of the font, so every one of these ends in the middle of a read.
        for length in [4, 5, 12, 13, 100, 1000] {
            #expect(error(Array(data[0..<length])) == "Invalid font file: the font ends too soon.")
        }
        try draws(data)
    }

    @Test func aFontWithoutWhatItIsDrawnWithIsRefused() throws {
        let ttf = font(thai)
        let otf = font(plex)
        let head = table(ttf, "head")
        let hhea = table(ttf, "hhea")

        // The name of the character map table, changed to one PDFjet does not
        // read, so that the font has none.
        var noCmap = ttf
        noCmap[entry(ttf, "cmap")] = UInt8(ascii: "x")
        #expect(error(noCmap) == "Invalid font file: no character map.")

        #expect(error(with(ttf, head + 18, 0)) == "Invalid font file: the units per em.")
        #expect(error(with(ttf, head + 18, 15)) == "Invalid font file: the units per em.")
        #expect(error(with(ttf, head + 18, 16385)) == "Invalid font file: the units per em.")
        #expect(error(with(ttf, hhea + 34, 0)) == "Invalid font file: no advance widths.")
        #expect(error(with(ttf, cmap4(ttf), 6)) ==
                "Invalid font file: the character map is not format 4.")
        // The length of the CFF table, past the end of the font.
        #expect(error(with(otf, entry(otf, "CFF ") + 12, 0x7FFF)) ==
                "Invalid font file: the CFF table is not in the font.")
        try draws(otf)
    }

    // The font with the table of the name spelled differently, so that
    // PDFjet does not read it and the font has none.
    private func without(_ font: [UInt8], _ name: String) -> [UInt8] {
        var patched = font
        patched[entry(font, name)] = UInt8(ascii: "z")
        return patched
    }

    @Test func aFontWithNoNameOfItsOwnIsRefused() throws {
        // The name goes into the PDF as the name of the font, where a name
        // that is not a PDF name would break the syntax, so a font with none
        // is refused as a stream font with none is.
        #expect(error(without(font(thai), "name")) == "Invalid font file: the font name.")
    }

    @Test func aFontWithoutTheTablesItNeedsNoneOfStillDraws() throws {
        // Without OS/2 the font says it holds no characters and maps none of
        // them; without post it has no underline; without GPOS its marks are
        // not placed. None of the three stops it from drawing.
        let data = font(thai)
        for name in ["OS/2", "post", "GPOS"] {
            try draws(without(data, name))
        }
    }

    @Test func aNameRecordOutsideTheFontIsLeftOut() throws {
        // The offset of the first name record, past the end of the font. The
        // font keeps its other records and still draws.
        let data = font(thai)
        try draws(with(data, table(data, "name") + 16, 0xFFFF))
    }

    @Test func aSegmentOutsideTheGlyphIDArrayGivesNoGlyph() throws {
        // The length of the format 4 subtable, cut to its header and the four
        // arrays of its segments, so that its glyph ID array holds nothing and
        // every segment that is read through it points outside.
        let data = font(thai)
        let subtable = cmap4(data)
        try draws(with(data, subtable + 2, 16 + 4*uint16(data, subtable + 6)))
    }

    @Test func aGposTableIsNotReadPastTheWorkAFontNeeds() throws {
        // The lookups of the GPOS table, and the subtables of its first
        // lookup, changed to the most a font can say it has. Read to the end
        // it says, the font takes every byte of memory there is and never
        // loads; read to the work a font needs, it loads at once.
        let data = font(thai)
        let gpos = table(data, "GPOS")
        let lookupList = gpos + uint16(data, gpos + 8)
        let lookup = lookupList + uint16(data, lookupList + 2)
        try draws(with(with(data, lookupList, 0xFFFF), lookup + 4, 0xFFFF))
    }
}
