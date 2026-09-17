/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_18.swift
 * This example shows how to write "Page X of N" footer on every page.
 */
public class Example_18 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_18.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let titles = [
            "1. Create the pages",
            "2. Draw the content",
            "3. Add the footers",
        ]
        let texts = [
            "The total number of pages is not known until all the content is drawn. "
                + "That is why the pages in this document are created with Page.DETACHED: "
                + "they are not added to the PDF yet, and they are kept in a list instead.",
            "Each page gets its content, like this heading and this paragraph. "
                + "Long documents would flow their text or tables from page to page here.",
            "Now the list holds every page, so its size is the total number of pages. "
                + "The footer \"Page X of N\" is drawn on each page, "
                + "and then all the pages are added to the PDF with addPages.",
        ]

        var pages = [Page]()
        for i in 0..<titles.count {
            let page = Page(pdf, A4.PORTRAIT, Page.DETACHED)

            let header = TextLine(f1, "How to number pages")
            header.setFontSize(10.0)
            header.setTextColor(Color.gray)
            header.setLocation(70.0, 50.0)
            header.drawOn(page)

            let line = Line(70.0, 60.0, page.getWidth() - 70.0, 60.0)
            line.setStrokeColor(Color.lightgray)
            line.drawOn(page)

            let title = TextLine(f2, titles[i])
            title.setFontSize(20.0)
            title.setLocation(70.0, 120.0)
            title.drawOn(page)

            let textBlock = TextBlock(f1, texts[i])
            textBlock.setFontSize(12.0)
            textBlock.setLineSpacing(1.5)
            textBlock.setLocation(70.0, 140.0)
            textBlock.setWidth(page.getWidth() - 140.0)
            textBlock.drawOn(page)

            pages.append(page)
        }

        let fontSize: Float = 10.0
        for i in 0..<pages.count {
            let page = pages[i]

            let line = Line(70.0, page.getHeight() - 60.0,
                    page.getWidth() - 70.0, page.getHeight() - 60.0)
            line.setStrokeColor(Color.lightgray)
            line.drawOn(page)

            let footer = "Page " + String(i + 1) + " of " + String(pages.count)
            page.setBrushColor(Color.black)
            page.drawString(
                    f1,
                    fontSize,
                    footer,
                    (page.getWidth() - f1.stringWidth(fontSize, footer))/2.0,
                    page.getHeight() - 40.0)
        }
        pdf.addPages(pages)

        try pdf.complete()
    }
}   // End of Example_18.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_18()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_18 => \(String(format: "%4lld", time1 - time0)) ms")
