import Foundation
import PDFjet

/**
 *  Example_02.swift
 */
public class Example_02 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_02.pdf", append: false)
        let pdf = PDF(stream!)

        let font1 = try Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream")
        font1.setSize(12.0)

        let font2 = try Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream")
        font2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = try String(contentsOfFile: "data/languages/japanese.txt", encoding: .utf8)
        var textBox = TextBox(font1, text)
        textBox.setLocation(50.0, 50.0)
        textBox.setWidth(415.0)
        textBox.drawOn(page)

        text = try String(contentsOfFile: "data/languages/korean.txt", encoding: .utf8)
        textBox = TextBox(font2, text)
        textBox.setLocation(50.0, 450.0)
        textBox.setWidth(415.0)
        textBox.drawOn(page)

        pdf.complete()
    }
}   // End of Example_02.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_02()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_02", time0, time1)
