import Foundation
import PDFjet

/**
 *  Example_14.swift
 *
 */
public class Example_14 {
    public init() {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_14.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f2 = Font(pdf, CoreFont.HELVETICA)
        f1.setSize(7.0)
        f2.setSize(7.0)

        let page = Page(pdf, A4.PORTRAIT)

        let table = Table()
        // table.setCellMargin(10.0)

        var tableData = [[Cell]]()
        var row: [Cell]?
        var cell: Cell?
        for i in 0..<5 {
            row = [Cell]()
            for j in 0..<5 {
                if i == 0 {
                    cell = Cell(f1)
                } else {
                    cell = Cell(f2)
                }
                cell!.setNoBorders()

                // WITH:
                cell!.setTopPadding(10.0)
                cell!.setBottomPadding(10.0)
                cell!.setLeftPadding(10.0)
                cell!.setRightPadding(10.0)

                cell!.setText("Hello \(i) \(j)")
                if i == 0 {
                    cell!.setBorder(Border.TOP, true)
                    cell!.setUnderline(true)
                    cell!.setUnderline(false)
                }
                if i == 4 {
                    cell!.setBorder(Border.BOTTOM, true)
                }
                if j == 0 {
                    cell!.setBorder(Border.LEFT, true)
                }
                if j == 4 {
                    cell!.setBorder(Border.RIGHT, true)
                }

                if i == 2 && j == 2 {
                    cell!.setBorder(Border.TOP, true)
                    cell!.setBorder(Border.BOTTOM, true)
                    cell!.setBorder(Border.LEFT, true)
                    cell!.setBorder(Border.RIGHT, true)

                    cell!.setColSpan(3)
                    cell!.setBgColor(Color.darkseagreen)
                    cell!.setLineWidth(1.0)
                    cell!.setTextAlignment(Align.RIGHT)
                }

                row!.append(cell!)
            }
            tableData.append(row!)
        }

        table.setData(tableData)
        table.setCellBordersWidth(0.2)
        table.setLocation(70.0, 30.0)
        table.drawOn(page)

        // Must call this method before drawing the table again.
        table.resetRenderedPagesCount()
        table.setLocation(70.0, 200.0)
        table.drawOn(page)

        pdf.complete()
    }
}   // End of Example_14.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = Example_14()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_14", time0, time1)
