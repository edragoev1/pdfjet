import Foundation
import PDFjet

/**
 *  Example_38.swift
 */
public class Example_38 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_38.pdf", append: false)!)
        let font = Font(pdf, CoreFont.COURIER)
        let page = Page(pdf, Letter.LANDSCAPE)

        let table = Table()
        table.setData(createTableData(font))
        table.setBottomMargin(10.0)
        table.setLocation(50.0, 50.0)
        table.mergeOverlaidBorders()
        table.drawOn(page)

        pdf.complete()
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
            }
            else if i == 1 {
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
            }
            else if i == 2 {
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
            }
            else if i == 3 {
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
            }
            else if i == 4 {
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
            }
            else if i == 5 {
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
            }
            else if i == 6 {
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
            }
            else if i == 7 {
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
            }
            else if i == 8 {
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
            }
            else if i == 9 {
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
            _ colSpan: UInt32,
            _ text: String,
            _ topBorder: Bool,
            _ bottomBorder: Bool) -> Cell {
        let cell = Cell(font)
        cell.setColSpan(colSpan)
        cell.setWidth(50.0)
        cell.setText(text)
        cell.setBorder(Border.TOP, topBorder)
        cell.setBorder(Border.BOTTOM, bottomBorder)
        cell.setTextAlignment(Align.CENTER)
        cell.setBgColor(Color.lightblue)
        cell.setLineWidth(0.5)
        return cell
    }
}   // End of Example_38.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_38()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_38", time0, time1)
