/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_21.swift
 *
 * Draws two tables. The first, of reading scores, has two header rows, a
 * header cell that spans two columns, and cells with background colors. The
 * second spans its cells over columns, with setColSpan, and over rows, with
 * setRowSpan.
 */
public class Example_21 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_21.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Table Headers and Spans")

        try drawHeaderRows(pdf)
        try drawSpans(pdf)

        try pdf.complete()
    }

    private func drawHeaderRows(_ pdf: PDF) throws {
        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(7.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(7.0)

        var tableData = [[Cell]]()
        let text = (try String(contentsOfFile:
                "data/winter-2009.txt", encoding: .utf8)).trimmingCharacters(in: .newlines)
        let lines = text.replacingOccurrences(of: "\r\n", with: "\n").components(separatedBy: "\n")
        for line1 in lines {
            let line = line1.trimmingCharacters(in: .newlines)
            var row = [Cell]()
            let columns = line.components(separatedBy: "|")
            for column in columns {
                row.append(Cell(f2, String(column)))
            }
            tableData.append(row)
        }

        let table = Table()
        table.setTableData(tableData, 2)
        table.setLocation(100.0, 50.0)
        table.setBottomMargin(10.0)

        table.setFontInRow(0, f1)
        table.setFontInRow(1, f1)

        table.autoAdjustColumnWidths()
        table.removeLineBetweenRows(0, 1)

        var cell = table.getCellAt(1, 1)
        cell.setBorder(Border.TOP, true)

        cell = table.getCellAt(1, 2)
        cell.setBorder(Border.TOP, true)

        cell = table.getCellAt(0, 1)
        cell.setColSpan(2)
        cell.setTextAlignment(Alignment.CENTER)

        var column = table.getColumn(7)
        for i in 0..<column.count {
            cell = column[i]
            cell.setTextAlignment(Alignment.CENTER)
        }

        column = table.getColumn(4)
        for i in 2..<column.count {
            cell = column[i]
            cell.setTextAlignment(Alignment.CENTER)
            if let value = Int(cell.getText()!) {
                if value > 40 {
                    cell.setBackgroundColor(Color.darkseagreen)
                } else {
                    cell.setBackgroundColor(Color.yellow)
                }
            }
        }

        column = table.getColumn(2)
        for i in 2..<column.count {
            cell = column[i]
            if cell.getText() == "Smith" {
                cell.setUnderline(true)
            }
            if cell.getText() == "Bowden" {
                cell.setStrikeout(true)
            }
        }

        column = table.getColumn(2)
        for i in 2..<column.count {
            cell = column[i]
            if cell.getText() == "Bowden" {
                cell.setStrikeout(false)
            }
        }

        setBgColorForRow(table, 0, Color.lightgray)
        setBgColorForRow(table, 1, Color.lightgray)

        table.setColumnWidth(3, 10.0)
        blankOutColumn(table, 3)

        table.setColumnWidth(8, 10.0)
        blankOutColumn(table, 8)

        // A heading on the first page only, above the table: the first page is
        // made here, and the table goes on to new pages.
        let first = Page(pdf, Letter.PORTRAIT, false)
        TextLine(f1, "Winter Reading Scores")
                .setStructureType(StructElem.H1)
                .setFontSize(16.0)
                .setLocation(100.0, 38.0)
                .drawOn(first)
        table.setFirstPageTopMargin(50.0)

        var pages = [first]
        table.drawOn(pdf, first, &pages, Letter.PORTRAIT)
        for page in pages {
            pdf.addPage(page)
        }
    }

    private func drawSpans(_ pdf: PDF) throws {
        let font = try Font(pdf, CascadiaMono.Regular)
        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        let f2 = try Font(pdf, IBMPlexSans.Regular)
        let page = Page(pdf, Letter.LANDSCAPE)

        let title = TextLine(f1, "Table Cells That Span Rows and Columns")
        title.setStructureType(StructElem.H1)
        title.setFontSize(18.0)
        title.setLocation(50.0, 50.0)
        title.drawOn(page)

        let textBlock = TextBlock(f2,
                "The cells of this table span up to five columns and up to four rows, as "
                + "the name in each cell says: 1x3 is one column wide and three rows tall. "
                + "A cell spans columns with setColSpan and rows with setRowSpan, and draws "
                + "its text, its background and its borders once over all of them. The table "
                + "keeps its shape, so every row holds a cell for every column and the cells "
                + "a span covers are left empty. The example is also a check of the geometry "
                + "of the cells: their backgrounds meet without gaps and their borders line "
                + "up. The header row above them numbers the columns.")
        textBlock.setFontSize(11.0)
        textBlock.setLineSpacing(1.3)
        textBlock.setLocation(50.0, 65.0)
        textBlock.setWidth(500.0)
        let xy = textBlock.drawOn(page)

        let table = Table()
        table.setTableData(createTableData(font, f1), 1)
        table.setBottomMargin(10.0)
        table.setLocation(50.0, xy[1] + 20.0)
        table.drawOn(page)
    }

    public func blankOutColumn(_ table: Table, _ index: Int) {
        let column = table.getColumn(index)
        for cell in column {
            cell.setBackgroundColor(Color.white)
            cell.setBorder(Border.TOP, false)
            cell.setBorder(Border.BOTTOM, false)
        }
    }

    public func setBgColorForRow(
            _ table: Table,
            _ index: Int,
            _ color: Int32) {
        let row = table.getRow(index)
        for cell in row {
            cell.setBackgroundColor(color)
        }
    }

    /**
     * Returns a header row that numbers the columns, in the header font, and
     * under it the cells of a 10 by 10 table whose cells span columns and
     * rows. They are the table of this HTML, cell for cell:
     * <pre>
     * &lt;table border="solid"&gt;
     * &lt;tr&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="5"&gt;5x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;
     *     &lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td colspan="3"&gt;3x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="4" rowspan="4"&gt;4x4&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;td rowspan="3"&gt;1x3&lt;/td&gt;
     *     &lt;td rowspan="3"&gt;1x3&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td rowspan="4"&gt;1x4&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td colspan="2"&gt;2x1&lt;/td&gt;
     *     &lt;td colspan="2" rowspan="2"&gt;2x2&lt;/td&gt;&lt;td rowspan="2"&gt;1x2&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;
     *     &lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;tr&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;td&gt;1x1&lt;/td&gt;&lt;/tr&gt;
     * &lt;/table&gt;
     * </pre>
     */
    private func createTableData(_ font: Font, _ headerFont: Font) -> [[Cell]] {
        // The columns and the rows each cell spans, in the order a browser
        // reads the cells of the HTML above.
        let spans = [
            [[2, 2], [2, 1], [2, 1], [2, 1], [2, 1]],
            [[2, 2], [1, 1], [5, 1]],
            [[1, 2], [1, 1], [2, 2], [1, 2], [3, 1]],
            [[1, 1], [1, 3], [1, 1], [2, 1], [1, 2]],
            [[1, 2], [1, 1], [2, 1], [4, 4]],
            [[1, 1], [1, 3], [1, 3], [1, 3]],
            [[1, 2], [1, 1], [1, 4]],
            [[1, 1]],
            [[1, 2], [1, 1], [2, 1], [2, 2], [1, 2], [1, 1], [1, 1]],
            [[1, 1], [1, 1], [1, 1], [1, 1], [1, 1]],
        ]
        let columns = 10
        var grid = [[Cell?]](repeating: [Cell?](repeating: nil, count: columns), count: spans.count)
        for r in 0..<spans.count {
            var c = 0
            for span in spans[r] {
                // The next column that no cell of a row above spans over.
                while c < columns && grid[r][c] != nil {
                    c += 1
                }
                grid[r][c] = getCell(font, span[0], span[1], "\(span[0])x\(span[1])")
                // A table keeps its shape, so every row holds a cell for every
                // column: the cells a span covers are there and are empty.
                var r2 = r
                while r2 < r + span[1] && r2 < spans.count {
                    var c2 = c
                    while c2 < c + span[0] && c2 < columns {
                        if grid[r2][c2] == nil {
                            grid[r2][c2] = getCell(font, 1, 1, "")
                        }
                        c2 += 1
                    }
                    r2 += 1
                }
                c += span[0]
            }
        }
        var rows = [[Cell]]()
        // The header row, above the cells: it numbers the columns.
        var header = [Cell]()
        for c in 0..<columns {
            header.append(getCell(headerFont, 1, 1, String(c + 1)))
        }
        rows.append(header)
        for row in grid {
            rows.append(row.map { $0! })
        }
        return rows
    }

    private func getCell(
            _ font: Font,
            _ colSpan: Int,
            _ rowSpan: Int,
            _ text: String) -> Cell {
        let cell = Cell(font, "")
        cell.setColSpan(colSpan)
        cell.setRowSpan(rowSpan)
        cell.setWidth(50.0)
        cell.setText(text)
        cell.setBorder(Border.TOP, true)
        cell.setBorder(Border.BOTTOM, true)
        cell.setBorder(Border.LEFT, true)
        cell.setBorder(Border.RIGHT, true)
        cell.setTextAlignment(Alignment.CENTER)
        cell.setVerticalAlignment(Alignment.CENTER)
        cell.setBackgroundColor(0xD8F0E4)     // A pastel mint
        cell.setBorderWidth(1.0)
        return cell
    }
}   // End of Example_21.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_21()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_21 => \(String(format: "%4lld", time1 - time0)) ms")
