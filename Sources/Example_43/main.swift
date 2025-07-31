import Foundation
import PDFjet

public class Example_43 {
    init() throws {
        let outputStream = OutputStream(toFileAtPath: "Example_43.pdf", append: false)!
        let pdf = PDF(outputStream)
        pdf.setCompliance(Compliance.PDF_UA)

        // Used for performance testing. Results in 2000+ pages PDF.
        let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_10_Pages.csv"

        let f1 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream")
        f1.setSize(10.0)

        let f2 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
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
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_43()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_43", time0, time1)
