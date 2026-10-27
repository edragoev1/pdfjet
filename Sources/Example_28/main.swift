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

        var page = Page(pdf, Letter.PORTRAIT)

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

        let chart = DonutChart(f2, f1)     // The bold font first
        chart.setLocation(100.0, 200.0)
        chart.setRadii(200.0, 120.0)     // an inner radius of 0 makes a pie chart

        chart.setTitle("Fruit sold this week")
        chart.setSubtitle("Share of 100 crates, in the default colors")
        chart.add("Apples", 25.0)
        chart.add("Oranges", 20.0)
        chart.add("Bananas", 30.0)
        chart.add("Grapes", 15.0)
        chart.add("Lemons", 10.0)
        chart.drawOn(page)

        // Many slices, some small: a legend under the circle in place of the
        // leader lines, and the total in the hole
        page = Page(pdf, Letter.PORTRAIT)
        let many = DonutChart(f2, f1)
        many.setLocation(100.0, 100.0)
        many.setRadii(150.0, 90.0)
        many.setTitle("Fruit sold this month")
        many.setSubtitle("Share of 100 crates, eight kinds, a color each")
        many.setCenterText("100 crates")
        many.setLegend(true)
        let kinds = ["Apples", "Bananas", "Oranges", "Grapes", "Lemons",
                "Pears", "Plums", "Cherries"]
        let crates: [Float] = [31.0, 24.0, 17.0, 10.0, 7.0, 5.0, 4.0, 2.0]
        for i in 0..<kinds.count {
            many.add(kinds[i], crates[i])
        }
        many.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_28.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_28()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_28 => \(String(format: "%4lld", time1 - time0)) ms")
