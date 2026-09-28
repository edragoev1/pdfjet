/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_57.swift
 */
public class Example_57 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_57.pdf", append: false)!)
        // Example_43's table, cut to 550 rows, 12 pages, as a PDF/UA document: a BigTable is
        // tagged as a table, a TR for each row, holding a TH or a TD with the text
        // of each cell, which is what a screen reader reads a table from. It is
        // the size a PDF/UA checker such as PAC can open, which the 2,000 pages of
        // Example_43 are not.
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicle Population Data")    // Required for PDF/UA !

        let fileName = "data/Electric_Vehicle_Population_10_Pages.csv"

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(9.0)

        let table = BigTable(pdf, f1, f2, Letter.LANDSCAPE)
        table.setNumberOfColumns(9)             // The order of the
        try table.setTableData(fileName, ",")   // these statements
        table.setLocation(0.0, 0.0)             // is
        table.setBottomMargin(20.0)             // very
        try table.complete()                    // important!

        try pdf.complete()
    }
}   // End of Example_57.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_57()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_57 => \(String(format: "%4lld", time1 - time0)) ms")
