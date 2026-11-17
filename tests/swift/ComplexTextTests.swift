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

        func shape(_ text: String, _ language: String?, _ fontFiles: [[UInt8]],
                _ fontSize: Float, _ width: Float) throws -> [ShapedLine] {
            self.files = fontFiles
            self.size = fontSize
            self.width = width
            self.language = language
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
        #expect(abs(try text.getHeight() - 2 * body) < 0.001)
        #expect(abs(try text.setLineSpacing(1.5).getHeight() - 3 * body) < 0.001)
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
        let height = try text.getHeight()
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
