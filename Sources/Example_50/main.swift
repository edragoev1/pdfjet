/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_50.swift
 *
 * Does what programs most often do with an existing PDF: it reads the PDF,
 * stamps every page APPROVED with a date, and adds page numbers at the foot.
 * What is drawn on a page that was read goes on top of its content, and the
 * rest of the document stays as it was.
 */
public class Example_50 {
    public init(_ fileName: String) throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_50.pdf", append: false)!)
        var objects = try pdf.read(from: InputStream(fileAtPath: fileName)!)

        // A font added to an existing PDF is embedded as a subset of the glyphs
        // drawn, as in a new PDF. IBM Plex Sans, which PDFjet carries, is read
        // from the library; the path of a font file of your own reads that file.
        let bold = try Font(&objects, IBMPlexSans.Bold).setSize(14.0)
        let regular = try Font(&objects, IBMPlexSans.Regular).setSize(10.0)

        // The date is fixed, so that the four ports write the same PDF: a program
        // would stamp the date of the day.
        let stamp = TextBlock(bold, "APPROVED\n2026-10-30")
        stamp.setWidth(120.0)
        stamp.setPadding(6.0)
        stamp.setTextColor(Color.red)
        stamp.setBorderColor(Color.red)
        stamp.setTextAlignment(Alignment.CENTER)

        let pages = pdf.getPageObjects(from: objects)
        for (i, pageObj) in pages.enumerated() {
            let page = Page(pdf, pageObj)
            page.addResource(bold, &objects)
            page.addResource(regular, &objects)

            stamp.setLocation(page.getWidth() - 156.0, 36.0)
            stamp.drawOn(page)

            let text = "Page \(i + 1) of \(pages.count)"
            let x = (page.getWidth() - regular.stringWidth(text)) / 2.0
            TextLine(regular, text).setLocation(x, page.getHeight() - 36.0).drawOn(page)

            page.complete(&objects) // The graphics stack is unwinded automatically
        }
        try pdf.addObjects(objects)

        try pdf.complete()
    }
}   // End of Example_50.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_50("resources/data/testPDFs/sample-document.pdf")
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_50 => \(String(format: "%4lld", time1 - time0)) ms")
