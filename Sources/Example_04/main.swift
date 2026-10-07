/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_04.swift
 */
public class Example_04 {
    public init() throws {
        let stream = OutputStream(toFileAtPath: "Example_04.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Universal Declaration of Human Rights in Japanese and Korean")

        let f0 = try Font(pdf, IBMPlexSans.Regular)

        f0.setSize(12.0)


        let f1 = try Font(pdf, IBMPlexSansJP.Regular)
        f1.setSize(12.0)

        let f2 = try Font(pdf, IBMPlexSansKR.Regular)
        f2.setSize(12.0)

        var page = Page(pdf, Letter.PORTRAIT)

        // The heading is in IBM Plex Sans, and the characters it has no glyph for,
        // the name of the language, are in the fallback font.
        // The line above each block is its heading
        TextLine(f0, "This block is Japanese: 日本語").setFallbackFont(f1).setStructureType(StructElem.H1).setLocation(50.0, 50.0).drawOn(page)

        var textBlock = TextBlock(
                f1, try Content.ofTextFile("data/languages/japanese.txt"))
        textBlock.setLanguage("ja")
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        page = Page(pdf, Letter.PORTRAIT)

        TextLine(f0, "This block is Korean: 한국어").setFallbackFont(f2).setStructureType(StructElem.H1).setLocation(50.0, 50.0).drawOn(page)

        textBlock = TextBlock(
                f2, try Content.ofTextFile("data/languages/korean.txt"))
        textBlock.setLanguage("ko")
        textBlock.setLocation(50.0, 70.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_04.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_04()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_04 => \(String(format: "%4lld", time1 - time0)) ms")
