/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_22.swift
 * This example links a contents page to three chapters, and each chapter
 * back to the contents, with destinations and "Go To" actions.
 */
public class Example_22 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_22.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Internal links and destinations")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let chapters = [
            "Destinations",
            "Go To actions",
            "Links on shapes and images",
        ]
        let texts = [
            "A destination is a named place in a document. The title of this chapter "
                + "is the destination \"chapter1\", set with setDestination.",
            "A Go To action makes something a link to a destination. The chapter titles "
                + "on the contents page are text lines with a Go To action.",
            "Rectangles and images can be links too. Click the arrow in the top left "
                + "corner, or the arrow image next to it, to go back to the contents.",
        ]

        // The contents page. The destination is the top of the page.
        var page = Page(pdf, Letter.PORTRAIT)
        page.addDestination("contents", 0.0, 0.0)

        var text = TextLine(f2, "Contents")
        text.setStructureType(StructElem.H1)
        text.setFontSize(24.0)
        text.setLocation(90.0, 100.0)
        text.drawOn(page)

        // The contents are a list: each item is the number of its chapter,
        // which labels the item, and the title of the chapter, which is the
        // body of the item and the link to it. A reader reads them as the
        // items of a list and not as lines of text that follow one another.
        var y: Float = 150.0
        page.beginStructElement(StructElem.L)
        for i in 0..<chapters.count {
            page.beginStructElement(StructElem.LI)
            let label = "Chapter " + String(i + 1) + ":"
            text = TextLine(f1, label)
            text.setStructureType(StructElem.LBL)
            text.setFontSize(14.0)
            text.setLocation(90.0, y)
            text.drawOn(page)

            // The title, its underline and its link are the body of the item,
            // which is what an LI may hold beside its label.
            page.beginStructElement(StructElem.LBODY)
            text = TextLine(f1, chapters[i])
            text.setFontSize(14.0)
            text.setTextColor(Color.blue)
            text.setUnderline(true)
            text.setGoToAction("chapter" + String(i + 1))
            text.setLocation(90.0 + f1.stringWidth(14.0, label + " "), y)
            text.drawOn(page)
            page.endStructElement()
            page.endStructElement()
            y += 30.0
        }
        page.endStructElement()

        for i in 0..<chapters.count {
            page = Page(pdf, Letter.PORTRAIT)

            // The title of the chapter is its destination.
            text = TextLine(f2, "Chapter " + String(i + 1) + ": " + chapters[i])
            text.setFontSize(20.0)
            text.setDestination("chapter" + String(i + 1))
            text.setLocation(90.0, 100.0)
            text.drawOn(page)

            let textBlock = TextBlock(f1, texts[i])
            textBlock.setFontSize(12.0)
            textBlock.setLineSpacing(1.5)
            textBlock.setLocation(90.0, 125.0)
            textBlock.setWidth(430.0)
            textBlock.drawOn(page)

            text = TextLine(f1, "Back to the contents")
            text.setFontSize(12.0)
            text.setTextColor(Color.blue)
            text.setUnderline(true)
            text.setGoToAction("contents")
            text.setLocation(90.0, 250.0)
            text.drawOn(page)
        }

        // On the last page, a rect with no border links to the contents too.
        let rect = Rect(20.0, 20.0, 20.0, 20.0)
        rect.setGoToAction("contents")
        rect.drawOn(page)

        // Create an up arrow and place it in the rect
        let path = Path()
        path.add(Point(30.0, 21.0))
        path.add(Point(37.0, 29.0))
        path.add(Point(33.0, 29.0))
        path.add(Point(33.0, 39.0))
        path.add(Point(27.0, 39.0))
        path.add(Point(27.0, 29.0))
        path.add(Point(23.0, 29.0))
        path.setClosed(true)
        path.setStrokeColor(Color.deepskyblue)
        path.setFillShape(true)
        path.drawOn(page)

        // And so does an image of an arrow.
        let image = try Image(pdf, "images/up-arrow.png")
        image.setLocation(50.0, 20.0)
        image.setGoToAction("contents")
        image.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_22.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_22()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_22 => \(String(format: "%4lld", time1 - time0)) ms")
