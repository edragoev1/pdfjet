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
        #expect(content.contains("10 772 Td\n"), "\(content)")
        #expect(content.contains("[<" + TestSupport.hex("Hello (x)") + ">] TJ\n"), "\(content)")
        TestSupport.expectNear(44.664, line.getWidth(), 0.001)
        TestSupport.expectNear(10 + line.getWidth(), xy[0])
    }

    @Test func theUnderlineAndTheStrikeoutOfTaggedTextAreArtifacts() throws {
        // The line is decoration: an element of its own, described as
        // "Underlined text: " and the text, is read after the text again.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let line = TextLine(TestSupport.helvetica(memory.pdf), "Hello")
        line.setUnderline(true)
        line.setStrikeout(true)
        _ = line.setLocation(10, 20)
        line.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.components(separatedBy: "/Artifact BMC\n").count - 1 == 2, "\(content)")
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/S /P\n").count - 1 == 1, "the text line is more than one element")
        #expect(!raw.contains("/Alt "), "the lines describe themselves")
    }

    @Test func emptyTextDrawsNothingAndReturnsTheLocation() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TestSupport.expectXY(5, 6, TextLine(TestSupport.helvetica(pdf), "").setLocation(5, 6).drawOn(page))
        #expect(page.getContent().isEmpty)
    }

    @Test func getLocationReturnsTheLocation() {
        let line = TextLine(TestSupport.helvetica(TestSupport.newPDF()), "x").setLocation(5, 6)
        TestSupport.expectXY(5, 6, line.getLocation())
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

    // The links of the page, each the width of the word it holds.
    private func checkLinks(_ what: String, _ page: Page, _ font: Font, _ words: [String]) {
        #expect(page.annots.count == words.count, "\(what)")
        for (i, annot) in page.annots.enumerated() where i < words.count {
            TestSupport.expectNear(TextLine(font, words[i]).getWidth(), annot.x2 - annot.x1,
                    TestSupport.delta, "\(what) \(words[i])")
        }
    }

    @Test func theLinkOfAWordDrawnWithItsSpaceEndsAtTheWord() {
        // A word of a TextColumn, or of a justified TextFrame row, is drawn with
        // the space after it, and its link reached one space past the word. The
        // box now holds the text that shows.
        var words = ["Click", "here", "for", "the", "whole", "story", "of", "it"]
        var page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        var font = TestSupport.helvetica(page.pdf)
        let column = TextColumn()
        column.setWidth(120.0)
        column.setLocation(50.0, 50.0)
        column.addParagraph(Paragraph().add(
                TextLine(font, words.joined(separator: " ")).setURIAction("https://pdfjet.com")))
        column.drawOn(page)
        checkLinks("TextColumn", page, font, words)

        // Rows of two words, justified and so drawn a word at a time, and a
        // last row of one, drawn as it is.
        words = ["word", "word", "word", "word", "word"]
        page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        font = TestSupport.helvetica(page.pdf)
        let paragraph = Paragraph().setTextAlignment(Alignment.JUSTIFY)
        paragraph.add(TextLine(font, words.joined(separator: " ")).setURIAction("https://pdfjet.com"))
        let frame = TextFrame([paragraph]).setWidth(TextLine(font, "word word").getWidth() + 2.0)
        frame.setLocation(50.0, 50.0)
        frame.drawOn(page)
        checkLinks("justified TextFrame", page, font, words)

        // A text line of its own keeps the spaces it is given out of its box too.
        page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        font = TestSupport.helvetica(page.pdf)
        let line = TextLine(font, "  link  ").setURIAction("https://pdfjet.com")
        line.setLocation(100.0, 100.0)
        line.drawOn(page)
        let annot = page.annots[0]
        TestSupport.expectNear(100.0 + TextLine(font, "  ").getWidth(), annot.x1, TestSupport.delta, "left")
        TestSupport.expectNear(TextLine(font, "link").getWidth(), annot.x2 - annot.x1, TestSupport.delta, "width")
    }
}
