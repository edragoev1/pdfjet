/**
 * BarcodeColorTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The color of the barcodes, setModuleColor, black unless set.
@Suite struct BarcodeColorTests {
    private static let navy: Int32 = 0x1F3A5F
    private static let navyStroke = "0.12 0.23 0.37 RG"
    private static let navyFill = "0.12 0.23 0.37 rg"

    @Test func aLinearBarcodeDrawsItsBarsAndItsTextInItsColor() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let barcode = try Barcode(Barcode.CODE_128, "A1").setFont(TestSupport.helvetica(pdf))
        barcode.setLocation(50, 50)
        barcode.setModuleColor(BarcodeColorTests.navy).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(BarcodeColorTests.navyStroke), "\(content)")
        #expect(TestSupport.fillColorBefore(content, "A1") == BarcodeColorTests.navyFill)
    }

    @Test func aLinearBarcodeIsBlackUnlessSet() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        page.setPenColor(Color.red)
        let barcode = try Barcode(Barcode.CODE_128, "A1").setFont(TestSupport.helvetica(pdf))
        barcode.setLocation(50, 50)
        barcode.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("0 0 0 RG"), "\(content)")
    }

    // The stroke and fill colors the content sets, as their operators, for
    // example "1 1 1 rg", in their order.
    private func colors(_ content: String) -> [String] {
        return content.split(separator: "\n").map(String.init).filter { $0.hasSuffix(" RG") || $0.hasSuffix(" rg") }
    }

    // Draws each linear barcode but Code 128 in each direction, with its text,
    // on a page of its own, in the module color when it is not nil, and
    // returns the names and the contents of the pages.
    private func drawLinearBarcodes(_ moduleColor: Int32?) throws -> [(String, String)] {
        let cases = [
            ("UPC-A", Barcode.UPC_A, "01234567890"),
            ("EAN-13", Barcode.EAN_13, "012345678901"),
            ("Code 39", Barcode.CODE_39, "HELLO-39"),
            ("ITF-14", Barcode.ITF_14, "1540014128876"),
        ]
        var drawn = [(String, String)]()
        for (name, barcodeType, text) in cases {
            for direction in [Direction.LEFT_TO_RIGHT, Direction.BOTTOM_TO_TOP, Direction.TOP_TO_BOTTOM] {
                let pdf = TestSupport.newPDF()
                let page = Page(pdf, Letter.PORTRAIT)
                let barcode = try Barcode(barcodeType, text).setFont(TestSupport.helvetica(pdf))
                _ = barcode.setDirection(direction)
                barcode.setLocation(50, 50)
                if let moduleColor = moduleColor {
                    _ = barcode.setModuleColor(moduleColor)
                }
                barcode.drawOn(page)
                drawn.append(("\(name), \(direction)", TestSupport.content(page)))
            }
        }
        return drawn
    }

    // UPC-A, EAN-13, Code 39 and ITF-14 draw their bars and their text in
    // their color in every direction, as Code 128 does: only Code 128, from
    // left to right, was tested (the review of 28 October 2026).
    @Test func everyLinearBarcodeDrawsInItsColorInEveryDirection() throws {
        for (name, content) in try drawLinearBarcodes(BarcodeColorTests.navy) {
            let colors = colors(content)
            #expect(colors.contains(BarcodeColorTests.navyStroke) && colors.contains(BarcodeColorTests.navyFill)
                    && content.contains("] TJ"), "\(name): not drawn in navy with its text: \(colors)")
            for op in colors {
                #expect(op == BarcodeColorTests.navyStroke || op == BarcodeColorTests.navyFill, "\(name): \(op)")
            }
        }
    }

    // Unless their color is set, they are black: drawn as they are with black
    // set.
    @Test func everyLinearBarcodeIsBlackUnlessSet() throws {
        let unset = try drawLinearBarcodes(nil)
        for (name, content) in unset {
            let colors = colors(content)
            #expect(colors.contains("0 0 0 RG") && colors.contains("0 0 0 rg"),
                    "\(name): not drawn in black: \(colors)")
            for op in colors {
                #expect(op == "0 0 0 RG" || op == "0 0 0 rg", "\(name): \(op)")
            }
        }
        let black = try drawLinearBarcodes(Color.black)
        try #require(black.count == unset.count)
        for i in unset.indices {
            #expect(unset[i].1 == black[i].1, "\(unset[i].0): black set draws another barcode")
        }
    }

    @Test func aPDF417DrawsInItsColor() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        try PDF417("A1").setLocation(50, 50).setModuleColor(BarcodeColorTests.navy).drawOn(page)
        #expect(TestSupport.content(page).contains(BarcodeColorTests.navyStroke), "\(TestSupport.content(page))")
    }

    @Test func theTwoDimensionalCodesDrawInTheirColor() throws {
        let pdf = TestSupport.newPDF()
        var page = Page(pdf, Letter.PORTRAIT)
        let qr = try QRCode("A1", ErrorCorrectionLevel.M).setModuleColor(BarcodeColorTests.navy)
        qr.setLocation(50, 50)
        qr.drawOn(page)
        #expect(TestSupport.content(page).contains(BarcodeColorTests.navyFill), "\(TestSupport.content(page))")
        page = Page(pdf, Letter.PORTRAIT)
        let dm = DataMatrix("A1").setModuleColor(BarcodeColorTests.navy)
        dm.setLocation(50, 50)
        dm.drawOn(page)
        #expect(TestSupport.content(page).contains(BarcodeColorTests.navyFill), "\(TestSupport.content(page))")
    }
}
