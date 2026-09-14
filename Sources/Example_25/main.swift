import Foundation
import PDFjet

/**
 * Example_25.swift
 */
public class Example_25 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_25.pdf", append: false)!)

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)
        let f2 = try Font(pdf, IBMPlexSans.Bold)
        f2.setSize(10.0)

        let chart = DonutChart(f1, f2)
        chart.setLocation(300.0, 400.0)
        chart.setRadii(200.0, 120.0)     // an inner radius of 0 makes a pie chart

        chart.addSlice(Slice(25.0, 0xC1121F, "Apples"))   // deep red
        chart.addSlice(Slice(20.0, 0x1D3557, "Oranges"))   // navy blue
        chart.addSlice(Slice(30.0, 0x1A7468, "Bananas"))   // dark teal
        chart.addSlice(Slice(15.0, 0xD97706, "Grapes"))   // burnt orange
        chart.addSlice(Slice(10.0, 0xCAAA2F, "Lemons"))   // dark gold
        chart.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_25.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_25()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_25 => \(time1 - time0) ms")
