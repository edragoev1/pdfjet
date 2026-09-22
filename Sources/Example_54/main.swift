/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_54.swift
 * A Markdown text, data/markdown/pdfjet.md, drawn down the pages by Markdown
 * as a PDF/UA document: its headings, paragraphs, lists, quote, code, table
 * and image, each tagged for screen readers, with a number at the foot of
 * every page. The image is read from data/markdown, the directory that
 * setImageDirectory names.
 */
public class Example_54 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_54.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("PDFjet")

        let regular = try Font(pdf, IBMPlexSans.Regular).setSize(11.0)
        let bold = try Font(pdf, IBMPlexSans.Bold).setSize(11.0)
        let italic = try Font(pdf, IBMPlexSans.Italic).setSize(11.0)
        let boldItalic = try Font(pdf, IBMPlexSans.BoldItalic).setSize(11.0)
        let code = try Font(pdf, IBMPlexMono.Regular).setSize(9.5)

        let markdown = Markdown(regular, bold, italic, boldItalic, code)
        markdown.setImageDirectory("data/markdown")
        var pages = [Page]()
        try markdown.drawOn(pdf, try Content.ofTextFile("data/markdown/pdfjet.md"), &pages, Letter.PORTRAIT)

        for (i, page) in pages.enumerated() {
            let number = TextLine(regular, String(i + 1))
            number.setFontSize(9.0)
            page.addFooter(number, 36.0)
        }
        pdf.addPages(pages)
        try pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_54()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_54 => \(String(format: "%4lld", time1 - time0)) ms")
