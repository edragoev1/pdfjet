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
}
