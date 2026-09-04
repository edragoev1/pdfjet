import Foundation
import PDFjet

/**
 * Example_40.swift
 */
public class Example_40 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_40.pdf", append: false)!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setItalic(true)
        f1.setSize(10.0)

        let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setItalic(true)
        f2.setSize(8.0)

        let chart = Chart(f1, f2)
        chart.setData(try getData())
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Vertical Bar Chart Example")
        chart.setXAxisTitle("Bar Chart")
        chart.setYAxisTitle("Vertical")
        chart.setDrawYAxisLines(false)
        chart.setDrawXAxisLabels(false)
        chart.setXYChart(false)
        chart.drawOn(page)

        pdf.complete()
    }

    public func getData() throws -> [[Point]] {
        var chartData = [[Point]]()

        let w: Float = 14.0
        var x: Float = 10.0
        let dx1: Float = 16.0
        let dx2: Float = 26.0

        addVerticalBar(&chartData, x, w, 45.0, Color.green, " January", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 75.0, Color.red, " January", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 65.0, Color.green, " February", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 20.0, Color.red, " February", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " March", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " March", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 45.0, Color.green, " April", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 75.0, Color.red, " April", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 65.0, Color.green, " May", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 20.0, Color.red, " May", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " June", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " June", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " July", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " July", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " August", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " August", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " September", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " September", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " October", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " October", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " November", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " November", Color.white)
        x += dx2
        addVerticalBar(&chartData, x, w, 31.0, Color.green, " December", Color.white)
        x += dx1
        addVerticalBar(&chartData, x, w, 73.0, Color.red, " December", Color.white)

        return chartData
    }

    private func addVerticalBar(
            _ chartData: inout [[Point]],
            _ x: Float,
            _ w: Float,
            _ h: Float,
            _ color: Int32,
            _ text: String,
            _ textColor: Int32) {
        var path1 = [Point]()

        var point = Point()
        point.setDrawPath()
        point.setX(x)
        point.setY(0.0)
        point.setShape(Point.INVISIBLE)
        point.setStrokeWidth(w)
        point.setStrokeColor(color)
        point.setText(text)
        point.setTextColor(textColor)
        point.setTextDirection(90)
        path1.append(point)

        point = Point()
        point.setX(x)
        point.setY(h)
        point.setShape(Point.INVISIBLE)
        path1.append(point)

        chartData.append(path1)
    }
}   // End of Example_40.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_40()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_40", time0, time1)