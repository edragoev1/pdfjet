/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_02.swift
 */
public class Example_02 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_02.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Universal Declaration of Human Rights in Four Languages")

        let f1 = try Font(pdf, IBMPlexSansJP.Regular)
        f1.setSize(14.0)

        let f2 = try Font(pdf, IBMPlexSansKR.Regular)
        f2.setSize(14.0)

        let f3 = try Font(pdf, IBMPlexSansSC.Regular)
        f3.setSize(14.0)

        let f4 = try Font(pdf, IBMPlexSansTC.Regular)
        f4.setSize(14.0)

        var page = Page(pdf, Letter.PORTRAIT)

        TextLine(f1, "This block is Japanese.").setLocation(50.0, 50.0).drawOn(page)

        var textBlock = TextBlock(
                f1, try Content.ofTextFile("data/languages/japanese.txt"))
        textBlock.setLanguage("ja")
        // The Japanese and Chinese fonts leave no space between lines.
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        TextLine(f2, "This block is Korean.").setLocation(50.0, 50.0).drawOn(page)

        textBlock = TextBlock(
                f2, try Content.ofTextFile("data/languages/korean.txt"))
        textBlock.setLanguage("ko")
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        TextLine(f3, "This block is Simplified Chinese.").setLocation(50.0, 50.0).drawOn(page)

        textBlock = TextBlock(
                f3, try Content.ofTextFile("data/languages/simplified-chinese.txt"))
        textBlock.setLanguage("zh-Hans")
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        TextLine(f4, "This block is Traditional Chinese.").setLocation(50.0, 50.0).drawOn(page)

        textBlock = TextBlock(
                f4, try Content.ofTextFile("data/languages/traditional-chinese.txt"))
        textBlock.setLanguage("zh-Hant")
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_02.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_02()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_02 => \(String(format: "%4lld", time1 - time0)) ms")
