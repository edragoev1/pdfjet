/**
 * MarkdownTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct MarkdownTests {
    // A Markdown text drawn in a PDF/UA document: its pages and the whole PDF.
    final class Drawn {
        var pages = [Page]()
        var pdf = ""
        var text = ""   // The text of the pages, from their content streams
        var contents = [String]()   // Of each page, before it is written
    }

    static func draw(_ text: String, _ imageDirectory: String?) throws -> Drawn {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let pdf = memory.pdf
        pdf.setTitle("Markdown")
        let regular = try Font(pdf, CoreFont.HELVETICA)
        let bold = try Font(pdf, CoreFont.HELVETICA_BOLD)
        let italic = try Font(pdf, CoreFont.HELVETICA_OBLIQUE)
        let boldItalic = try Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE)
        let code = try Font(pdf, CoreFont.COURIER)
        let markdown = Markdown(regular, bold, italic, boldItalic, code)
        if let imageDirectory {
            markdown.setImageDirectory(imageDirectory)
        }
        let drawn = Drawn()
        try markdown.drawOn(pdf, text, &drawn.pages, Letter.PORTRAIT)
        var content = ""
        for page in drawn.pages {
            drawn.contents.append(TestSupport.content(page))
            content += TestSupport.content(page)
        }
        drawn.text = content
        if !drawn.pages.isEmpty {
            pdf.addPages(drawn.pages)
            try pdf.complete()
            drawn.pdf = TestSupport.latin1(memory.bytes)
        }
        return drawn
    }

    static func count(_ pdf: String, _ structure: String) -> Int {
        return pdf.components(separatedBy: "/S /" + structure + "\n").count - 1
    }

    // The first group of each match of the pattern in the text.
    static func groups(_ pattern: String, _ text: String) -> [String] {
        let regex = try! NSRegularExpression(pattern: pattern) // The pattern is valid.
        let string = text as NSString
        return regex.matches(in: text, range: NSRange(location: 0, length: string.length)).map {
            string.substring(with: $0.range(at: 1))
        }
    }

    @Test func anEmptyTextNeedsNoPage() throws {
        #expect(try MarkdownTests.draw("", nil).pages.count == 0)
        #expect(try MarkdownTests.draw("\n  \n", nil).pages.count == 0)
    }

    @Test func everyBlockIsTaggedForPDFUA() throws {
        let drawn = try MarkdownTests.draw("# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n"
                + "```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next", nil)
        #expect(drawn.pages.count == 1)
        #expect(MarkdownTests.count(drawn.pdf, "H1") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "H2") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "L") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "LI") == 2)
        #expect(MarkdownTests.count(drawn.pdf, "Lbl") == 2)
        #expect(MarkdownTests.count(drawn.pdf, "LBody") == 2)
        #expect(MarkdownTests.count(drawn.pdf, "BlockQuote") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "Code") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "Table") == 1)
    }

    @Test func headingLevelsSkipNone() throws {
        // A text that starts at ### and goes on to ##### is H1, then H2.
        let drawn = try MarkdownTests.draw("### Three\n\n##### Five\n\n# One", nil)
        #expect(MarkdownTests.count(drawn.pdf, "H1") == 2)
        #expect(MarkdownTests.count(drawn.pdf, "H2") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "H3") == 0)
    }

    @Test func theTextFlowsOntoAsManyPagesAsItNeeds() throws {
        var text = "# A long text\n\n"
        for i in 0..<150 {
            text += "Paragraph p\(1000 + i) has words enough to take a line or two of the page.\n\n"
        }
        let drawn = try MarkdownTests.draw(text, nil)
        #expect(drawn.pages.count >= 3, "\(drawn.pages.count) pages")
        for i in 0..<150 {
            let word = TestSupport.hex("p\(1000 + i)")
            #expect(drawn.text.components(separatedBy: word).count - 1 == 1, "p\(1000 + i)")
        }
        // No text is drawn under the bottom margin of 72 points.
        for y in MarkdownTests.groups("[-0-9.]+ ([-0-9.]+) Td\n", drawn.text) {
            #expect(Float(y)! >= 72.0, "a baseline at y = \(y)")
        }
    }

    @Test func aListOverPagesIsOneList() throws {
        var text = ""
        for i in 0..<80 {
            text += "- item \(i)\n"
        }
        let drawn = try MarkdownTests.draw(text, nil)
        #expect(drawn.pages.count >= 2, "\(drawn.pages.count) pages")
        #expect(MarkdownTests.count(drawn.pdf, "L") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "LI") == 80)
    }

    @Test func aTableGoesOnOnTheNextPageWithItsHeaderRow() throws {
        // Letters of the core fonts that are kerned are drawn apart, so the
        // header is one letter a column.
        var text = "Some text first.\n\n| N | S |\n|---:|---:|\n"
        for i in 1...120 {
            text += "| \(i) | \(i * i) |\n"
        }
        let drawn = try MarkdownTests.draw(text, nil)
        #expect(drawn.pages.count >= 2, "\(drawn.pages.count) pages")
        #expect(MarkdownTests.count(drawn.pdf, "Table") == 1)
        for content in drawn.contents {
            #expect(content.contains("<" + TestSupport.hex("S") + ">"), "the header row is on every page")
        }
        #expect(drawn.text.contains(TestSupport.hex("14400")))
    }

    @Test func imagesAreReadOnlyFromTheImageDirectory() throws {
        let text = "![Tux](linux-logo.png)"
        // With no directory, the image's text is drawn instead.
        var drawn = try MarkdownTests.draw(text, nil)
        #expect(MarkdownTests.count(drawn.pdf, "Figure") == 0)
        #expect(drawn.text.contains(TestSupport.hex("Tux")))
        // From the directory, the image is a figure.
        let images = TestSupport.path("images")
        drawn = try MarkdownTests.draw(text, images)
        #expect(MarkdownTests.count(drawn.pdf, "Figure") == 1)
        let alt = MarkdownTests.groups("/Alt <([0-9A-Fa-f]+)>", drawn.pdf)
        #expect(!alt.isEmpty && TestSupport.utf16Hex(alt[0]) == "Tux",
                "the text of the image is its description")
        // Not above it, not an absolute path, not a URL.
        for source in ["../images/linux-logo.png", "/etc/passwd",
                TestSupport.path("images/linux-logo.png"), "https://pdfjet.com/logo.png",
                "missing.png"] {
            drawn = try MarkdownTests.draw("![Not read](" + source + ")", images)
            #expect(MarkdownTests.count(drawn.pdf, "Figure") == 0, "\(source)")
            #expect(drawn.text.contains(TestSupport.hex("Not read")), "\(source)")
        }
    }

    @Test func codeKeepsItsLinesAndGoesOnOnTheNextPage() throws {
        var text = "```\n"
        for i in 0..<90 {
            text += "  line \(i) *not emphasis*\n"
        }
        text += "```\n"
        let drawn = try MarkdownTests.draw(text, nil)
        #expect(drawn.pages.count >= 2, "\(drawn.pages.count) pages")
        #expect(MarkdownTests.count(drawn.pdf, "Code") == 1)
        #expect(drawn.text.contains(TestSupport.hex("  line 0 *not emphasis*")))
        #expect(drawn.text.contains(TestSupport.hex("  line 89 *not emphasis*")))
    }

    @Test func numberedListsStartAtTheirFirstNumber() throws {
        let drawn = try MarkdownTests.draw("3. three\n4. four", nil)
        #expect(drawn.text.contains(TestSupport.hex("3.")))
        #expect(drawn.text.contains(TestSupport.hex("4.")))
        #expect(!drawn.text.contains("<" + TestSupport.hex("1.") + ">"))
    }

    @Test func htmlIsDrawnAsText() throws {
        let drawn = try MarkdownTests.draw("<b>not bold</b>", nil)
        #expect(drawn.text.contains(TestSupport.hex("<b>not")))
    }

    @Test func longCodeLinesAreCutInLinearTime() throws {
        var line = [UInt16]()
        for i in 0..<200000 {
            line.append(UInt16(0x61 + i % 26))
        }
        let start = ContinuousClock.now
        let drawn = try MarkdownTests.draw("```\n" + String(decoding: line, as: UTF16.self) + "\n```", nil)
        let elapsed = ContinuousClock.now - start
        #expect(drawn.pages.count > 1)
        #expect(elapsed < .seconds(5), "\(elapsed)")
    }

    @Test func aCodeLineOfOneColumnKeepsItsSurrogatePairs() throws {
        let memory = MemoryPDF(Compliance.PDF_1_7)
        let pdf = memory.pdf
        let font = try Font(pdf, CoreFont.HELVETICA)
        let code = try Font(pdf, CoreFont.COURIER)
        // A width that one character of the code fills.
        let markdown = Markdown(font, font, font, font, code).setMargins(300, 72, 300, 72)
        var pages = [Page]()
        try markdown.drawOn(pdf, "```\n\u{1F600}x\u{1F600}\n```", &pages, Letter.PORTRAIT)
        #expect(pages.count == 1)
    }
}
