/**
 * DataMatrixTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct DataMatrixTests {
    @Test func sixDigitsFitTheSmallestSquareWithItsFinderPattern() {
        let modules = DataMatrix("123456").getModules()
        #expect(modules.count == 10)
        #expect(modules[0].count == 10)
        for i in 0..<10 {
            #expect(modules[0][i] == (i % 2 == 0), "top row alternates")
            #expect(modules[9][i], "bottom row is solid")
            #expect(modules[i][0], "left column is solid")
        }
    }

    @Test func longerDataGetsALargerSymbol() {
        #expect(DataMatrix(String(repeating: "Z", count: 60)).getModules().count == 32)
    }

    @Test func theRectangleShapeIsWiderThanTall() {
        let modules = DataMatrix("Hello, World!", DataMatrix.RECTANGLE).getModules()
        #expect(modules.count == 12)
        #expect(modules[0].count == 26)
    }

    @Test func drawOnReturnsTheCornerOfTheModules() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let dm = DataMatrix("123456").setLocation(5, 5).setModuleLength(3)
        TestSupport.expectXY(35, 35, dm.drawOn(page))
    }
}
