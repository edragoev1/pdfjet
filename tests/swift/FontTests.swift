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

    @Test func aCoreFontDrawsTheWinAnsiCharactersFrom128To159() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        // ’ is 146 in WinAnsi and 222 units wide, where a space is 278.
        TestSupport.expectNear(2.22, font.stringWidth(10, "\u{2019}"), 0.001)
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "Don\u{2019}t \u{20AC}5 \u{2014} \u{201C}Hi\u{201D}").setLocation(10, 20).drawOn(page)
        #expect(TestSupport.content(page).lowercased().contains("<446f6e927420803520972093486994>"))
    }

    @Test func aCoreFontDrawsDeleteAsASpace() {
        // WinAnsi draws a bullet at 127, where the widths of the core fonts
        // have a space: U+007F is a control, drawn as a space as the C1
        // controls are.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        #expect(font.stringWidth(10, " ") == font.stringWidth(10, "\u{7F}"))
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "a\u{7F}b\u{85}c").setLocation(10, 20).drawOn(page)
        #expect(TestSupport.content(page).contains("<6120622063>"))
    }

    // IBM Plex Sans, read from its stream file. Its space is 236 units wide
    // and its .notdef 472, of 1000 units to the em; it has the characters
    // from U+0020 to U+FFFD, and no Thai.
    private func ibmPlexSans(_ pdf: PDF) throws -> Font {
        return try Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func aCharacterTheFontDoesNotHaveIsDrawnWithNotdef() throws {
        let pdf = TestSupport.newPDF()
        let font = try ibmPlexSans(pdf)
        // ก, U+0E01, is in the range of the font and not in the font, and 😀,
        // U+1F600, is past its range: both are drawn with .notdef, as wide as
        // it is. A control character is drawn as a space.
        TestSupport.expectNear(4.72, font.stringWidth(10, "\u{0E01}"), 0.001, "a character in the range")
        TestSupport.expectNear(4.72, font.stringWidth(10, "\u{1F600}"), 0.001, "a character past the range")
        for c in ["\t", "\u{7F}", "\u{85}"] {
            TestSupport.expectNear(2.36, font.stringWidth(10, c), 0.001, "\(c.unicodeScalars.first!.value)")
        }
        // The width of the text that fits is that of the glyphs drawn.
        #expect(font.setSize(10).getFitChars("\u{0E01}\u{0E01}", 9.5) == 2)
        // The glyph of a missing character is .notdef, in a span whose actual
        // text is the character, so that a copy of the text has it and not
        // the U+FFFD the ToUnicode map gives .notdef. A control is the space
        // glyph.
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "\u{0E01}\t\u{1F600}").setLocation(10, 20).drawOn(page)
        let content = TestSupport.content(page)
        let space = String(format: "%04X", font.unicodeToGID[0x20])
        #expect(content.contains("/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<\(space)>"))
        #expect(content.contains("/Span <</ActualText <FEFFD83DDE00>>> BDC\n<0000> Tj\nEMC\n"))
        // The text a glyph maps to is the character, and a space for a control.
        #expect(Page.textOf(font, 0x0E01) == "\u{0E01}")
        #expect(Page.textOf(font, 0x0085) == " ")
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func aStampDrawsACharacterTheFontDoesNotHaveWithNotdef() throws {
        let memory = MemoryPDF()
        let font = try ibmPlexSans(memory.pdf)
        _ = Page(memory.pdf, Letter.PORTRAIT)
        let stamp = Stamp(memory.pdf).setSize(100, 50)
        stamp.drawText(font, 10, 5, 20, "a\u{0E01}\t")
        try stamp.complete()
        try memory.pdf.complete()
        let want = String(format: "<%04X> Tj\n/Span <</ActualText <FEFF0E01>>> BDC\n<0000> Tj\nEMC\n<%04X> Tj\n",
                font.unicodeToGID[0x61], font.unicodeToGID[0x20])
        #expect(TestSupport.latin1(memory.bytes).contains(want))
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func aCompliantDocumentDrawsACharacterTheFontDoesNotHaveWithoutNotdef() throws {
        // PDF/UA and PDF/A forbid .notdef, so a PDF/UA document draws the
        // replacement character of the font, as wide as it is, with the missing
        // character as its actual text, and never glyph 0.
        let memory = MemoryPDF()
        memory.pdf.setCompliance(Compliance.PDF_UA_1)
        let font = try ibmPlexSans(memory.pdf)
        let replacement = font.unicodeToGID[0xFFFD]
        try #require(replacement != 0, "IBM Plex Sans has no U+FFFD")
        let width = Float(font.glyphAdvance(replacement)) * 10 / Float(font.unitsPerEm)
        TestSupport.expectNear(width, font.stringWidth(10, "\u{0E01}"), 0.001, "a character in the range")
        TestSupport.expectNear(width, font.stringWidth(10, "\u{1F600}"), 0.001, "a character past the range")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextLine(font, "\u{0E01}\u{1F600}").setLocation(10, 20).drawOn(page)
        let content = TestSupport.content(page)
        let glyph = String(format: "<%04X> Tj\nEMC\n", replacement)
        for want in [
                "/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph,
                "/Span <</ActualText <FEFFD83DDE00>>> BDC\n" + glyph] {
            #expect(content.contains(want), "\(want) is not in \(content)")
        }
        #expect(!content.contains("<0000>"), ".notdef is drawn: \(content)")
        // The stamp draws it so too.
        let stamp = Stamp(memory.pdf).setSize(100, 50)
        stamp.drawText(font, 10, 5, 20, "\u{0E01}")
        try stamp.complete()
        try memory.pdf.complete()
        let want = "/Span <</ActualText <FEFF0E01>>> BDC\n" + glyph
        #expect(TestSupport.latin1(memory.bytes).contains(want), "\(want) is not in the stamp")
        // The character still has no glyph, so a fallback font draws it.
        #expect(!font.hasGlyph(0x0E01), "a missing character has a glyph")
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"),
            "the fonts directory is not here"))
    func aControlCharacterStaysInTheFontOfItsText() throws {
        // A control character is drawn as a space by the font, not by the
        // fallback font: the fallback font would draw it as a space too.
        let pdf = TestSupport.newPDF()
        let latin = try ibmPlexSans(pdf)
        let helvetica = TestSupport.helvetica(pdf)
        for font in [latin, helvetica] {
            for c in [0x09, 0x7F, 0x85] {
                #expect(font.hasGlyph(c), "\(font.name) has no glyph for \(c)")
            }
        }
        #expect(!latin.hasGlyph(0x0E01) && !latin.hasGlyph(0x1F600), "a character drawn with .notdef has a glyph")
    }

    @Test func aSoftHyphenIsAHyphenAndANoBreakSpaceIsASpace() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        font.setKernPairs(true)
        #expect(font.stringWidth(10, "T-") == font.stringWidth(10, "T\u{00AD}"))
        #expect(font.stringWidth(10, ". ") == font.stringWidth(10, ".\u{00A0}"))
        // KPX period space -60
        TestSupport.expectNear(4.96, font.stringWidth(10, ".\u{00A0}"), 0.001)
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"),
            "the fonts directory is not here"))
    func aFallbackFontDrawsOnlyTheCharactersTheFontHasNoGlyphFor() throws {
        let pdf = TestSupport.newPDF()
        let latin = try Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
        let jp = try Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"))
        let helvetica = TestSupport.helvetica(pdf)
        // The Latin letters after the Japanese ones are in the font again.
        TestSupport.expectNear(latin.stringWidth(10, "abc") + jp.stringWidth(10, "\u{65E5}\u{672C}") + latin.stringWidth(10, "def"),
                latin.stringWidth(jp, 10, "abc\u{65E5}\u{672C}def"), 0.001)
        // A core font has a fallback font too.
        TestSupport.expectNear(helvetica.stringWidth(10, "Tokyo ") + jp.stringWidth(10, "\u{6771}\u{4EAC}"),
                helvetica.stringWidth(jp, 10, "Tokyo \u{6771}\u{4EAC}"), 0.001)
        // A character that neither font has stays in the font.
        #expect(latin.stringWidth(10, "x\u{0E01}y") == latin.stringWidth(jp, 10, "x\u{0E01}y"))
        // A combining mark stays with the character before it, in one run of one font.
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(latin, "\u{65E5}\u{0301}").setFallbackFont(jp).setLocation(10, 20).drawOn(page)
        #expect(TestSupport.content(page).components(separatedBy: " Tf\n").count - 1 == 1)
    }
    // The number of font programs the PDF embeds.
    private func embeddedFonts(_ pdf: [UInt8]) -> Int {
        return String(decoding: pdf, as: UTF8.self).components(separatedBy: "/FontFile").count - 1
    }

    // Draws a character with each font of a PDF made from the font files.
    private func documentWithFonts(_ paths: [String]) throws -> [UInt8] {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        var y: Float = 50
        for path in paths {
            let font = try Font(memory.pdf, TestSupport.open(path)).setSize(12)
            TextLine(font, "\u{4E2D}").setLocation(50, y).drawOn(page)
            y += 20
        }
        try memory.pdf.complete()
        return memory.bytes
    }

    @Test(.enabled(if: TestSupport.exists("fonts/NotoSansSC/NotoSansSC-Regular.ttf"),
            "the fonts directory is not here"))
    func aFontAndASubsetOfItWithTheSameNameAreBothEmbedded() throws {
        // PDFjet ships subsets of the Noto CJK fonts whose name inside is the
        // name of the whole font. A PDF embedded the font file of the first
        // of two fonts of one name for both, so the text drawn with the
        // second came out in the glyphs of the first.
        #expect(try embeddedFonts(documentWithFonts([
                "fonts/NotoSansSC/NotoSansSC-Regular.ttf",
                "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf"])) == 2)
        #expect(try embeddedFonts(documentWithFonts([
                "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream",
                "fonts/NotoSansSC/NotoSansSC-Regular-SC3500.ttf.stream"])) == 2)
    }

    @Test(.enabled(if: TestSupport.exists("fonts/IBMPlexSans/IBMPlexSans-Regular.otf"),
            "the fonts directory is not here"))
    func oneFontProgramIsEmbeddedOnce() throws {
        // The same font added twice, and the same font read from a .otf and
        // from the .stream file made of it, are one font program: Example_28
        // draws with both and embeds one of each font.
        #expect(try embeddedFonts(documentWithFonts([
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf"])) == 1)
        #expect(try embeddedFonts(documentWithFonts([
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
                "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"])) == 1)
    }
}
