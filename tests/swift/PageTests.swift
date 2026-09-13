/**
 * PageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct PageTests {
    @Test func aNewPageTracksTheDefaultGraphicsState() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        #expect(page.getPenWidth() == 1)
        TestSupport.expectRGB(0, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
    }

    @Test func cmykSettersWriteCmykAndTrackTheRgbOfTheStandard() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setPenColorCMYK(0, 1, 1, 0)
        page.setBrushColorCMYK(0, 0, 0, 1)
        #expect(TestSupport.content(page) == "0 1 1 0 K\n0 0 0 1 k\n")
        TestSupport.expectRGB(1, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
    }

    @Test func restoreGraphicsStateRestoresTheTrackedState() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.setPenColor(Int32(0xFF0000))
        page.saveGraphicsState()
        page.setPenWidth(3)
        page.setPenColor(Int32(0x00FF00))
        page.restoreGraphicsState()
        #expect(page.getPenWidth() == 1)
        TestSupport.expectRGB(1, 0, 0, page.getPenColor())
        #expect(TestSupport.content(page).hasSuffix("q\n3 w\n0 1 0 RG\nQ\n"), "\(TestSupport.content(page))")
    }

    @Test func gettersReturnCopies() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        var pen = page.getPenColor()
        pen[0] = 1
        var brush = page.getBrushColor()
        brush[0] = 1
        TestSupport.expectRGB(0, 0, 0, page.getPenColor())
        TestSupport.expectRGB(0, 0, 0, page.getBrushColor())
        page.drawLine(0, 0, 10, 10)
        var content = page.getContent()
        content[0] = UInt8(ascii: "X")
        #expect(page.getContent()[0] != UInt8(ascii: "X"))
    }

    @Test func drawLineWritesAStrokedPathWithTheYFlipped() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.drawLine(10, 20, 30, 40)
        let content = TestSupport.content(page)
        #expect(content.contains("10 772 m\n30 752 l\nS\n"), "\(content)")
    }
}
