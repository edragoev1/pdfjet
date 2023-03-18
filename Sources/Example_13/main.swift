import Foundation
import PDFjet


/**
 *  Example_13.swift
 *
 */
public class Example_13 {

    public init() throws {

        if let stream = OutputStream(toFileAtPath: "Example_13.pdf", append: false) {

            let pdf = PDF(stream)

            let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
            let f2 = Font(pdf, CoreFont.HELVETICA)
            f1.setSize(7.0)
            f2.setSize(7.0)

            var tableData = [[Cell]]()
            let text = (try String(contentsOfFile:
                    "data/winter-2009.txt", encoding: .utf8)).trimmingCharacters(in: .newlines)
            let lines = text.components(separatedBy: "\n")
            for line1 in lines {
                let line = line1.trimmingCharacters(in: .newlines) 
                var row = [Cell]()
                let columns = line.components(separatedBy: "|")
                for i in 0..<columns.count {
                    let cell = Cell(f2, String(columns[i]))
                    cell.setTopPadding(2.0)
                    cell.setBottomPadding(2.0)
                    cell.setLeftPadding(2.0)
                    cell.setRightPadding(2.0)
                    row.append(cell)
                }
                tableData.append(row)
            }

            let table = Table()
            table.setData(tableData, Table.DATA_HAS_2_HEADER_ROWS)
            table.setLocation(100.0, 50.0)

            // REPLACED:
            // table.setCellMargin(2.0)

            setFontForRow(table, 0, f1)
            setFontForRow(table, 1, f1)

            table.autoAdjustColumnWidths()

            table.removeLineBetweenRows(0, 1)

            var cell = table.getCellAt(1, 1)
            cell.setBorder(Border.TOP, true)

            cell = table.getCellAt(1, 2)
            cell.setBorder(Border.TOP, true)

            cell = table.getCellAt(0, 1)
            cell.setColSpan(2)
            cell.setTextAlignment(Align.CENTER)

            var column = table.getColumnAtIndex(7)
            for i in 0..<column.count {
                cell = column[i]
                cell.setTextAlignment(Align.CENTER)
            }

            column = table.getColumnAtIndex(4)
            for i in 2..<column.count {
                cell = column[i]
                cell.setTextAlignment(Align.CENTER)
                if Int(cell.getText()!)! > 40 {
                    cell.setBgColor(Color.darkseagreen)
                }
                else {
                    cell.setBgColor(Color.yellow)
                }
            }

            column = table.getColumnAtIndex(2)
            for i in 2..<column.count {
                cell = column[i]
                if cell.getText() == "Smith" {
                    cell.setUnderline(true)
                }
                if cell.getText() == "Bowden" {
                    cell.setStrikeout(true)
                }
            }

            column = table.getColumnAtIndex(2)
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

            var page = Page(pdf, Letter.PORTRAIT)
            let numOfPages = try table.getNumberOfPages(page)
            var pageNumber = 1
            while true {
                table.drawOn(page)

                let text = TextLine(f1)
                text.setText("Page \(pageNumber) of \(numOfPages)")
                text.setLocation(300.0, 780.0)
                text.drawOn(page)

                pageNumber += 1

                if !table.hasMoreData() {
                    table.resetRenderedPagesCount()
                    break
                }

                page = Page(pdf, Letter.PORTRAIT)
            }

            pdf.complete()
        }
    }


    public func blankOutColumn(_ table: Table, _ index: Int) {
        let column = table.getColumnAtIndex(index)
        for cell in column {
            cell.setBgColor(Color.white)
            cell.setBorder(Border.TOP, false)
            cell.setBorder(Border.BOTTOM, false)
        }
    }


    public func setBgColorForRow(
            _ table: Table,
            _ index: Int,
            _ color: Int32) {
        let row = table.getRowAtIndex(index)
        for cell in row {
            cell.setBgColor(color)
        }
    }


    public func setFontForRow(
            _ table: Table,
            _ index: Int,
            _ font: Font) {
        let row = table.getRowAtIndex(index)
        for cell in row {
            cell.setFont(font)
        }
    }

}   // End of Example_13.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_13()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_13 => \(time1 - time0)")
