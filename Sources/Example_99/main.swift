import Foundation
import PDFjet

/**
 *  Example_99.swift
 */
public class Example_99 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_99.pdf", append: false)!)
        // let f1 = Font(pdf, CoreFont.HELVETICA_BOLD)
        // let f2 = Font(pdf, CoreFont.HELVETICA)
        // let f1 = Font(pdf, CoreFont.COURIER_BOLD)
        // let f2 = Font(pdf, CoreFont.COURIER)
        // let f1 = try Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream")
        // let f2 = try Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream")
        // let f1 = try Font(pdf, "fonts/Andika/Andika-Bold.ttf.stream")
        // let f2 = try Font(pdf, "fonts/Andika/Andika-Regular.ttf.stream")
        let f1 = try Font(pdf, "fonts/SourceCodePro/SourceCodePro-SemiBold.ttf.stream")
        let f2 = try Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream")

        f1.setSize(7.0)
        f2.setSize(7.0)

        let L = 0
        let R = 1

        let table = BigTable(pdf, f1, f2, Letter.PORTRAIT)
        table.setLocation(20.0, 15.0)
        table.setBottomMargin(15.0)
        table.setColumnWidths([80, 80, 35, 60, 60, 75, 110, 90])
        table.setTextAlignment([L,  L,  L,  R,  R,  L,   L,  L])
        table.setColumnSpacing(2.0)
        table.setDrawVerticalLines(false)
        // table.setHeaderRowColor(Color.darkolivegreen)

        let widths = [15, 15, 18,  7, 12, 12, 15, 15, 25]
        let align =  [ L,  L,  L,  L,  R,  R,  L,  L,  L]

        let text = try String(contentsOfFile:
            "../datasets/Electric_Vehicle_Population_Data.csv", encoding: String.Encoding.utf8)
        let lines = text.components(separatedBy: .newlines)
        for line in lines {
            if line == "" {
                break
            }
            let fields = line.components(separatedBy: ",")

            let textLine = table.getTextLine(fields, widths, align)
            table.add(textLine)
            if textLine.contains("FORD") {
                table.drawRow(Color.blue)
            } else if textLine.contains("VOLKSWAGEN") {
                table.drawRow(Color.red)
            } else {
                table.drawRow(Color.black)
            }

            // table.add(fields[0])
            // table.add(fields[2])
            // table.add(fields[3])
            // table.add(fields[4])
            // table.add(fields[5])
            // table.add(fields[6])
            // table.add(fields[7])
            // if fields[8].hasPrefix("B") {
            //     table.add("BEV")
            // } else if fields[8].hasPrefix("P") {
            //     table.add("PHEV")
            // } else {
            //     table.add(fields[8])
            // }
            // if fields[6] == "FORD" {
            //     table.drawRow(Color.blue)
            // } else if fields[6] == "VOLKSWAGEN" {
            //     table.drawRow(Color.red)
            // } else {
            //     table.drawRow(Color.black)
            // }
        }

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
}   // End of Example_99.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_99()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_99", time0, time1)
