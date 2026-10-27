/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_26.swift
 * This example draws a SimpleScatterChart of the life expectancy and the
 * children per woman of 217 countries and economies, from the World Bank's
 * data of 2023: a gray dot for each, seven of them named, and the trend line,
 * with a table of the named ones under the chart.
 */
public class Example_26 {
    // The countries named on the chart, by their codes
    static let named: Set<String> = ["BRA", "CAN", "IND", "JPN", "NER", "NGA", "USA"]

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_26.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Life Expectancy and Children per Woman")

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        f1.setSize(12.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(9.0)

        // Country|Code|Life expectancy|Children per woman, after a header line
        let lines = try Content.linesOfTextFile("data/life-expectancy-and-fertility-2023.txt")
        let total = lines.count - 1

        let page = Page(pdf, Letter.PORTRAIT)

        TextLine(f1, "Life Expectancy and Children per Woman")
                .setStructureType(StructElem.H1).setFontSize(20.0)
                .setLocation(50.0, 70.0).drawOn(page)
        let intro = [Paragraph(TextLine(f2, "Where women have fewer children, people live longer: the "
                + "\(total) countries and economies of the World Bank's data of 2023, each a gray dot, "
                + "seven of them named, each a link to its page of the World Bank's data, and the trend line "
                + "through them, with the named ones in the table "
                + "under the chart.").setFontSize(10.0))]
        let frame = TextFrame(intro).setLocation(50.0, 84.0).setWidth(512.0)
        frame.drawOn(page)
        var y = frame.getTextBottom() + 18.0

        let chart = SimpleScatterChart(f1, f2)
                .setTitle("Life expectancy and children per woman")
                .setSubtitle("\(total) countries and economies, 2023")
                .setXAxisTitle("Children per woman")
                .setYAxisTitle("Life expectancy at birth, years")
                .setTrendLine(true)
        var data = [[Cell]]()
        data.append(Example_26.row(f1, true, "Country", "Children per woman", "Life expectancy"))
        for line in lines[1...] {
            let field = line.components(separatedBy: "|")
            let life = Float(field[2]) ?? 0
            let children = Float(field[3]) ?? 0
            if Example_26.named.contains(field[1]) {
                chart.add(field[0], children, life, Example_26.worldBank(field[0]))   // A navy dot, named, a link
                // The figures as the file writes them, two decimals and one,
                // not rounded again, so that every port writes them alike
                data.append(Example_26.row(f2, false, field[0], Example_26.decimals(field[3], 2),
                        Example_26.decimals(field[2], 1) + " years"))
            } else {
                chart.add(children, life)               // A gray dot
            }
        }
        chart.setLocation(50.0, y).setSize(512.0, 340.0)
        y = chart.drawOn(page)[1] + 30.0

        // The countries named on the chart
        TextLine(f1, "The countries named on the chart").setStructureType(StructElem.H2)
                .setFontSize(14.0).setLocation(50.0, y).drawOn(page)
        let table = Table()
        table.setTableData(data, 1)
        table.setColumnWidth(0, 232.0)
        table.setColumnWidth(1, 140.0)
        table.setColumnWidth(2, 140.0)
        table.setLocation(50.0, y + 10.0)
        y = table.drawOn(page)[1] + 16.0

        TextLine(f2, "Source: World Bank, World Development Indicators, SP.DYN.LE00.IN and "
                + "SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0.")
                .setFontSize(8.0).setTextColor(0x5F6873).setLocation(50.0, y).drawOn(page)

        try pdf.complete()
    }

    // The page of the country in the World Bank's data: its name in lower
    // case, a hyphen for each space, as data.worldbank.org/country/united-states
    static func worldBank(_ country: String) -> String {
        return "https://data.worldbank.org/country/" + country.lowercased().replacingOccurrences(of: " ", with: "-")
    }

    // The number of the text with the decimals, zeros added: 1.5 is 1.50.
    static func decimals(_ number: String, _ count: Int) -> String {
        let units = Array(number.utf16)
        let point = units.firstIndex(of: 0x2E)      // "."
        var text = (point == nil) ? number + "." : number
        let have = (point == nil) ? 0 : units.count - point! - 1
        var i = have
        while i < count {
            text += "0"
            i += 1
        }
        return text
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the chart: a light band behind the header row, a thin gray
    // line under each row, and the numbers, the cells after the first,
    // aligned right.
    static func row(_ font: Font, _ header: Bool, _ texts: String...) -> [Cell] {
        var row = [Cell]()
        for (i, text) in texts.enumerated() {
            let cell = Cell(font, text).setFontSize(9.0)
            cell.setTopPadding(5.0).setBottomPadding(5.0).setLeftPadding(6.0).setRightPadding(6.0)
            cell.setBorders(false).setBorder(Border.BOTTOM, true)
            cell.setBorderWidth(0.5).setBorderColor(0xD5D9DE)
            if header {
                cell.setBackgroundColor(0xF1F3F6)
            }
            if i > 0 {
                cell.setTextAlignment(Alignment.RIGHT)
            }
            row.append(cell)
        }
        return row
    }
}   // End of Example_26.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_26()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_26 => \(String(format: "%4lld", time1 - time0)) ms")
