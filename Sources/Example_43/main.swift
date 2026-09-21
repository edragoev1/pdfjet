/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_43.swift
 */
public class Example_43 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_43.pdf", append: false)!)
        // Uncomment the line below to make this a PDF/UA document. A BigTable
        // is tagged as a table: a TR for each row, holding a TH or a TD with
        // the text of each cell, which is what a screen reader reads a table
        // from. It is off here because of what it costs at this size: every
        // tagged cell is an object of its own, so this document goes from
        // 5,108 objects and 11.8 MB to 1.25 million objects and 249 MB. The
        // 10-page file below is the size to see the tagging at.
        // pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicle Population Data")    // Required for PDF/UA !

        // Used for performance testing. Results in 2000+ pages PDF.
        let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_10_Pages.csv"

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
}   // End of Example_43.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_43()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_43 => \(String(format: "%4lld", time1 - time0)) ms")
