import Foundation
import PDFjet

/**
 *  Example_35.swift
 */
public class Example_35 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_35.pdf", append: false)!)
        let page = Page(pdf, A4.PORTRAIT)

        let text = try String(contentsOfFile: "data/chinese-english.txt", encoding: .utf8)

        let mainFont = Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT)
        let fallbackFont = try Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf")

        var textLine = TextLine(mainFont)
        textLine.setText(text)
        textLine.setLocation(50.0, 50.0)
        textLine.drawOn(page)

        textLine = TextLine(mainFont)
        textLine.setFallbackFont(fallbackFont)
        textLine.setText(text)
        textLine.setLocation(50.0, 80.0)
        textLine.drawOn(page)

        pdf.complete()
    }
}   // End of Example_35.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_35()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_35", time0, time1)
