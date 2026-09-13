/**
 * TextBlockTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct TextBlockTests {
    private let tenWords = "one two three four five six seven eight nine ten"

    @Test func aNewlineIsOneEmptyLine() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let x = TextBlock(font, "x").setLocation(0, 0).drawOn(nil)
        TestSupport.expectXY(500, 13.872, x)
        TestSupport.expectXY(x[0], x[1], TextBlock(font, "\n").setLocation(0, 0).drawOn(nil))
        TestSupport.expectXY(x[0], x[1], TextBlock(font, "").setLocation(0, 0).drawOn(nil))
    }

    @Test func wrappedTextMakesTheBlockTallerThanItsSetHeight() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let block = TextBlock(font, tenWords).setLocation(0, 0).setSize(60, 10)
        // Six lines of 13.872 points.
        TestSupport.expectXY(60, 83.232, block.drawOn(nil))
        TestSupport.expectNear(83.232, block.getHeight())
        #expect(block.getWidth() == 60)
    }

    @Test func drawOnAPageWritesEveryWord() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let xy = TextBlock(TestSupport.helvetica(pdf), tenWords).setLocation(0, 0).setSize(60, 10).drawOn(page)
        TestSupport.expectXY(60, 83.232, xy)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("one")), "\(content)")
        #expect(content.contains(TestSupport.hex("ten")), "\(content)")
    }
}
