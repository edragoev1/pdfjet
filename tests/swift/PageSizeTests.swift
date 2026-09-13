/**
 * PageSizeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct PageSizeTests {
    private func expectSize(_ width: Float, _ height: Float, _ portrait: PageSize, _ landscape: PageSize,
            sourceLocation: SourceLocation = #_sourceLocation) {
        #expect(portrait.getWidth() == width, sourceLocation: sourceLocation)
        #expect(portrait.getHeight() == height, sourceLocation: sourceLocation)
        #expect(landscape.getWidth() == height, sourceLocation: sourceLocation)
        #expect(landscape.getHeight() == width, sourceLocation: sourceLocation)
    }

    @Test func isoSizesInPoints() {
        expectSize(842, 1191, A3.PORTRAIT, A3.LANDSCAPE)
        expectSize(595, 842, A4.PORTRAIT, A4.LANDSCAPE)
        expectSize(420, 595, A5.PORTRAIT, A5.LANDSCAPE)
        expectSize(499, 709, B5.PORTRAIT, B5.LANDSCAPE)
    }

    @Test func japaneseAndNorthAmericanSizesInPoints() {
        expectSize(516, 729, JISB5.PORTRAIT, JISB5.LANDSCAPE)
        expectSize(612, 792, Letter.PORTRAIT, Letter.LANDSCAPE)
        expectSize(612, 1008, Legal.PORTRAIT, Legal.LANDSCAPE)
        expectSize(522, 756, Executive.PORTRAIT, Executive.LANDSCAPE)
        expectSize(792, 1224, Tabloid.PORTRAIT, Tabloid.LANDSCAPE)
    }

    @Test func aPageTakesItsSizeFromThePageSize() {
        let page = Page(TestSupport.newPDF(), A4.LANDSCAPE)
        #expect(page.getWidth() == 842)
        #expect(page.getHeight() == 595)
        let custom = PageSize(100, 200)
        #expect(custom.getWidth() == 100)
        #expect(custom.getHeight() == 200)
    }
}
