/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_11.swift
 * This example draws a Code 128, a Code 39, a UPC-A and an EAN-13 barcode,
 * each next to a label, and then barcodes drawn from top to bottom and from
 * bottom to top.
 */
public class Example_11 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_11.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        let text = TextLine(f2, "Linear Barcodes")
        text.setFontSize(22.0)
        text.setLocation(70.0, 80.0)
        text.drawOn(page)

        let labels = [
            "Code 128",
            "Code 39",
            "UPC-A",
            "EAN-13",
        ]
        let notes = [
            "Letters, digits and symbols",
            "Upper case letters and digits",
            "11 digits, the check digit is added",
            "12 digits, the check digit is added",
        ]
        let barcodes = [
            try Barcode(Barcode.CODE_128, "Hellö, World!"),
            try Barcode(Barcode.CODE_39, "WIKIPEDIA"),
            try Barcode(Barcode.UPC_A, "51234567890"),
            try Barcode(Barcode.EAN_13, "051234567890"),
        ]
        // UPC-A and EAN-13 need wider bars for the digits under them.
        let moduleLengths: [Float] = [0.75, 0.75, 1.0, 1.0]

        var y: Float = 130.0
        for i in 0..<barcodes.count {
            TextLine(f2, labels[i]).setLocation(70.0, y + 15.0).drawOn(page)
            let note = TextLine(f1, notes[i])
            note.setFontSize(10.0)
            note.setTextColor(Color.gray)
            note.setLocation(70.0, y + 32.0)
            note.drawOn(page)

            let barcode = barcodes[i]
            barcode.setLocation(290.0, y)
            barcode.setModuleLength(moduleLengths[i])
            barcode.setFont(f1)
            let xy = barcode.drawOn(page)
            y = xy[1] + 30.0
        }

        // The same barcodes can be drawn from top to bottom and from bottom to top.
        TextLine(f2, "Vertical barcodes").setLocation(70.0, y + 15.0).drawOn(page)

        var barcode = try Barcode(Barcode.CODE_128, "G86513JVW0C")
        barcode.setLocation(70.0, y + 35.0)
        barcode.setModuleLength(0.75)
        barcode.setDirection(Direction.TOP_TO_BOTTOM)
        barcode.setFont(f1)
        var xy = barcode.drawOn(page)

        barcode = try Barcode(Barcode.CODE_39, "CODE39")
        barcode.setLocation(xy[0] + 60.0, y + 35.0)
        barcode.setModuleLength(0.75)
        barcode.setDirection(Direction.BOTTOM_TO_TOP)
        barcode.setFont(f1)
        xy = barcode.drawOn(page)

        barcode = try Barcode(Barcode.EAN_13, "051234567890")
        barcode.setLocation(xy[0] + 60.0, y + 35.0)
        barcode.setModuleLength(1.0)
        barcode.setDirection(Direction.BOTTOM_TO_TOP)
        barcode.setFont(f1)
        barcode.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_11.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_11()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_11 => \(String(format: "%4lld", time1 - time0)) ms")
