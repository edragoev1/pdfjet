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

    @Test func aWrappedLineIsDrawnWithTheSpaceItWasWrappedAt() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        TextBlock(font, tenWords).setLocation(0, 0).setWidth(60).drawOn(page)
        let content = TestSupport.content(page)
        // Six lines: after each of the first five, the space it was wrapped at,
        // so that a screen reader and a text extractor see the word break; the
        // last line has none.
        let space = "[<" + TestSupport.hex(" ") + ">] TJ\n"
        #expect(content.components(separatedBy: space).count - 1 == 5, "\(content)")
        #expect(content.contains("[<" + TestSupport.hex("ten") + ">] TJ\nET"), "\(content)")
        // Drawn after the line, not in its text: the line is measured without it
        #expect(content.contains("[<" + TestSupport.hex("one two") + ">] TJ\n"), "\(content)")
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
    @Test func isTaggedAsItsStructureType() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        memory.pdf.setTitle("Test")
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let heading = TextBlock(font, "Invoice").setStructureType(StructElem.H1)
        _ = heading.setLocation(50, 50)
        _ = heading.drawOn(page)
        let paragraph = TextBlock(font, "Thank you for your order.")
        _ = paragraph.setLocation(50, 100)
        _ = paragraph.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("/H1 <</MCID 0>>") && content.contains("/P <</MCID 1>>"), "\(content)")
    }

    // A right to left text cut to its height keeps the start of its last line,
    // at the right as it is drawn, and its "..." at the left, where the line
    // ends in the reading (review of 11 October 2026, #7).
    @Test func aRightToLeftTextCutToItsHeightKeepsTheStartOfItsLastLine() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
        let text = "שלום עולם אחד שתיים שלוש ארבע חמש שש שבע שמונה"
        let lines = TextBlock(font, text).setRightToLeft(true).setWidth(80).layout().textLines
        let fit = TextBlock(font, text).setRightToLeft(true).setSize(80, 20).layout().textLines
        try #require(fit.count < lines.count, "the text is not cut: \(fit.count) lines of \(lines.count)")
        let last = Array(lines[fit.count - 1].text!.unicodeScalars)
        let got = fit[fit.count - 1].text!
        var kept = ""
        kept.unicodeScalars.append(contentsOf: last[3...])
        #expect(got.hasPrefix("...") && got.hasSuffix(kept),
                "the last line \(got), cut from \(lines[fit.count - 1].text!), has lost its start")
    }

    // A character Unicode does not draw without a glyph, as the variation
    // selector after ™ in text pasted from a phone, a zero width space or a word
    // joiner, takes no width and draws nothing, where it was a box with a width
    // (review of 11 October 2026, #9).
    @Test func anIgnorableCharacterIsNotDrawn() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
        let plain = "Brand™ © 2026 word"
        let marked = "Brand™\u{FE0F} ©\u{FE0F} 2026\u{200B} wo\u{2060}rd"
        #expect(font.stringWidth(12, plain) == font.stringWidth(12, marked),
                "the text is \(font.stringWidth(12, marked)) wide with the marks, \(font.stringWidth(12, plain)) without")
        func draw(_ text: String) -> String {
            let page = Page(pdf, Letter.PORTRAIT)
            TextLine(font, text).setLocation(10, 10).drawOn(page)
            return TestSupport.content(page)
        }
        let a = draw(plain)
        let b = draw(marked)
        #expect(a == b, "the marks draw something:\n\(b)\n\(a)")
    }

    // A core font does not draw or measure them either, nor the marks of
    // direction, the joiners and the byte order mark: they were spaces, so that
    // "©️ 2026" had two (review of 12 October 2026, R14). getFitChars kerns with
    // the next character drawn, not with a soft hyphen, as stringWidth does (R19).
    @Test func aCoreFontDrawsNoIgnorableCharacter() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        font.setSize(12)
        let plain = "ab \u{00A9} 2026 word"
        let marked = "a\u{FE0F}b \u{00A9}\u{FE0F} 2026\u{200B} wo\u{2060}r\u{200D}d\u{200E}\u{200C}\u{200F}\u{FEFF}\u{E0041}"
        #expect(font.stringWidth(12, plain) == font.stringWidth(12, marked),
                "the text is \(font.stringWidth(12, marked)) wide with the marks, \(font.stringWidth(12, plain)) without")
        func draw(_ text: String) -> String {
            let page = Page(pdf, Letter.PORTRAIT)
            TextLine(font, text).setLocation(10, 10).drawOn(page)
            return TestSupport.content(page)
        }
        let a = draw(plain)
        let b = draw(marked)
        #expect(a == b, "the marks draw something:\n\(b)\n\(a)")
        // A hair over the width, as Float rounds it
        font.setKernPairs(true)
        let width = font.stringWidth(12, "AVAV") + 0.001
        #expect(font.getFitChars("A\u{00AD}VA\u{00AD}V", width) == 6)
    }

    // A Stamp leaves out the marks of direction and the joiners its font has no
    // glyph for, as Page does: Hebrew with brackets, reordered for drawing, wrote
    // two .notdef glyphs (review of 11 October 2026, #10).
    @Test func aStampLeavesOutTheMarksOfDirection() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
        let text = Bidi.reorderVisually("שלום (abc) עולם")
        try #require(text.unicodeScalars.contains { $0.value == 0x200E || $0.value == 0x200F },
                "the text has no marks of direction: \(text)")
        let stamp = Stamp(pdf)
        stamp.drawText(font, 12, 0, 12, text)
        let content = TestSupport.latin1(stamp.buf)
        // A mark drawn as a glyph the font lacks is in a span of its own, after
        // the first run of glyphs; this text has no character the font lacks
        #expect(!content.contains("/ActualText"), "the stamp draws a mark as a glyph the font lacks: \(content)")
        let start = try #require(content.firstIndex(of: "<"))
        let end = try #require(content.firstIndex(of: ">"))
        let glyphs = Array(content[content.index(after: start)..<end])
        var i = 0
        while i + 4 <= glyphs.count {
            #expect(String(glyphs[i..<(i + 4)]) != "0000", "the stamp draws .notdef: \(content)")
            i += 4
        }
    }

    // A word too wide for its line breaks at a soft hyphen, the line ending with
    // a hyphen; it was drawn as a hyphen wherever it was, and broken between its
    // characters (review of 11 October 2026, #21).
    @Test func breaksAWordAtASoftHyphen() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let block = TextBlock(font, "x " + TestSupport.softHyphenWord)
        block.setWidth(TestSupport.softHyphenWidth(font) + 2 * block.getPadding())
        let lines = block.getTextLines().map { $0.text!.replacingOccurrences(of: "\u{00AD}", with: "") }
        guard lines.count >= 2 && lines[0] == "x Donau-" else {
            Issue.record("the lines \(lines)")
            return
        }
        for line in lines.dropLast() where !line.hasSuffix("-") {
            Issue.record("the lines \(lines): \(line) is not broken at a soft hyphen")
        }
        #expect(lines.joined().replacingOccurrences(of: "-", with: "") == "x Donaudampfschifffahrt", "\(lines)")
        // A word that fits draws no hyphen.
        let fits = TextBlock(font, TestSupport.softHyphenWord).setWidth(500).getTextLines()
        #expect(fits.count == 1 && !fits[0].text!.contains("-"), "the lines of a word that fits: \(fits.count)")
    }

    // A word broken between its characters breaks at a soft hyphen where one
    // fits, as TextFrame breaks each row: "1234\u00ad5678" was "12", "34", "56",
    // "78" with no hyphen, where TextFrame draws "34-", and a line ran across
    // the soft hyphen of "aaaaaaaaaa\u00adbbbbbbbbbb" where "aa-" fit (review of
    // 12 October 2026, R18).
    @Test func aWordBrokenBetweenCharactersBreaksAtASoftHyphen() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        // Two digits and a hyphen fit, three digits do not.
        #expect(lineTexts(font, "1234\u{00AD}5678", font.stringWidth(font.getSize(), "00-") + 1, false)
                == "12|34-|56|78")
        #expect(lineTexts(font, "aaaaaaaaaa\u{00AD}bbbbbbbbbb", font.stringWidth(font.getSize(), "aaaa") + 1, false)
                == "aaaa|aaaa|aa-|bbbb|bbbb|bb")
    }

    // A right to left text block breaks a word at a soft hyphen, the hyphen at
    // the end of the line, on its left: it was cut between two characters, with
    // no hyphen (review of 12 October 2026, R17).
    @Test func aRightToLeftWordBreaksAtASoftHyphen() throws {
        let font = try Font(TestSupport.newPDF(), TestSupport.open("fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"))
        font.setSize(12)
        let width = font.stringWidth(12, "\u{05E9}\u{05DC}\u{05D5}\u{05DD}-") + 1
        let word = "\u{05E9}\u{05DC}\u{05D5}\u{05DD}\u{00AD}\u{05E2}\u{05D5}\u{05DC}\u{05DD}\u{00AD}\u{05D0}\u{05D7}\u{05D3}"
        let lines = "-\u{05DD}\u{05D5}\u{05DC}\u{05E9}|-\u{05DD}\u{05DC}\u{05D5}\u{05E2}|\u{05D3}\u{05D7}\u{05D0}"
        #expect(lineTexts(font, word, width, true) == lines)
        #expect(lineTexts(font, "\u{05D0} " + word, width, true) == "\u{05D0}|" + lines)
    }

    // Returns the lines of a text block of the text, at the width of text, joined by "|".
    private func lineTexts(_ font: Font, _ text: String, _ width: Float, _ rightToLeft: Bool) -> String {
        let block = TextBlock(font, text)
        block.setRightToLeft(rightToLeft)
        block.setWidth(width + 2 * block.getPadding())
        return block.getTextLines().map {
            String(String.UnicodeScalarView($0.text!.unicodeScalars.filter { $0.value != 0x00AD }))
        }.joined(separator: "|")
    }
}
