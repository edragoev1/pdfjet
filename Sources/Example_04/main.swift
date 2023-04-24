import Foundation
import PDFjet

/**
 *  Example_04.swift
 */
public class Example_04 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_04.pdf", append: false)!)

        // Chinese (Traditional) font
        let f1 = Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT)

        // Chinese (Simplified) font
        let f2 = Font(pdf, CJKFont.ST_HEITI_SC_LIGHT)

        // Japanese font
        let f3 = Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR)

        // Korean font
        let f4 = Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM)

        let page = Page(pdf, Letter.PORTRAIT)

        f1.setSize(14.0)
        f2.setSize(14.0)
        f3.setSize(14.0)
        f4.setSize(14.0)

        let x_pos: Float = 100.0
        var y_pos: Float = 100.0

        let fileName = "data/happy-new-year.txt"
        let lines = (try String(
                contentsOfFile: fileName, encoding: .utf8)).components(separatedBy: .newlines)

        let text = TextLine(f1)
        for line in lines {
            if line.contains("Simplified") {
                text.setFont(f2)
            } else if line.contains("Japanese") {
                text.setFont(f3)
            } else if line.contains("Korean") {
                text.setFont(f4)
            }
            text.setText(line)
            text.setLocation(x_pos, y_pos)
            text.drawOn(page)
            y_pos += Float(25.0)
        }

        pdf.complete()
    }
}   // End of Example_04.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_04()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_04", time0, time1)
