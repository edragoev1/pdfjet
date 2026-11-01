/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_24.swift
 */
public class Example_24 {
    public init(_ fileName: String, _ pdfUA: Bool) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_24.pdf", append: false)!)
        // The sample is a PDF/UA document: a BigTable is tagged as a table, a
        // TR for each row holding a TH or a TD with the text of each cell,
        // which is what a screen reader reads a table from. A file given as
        // the argument is left untagged, so that the benchmark stays
        // comparable with earlier releases: every tagged cell is an object of
        // its own, and the full data file goes from 5,108 objects and 11.8 MB
        // to 1.25 million objects and 249 MB.
        if pdfUA {
            pdf.setCompliance(Compliance.PDF_UA_1)
        }
        pdf.setTitle("Electric Vehicle Population Data")    // Required for PDF/UA !

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
}   // End of Example_24.swift

// The 10-page sample, or the file given as the first argument: the full
// Electric Vehicle Population Data, 124,716 rows, makes 2,546 pages and is
// used for performance testing.
let fileName = (CommandLine.arguments.count > 1) ?
        CommandLine.arguments[1] : "resources/data/Electric_Vehicle_Population_10_Pages.csv"
let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_24(fileName, CommandLine.arguments.count == 1)
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_24 => \(String(format: "%4lld", time1 - time0)) ms")
