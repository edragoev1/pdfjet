/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_23.swift
 *
 * A table of PDF/A-1a, tagged, over four pages: the life expectancy and the
 * children per woman of 217 countries and economies in 2023, from the World
 * Bank's World Development Indicators, with a marker on three of the rows,
 * each a link, and the longest and the shortest life in color.
 */
public class Example_23 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_23.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_A_1A)
        pdf.setTitle("Life Expectancy and Children per Woman, 2023")

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        let f2 = try Font(pdf, IBMPlexSans.Regular)
        let f3 = try Font(pdf, IBMPlexSans.BoldItalic)

        f1.setSize(7.0)
        f2.setSize(7.0)
        f3.setSize(7.0)

        let table = Table()
        // Country|Code|Life expectancy|Children per woman, after a header line
        let tableData = try getData(
                "data/life-expectancy-and-fertility-2023.txt", "|", 1, f1, f2)

        // A marker on three rows, found by their countries, each a link
        var p1 = Point()
        p1.setShape(Shape.CIRCLE)
        p1.setRadius(2.0)
        p1.setStrokeColor(Color.darkolivegreen)
        p1.setURIAction("https://data.worldbank.org/country/india")
        tableData[Example_23.rowOf(tableData, "India")][3].setMarker(p1, Alignment.RIGHT)

        p1 = Point()
        p1.setShape(Shape.DIAMOND)
        p1.setRadius(2.5)
        p1.setStrokeColor(Color.blue)
        p1.setURIAction("https://data.worldbank.org/country/japan")
        tableData[Example_23.rowOf(tableData, "Japan")][3].setMarker(p1, Alignment.RIGHT)

        p1 = Point()
        p1.setShape(Shape.STAR)
        p1.setRadius(3.0)
        p1.setStrokeColor(Color.red)
        p1.setURIAction("https://data.worldbank.org/country/united-states")
        tableData[Example_23.rowOf(tableData, "United States")][3].setMarker(p1, Alignment.RIGHT)

        // The longest life in blue, the shortest in red, and Canada in bold italic
        var longest = 1
        var shortest = 1
        for i in 1..<tableData.count {
            if Example_23.life(tableData, i) > Example_23.life(tableData, longest) {
                longest = i
            }
            if Example_23.life(tableData, i) < Example_23.life(tableData, shortest) {
                shortest = i
            }
        }

        table.setTableData(tableData, 1)
        table.setBottomMargin(26.0)
        // 400 points wide, in the middle of the page
        table.setLocation(106.0, 30.0)
        table.setTextColorInRow(longest, Color.blue)
        table.setTextColorInRow(shortest, Color.firebrick)
        table.setFontInRow(Example_23.rowOf(tableData, "Canada"), f3)
        table.setColumnWidth(0, 170.0)
        table.setColumnWidth(1, 50.0)
        table.setColumnWidth(2, 90.0)
        table.setColumnWidth(3, 90.0)
        table.rightAlignNumbers()

        var pages = [Page]()
        table.drawOn(pdf, &pages, Letter.PORTRAIT)
        for i in 0..<pages.count {
            let page = pages[i]
            page.addFooter(TextLine(f2, "Source: World Bank, World Development Indicators, "
                    + "SP.DYN.LE00.IN and SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0. "
                    + "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
        }

        try pdf.complete()
    }

    // The row of the country, in the first column.
    private static func rowOf(_ tableData: [[Cell]], _ country: String) -> Int {
        for i in 0..<tableData.count where tableData[i][0].getText() == country {
            return i
        }
        fatalError("\(country) is not in the table")
    }

    // The life expectancy of the row.
    private static func life(_ tableData: [[Cell]], _ row: Int) -> Float {
        return Float(tableData[row][2].getText()!)!
    }

    public func getData(
            _ fileName: String,
            _ delimiter: String,
            _ numOfHeaderRows: Int,
            _ f1: Font,
            _ f2: Font) throws -> [[Cell]] {
        var tableData = [[Cell]]()

        var currentRow: Int = 0
        let lines = (try String(contentsOfFile: fileName, encoding: .utf8))
                .replacingOccurrences(of: "\r\n", with: "\n")
                .components(separatedBy: "\n")

        for line in lines {
            if line.isEmpty {
                continue
            }

            var row = [Cell]()
            var cols: [String]?
            if delimiter == "|" {
                cols = line.components(separatedBy: "|")
            } else if delimiter == "\t" {
                cols = line.components(separatedBy: "\t")
            } else {
                print("Only pipes and tabs can be used as delimiters")
            }

            for i in 0..<cols!.count {
                let text = cols![i].trimmingCharacters(in: .whitespacesAndNewlines)
                var cell: Cell?
                if currentRow < numOfHeaderRows {
                    cell = Cell(f1, text)
                } else {
                    cell = Cell(f2, text)
                }
                cell!.setTopPadding(2.0)
                cell!.setBottomPadding(2.0)
                cell!.setLeftPadding(2.0)
                if i == 3 {
                    cell!.setRightPadding(10.0)
                } else {
                    cell!.setRightPadding(2.0)
                }
                row.append(cell!)
            }
            tableData.append(row)

            currentRow += 1
        }
        appendMissingCells(&tableData, f2)
        return tableData
    }

    private func appendMissingCells(_ tableData: inout [[Cell]], _ f2: Font) {
        let firstRow = tableData[0]
        let numOfColumns = firstRow.count
        for i in 0..<tableData.count {
            var dataRow = tableData[i]
            let dataRowColumns = dataRow.count
            if dataRowColumns < numOfColumns {
                for _ in 0..<(numOfColumns - dataRowColumns) {
                    dataRow.append(Cell(f2, ""))
                }
                tableData[i] = dataRow
                dataRow[dataRowColumns - 1].setColSpan(numOfColumns - dataRowColumns + 1)
            }
        }
    }
}   // End of Example_23.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_23()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_23 => \(String(format: "%4lld", time1 - time0)) ms")
