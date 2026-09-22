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

    @Test func aPageBreakKeepsTheWrappedLinesOfARowTogether() {
        // The lines a cell's text wraps into are rows of their own, which a
        // page break moves to the next page together, with the other cells of
        // the row. The row is put at each place near the end of the first page.
        var moved = false
        for at in 30..<50 {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            var data = [[Cell]]()
            for r in 0..<60 {
                let texts = (r == at) ? [longText, "beside"] : ["r\(r)", "x"]
                data.append(texts.map { text in
                    let cell = Cell(font, text)
                    cell.setWidth(60)
                    return cell
                })
            }
            let table = Table().setTableData(data, 1).setLocation(50, 50)
            table.setBottomMargin(20)
            var pages = [Page]()
            _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
            #expect(table.getRow(at)[0].getText() != longText, "the text did not wrap")
            var first = -1
            var last = -1
            var beside = -1
            for (i, page) in pages.enumerated() {
                let content = TestSupport.content(page)
                if content.contains(TestSupport.hex("one")) {
                    first = i
                }
                if content.contains(TestSupport.hex("twelve")) {
                    last = i
                }
                if content.contains(TestSupport.hex("beside")) {
                    beside = i
                }
            }
            #expect(first >= 0, "the wrapped text was not drawn")
            #expect(first == last, "row \(at): the page break cut the wrapped text")
            #expect(first == beside, "row \(at): the other cell of the row is not with its lines")
            if first == 1 && TestSupport.content(pages[0]).contains(TestSupport.hex("r\(at - 1)")) {
                moved = true
            }
        }
        #expect(moved, "no row was moved to the next page whole")
    }

    @Test func theWrappedLinesOfARowThatFitNoPageAreCutWhereThePageEnds() {
        // Moved to the next page, lines taller than a page would go past its
        // end there too, so they are drawn from where the row starts and go
        // on over the next pages.
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let text = (0..<200).map { "word\($0)" }.joined(separator: " ")
        let data = [["header", "h"], ["r1", "x"], [text, "beside"]].map { texts in
            texts.map { t in
                let cell = Cell(font, t)
                cell.setWidth(60)
                return cell
            }
        }
        let table = Table().setTableData(data, 1).setLocation(50, 50)
        table.setBottomMargin(20)
        var pages = [Page]()
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count >= 3, "\(pages.count) pages")
        guard pages.count >= 3 else { return }
        #expect(table.getRowsRendered() == -1)
        let firstPage = TestSupport.content(pages[0])
        #expect(firstPage.contains(TestSupport.hex("r1")) && firstPage.contains(TestSupport.hex("word0")),
                "the row does not start on the first page, after the row above it")
        #expect(TestSupport.content(pages[pages.count - 1]).contains(TestSupport.hex("word199")))
    }

    // A table of 60 rows of two cells, "r0" to "r59" and "x", whose row at
    // index has the texts given instead.
    private func rowsWith(_ font: Font, _ index: Int, _ texts: [String] = []) -> [[Cell]] {
        return (0..<60).map { r in
            ((r == index) ? texts : ["r\(r)", "x"]).map { text in
                let cell = Cell(font, text)
                cell.setWidth(60)
                return cell
            }
        }
    }

    // The index of the last page that draws the text, or -1.
    private func pageOf(_ pages: [Page], _ text: String) -> Int {
        var page = -1
        for (i, p) in pages.enumerated() where TestSupport.content(p).contains(TestSupport.hex(text)) {
            page = i
        }
        return page
    }

    @Test func aRowKeptWithTheNextOneGoesToThePageOfTheNextOne() {
        // A heading row, whose text wraps, is kept with the row under it: a
        // page break does not fall between them, and moves both to the next
        // page. The rows are put at each place near the end of the first page.
        var moved = false
        for at in 30..<50 {
            let pdf = TestSupport.newPDF()
            let data = rowsWith(TestSupport.helvetica(pdf), at, [longText, "heading"])
            data[at + 1][0].setText("follows")
            let table = Table().setTableData(data, 1).setLocation(50, 50)
            table.setBottomMargin(20)
            table.keepRowWithNext(at)
            var pages = [Page]()
            _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
            let heading = pageOf(pages, "one")
            #expect(heading >= 0, "the heading was not drawn")
            #expect(heading == pageOf(pages, "twelve"), "row \(at): the page break cut the heading")
            #expect(heading == pageOf(pages, "follows"), "row \(at): the heading is not with the next row")
            if heading == 1 && pageOf(pages, "r\(at - 1)") == 0 {
                moved = true
            }
        }
        #expect(moved, "no heading was moved to the next page with the next row")
    }

    @Test func rowsKeptWithTheNextOneOneAfterAnotherAreKeptTogether() {
        for at in 30..<50 {
            let pdf = TestSupport.newPDF()
            let table = Table().setTableData(rowsWith(TestSupport.helvetica(pdf), -1), 1).setLocation(50, 50)
            table.setBottomMargin(20)
            table.keepRowWithNext(at).keepRowWithNext(at + 1).keepRowWithNext(at + 2)
            var pages = [Page]()
            _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
            let page = pageOf(pages, "r\(at)")
            for r in (at + 1)...(at + 3) {
                #expect(page == pageOf(pages, "r\(r)"), "row \(r) is not with row \(at)")
            }
        }
    }

    @Test func rowsKeptTogetherThatFitNoPageAreDrawnEachOnItsOwn() {
        // Moved to the next page, rows taller than a page would go past its
        // end there too, so they are drawn from where they start as if they
        // were not kept together: the pages hold the rows they hold without
        // the marks.
        var drawn = [[Page]]()
        for kept in [false, true] {
            let pdf = TestSupport.newPDF()
            let table = Table().setTableData(rowsWith(TestSupport.helvetica(pdf), -1), 1).setLocation(50, 50)
            table.setBottomMargin(20)
            if kept {
                for r in 1..<59 {
                    table.keepRowWithNext(r)
                }
            }
            var pages = [Page]()
            _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
            #expect(table.getRowsRendered() == -1)
            drawn.append(pages)
        }
        #expect(drawn[1].count == 2)
        for r in 1..<60 {
            #expect(pageOf(drawn[0], "r\(r)") == pageOf(drawn[1], "r\(r)"), "row \(r)")
        }
    }

    // The y of the baseline of the text on the page, or NaN.
    private func yOf(_ page: Page, _ text: String) -> Float {
        let content = TestSupport.content(page)
        let pattern = "[-0-9.]+ ([-0-9.]+) Td\\n(?:/F\\d+ [0-9.]+ Tf\\n)?\\[<" + TestSupport.hex(text) + ">\\] TJ"
        let regex = try! NSRegularExpression(pattern: pattern)
        guard let m = regex.firstMatch(in: content, range: NSRange(content.startIndex..., in: content)),
                let range = Range(m.range(at: 1), in: content) else {
            return Float.nan
        }
        return Float(content[range])!
    }

    // A table of 60 rows, a header row, the rows "r1" to "r58", and a footer
    // row with the texts given, drawn on as many pages as it needs.
    private func withFooter(_ pdf: PDF, _ footer: [String]) -> [Page] {
        let data = rowsWith(TestSupport.helvetica(pdf), 59, footer)
        let table = Table().setTableData(data, 1).setNumberOfFooterRows(1).setLocation(50, 50)
        table.setBottomMargin(20)
        var pages = [Page]()
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(table.getRowsRendered() == -1)
        return pages
    }

    @Test func theFooterRowsAreDrawnUnderTheLastRowOfEveryPage() {
        let pages = withFooter(TestSupport.newPDF(), ["total", "sum"])
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        for r in 1..<59 {
            let n = pages.reduce(0) { $0 + count(TestSupport.content($1), "<" + TestSupport.hex("r\(r)") + ">") }
            #expect(n == 1, "row \(r) is drawn \(n) times")
        }
        let rowHeight = yOf(pages[0], "r1") - yOf(pages[0], "r2")
        for (i, page) in pages.enumerated() {
            var last = 0
            for r in 1..<59 where !yOf(page, "r\(r)").isNaN {
                last = r
            }
            TestSupport.expectNear(rowHeight, yOf(page, "r\(last)") - yOf(page, "total"), 0.02)
            // The bottom of the footer, 4.7 under the baseline, is over the
            // bottom margin.
            #expect(yOf(page, "total") - 4.7 >= 20 - 0.01, "page \(i): the footer is in the margin")
        }
    }

    @Test func aFooterRowThatWrapsIsDrawnWholeOnEveryPage() {
        let pages = withFooter(TestSupport.newPDF(), [longText, "sum"])
        #expect(pages.count == 2)
        for page in pages {
            let content = TestSupport.content(page)
            #expect(content.contains(TestSupport.hex("one")) && content.contains(TestSupport.hex("twelve")))
            #expect(count(content, TestSupport.hex("sum")) == 1)
        }
    }

    @Test func theFooterRowsBeforeTheEndOfTheTableAreArtifacts() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let pages = withFooter(memory.pdf, ["total", "sum"])
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        for (i, page) in pages.enumerated() {
            let content = TestSupport.content(page)
            let before = String(content[..<content.range(of: TestSupport.hex("total"))!.lowerBound])
            let artifact = before.range(of: "/Artifact BMC", options: .backwards).map { a in
                before.range(of: "EMC", options: .backwards).map { a.lowerBound > $0.lowerBound } ?? true
            } ?? false
            #expect(artifact == (i < pages.count - 1), "page \(i)")
        }
        memory.pdf.addPages(pages)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(count(raw, "/S /Table\n") == 1)
        #expect(count(raw, "/S /TR\n") == 60)
        #expect(count(raw, "/S /TH\n") == 2)
        #expect(count(raw, "/S /TD\n") == 118)
    }

    @Test func measuringATableWithFooterRowsReturnsTheCornerDrawingDoes() {
        let pdf = TestSupport.newPDF()
        let data = rows(TestSupport.helvetica(pdf), 5, 3)
        data[4][0].setText("total")
        let table = Table().setTableData(data, 1).setNumberOfFooterRows(1).setLocation(20, 20)
        let measured = table.drawOn(nil)
        let page = Page(pdf, Letter.PORTRAIT)
        TestSupport.expectXY(measured[0], measured[1], table.drawOn(page))
        TestSupport.expectXY(245, 109.36, measured)
        #expect(count(TestSupport.content(page), TestSupport.hex("total")) == 1)
    }

    @Test func theFooterSumsAreTheTotalsOfThePageAndOfThePagesUpToIt() {
        // Rows r1 to r57 hold r + 0.25 in their second cell, but for row 10,
        // which holds no number, and the two footer rows the page total and
        // the total carried forward.
        let pdf = TestSupport.newPDF()
        let data = rowsWith(TestSupport.helvetica(pdf), -1)
        for r in 1..<59 {
            data[r][1].setText(r == 10 ? "n/a" : "\(r).25")
        }
        data[58][0].setText("page")
        data[59][0].setText("carried")
        let table = Table().setTableData(data, 1).setNumberOfFooterRows(2)
                .setPageSum(58, 1, 2).setRunningSum(59, 1, 2).setLocation(50, 50)
        table.setBottomMargin(20)
        // Until it is drawn, a cell has the sum of all the rows.
        #expect(table.getCellAt(58, 1).getText() == "1,657.00")
        var pages = [Page]()
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count == 2)
        var carried: Int64 = 0
        for page in pages {
            var total: Int64 = 0
            for r in 1..<58 where r != 10 && !yOf(page, "r\(r)").isNaN {
                total += Int64(100 * r + 25)
            }
            carried += total
            let content = TestSupport.content(page)
            #expect(content.contains("<" + TestSupport.hex(Table.formatSum(total, 2)) + ">"),
                    "no page total \(Table.formatSum(total, 2))")
            #expect(content.contains("<" + TestSupport.hex(Table.formatSum(carried, 2)) + ">"),
                    "no total carried \(Table.formatSum(carried, 2))")
        }
        // Rows 1 to 57 but row 10: 1,643 and 56 quarters.
        #expect(carried == 164300 + 1400)
    }

    @Test func aSumReadsTheNumbersAsRightAlignNumbersDoes() {
        #expect(Table.numberOf("1,234.50", 2) == 123450)
        #expect(Table.numberOf("(1,234.50)", 2) == -123450)
        #expect(Table.numberOf(" -5 ", 0) == -5)
        #expect(Table.numberOf("1.5E+3", 0) == 1500)
        #expect(Table.numberOf("1.5e1", 0) == 15)
        #expect(Table.numberOf("1'234", 0) == 1234)
        #expect(Table.numberOf(".5", 2) == 50)
        // Halves away from zero.
        #expect(Table.numberOf("0.125", 2) == 13)
        #expect(Table.numberOf("-0.125", 2) == -13)
        #expect(Table.numberOf("0.004", 2) == 0)
        #expect(Table.numberOf("0.5", 0) == 1)
        #expect(Table.numberOf("1.234.567", 0) == nil)
        #expect(Table.numberOf("n/a", 0) == nil)
        #expect(Table.numberOf("", 0) == nil)
        #expect(Table.numberOf("1234567890123456789", 0) == nil)
        #expect(Table.numberOf("1E99999", 0) == nil)
        #expect(Table.formatSum(123450, 2) == "1,234.50")
        #expect(Table.formatSum(-1234567, 0) == "-1,234,567")
        #expect(Table.formatSum(5, 2) == "0.05")
        #expect(Table.formatSum(-5, 2) == "-0.05")
        #expect(Table.formatSum(0, 2) == "0.00")
        #expect(Table.formatSum(999, 0) == "999")
        #expect(Table.formatSum(1000, 0) == "1,000")
    }

    @Test func theTotalBroughtForwardIsTheTotalCarriedFromThePageBefore() {
        // A header row, a header row that brings the total forward, the rows
        // r2 to r58 with r + 0.25 in their second cell, and a footer row that
        // carries the total forward.
        let pdf = TestSupport.newPDF()
        let data = rowsWith(TestSupport.helvetica(pdf), -1)
        for r in 2..<59 {
            data[r][1].setText("\(r).25")
        }
        data[1][0].setText("brought")
        data[59][0].setText("carried")
        let table = Table().setTableData(data, 2).setNumberOfFooterRows(1)
                .setBroughtForwardSum(1, 1, 2).setRunningSum(59, 1, 2).setLocation(50, 50)
        table.setBottomMargin(20)
        var pages = [Page]()
        _ = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count == 2)
        guard pages.count == 2 else { return }
        #expect(yOf(pages[0], "brought").isNaN, "the first page brings a total forward")
        var carried: Int64 = 0
        for r in 2..<59 where !yOf(pages[0], "r\(r)").isNaN {
            carried += Int64(100 * r + 25)
        }
        let next = TestSupport.content(pages[1])
        let text = "<" + TestSupport.hex(Table.formatSum(carried, 2)) + ">"
        #expect(TestSupport.content(pages[0]).contains(text), "the first page does not carry \(text)")
        #expect(next.contains(text), "the second page does not bring \(text) forward")
        // Under the header row, and over the rows of the page.
        let rowHeight = yOf(pages[0], "r2") - yOf(pages[0], "r3")
        TestSupport.expectNear(rowHeight, yOf(pages[1], "r0") - yOf(pages[1], "brought"), 0.02)
        var firstRow = 2
        while yOf(pages[1], "r\(firstRow)").isNaN {
            firstRow += 1
        }
        TestSupport.expectNear(rowHeight, yOf(pages[1], "brought") - yOf(pages[1], "r\(firstRow)"), 0.02)
        // The rows are each drawn once, and the total is that of all of them.
        for r in 2..<59 {
            let n = pages.reduce(0) { $0 + count(TestSupport.content($1), "<" + TestSupport.hex("r\(r)") + ">") }
            #expect(n == 1, "row \(r)")
        }
        // Rows 2 to 58: 1,710 and 57 quarters.
        #expect(next.contains("<" + TestSupport.hex(Table.formatSum(171000 + 1425, 2)) + ">"), "no total")
    }
}
