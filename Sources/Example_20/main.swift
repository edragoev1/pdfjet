/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_20.swift
 * This example draws a page of results from a CSV file of electric vehicles:
 * two simple bar charts, the five makes and the model years, and under them a
 * table of the same numbers, then a table of one car with its photo and its
 * VIN as a barcode, in their cells.
 */
public class Example_20 {
    static let makes = ["TESLA", "NISSAN", "CHEVROLET", "BMW", "FORD"]
    static let names = ["Tesla", "Nissan", "Chevrolet", "BMW", "Ford"]
    static let firstYear = 2019
    static let lastYear = 2023

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_20.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicles by Make and Model Year")

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        f1.setSize(11.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(9.0)

        // The cars of the file: by make, the battery electric ones and the
        // electric range of those that give one, and by model year
        let lines = try Content.linesOfTextFile("resources/data/Electric_Vehicle_Population_10_Pages.csv")
        let makes = Example_20.makes
        var cars = [Int](repeating: 0, count: makes.count)
        var battery = [Int](repeating: 0, count: makes.count)
        var ranges = [Int](repeating: 0, count: makes.count)
        var ranged = [Int](repeating: 0, count: makes.count)
        var years = [Int](repeating: 0, count: Example_20.lastYear - Example_20.firstYear + 1)
        var modelX: [String]? = nil     // The first Tesla Model X of the file
        for line in lines[1...] {
            let field = line.components(separatedBy: ",")
            let year = Int(field[5]) ?? 0
            if year >= Example_20.firstYear && year <= Example_20.lastYear {
                years[year - Example_20.firstYear] += 1
            }
            if let i = makes.firstIndex(of: field[6]) {
                cars[i] += 1
                if field[8].hasPrefix("Battery") {
                    battery[i] += 1
                }
                let range = Int(field[10]) ?? 0
                if range > 0 {
                    ranges[i] += range
                    ranged[i] += 1
                }
            }
            if modelX == nil && field[7] == "MODEL X" {
                modelX = field
            }
        }
        let total = lines.count - 1

        let page = Page(pdf, Letter.PORTRAIT)

        TextLine(f1, "Electric Vehicles by Make and Model Year")
                .setStructureType(StructElem.H1).setFontSize(20.0)
                .setLocation(50.0, 70.0).drawOn(page)
        let intro = [Paragraph(TextLine(f2, "The \(total) electric vehicles of the first rows of "
                + "the data file that Example_24 draws whole: the five makes with the most cars, and the "
                + "cars of each model year from \(Example_20.firstYear) to \(Example_20.lastYear), with the numbers "
                + "in the table under the charts.").setFontSize(10.0))]
        let frame = TextFrame(intro).setLocation(50.0, 84.0).setWidth(512.0)
        frame.drawOn(page)
        var y = frame.getTextBottom() + 18.0

        let byMake = SimpleBarChart(f1, f2).setTitle("The five makes").setSubtitle("cars").setHighlight(0)
        for i in 0..<makes.count {
            byMake.add(Example_20.names[i], Float(cars[i]))
        }
        byMake.setLocation(50.0, y).setWidth(248.0).drawOn(page)

        // The chart highlights the busiest year itself, and marks the
        // average, in whole cars, with a dashed line; its bars are rounded
        let byYear = SimpleBarChart(f1, f2).setTitle("By model year").setSubtitle("cars")
                .setHighlightHighest().setRoundedBars(true)
        var allYears = 0
        for i in 0..<years.count {
            byYear.add(String(Example_20.firstYear + i), Float(years[i]))
            allYears += years[i]
        }
        byYear.setReferenceLine(Float(allYears / years.count), "Average")
        byYear.setLocation(314.0, y).setWidth(248.0).drawOn(page)
        y += max(byMake.getHeight(), byYear.getHeight()) + 30.0

        // The numbers of the charts
        TextLine(f1, "The five makes").setStructureType(StructElem.H2)
                .setFontSize(14.0).setLocation(50.0, y).drawOn(page)
        var data = [[Cell]]()
        data.append(Example_20.row(f1, true, "Make", "Cars", "Share of all", "Battery electric", "Average range"))
        for i in 0..<makes.count {
            let share = Float(100 * cars[i]) / Float(total)
            data.append(Example_20.row(f2, false, Example_20.names[i], String(cars[i]),
                    String(format: "%.1f%%", Double(share)), String(battery[i]),
                    ranged[i] == 0 ? "" : "\(ranges[i] / ranged[i]) mi"))
        }
        var table = Table()
        table.setTableData(data, 1)
        let widths: [Float] = [142.0, 80.0, 90.0, 100.0, 100.0]
        for i in 0..<widths.count {
            table.setColumnWidth(i, widths[i])
        }
        table.setLocation(50.0, y + 10.0)
        y = table.drawOn(page)[1] + 30.0

        // One car of the data, its photo and its VIN in their cells
        TextLine(f1, "One car of the data").setStructureType(StructElem.H2)
                .setFontSize(14.0).setLocation(50.0, y).drawOn(page)
        let image = try Image(pdf, "images/TeslaX.png")
        image.setAltDescription("A white Tesla Model X with a black glass roof, seen from the front left.")
        image.scaleBy(1.0 / 3.0)    // The image is 120 DPI, so 0.6 of its pixels
        let vin = try Barcode(Barcode.CODE_128, modelX![0])
        vin.setModuleLength(0.75)
        vin.setFont(f2)
        data = [[Cell]]()
        data.append(Example_20.row(f1, true, "Car", "Model", "VIN"))
        for cell in data[0] {
            cell.setTextAlignment(Alignment.LEFT)   // Text, not numbers
        }
        let car = Example_20.row(f2, false, "", "Tesla Model X, \(modelX![5]), \(modelX![2]), "
                + "\(modelX![10]) mi of range", "")
        for cell in car {
            cell.setTextAlignment(Alignment.LEFT)
        }
        car[0].setImage(image)
        car[2].setBarcode(vin)
        data.append(car)
        table = Table()
        table.setTableData(data, 1)
        table.setColumnWidth(0, image.getWidth() + 8.0)
        table.setColumnWidth(1, 220.0)
        table.setColumnWidth(2, 512.0 - image.getWidth() - 228.0)
        table.setLocation(50.0, y + 10.0)
        table.drawOn(page)

        try pdf.complete()
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the charts: a light band behind the header row, a thin gray
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
}   // End of Example_20.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_20()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_20 => \(String(format: "%4lld", time1 - time0)) ms")
