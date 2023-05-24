import Foundation
import PDFjet

/**
 *  Example_15.swift
 */
public class Example_15 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_15.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f2 = Font(pdf, CoreFont.HELVETICA)
        let f3 = Font(pdf, CoreFont.HELVETICA)
        let f4 = Font(pdf, CoreFont.HELVETICA_BOLD)
        let f5 = Font(pdf, CoreFont.HELVETICA)

        var tableData = [[Cell]]()
        var row = [Cell]()
        var cell = Cell(f1)
        for i in 0..<60 {
            row = [Cell]()
            for j in 0..<5 {
                if i == 0 {
                    cell = Cell(f1)
                } else {
                    cell = Cell(f2)
                }
                // cell.setNoBorders()
                cell.setTopPadding(10.0)
                cell.setBottomPadding(10.0)
                cell.setLeftPadding(10.0)
                cell.setRightPadding(10.0)
                cell.setText("Hello \(i) \(j)")

                let composite = CompositeTextLine(0.0, 0.0)
                composite.setFontSize(12.0)
                let line1 = TextLine(f3, "H")
                let line2 = TextLine(f4, "2")
                let line3 = TextLine(f5, "O")
                line2.setTextEffect(Effect.SUBSCRIPT)
                composite.addComponent(line1)
                composite.addComponent(line2)
                composite.addComponent(line3)
                if i == 0 || j == 0 {
                    cell.setCompositeTextLine(composite)
                    cell.setBgColor(Color.deepskyblue)
                } else {
                    cell.setBgColor(Color.dodgerblue)
                }
                cell.setPenColor(Color.lightgray)
                cell.setBrushColor(Color.black)
                row.append(cell)
            }
            tableData.append(row)
        }

        let table = Table()
        table.setData(tableData, Table.WITH_2_HEADER_ROWS)
        table.setCellBordersWidth(0.2)
        table.setLocation(70.0, 30.0)
        table.setColumnWidths()
        var pages = [Page]()
        let xy = table.drawOn(pdf, &pages, Letter.PORTRAIT)
        for i in 0..<pages.count {
            let page = pages[i]
            if i == pages.count - 1 {
                let textLine = TextLine(f2, "xy coordinate of table")
                textLine.setLocation(xy[0] + table.getWidth(), xy[1])
                textLine.drawOn(page)
            }
            try page.addFooter(TextLine(f2, "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
        }

        pdf.complete()
    }
}   // End of Example_15.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_15()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_15", time0, time1)
