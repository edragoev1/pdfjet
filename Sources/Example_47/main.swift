/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_47.swift
 */
public class Example_47 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_47.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Text flowing through columns")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(14.0)

        let paragraphs = try Content.ofTextFile(
                "data/dostoevsky.txt").components(separatedBy: "\n\n")

        var x: Float = 50.0
        var y: Float = 50.0
        let w: Float = 230.0
        let h: Float = 500.0
        let gap: Float = 20.0

        var page: Page? = nil
        let textFrame = TextFrame(f1, paragraphs)
        var first = true
        while textFrame.hasMoreText() {
            page = Page(pdf, Letter.LANDSCAPE)
            if first {
                // The heading, on the first page only, which its columns start under
                TextLine(f1, "The Idiot, by Fyodor Dostoevsky")
                        .setStructureType(StructElem.H1)
                        .setFontSize(20.0)
                        .setLocation(50.0, 50.0)
                        .drawOn(page!)
                y = 80.0
                first = false
            }

            textFrame.setLocation(x, y)
            textFrame.setWidth(w)
            textFrame.setHeight(h)
            textFrame.drawOn(page)

            if (textFrame.hasMoreText()) {
                x += w + gap
                textFrame.setLocation(x, y)
                textFrame.setWidth(w)
                textFrame.setHeight(h)
                textFrame.drawOn(page)
            }

            if (textFrame.hasMoreText()) {
                x += w + gap
                textFrame.setLocation(x, y)
                textFrame.setWidth(w)
                textFrame.setHeight(h)
                textFrame.drawOn(page)
            }

            x = 50.0
            y = 50.0
        }

        try pdf.complete()
    }
}   // End of Example_47.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_47()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_47 => \(String(format: "%4lld", time1 - time0)) ms")
