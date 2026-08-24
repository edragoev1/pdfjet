import Foundation
import PDFjet

/**
 * Example_25.swift
 */
import Foundation
import PDFjet

public class Example_25 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_25.pdf", append: false)
        let pdf = PDF(stream!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.Bold)

        let chart = DonutChart(f1, f2, true)           // true = full donut (with hole)
        chart.setLocation(300.0, 400.0)
        chart.setR1AndR2(200.0, 120.0)

        chart.addSlice(Slice(90.0,  0xC1121F,  "Apples",   ""))   // deep red
        chart.addSlice(Slice(72.0,  0x1D3557,  "Oranges",  ""))   // navy blue
        chart.addSlice(Slice(108.0, 0x1A7468,  "Bananas",  ""))   // dark teal
        chart.addSlice(Slice(54.0,  0xD97706,  "Grapes",   ""))   // burnt orange
        chart.addSlice(Slice(36.0,  0xCAAA2F,  "Lemons",   ""))   // dark gold
        chart.drawOn(page)

        pdf.complete()
    }

}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_25", time0, time1)
