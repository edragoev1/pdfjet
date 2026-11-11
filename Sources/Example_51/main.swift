/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_51.swift
 *
 * Eight columns of the Electric Vehicle data of Example_24 on portrait pages,
 * with setWrapText: a field too long for its column wraps onto more lines, the
 * row as tall as its tallest field, so no column is cut short and none is lost
 * off the page; the memory stays flat, a row at a time.
 */
public class Example_51 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_51.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicle Population Data, wrapped")   // Required for PDF/UA !

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        f1.setSize(9.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(8.0)

        let table = BigTable(pdf, f1, f2, Letter.PORTRAIT)
        // Make, Model, Model Year, City, Electric Vehicle Type, Clean Alternative
        // Fuel Vehicle (CAFV) Eligibility, Electric Range and Electric Utility
        table.setColumns([6, 7, 5, 2, 8, 9, 10, 15])
        table.setWrapText(true)
        try table.setTableData("resources/data/Electric_Vehicle_Population_10_Pages.csv", ",")
        table.setVerticalPadding(2.0)
        table.setLocation(20.0, 20.0)
        table.setBottomMargin(30.0)
        try table.complete()

        try pdf.complete()
    }
}   // End of Example_51.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_51()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_51 => \(String(format: "%4lld", time1 - time0)) ms")
