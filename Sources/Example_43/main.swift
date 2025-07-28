import Foundation
import PDFjet

/**
 *  Example_43.swift
 */
public class Example_43 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_43.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA)

        let fileName = "data/Electric_Vehicle_Population_Data.csv"
        // let fileName = "data/Electric_Vehicle_Population_550.csv"

        let f1 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream")
        let f2 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")
        f1.setSize(9.0)
        f2.setSize(9.0)
        let table = BigTable(pdf, f1, f2, Letter.LANDSCAPE)
        var widths = try table.getColumnWidths(fileName)
        widths[8] = 60.0    // Override the calculated width
        widths[9] = 70.0    // Override the calculated width
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
                let fields = line.components(separatedBy: ",")
                drawRow(table, fields, headerRow)
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

    func drawRow(_ table: BigTable, _ fields: [String], _ headerRow: Bool) {
        var row = [String]()
        for i in 0..<10 {
            let field = fields[i]
            if i == 8 {
                if headerRow {
                    row.append("Vehicle Type");
                } else {
                    if field.hasPrefix("B") {
                        row.append("BEV")
                    } else if field.hasPrefix("P") {
                        row.append("PHEV")
                    } else {
                        row.append(field)
                    }
                }
            } else if i == 9 {
                if headerRow {
                    row.append("Green Vehicle");
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
        if fields[6] == "TOYOTA" {
            table.drawRow(row, Color.red)
        } else if fields[6] == "JEEP" {
            table.drawRow(row, Color.green)
        } else if fields[6] == "FORD" {
            table.drawRow(row, Color.blue)
        } else {
            table.drawRow(row, Color.black)
        }
    }
}   // End of Example_43.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_43()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_43", time0, time1)
