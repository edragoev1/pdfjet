/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_08.swift
 */
public class Example_08 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_08.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("The Ascent and Descent of a Font")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(72.0)

        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(24.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let x1: Float = 90.0
        let y1: Float = 100.0

        // A heading, which PAC asks a document for
        TextLine(f2, "The Ascent and Descent of a Font")
                .setStructureType(StructElem.H1)
                .setFontSize(18.0)
                .setLocation(x1, 45.0)
                .drawOn(page)

        let textLine = TextLine(f2, "(x1, y1)")
        textLine.setLocation(x1, y1 - 15.0)
        textLine.drawOn(page)

        let textBlock = TextBlock(f1,
            "Hello, World! This example shows the functionality of the TextBlock.")
        textBlock.setLocation(x1, y1)
        textBlock.setWidth(500.0)
        textBlock.setBorderColor(0xF1F3F6)     // The slate of the charts
        textBlock.setBackgroundColor(0xF1F3F6)
        textBlock.setTextColor(Color.black)
        let xy = textBlock.drawOn(page)

        // Text on the left
        let ascentText = TextLine(f2, "Ascent")
        ascentText.setFontSize(18.0)
        ascentText.setLocation(x1 - 85.0, y1 + 40.0)
        ascentText.drawOn(page)

        let descentText = TextLine(f2, "Descent")
        descentText.setFontSize(18.0)
        descentText.setLocation(x1 - 85.0, y1 + f1.getAscent() + 15.0)
        descentText.drawOn(page)

        // Line beside the text ascent
        let ascentMark = Line(
            x1 - 10.0,
            y1,
            x1 - 10.0,
            y1 + f1.getAscent())
        ascentMark.setStrokeColor(0x1F3A5F)     // Navy
        ascentMark.setStrokeWidth(3.0)
        ascentMark.drawOn(page)

        // Line beside the text descent
        let descentMark = Line(
            x1 - 10.0,
            y1 + f1.getAscent(),
            x1 - 10.0,
            y1 + f1.getAscent() + f1.getDescent())
        descentMark.setStrokeColor(0xA7AFB8)      // Gray
        descentMark.setStrokeWidth(3.0)
        descentMark.drawOn(page)

        let baseLine = Line(
                x1,
                y1 + f1.getAscent(),
                xy[0],
                y1 + f1.getAscent())
        baseLine.drawOn(page)

        let descentLine = Line(
                x1,
                y1 + (f1.getAscent() + f1.getDescent()),
                xy[0],
                y1 + (f1.getAscent() + f1.getDescent()))
        descentLine.drawOn(page)

        let ascentLine = Line(
                x1,
                y1 + f1.getBodyHeight() + f1.getAscent(),
                xy[0],
                y1 + f1.getBodyHeight() + f1.getAscent())
        ascentLine.drawOn(page)

        let p1 = Point(x1, y1)
        p1.setRadius(5.0)
        p1.setStrokeColor(0x1F3A5F)
        p1.drawOn(page)

        let p2 = Point(xy[0], xy[1])
        p2.setRadius(5.0)
        p2.setStrokeColor(0x1F3A5F)
        p2.drawOn(page)

        f2.setSize(24.0)
        let textLine3 = TextLine(f2, "(x2, y2)")
        textLine3.setLocation(xy[0] - 80.0, xy[1] + 30.0)
        textLine3.drawOn(page)

        let rect = Rect(xy[0], xy[1], 20.0, 20.0)
        rect.setBorderColor(0x1F3A5F)
        rect.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_08.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_08()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_08 => \(String(format: "%4lld", time1 - time0)) ms")
