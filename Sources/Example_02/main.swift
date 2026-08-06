import Foundation
import PDFjet

/**
 * Example_02.swift
 */
public class Example_02 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_02.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream")
        f1.setSize(14.0)

        let f2 = try Font(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream")
        f2.setSize(14.0)

        let f3 = try Font(pdf, "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream")
        f3.setSize(14.0)

        let f4 = try Font(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream")
        f4.setSize(14.0)

        var page = Page(pdf, Letter.PORTRAIT)

        var text = try String(contentsOfFile: "data/languages/japanese.txt", encoding: .utf8)
        var textBlock = TextBlock(f1, text)
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = try String(contentsOfFile: "data/languages/korean.txt", encoding: .utf8)
        textBlock = TextBlock(f2, text)
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = try String(contentsOfFile: "data/languages/simplified-chinese.txt", encoding: .utf8)
        textBlock = TextBlock(f3, text)
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        text = try String(contentsOfFile: "data/languages/traditional-chinese.txt", encoding: .utf8)
        textBlock = TextBlock(f4, text)
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        pdf.complete()
    }
}   // End of Example_02.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_02()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_02", time0, time1)
