/**
 * PDF417Tests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct PDF417Tests {
    @Test func drawingTwiceDoesNotMoveTheSymbol() throws {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let symbol = try PDF417("Hello, World!")
        TestSupport.expectXY(281.25, 11.25, symbol.drawOn(page))
        TestSupport.expectXY(281.25, 11.25, symbol.drawOn(page))
    }
}
