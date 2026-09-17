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

    @Test func withoutAHeightTheBlockIsAsTallAsItsText() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let block = TextBlock(font, tenWords).setLocation(0, 0).setWidth(60)
        // Six lines of 13.872 points.
        TestSupport.expectXY(60, 83.232, block.drawOn(nil))
        TestSupport.expectNear(83.232, block.getHeight())
        #expect(block.getWidth() == 60)
    }

    @Test func aHeightCutsTheTextThatDoesNotFitAndAlignsTheRest() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let block = TextBlock(font, tenWords).setLocation(0, 0).setSize(60, 30)
        // Two of the six lines fit
        TestSupport.expectXY(60, 30, block.drawOn(nil))
        #expect(block.getHeight() == 30)
        var page = Page(pdf, Letter.PORTRAIT)
        block.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("...")), "\(content)")
        #expect(!content.contains(TestSupport.hex("ten")), "\(content)")

        // Aligned to the bottom of a block 10 points taller than its two lines,
        // the text sits where a block without a height draws it 10 points lower
        page = Page(pdf, Letter.PORTRAIT)
        block.setSize(60, 2 * 13.872 + 10).setVerticalAlignment(Alignment.BOTTOM).drawOn(page)
        let bottom = TestSupport.content(page)
        page = Page(pdf, Letter.PORTRAIT)
        TextBlock(font, "one two three four").setLocation(0, 10).setWidth(60).drawOn(page)
        let lower = TestSupport.content(page)
        let start = lower.range(of: "1 0 0 1 0 ")!.lowerBound
        let textMatrix = String(lower[start..<lower.range(of: " Tm\n")!.upperBound])
        #expect(bottom.contains(textMatrix), "\(textMatrix) missing from \(bottom)")
    }

    @Test func paddingWiderThanTheBlockDrawsOneCharacterALine() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        // The text area was negative, and the word was broken past its end.
        let block = TextBlock(font, "Hello")
        block.setLocation(50.0, 50.0).setWidth(100.0).setPadding(60.0).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("H")))
        #expect(content.contains(TestSupport.hex("o")))
    }

    @Test func theUnderlineIsDrawnInTheTextColorAtTheFontThickness() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        page.setPenColor(Color.red)     // the underline took the pen color
        let block = TextBlock(font, "Hello")
        block.setLocation(50.0, 50.0).setTextColor(Color.blue)
        block.setUnderline(true)
        block.setBorderWidth(3.0)       // and the border width
        block.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("0 0 1 RG"))
        #expect(!content.contains("3 w"))
        #expect(content.contains("\(font.getUnderlineThickness(font.getSize())) w"))
    }

    @Test func theCornerRadiusRoundsABackgroundWithoutABorder() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let block = TextBlock(TestSupport.helvetica(pdf), "Hello")
        block.setLocation(50.0, 50.0).setBackgroundColor(Color.yellow)
        block.setCornerRadius(10.0)
        block.drawOn(page)
        // A rounded rectangle draws its corners with the curve operator.
        #expect(TestSupport.content(page).contains(" c\n"))
    }

    @Test func strikeoutDrawsALineThroughEachLine() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TextBlock(TestSupport.helvetica(pdf), "one\ntwo").setLocation(0, 0).setStrikeout(true).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.components(separatedBy: " l\n").count - 1 == 2, "\(content)")
    }

    @Test func drawOnAPageWritesEveryWord() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let xy = TextBlock(TestSupport.helvetica(pdf), tenWords).setLocation(0, 0).setWidth(60).drawOn(page)
        TestSupport.expectXY(60, 83.232, xy)
        let content = TestSupport.content(page)
        #expect(content.contains(TestSupport.hex("one")), "\(content)")
        #expect(content.contains(TestSupport.hex("ten")), "\(content)")
    }

    @Test func transparentLeavesTheTextColorUnchanged() {
        let block = TextBlock(TestSupport.helvetica(TestSupport.newPDF()), "x")
        block.setTextColor(Color.blue).setTextColor(Color.transparent)
        TestSupport.expectRGB(0, 0, 1, block.getTextColor())
    }

    @Test func setFontChangesTheFallbackFontUnlessAnotherWasSet() throws {
        let pdf = TestSupport.newPDF()
        let helvetica = TestSupport.helvetica(pdf)
        let courier = try Font(pdf, CoreFont.COURIER)
        let block = TextBlock(helvetica, "x").setFont(courier)
        #expect(block.fallbackFont === courier)
        block.setFallbackFont(helvetica).setFont(try Font(pdf, CoreFont.TIMES_ROMAN))
        #expect(block.fallbackFont === helvetica)
    }
}
