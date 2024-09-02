import Foundation
import PDFjet

/**
 *  Example_43.swift
 */
public class Example_43 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_43.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA)

        // let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        // let f2 = Font(pdf, CoreFont.HELVETICA)
        let f1 = try Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf")
        let f2 = try Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream")

        f1.setSize(8.0)
        f2.setSize(8.0)

        let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_1000.csv"

        let table = BigTable(pdf, f1, f2, Letter.LANDSCAPE)
        var widths = try table.getColumnWidths(fileName)
        widths[8] = 70.0    // Override the calculated width
        widths[9] = 99.0    // Override the calculated width
        table.setColumnSpacing(7.0)
        table.setLocation(20.0, 15.0)
        table.setBottomMargin(15.0)
        table.setColumnWidths(widths)

        // final int LEFT = 0;                  // Align Left
        // final int RIGHT = 1;                 // Align Right
        // table.setTextAlignment(1, RIGHT);    // Override the auto alignment
        // table.setTextAlignment(5, LEFT);     // Override the auto alignment

        let text = try String(contentsOfFile: fileName, encoding: String.Encoding.utf8)
        var headerRow = true
        let lines = text.components(separatedBy: .newlines)
        for line in lines {
            if line != "" {
                var fields = line.components(separatedBy: ",")
                // Optional step:
                fields = selectAndProcessFields(table, fields, headerRow)
                if fields[6] == "TOYOTA" {
                    table.drawRow(fields, Color.red)
                } else if fields[6] == "JEEP" {
                    table.drawRow(fields, Color.green)
                } else if fields[6] == "FORD" {
                    table.drawRow(fields, Color.blue)
                } else {
                    table.drawRow(fields, Color.black)
                }
                headerRow = false
            }
        }
        table.complete()

        let pages = table.getPages()
        var i = 0
        while i < pages.count {
            let page = pages[i]
            try page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)"))
            pdf.addPage(page)
            i += 1
        }

        pdf.complete()
    }

    func selectAndProcessFields(_ table: BigTable, _ fields: [String], _ headerRow: Bool) -> [String] {
        var row = [String]()
        for i in 0..<10 {
            let field = fields[i]
            if i == 8 {
                if field.hasPrefix("B") {
                    row.append("BEV")
                } else if field.hasPrefix("P") {
                    row.append("PHEV")
                } else {
                    row.append(field)
                }
            } else if i == 9 {
                if headerRow {
                    row.append("Clean Alternative Fuel Vehicle");
                } else {
                    if field.hasPrefix("C") {
                        row.append("YES")
                    } else if field.hasPrefix("N") {
                        row.append("N")
                    } else {
                        row.append("UNKNOWN")
                    }
                }
            } else {
                row.append(field)
            }
        }
        return row
    }
}   // End of Example_43.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_43()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_43", time0, time1)
