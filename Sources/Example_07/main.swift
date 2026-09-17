/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_07.swift
 * This example adds a "DRAFT" watermark to every page of a two-page
 * PDF/A-3B document. The watermark is drawn first, so the text of the page
 * is drawn over it.
 */
public class Example_07 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_07.pdf", append: false)!, Compliance.PDF_A_3B)
        pdf.setTitle("PDF/A-3B compliant PDF")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        let f3 = try Font(pdf, IBMPlexSans.Bold)

        let titles = [
            "Project Proposal",
            "Budget and Schedule",
        ]
        let texts = [
            "This proposal describes a new reporting service that creates invoices, "
                + "statements and delivery notes as PDF documents. The documents are "
                + "archived as PDF/A-3B, so they can be opened and printed exactly "
                + "the same way for many years.\n\n"
                + "The watermark tells every reader that this is a draft. It is drawn "
                + "in light gray behind the text, at an angle from the bottom left "
                + "corner to the top right corner of the page.",
            "The service will be built in three phases over six months. The first "
                + "phase delivers invoices, the second statements, and the third "
                + "delivery notes.\n\n"
                + "The budget and the schedule will be final once the proposal is "
                + "approved. Until then, every page of this document is marked as a draft.",
        ]

        for i in 0..<titles.count {
            let page = Page(pdf, A4.LANDSCAPE)

            // The watermark is drawn before the content of the page.
            f3.setSize(120.0)
            page.addWatermark(f3, "DRAFT")

            let title = TextLine(f2, titles[i])
            title.setFontSize(28.0)
            title.setLocation(70.0, 100.0)
            title.drawOn(page)

            let textBlock = TextBlock(f1, texts[i])
            textBlock.setFontSize(14.0)
            textBlock.setLineSpacing(1.5)
            textBlock.setLocation(70.0, 130.0)
            textBlock.setWidth(page.getWidth() - 140.0)
            textBlock.drawOn(page)

            let footer = TextLine(f1, "Page " + String(i + 1) + " of " + String(titles.count))
            footer.setFontSize(10.0)
            footer.setTextColor(Color.gray)
            footer.setLocation(70.0, page.getHeight() - 40.0)
            footer.drawOn(page)
        }

        try pdf.complete()
    }
}   // End of Example_07.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_07()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_07 => \(String(format: "%4lld", time1 - time0)) ms")
