/**
 * TextLineTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct TextLineTests {
    @Test func drawOnWritesTheTextAsHexAtTheFlippedY() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let line = TextLine(TestSupport.helvetica(pdf), "Hello (x)").setLocation(10, 20)
        let xy = line.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("1 0 0 1 10 772 Tm\n"), "\(content)")
        #expect(content.contains("[<" + TestSupport.hex("Hello (x)") + ">] TJ\n"), "\(content)")
        TestSupport.expectNear(44.664, line.getWidth(), 0.001)
        TestSupport.expectNear(10 + line.getWidth(), xy[0])
    }

    @Test func emptyTextDrawsNothingAndReturnsTheLocation() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TestSupport.expectXY(5, 6, TextLine(TestSupport.helvetica(pdf), "").setLocation(5, 6).drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func colorSettersConvertAndCopy() {
        let line = TextLine(TestSupport.helvetica(TestSupport.newPDF()), "x")
        _ = line.setTextColor(Int32(0xFF8000))
        TestSupport.expectRGB(1, 128 / 255, 0, line.getTextColor())

        var rgb: [Float] = [0.1, 0.2, 0.3]
        _ = line.setTextColor(rgb)
        rgb[0] = 0.9
        var copy = line.getTextColor()
        copy[1] = 0.9
        TestSupport.expectRGB(0.1, 0.2, 0.3, line.getTextColor())
    }

    @Test func underlineAddsAStrokedLine() {
        let pdf = TestSupport.newPDF()
        let plain = Page(pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(pdf), "Hello").setLocation(10, 20).drawOn(plain)
        #expect(!TestSupport.content(plain).contains("\nS\n"))

        let underlined = Page(pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(pdf), "Hello").setLocation(10, 20).setUnderline(true).drawOn(underlined)
        #expect(TestSupport.content(underlined).contains("\nS\n"))
    }

    @Test func setFontChangesTheFallbackFontUnlessAnotherWasSet() throws {
        let pdf = TestSupport.newPDF()
        let helvetica = TestSupport.helvetica(pdf)
        let courier = try Font(pdf, CoreFont.COURIER)
        let line = TextLine(helvetica, "x").setFont(courier)
        #expect(line.getFallbackFont() === courier)
        line.setFallbackFont(helvetica).setFont(try Font(pdf, CoreFont.TIMES_ROMAN))
        #expect(line.getFallbackFont() === helvetica)
    }
}
