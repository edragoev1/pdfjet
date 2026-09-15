import Foundation
import PDFjet

/**
 * Example_40.swift
 *
 * Draws two bar charts with vertical bars from the same data: the two series
 * grouped by month, and the same series stacked, with a legend under each title.
 */
public class Example_40 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_40.pdf", append: false)!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        let months = [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
        let units2025: [Float] = [45.0, 65.0, 31.0, 45.0, 65.0, 31.0, 38.0, 52.0, 47.0, 59.0, 66.0, 72.0]
        let units2026: [Float] = [75.0, 20.0, 73.0, 75.0, 20.0, 73.0, 61.0, 58.0, 69.0, 64.0, 77.0, 80.0]

        let chart = BarChart(f1, f2)
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Units sold by month")
        chart.setXAxisTitle("Month")
        chart.setYAxisTitle("Units")
        chart.setCategories(months)
        chart.addSeries("2025", units2025, Color.seagreen)
        chart.addSeries("2026", units2026, Color.indianred)
        chart.setGroupGap(0.4)
        chart.setBarGap(0.1)
        chart.drawOn(page)

        let stacked = BarChart(f1, f2)
        stacked.setLocation(70.0, 400.0)
        stacked.setSize(500.0, 300.0)
        stacked.setTitle("Units sold by month, stacked")
        stacked.setXAxisTitle("Month")
        stacked.setYAxisTitle("Units")
        stacked.setCategories(months)
        stacked.addSeries("2025", units2025, Color.seagreen)
        stacked.addSeries("2026", units2026, Color.indianred)
        stacked.setStacked(true)
        stacked.setDrawValueLabels(true)
        stacked.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_40.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_40()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_40 => \(String(format: "%4lld", time1 - time0)) ms")
