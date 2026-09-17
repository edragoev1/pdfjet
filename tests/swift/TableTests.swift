/**
 * TableTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct TableTests {
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

    @Test func measuringAndDrawingReturnTheSameCorner() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let table = Table().setTableData(rows(font, 5, 3), 1).setLocation(20, 20)
        TestSupport.expectXY(245, 109.36, table.drawOn(nil))
        TestSupport.expectXY(245, 109.36, table.drawOn(Page(pdf, Letter.PORTRAIT)))
    }

    @Test func measuringFirstStillDrawsEveryRowOnThePage() {
        let pdf = TestSupport.newPDF()
        let table = Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 1), 1).setLocation(20, 20)
        _ = table.drawOn(nil)
        let page = Page(pdf, Letter.PORTRAIT)
        _ = table.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("row0")))
        #expect(content.contains(TestSupport.hex("row1")))
        #expect(table.getRowsRendered() == 42)
    }

    @Test func headerRowsRepeatOnEveryPage() {
        let pdf = TestSupport.newPDF()
        let table = Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 1), 1).setLocation(20, 20)
        var pages = [Page]()
        TestSupport.expectXY(95, 341.696, table.drawOn(pdf, &pages, Letter.PORTRAIT))
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        let first = TestSupport.content(pages[0])
        let second = TestSupport.content(pages[1])
        #expect(first.contains(TestSupport.hex("row0")) && second.contains(TestSupport.hex("row0")))
        #expect(first.contains(TestSupport.hex("row1")) && !second.contains(TestSupport.hex("row1")))
        #expect(!first.contains(TestSupport.hex("row59")) && second.contains(TestSupport.hex("row59")))
    }

    @Test func theFileConstructorReadsLineBreaksInQuotedFieldsAsSpaces() throws {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("breaks-\(UUID().uuidString).csv")
        let data = "Name,Address\r\n"
                + "\"Smith, John\",\"12 Main St\r\nApt 4\"\r\n"
                + "Plain,\"one\n\ntwo\"\n"
        try Data(data.utf8).write(to: url)
        defer { try? FileManager.default.removeItem(at: url) }
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let table = try Table(font, font, url.path)
        #expect(table.getCellAt(1, 1).getText() == "12 Main St Apt 4")
        #expect(table.getCellAt(2, 0).getText() == "Plain")
        #expect(table.getCellAt(2, 1).getText() == "one  two")
        #expect(table.getColumn(0).count == 3)
    }

    @Test func theFileConstructorReadsQuotedFields() throws {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("quoted-\(UUID().uuidString).csv")
        let data = "\"Name\",\"Note\",\"Amount\"\n"
                + "\"Smith, John\",\"said \"\"hi\"\"\",\"1,200\"\n"
                + "Plain,,7\n"
        try Data(data.utf8).write(to: url)
        defer { try? FileManager.default.removeItem(at: url) }
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let table = try Table(font, font, url.path)
        #expect(table.getRow(0).count == 3)
        #expect(table.getCellAt(0, 0).getText() == "Name")
        #expect(table.getCellAt(1, 0).getText() == "Smith, John")
        #expect(table.getCellAt(1, 1).getText() == "said \"hi\"")
        #expect(table.getCellAt(1, 2).getText() == "1,200")
        #expect(table.getRow(2).count == 3)
        #expect(table.getCellAt(2, 0).getText() == "Plain")
        #expect(table.getCellAt(2, 1).getText() == "")
        #expect(table.getCellAt(2, 2).getText() == "7")
    }

    @Test func aRowTallerThanThePageIsDrawnRatherThanAskedForForever() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var text = ""
        for i in 0..<200 {
            text += "word\(i) "
        }
        let header = [Cell(font, "header")]
        let tall = Cell(font)
        tall.setTextBlock(TextBlock(font, text)).setWidth(70.0)
        let table = Table().setTableData([header, [tall]], 1)
        table.setLocation(50.0, 50.0)
        table.setBottomMargin(20.0)
        #expect(tall.getHeight(66.0) > 792.0)       // taller than a Letter page
        var pages = [Page]()
        // Asked for pages forever before.
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count == 1)
        #expect(table.getRowsRendered() == -1)
        #expect(TestSupport.content(pages[0]).contains(TestSupport.hex("word0")))
    }

    @Test func theFileConstructorDropsAByteOrderMarkAndPadsShortRows() throws {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent("table-\(UUID().uuidString).txt")
        try Data("\u{FEFF}a|b|c\n1||\n2\n".utf8).write(to: url)
        defer { try? FileManager.default.removeItem(at: url) }
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let table = try Table(font, font, url.path)
        #expect(table.getCellAt(0, 0).getText() == "a")
        #expect(table.getCellAt(0, 2).getText() == "c")
        #expect(table.getRow(1).count == 3)
        #expect(table.getCellAt(1, 0).getText() == "1")
        #expect(table.getCellAt(1, 2).getText() == "")
        #expect(table.getRow(2).count == 3)
        #expect(table.getCellAt(2, 2).getText() == "")
        #expect(table.getColumn(0).count == 3)
    }

    @Test func getCellAtGetRowAndGetColumnAgree() {
        let table = Table().setTableData(rows(TestSupport.helvetica(TestSupport.newPDF()), 4, 3), 1)
        #expect(table.getCellAt(2, 1) === table.getRow(2)[1])
        #expect(table.getCellAt(2, 1) === table.getColumn(1)[2])
        #expect(table.getCellAt(2, 1).getText() == "r2c1")
    }

    @Test func rightAlignNumbersRightAlignsOnlyNumbers() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        var data = [[Cell]]()
        for text in ["header", "-1.5e3", "12a", "+7", "3."] {
            data.append([Cell(font, text)])
        }
        let table = Table().setTableData(data, 1).rightAlignNumbers()
        #expect(table.getCellAt(1, 0).getTextAlignment() == Alignment.RIGHT)
        #expect(table.getCellAt(2, 0).getTextAlignment() == Alignment.LEFT)
        #expect(table.getCellAt(3, 0).getTextAlignment() == Alignment.RIGHT)
        #expect(table.getCellAt(4, 0).getTextAlignment() == Alignment.RIGHT)
        #expect(table.getCellAt(0, 0).getTextAlignment() != Alignment.RIGHT)
    }

    @Test func anEmptyTableHasNoWidth() {
        #expect(Table().getWidth() == 0)
    }

    @Test func anEmptyTableDrawsNothing() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TestSupport.expectXY(20, 30, Table().setLocation(20, 30).drawOn(page))
        TestSupport.expectXY(20, 30, Table().setLocation(20, 30).drawOn(nil))
        var pages = [Page]()
        TestSupport.expectXY(20, 30, Table().setLocation(20, 30).drawOn(pdf, &pages, Letter.PORTRAIT))
        #expect(pages.isEmpty)
        let empty = Table().setTableData([[Cell]](), 1)
        empty.autoAdjustColumnWidths().rightAlignNumbers()
        TestSupport.expectXY(0, 0, empty.drawOn(page))
        #expect(TestSupport.content(page) == "")
    }

    @Test func moreHeaderRowsThanRowsDrawsTheRows() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let expected = Table().setTableData(rows(font, 2, 1), 1).setLocation(20, 20).drawOn(nil)
        let xy = Table().setTableData(rows(font, 2, 1), 5).setLocation(20, 20).drawOn(Page(pdf, Letter.PORTRAIT))
        TestSupport.expectXY(expected[0], expected[1], xy)
    }
}
