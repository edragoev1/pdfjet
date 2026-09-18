/**
 * FontTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct FontTests {
    @Test func coreFontWidthsComeFromTheAfmMetrics() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        #expect(font.getName() == "Helvetica")
        #expect(font.getSize() == 12)
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        TestSupport.expectNear(27.336, font.stringWidth(12, "Hello"), 0.001)
        TestSupport.expectNear(27.336, font.stringWidth("Hello"), 0.001)
        _ = font.setSize(24)
        TestSupport.expectNear(54.672, font.stringWidth("Hello"), 0.001)
    }

    @Test func theWidthOfNoTextIsZero() {
        #expect(TestSupport.helvetica(TestSupport.newPDF()).stringWidth(nil) == 0)
    }

    @Test func kerningPairsNarrowTheText() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        TestSupport.expectNear(16.008, font.stringWidth(12, "AV"), 0.001)
        _ = font.setKernPairs(true)
        // KPX A V -70
        TestSupport.expectNear(15.168, font.stringWidth(12, "AV"), 0.001)
    }

    @Test func coreFontVerticalMetrics() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        TestSupport.expectNear(11.172, font.getAscent(12), 0.001)
        TestSupport.expectNear(2.7, font.getDescent(12), 0.001)
        TestSupport.expectNear(13.872, font.getBodyHeight(12), 0.001)
    }

    @Test func getFitCharsCountsTheCharactersThatFit() {
        #expect(TestSupport.helvetica(TestSupport.newPDF()).getFitChars("Hello world", 30) == 5)
    }

    @Test func everyCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() {
        let font = Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT)
        #expect(font.stringWidth(10, "日本") == 20)
        #expect(font.stringWidth(10, "\u{2000B}") == 10)
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func readsAStreamFont() throws {
        let stream = TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
        let font = try Font(TestSupport.newPDF(), stream)
        #expect(font.getName() == "IBMPlexSans")
        TestSupport.expectNear(28.32, font.stringWidth(12, "Hello"), 0.001)
    }

    // The embedded font file of a PDF that draws a line of text in the font.
    private func embeddedFontFile(_ fontStream: [UInt8]) throws -> [UInt8] {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, InputStream(data: Data(fontStream)))
        let text = TextLine(font, "Hello")
        text.setLocation(50, 50)
        text.drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        for obj in try TestSupport.read(memory.bytes) where obj.getValue("/Subtype") == "/CIDFontType0C" {
            return obj.getData()
        }
        return []
    }

    private func int32(_ buffer: [UInt8], _ i: Int) -> Int {
        return Int(buffer[i]) << 24 | Int(buffer[i + 1]) << 16 | Int(buffer[i + 2]) << 8 | Int(buffer[i + 3])
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func anOpenTypeStreamFontKeepsItsOtherTablesAndEmbedsOnlyItsCFFData() throws {
        let path = TestSupport.path("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")
        let whole = [UInt8](try Data(contentsOf: URL(fileURLWithPath: path)))
        // The name, the info and the metrics come first, then 'R' with the
        // length of the other tables of the font, and then the CFF data.
        var i = 1 + Int(whole[0])
        i += 3 + (Int(whole[i]) << 16 | Int(whole[i + 1]) << 8 | Int(whole[i + 2]))
        i += 4 + int32(whole, i)
        #expect(whole[i] == UInt8(ascii: "R"))
        let length = int32(whole, i + 1)
        // The same stream without the tables, as streams were written before.
        let cffOnly = Array(whole[0..<i]) + Array(whole[(i + 5 + length)...])
        #expect(cffOnly[i] == UInt8(ascii: "Y"))

        let embedded = try embeddedFontFile(whole)
        #expect(!embedded.isEmpty)
        #expect(embedded == (try embeddedFontFile(cffOnly)))
    }

    // The content of a page with a line of Thai in the font: po pla, the upper
    // vowel sara ii on it and the tone mark mai ek above the vowel.
    private func thaiContent(_ path: String) throws -> String {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open(path))
        let page = Page(pdf, Letter.PORTRAIT)
        let text = TextLine(font, "\u{0E1B}\u{0E35}\u{0E48}")
        text.setLocation(50, 50)
        text.drawOn(page)
        return TestSupport.content(page)
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"),
            "the fonts directory is not here"))
    func aStreamFontPlacesTheMarksAsTheOpenTypeFontDoes() throws {
        #expect(try thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf") == (try thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf.stream")))
    }

    @Test func aCoreFontNumberOutsideTheFourteenIsRejected() {
        let pdf = TestSupport.newPDF()
        #expect(throws: PDFjetError.self) { try Font(pdf, 0) }
        #expect(throws: PDFjetError.self) { try Font(pdf, 15) }
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"),
            "the fonts directory is not here"))
    func theLineGapOfAFontSpacesTheLinesOfATextBlock() throws {
        let pdf = TestSupport.newPDF()
        let jp = try Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"))
        TestSupport.expectNear(10, jp.getLineGap(10), 0.001)
        TestSupport.expectNear(10, try Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")).getLineGap(10), 0.001)
        #expect(try Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream")).getLineGap(10) == 0)
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        jp.setSize(10)
        TestSupport.expectXY(500, 40, TextBlock(jp, "日本\n日本").setLocation(0, 0).drawOn(nil))
    }
}
