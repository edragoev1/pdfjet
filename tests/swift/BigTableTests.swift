/**
 * BigTableTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct BigTableTests {
    private let header = ["Name", "City", "Total"]

    /// A hundred rows, with a delimiter inside a quoted field, and a short row
    /// near the end, which is skipped.
    private func rows() -> [[String]] {
        var rows = [[String]]()
        for i in 0..<100 {
            if i == 90 {
                rows.append(["short"])
            }
            rows.append(["n\(i)", "City, \(i)", "\(i).5"])
        }
        return rows
    }

    private func draw(_ table: BigTable) throws -> [Page] {
        table.setLocation(10, 10)
        try table.complete()
        return table.getPages()
    }

    /// Writes the rows as a delimited file, with the fields that hold a comma quoted.
    private func writeCSV() throws -> URL {
        var csv = "Name,City,Total\n"
        for row in rows() {
            csv += row.count == 1 ? row[0] + "\n" : row[0] + ",\"" + row[1] + "\"," + row[2] + "\n"
        }
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("rows-\(UUID().uuidString).csv")
        try Data(csv.utf8).write(to: url)
        return url
    }

    @Test func rowsFromMemoryDrawWhatTheSameFileDraws() throws {
        let url = try writeCSV()
        defer { try? FileManager.default.removeItem(at: url) }

        let pdf1 = TestSupport.newPDF()
        let font1 = TestSupport.helvetica(pdf1)
        let filePages = try draw(
                BigTable(pdf1, font1, font1, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(url.path, ","))

        let pdf2 = TestSupport.newPDF()
        let font2 = TestSupport.helvetica(pdf2)
        let memoryPages = try draw(
                BigTable(pdf2, font2, font2, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(header, rows()))

        // A page releases its content when it is written, so the last page is
        // compared, which also shows the column widths of the first pass.
        #expect(filePages.count == 2)
        #expect(memoryPages.count == 2)
        let last = TestSupport.content(memoryPages[1])
        #expect(TestSupport.content(filePages[1]) == last)
        #expect(last.contains(TestSupport.hex("Name")) && last.contains(TestSupport.hex("n99")))
        #expect(!last.contains(TestSupport.hex("short")))
    }

    @Test func chosenColumnsAreDrawnInTheirOrder() throws {
        let url = try writeCSV()
        defer { try? FileManager.default.removeItem(at: url) }
        var rows = self.rows()
        rows.insert(["n95b", "x"], at: 95)      // No third field, so it is skipped

        let pdf1 = TestSupport.newPDF()
        let font1 = TestSupport.helvetica(pdf1)
        let filePages = try draw(
                BigTable(pdf1, font1, font1, Letter.PORTRAIT).setColumns([2, 0]).setTableData(url.path, ","))
        let pdf2 = TestSupport.newPDF()
        let font2 = TestSupport.helvetica(pdf2)
        let memoryPages = try draw(
                BigTable(pdf2, font2, font2, Letter.PORTRAIT).setColumns([2, 0]).setTableData(header, rows))

        #expect(memoryPages.count == 2)
        let last = TestSupport.content(memoryPages[1])
        #expect(TestSupport.content(filePages[1]) == last)
        let total = try #require(last.range(of: TestSupport.hex("Total")))
        let name = try #require(last.range(of: TestSupport.hex("Name")))
        #expect(total.lowerBound < name.lowerBound)
        #expect(last.contains(TestSupport.hex("n99")))
        #expect(!last.contains(TestSupport.hex("City")) && !last.contains(TestSupport.hex("n95b")))
    }

    @Test func aNegativeColumnIndexIsRefused() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        BigTable(pdf, font, font, Letter.PORTRAIT).setColumns([1, -1])
        #expect(pdf.error == "A column index cannot be negative.")
    }

    /// Draws a table of the first five rows on one page, after the change, and
    /// returns the content of the page.
    private func drawSmall(_ pdf: PDF, _ change: (BigTable) -> Void) throws -> String {
        let font = TestSupport.helvetica(pdf)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(header, Array(rows()[0..<5]))
        change(table)
        let pages = try draw(table)
        #expect(pages.count == 1)
        return TestSupport.content(pages[0])
    }

    // The lines between the columns and at the sides, one for each of the
    // three columns and one more, are left out, and the lines between the
    // rows are drawn as before.
    @Test func theVerticalLinesCanBeLeftOut() throws {
        let all = try drawSmall(TestSupport.newPDF()) { _ in }
        let rows = try drawSmall(TestSupport.newPDF()) { $0.setVerticalLines(false) }
        #expect(count(rows, "\nS\n") == count(all, "\nS\n") - 4)
        #expect(count(rows, "\nS\n") > 0, "the lines between the rows are drawn")
        #expect(try drawSmall(TestSupport.newPDF()) { $0.setVerticalLines(true) } == all)
    }

    @Test func theShadingAndTheBorderColorsCanBeChangedOrLeftOut() throws {
        let defaults = try drawSmall(TestSupport.newPDF()) { _ in }
        #expect(defaults.contains("0.94 0.94 0.94 rg\n") && defaults.contains("0.69 0.69 0.69 RG\n"))
        #expect(defaults.contains(" re\nf\n"))    // A shaded row is one rectangle

        let colored = try drawSmall(TestSupport.newPDF()) { table in
            table.setShadingColor(Int32(0xFF0000)).setBorderColor([Float(0), 0, 1])
        }
        #expect(colored.contains("1 0 0 rg\n") && colored.contains("0 0 1 RG\n"))
        #expect(!colored.contains("0.94 0.94 0.94 rg") && !colored.contains("0.69 0.69 0.69 RG"))

        let plain = try drawSmall(TestSupport.newPDF()) { table in
            table.setShadingColor(Color.transparent).setBorderColor(nil)
        }
        #expect(!plain.contains("\nf\n") && !plain.contains("\nS\n"))
        #expect(plain.contains(TestSupport.hex("n4")))
    }

    @Test func thePaddingCanBeSetAfterTheData() throws {
        let content = try drawSmall(TestSupport.newPDF()) { table in table.setPadding(10) }
        #expect(content.contains("BT\n20 "))     // The location is 10, 10
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        BigTable(pdf, font, font, Letter.PORTRAIT).setPadding(-1)
        #expect(pdf.error == "The padding cannot be negative.")
    }

    // The rows are taller by the padding above and below their text, so the
    // table takes more pages, and the footer counts them all.
    @Test func theVerticalPaddingMakesTallerRowsOnMorePages() throws {
        var counts = [Int]()
        for padding: Float in [0, 3] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            let table = BigTable(pdf, font, font, Letter.PORTRAIT)
                    .setNumberOfColumns(3).setTableData(header, rows())
                    .setVerticalPadding(padding).setFooter("{page}/{pages}", nil)
            let pages = try draw(table)
            counts.append(pages.count)
            let last = TestSupport.content(pages[pages.count - 1])
            #expect(last.contains(TestSupport.hex("\(pages.count)/\(pages.count)")), "the footer counts the pages")
            #expect(last.contains(TestSupport.hex("n99")))
        }
        #expect(counts[1] > counts[0], "\(counts[0]) pages, then \(counts[1])")
        // 0 is the table as it was.
        #expect(try drawSmall(TestSupport.newPDF()) { _ in } ==
                drawSmall(TestSupport.newPDF()) { table in table.setVerticalPadding(0) })
        for bad: Float in [-1, Float.nan, Float.infinity] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            BigTable(pdf, font, font, Letter.PORTRAIT).setVerticalPadding(bad)
            #expect(pdf.error == "The vertical padding must be a number, 0 or more.")
        }
    }

    // With no shading, the header alone is filled, in its color; with the
    // shading, the rows under it are shaded as before.
    @Test func theHeaderCanHaveAColorOfItsOwn() throws {
        let header = try drawSmall(TestSupport.newPDF()) { table in
            table.setShadingColor(Color.transparent).setHeaderColor(0xFF0000)
        }
        #expect(header.contains("1 0 0 rg\n"))
        #expect(header.components(separatedBy: "\nf\n").count - 1 == 1, "one filled rectangle, the header's")
        let shaded = try drawSmall(TestSupport.newPDF()) { table in table.setHeaderColor(0xFF0000) }
        let defaults = try drawSmall(TestSupport.newPDF()) { _ in }
        #expect(shaded.components(separatedBy: "\nf\n").count == defaults.components(separatedBy: "\nf\n").count)
        #expect(shaded.contains("1 0 0 rg\n") && shaded.contains("0.94 0.94 0.94 rg\n"))
        // Color.transparent is the table as it was.
        #expect(try defaults == drawSmall(TestSupport.newPDF()) { table in table.setHeaderColor(Color.transparent) })
    }

    @Test func theFooterCanBeSmallerThanItsFont() throws {
        let small = try drawSmall(TestSupport.newPDF()) { table in table.setFooterFontSize(7) }
        let defaults = try drawSmall(TestSupport.newPDF()) { _ in }
        #expect(small.contains(" 7 Tf\n") && !defaults.contains(" 7 Tf\n"))
        for bad: Float in [0, -8, Float.nan, Float.infinity] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            BigTable(pdf, font, font, Letter.PORTRAIT).setFooterFontSize(bad)
            #expect(pdf.error == "The size of the footer must be a number more than 0.")
        }
    }

    // The baseline of the footer is the offset above the bottom of the page,
    // y = 30 in the coordinates of the PDF.
    @Test func theFooterCanSitAtTheBottomMargin() throws {
        let raised = try drawSmall(TestSupport.newPDF()) { table in table.setFooterOffset(30) }
        let defaults = try drawSmall(TestSupport.newPDF()) { _ in }
        #expect(raised.contains(" 30 Td\n") && !defaults.contains(" 30 Td\n"))
        for bad: Float in [-1, Float.nan, Float.infinity] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            BigTable(pdf, font, font, Letter.PORTRAIT).setFooterOffset(bad)
            #expect(pdf.error == "The offset of the footer must be a number, 0 or more.")
        }
    }

    @Test func theFooterCanHaveItsOwnTextAndFontOrBeLeftOut() throws {
        let pdf = TestSupport.newPDF()
        let big = TestSupport.helvetica(pdf).setSize(20)
        let custom = try drawSmall(pdf) { table in table.setFooter("{page}/{pages}", big) }
        #expect(custom.contains(TestSupport.hex("1/1")) && custom.contains(" 20 Tf\n"))

        let none = try drawSmall(TestSupport.newPDF()) { table in table.setFooter(nil, nil) }
        let defaults = try drawSmall(TestSupport.newPDF()) { _ in }
        #expect(defaults.hasPrefix(none) && defaults.count > none.count)
    }

    @Test func lineBreaksInFieldsAreDrawnAsSpaces() throws {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("breaks-\(UUID().uuidString).csv")
        try Data("Name,City,Total\n\"n\n0\",\"City\r\n0\",1\nn1,City 1,2\n".utf8).write(to: url)
        defer { try? FileManager.default.removeItem(at: url) }

        let pdf1 = TestSupport.newPDF()
        let font1 = TestSupport.helvetica(pdf1)
        let fromFile = try draw(
                BigTable(pdf1, font1, font1, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(url.path, ","))

        let pdf2 = TestSupport.newPDF()
        let font2 = TestSupport.helvetica(pdf2)
        let rows = [["n\r0", "City\r\n0", "1"], ["n1", "City 1", "2"]]
        let fromMemory = try draw(
                BigTable(pdf2, font2, font2, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(header, rows))

        let pdf3 = TestSupport.newPDF()
        let font3 = TestSupport.helvetica(pdf3)
        let spaces = [["n 0", "City 0", "1"], ["n1", "City 1", "2"]]
        let withSpaces = try draw(
                BigTable(pdf3, font3, font3, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(header, spaces))

        #expect(fromFile.count == 1)
        #expect(TestSupport.content(fromFile[0]) == TestSupport.content(withSpaces[0]))
        #expect(TestSupport.content(fromMemory[0]) == TestSupport.content(withSpaces[0]))
    }

    @Test func theRowsAreReadTwice() throws {
        let rows = self.rows()
        final class Counter {
            var opened = 0
        }
        let counter = Counter()
        let source = AnySequence { () -> IndexingIterator<[[String]]> in
            counter.opened += 1
            return rows.makeIterator()
        }
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        _ = try draw(BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3).setTableData(header, source))
        #expect(counter.opened == 2)
    }

    @Test func aHeaderWithFewerFieldsThanColumnsIsRefused() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(4)
        table.setTableData(header, rows())
        #expect(pdf.error == "The header does not have a field for every column.")
        try table.complete()
        #expect(table.getPages().isEmpty)
    }
    private func count(_ str: String, _ text: String) -> Int {
        return str.components(separatedBy: text).count - 1
    }

    @Test func isTaggedAsATableInAPDFUADocument() throws {
        // The table is one Table element over all its pages: a TR for each
        // row, and a TH for each header field the first time the header is
        // drawn or a TD for each field of a row, each holding its own text.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.coreFontsInCompliance = true    // Draws in a core font
        memory.pdf.setTitle("Test")
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(3)
        table.setTableData(header, rows())
        let pages = try draw(table)
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        // The header that repeats on the second page is an artifact, and the
        // shading and the lines of every page are artifacts too.
        let content = TestSupport.content(pages[1])
        #expect(content.range(of: TestSupport.hex("Name"))!.lowerBound
                < content.range(of: "BDC\n")!.lowerBound)
        #expect(content.range(of: "/Artifact BMC\n")!.lowerBound
                < content.range(of: TestSupport.hex("Name"))!.lowerBound)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /Table\n") == 1)
        // The 100 rows of the data, and the header row of the first page.
        #expect(count(raw, "/S /TR\n") == 101)
        #expect(count(raw, "/S /TH\n") == 3)
        #expect(count(raw, "/S /TD\n") == 300)
        // A cell holds the text itself and has no paragraph under it.
        #expect(count(raw, "/S /P\n") == 0)
        #expect(count(raw, "/A <</O /Table /Scope /Column>>") == 3)
    }

    @Test func isNotTaggedInADocumentThatIsNotPDFUA() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(3)
        table.setTableData(header, rows())
        _ = try draw(table)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        for text in ["/S /Table\n", "/S /TR\n", "BDC\n", "/Artifact BMC\n"] {
            #expect(count(raw, text) == 0)
        }
    }

    // The x coordinate and the length of the text the page draws, in order.
    private func textPositions(_ page: Page) -> [(Float, Int)] {
        var list = [(Float, Int)]()
        let content = TestSupport.latin1(page.getContent())
        let pattern = "([-0-9.]+) [-0-9.]+ Td\\n(?:/F\\d+ [0-9.]+ Tf\\n)?\\[<([0-9A-Fa-f]*)>\\] TJ"
        let regex = try! NSRegularExpression(pattern: pattern)
        let range = NSRange(content.startIndex..., in: content)
        for match in regex.matches(in: content, range: range) {
            let x = String(content[Range(match.range(at: 1), in: content)!])
            let hex = String(content[Range(match.range(at: 2), in: content)!])
            list.append((Float(x)!, hex.count / 2))
        }
        return list
    }

    @Test func theAlignmentOfAColumnTheTableDoesNotHaveIsRefused() throws {
        // The alignment of a column was written into the array of them
        // whatever the column was, and there is none before setTableData.
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(2)
        table.setTextAlignment(0, Alignment.RIGHT)
        table.setTableData(["A", "B"], [["a", "b"]])
        table.setTextAlignment(1, Alignment.RIGHT)
        table.setTextAlignment(2, Alignment.RIGHT)
        do {
            try memory.pdf.complete()
            Issue.record("a table with no column 0 or 2 was completed")
        } catch {
            #expect(TestSupport.message(error) == "The PDF was not completed because of an "
                    + "earlier error: The table has no column 0: set the alignment of a "
                    + "column after setTableData.")
        }
    }

    @Test func aColumnIsAsWideAsTheFontOfEachRowDrawsIt() throws {
        // The columns were measured with the header font whatever font a row
        // was drawn with, so a body font wider than the header font ran over
        // the column on its right.
        let pdf = TestSupport.newPDF()
        let f1 = try Font(pdf, CoreFont.HELVETICA_BOLD).setSize(8)
        let f2 = try Font(pdf, CoreFont.HELVETICA).setSize(14)
        let table = BigTable(pdf, f1, f2, Letter.PORTRAIT)
        table.setNumberOfColumns(2)
        table.setTableData(["A", "B"], [["wwww", "xx"]])
        table.setLocation(50, 50)
        table.setFooter(nil, nil)
        try table.complete()

        let positions = textPositions(table.getPages()[0])
        #expect(positions.count == 4)
        // The header "A" ends before "B" starts, and so does the row under it.
        #expect(positions[0].0 + f1.stringWidth("A") <= positions[1].0,
                "the header runs into the next column")
        #expect(positions[2].0 + f2.stringWidth("wwww") <= positions[3].0,
                "the row runs into the next column")
    }

    // A table of one column of the given text, drawn on a letter page in a
    // font wide enough that the text may not fit it.
    // The columns take the percentages of the table's width that
    // setColumnWidthsInPercent gives them, the page less the margin on each
    // side, and a field wider than its column is cut to fit it; percentages that
    // are not one for each column, or a negative one, are refused.
    @Test func theColumnsCanBeGivenWidthsInPercent() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, CoreFont.HELVETICA)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(3)
        table.setColumnWidthsInPercent(20, 30, 50)
        table.setTableData(["A", "B", "C"], [["A field far wider than a fifth of the width of the table", "b", "c"]])
        table.setLocation(36, 20)
        table.setFooter(nil, nil)
        try table.complete()
        let page = table.getPages()[0]
        // The vertical lines, 540 points of the table at 20, 30 and 50 percent.
        let content = TestSupport.latin1(page.getContent())
        let regex = try NSRegularExpression(pattern: "([-0-9.]+) [-0-9.]+ m")
        let lines = regex.matches(in: content, range: NSRange(content.startIndex..., in: content)).map {
            Float(String(content[Range($0.range(at: 1), in: content)!]))!
        }
        for x: Float in [36, 144, 306, 576] {
            #expect(lines.contains(x), "no line at x=\(x): \(lines)")
        }
        let drawn = drawnText(page)
        #expect(drawn.count == 6 && drawn[3].hasSuffix(" ...") && drawn[4] == "b", "\(drawn)")
        #expect(font.stringWidth(drawn[3]) <= 108 - 4, "the cut field is \(font.stringWidth(drawn[3]))")

        let tooFew = TestSupport.newPDF()
        BigTable(tooFew, font, font, Letter.PORTRAIT).setNumberOfColumns(3).setColumnWidthsInPercent(50, 50)
        #expect(tooFew.error != nil, "two percentages for three columns are not refused")
        let negative = TestSupport.newPDF()
        BigTable(negative, font, font, Letter.PORTRAIT).setNumberOfColumns(3).setColumnWidthsInPercent(20, 30, -50)
        #expect(negative.error != nil, "a negative percentage is not refused")
    }

    private func tooWide(_ pdf: PDF, _ font: Font, _ text: String) throws -> BigTable {
        let table = BigTable(pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(1)
        table.setTableData(["Header"], [[text]])
        table.setLocation(0, 20)
        table.setFooter(nil, nil)
        try table.complete()
        return table
    }

    // The strings the page draws, in the order they are drawn. A core font
    // draws a character as the one byte of its code.
    private func drawnText(_ page: Page) -> [String] {
        let content = TestSupport.latin1(page.getContent())
        let regex = try! NSRegularExpression(pattern: "\\[<([0-9A-Fa-f]*)>\\] TJ")
        var list = [String]()
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let hex = (content as NSString).substring(with: m.range(at: 1))
            var text = ""
            var i = hex.startIndex
            while let next = hex.index(i, offsetBy: 2, limitedBy: hex.endIndex) {
                text.append(Character(UnicodeScalar(UInt8(hex[i..<next], radix: 16)!)))
                i = next
            }
            list.append(text)
        }
        return list
    }

    @Test func aTableTooWideForItsPageIsCutBackToItAndSaysSo() throws {
        // The columns of a table are as wide as their widest field, which can
        // come to more than the page holds; the last of them were drawn off
        // the right edge, where they are lost. They are cut back to the page
        // now, and a field that was cut ends in " ..." to say it was.
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, CoreFont.HELVETICA).setSize(24)
        let text = "A string that is far too long to fit across the width of a letter page"
        let table = try tooWide(pdf, font, text)
        let page = table.getPages()[0]
        let drawn = drawnText(page)
        #expect(drawn.count == 2)
        guard drawn.count == 2 else { return }
        #expect(drawn[1].hasSuffix(" ..."), "the field does not say it was cut: \(drawn[1])")
        #expect(text.hasPrefix(String(drawn[1].dropLast(4))),
                "the text drawn is not the start of the text: \(drawn[1])")
        // What is drawn fits the page, which the whole of the text does not.
        #expect(font.stringWidth(text) > Letter.PORTRAIT.getWidth(), "the text fits the page")
        #expect(font.stringWidth(drawn[1]) <= Letter.PORTRAIT.getWidth(),
                "the text drawn does not fit the page")
        // And the vertical lines of the table are on the page with it.
        let content = TestSupport.content(page)
        let regex = try! NSRegularExpression(pattern: "([-0-9.]+) [-0-9.]+ m")
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let x = Float((content as NSString).substring(with: m.range(at: 1)))!
            #expect(x <= Letter.PORTRAIT.getWidth(), "a line of the table is off the page at x=\(x)")
        }
    }

    @Test func aTableThatFitsItsPageIsNotCut() throws {
        // Nothing is measured or cut when the columns fit, and no field of a
        // table that fits ends in the mark of one that was cut.
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, CoreFont.HELVETICA).setSize(24)
        let text = "Short enough"
        let table = try tooWide(pdf, font, text)
        #expect(drawnText(table.getPages()[0]) == ["Header", text])
    }

    @Test func theCellOfACutFieldKeepsTheWholeOfItsText() throws {
        // A field the page was too narrow for is drawn cut, but a reader is
        // read the whole of it: the cell of the structure tree keeps it.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.coreFontsInCompliance = true    // Draws in a core font
        memory.pdf.setTitle("Test")
        _ = memory.pdf.setTitle("Title")
        let font = try Font(memory.pdf, CoreFont.HELVETICA).setSize(24)
        let text = "A string that is far too long to fit across the width of a letter page"
        let table = try tooWide(memory.pdf, font, text)
        // The content of a page is written out and let go by complete().
        let drawn = drawnText(table.getPages()[0])
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        // An Alt is written as the UTF-16 of the text, after a byte order mark.
        var alt = "feff"
        for unit in text.utf16 {
            alt += String(format: "%04x", unit)
        }
        #expect(raw.contains("/Alt <" + alt + ">"), "the cell does not keep the whole of the text")
        #expect(drawn.count == 2 && drawn[1].hasSuffix(" ...") && drawn[1] != text)
    }

    @Test func canStartOnAPageWithAHeadingAboveIt() throws {
        // The first page is the one given, with the table under what is on it;
        // the next pages are new, and the footer counts them all.
        let doc = MemoryPDF()
        let font = TestSupport.helvetica(doc.pdf)
        let first = Page(doc.pdf, Letter.PORTRAIT)
        TextLine(font, "Heading").setLocation(10, 20).drawOn(first)
        let table = BigTable(doc.pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(3)
                .setTableData(header, rows())
                .setFirstPage(first, 500)
        let pages = try draw(table)
        #expect(pages.count >= 2 && pages[0] === first)
        guard pages.count >= 2 else { return }
        let last = "Page \(pages.count) of \(pages.count)"
        #expect(TestSupport.content(pages[pages.count - 1]).contains(TestSupport.hex(last)),
                "the last page's footer is not \(last)")
        // The page given is a page of the PDF once, not a new one as well.
        try doc.pdf.complete()
        #expect(TestSupport.latin1(doc.bytes).contains("/Count \(pages.count)\n"),
                "the PDF does not have the \(pages.count) pages of the table")
        // Without a first page, the rows fit on fewer pages.
        let pdf = TestSupport.newPDF()
        let plain = try draw(BigTable(pdf, TestSupport.helvetica(pdf), TestSupport.helvetica(pdf), Letter.PORTRAIT)
                .setNumberOfColumns(3).setTableData(header, rows()))
        #expect(plain.count < pages.count,
                "\(pages.count) pages from a y of 500 on the first page, and \(plain.count) from the top")
    }

    // A table of three columns, which fill gives a field wider than its
    // column in the first and the third, and a fourth field in each row.
    private func edgeTable(_ pdf: PDF) throws -> BigTable {
        let font = try Font(pdf, CoreFont.HELVETICA)
        let table = BigTable(pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(3)
        table.setLocation(36, 20)
        table.setFooter(nil, nil)
        return table
    }

    @discardableResult
    private func fill(_ table: BigTable) -> BigTable {
        let wide = "A field far wider than a fifth of the width of the table"
        return table.setTableData(["A", "B", "C", "D"], [[wide, "b", wide, "d"]])
    }

    // The edge cases of setColumnWidthsInPercent: the columns set again after
    // the percentages, fewer stopped the program (review of 12 October 2026,
    // C4); a column too narrow for " ..." drew it over the next column; the
    // largest float as a share made the widths infinite; a margin of half the
    // page or more made widths of less than nothing (T5).
    @Test func theEdgeCasesOfColumnWidthsInPercent() throws {
        for columns in [2, 4] {
            let pdf = TestSupport.newPDF()
            let table = try edgeTable(pdf)
            table.setColumnWidthsInPercent(20, 30, 50)
            table.setNumberOfColumns(columns)
            fill(table)
            _ = try? table.complete()
            #expect(pdf.error == "The table has \(columns) columns, and 3 percentages: give one for each column.")
        }
        let narrow = try edgeTable(TestSupport.newPDF())
        narrow.setColumnWidthsInPercent(1, 98, 1)
        try fill(narrow).complete()
        #expect(!drawnText(narrow.getPages()[0]).contains(" ..."))
        let huge = try edgeTable(TestSupport.newPDF())
        huge.setColumnWidthsInPercent(.greatestFiniteMagnitude, 1, 1)
        try fill(huge).complete()
        #expect(TestSupport.latin1(huge.getPages()[0].getContent()).contains("576 "))
        let pdf = TestSupport.newPDF()
        let margin = try fill(edgeTable(pdf))
        margin.setColumnWidthsInPercent(20, 30, 50)
        margin.setLocation(400, 20)
        #expect(pdf.error == "The margin of the table, its x, is half the width of the page or more: the columns have no room.")
    }
}
