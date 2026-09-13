/**
 * QRCodeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// Java also tests that data that does not fit throws; Swift stops with
/// fatalError there, which a test cannot catch.
@Suite struct QRCodeTests {
    private func dark(_ modules: [[Bool?]], _ row: Int, _ column: Int) -> Bool {
        return modules[row][column] == true
    }

    @Test func theSymbolIs33ModulesAtEveryLevel() throws {
        for level in [ErrorCorrectionLevel.L, .M, .Q, .H] {
            let modules = try #require(QRCode("Hello", level).getModules())
            #expect(modules.count == 33, "\(level)")
            #expect(modules[0].count == 33, "\(level)")
        }
    }

    @Test func finderPatternsAreInThreeCorners() throws {
        let modules = try #require(QRCode("Hello", ErrorCorrectionLevel.L).getModules())
        for i in 0..<7 {
            #expect(dark(modules, 0, i), "top left, top row")
            #expect(dark(modules, 0, 32 - i), "top right, top row")
            #expect(dark(modules, 32, i), "bottom left, bottom row")
            #expect(dark(modules, i, 0), "top left, left column")
        }
        #expect(!dark(modules, 0, 7), "separator")
        #expect(!dark(modules, 7, 0), "separator")
    }

    @Test func dataThatFitsTheSymbol() throws {
        let modules = try #require(QRCode(String(repeating: "a", count: 50), ErrorCorrectionLevel.M).getModules())
        #expect(modules.count == 33)
    }

    @Test func drawOnReturnsTheSameCornerEveryTime() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let qr = QRCode("Hello", ErrorCorrectionLevel.L).setLocation(10, 10).setModuleLength(2)
        TestSupport.expectXY(76, 76, qr.drawOn(page))
        TestSupport.expectXY(76, 76, qr.drawOn(page))
    }
}
