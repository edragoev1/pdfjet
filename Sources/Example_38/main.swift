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
                + "A cell spans columns with setColSpan. It spans rows by leaving out its "
                + "bottom border and the top borders of the cells under it, which continue "
                + "it and are marked with ^ or left empty. The example is also a check of "
                + "the geometry of the cells: their backgrounds meet without gaps and their "
                + "borders line up.")
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
     * This will return a 10x10 matrix. The HTML-Like table will be like:
     * <table border="solid">
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * </tr>
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td>1x1</td>
     * <td colspan="5">5x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td colspan="3">3x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td rowspan="2">1x2</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="4" rowspan="4">4x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td rowspan="4">1x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * </table>
     *
     * @return
     * @throws Exception
     */
    private func createTableData(_ font: Font) -> [[Cell]] {
        var rows = [[Cell]]()
        for i in 0..<10 {
            var row = [Cell]()
            if i == 0 {
                row.append(getCell(font, 2, "2x2", true, false))
                row.append(getCell(font, 1,    "", true, false))
                row.append(getCell(font, 2, "2x1", true, true))
                row.append(getCell(font, 1,    "", true, false))
                row.append(getCell(font, 2, "2x1", true, true))
                row.append(getCell(font, 1,    "", true, false))
                row.append(getCell(font, 2, "2x1", true, true))
                row.append(getCell(font, 1,    "", true, false))
                row.append(getCell(font, 2, "2x1", true, true))
                row.append(getCell(font, 1,    "", true, false))
            } else if i == 1 {
                row.append(getCell(font, 2,   "^", false, true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 2, "2x2", true,  false))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 5, "5x1", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
            } else if i == 2 {
                row.append(getCell(font, 1, "1x2", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 2,   "^", false, true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 2, "2x2", true,  false))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 3, "3x1", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1, "1x1", true,  true))
            } else if i == 3 {
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1, "1x3", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 2,   "^", false, true))
                row.append(getCell(font, 1,    "", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 2, "2x1", true,  true))
                row.append(getCell(font, 1,    "", true,  false))
                row.append(getCell(font, 1, "1x2", true,  false))
            } else if i == 4 {
                row.append(getCell(font, 1, "1x2", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1,   "^", false, false))
                row.append(getCell(font, 2, "2x1", true,  true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 4, "4x4", true,  false))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,   "^", false, true))
            } else if i == 5 {
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x3", true,  false))
                row.append(getCell(font, 1, "1x3", true,  false))
                row.append(getCell(font, 4,   "^", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1, "1x3", true,  false))
            } else if i == 6 {
                row.append(getCell(font, 1, "1x2", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1, "1x4", true,  false))
                row.append(getCell(font, 1,   "^", false, false))
                row.append(getCell(font, 1,   "^", false, false))
                row.append(getCell(font, 4,   "^", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1,    "", false, false))
                row.append(getCell(font, 1,   "^", false, false))
            } else if i == 7 {
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1,   "^", false, false))
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 4,   "^", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,   "^", false, true))
            } else if i == 8 {
                row.append(getCell(font, 1, "1x2", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1,   "^", false, false))
                row.append(getCell(font, 2, "2x1", true,  true))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 2, "2x2", true,  false))
                row.append(getCell(font, 1,    "", true,  true))
                row.append(getCell(font, 1, "1x2", true,  false))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1, "1x1", true,  true))
            } else if i == 9 {
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 1, "1x1", true,  true))
                row.append(getCell(font, 2,   "^", false, true))
                row.append(getCell(font, 1,    "", false, true))
                row.append(getCell(font, 1,   "^", false, true))
                row.append(getCell(font, 1, "1x1", true, true))
                row.append(getCell(font, 1, "1x1", true, true))
            }
            rows.append(row)
        }
        return rows
    }

    private func getCell(
            _ font: Font,
            _ colSpan: Int,
            _ text: String,
            _ topBorder: Bool,
            _ bottomBorder: Bool) -> Cell {
        let cell = Cell(font, "")
        cell.setColSpan(colSpan)
        cell.setWidth(50.0)
        cell.setText(text)
        cell.setBorder(Border.TOP, topBorder)
        cell.setBorder(Border.BOTTOM, bottomBorder)
        cell.setTextAlignment(Alignment.CENTER)
        cell.setBackgroundColor(0xD8F0E4)     // A pastel mint
        cell.setBorderWidth(1.0)
        return cell
    }
}   // End of Example_38.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_38()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_38 => \(String(format: "%4lld", time1 - time0)) ms")
