/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_21.swift
 * This example draws the same web address as a QR code with each of the four
 * error correction levels. A higher level lets a scanner read a code that is
 * more damaged or covered, and leaves room for less data in the code.
 */
public class Example_21 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_21.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("QR Code Error Correction")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "QR Code Error Correction")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "Each QR code below holds the same address, https://pdfjet.com. "
                + "A higher error correction level lets a scanner read the code when "
                + "more of it is damaged or covered, and leaves room for less data, "
                + "so longer data at a higher level makes a larger code.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(70.0, 95.0)
        textBlock.setWidth(470.0)
        textBlock.drawOn(page)

        let levels = [
            ErrorCorrectionLevel.L,
            ErrorCorrectionLevel.M,
            ErrorCorrectionLevel.Q,
            ErrorCorrectionLevel.H,
        ]
        let names = [
            "L (Low)",
            "M (Medium)",
            "Q (Quartile)",
            "H (High)",
        ]
        let notes = [
            "About 7% can be restored, 78 bytes fit in this size",
            "About 15% can be restored, 62 bytes fit in this size",
            "About 25% can be restored, 46 bytes fit in this size",
            "About 30% can be restored, 34 bytes fit in this size",
        ]

        // Two rows of two codes.
        for i in 0..<levels.count {
            let x: Float = 70.0 + Float(i % 2) * 250.0
            let y: Float = 200.0 + Float(i / 2) * 250.0

            let qr = try QRCode("https://pdfjet.com", levels[i])
            qr.setModuleLength(5.0)
            qr.setLocation(x, y)
            let xy = qr.drawOn(page)

            text = TextLine(f2, names[i])
            text.setFontSize(12.0)
            text.setLocation(x, xy[1] + 20.0)
            text.drawOn(page)

            text = TextLine(f1, notes[i])
            text.setFontSize(10.0)
            text.setTextColor(Color.gray)
            text.setLocation(x, xy[1] + 35.0)
            text.drawOn(page)
        }

        try pdf.complete()
    }
}   // End of Example_21.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_21()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_21 => \(String(format: "%4lld", time1 - time0)) ms")
