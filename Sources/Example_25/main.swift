import Foundation
import PDFjet

/**
 * Example_25.swift
 */
public class Example_25 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_25.pdf", append: false)
        let pdf = PDF(stream!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.Bold)

        let chart = DonutChart(f1, f2, false)
        chart.setLocation(300.0, 300.0)
        chart.setR1AndR2(200.0, 100.0)
        chart.addSlice(Slice(10.0, Color.red, "", ""))
        chart.addSlice(Slice(20.0, Color.green, "", ""))
        chart.addSlice(Slice(30.0, Color.blue, "", ""))
        chart.addSlice(Slice(40.0, Color.peachpuff, "", ""))
        chart.addSlice(Slice(75.0, Color.red, "", ""))
        chart.addSlice(Slice(25.0, Color.blue, "", ""))
        chart.drawOn(page)

        pdf.complete()
    }

}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_25", time0, time1)
