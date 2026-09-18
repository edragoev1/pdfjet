/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

///
/// Example_09.swift
///
/// Draws an XY chart of the countries of a data file, each a marker that links
/// to a page about the country, with the trend line of the points, and a table
/// of the countries with a marker of their own.
///
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

/// A country of the data file: its name and its point on the chart.
struct Country {
    let name: String
    let point: Point
}

public class Example_09 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_09.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("World View - Communications")

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(8.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let countries = try readCountries("data/world-communications.txt", "|")

        let chart = Chart(f1, f2)
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("World View - Communications")
        chart.setXAxisTitle("Cell phones per capita")
        chart.setYAxisTitle("Internet users % of the population")
        let markers = chart.addSeries("").setStrokeColor(Color.gray)
        for country in countries {
            markers.addPoint(country.point)
        }
        addTrendLine(chart, countries)
        chart.drawOn(page)

        f1.setSize(7.0)
        f2.setSize(7.0)
        try addTableToChart(page, countries, f1, f2)

        try pdf.complete()
    }

    func addTrendLine(_ chart: Chart, _ countries: [Country]) {
        let points = countries.map { $0.point }
        let m = slope(points)
        let b = intercept(points, m)
        chart.addSeries("Trend line")
                .setDrawPath(true)
                .setStrokeColor(Color.blue)
                .setShape(Shape.INVISIBLE)
                .addPoint(0.0, b)
                .addPoint(1.5, m * 1.5 + b)
    }

    func addTableToChart(
            _ page: Page,
            _ countries: [Country],
            _ f1: Font,
            _ f2: Font) throws {
        let table = Table()
        var tableData = [[Cell]]()
        for country in countries {
            if country.point.getShape() != Shape.CIRCLE {
                var tableRow = [Cell]()

                var cell = Cell(f2, "")
                cell.setMarker(country.point, Alignment.LEFT)
                tableRow.append(cell)

                cell = Cell(f1, country.name)
                tableRow.append(cell)

                cell = Cell(f2, country.point.getURIAction())
                tableRow.append(cell)

                tableData.append(tableRow)
            }
        }
        table.setTableData(tableData)
        table.autoAdjustColumnWidths()
        table.setCellBorderWidth(0.2)
        table.setLocation(70.0, 360.0)
        table.setColumnWidth(0, 9.0)
        table.drawOn(page)
    }

    func readCountries(
            _ fileName: String,
            _ delimiter: String) throws -> [Country] {
        var countries = [Country]()

        let text = (try String(contentsOfFile:
                fileName, encoding: .utf8)).trimmingCharacters(in: .newlines)
        let lines = text.replacingOccurrences(of: "\r\n", with: "\n").components(separatedBy: "\n")
        for line1 in lines {
            let line = line1.trimmingCharacters(in: .whitespacesAndNewlines)
            var cols: [String]?
            if delimiter == "|" {
                cols = line.components(separatedBy: "|")
            } else if delimiter == "\t" {
                cols = line.components(separatedBy: "\t")
            } else {
                throw ExampleError(
                        message: "Only pipes and tabs can be used as delimiters")
            }

            let name = cols![0].trimmingCharacters(in: .whitespacesAndNewlines)
            let population = Double(cols![1].filter({ $0 != "," }))
            let x = Double(cols![5].filter({ $0 != "," }))
            let y = Double(cols![7].filter({ $0 != "," }).trimmingCharacters(in: .whitespacesAndNewlines))

            if population != nil && x != nil && y != nil {
                var country_name = name
                country_name = country_name.replacingOccurrences(of: " ", with: "_")
                country_name = country_name.replacingOccurrences(of: "'", with: "_")
                country_name = country_name.replacingOccurrences(of: ",", with: "_")
                country_name = country_name.replacingOccurrences(of: "(", with: "_")
                country_name = country_name.replacingOccurrences(of: ")", with: "_")
                let point = Point(Float(x! / population!), Float(y! / population! * 100.0))
                point.setURIAction("http://pdfjet.com/country/\(country_name).txt")
                point.setRadius(2.0)

                if point.getX() > 1.25 {
                    point.setShape(Shape.RIGHT_ARROW)
                    point.setStrokeColor(Color.black)
                } else if point.getY() > 80.0 {
                    point.setShape(Shape.UP_ARROW)
                    point.setStrokeColor(Color.blue)
                } else if name == "France" {
                    point.setShape(Shape.MULTIPLY)
                    point.setStrokeColor(Color.green)
                } else if name == "Canada" {
                    point.setShape(Shape.BOX)
                    point.setStrokeColor(Color.orange)
                } else if name.hasPrefix("United States") {
                    point.setShape(Shape.STAR)
                    point.setStrokeColor(Color.red)
                }
                countries.append(Country(name: name, point: point))
            }
        }
        return countries
    }

    // The slope and intercept of the ordinary least squares trend line of the points.
    private func slope(_ points: [Point]) -> Float {
        return covar(points) / devsq(points) * Float(points.count - 1)
    }

    private func intercept(_ points: [Point], _ slope: Float) -> Float {
        let m = mean(points)
        return m[1] - slope * m[0]
    }

    private func mean(_ points: [Point]) -> [Float] {
        var m = [Float](repeating: 0, count: 2)
        for point in points {
            m[0] += point.getX()
            m[1] += point.getY()
        }
        let n = Float(points.count)
        m[0] /= n
        m[1] /= n
        return m
    }

    private func covar(_ points: [Point]) -> Float {
        var covariance: Float = 0.0
        let m = mean(points)
        for point in points {
            covariance += (point.getX() - m[0]) * (point.getY() - m[1])
        }
        return covariance / Float(points.count - 1)
    }

    private func devsq(_ points: [Point]) -> Float {
        var sum: Float = 0.0
        let m = mean(points)
        for point in points {
            sum += Float(pow(Double(point.getX() - m[0]), 2))
        }
        return sum
    }
}   // End of Example_09.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_09()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_09 => \(String(format: "%4lld", time1 - time0)) ms")
