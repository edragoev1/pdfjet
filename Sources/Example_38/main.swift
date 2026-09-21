/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_38.swift
 *
 * Draws a table whose cells span columns and rows, and explains how. A cell
 * spans columns with setColSpan and rows with setRowSpan. The table is also a
 * check of the geometry of the cells: their backgrounds meet without gaps and
 * their borders line up.
 */
public class Example_38 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_38.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Table Cells That Span Rows and Columns")
        let font = try Font(pdf, IBMPlexMono.Regular)
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
                + "up.")
        textBlock.setFontSize(11.0)
        textBlock.setLineSpacing(1.3)
        textBlock.setLocation(50.0, 65.0)
        textBlock.setWidth(500.0)
        let xy = textBlock.drawOn(page)

        let table = Table()
        table.setTableData(createTableData(font))
        table.setBottomMargin(10.0)
        table.setLocation(50.0, xy[1] + 20.0)
        table.drawOn(page)

        try pdf.complete()
    }

    /**
     * Returns the cells of a 10 by 10 table whose cells span columns and
     * rows. It is the table of this HTML, cell for cell:
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
    private func createTableData(_ font: Font) -> [[Cell]] {
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
}   // End of Example_38.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_38()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_38 => \(String(format: "%4lld", time1 - time0)) ms")
