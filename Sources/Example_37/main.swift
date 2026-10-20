/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_37.swift
 *
 * Draws two-dimensional barcodes. The same web address as a QR code with
 * each of the four error correction levels, where a higher level lets a
 * scanner read a code that is more damaged or covered and leaves room for
 * less data; GS1 data as a GS1 Digital Link in a QR code; and Data Matrix
 * symbols of a web address, text in UTF-8, a rectangle, a larger symbol and
 * GS1 data.
 */
public class Example_37 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_37.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("QR Codes and Data Matrix Barcodes")

        try drawQRCodes(pdf)
        try drawDataMatrix(pdf)

        try pdf.complete()
    }

    private func drawQRCodes(_ pdf: PDF) throws {
        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "QR Code Error Correction")
        text.setStructureType(StructElem.H1)
        text.setFontSize(22.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "Each QR code below holds the same address, the home page of PDFjet. "
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
            text.setTextColor(Color.dimgray)
            text.setLocation(x, xy[1] + 35.0)
            text.drawOn(page)
        }

        // GS1 data as a GS1 Digital Link, a web address an ordinary QR code carries.
        let page2 = Page(pdf, Letter.PORTRAIT)
        text = TextLine(f2, "GS1 Digital Link")
        text.setStructureType(StructElem.H2)
        text.setFontSize(18.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page2)

        let link = try GS1.digitalLink(domain: "https://id.gs1.org", data: "(01)09506000134352(10)ABC123(17)261231")
        let note = TextBlock(f1, "The GS1 data (01)09506000134352(10)ABC123(17)261231, a GTIN, a batch and an expiry date, "
                + "is this web address, a GS1 Digital Link, which an ordinary QR code carries and any phone opens:")
        note.setFontSize(12.0)
        note.setLineSpacing(1.5)
        note.setLocation(70.0, 95.0)
        note.setWidth(470.0)
        let corner = note.drawOn(page2)

        // The address as a link, not as text in the paragraph, which PDF/UA
        // checkers take for a link without its Link element
        text = TextLine(f1, link)
        text.setFontSize(12.0)
        text.setTextColor(Color.blue)
        text.setUnderline(true)
        text.setURIAction(link)
        text.setLocation(70.0, corner[1] + 20.0)
        text.drawOn(page2)

        let code = try QRCode(link, ErrorCorrectionLevel.M)
        code.setModuleLength(4.0)
        code.setLocation(70.0, corner[1] + 50.0)
        code.drawOn(page2)
    }

    private func drawDataMatrix(_ pdf: PDF) throws {
        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(10.0)

        let page = Page(pdf, Letter.PORTRAIT)

        // The heading of the document, above the symbols
        TextLine(f1, "Data Matrix Barcodes")
                .setStructureType(StructElem.H1)
                .setFontSize(18.0)
                .setLocation(50.0, 36.0)
                .drawOn(page)

        var barcode = DataMatrix("https://github.com/edragoev1/pdfjet")
        barcode.setLocation(50.0, 50.0)
        barcode.setModuleLength(3.0)
        var xy = barcode.drawOn(page)
        var caption = TextLine(f1, "A web address")
        caption.setLocation(50.0, xy[1] + 20.0)
        caption.drawOn(page)

        barcode = DataMatrix("Grüße aus München! こんにちは 😀")
        barcode.setLocation(300.0, 50.0)
        barcode.setModuleLength(3.0)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "Text in UTF-8")
        caption.setLocation(300.0, xy[1] + 20.0)
        caption.drawOn(page)

        barcode = DataMatrix("PDFjet 9.1.1", DataMatrix.RECTANGLE)
        barcode.setLocation(50.0, 250.0)
        barcode.setModuleLength(4.0)
        barcode.setModuleColor(Color.blue)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "A rectangular symbol")
        caption.setLocation(50.0, xy[1] + 20.0)
        caption.drawOn(page)

        var sb = ""
        for i in 1...20 {
            sb += "Line \(i) of a longer text in a larger symbol.\n"
        }
        barcode = DataMatrix(sb)
        barcode.setLocation(300.0, 250.0)
        barcode.setModuleLength(2.0)
        xy = barcode.drawOn(page)
        caption = TextLine(f1, "A larger symbol")
        caption.setLocation(300.0, xy[1] + 20.0)
        caption.drawOn(page)

        barcode = try DataMatrix(gs1: "(01)09506000134352(17)261231(10)ABC123(21)XYZ-42")
        barcode.setLocation(50.0, 400.0)
        barcode.setModuleLength(3.0)
        xy = barcode.drawOn(page)
        // On two lines, clear of the larger symbol at its right
        caption = TextLine(f1, "GS1: a GTIN, an expiry date,")
        caption.setLocation(50.0, xy[1] + 20.0)
        caption.drawOn(page)
        caption = TextLine(f1, "a batch and a serial number")
        caption.setLocation(50.0, xy[1] + 20.0 + 14.0)  // A line of 10 points, and its spacing
        caption.drawOn(page)
    }

}   // End of Example_37.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_37()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_37 => \(String(format: "%4lld", time1 - time0)) ms")
