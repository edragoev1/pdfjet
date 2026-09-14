import Foundation
import PDFjet

///
/// Example_09.swift
///
struct ExampleError: Error, CustomStringConvertible {
    let message: String
    var description: String { message }
}

public class Example_09 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_09.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(8.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let chart = Chart(f1, f2)
        chart.setData(try getData("data/world-communications.txt", "|"))
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("World View - Communications")
        chart.setXAxisTitle("Cell phones per capita")
        chart.setYAxisTitle("Internet users % of the population")
        addTrendLine(chart)
        chart.drawOn(page)

        f1.setSize(7.0)
        f2.setSize(7.0)
        try addTableToChart(page, chart, f1, f2)

        try pdf.complete()
    }

    public func addTrendLine(_ chart: Chart) {
        let points = chart.getData()![0]

        let m = slope(points)
        let b = intercept(points, m)

        var trendLine = [Point]()
        var x: Float = 0.0
        var y: Float = m * x + b
        let p1 = Point(x, y)
        p1.setDrawPath(true)
        p1.setStrokeColor(Color.blue)
        p1.setShape(Shape.INVISIBLE)

        x = 1.5
        y = m * x + b
        let p2 = Point(x, y)
        p2.setShape(Shape.INVISIBLE)

        trendLine.append(p1)
        trendLine.append(p2)

        var chartData = chart.getData()!
        chartData.append(trendLine)
        chart.setData(chartData)
    }

    public func addTableToChart(
            _ page: Page,
            _ chart: Chart,
            _ f1: Font,
            _ f2: Font) throws {
        let table = Table()
        var tableData = [[Cell]]()
        let points = chart.getData()![0]
        for point in points {
            if point.getShape() != Shape.CIRCLE {
                var tableRow = [Cell]()

                point.setRadius(2.0)
                point.setAlignment(Alignment.LEFT)

                var cell = Cell(f2, "")
                cell.setMarker(point)
                cell.setText("")

                tableRow.append(cell)

                cell = Cell(f1, "")
                cell.setText(point.getText())
                tableRow.append(cell)

                cell = Cell(f2, "")
                cell.setText(point.getURIAction())
                tableRow.append(cell)

                tableData.append(tableRow)
            }
        }
        table.setData(tableData)
        table.autoAdjustColumnWidths()
        table.setCellBorderWidth(0.2)
        table.setLocation(70.0, 360.0)
        table.setColumnWidth(0, 9.0)
        table.drawOn(page)
    }

    public func getData(
            _ fileName: String,
            _ delimiter: String) throws -> [[Point]] {
        var chartData = [[Point]]()
        var points = [Point]()

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

            var country_name = cols![0].trimmingCharacters(in: .whitespacesAndNewlines)
            let population = Double(cols![1].filter({ $0 != "," }))
            let x = Double(cols![5].filter({ $0 != "," }))
            let y = Double(cols![7].filter({ $0 != "," }).trimmingCharacters(in: .whitespacesAndNewlines))

            if population != nil && x != nil && y != nil {
                let point = Point()
                point.setText(country_name)
                country_name = country_name.replacingOccurrences(of: " ", with: "_")
                country_name = country_name.replacingOccurrences(of: "'", with: "_")
                country_name = country_name.replacingOccurrences(of: ",", with: "_")
                country_name = country_name.replacingOccurrences(of: "(", with: "_")
                country_name = country_name.replacingOccurrences(of: ")", with: "_")
                point.setURIAction("http://pdfjet.com/country/\(country_name).txt")
                point.setX(Float(x! / population!))
                point.setY(Float(y! / population! * 100.0))

                point.setRadius(2.0)
                point.setStrokeColor(Color.gray)

                if point.getX() > 1.25 {
                    point.setShape(Shape.RIGHT_ARROW)
                    point.setStrokeColor(Color.black)
                } else if point.getY() > 80.0 {
                    point.setShape(Shape.UP_ARROW)
                    point.setStrokeColor(Color.blue)
                } else if point.getText() == "France" {
                    point.setShape(Shape.MULTIPLY)
                    point.setStrokeColor(Color.green)
                } else if point.getText() == "Canada" {
                    point.setShape(Shape.BOX)
                    point.setStrokeColor(Color.orange)
                } else if point.getText()!.hasPrefix("United States") {
                    point.setShape(Shape.STAR)
                    point.setStrokeColor(Color.red)
                }
                points.append(point)
            }
        }
        chartData.append(points)

        return chartData
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
print("Example_09 => \(time1 - time0) ms")
