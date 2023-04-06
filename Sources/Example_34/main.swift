import Foundation
import PDFjet

///
/// Example_34.swift
///
public class Example_34 {
    public init() throws {
        if let stream = OutputStream(toFileAtPath: "Example_34.pdf", append: false) {

            let pdf = PDF(stream)

            let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
            let f2 = Font(pdf, CoreFont.HELVETICA)
            let f3 = Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE)

            f1.setSize(7.0)
            f2.setSize(7.0)
            f3.setSize(7.0)

            let table = Table()
            let tableData = try getData(
            		"data/world-communications.txt", "|", Table.DATA_HAS_2_HEADER_ROWS, f1, f2)

            var p1 = Point()
            p1.setShape(Point.CIRCLE)
            p1.setRadius(2.0)
            p1.setColor(Color.darkolivegreen)
            p1.setFillShape(true)
            p1.setAlignment(Align.RIGHT)
            p1.setURIAction("https://en.wikipedia.org/wiki/India")
            tableData[4][3].setPoint(p1)

            p1 = Point()
            p1.setShape(Point.DIAMOND)
            p1.setRadius(2.5)
            p1.setColor(Color.blue)
            p1.setFillShape(true)
            p1.setAlignment(Align.RIGHT)
            p1.setURIAction("https://en.wikipedia.org/wiki/European_Union")
            tableData[5][3].setPoint(p1)

            p1 = Point()
            p1.setShape(Point.STAR)
            p1.setRadius(3.0)
            p1.setColor(Color.red)
            p1.setFillShape(true)
            p1.setAlignment(Align.RIGHT)
            p1.setURIAction("https://en.wikipedia.org/wiki/United_States")
            tableData[6][3].setPoint(p1)

            table.setData(tableData, Table.DATA_HAS_2_HEADER_ROWS);
            table.setBottomMargin(15.0)
            // table.setCellBordersWidth(1.2)
            table.setCellBordersWidth(0.2)
            table.setLocation(70.0, 30.0);
            table.setTextColorInRow(6, Color.blue)
            table.setTextColorInRow(39, Color.red)
            table.setFontInRow(26, f3)
            table.removeLineBetweenRows(0, 1)
            table.autoAdjustColumnWidths()
            // table.setColumnWidth(0, 120f)
            table.setColumnWidth(0, 50.0);
            table.wrapAroundCellText();
            table.rightAlignNumbers();

            var pages = [Page]()
            table.drawOn(pdf, &pages, Letter.PORTRAIT)
            for i in 0..<pages.count {
                let page = pages[i]
                // try page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)"))
                pdf.addPage(page)
            }

            pdf.complete()
        }
    }


    private func appendMissingCells(_ tableData: [[Cell]], _ f2: Font) {
        let firstRow = tableData[0]
        let numOfColumns = firstRow.count
        for i in 0..<tableData.count {
            var dataRow = tableData[i]
            let dataRowColumns = dataRow.count
            if dataRowColumns < numOfColumns {
                for _ in 0..<(numOfColumns - dataRowColumns) {
                    dataRow.append(Cell(f2))
                }
                dataRow[dataRowColumns - 1].setColSpan(UInt32(numOfColumns - dataRowColumns) + 1)
            }
        }
    }


    public func getData(
            _ fileName: String,
            _ delimiter: String,
            _ numOfHeaderRows: Int,
            _ f1: Font,
            _ f2: Font) throws -> [[Cell]] {

        var tableData = [[Cell]]()

        var currentRow: Int = 0
        let lines = (try String(contentsOfFile:
                fileName, encoding: .utf8)).components(separatedBy: "\n")

        for line in lines {
            if line.isEmpty {
                continue
            }

            var row = [Cell]()
            var cols: [String]?
            if delimiter == "|" {
                cols = line.components(separatedBy: "|")
            }
            else if delimiter == "\t" {
                cols = line.components(separatedBy: "\t")
            }
            else {
                print("Only pipes and tabs can be used as delimiters")
            }

            for i in 0..<cols!.count {
                let text = cols![i].trimmingCharacters(in: .whitespacesAndNewlines)
                var cell: Cell?
                if currentRow < numOfHeaderRows {
                    cell = Cell(f1, text)
                }
                else {
                    cell = Cell(f2, text)
                }
                cell!.setTopPadding(2.0)
                cell!.setBottomPadding(2.0)
                cell!.setLeftPadding(2.0)
                if i == 3 {
                    cell!.setRightPadding(10.0)
                }
                else {
                    cell!.setRightPadding(2.0)
                }
                row.append(cell!)
            }
            tableData.append(row)

            currentRow += 1
        }

        appendMissingCells(tableData, f2)

        return tableData
    }


}   // End of Example_34.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_34()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_34 => \(time1 - time0)")
