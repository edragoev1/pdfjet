/**
 * BarcodeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// Java also tests the errors for invalid Code 39 characters and wrong UPC-A
/// and EAN-13 digit counts; Swift stops with fatalError there, which a test
/// cannot catch.
@Suite struct BarcodeTests {
    @Test func drawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        // type, text, direction, with font, corner x, corner y
        let corners: [(Int, String, Direction, Bool, Float, Float)] = [
            (Barcode.EAN_13, "012345678901", .LEFT_TO_RIGHT, false, 171.25, 145.5),
            (Barcode.EAN_13, "012345678901", .LEFT_TO_RIGHT, true, 171.25, 151.31),
            (Barcode.EAN_13, "012345678901", .BOTTOM_TO_TOP, false, 145.5, 171.25),
            (Barcode.EAN_13, "012345678901", .BOTTOM_TO_TOP, true, 151.31, 179.59),
            (Barcode.EAN_13, "012345678901", .TOP_TO_BOTTOM, false, 145.5, 171.25),
            (Barcode.EAN_13, "012345678901", .TOP_TO_BOTTOM, true, 145.5, 171.25),
            (Barcode.UPC_A, "01234567890", .LEFT_TO_RIGHT, false, 171.25, 145.5),
            (Barcode.UPC_A, "01234567890", .LEFT_TO_RIGHT, true, 179.59, 151.31),
            (Barcode.UPC_A, "01234567890", .BOTTOM_TO_TOP, false, 145.5, 171.25),
            (Barcode.UPC_A, "01234567890", .BOTTOM_TO_TOP, true, 151.31, 179.59),
            (Barcode.UPC_A, "01234567890", .TOP_TO_BOTTOM, false, 145.5, 171.25),
            (Barcode.UPC_A, "01234567890", .TOP_TO_BOTTOM, true, 145.5, 179.59),
            (Barcode.CODE_128, "Hello", .LEFT_TO_RIGHT, false, 167.5, 137.5),
            (Barcode.CODE_128, "Hello", .LEFT_TO_RIGHT, true, 167.5, 154.072),
            (Barcode.CODE_128, "Hello", .BOTTOM_TO_TOP, false, 137.5, 167.5),
            (Barcode.CODE_128, "Hello", .BOTTOM_TO_TOP, true, 154.072, 167.5),
            (Barcode.CODE_128, "Hello", .TOP_TO_BOTTOM, false, 137.5, 167.5),
            (Barcode.CODE_128, "Hello", .TOP_TO_BOTTOM, true, 137.5, 167.5),
            (Barcode.CODE_39, "HELLO-39", .LEFT_TO_RIGHT, false, 219.25, 137.5),
            (Barcode.CODE_39, "HELLO-39", .LEFT_TO_RIGHT, true, 219.25, 154.072),
            (Barcode.CODE_39, "HELLO-39", .BOTTOM_TO_TOP, false, 137.5, 219.25),
            (Barcode.CODE_39, "HELLO-39", .BOTTOM_TO_TOP, true, 154.072, 219.25),
            (Barcode.CODE_39, "HELLO-39", .TOP_TO_BOTTOM, false, 137.5, 219.25),
            (Barcode.CODE_39, "HELLO-39", .TOP_TO_BOTTOM, true, 137.5, 219.25),
        ]
        for row in corners {
            let barcode = Barcode(row.0, row.1).setLocation(100, 100).setDirection(row.2)
            if row.3 {
                _ = barcode.setFont(font)
            }
            let name = "\(row.0) \(row.2) font \(row.3)"
            let first = barcode.drawOn(page)
            TestSupport.expectNear(row.4, first[0], TestSupport.delta, "\(name) x")
            TestSupport.expectNear(row.5, first[1], TestSupport.delta, "\(name) y")
            let second = barcode.drawOn(page)
            #expect(first == second, "\(name) drawn again")
            TestSupport.expectNear(row.3 ? 51.372 : 37.5, barcode.getHeight(), TestSupport.delta, "\(name) height")
        }
    }
}
