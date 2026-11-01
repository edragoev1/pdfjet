/**
 * ReviewFiveTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The findings of the fresh-eyes review of the charts, Markdown and the
/// barcodes, of 27 October 2026.
@Suite struct ReviewFiveTests {
    private static func markup(_ pdf: PDF) -> Markup {
        let font = TestSupport.helvetica(pdf)
        return Markup(font, font, font, font, font)
    }

    // The words of the text, one space between them, as Util.joinFields in Java.
    private static func joinFields(_ text: String) -> String {
        return text.split(whereSeparator: { $0.isWhitespace }).joined(separator: " ")
    }

    @Test func theTextOfAParagraphHasNoSpaceWhereItsRunsAreJoined() {
        let markup = ReviewFiveTests.markup(TestSupport.newPDF())
        #expect(ReviewFiveTests.joinFields(markup.paragraph("Version **9.1.5**: notes").text()) == "Version 9.1.5: notes")
        #expect(ReviewFiveTests.joinFields(markup.paragraph("The `Table`s").text()) == "The Tables")
        #expect(ReviewFiveTests.joinFields(markup.paragraph("one<br>two").text()) == "one two")
    }

    @Test func theBookmarkOfAHeadingHasItsText() throws {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font).drawOn(pdf, "# Version **9.1.5**: notes", &pages, Letter.PORTRAIT)
        pdf.addPages(pages)
        try pdf.complete()
        let text = TestSupport.latin1(memory.bytes)
        #expect(text.contains(TestSupport.textString("Version 9.1.5: notes")), "the title")
    }

    @Test func aBackslashAtTheEndOfAParagraphIsKept() {
        let markup = ReviewFiveTests.markup(TestSupport.newPDF())
        let paragraphs = markup.paragraphs("Save it to C:\\Temp\\")
        #expect(paragraphs[0].text().trim() == "Save it to C:\\Temp\\")
    }

    @Test func theAltTextOfAnImageIsItsText() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        // No image directory: the text is drawn in the image's place
        try Markdown(font, font, font, font, font)
                .drawOn(pdf, "![Fish &amp; Chips \\*](fish.png)", &pages, Letter.PORTRAIT)
        let content = TestSupport.content(pages[0])
        #expect(content.contains(TestSupport.hex("Fish & Chips *")), "\(content)")
    }

    @Test func aLineValueOutsideTheRangeIsNotDrawnAtTheEdge() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var page = Page(pdf, Letter.PORTRAIT)
        var chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200)
                .setCategories("a", "b").setValueAxisRange(0, 100)
        chart.addSeries("", [50, 60])
        chart.addLineSeries("target", [150, 80])
        chart.drawOn(page)
        var content = TestSupport.content(page)
        #expect(content.contains("W\nn\n"), "\(content)")     // Clipped
        // In range, no clip
        page = Page(pdf, Letter.PORTRAIT)
        chart = BarChart(font, font).setLocation(50, 50).setSize(300, 200)
                .setCategories("a", "b").setValueAxisRange(0, 100)
        chart.addSeries("", [50, 60])
        chart.addLineSeries("target", [90, 80])
        chart.drawOn(page)
        content = TestSupport.content(page)
        #expect(!content.contains("W\nn\n"), "\(content)")
    }

    @Test func aHeadingOnAPageThatIsNotAddedHasNoBookmark() throws {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let font = TestSupport.helvetica(pdf)
        var text = "# One\n\n"
        for i in 0..<80 {
            text += "p\(i)\n\n"
        }
        text += "# Two"
        var pages = [Page]()
        try Markdown(font, font, font, font, font).drawOn(pdf, text, &pages, Letter.PORTRAIT)
        #expect(pages.count > 1)
        pdf.addPage(pages[0])       // The page of One only
        try pdf.complete()          // It failed: "Two" was on a page that is not added
        let out = TestSupport.latin1(memory.bytes)
        #expect(out.contains(TestSupport.textString("One")))
        #expect(!out.contains(TestSupport.textString("Two")))
    }

    @Test func anEmptyCode128IsRefused() {
        #expect(throws: (any Error).self) {
            _ = try Barcode(Barcode.CODE_128, "")
        }
    }

    @Test func twoBreaksLeaveAnEmptyRow() {
        let pdf = TestSupport.newPDF()
        let markup = ReviewFiveTests.markup(pdf)
        #expect(markup.paragraph("p1<br><br>p2").lines[0].lineBreaks == 2)
        var gaps: [Float] = [0, 0]
        let texts = ["p1<br>p2", "p1<br><br>p2"]
        for k in 0..<2 {
            let page = Page(pdf, Letter.PORTRAIT)
            TextFrame([markup.paragraph(texts[k])]).setLocation(50, 50).setWidth(400).drawOn(page)
            let content = TestSupport.content(page)
            gaps[k] = TestSupport.positionOf(content, "p1")[1] - TestSupport.positionOf(content, "p2")[1]
        }
        TestSupport.expectNear(2 * gaps[0], gaps[1], 0.05)     // To the 0.01 of the positions written
    }

    @Test func aPipeInCodeInATableCell() {
        #expect(MarkdownParserTests.describe(MarkdownParser.parse("| a |\n|---|\n| `x\\|y` |")) == "TABLE(a;`x|y`|L)")
    }

    @Test func hardBreaksOfCRLFText() {
        let markup = ReviewFiveTests.markup(TestSupport.newPDF())
        #expect(markup.paragraph("one  \r\ntwo").lines[0].lineBreaks == 1)
        #expect(markup.paragraph("one\\\r\ntwo").lines[0].lineBreaks == 1)
        #expect(markup.paragraph("one \r\ntwo").lines[0].lineBreaks == 0)
    }

    @Test func entitiesInTheAddressOfALink() {
        let markup = ReviewFiveTests.markup(TestSupport.newPDF())
        #expect(markup.paragraph("[a](https://x/?a=1&amp;b=2)").lines[0].getURIAction() == "https://x/?a=1&b=2")
    }

    @Test func aControlCharacterOfAnEntityIsASpace() {
        let markup = ReviewFiveTests.markup(TestSupport.newPDF())
        #expect(markup.paragraph("a&#10;b&#9;c&#x7;d").lines[0].text == "a b c d")
    }

    @Test func theTrendIsOfThePointsDrawn() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let chart = SimpleScatterChart(font, font).setTitle("T")
                .add(1, 1).add(2, 2).add(3, 3).add(4, -1000).setTrendLine(true).setYAxisRange(0, 10)
        #expect(chart.getAltDescription() == "T: 3 points; the trend rises; 1 point outside the range.")
    }

    @Test func theIgnorableCharactersAtTheEdgesOfTheGapsSkipped() {
        let chars = [0x034F, 0x0350, 0x061B, 0x061C, 0x3164, 0x3165, 0xFDFF, 0xFE00]
        let ignorable = [true, false, false, true, true, false, false, true]
        for (i, ch) in chars.enumerated() {
            #expect(Font.isIgnorable(ch) == ignorable[i], "\(String(ch, radix: 16))")
        }
    }

    @Test func glyphsAddedTogetherAsOneByOne() {
        let gids = [3, -1, 70000, 200, 3, 65535, 0]
        var together = Subset.GlyphSet(10)
        together.add(gids)
        var oneByOne = Subset.GlyphSet(10)
        for gid in gids {
            oneByOne.add(gid)
        }
        #expect(together.bools() == oneByOne.bools())
        #expect(together.bools().count == 0x10000)
        var none = Subset.GlyphSet(10)
        none.add([-5, 70000])
        #expect(none.bools() == [Bool](repeating: false, count: 64))
    }
}
