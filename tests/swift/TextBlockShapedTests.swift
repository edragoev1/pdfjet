/**
 * TextBlockShapedTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// TextBlock and TextLine with text such as Hindi or Khmer: the platform's
/// text engine, or a shaper given, wraps and shapes it, and the glyphs it
/// found are drawn as outlines, each line tagged with its text; the other
/// paragraphs are drawn as text. Serialized: two tests set or need the
/// default shaper, which is the program's.
@Suite(.serialized) struct TextBlockShapedTests {
    private static let devanagari = "resources/fonts/world/NotoSansDevanagari/NotoSansDevanagari-Regular.otf"
    private static let arabic = "resources/fonts/world/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf"
    private static let latin = "resources/fonts/IBMPlexSans/IBMPlexSans-Regular.otf"
    private static let cascadia = "tests/fonts/CascadiaMono-Regular.ttf"
    private static let hindi = "सभी मनुष्यों को गौरव और अधिकारों के मामले में जन्मजात स्वतन्त्रता "
            + "और समानता प्राप्त है। John Smith, 1948."

    private func count(_ text: String, _ part: String) -> Int {
        text.components(separatedBy: part).count - 1
    }

    // A shaper that gives the lines it was made with, and keeps what it was given
    private final class FixedShaper: Shaper {
        let lines: [ShapedLine]
        var files = [[UInt8]]()
        var size: Float = 0
        var width: Float = 0
        var language: String?
        var calls = 0

        init(_ lines: [ShapedLine]) {
            self.lines = lines
        }

        func shape(_ request: ShapingRequest) throws -> [ShapedLine] {
            self.files = request.fontFiles
            self.size = request.fontSize
            self.width = request.width
            self.language = request.language
            calls += 1
            return lines
        }
    }

    // A shaper that gives each paragraph as many lines as its letters, one
    // glyph of the letter each
    private final class LetterShaper: Shaper {
        let font: Font

        init(_ font: Font) {
            self.font = font
        }

        func shape(_ request: ShapingRequest) throws -> [ShapedLine] {
            var lines = [ShapedLine]()
            for (i, unit) in request.text.utf16.enumerated() {
                lines.append(ShapedLine(i, i + 1, [0], [font.unicodeToGID[Int(unit)]], [0, 0], 7))
            }
            return lines
        }
    }

    // The glyphs placed in the content: the matrix's scale, x and y of each
    private func places(_ content: String) -> [[Float]] {
        var places = [[Float]]()
        for line in content.components(separatedBy: "\n") where line.hasPrefix("q ") && line.contains(" cm /Fm") {
            let parts = line.components(separatedBy: " ")
            places.append([Float(parts[1])!, Float(parts[5])!, Float(parts[6])!])
        }
        return places
    }

    private func body(_ font: Font, _ size: Float) -> Float {
        font.getAscent(size) + font.getDescent(size) + font.getLineGap(size)
    }

    @Test func theShaperGivesTheGlyphsAndTheirPlaces() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TextBlockShapedTests.latin)
        let fallback = try Font(memory.pdf, TextBlockShapedTests.cascadia)
        let a = font.unicodeToGID[0x41]
        let b = fallback.unicodeToGID[0x42]
        let shaper = FixedShaper([
            ShapedLine(0, 2, [0, 1], [a, b], [0, 0, 7, 0], 13),
            // A glyph of a font that was not given is not drawn
            ShapedLine(2, 3, [-1], [5], [0, 0], 6),
        ])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let text = TextBlock(font, "कखग").setFallbackFont(fallback).setFontSize(10).setLanguage("en")
                .setShaper(shaper).setWidth(120).setLocation(50, 100)
        let corner = text.drawOn(page)
        #expect(shaper.files.count == 2)
        #expect(shaper.size == 10)
        #expect(shaper.width == 120)
        #expect(shaper.language == "en")
        #expect(abs(corner[1] - (100 + 2 * body(font, 10))) < 0.001)
        // The fallback's glyph at the block's size, 10 points, on the first line's baseline
        let y = 792 - (100 + font.getAscent(10))
        let placed = places(TestSupport.content(page))
        #expect(placed.count == 2)
        #expect(abs(placed[0][0] - 0.01) < 1e-6)            // Plex, 1,000 units an em
        #expect(abs(placed[1][0] - 10.0 / 2048) < 1e-6)     // Cascadia, 2,048
        #expect(placed[0][1] == 50 && placed[1][1] == 57)
        #expect(abs(placed[0][2] - y) < 0.006 && abs(placed[1][2] - y) < 0.006)
        // Shaped once however often the block is measured and drawn
        text.drawOn(nil)
        text.drawOn(page)
        #expect(shaper.calls == 1)
        try memory.pdf.complete()
    }

    // A block of a paragraph of English, one of Hindi and one of Arabic: the
    // English and the Arabic drawn as text, the Arabic right to left by itself
    // and aligned to the right; the Hindi as glyphs, between them.
    @Test func drawsEachParagraphAsItNeeds() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari)
        let arabic = try Font(memory.pdf, TextBlockShapedTests.arabic)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextBlock(font, "Hello\nकखग\nمرحبا").setFallbackFont(arabic).setFontSize(10).setLanguage("hi")
                .setShaper(LetterShaper(font)).setWidth(300).setLocation(50, 100).drawOn(page)
        let content = TestSupport.content(page)
        try memory.pdf.complete()
        #expect(count(content, " cm /Fm") == 3)
        for letter in ["क", "ख", "ग"] {
            #expect(content.contains("/ActualText <" + Page.toUTF16Hex(letter) + ">"), "\(letter)")
        }
        let xs = content.components(separatedBy: "\n")
                .filter { $0.hasPrefix("1 0 0 1 ") && $0.hasSuffix(" Tm") }
                .map { Float($0.components(separatedBy: " ")[4])! }
        #expect(xs.count == 2, "\(content)")
        #expect(xs.first == 50)
        #expect((xs.last ?? 0) > 300, "Arabic at the left: \(xs)")
    }

    @Test func theFontFileIsWhatTheFontWasLoadedFrom() throws {
        let memory = MemoryPDF()
        let bytes = [UInt8](try Data(contentsOf: URL(fileURLWithPath: TextBlockShapedTests.devanagari)))
        let fromBytes = try Font(memory.pdf, bytes)
        let fromPath = try Font(memory.pdf, TextBlockShapedTests.devanagari)
        let shaper = FixedShaper([])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextBlock(fromBytes, "क").setFallbackFont(fromPath).setShaper(shaper).drawOn(page)
        #expect(shaper.files[0] == bytes)
        #expect(shaper.files[1] == bytes)
    }

    @Test func anOpenTypeFontFromAStreamFailsTheDocument() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextBlock(font, "क").setShaper(FixedShaper([])).drawOn(page)
        #expect(memory.pdf.error?.contains("font files") == true, "\(memory.pdf.error ?? "")")
        #expect(throws: (any Error).self) {
            try memory.pdf.complete()
        }
    }

    // Two lines: "A A B " of 30 points without its last space, then "B";
    // the x of each glyph drawn
    private func aligned(_ alignment: Alignment) throws -> [Float] {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TextBlockShapedTests.latin)
        let a = font.unicodeToGID[0x41]
        let b = font.unicodeToGID[0x42]
        let space = font.unicodeToGID[0x20]
        let shaper = FixedShaper([
            ShapedLine(0, 6, [0, 0, 0, 0, 0, 0], [a, space, a, space, b, space],
                    [0, 0, 10, 0, 12, 0, 22, 0, 24, 0, 30, 0], 30),
            ShapedLine(6, 7, [0], [b], [0, 0], 6),
        ])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        TextBlock(font, "क क ख ख").setFontSize(10).setShaper(shaper).setTextAlignment(alignment)
                .setWidth(100).setLocation(50, 100).drawOn(page)
        return places(TestSupport.content(page)).map { $0[1] }
    }

    @Test func theShapedLinesAreAligned() throws {
        #expect(try aligned(Alignment.LEFT) == [50, 62, 74, 50])
        #expect(try aligned(Alignment.CENTER) == [85, 97, 109, 97])
        #expect(try aligned(Alignment.RIGHT) == [120, 132, 144, 144])
    }

    @Test func shapedLinesAreSpacedAsItsText() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari)
        let body = self.body(font, 10)
        let text = TextBlock(font, "कख").setFontSize(10).setShaper(LetterShaper(font)).setWidth(200)
        #expect(abs(text.drawOn(nil)[1] - 2 * body) < 0.001)
        text.setLineSpacing(1.5)
        #expect(abs(text.drawOn(nil)[1] - 3 * body) < 0.001)
        // With a height, the lines that fit, the glyphs not cut with "..."
        text.setLineSpacing(1).setHeight(1.5 * body)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        text.drawOn(page)
        #expect(count(TestSupport.content(page), " cm /Fm") == 1)
    }

    // Text such as Hindi in Table cells: each cell as tall as its text
    // wrapped to the column, the text drawn whole each time, as the header
    // row is on each page.
    @Test func inTableCellsItWrapsAndRepeatsTheHeader() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari)
        let line = body(font, 10)
        let shaper = LetterShaper(font)
        func hindi(_ text: String) -> TextBlock {
            TextBlock(font, text).setFontSize(10).setShaper(shaper).setLanguage("hi")
        }
        var data: [[Cell]] = [[Cell(font).setTextBlock(hindi("क")), Cell(font).setTextBlock(hindi("ख"))]]
        for r in 0..<40 {
            data.append([Cell(font, "Row \(r + 1)"), Cell(font).setTextBlock(hindi("कखग"))])
        }
        let table = Table().setTableData(data, 1)
        table.setLocation(50, 50)
        table.setColumnWidth(0, 100).setColumnWidth(1, 250)
        let cell = data[1][1]
        #expect(abs(cell.getHeight(250) - (3 * line + cell.getTopPadding() + cell.getBottomPadding())) < 0.01)
        var pages = [Page]()
        table.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 2, "the table on \(pages.count) page")
        let firstPage = TestSupport.content(pages[0])
        #expect(firstPage.contains("/ActualText <" + Page.toUTF16Hex("क") + ">"))
        #expect(firstPage.contains("/ActualText <" + Page.toUTF16Hex("ख") + ">"))
        for i in 1..<pages.count {
            #expect(count(TestSupport.content(pages[i]), " cm /Fm") >= 2, "page \(i + 1): no glyphs")
        }
    }

    // TextLine with text such as Hindi: shaped as one line, drawn as glyphs,
    // tagged with its text, its width the glyphs'.
    @Test func textLineShapesTextSuchAsHindi() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari).setSize(10)
        TextBlock.setDefaultShaper(LetterShaper(font))
        defer { TextBlock.setDefaultShaper(nil) }
        let line = TextLine(font, "कखग")
        line.setLocation(50, 100)
        #expect(abs(line.getWidth() - 21) < 0.001)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        line.drawOn(page)
        let content = TestSupport.content(page)
        #expect(count(content, " cm /Fm") == 3, "\(content)")
        #expect(content.contains("/ActualText <" + Page.toUTF16Hex("कखग") + ">"), "\(content)")
        #expect(!content.contains("Tj"), "\(content)")
        try memory.pdf.complete()
    }

    // A TextLine of text such as Hindi turned: its glyphs turned around its
    // start as the same line of English is, the one rotation for both.
    @Test func textLineTurnsItsGlyphsAsItsText() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari).setSize(10)
        let latin = try Font(memory.pdf, TextBlockShapedTests.latin).setSize(10)
        TextBlock.setDefaultShaper(LetterShaper(font))
        defer { TextBlock.setDefaultShaper(nil) }
        func numbers(_ line: String) -> [Float] {
            line.components(separatedBy: " ").prefix(4).map { Float($0)! }
        }
        for degrees in [30, 90, -45] {
            let english = Page(memory.pdf, Letter.PORTRAIT)
            let text = TextLine(latin, "Hello")
            text.setLocation(50, 100)
            text.setTextRotation(degrees)
            text.drawOn(english)
            let englishContent = TestSupport.content(english)
            let hindi = Page(memory.pdf, Letter.PORTRAIT)
            let line = TextLine(font, "कखग")
            line.setLocation(50, 100)
            line.setTextRotation(degrees)
            line.drawOn(hindi)
            let content = TestSupport.content(hindi)
            let lines = content.components(separatedBy: "\n")
            guard let tm = englishContent.components(separatedBy: "\n").first(where: { $0.hasSuffix(" Tm") }),
                    let at = lines.firstIndex(of: "1 0 0 1 50 692 cm"), at > 0, lines[at - 1] == "q",
                    at + 2 < lines.count, lines[at + 2] == "1 0 0 1 -50 -692 cm" else {
                Issue.record("\(degrees) degrees:\n\(englishContent)\n\(content)")
                continue
            }
            let a = numbers(tm)
            let b = numbers(lines[at + 1])
            for i in 0..<4 {
                #expect(abs(a[i] - b[i]) < 1e-5, "\(degrees) degrees: \(tm) and \(lines[at + 1])")
            }
            #expect(count(content, " cm /Fm") == 3)
        }
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let flat = TextLine(font, "कखग")
        flat.setLocation(50, 100)
        flat.drawOn(page)
        #expect(!TestSupport.content(page).contains("1 0 0 1 -50 -692 cm"))
        try memory.pdf.complete()
    }

#if os(Linux)
    // On Linux, without a shaper set, TextBlock does not run hb-shape, even
    // where it is installed: it says to set HBShape, so that a program
    // shapes the same wherever it runs
    @Test func withoutAShaperRunsNoHBShape() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TextBlockShapedTests.devanagari)
        TextBlock(font, "कखग").setWidth(300).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        #expect(memory.pdf.error?.contains(".setShaper(HBShape())") == true, "\(memory.pdf.error ?? "")")
    }
#endif

#if canImport(CoreText)
    // Core Text shapes Hindi with an English name: tagged lines of outlines
    @Test func coreTextShapesHindiIntoTaggedLinesOfOutlines() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let hindi = try Font(memory.pdf, TextBlockShapedTests.devanagari).setSize(12)
        let latin = try Font(memory.pdf, TextBlockShapedTests.latin)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let corner = TextBlock(hindi, TextBlockShapedTests.hindi).setFallbackFont(latin).setLanguage("hi")
                .setWidth(200).setLocation(50, 100).drawOn(page)
        let content = TestSupport.content(page)
        let lines = count(content, "/ActualText <")
        #expect(lines >= 3, "wrapped to 200 points: \(lines) lines")
        #expect(abs(corner[1] - (100 + Float(lines) * body(hindi, 12))) < 0.01)
        #expect(!content.contains("Tj"), "\(content)")
        try memory.pdf.complete()
    }
#endif
}
