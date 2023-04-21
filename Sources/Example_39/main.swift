import Foundation
import PDFjet

/**
 *  Example_39.swift
 *
 */
public class Example_39 {
    public init() throws {
        if let stream = OutputStream(toFileAtPath: "Example_39.pdf", append: false) {
            let pdf = PDF(stream)
            let page = Page(pdf, Letter.PORTRAIT)

            let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
            let f2 = Font(pdf, CoreFont.HELVETICA_BOLD)

            f1.setItalic(true)
            f2.setItalic(true)

            f1.setSize(10.0)
            f2.setSize(8.0)

            let chart = Chart(f1, f2)
            chart.setLocation(70.0, 50.0)
            chart.setSize(500.0, 300.0)
            chart.setTitle("Horizontal Bar Chart Example")
            chart.setXAxisTitle("")
            chart.setYAxisTitle("")
            chart.setData(getData())
            chart.setDrawYAxisLabels(false)
            chart.drawOn(page)

            pdf.complete()
        }
    }

    public func getData() -> [[Point]] {
        var chartData = [[Point]]()
        var path1 = [Point]()

        var point = Point()
        point.setDrawPath()
        point.setX(0.0)
        point.setY(45.0)
        point.setShape(Point.INVISIBLE)
        point.setColor(Color.blue)
        point.setLineWidth(20.0)
        point.setText(" Horizontal")
        point.setTextColor(Color.white)
        path1.append(point)

        point = Point()
        point.setX(35.0)
        point.setY(45.0)
        point.setShape(Point.INVISIBLE)
        path1.append(point)

        var path2 = [Point]()
        point = Point()
        point.setDrawPath()
        point.setX(0.0)
        point.setY(35.0)
        point.setShape(Point.INVISIBLE)
        point.setColor(Color.gold)
        point.setLineWidth(20.0)
        point.setText(" Bar")
        point.setTextColor(Color.black)
        path2.append(point)

        point = Point()
        point.setX(22.0)
        point.setY(35.0)
        point.setShape(Point.INVISIBLE)
        path2.append(point)

        var path3 = [Point]()
        point = Point()
        point.setDrawPath()
        point.setX(0.0)
        point.setY(25.0)
        point.setShape(Point.INVISIBLE)
        point.setColor(Color.green)
        point.setLineWidth(20.0)
        point.setText(" Chart")
        point.setTextColor(Color.white)
        path3.append(point)

        point = Point()
        point.setX(30.0)
        point.setY(25.0)
        point.setShape(Point.INVISIBLE)
        path3.append(point)

        var path4 = [Point]()
        point = Point()
        point.setDrawPath()
        point.setX(0.0)
        point.setY(15.0)
        point.setShape(Point.INVISIBLE)
        point.setColor(Color.red)
        point.setLineWidth(20.0)
        point.setText(" Example")
        point.setTextColor(Color.white)
        path4.append(point)

        point = Point()
        point.setX(47.0)
        point.setY(15.0)
        point.setShape(Point.INVISIBLE)
        path4.append(point)

        chartData.append(path1)
        chartData.append(path2)
        chartData.append(path3)
        chartData.append(path4)

        return chartData
    }
}   // End of Example_39.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_39()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_39 => \(time1 - time0)")
