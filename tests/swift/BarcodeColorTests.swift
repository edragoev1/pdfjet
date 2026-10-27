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
