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
    // The y coordinates of the horizontal rules the page draws, top to bottom.
    private func rules(_ page: Page) -> [Float] {
        let content = TestSupport.content(page)
        let regex = try! NSRegularExpression(pattern: "([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l")
        var ys = Set<Float>()
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let y1 = Float((content as NSString).substring(with: m.range(at: 2)))!
            let y2 = Float((content as NSString).substring(with: m.range(at: 4)))!
            if abs(y1 - y2) < 0.01 {
                ys.insert(y1)
            }
        }
        return ys.sorted(by: >)
    }

    // A table of rows by columns of cells with every border, the cell at 0,0
    // spanning the rows.
    private func spanning(_ font: Font, _ rows: Int, _ columns: Int, _ rowspan: Int) -> [[Cell]] {
        var data = [[Cell]]()
        for r in 0..<rows {
            var row = [Cell]()
            for c in 0..<columns {
                let text = (r == 0 && c == 0) ? "spans" : (r < rowspan && c == 0) ? "" : "r\(r)c\(c)"
                let cell = Cell(font, text)
                cell.setWidth(60)
                cell.setBorder(Border.TOP, true)
                cell.setBorder(Border.BOTTOM, true)
                cell.setBorder(Border.LEFT, true)
                cell.setBorder(Border.RIGHT, true)
                row.append(cell)
            }
            data.append(row)
        }
        data[0][0].setRowSpan(rowspan)
        return data
    }

    @Test func aCellThatSpansRowsIsDrawnOnceOverAllOfThem() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        _ = Table().setTableData(spanning(font, 3, 2, 2)).setLocation(50, 50).drawOn(page)
        let content = TestSupport.content(page)
        // The spanning cell is drawn once, and the cell it covers not at all.
        #expect(count(content, TestSupport.hex("spans")) == 1)
        #expect(count(content, TestSupport.hex("r1c0")) == 0)
        #expect(count(content, TestSupport.hex("r1c1")) == 1)
        // Its left border runs from the top of its row to the bottom of the
        // row under it, which is two rows of the three rules the table draws.
        let ys = rules(page)
        #expect(ys.count == 4)
        guard ys.count == 4 else { return }
        let rowHeight = ys[0] - ys[1]
        let regex = try! NSRegularExpression(pattern: "50 ([-0-9.]+) m\n50 ([-0-9.]+) l")
        guard let m = regex.firstMatch(in: content, range: NSRange(content.startIndex..., in: content)) else {
            Issue.record("the spanning cell drew no left border")
            return
        }
        let top = Float((content as NSString).substring(with: m.range(at: 1)))!
        let bottom = Float((content as NSString).substring(with: m.range(at: 2)))!
        #expect(abs(2 * rowHeight - (top - bottom)) < 0.01, "the spanning cell is not two rows tall")
    }

    @Test func aPageBreakKeepsTheRowsOfASpanTogether() {
        // The rows a cell spans go to the next page with it, so that a span is
        // never cut in two.
        for rowspan in [1, 2, 3, 4] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            let data = spanning(font, 60, 2, rowspan)
            // The span sits where the first page ends.
            data[0][0].setRowSpan(1)
            data[48][0].setRowSpan(rowspan)
            data[48][0].setText("spans")
            for r in 49..<(48 + rowspan) {
                data[r][0].setText("")
            }
            let table = Table().setTableData(data).setLocation(50, 50)
            table.setBottomMargin(20)
            var pages = [Page]()
            _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
            let contents = pages.map { TestSupport.content($0) }
            var spanPage = -1
            for i in 0..<contents.count where count(contents[i], TestSupport.hex("spans")) > 0 {
                spanPage = i
            }
            #expect(spanPage >= 0, "the spanning cell was not drawn")
            guard spanPage >= 0 else { return }
            for r in 48..<(48 + rowspan) {
                #expect(count(contents[spanPage], TestSupport.hex("r\(r)c1")) == 1,
                        "row \(r) is not on the page of the span it belongs to")
            }
        }
    }

    @Test func aCellThatSpansRowsSaysSoInAPDFUADocument() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        let data = spanning(font, 3, 2, 2)
        data[0][0].setColSpan(2)
        data[0][1].setText("")
        data[1][1].setText("")      // The second row is covered whole.
        _ = Table().setTableData(data, 1).setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/A <</O /Table /Scope /Column /ColSpan 2 /RowSpan 2>>") == 1)
        // A row that a span covers whole holds no cell of its own.
        #expect(count(raw, "/S /TR\n") == 2)
        #expect(count(raw, "/S /TH\n") == 1)
        #expect(count(raw, "/S /TD\n") == 2)
    }
    private let longText = "one two three four five six seven eight nine ten eleven twelve"

    // A one row table of a cell that wraps and a cell that does not, each with
    // every border.
    private func wrapping(_ font: Font) -> [[Cell]] {
        var row = [Cell]()
        for text in [longText, "one"] {
            let cell = Cell(font, text)
            cell.setWidth(60)
            cell.setBorders(true)
            row.append(cell)
        }
        return [row]
    }

    @Test func aCellWhoseTextWrapsDrawsOneBorderUnderIt() {
        // The rows a table wraps the text of a cell into are one cell, so the
        // border under it is drawn once, under the last of its lines. Each of
        // those rows kept the borders of the cell, so a cell of seven lines
        // drew seven rules across itself.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let table = Table().setTableData(wrapping(font)).setLocation(50, 50)
        let xy = table.drawOn(page)
        #expect(table.getRow(0)[0].getText() != longText, "the text did not wrap")
        // The table draws two rules: the one over the row and the one under it.
        let ys = rules(page)
        #expect(ys.count == 2)
        guard ys.count == 2 else { return }
        #expect(abs(ys[0] - (page.height - 50)) < 0.01, "the rule over the row")
        #expect(abs(ys[1] - (page.height - xy[1])) < 0.01, "the rule under the row")
    }

    @Test func theRowsACellWrapsIntoAreOneCellOfEveryBorderButTheirOwn() {
        // The left and the right borders are drawn down every row of the wrap,
        // which is what makes the rows one cell; only the top and the bottom
        // of the cell are drawn once.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let table = Table().setTableData(wrapping(font)).setLocation(50, 50)
        let xy = table.drawOn(page)
        // A vertical rule of each row of the wrap, down each of the three
        // edges the two cells have between and beside them.
        let content = TestSupport.content(page)
        let regex = try! NSRegularExpression(pattern: "([-0-9.]+) ([-0-9.]+) m\n([-0-9.]+) ([-0-9.]+) l")
        var down = [String: Float]()
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let x1 = (content as NSString).substring(with: m.range(at: 1))
            let x2 = (content as NSString).substring(with: m.range(at: 3))
            if x1 == x2 {
                let y1 = Float((content as NSString).substring(with: m.range(at: 2)))!
                let y2 = Float((content as NSString).substring(with: m.range(at: 4)))!
                down[x1, default: 0] += abs(y1 - y2)
            }
        }
        #expect(down.keys.sorted() == ["110", "170", "50"])
        let height = xy[1] - 50
        // The edge between the two cells is the right border of the one and
        // the left border of the other, so it is drawn twice.
        #expect(abs((down["50"] ?? 0) - height) < 0.01, "the left edge of the row")
        #expect(abs((down["110"] ?? 0) - 2 * height) < 0.01, "the edge between the cells")
        #expect(abs((down["170"] ?? 0) - height) < 0.01, "the right edge of the row")
    }
    // The y of every rule the page draws across the first column, in the order
    // they are drawn and with none of them left out.
    private func rulesAcrossTheFirstColumn(_ page: Page) -> [Float] {
        let content = TestSupport.content(page)
        let regex = try! NSRegularExpression(pattern: "50 ([-0-9.]+) m\n110 ([-0-9.]+) l")
        var ys = [Float]()
        for m in regex.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
            let y1 = (content as NSString).substring(with: m.range(at: 1))
            let y2 = (content as NSString).substring(with: m.range(at: 2))
            if y1 == y2 {
                ys.append(Float(y1)!)
            }
        }
        return ys
    }

    @Test func aCellThatWrapsAndSpansRowsDrawsItsBorderUnderTheWholeSpan() {
        // A cell that spans rows is drawn over all of them at once, so its
        // bottom border is drawn under the whole span and not at the end of
        // the rows its own text wraps into, which is where a cell that spans
        // no rows draws it.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let data = spanning(font, 3, 2, 2)
        data[0][0].setText(longText)
        let page = Page(pdf, Letter.PORTRAIT)
        let table = Table().setTableData(data).setLocation(50, 50)
        let xy = table.drawOn(page)
        #expect(table.getRow(0)[0].getText() != longText, "the text did not wrap")
        // The rule over the span, the one under it, the one over the row below
        // it, which is the same line drawn by that row, and the one under the
        // table. The rows the text wrapped into draw none of their own.
        let ys = rulesAcrossTheFirstColumn(page)
        #expect(ys.count == 4)
        guard ys.count == 4 else { return }
        #expect(abs(ys[0] - (page.height - 50)) < 0.01, "the rule over the span")
        #expect(abs(ys[1] - ys[2]) < 0.01, "the span and the row under it do not meet")
        #expect(ys[1] < ys[0] && ys[1] > ys[3],
                "the rule under the span is not between the top and the bottom of the table")
        #expect(abs(ys[3] - (page.height - xy[1])) < 0.01, "the rule under the table")
    }
}
