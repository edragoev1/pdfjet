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
        let page = TestSupport.newPage()
        let qr = try QRCode("Hello", ErrorCorrectionLevel.L).setLocation(10, 10).setModuleLength(2)
        TestSupport.expectXY(76, 76, qr.drawOn(page))
        TestSupport.expectXY(76, 76, qr.drawOn(page))
    }
    @Test func aDescribedQRCodeIsAFigure() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let qr = try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M)
        qr.setAltDescription("https://pdfjet.com")
        _ = qr.setLocation(50, 50)
        _ = qr.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.hasPrefix("/Figure <</MCID 0>>\nBDC\n") && !content.contains("/Artifact"))
        // Not described, it is decoration, as before
        let plain = Page(memory.pdf, Letter.PORTRAIT)
        _ = try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M).drawOn(plain)
        #expect(TestSupport.content(plain).hasPrefix("/Artifact BMC\n"))
    }

    // fits says what the initializer would do, at the edge: the longest text
    // that fits, and one byte more, with and without the ECI
    @Test func fitsTellsAtTheEdge() throws {
        #expect(QRCode.fits(String(repeating: "a", count: 2331), ErrorCorrectionLevel.M))
        #expect(!QRCode.fits(String(repeating: "a", count: 2332), ErrorCorrectionLevel.M))
        #expect(QRCode.fits(String(repeating: "\u{20AC}", count: 776), ErrorCorrectionLevel.M))   // 2,328 bytes and the ECI
        #expect(!QRCode.fits(String(repeating: "\u{20AC}", count: 777), ErrorCorrectionLevel.M))  // 2,331 bytes and the ECI
        #expect(QRCode.fits(String(repeating: "a", count: 2953), ErrorCorrectionLevel.L))
        #expect(!QRCode.fits(String(repeating: "a", count: 1274), ErrorCorrectionLevel.H))
        #expect(QRCode.fits("", ErrorCorrectionLevel.H))
        for level in [ErrorCorrectionLevel.L, ErrorCorrectionLevel.M, ErrorCorrectionLevel.Q, ErrorCorrectionLevel.H] {
            for n in [1270, 1273, 1274, 1663, 1664, 2330, 2331, 2332, 2953, 2954] {
                let text = String(repeating: "a", count: n)
                let made = (try? QRCode(text, level)) != nil
                #expect(made == QRCode.fits(text, level), "\(level) \(n)")
            }
        }
    }

}
