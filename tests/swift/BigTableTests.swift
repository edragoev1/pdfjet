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
