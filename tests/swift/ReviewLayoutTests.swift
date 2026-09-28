/**
 * ReviewLayoutTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The tests of the text, table and Markdown layout, from a review of them.
@Suite struct ReviewLayoutTests {
    private let header = ["Name", "City", "Total"]

    private func rows(_ font: Font, _ count: Int, _ columns: Int) -> [[Cell]] {
        var data = [[Cell]]()
        for r in 0..<count {
            var row = [Cell]()
            for c in 0..<columns {
                row.append(Cell(font, columns == 1 ? "row\(r)" : "r\(r)c\(c)"))
            }
            data.append(row)
        }
        return data
    }

    // The first count rows like those of the other tests of BigTable.
    private func bigTableRows(_ count: Int) -> [[String]] {
        return (0..<count).map { ["n\($0)", "City, \($0)", "\($0).5"] }
    }

    private func count(_ str: String, _ text: String) -> Int {
        return str.components(separatedBy: text).count - 1
    }

    // The lowest baseline of the text the page draws, in the coordinates of
    // the PDF, which grow upwards from the bottom of the page.
    private func lowestBaseline(_ page: Page) -> Float {
        var lowest = page.height
        for line in TestSupport.content(page).components(separatedBy: "\n") where line.hasSuffix(" Td") {
            let parts = line.components(separatedBy: " ")
            if parts.count == 3, let y = Float(parts[1]), y < lowest {
                lowest = y
            }
        }
        return lowest
    }

    private func seconds(_ body: () throws -> Void) rethrows -> Double {
        let start = Date()
        try body()
        return Date().timeIntervalSince(start)
    }

    private func temporaryFile(_ name: String, _ text: String) throws -> String {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("\(UUID().uuidString)-\(name)")
        try Data(text.utf8).write(to: url)
        return url.path
    }

    private func errorOf(_ body: () throws -> Any) -> String {
        do {
            _ = try body()
            return ""
        } catch {
            return TestSupport.message(error)
        }
    }

    // Reads the first record of the text, as the data file readers do.
    private func firstRecord(_ text: String) throws -> [String] {
        let lines = text.components(separatedBy: "\n")
        var i = 1
        return try Util.readRecord(lines[0], ",") {
            guard i < lines.count else {
                return nil
            }
            i += 1
            return lines[i - 1]
        }
    }

    @Test func textFrameBreaksALongWordInLinearTime() {
        // The rest of the word was measured whole for every row it was broken
        // into: 100,000 characters took 75 seconds.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let frame = TextFrame(font, [String(repeating: "a", count: 200000)])
        frame.setLocation(50, 50).setWidth(400)
        let took = seconds { frame.drawOn(Page(pdf, Letter.PORTRAIT, false)) }
        #expect(took < 10)
        #expect(!frame.hasMoreText(), "the word is not all drawn")
    }

    @Test func markdownQuotesNestedDeeplyKeepTheTextWide() throws {
        // Quotes nested so deeply that the text was narrower than nothing
        // drew one character on each row.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        let took = try seconds {
            try Markdown(font, font, font, font, font).drawOn(
                    pdf, String(repeating: ">", count: 20000), &pages, Letter.PORTRAIT)
        }
        #expect(took < 10)
        #expect(pages.count <= 40, "\(pages.count) pages for 20,000 characters")
    }

    @Test func aRunningSumIsAddedUpInLinearTime() {
        // The sums were added up again from the first row for every page,
        // with the numbers read again: 40,000 rows took 33 seconds.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let count = 40000
        let data = rows(font, count, 2)
        for r in 1..<count {
            data[r][1].setText("1,234.50")
        }
        let table = Table().setTableData(data, 1)
        table.setNumberOfFooterRows(1)
        table.setRunningSum(count - 1, 1, 2)
        table.setLocation(20, 20)
        var pages = [Page]()
        let took = seconds { table.drawOn(pdf, &pages, Letter.PORTRAIT) }
        #expect(took < 20)
        // The footer row is the last row, which has no number of its own.
        #expect(TestSupport.content(pages.last!).contains(TestSupport.hex("49,377,531.00")),
                "the running sum of the last page is not the total")
    }

    @Test func aSumReadsANumberAsRightAlignNumbersDoes() {
        // A number with a line break or a control character after it is a
        // number to isNumber, which trims them, and trapped in numberOf.
        for text in ["100\n", "100\u{1}", " 100 ", "(100)"] {
            let want: Int64 = text.contains("(") ? -100 : 100
            #expect(Table.numberOf(text, 0) == want, "\(text.debugDescription)")
        }
        // Input that isNumber takes and that is not all digits is 0, not a trap.
        #expect(Table.numberOf(". 5", 0) != nil)
    }

    @Test func aRowSpanCountsTheRowsOfTheTable() throws {
        // The span of a cell over a row that wraps to four lines was written
        // as the five rows of the drawing, and not as the two rows of the table.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let data = [
            [Cell(font, "H1"), Cell(font, "H2")],
            [Cell(font, "span").setRowSpan(2), Cell(font, "a note long enough to wrap to four lines")],
            [Cell(font, ""), Cell(font, "x")],
            [Cell(font, "y"), Cell(font, "z")],
        ]
        let table = Table().setTableData(data, 1)
        table.setColumnWidth(0, 60).setColumnWidth(1, 60)
        table.setLocation(20, 20)
        table.drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/RowSpan ") == 1)
        #expect(count(raw, "/RowSpan 2") == 1)
        #expect(count(raw, "/S /TR\n") == 4)
    }

    @Test func aRowThatDoesNotFitUnderAHeadingGoesToTheNextPage() {
        // The first row of the first page was drawn where it was, past the
        // bottom of the page, when it did not fit under what was above the table.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let first = Page(pdf, Letter.PORTRAIT, false)
        let data = rows(font, 5, 1)
        data[1][0].setText("one two three four five six seven eight nine ten eleven twelve")
        let table = Table().setTableData(data, 1)
        table.setColumnWidth(0, 60)
        table.setLocation(20, 20)
        table.setBottomMargin(20)
        table.setFirstPageTopMargin(720)
        var pages = [first]
        table.drawOn(pdf, first, &pages, Letter.PORTRAIT)
        #expect(!TestSupport.content(first).contains(TestSupport.hex("one")), "the row is drawn on the first page")
        #expect(pages.count == 2)
        #expect(pages.count == 2 && TestSupport.content(pages[1]).contains(TestSupport.hex("twelve")))
        for (i, page) in pages.enumerated() {
            #expect(lowestBaseline(page) >= 20, "page \(i) draws text under the bottom margin")
        }
    }

    @Test func aRowSpanTallerThanAPageIsCutBetweenItsRows() {
        // A cell that spans rows taller than a page drew them all on one page,
        // past its bottom.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let data = rows(font, 80, 2)
        data[3][0].setRowSpan(70)
        let table = Table().setTableData(data, 1)
        table.setLocation(20, 20)
        table.setBottomMargin(20)
        var pages = [Page]()
        table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 2)
        var drawn = 0
        var spans = 0
        for (i, page) in pages.enumerated() {
            #expect(lowestBaseline(page) >= 20, "page \(i) draws text under the bottom margin")
            let content = TestSupport.content(page)
            for r in 1..<80 {
                drawn += count(content, "<" + TestSupport.hex("r\(r)c1") + ">")
            }
            // The spanning cell draws its text once, and its box on each page.
            spans += count(content, "<" + TestSupport.hex("r3c0") + ">")
        }
        #expect(drawn == 79)
        #expect(spans == 1)
    }

    @Test func aTableWithNoRowsLeftEndsWhereItStarts() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let table = Table().setTableData(rows(font, 3, 2), 1)
        table.setLocation(20, 30)
        var pages = [Page]()
        table.drawOn(pdf, &pages, Letter.PORTRAIT)
        let xy = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count == 1)
        TestSupport.expectXY(20 + table.getWidth(), 30, xy)
    }

    @Test func aColumnSpanIsWithinTheRow() {
        // A column span of 0 hung the drawing, and one past the end of the row
        // ran past the cells of the row.
        for colspan in [0, -3, 5] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            let data = rows(font, 3, 3)
            for row in data {
                for cell in row {
                    cell.setBorders(true)
                }
            }
            data[1][1].setColSpan(colspan)
            let table = Table().setTableData(data, 1)
            table.setLocation(20, 20)
            table.drawOn(Page(pdf, Letter.PORTRAIT))
            if colspan < 1 {
                #expect(data[1][1].getColSpan() == 1)
            }
        }
    }

    @Test func aListItemGoesOnInTheNextFrameWithoutItsLabel() throws {
        // The label of an item was drawn again, as a new item, in every frame
        // the item went on into.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let item = Paragraph().add(TextLine(font, String(repeating: "word ", count: 400)))
        item.setListLabel(TextLine(font, "LABEL"), 15)
        let frame = TextFrame([item])
        frame.setLocation(50, 50).setWidth(200).setHeight(200)
        var pages = [Page]()
        frame.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 2)
        var labels = 0
        for page in pages {
            labels += count(TestSupport.content(page), TestSupport.hex("LABEL"))
        }
        #expect(labels == 1)
        memory.pdf.addPages(pages)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /Lbl\n") == 1)
        #expect(count(raw, "/S /LBody\n") == pages.count)
    }

    @Test func aHeadingThatGoesOnInTheNextFrameIsOneBookmark() {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let heading = Paragraph().add(TextLine(font, String(repeating: "heading ", count: 200)))
        heading.setStructureType(StructElem.H1)
        let frame = TextFrame([heading])
        frame.setLocation(50, 50).setWidth(200).setHeight(100)
        var pages = [Page]()
        frame.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 2)
        #expect(memory.pdf.headings.count == 1)
    }

    @Test func aTextColumnDrawsTheLabelOfAListItem() throws {
        // The label that Paragraph.setListLabel sets was left out by TextColumn.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let column = TextColumn()
        column.setLocation(50, 50)
        column.setWidth(300)
        for text in ["first item", "second item"] {
            let item = Paragraph().add(TextLine(font, text))
            item.setListLabel(TextLine(font, "LABEL"), 15)
            column.addParagraph(item)
        }
        column.addParagraph(Paragraph().add(TextLine(font, "after the list")))
        column.drawOn(page)
        let content = TestSupport.content(page)
        #expect(count(content, TestSupport.hex("LABEL")) == 2)
        let label = TestSupport.positionOf(content, "LABEL")
        let text = TestSupport.positionOf(content, "first")
        TestSupport.expectNear(35, label[0])
        TestSupport.expectNear(text[1], label[1])
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /L\n") == 1)
        #expect(count(raw, "/S /LI\n") == 2)
        #expect(count(raw, "/S /Lbl\n") == 2)
        #expect(count(raw, "/S /LBody\n") == 2)
        #expect(count(raw, "/S /P\n") == 3)
    }

    @Test func aTextFrameInTheBottomHalfOfThePageNeedsAHeight() {
        // The height the frame took on each page was less than nothing, so it
        // drew all of its text on the first page, past the bottom.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let frame = TextFrame(font, [String(repeating: "word ", count: 1000)])
        frame.setLocation(50, 500).setWidth(200)
        var pages = [Page]()
        frame.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.isEmpty)
        #expect(pdf.error == "The text frame has no height and is in the bottom half of the page: "
                + "set its height, or put it higher on the page.")
    }

    @Test func aRecordOfManyLinesIsReadInLinearTime() throws {
        // Every line closes a quoted field and opens the next, and the record
        // was split again at each line.
        var text = "\"a"
        for _ in 0..<5000 {
            text += "\n" + String(repeating: "x", count: 1000) + "\",\""
        }
        text += "\nend\""
        var fields = [String]()
        let took = try seconds { fields = try firstRecord(text) }
        #expect(took < 10)
        #expect(fields.count == 5001)
        #expect(fields.first == "a " + String(repeating: "x", count: 1000))
        #expect(fields.last == " end")
        // A quote after the delimiter opens a field, and one inside a field
        // that does not start with one is text.
        #expect(try firstRecord("\"a\nb\",c\"d,\"e\nf\"") == ["a b", "c\"d", "e f"])
    }

    @Test func aBigTableThrowsTheErrorOfAQuoteThatIsNotClosed() throws {
        // The quote that is not closed stopped the program, and the table throws.
        let path = try temporaryFile("open.csv", "A,B\n1,\"2\n3,4\n")
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        #expect(errorOf { try BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2).setTableData(path, ",") }
                == "A quoted field is not closed by the end of the data file: 1,\"2\n3,4")
    }

    @Test func theLinesOfADataFileEndAtACarriageReturnToo() throws {
        // A carriage return alone ended a line in Java and C#, and not in Go
        // and Swift, which kept one at the end of the last line.
        let path = try temporaryFile("cr.csv", "A,B\r1,2\r\n3,4\n5,6\r")
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let table = try Table(font, font, path)
        #expect(table.getColumn(0).count == 4)
        #expect(table.getCellAt(3, 0).getText()! + " " + table.getCellAt(3, 1).getText()! == "5 6")
        let lines = try DataFileLines(path)
        var read = [String]()
        while let line = lines.next() {
            read.append(line)
        }
        #expect(read == ["A,B", "1,2", "3,4", "5,6"])
    }

    @Test func aBigTableThatDoesNotFitOnItsFirstPageStartsOnTheNext() throws {
        // The header and the first row were drawn wherever the first page had
        // them start, even under its bottom margin.
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let first = Page(memory.pdf, Letter.PORTRAIT)
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(header, bigTableRows(5))
        table.setFirstPage(first, first.height - 15)
        table.setLocation(10, 10)
        try table.complete()
        let pages = table.getPages()
        #expect(pages.count == 1)
        #expect(pages.first !== first)
        #expect(!TestSupport.content(first).contains(TestSupport.hex(header[0])), "the header is drawn on the first page")
        #expect(TestSupport.content(pages[0]).contains(TestSupport.hex("Page 1 of 1")))
    }

    @Test func aBigTableCountsItsFirstPageAtItsOwnHeight() throws {
        // The pages were counted as if the first were of the size of the next.
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let first = Page(memory.pdf, Letter.LANDSCAPE)
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(header, bigTableRows(200))
        table.setFirstPage(first, 100)
        table.setLocation(10, 10)
        try table.complete()
        let pages = table.getPages()
        let want = "Page \(pages.count) of \(pages.count)"
        #expect(TestSupport.content(pages.last!).contains(TestSupport.hex(want)), "the last page is not \(want)")
    }

    @Test func aTextBlockWithALeadingOfZeroDrawsItsLines() {
        // The lines that fit were the height divided by a leading of 0, which
        // trapped in Int(_:).
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let block = TextBlock(font, "one two three four five six seven eight nine ten")
        block.setWidth(40).setHeight(30).setLineSpacing(0)
        #expect(block.layout().textLines.count >= 5)
        let zero: Float = 0
        let values: [Int] = [Util.saturatingInt(1 / zero), Util.saturatingInt(zero / zero),
                Util.saturatingInt(-1 / zero), Util.saturatingInt(3.9)]
        #expect(values == [2147483647, 0, -2147483648, 3])
    }

    @Test func markdownCodeInAFontOfSizeZeroIsDrawn() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let code = try Font(pdf, CoreFont.COURIER).setSize(0)
        var pages = [Page]()
        try Markdown(font, font, font, font, code).drawOn(pdf, "```\none\ntwo\n```", &pages, Letter.PORTRAIT)
        #expect(pages.count == 1)
    }

    @Test func markdownReadsTheSourceOfAnImageTheSameWayInEveryPort() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let images = TestSupport.path("images")
        let markdown = Markdown(font, font, font, font, font).setImageDirectory(images)
        let sources: [String: Bool] = [
            "linux-logo.png": true,
            "./linux-logo.png": true,
            ".//linux-logo.png": true,
            "linux-logo.png/": false,
            "linux-logo.png/.": false,
            "../images/linux-logo.png": false,
            "x/../linux-logo.png": false,
            "": false,
            ".": false,
            "\u{301}/../linux-logo.png": false,
            "/\u{301}linux-logo.png": false,
            "../\u{301}images/linux-logo.png": false,
            "c:linux-logo.png": false,
            "linux-logo.png\\..\\x.png": false,
            "%2E%2E/images/linux-logo.p": false,
        ]
        for (source, read) in sources {
            #expect((markdown.imagePath(source) != nil) == read, "\(source.debugDescription)")
        }
        // An empty directory is the working directory, which swift test makes
        // the root of the package; the tests run at the same time, so it is
        // not changed here.
        if URL(fileURLWithPath: FileManager.default.currentDirectoryPath).standardizedFileURL.path
                == TestSupport.root.standardizedFileURL.path {
            #expect(Markdown(font, font, font, font, font).setImageDirectory("")
                    .imagePath("images/linux-logo.png") == "images/linux-logo.png")
        }
    }
}
