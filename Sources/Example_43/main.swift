import Foundation
import PDFjet

/**
 * Example_43.swift
 */
public class Example_43 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_43.pdf", append: false)!)
        // pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Electric Vehicle Population Data")    // Required for PDF/UA !

        // Used for performance testing. Results in 2000+ pages PDF.
        let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_10_Pages.csv"
        // let fileName = "data/Electric_Vehicle_Population_5_Lines.csv"

        let f1 = try Font(pdf, IBMPlexSans.SemiBold)
        // let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        f1.setSize(10.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        // let f2 = Font(pdf, CoreFont.HELVETICA)
        f2.setSize(9.0)

        let table = BigTable(pdf, f1, f2, Letter.LANDSCAPE)
        table.setNumberOfColumns(9)             // The order of the
        try table.setTableData(fileName, ",")   // these statements
        table.setLocation(0.0, 0.0)             // is
        table.setBottomMargin(20.0)             // very
        try table.complete()                    // important!

        let pages = table.getPages()
        for i in 0..<pages.count {
            let page = pages[i]
            try page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
        }

        pdf.complete()
    }
}   // End of Example_43.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_43()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_43", time0, time1)
