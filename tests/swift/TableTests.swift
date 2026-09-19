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

    // The number of times the text is in the string.
    private func count(_ str: String, _ text: String) -> Int {
        return str.components(separatedBy: text).count - 1
    }

    @Test func aTableInAPDFUADocumentIsTaggedAsATable() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let underlined = Cell(font, "b").setUnderline(true)
        let data: [[Cell]] = [
            [Cell(font, "Name"), Cell(font, "Notes")],
            [Cell(font, "a"), Cell(font, "a note long enough to wrap to four lines")],
            [Cell(font, "spanned").setColSpan(2), Cell(font, "")],
            [underlined, Cell(font, "c")]]
        _ = Table().setTableData(data, 1).setLocation(20, 20).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /Table\n") == 1)
        // The lines of the wrapped note are one row and one cell.
        #expect(count(raw, "/S /TR\n") == 4)
        #expect(count(raw, "/S /TH\n") == 2)
        #expect(count(raw, "/S /TD\n") == 5)
        #expect(count(raw, "/A <</O /Table /Scope /Column>>") == 2)
        #expect(count(raw, "/A <</O /Table /ColSpan 2>>") == 1)
        let fourKids = try NSRegularExpression(
                pattern: "/S /TD\n[^\n]*\n/K \\[\\d+ 0 R \\d+ 0 R \\d+ 0 R \\d+ 0 R \\]")
        #expect(fourKids.numberOfMatches(in: raw, range: NSRange(raw.startIndex..., in: raw)) == 1)
        // The text of the cells, and not the underline, is in P elements.
        #expect(count(raw, "/S /P\n") == 10)
    }

    @Test func theHeaderRowsOnTheNextPagesAreArtifacts() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let table = Table().setTableData(rows(TestSupport.helvetica(memory.pdf), 60, 2), 1).setLocation(20, 20)
        var pages = [Page]()
        _ = table.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        let content = TestSupport.content(pages[1])
        #expect(content.hasPrefix("/Artifact BMC\n"))
        let end = content.range(of: "EMC\n")!.lowerBound
        #expect(content.range(of: TestSupport.hex("r0c1"))!.lowerBound < end)
        #expect(content.range(of: "BDC")!.lowerBound > end)
        memory.pdf.addPages(pages)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /Table\n") == 1)
        #expect(count(raw, "/S /TR\n") == 60)
        #expect(count(raw, "/S /TH\n") == 2)
        #expect(count(raw, "/S /TD\n") == 118)
    }

    @Test func aTableIsNotTaggedInADocumentThatIsNotPDFUA() {
        let pdf = TestSupport.newPDF()
        let table = Table().setTableData(rows(TestSupport.helvetica(pdf), 60, 2), 1).setLocation(20, 20)
        var pages = [Page]()
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        guard pages.count == 2 else { return }
        let content = TestSupport.content(pages[1])
        #expect(!content.contains("BMC") && !content.contains("BDC") && !content.contains("EMC"))
    }

    @Test func aTableWithAPageLeftOutOfTheDocumentStillHasAStructureTree() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let table = Table().setTableData(rows(TestSupport.helvetica(memory.pdf), 60, 2), 1).setLocation(20, 20)
        var pages = [Page]()
        _ = table.drawOn(memory.pdf, &pages, Letter.PORTRAIT)
        guard pages.count == 2 else { return }
        memory.pdf.addPage(pages[1])    // The page with the Table element is left out.
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains("/P 0 0 R"))
        #expect(!raw.contains("/K [0 0 R") && !raw.contains(" 0 0 R ]"))
    }
}
