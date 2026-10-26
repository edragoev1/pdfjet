/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_28.swift
 */
public class Example_28 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_28.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Fruit Donut Chart")

        let page = Page(pdf, Letter.PORTRAIT)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)
        let f2 = try Font(pdf, IBMPlexSans.Bold)
        f2.setSize(10.0)

        // A heading, which PAC asks a document for
        TextLine(f2, "Fruit Donut Chart")
                .setStructureType(StructElem.H1)
                .setFontSize(18.0)
                .setLocation(100.0, 100.0)
                .drawOn(page)

        let chart = DonutChart(f1, f2)
        chart.setLocation(100.0, 200.0)
        chart.setRadii(200.0, 120.0)     // an inner radius of 0 makes a pie chart

        chart.setTitle("Fruit sold this week")
        chart.setSubtitle("Share of 100 crates, in the default colors")
        chart.addSlice(Slice(25.0, "Apples"))
        chart.addSlice(Slice(20.0, "Oranges"))
        chart.addSlice(Slice(30.0, "Bananas"))
        chart.addSlice(Slice(15.0, "Grapes"))
        chart.addSlice(Slice(10.0, "Lemons"))
        chart.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_28 => \(String(format: "%4lld", time1 - time0)) ms")
