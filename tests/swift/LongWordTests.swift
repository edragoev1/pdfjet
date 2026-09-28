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
        // 30 s, not the 3 of the other ports: the tests of Swift are built for
        // debugging and share a runner of GitHub, where this took 3.6 s. The
        // time that grew with the square of the word was 39 s and more.
        #expect(took < 30, "breaking a word of 100,000 characters took \(took) s")
        TestSupport.expectXY(100, 78030, xy)   // 5625 lines of 13.872 points
    }

    @Test func aLongRightToLeftWordIsBrokenInTimeThatGrowsWithTheWord() throws {
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.otf.stream"))
        let word = String(repeating: "محمد", count: 25000)    // 100,000 letters, which took about 20 minutes
        let block = TextBlock(font, word).setRightToLeft(true).setWidth(100)
        let start = Date()
        let xy = block.setLocation(0, 0).drawOn(nil)
        let took = Date().timeIntervalSince(start)
        // 30 s, as above: the time that grew with the square of the word was
        // about 20 minutes.
        #expect(took < 30, "breaking a right to left word of 100,000 letters took \(took) s")
        TestSupport.expectXY(100, 120006, xy)  // As the Go port breaks it
    }
}
