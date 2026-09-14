import Foundation
import PDFjet

/**
 * Example_40.swift
 *
 * Draws a bar chart with vertical bars: two series grouped by month, with a
 * legend under the title.
 */
public class Example_40 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_40.pdf", append: false)!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setSize(10.0)

        let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setSize(8.0)

        let chart = BarChart(f1, f2)
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Units sold by month")
        chart.setXAxisTitle("Month")
        chart.setYAxisTitle("Units")
        chart.setCategories(
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
        chart.addSeries("2025",
                [45.0, 65.0, 31.0, 45.0, 65.0, 31.0, 38.0, 52.0, 47.0, 59.0, 66.0, 72.0],
                Color.seagreen)
        chart.addSeries("2026",
                [75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0],
                Color.indianred)
        chart.setGroupGap(0.4)
        chart.setBarGap(0.1)
        chart.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_40.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_40()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_40", time0, time1)
