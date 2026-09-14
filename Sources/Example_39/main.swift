import Foundation
import PDFjet

/**
 * Example_39.swift
 *
 * Draws a bar chart with horizontal bars: one series, the value written at
 * the end of each bar.
 */
public class Example_39 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_39.pdf", append: false)!)

        let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setSize(10.0)

        let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setSize(8.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let chart = BarChart(f1, f2)
        chart.setLocation(70.0, 50.0)
        chart.setSize(500.0, 300.0)
        chart.setTitle("Longest rivers")
        chart.setXAxisTitle("Length in km")
        chart.setCategories("Nile", "Amazon", "Yangtze", "Mississippi", "Yenisei", "Yellow River")
        chart.addSeries("", [6650.0, 6400.0, 6300.0, 6275.0, 5539.0, 5464.0], Color.steelblue)
        chart.setHorizontal(true)
        chart.setDrawValueLabels(true)
        chart.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_39.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_39()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_39 => \(time1 - time0) ms")
