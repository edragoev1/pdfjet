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
        return try draw(text, imageDirectory) { _ in }
    }

    // The Markdown drawn after the change, as an option set on it.
    static func draw(_ text: String, _ imageDirectory: String?, _ change: (Markdown) -> Void) throws -> Drawn {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        // The tests find the text of the core fonts by its encoding.
        memory.pdf.coreFontsInCompliance = true
        memory.pdf.setTitle("Test")
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
        change(markdown)
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

    @Test func tasksAreLabeledByTheirBoxes() throws {
        let drawn = try MarkdownTests.draw("- [x] done\n- [ ] open\n- plain", nil)
        #expect(MarkdownTests.count(drawn.pdf, "L") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "LI") == 3)
        #expect(MarkdownTests.count(drawn.pdf, "Lbl") == 3)
        #expect(MarkdownTests.count(drawn.pdf, "LBody") == 3)
        // The boxes are read as ☑ and ☐, in UTF-16BE, their actual text and
        // their description, the plain item by its bullet
        for actualText in ["<feff2611>", "<feff2610>"] {
            #expect(drawn.pdf.components(separatedBy: "/ActualText " + actualText).count - 1 == 1)
            #expect(drawn.pdf.components(separatedBy: "/Alt " + actualText).count - 1 == 1)
        }
    }

    // Color.transparent leaves the color of a done task's box green: it filled
    // the box with the color last set, black (review of 15 October 2026).
    @Test func aTransparentCheckColorKeepsTheGreen() throws {
        let font = try TestSupport.plexSans(MemoryPDF(Compliance.PDF_1_7).pdf)
        let markdown = Markdown(font, font, font, font, font).setCheckColor(Color.transparent)
        let checkColor = Mirror(reflecting: markdown).children.first { $0.label == "checkColor" }?.value as? Int32
        #expect(checkColor == 0x1A7F37, "the check color is \(String(checkColor ?? 0, radix: 16))")
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

    // The right-aligned header of the last column is drawn at the right end of
    // the table: near the text's right margin, 72 pt from the edge of the
    // page, only when the tables are as wide as the text.
    // A cell that starts with a check mark is green, as a table of features
    // marks what each has; a header and other cells are not.
    @Test func aTableCellThatStartsWithACheckMarkIsInTheCheckColor() throws {
        let green = fillOf(0x1A7F37)
        let red = fillOf(0xFF0000)
        let table = "| Feature | \u{2713} |\n|---|---|\n| Tables | \u{2713} |\n| SVG | \u{2713} with a module |\n| Charts | no |"
        var content = try MarkdownTests.draw(table, nil).contents[0]
        #expect(content.components(separatedBy: green).count - 1 == 2, "the two cells that start with a check mark are green")
        #expect(!(try MarkdownTests.draw("| Feature | A |\n|---|---|\n| Tables | yes |", nil).contents[0].contains(green)))
        // The color is the check color, which colors the task boxes too.
        content = try MarkdownTests.draw(table, nil) { $0.setCheckColor(0xFF0000) }.contents[0]
        #expect(content.contains(red) && !content.contains(green))
    }

    // How a fill color is written: the content of a page with only that color set.
    private func fillOf(_ color: Int32) -> String {
        let page = Page(MemoryPDF(Compliance.PDF_1_7).pdf, Letter.PORTRAIT, Page.DETACHED)
        page.setBrushColor(color)
        return TestSupport.content(page)
    }

    // The text of k short paragraphs, p0, p1 and so on, then the lead-in, then
    // a table of the rows, the cells of row r named r1, r2 and so on: words
    // of no letters that the core fonts kern, so that each is drawn whole.
    private func fillerAndTable(_ k: Int, _ leadIn: String?, _ rows: Int) -> String {
        var text = ""
        for i in 0..<k {
            text += "p\(i)\n\n"
        }
        if let leadIn {
            text += leadIn + "\n\n"
        }
        text += "| N | V |\n|---|---|\n"
        for r in stride(from: 1, through: rows, by: 1) {     // No rows for 0
            text += "| r\(r) | v\(r) |\n"
        }
        return text
    }

    // The pages, counted from 0, that draw the text.
    private func pagesOf(_ drawn: Drawn, _ text: String) -> [Int] {
        return drawn.contents.indices.filter { drawn.contents[$0].contains(TestSupport.hex(text)) }
    }

    // Whatever the text before it, the table of eight rows is on one page, its
    // first row and its last; at some length of the text it is moved to the
    // next page, which shows the rule at work.
    @Test func aTableThatFitsOnAPageIsNotSplit() throws {
        var moved = false
        for k in 0...60 {
            let drawn = try MarkdownTests.draw(fillerAndTable(k, nil, 8), nil)
            let first = pagesOf(drawn, "r1")
            #expect(first == pagesOf(drawn, "r8"), "\(k) paragraphs: the table is split")
            let lastFiller = (k > 0) ? pagesOf(drawn, "p\(k - 1)")[0] : 0
            moved = moved || first[0] > lastFiller
        }
        #expect(moved, "the table never moved to the next page")
    }

    @Test func aParagraphThatEndsInAColonStaysWithItsTable() throws {
        var moved = false
        for k in 0...60 {
            let drawn = try MarkdownTests.draw(fillerAndTable(k, "555:", 8), nil)
            let leadIn = pagesOf(drawn, "555:")
            #expect(leadIn == pagesOf(drawn, "r1") && leadIn == pagesOf(drawn, "r8"),
                    "\(k) paragraphs: the lead-in or the table is apart")
            let lastFiller = (k > 0) ? pagesOf(drawn, "p\(k - 1)")[0] : 0
            moved = moved || leadIn[0] > lastFiller
        }
        #expect(moved, "the lead-in never moved with its table")
    }

    // A heading over the lead-in goes with it and its table: the two went to
    // the next page and left the heading at the bottom of this one (the
    // report of the fair test, 26 October 2026).
    @Test func aHeadingStaysWithALeadInAndItsTable() throws {
        var moved = false
        for k in 0...60 {
            let drawn = try MarkdownTests.draw(fillerAndTable(k, "## H7\n\n555:", 8), nil)
            let heading = pagesOf(drawn, "H7")
            #expect(heading == pagesOf(drawn, "555:"), "\(k) paragraphs: the heading is apart from its lead-in")
            #expect(heading == pagesOf(drawn, "r8"), "\(k) paragraphs: the heading is apart from its table")
            let lastFiller = (k > 0) ? pagesOf(drawn, "p\(k - 1)")[0] : 0
            moved = moved || heading[0] > lastFiller
        }
        #expect(moved, "the heading never moved with its table")
    }

    @Test func aTableLongerThanAPageStillStartsWhereItIs() throws {
        let drawn = try MarkdownTests.draw(fillerAndTable(3, nil, 120), nil)
        #expect(pagesOf(drawn, "p2") == [0])
        #expect(pagesOf(drawn, "r1")[0] == 0, "the long table starts after the text")
        #expect(pagesOf(drawn, "r120")[0] > 0, "the long table goes on to the next pages")
    }

    @Test func aTableIsAsWideAsTheTextWhenSet() throws {
        let text = "| N | V |\n|---|---:|\n| a | 15 |"
        let narrow = TestSupport.positionOf(try MarkdownTests.draw(text, nil).contents[0], "V")[0]
        let wide = TestSupport.positionOf(try MarkdownTests.draw(text, nil) { $0.setFullWidthTables(true) }.contents[0], "V")[0]
        #expect(narrow < 200, "a table is as wide as its text: \(narrow)")
        #expect(wide > 612 - 72 - 30, "a table is as wide as the text: \(wide)")
    }

    // The font selected last before the text is drawn: the Tf of its line.
    private func fontBefore(_ content: String, _ text: String) throws -> String? {
        let range = try #require(content.range(of: "<" + TestSupport.hex(text) + ">"), "\(text) is not drawn")
        let before = String(content[..<range.lowerBound])
        let regex = try NSRegularExpression(pattern: "/F\\d+ [\\d.]+ Tf")
        let matches = regex.matches(in: before, range: NSRange(before.startIndex..., in: before))
        guard let last = matches.last, let r = Range(last.range, in: before) else {
            return nil
        }
        return String(before[r])
    }

    // A whole cell in bold is drawn in the bold font, as the header is, so that
    // a table can mark a value; a cell of mixed emphasis stays regular.
    @Test func aTableCellInOneEmphasisIsDrawnInItsFont() throws {
        let drawn = try MarkdownTests.draw("| N | V |\n|---|---:|\n| a | 15 |\n| b | **25** |\n| c | **7** 8 |", nil)
        let content = drawn.contents[0]
        let header = try fontBefore(content, "V")
        #expect(try fontBefore(content, "25") == header, "the bold cell is in the bold font")
        let plain = try fontBefore(content, "15")
        #expect(plain != header)
        #expect(try fontBefore(content, "7 8") == plain, "a cell of mixed emphasis is regular")
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
        let text = "![Circles](circles.png)"
        // With no directory, the image's text is drawn instead.
        var drawn = try MarkdownTests.draw(text, nil)
        #expect(MarkdownTests.count(drawn.pdf, "Figure") == 0)
        #expect(drawn.text.contains(TestSupport.hex("Circles")))
        // From the directory, the image is a figure.
        let images = TestSupport.path("resources/images")
        drawn = try MarkdownTests.draw(text, images)
        #expect(MarkdownTests.count(drawn.pdf, "Figure") == 1)
        let alt = MarkdownTests.groups("/Alt <([0-9A-Fa-f]+)>", drawn.pdf)
        #expect(!alt.isEmpty && TestSupport.utf16Hex(alt[0]) == "Circles",
                "the text of the image is its description")
        // Not above it, not an absolute path, not a URL.
        for source in ["../images/circles.png", "/etc/passwd",
                TestSupport.path("resources/images/circles.png"), "https://pdfjet.com/logo.png",
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
        var drawn = Drawn()
        let elapsed = try shortestTime(.seconds(5)) {
            drawn = try MarkdownTests.draw("```\n" + String(decoding: line, as: UTF16.self) + "\n```", nil)
        }
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

    // A table of more than 64 columns, and tables of more than 100,000 cells,
    // make the text an error: the first was drawn as a paragraph of pipes, and a
    // header of 64 columns over rows of "|" made 64 cells of a row of 2 bytes,
    // 895 MB in Go from 64 KB (review of 12 October 2026, R33).
    @Test func aTableTooWideOrTooLargeIsAnError() throws {
        func table(_ columns: Int, _ rows: Int) -> String {
            String(repeating: "| a ", count: columns) + "|\n" +
                String(repeating: "| - ", count: columns) + "|\n" + String(repeating: "|\n", count: rows)
        }
        func draw(_ text: String) throws {
            let pdf = MemoryPDF(Compliance.PDF_1_7).pdf
            let font = try Font(pdf, CoreFont.HELVETICA)
            var pages = [Page]()
            try Markdown(font, font, font, font, font).drawOn(pdf, text, &pages, Letter.PORTRAIT)
        }
        try draw(table(64, 2))
        #expect(throws: PDFjetError.self) { try draw(table(65, 2)) }
        #expect(throws: PDFjetError.self) { try draw(table(64, 1600)) }
        // A line of pipes that is no table, with no row of delimiters, is text
        try draw(String(repeating: "| a ", count: 70) + "|\n")
    }

    // A heading that starts a list item or a quote keeps a line of the text
    // after it on its page, as any other heading does: it was drawn into the
    // bottom margin, with nothing after it (review of 12 October 2026, F16).
    @Test func aHeadingThatStartsAnItemStaysAboveTheMargin() throws {
        for container in ["- ", "> "] {
            for lines in 15..<35 {
                let text = String(repeating: "Line\n\n", count: lines) + container + "# HEADING\n" + container
                    + "\n" + container + "after\n"
                for content in try MarkdownTests.draw(text, nil).contents where content.contains(TestSupport.hex("HEADING")) {
                    let y = TestSupport.positionOf(content, "HEADING")[1]
                    #expect(y >= 72 + 12, "\(container)\(lines): the heading is at \(y)")
                    #expect(content.contains(TestSupport.hex("after")), "\(container)\(lines): nothing after it")
                }
            }
        }
    }

    // The review of the Markdown of 26 October 2026. The first block of a
    // quote or a list item keeps the room it needs, as any other block: a
    // short table there is not split, and a lead-in there keeps its table. A
    // short table after a lead-in stays with it, and a heading with a table
    // right under it.
    @Test func blocksKeepTheRoomTheyNeed() throws {
        for k in 0...60 {
            var filler = fillerAndTable(k, nil, 0)
            filler = String(filler[..<filler.range(of: "| N |")!.lowerBound])
            var drawn = try MarkdownTests.draw(filler
                    + "> | N | V |\n> |---|---|\n> | r1 | v1 |\n> | r2 | v2 |\n> | r3 | v3 |\n", nil)
            #expect(pagesOf(drawn, "r1") == pagesOf(drawn, "r3"), "\(k) paragraphs: the table in the quote is split")
            drawn = try MarkdownTests.draw(filler + "- Sizes:\n\n  | N | V |\n  |---|---|\n  | r1 | v1 |\n", nil)
            #expect(pagesOf(drawn, "Sizes:") == pagesOf(drawn, "r1"), "\(k) paragraphs: the item's lead-in is apart")
            drawn = try MarkdownTests.draw(fillerAndTable(k, "555:", 1), nil)
            #expect(pagesOf(drawn, "555:") == pagesOf(drawn, "r1"), "\(k) paragraphs: the short table left its lead-in")
            drawn = try MarkdownTests.draw(filler + "## H7\n\n" + fillerAndTable(0, nil, 8), nil)
            #expect(pagesOf(drawn, "H7") == pagesOf(drawn, "r1"), "\(k) paragraphs: the heading is apart from its table")
        }
    }

    // A heading of two lines is kept whole on its page, with a line of the
    // text after it; it was kept by its first line.
    @Test func aHeadingOfTwoLinesIsKeptWhole() throws {
        var heading = "# Alpha"
        for _ in 0..<12 {
            heading += " word"
        }
        heading += " Omega\n\nThe text after it.\n"
        for k in 0...60 {
            let filler = fillerAndTable(k, nil, 0)
            let drawn = try MarkdownTests.draw(String(filler[..<filler.range(of: "| N |")!.lowerBound]) + heading, nil)
            #expect(pagesOf(drawn, "Alpha") == pagesOf(drawn, "Omega"), "\(k) paragraphs: the heading is split")
            #expect(pagesOf(drawn, "Omega") == pagesOf(drawn, "The text after it."), "\(k) paragraphs: it ends a page")
        }
    }

    // An empty heading takes no level: the "##" after it is the H1. A table
    // interrupts a paragraph, as in GitHub's Markdown. An empty block of code
    // is drawn as nothing.
    @Test func anEmptyHeadingATableAfterALineAndEmptyCode() throws {
        var drawn = try MarkdownTests.draw("#\n\n## Intro\n\nText.\n", nil)
        #expect(MarkdownTests.count(drawn.pdf, "H1") == 1)
        #expect(MarkdownTests.count(drawn.pdf, "H2") == 0)
        // Nor does one whose text draws nothing: it took the H1 that was not
        // drawn (the review of 28 October 2026)
        for empty in ["# <br>\n### Intro\n", "# &#32;\n### Intro\n"] {
            drawn = try MarkdownTests.draw(empty, nil)
            #expect(MarkdownTests.count(drawn.pdf, "H1") == 1, "\(empty.debugDescription)")
            #expect(MarkdownTests.count(drawn.pdf, "H2") == 0, "\(empty.debugDescription)")
        }
        drawn = try MarkdownTests.draw("Sizes:\n| a | b |\n|---|---|\n| 1 | 2 |\n", nil)
        #expect(MarkdownTests.count(drawn.pdf, "Table") == 1)
        drawn = try MarkdownTests.draw("Text.\n\n```\n```\n", nil)
        #expect(MarkdownTests.count(drawn.pdf, "Code") == 0)
    }

    // A table split inside a quote has the quote's bar on every page it is on:
    // the bar stopped where it started, and the pages between had none.
    @Test func aSplitTableInAQuoteHasItsBarOnEveryPage() throws {
        var text = "> | N | V |\n> |---|---|\n"
        for r in 1...160 {
            text += "> | r\(r) | v\(r) |\n"
        }
        let drawn = try MarkdownTests.draw(text, nil)
        #expect(drawn.contents.count >= 3, "\(drawn.contents.count) pages")
        for i in 0..<drawn.contents.count {
            #expect(drawn.contents[i].contains("0.69 0.71 0.75 RG"), "no bar on page \(i)")
        }
    }

    // Swift only: a check mark with a variation selector after it, "✓️", is
    // found by its first code point, as the other ports find it by their first
    // code unit; the first Character of the text holds both.
    @Test func aCheckMarkWithAVariationSelectorIsFound() throws {
        let green = fillOf(0x1A7F37)
        let content = try MarkdownTests.draw("| F | A |\n|---|---|\n| T | \u{2713}\u{FE0F} |", nil).contents[0]
        #expect(content.components(separatedBy: green).count - 1 == 1, "\(content)")
    }

    // Swift only: a lead-in is trimmed as Java's String.trim() trims, of the
    // characters up to U+0020: "555:" and an em space is no lead-in, and its
    // table goes to the next page alone where the lead-in ends a page.
    @Test func aLeadInIsTrimmedOfSpacesAndControlsOnly() throws {
        var apart = false
        for k in 0...60 {
            let drawn = try MarkdownTests.draw(fillerAndTable(k, "555:\u{2003}", 8), nil)
            apart = apart || pagesOf(drawn, "555:") != pagesOf(drawn, "r1")
        }
        #expect(apart, "the lead-in with an em space after its colon kept its table")
    }

    // Swift only: a lead-in ends in a colon by its last scalar, as in the
    // other ports: hasSuffix compared whole characters, and a colon that
    // U+0600, a prepended mark, joins into one was not seen (the review of 28
    // October 2026).
    @Test func aLeadInEndsInAColonByItsLastScalar() throws {
        for k in 0...60 {
            let drawn = try MarkdownTests.draw(fillerAndTable(k, "555 \u{0600}:", 8), nil)
            #expect(pagesOf(drawn, "555") == pagesOf(drawn, "r1"), "\(k) paragraphs: the lead-in is apart")
        }
    }

    // A run of headings is laid out in linear time: each heading measured the
    // headings after it to the end of the run, and 5,000 took four and a half
    // minutes in a debug build (the review of 28 October 2026).
    @Test func aRunOfHeadingsIsLaidOutInLinearTime() throws {
        let text = String(repeating: "# h\n", count: 5000)
        var drawn = Drawn()
        let elapsed = try shortestTime(.seconds(10)) {
            drawn = try MarkdownTests.draw(text, nil)
        }
        #expect(drawn.pages.count > 1)
        #expect(elapsed < .seconds(10), "\(elapsed)")
    }

    // Only a regular file of the image directory is read: a FIFO there was
    // read too, and blocked the reading until something wrote to it (the
    // review of 28 October 2026). A link to an image is followed.
    @Test func onlyARegularFileIsReadAsAnImage() throws {
        let directory = FileManager.default.temporaryDirectory
                .appendingPathComponent("pdfjet-markdown-" + UUID().uuidString).path
        try FileManager.default.createDirectory(atPath: directory, withIntermediateDirectories: true)
        defer {
            try? FileManager.default.removeItem(atPath: directory)
        }
        #expect(mkfifo(directory + "/fifo.png", 0o600) == 0)
        try FileManager.default.createSymbolicLink(atPath: directory + "/fifo-link.png",
                withDestinationPath: directory + "/fifo.png")
        try FileManager.default.createSymbolicLink(atPath: directory + "/circles.png",
                withDestinationPath: TestSupport.path("resources/images/circles.png"))
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let markdown = Markdown(font, font, font, font, font).setImageDirectory(directory)
        #expect(markdown.imagePath("fifo.png") == nil)
        #expect(markdown.imagePath("fifo-link.png") == nil)
        #expect(markdown.imagePath("circles.png") == directory + "/circles.png")
    }

    // A table split over pages whose header wraps tall starts the next page
    // when the header does not fit: drawn at the foot of the page, it ran off
    // it (the review of 30 October 2026, E3).
    @Test func aTallHeaderOfATableStaysOnThePage() throws {
        var text = ""
        for i in 0..<25 {
            text += "line \(i)\n\n"
        }
        text += String(repeating: "| " + String(repeating: "word ", count: 20) + " ", count: 6) + "|\n"
        text += String(repeating: "|---", count: 6) + "|\n"
        text += String(repeating: String(repeating: "| a ", count: 6) + "|\n", count: 80)
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font).drawOn(memory.pdf, text, &pages, Letter.PORTRAIT)
        memory.pdf.addPages(pages)
        try memory.pdf.complete()
    }
}
