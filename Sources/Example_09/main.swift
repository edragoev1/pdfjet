import Foundation
import PDFjet

///
/// Example_09.swift
///
public class Example_09 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_09.pdf", append: false)!)
        let page = Page(pdf, Letter.PORTRAIT)

        // let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        // let f2 = Font(pdf, CoreFont.HELVETICA)

        let f1 = try Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream")
        let f2 = try Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")

        f1.setSize(8.0)
        f2.setSize(8.0)

        var chartData = [[Point]]()

        var path1 = [Point]()
        path1.append(Point(50.0, 50.0).setDrawPath().setColor(Color.blue))
        path1.append(Point(55.0, 55.0))
        path1.append(Point(60.0, 60.0))
        path1.append(Point(65.0, 58.0))
        path1.append(Point(70.0, 59.0))
        path1.append(Point(75.0, 63.0))
        path1.append(Point(80.0, 65.0))
        chartData.append(path1)

        var path2 = [Point]()
        path2.append(Point(50.0, 30.0).setDrawPath().setColor(Color.red))
        path2.append(Point(55.0, 35.0))
        path2.append(Point(60.0, 40.0))
        path2.append(Point(65.0, 48.0))
        path2.append(Point(70.0, 49.0))
        path2.append(Point(75.0, 53.0))
        path2.append(Point(80.0, 55.0))
        chartData.append(path2)

        var path3 = [Point]()
        path3.append(Point(50.0, 80.0).setDrawPath().setColor(Color.green))
        path3.append(Point(55.0, 70.0))
        path3.append(Point(60.0, 60.0))
        path3.append(Point(65.0, 55.0))
        path3.append(Point(70.0, 59.0))
        path3.append(Point(75.0, 63.0))
        path3.append(Point(80.0, 61.0))
        chartData.append(path3)

        let chart = Chart(f1, f2)
        chart.setData(try getData("data/world-communications.txt", "|"))
        // chart.setData(chartData)
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("World View - Communications")
        chart.setXAxisTitle("Cell phones per capita")
        chart.setYAxisTitle("Internet users % of the population")
        addTrendLine(chart)
        // chart.setXAxisMinMax(0.0, 100.0, 10)
        // chart.setYAxisMinMax(0.0, 100.0, 10)
        chart.drawOn(page)

        // f1.setSize(7.0)
        // f2.setSize(7.0)
        // try addTableToChart(page, chart, f1, f2)
        pdf.complete()
    }
}

public func addTrendLine(_ chart: Chart) {
    let points = chart.getData()![0]

    let m = chart.slope(points)
    let b = chart.intercept(points, m)

    var trendline = [Point]()
    var x: Float = 0.0
    var y: Float = m * x + b
    let p1 = Point(x, y)
    p1.setDrawPath()
    p1.setColor(Color.blue)
    p1.setShape(Point.INVISIBLE)

    x = 1.5
    y = m * x + b
    let p2 = Point(x, y)
    p2.setShape(Point.INVISIBLE)

    trendline.append(p1)
    trendline.append(p2)

    chart.chartData!.append(trendline)
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
        if point.getShape() != Point.CIRCLE {
            var tableRow = [Cell]()

            point.setRadius(2.0)
            point.setFillShape(true)
            point.setAlignment(Align.LEFT)

            var cell = Cell(f2)
            cell.setPoint(point)
            cell.setText("")

            tableRow.append(cell)

            cell = Cell(f1)
            cell.setText(point.getText())
            tableRow.append(cell)

            cell = Cell(f2)
            cell.setText(point.getURIAction())
            tableRow.append(cell)

            tableData.append(tableRow)
        }
    }
    table.setData(tableData)
    table.setColumnWidths()
    table.setCellBordersWidth(0.2)
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
    let lines = text.components(separatedBy: "\n")
    for line1 in lines {
        let line = line1.trimmingCharacters(in: .whitespacesAndNewlines)
        var cols: [String]?
        if delimiter == "|" {
            cols = line.components(separatedBy: "|")
        } else if delimiter == "\t" {
            cols = line.components(separatedBy: "\t")
        } else {
            // TODO:
            // print("Only pipes and tabs can be used as delimiters")
        }

        var country_name = cols![0].trimmingCharacters(in: .whitespacesAndNewlines)
        let population = Float(cols![1].filter({ $0 != "," }))
        let x = Float(cols![5].filter({ $0 != "," }))
        let y = Float(cols![7].filter({ $0 != "," }).trimmingCharacters(in: .whitespacesAndNewlines))

        if population != nil && x != nil && y != nil {
            let point = Point()
            point.setRadius(2.0)
            point.setText(country_name)
            point.setX(x! / population!)
            point.setY((y! / population!) * Float(100.0))

            country_name = country_name.replacingOccurrences(of: " ", with: "_")
            country_name = country_name.replacingOccurrences(of: "'", with: "_")
            country_name = country_name.replacingOccurrences(of: ",", with: "_")
            country_name = country_name.replacingOccurrences(of: "(", with: "_")
            country_name = country_name.replacingOccurrences(of: ")", with: "_")
            point.setURIAction("http://pdfjet.com/country/\(country_name).txt")

            if point.getX() > 1.25 {
                point.setShape(Point.RIGHT_ARROW)
                point.setColor(Color.black)
            }
            if point.getY() > 80.0 {
                point.setShape(Point.UP_ARROW)
                point.setColor(Color.blue)
            }
            if point.getText() == "France" {
                point.setShape(Point.MULTIPLY)
                point.setColor(Color.black)
            }
            if point.getText() == "Canada" {
                point.setShape(Point.BOX)
                point.setColor(Color.darkolivegreen)
            }
            if point.getText() == "United States" {
                point.setShape(Point.STAR)
                point.setColor(Color.red)
            }
            points.append(point)
        }
    }
    chartData.append(points)

    return chartData
}   // End of Example_09.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_09()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_09", time0, time1)
