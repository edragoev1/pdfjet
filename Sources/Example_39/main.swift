/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_39.swift
 *
 * Draws the pages of a document first and finishes them last. The two map
 * pages are drawn first and the contents page last, and the three are added
 * to the PDF in reading order, the contents first. They are made detached,
 * outside the PDF, so a "Page X of N" footer is drawn on each once the
 * number of pages is known.
 */
public class Example_39 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_39.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Maps")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let titles = ["Europe", "Spain"]
        let files = ["resources/images/ee-map.png", "resources/images/spain-admin.jpg"]
        let descriptions = [
                "A map of Europe in which the member states of the European Union are shaded, and Turkey, a candidate to join when the map was made, in another shade.",
                "A map of the 17 autonomous communities of Spain and its two autonomous cities, Ceuta and Melilla, with their capitals.",
        ]

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
            image.setAltDescription(descriptions[i])
            image.scaleBy((page.getWidth() - 100.0) / image.getWidth())
            image.setLocation(50.0, 100.0)
            image.drawOn(page)

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
                + "of the document: the pages were made detached and are added to the PDF "
                + "in reading order. The footer \"Page X of N\" is drawn on each page last, "
                + "when the number of pages is known.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setTextColor(Color.dimgray)
        textBlock.setLocation(50.0, y + 20.0)
        textBlock.setWidth(contents.getWidth() - 100.0)
        textBlock.drawOn(contents)

        // 3. The pages in reading order, and a footer on each, now that their
        // number is known.
        let pages = [contents] + mapPages
        let fontSize: Float = 10.0
        for i in 0..<pages.count {
            let page = pages[i]

            let line = Line(70.0, page.getHeight() - 60.0,
                    page.getWidth() - 70.0, page.getHeight() - 60.0)
            line.setStrokeColor(Color.lightgray)
            line.drawOn(page)

            // A page number is an artifact: a screen reader skips it.
            let footer = "Page " + String(i + 1) + " of " + String(pages.count)
            page.addArtifactBMC()
            page.setBrushColor(Color.black)
            page.drawString(
                    f1,
                    fontSize,
                    footer,
                    (page.getWidth() - f1.stringWidth(fontSize, footer))/2.0,
                    page.getHeight() - 40.0)
            page.addEMC()
        }
        pdf.addPages(pages)

        try pdf.complete()
    }
}   // End of Example_39.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_39()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_39 => \(String(format: "%4lld", time1 - time0)) ms")
