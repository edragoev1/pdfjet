import Foundation
import PDFjet

/**
 * Example_02.swift
 */
public class Example_02 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_02.pdf", append: false)
        let pdf = PDF(stream!)

        let f1 = try Font(pdf, IBMPlexSansJP.Regular)
        f1.setSize(14.0)

        let f2 = try Font(pdf, IBMPlexSansKR.Regular)
        f2.setSize(14.0)

        let f3 = try Font(pdf, IBMPlexSansSC.Regular)
        f3.setSize(14.0)

        let f4 = try Font(pdf, IBMPlexSansTC.Regular)
        f4.setSize(14.0)

        var page = Page(pdf, Letter.PORTRAIT)

        var textBlock = TextBlock(
                f1, try Content.ofTextFile("data/languages/japanese.txt"))
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        textBlock = TextBlock(
                f2, try Content.ofTextFile("data/languages/korean.txt"))
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        textBlock = TextBlock(
                f3, try Content.ofTextFile("data/languages/simplified-chinese.txt"))
        textBlock.setLocation(50.0, 50.0)
        textBlock.setWidth(415.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        textBlock = TextBlock(
                f4, try Content.ofTextFile("data/languages/traditional-chinese.txt"))
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
