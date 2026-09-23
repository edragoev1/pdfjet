/**
 * BarcodeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct BarcodeTests {
    @Test func drawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection() throws {
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
            let barcode = try Barcode(row.0, row.1).setLocation(100, 100).setDirection(row.2)
            if row.3 {
                _ = barcode.setFont(font)
            }
            let name = "\(row.0) \(row.2) font \(row.3)"
            let first = barcode.drawOn(page)
            TestSupport.expectNear(row.4, first[0], TestSupport.delta, "\(name) x")
            TestSupport.expectNear(row.5, first[1], TestSupport.delta, "\(name) y")
            let second = barcode.drawOn(page)
            #expect(first == second, "\(name) drawn again")
            TestSupport.expectNear(row.5 - 100, barcode.getHeight(), TestSupport.delta, "\(name) height")
        }
    }

    /// Java checks the Code 39 text in drawOn; the Swift drawOn cannot throw,
    /// so the initializer checks it.
    @Test func code39RejectsCharactersItCannotEncode() {
        let error = #expect(throws: PDFjetError.self) { _ = try Barcode(Barcode.CODE_39, "hello") }
        #expect(error?.message == "The input string '*hello*' contains characters that are invalid in a Code39 barcode.")
    }

    @Test func upcAndEanNeedTheirNumberOfDigits() {
        let upc = #expect(throws: PDFjetError.self) { _ = try Barcode(Barcode.UPC_A, "123") }
        #expect(upc?.message == "UPC-A barcodes must have exactly 11 digits!")
        let ean = #expect(throws: PDFjetError.self) { _ = try Barcode(Barcode.EAN_13, "0123456789012") }
        #expect(ean?.message == "EAN-13 barcodes must have exactly 12 digits!")
    }

    @Test func code128RefusesATextItCannotHold() throws {
        let tooLong = "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!"
        for (text, message) in [(String(repeating: "7", count: 49), tooLong),
                                (String(repeating: "\u{e9}", count: 25), tooLong),
                                ("A\u{20ac}", "Code 128 barcodes can only hold characters up to U+00FF!")] {
            let error = #expect(throws: PDFjetError.self) { _ = try Barcode(Barcode.CODE_128, text) }
            #expect(error?.message == message)
        }
        // The most a barcode holds is drawn whole: 48 codewords, and the start,
        // the check digit and the stop, of 11 modules each but the stop of 13
        for text in [String(repeating: "7", count: 48), String(repeating: "\u{e9}", count: 24)] {
            #expect(try Barcode(Barcode.CODE_128, text).drawOn(nil)[0] == Float(11 * 50 + 13) * 0.75)
        }
    }

    @Test func isBlackWhateverPenColorThePageHas() throws {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setPenColor(Color.blue)
        try Barcode(Barcode.CODE_128, "AB").drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("q\n0 0 0 RG\n") && content.hasSuffix("Q\n"))
    }

    /// The GS1-128 barcodes below were read back with ZXing, which gave the
    /// symbology identifier ]C1 of GS1-128.
    @Test func gs1128TakesCodeSetCForRunsOfDigits() throws {
        // Start C, FNC1, 10 codewords for the 20 digits, the check digit and the stop
        #expect(try Barcode(Barcode.GS1_128, "(00)106141412345678908").drawOn(nil)[0] == Float(11 * 13 + 13) * 0.75)
        // Start B, FNC1, 1 0 A 1 in code set B, Code C, 23 45, Code B, B,
        // FNC1 after the batch, 2 1 7: 14 codewords, with the start and the check digit 16
        #expect(try Barcode(Barcode.GS1_128, "(10)A12345B(21)7").drawOn(nil)[0] == Float(11 * 16 + 13) * 0.75)
        #expect(Barcode.gs1128Codewords("(10)A12345B(21)7").codewords ==
                [104, 102, 17, 16, 33, 17, 99, 23, 45, 100, 34, 102, 18, 17, 23])
    }

    @Test func gs1128RefusesDataThatIsNotGS1OrTooLong() throws {
        _ = try Barcode(Barcode.GS1_128, "(91)" + String(repeating: "X", count: 46))     // 48 characters
        for (data, message) in [
            ("(91)" + String(repeating: "X", count: 47), "GS1-128 barcodes hold at most 48 characters, not counting the separators!"),
            ("(01)09506000134353", "The check digit of (01) is wrong!"),
            ("01095060001343", "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"),
        ] {
            let error = #expect(throws: PDFjetError.self) { _ = try Barcode(Barcode.GS1_128, data) }
            #expect(error?.message == message)
        }
    }
}
