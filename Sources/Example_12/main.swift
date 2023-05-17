import Foundation
import PDFjet

// Example_12.swift
public class Example_12 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_12.pdf", append: false)!)
        let font = Font(pdf, CoreFont.HELVETICA)
        let page = Page(pdf, Letter.PORTRAIT)

        let lines = try Text.readLines("Sources/Example_12/main.swift")
        var buf = String()
        for line in lines {
            buf.append(line)
            buf.append("\r\n")  // CR and LF both required!
        }

        let code2D = try Barcode2D(buf)
        code2D.setModuleWidth(0.5)
        code2D.setLocation(100.0, 60.0)
        code2D.drawOn(page)

        let textLine = TextLine(font, "PDF417 barcode containing the program that created it.")
        textLine.setLocation(100.0, 40.0)
        textLine.drawOn(page)

        pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_12()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_12", time0, time1)
