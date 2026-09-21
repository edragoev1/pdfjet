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
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicle Population Data")    // Required for PDF/UA !

        // A tagged table has a structure element for every cell, and they are
        // held until the document is written, so a PDF/UA document of this
        // table is as large as the rows it draws. The whole file, which is
        // 2000+ pages, is the one to time the library with, without the
        // compliance above.
        let fileName = "data/Electric_Vehicle_Population_10_Pages.csv"
        // let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_5_Lines.csv"

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
