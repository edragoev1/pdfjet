/**
 * QRCodeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct QRCodeTests {
    private func dark(_ modules: [[Bool?]], _ row: Int, _ column: Int) -> Bool {
        return modules[row][column] == true
    }

    @Test func theSymbolIs33ModulesAtEveryLevel() throws {
        for level in [ErrorCorrectionLevel.L, .M, .Q, .H] {
            let modules = try #require(try QRCode("Hello", level).getModules())
            #expect(modules.count == 33, "\(level)")
            #expect(modules[0].count == 33, "\(level)")
        }
    }

    @Test func finderPatternsAreInThreeCorners() throws {
        let modules = try #require(try QRCode("Hello", ErrorCorrectionLevel.L).getModules())
        for i in 0..<7 {
            #expect(dark(modules, 0, i), "top left, top row")
            #expect(dark(modules, 0, 32 - i), "top right, top row")
            #expect(dark(modules, 32, i), "bottom left, bottom row")
            #expect(dark(modules, i, 0), "top left, left column")
        }
        #expect(!dark(modules, 0, 7), "separator")
        #expect(!dark(modules, 7, 0), "separator")
    }

    @Test func dataThatDoesNotFitThrows() throws {
        let modules = try #require(try QRCode(String(repeating: "a", count: 50), ErrorCorrectionLevel.M).getModules())
        #expect(modules.count == 33)
        #expect(throws: PDFjetError.self) { _ = try QRCode(String(repeating: "a", count: 80), ErrorCorrectionLevel.L) }
        #expect(throws: PDFjetError.self) { _ = try QRCode(String(repeating: "a", count: 50), ErrorCorrectionLevel.Q) }
    }

    @Test func drawOnReturnsTheSameCornerEveryTime() throws {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let qr = try QRCode("Hello", ErrorCorrectionLevel.L).setLocation(10, 10).setModuleLength(2)
        TestSupport.expectXY(76, 76, qr.drawOn(page))
        TestSupport.expectXY(76, 76, qr.drawOn(page))
    }
}
