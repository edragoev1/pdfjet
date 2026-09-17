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

    @Test func shortDataKeepsTheSymbolAt33ModulesAtEveryLevel() throws {
        for level in [ErrorCorrectionLevel.L, .M, .Q, .H] {
            let modules = try #require(try QRCode("Hello", level).getModules())
            #expect(modules.count == 33, "\(level)")
            #expect(modules[0].count == 33, "\(level)")
        }
    }

    @Test func longerDataMakesALargerSymbol() throws {
        #expect(try QRCode(String(repeating: "a", count: 78), ErrorCorrectionLevel.L).getModules()!.count == 33)     // version 4
        #expect(try QRCode(String(repeating: "a", count: 79), ErrorCorrectionLevel.L).getModules()!.count == 37)     // version 5
        #expect(try QRCode(String(repeating: "a", count: 2953), ErrorCorrectionLevel.L).getModules()!.count == 177)  // version 40
        #expect(try QRCode(String(repeating: "a", count: 1273), ErrorCorrectionLevel.H).getModules()!.count == 177)  // version 40
    }

    @Test func versionsFrom7CarryTheirVersionNumber() throws {
        // 120 bytes at level M need version 7, 45 modules, whose version information is 0x07C94.
        let modules = try #require(try QRCode(String(repeating: "a", count: 120), ErrorCorrectionLevel.M).getModules())
        #expect(modules.count == 45)
        for i in 0..<18 {
            let bit = ((0x07C94 >> i) & 1) == 1
            #expect(dark(modules, i / 3, i % 3 + 45 - 11) == bit, "top right, bit \(i)")
            #expect(dark(modules, i % 3 + 45 - 11, i / 3) == bit, "bottom left, bit \(i)")
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

    @Test func dataThatDoesNotFitVersion40Throws() throws {
        #expect(throws: PDFjetError.self) { _ = try QRCode(String(repeating: "a", count: 2954), ErrorCorrectionLevel.L) }
        #expect(throws: PDFjetError.self) { _ = try QRCode(String(repeating: "a", count: 1274), ErrorCorrectionLevel.H) }
    }

    @Test func drawOnReturnsTheSameCornerEveryTime() throws {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let qr = try QRCode("Hello", ErrorCorrectionLevel.L).setLocation(10, 10).setModuleLength(2)
        TestSupport.expectXY(76, 76, qr.drawOn(page))
        TestSupport.expectXY(76, 76, qr.drawOn(page))
    }
}
