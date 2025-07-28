import Foundation
import PDFjet

/**
 *  Example_01.swift
 */
public class Example_01 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_01.pdf", append: false)
        let pdf = PDF(stream!)

        let font1 = try Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream")

        let page = Page(pdf, Letter.PORTRAIT)

        var text = try String(contentsOfFile: "data/languages/english.txt", encoding: .utf8)
        var textBox = TextBox(font1, text)
        textBox.setLocation(50.0, 50.0)
        textBox.setWidth(430.0)
        textBox.drawOn(page)

        text = try String(contentsOfFile: "data/languages/greek.txt", encoding: .utf8)
        textBox = TextBox(font1, text)
        textBox.setLocation(50.0, 250.0)
        textBox.setWidth(430.0)
        textBox.drawOn(page)
        
        text = try String(contentsOfFile: "data/languages/bulgarian.txt", encoding: .utf8)
        textBox = TextBox(font1, text)
        textBox.setLocation(50.0, 450.0)
        textBox.setWidth(430.0)
        textBox.drawOn(page)

        pdf.complete()
    }
}   // End of Example_01.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_01()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_01", time0, time1)
