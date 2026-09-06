import Foundation
import PDFjet

// Example_12.swift
public class Example_12 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_12.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("PDF417 barcode example")
        let font = try Font(pdf, IBMPlexSans.Regular)
        let page = Page(pdf, Letter.PORTRAIT)

        let lines = try Text.readLines("data/Example_12.java")
        var buf = String()
        for line in lines {
            buf.append(line)
            buf.append("\r\n")  // CR and LF both required!
        }

        let barcode = try PDF417(buf)
        barcode.setModuleWidth(0.5)
        barcode.setLocation(100.0, 60.0)
        barcode.drawOn(page)

        let textLine = TextLine(font, "PDF417 barcode containing the contents of data/Example_12.java")
        textLine.setLocation(100.0, 40.0)
        textLine.drawOn(page)

        pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_12()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_12", time0, time1)
