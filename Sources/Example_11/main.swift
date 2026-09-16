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
 */
public class Example_11 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_11.pdf", append: false)!)
        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)

        var code = try Barcode(Barcode.CODE_128, "Hellö, World!")
        code.setLocation(170.0, 70.0)
        code.setModuleLength(0.75)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.CODE_128, "G86513JVW0C")
        code.setLocation(170.0, 170.0)
        code.setModuleLength(0.75)
        code.setDirection(Direction.TOP_TO_BOTTOM)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.CODE_39, "WIKIPEDIA")
        code.setLocation(270.0, 370.0)
        code.setModuleLength(0.75)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.CODE_39, "CODE39")
        code.setLocation(400.0, 70.0)
        code.setModuleLength(0.75)
        code.setDirection(Direction.TOP_TO_BOTTOM)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.CODE_39, "CODE39")
        code.setLocation(450.0, 70.0)
        code.setModuleLength(0.75)
        code.setDirection(Direction.BOTTOM_TO_TOP)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.UPC_A, "51234567890") // UPC-A without the check digit which we calculate!!
        code.setLocation(450.0, 250.0)
        code.setModuleLength(1.0)
        code.setDirection(Direction.BOTTOM_TO_TOP)
        code.setFont(f1)
        code.drawOn(page)

        code = try Barcode(Barcode.EAN_13, "051234567890") // EAN-13 without the check digit which we calculate!!
        code.setLocation(450.0, 450.0)
        code.setModuleLength(1.0)
        code.setDirection(Direction.BOTTOM_TO_TOP)
        code.setFont(f1)
        code.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_11.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_11()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_11 => \(String(format: "%4lld", time1 - time0)) ms")
