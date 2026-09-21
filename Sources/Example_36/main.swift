/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_36.swift
 * This example draws two map pages first and their contents page last, and
 * then adds the pages to the PDF in reading order, with the contents first.
 */
public class Example_36 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_36.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Maps")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let titles = ["Europe", "Spain"]
        let files = ["images/ee-map.png", "images/spain-admin.jpg"]

        // 1. Draw the map pages. They are detached, so they are not in the PDF yet.
        var mapPages = [Page]()
        for i in 0..<titles.count {
            let page = Page(pdf, A4.PORTRAIT, Page.DETACHED)

            let title = TextLine(f2, titles[i])
            title.setFontSize(24.0)
            title.setLocation(50.0, 80.0)
            title.drawOn(page)

            // Scale the image to the width of the page between the margins.
            let image = try Image(pdf, files[i])
            image.scaleBy((page.getWidth() - 100.0) / image.getWidth())
            image.setLocation(50.0, 100.0)
            image.drawOn(page)

            let footer = TextLine(f1, "Page " + String(i + 2))
            footer.setFontSize(10.0)
            footer.setTextColor(Color.gray)
            footer.setLocation(50.0, page.getHeight() - 40.0)
            footer.drawOn(page)

            mapPages.append(page)
        }

        // 2. Draw the contents page last, now that the map pages are ready.
        let contents = Page(pdf, A4.PORTRAIT, Page.DETACHED)

        var text = TextLine(f2, "Maps")
        text.setStructureType(StructElem.H1)
        text.setFontSize(24.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(contents)

        var y: Float = 130.0
        for i in 0..<titles.count {
            text = TextLine(f1, titles[i] + " . . . . . . . . . . page " + String(i + 2))
            text.setFontSize(14.0)
            text.setLocation(50.0, y)
            text.drawOn(contents)
            y += 25.0
        }

        let textBlock = TextBlock(f1,
                "This page was drawn after the two map pages, but it is the first page "
                + "of the document, because the pages were created detached and added "
                + "to the PDF in reading order with addPage.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setTextColor(Color.gray)
        textBlock.setLocation(50.0, y + 20.0)
        textBlock.setWidth(contents.getWidth() - 100.0)
        textBlock.drawOn(contents)

        // 3. Add the pages in reading order.
        pdf.addPage(contents)
        for i in 0..<mapPages.count {
            pdf.addPage(mapPages[i])
        }

        try pdf.complete()
    }
}   // End of Example_36.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_36()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_36 => \(String(format: "%4lld", time1 - time0)) ms")
