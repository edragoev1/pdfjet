/**
 * LongWordTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct LongWordTests {
    @Test func aWordIsBrokenBetweenItsCharactersAsTheGoPortBreaksIt() {
        // Five lines: pneumono, ultramicros, copicsilico, volcanoco, niosis
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let block = TextBlock(font, "pneumonoultramicroscopicsilicovolcanoconiosis").setWidth(60)
        TestSupport.expectXY(60, 69.36, block.setLocation(0, 0).drawOn(nil))
    }

    @Test func aLongWordIsBrokenInTimeThatGrowsWithTheWord() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let word = String(repeating: "abcdefghij", count: 10000)    // 100,000 characters
        let block = TextBlock(font, word).setWidth(100)
        let start = Date()
        let xy = block.setLocation(0, 0).drawOn(nil)
        let took = Date().timeIntervalSince(start)
        #expect(took < 3, "breaking a word of 100,000 characters took \(took) s")
        TestSupport.expectXY(100, 78030, xy)   // 5625 lines of 13.872 points
    }
}
