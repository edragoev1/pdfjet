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
}
