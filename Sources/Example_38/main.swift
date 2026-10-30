/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_38.swift
 * This example draws a PDF417 barcode that holds a whole source file, with an
 * explanation and a caption.
 */
public class Example_38 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_38.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("PDF417 barcode example")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "PDF417 Barcode")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "PDF417 is a stacked two-dimensional barcode, used on boarding passes, "
                + "identity cards and shipping labels. It holds text and binary data, "
                + "and its error correction lets a scanner read it even when part of "
                + "it is damaged.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(70.0, 95.0)
        textBlock.setWidth(470.0)
        var xy = textBlock.drawOn(page)

        // A barcode that holds a whole source file.
        let lines = try Content.linesOfTextFile("resources/data/pdf417-sample.java")
        var buf = String()
        for line in lines {
            buf.append(line)
            buf.append("\r\n")  // CR and LF both required!
        }

        let barcode = try PDF417(buf)
        barcode.setModuleLength(1.0)
        barcode.setLocation(70.0, xy[1] + 30.0)
        xy = barcode.drawOn(page)

        text = TextLine(f1, "The source code of resources/data/pdf417-sample.java, "
                + String(lines.count) + " lines")
        text.setFontSize(10.0)
        text.setTextColor(Color.dimgray)
        text.setLocation(70.0, xy[1] + 20.0)
        text.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_38.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_38()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_38 => \(String(format: "%4lld", time1 - time0)) ms")
