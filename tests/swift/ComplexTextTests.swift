/**
 * ComplexTextTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// ComplexText: the platform's text engine shapes and wraps the text, and the
/// glyphs it found are drawn with drawGlyphs, each line tagged with its text.
@Suite struct ComplexTextTests {
    private static let devanagari = "examples/fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
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

        init(_ lines: [ShapedLine]) {
            self.lines = lines
        }

        func shape(_ request: ShapingRequest) throws -> [ShapedLine] {
            self.files = request.fontFiles
            self.size = request.fontSize
            self.width = request.width
            self.language = request.language
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

    @Test func theShaperGivesTheGlyphsAndTheirPlaces() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let fallback = try Font(memory.pdf, ComplexTextTests.cascadia)
        let a = font.unicodeToGID[0x41]
        let b = fallback.unicodeToGID[0x42]
        let shaper = FixedShaper([
            ShapedLine(0, 2, [0, 1], [a, b], [0, 0, 7, 0], 13),
            // A glyph of a font that was not given is not drawn
            ShapedLine(2, 3, [-1], [5], [0, 0], 6),
        ])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let corner = ComplexText(font, "ABC")
                .setFallbackFont(fallback)
                .setLanguage("en")
                .setShaper(shaper)
                .setLocation(50, 100)
                .setWidth(120)
                .drawOn(page)
        #expect(shaper.files.count == 2)
        #expect(shaper.size == 10)
        #expect(shaper.width == 120)
        #expect(shaper.language == "en")

        let lineHeight = font.getAscent() + font.getDescent() + font.getLineGap(10)
        #expect(abs(corner[1] - (100 + 2 * lineHeight)) < 0.001)
        // The fallback's glyph at the font's size, 10 points, on the first line's baseline
        let y = 792 - (100 + font.getAscent())
        let placed = places(TestSupport.content(page))
        #expect(placed.count == 2)
        #expect(abs(placed[0][0] - 0.01) < 1e-6)            // Plex, 1,000 units an em
        #expect(abs(placed[1][0] - 10.0 / 2048.0) < 1e-6)   // Cascadia, 2,048
        #expect(placed[0][1] == 50 && placed[1][1] == 57)
        #expect(abs(placed[0][2] - y) < 0.006 && abs(placed[1][2] - y) < 0.006)
        try memory.pdf.complete()
    }

    // Two lines: "A A B " of 30 points without its last space, then "B";
    // the x of each glyph drawn
    private func aligned(_ alignment: Alignment) throws -> [Float] {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let a = font.unicodeToGID[0x41]
        let b = font.unicodeToGID[0x42]
        let space = font.unicodeToGID[0x20]
        let shaper = FixedShaper([
            ShapedLine(0, 6, [0, 0, 0, 0, 0, 0], [a, space, a, space, b, space],
                    [0, 0, 10, 0, 12, 0, 22, 0, 24, 0, 30, 0], 30),
            ShapedLine(6, 7, [0], [b], [0, 0], 6),
        ])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        ComplexText(font, "A A B B").setShaper(shaper).setTextAlignment(alignment)
                .setWidth(100).setLocation(50, 100).drawOn(page)
        return places(TestSupport.content(page)).map { $0[1] }
    }

    @Test func theLinesAreAligned() throws {
        // The glyphs drawn: A, A, B of the first line, B of the second
        #expect(try aligned(Alignment.LEFT) == [50, 62, 74, 50])
        #expect(try aligned(Alignment.CENTER) == [85, 97, 109, 97])
        #expect(try aligned(Alignment.RIGHT) == [120, 132, 144, 144])
        // Justified: the 70 points left over go to the two spaces between the
        // words; the last line is left as it is
        #expect(try aligned(Alignment.JUSTIFY) == [50, 97, 144, 50])
    }

    @Test func theLineSpacingIsAMultipleOfTheBodyHeightAsTextBlocks() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let a = font.unicodeToGID[0x41]
        let shaper = FixedShaper([
            ShapedLine(0, 1, [0], [a], [0, 0], 7),
            ShapedLine(1, 2, [0], [a], [0, 0], 7),
        ])
        let text = ComplexText(font, "AA").setShaper(shaper)
        let body = font.getAscent() + font.getDescent() + font.getLineGap(10)
        #expect(abs(try text.getTextHeight() - 2 * body) < 0.001)
        #expect(abs(try text.setLineSpacing(1.5).getTextHeight() - 3 * body) < 0.001)
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
            for (i, c) in request.text.utf16.enumerated() {
                lines.append(ShapedLine(i, i + 1, [0], [font.unicodeToGID[Int(c)]], [0, 0], 7))
            }
            return lines
        }
    }

    @Test func paragraphsFlowFromPageToPageCutWhereTheHeightEnds() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let line = font.getAscent() + font.getDescent() + font.getLineGap(10)
        let text = ComplexText(font, ["AAA", "BBB"])
                .setShaper(LetterShaper(font))
                .setLanguage("en")
                .setParagraphGap(5)
                .setHeight(4 * line + 5)
        #expect(abs(try text.getTextHeight() - (6 * line + 5)) < 0.001)
        // Three lines of the first paragraph, the gap, one of the second
        let first = Page(memory.pdf, Letter.PORTRAIT)
        var corner = text.setLocation(50, 100).drawOn(first)
        #expect(abs(corner[1] - (100 + 4 * line + 5)) < 0.001)
        #expect(try text.hasMoreText())
        #expect(abs(try text.getTextHeight() - 2 * line) < 0.001)
        let content = TestSupport.content(first)
        #expect(count(content, " cm /Fm") == 4, "\(content)")
        #expect(count(content, "/P <<") == 2, "a paragraph each, the second begun")
        // The rest of the second paragraph, from the top of the next page
        let second = Page(memory.pdf, Letter.PORTRAIT)
        corner = text.setLocation(50, 50).drawOn(second)
        #expect(abs(corner[1] - (50 + 2 * line)) < 0.001)
        #expect(try !text.hasMoreText())
        #expect(try text.getTextHeight() == 0)
        #expect(count(TestSupport.content(second), " cm /Fm") == 2)
        try memory.pdf.complete()
    }

    @Test func getHeightIsTheHeightSetAndTheGapIsTextFramesEmptyLine() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let text = ComplexText(font, ["A", "B"]).setShaper(LetterShaper(font))
        #expect(text.getHeight() == 0)
        #expect(text.setHeight(500).getHeight() == 500)
        // Two lines, one empty line between them, as a TextFrame's default
        let line = font.getAscent() + font.getDescent() + font.getLineGap(10)
        let gap = font.getAscent() + font.getDescent() + 2 * font.getLineGap(10)
        #expect(abs(try text.getTextHeight() - (2 * line + gap)) < 0.001)
    }

    @Test func aHeightTooSmallForALineStillDrawsOneEachTime() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let text = ComplexText(font, "AB").setShaper(LetterShaper(font)).setHeight(1)
        var pages = 0
        while try text.hasMoreText() {
            text.drawOn(Page(memory.pdf, Letter.PORTRAIT))
            pages += 1
        }
        #expect(pages == 2)
    }

    @Test func theFontFileIsWhatTheFontWasLoadedFrom() throws {
        let memory = MemoryPDF()
        let bytes = [UInt8](try Data(contentsOf: URL(fileURLWithPath: ComplexTextTests.devanagari)))
        let fromBytes = try Font(memory.pdf, bytes)
        let fromPath = try Font(memory.pdf, ComplexTextTests.devanagari)
        let trueType = try Font(memory.pdf, InputStream(fileAtPath: ComplexTextTests.cascadia)!)
        let shaper = FixedShaper([])
        let page = Page(memory.pdf, Letter.PORTRAIT)
        ComplexText(fromBytes, "x").setFallbackFont(fromPath).setShaper(shaper).drawOn(page)
        #expect(shaper.files[0] == bytes)
        #expect(shaper.files[1] == bytes)
        // A TrueType font keeps its whole file, from a stream too
        ComplexText(trueType, "x").setShaper(shaper).drawOn(page)
        #expect(shaper.files[0] == [UInt8](try Data(contentsOf: URL(fileURLWithPath: ComplexTextTests.cascadia))))
    }

    @Test func anOpenTypeFontFromAStreamFailsTheDocument() throws {
        let memory = MemoryPDF()
        let font = try TestSupport.plexSans(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        ComplexText(font, "x").setShaper(FixedShaper([])).drawOn(page)
        #expect(throws: (any Error).self) {
            try memory.pdf.complete()
        }
    }

    // Without a page, drawOn measures: the corner drawOn would return, the
    // text left as it was, so that a Table cell can measure it
    @Test func drawOnWithoutAPageMeasuresAndKeepsTheText() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let line = font.getAscent() + font.getDescent() + font.getLineGap(10)
        let text = ComplexText(font, "ABCD").setShaper(LetterShaper(font)).setWidth(150)
        text.setLocation(50, 100)
        let first = text.drawOn(nil)
        let second = text.drawOn(nil)
        #expect(abs(first[0] - 200) < 0.001)
        #expect(abs(first[1] - (100 + 4 * line)) < 0.001)
        #expect(first == second)
        #expect(try text.hasMoreText())
        // With a height, the lines that fit, as drawOn draws them
        text.setHeight(2.5 * line)
        #expect(abs(text.drawOn(nil)[1] - (100 + 2 * line)) < 0.001)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        #expect(abs(text.drawOn(page)[1] - (100 + 2 * line)) < 0.001)
        try memory.pdf.complete()
    }

    // Text such as Hindi in Table cells: each cell as tall as its text
    // wrapped to the column, the text drawn whole each time, as the header
    // row is on each page; the caller's text where it was
    @Test func inTableCellsItWrapsAndRepeatsTheHeader() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, ComplexTextTests.latin).setSize(10)
        let line = font.getAscent() + font.getDescent() + font.getLineGap(10)
        let shaper = LetterShaper(font)
        var data: [[Cell]] = [[
            Cell(font).setDrawable(ComplexText(font, "X").setShaper(shaper).setLanguage("hi")),
            Cell(font).setDrawable(ComplexText(font, "Z").setShaper(shaper).setLanguage("hi")),
        ]]
        let kept = ComplexText(font, "ABC").setShaper(shaper).setLanguage("hi")
        kept.setLocation(7, 9)
        for r in 0..<40 {
            let text = r == 0 ? kept : ComplexText(font, "ABC").setShaper(shaper).setLanguage("hi")
            data.append([Cell(font, "Row \(r + 1)"), Cell(font).setDrawable(text)])
        }
        let table = Table().setTableData(data, 1)
        table.setLocation(50, 50)
        table.setColumnWidth(0, 100).setColumnWidth(1, 250)
        // A cell is as tall as its lines, three here, and its padding
        let cell = data[1][1]
        #expect(abs(cell.getHeight(250) - (3 * line + cell.getTopPadding() + cell.getBottomPadding())) < 0.01)
        var pages = [Page]()
        table.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 2, "the table on \(pages.count) page")
        // The header row: its words as text on the first page; on the next
        // pages drawn again, as an artifact, as PDF/UA has a repeated header
        let firstPage = TestSupport.content(pages[0])
        #expect(count(firstPage, "/ActualText <" + Page.toUTF16Hex("X") + ">") == 1)
        #expect(count(firstPage, "/ActualText <" + Page.toUTF16Hex("Z") + ">") == 1)
        for i in 1..<pages.count {
            let content = TestSupport.content(pages[i])
            guard let start = content.range(of: "/Artifact BMC\n") else {
                Issue.record("page \(i + 1) has no header")
                continue
            }
            let rest = content[start.lowerBound...]
            let end = rest.range(of: "EMC\n")?.lowerBound ?? rest.endIndex
            #expect(count(String(rest[..<end]), " cm /Fm") >= 2, "page \(i + 1): its header's glyphs drawn")
        }
        // The caller's text kept where it was, and whole; as wide as the
        // cell's inside, as a text block in a cell is
        let corner = kept.drawOn(nil)
        #expect(abs(corner[0] - (7 + 250 - (cell.getLeftPadding() + cell.getRightPadding()))) < 0.001)
        #expect(abs(corner[1] - (9 + 3 * line)) < 0.001)
    }

#if canImport(CoreText)
    @Test func coreTextShapesHindiIntoTaggedLinesOfOutlines() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Title")
        let hindi = try Font(memory.pdf, ComplexTextTests.devanagari).setSize(12)
        let latin = try Font(memory.pdf, ComplexTextTests.latin)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let text = ComplexText(hindi, ComplexTextTests.hindi)
                .setFallbackFont(latin)
                .setLanguage("hi")
                .setLocation(50, 100)
                .setWidth(200)
        let height = try text.getTextHeight()
        let corner = text.drawOn(page)
        #expect(corner[0] == 250)
        #expect(abs(corner[1] - (100 + height)) < 0.001)

        let content = TestSupport.content(page)
        // One paragraph in Hindi, a span of actual text for each line, no text
        #expect(count(content, "/P <</MCID 0>>\nBDC\n") == 1, "\(content)")
        let lines = count(content, "/Span <</Lang <" + memory.pdf.toHex("hi") + "> /ActualText <")
        #expect(lines >= 3, "wrapped to 200 points: \(lines) lines")
        #expect(abs(Float(lines) * (hindi.getAscent() + hindi.getDescent() + hindi.getLineGap(12)) - height) < 0.01)
        #expect(!content.contains("BT"), "\(content)")
        try memory.pdf.complete()
    }
#endif
}   // End of ComplexTextTests.swift
