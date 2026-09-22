/**
 * MarkupTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct MarkupTests {
    // The fonts of one markup, so that a test can tell which font a text line has.
    private final class Fonts {
        let regular: Font
        let bold: Font
        let italic: Font
        let boldItalic: Font
        let code: Font
        let markup: Markup

        init(_ pdf: PDF = TestSupport.newPDF()) {
            // The numbers of the core fonts are valid.
            regular = try! Font(pdf, CoreFont.HELVETICA)
            bold = try! Font(pdf, CoreFont.HELVETICA_BOLD)
            italic = try! Font(pdf, CoreFont.HELVETICA_OBLIQUE)
            boldItalic = try! Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE)
            code = try! Font(pdf, CoreFont.COURIER)
            markup = Markup(regular, bold, italic, boldItalic, code)
        }

        // The text lines of the paragraph, each as its text and its font: R, B, I,
        // X for bold italic, or C for code, with the link after a space, and a +
        // before the text of one that is joined to the text line before it.
        func describe(_ paragraph: Paragraph) -> [String] {
            var list = [String]()
            for i in 0..<paragraph.lines.count {
                let line = paragraph.lines[i]
                let font = line.getFont()
                let style = (font === bold) ? "B" : (font === italic) ? "I"
                        : (font === boldItalic) ? "X" : (font === code) ? "C" : "R"
                let link = line.getURIAction().map { " " + $0 } ?? ""
                list.append((paragraph.joinsPrevious(i) ? "+" : "") + line.getText()! + "|" + style + link)
            }
            return list
        }
    }

    private func parse(_ text: String) -> [String] {
        let fonts = Fonts()
        return fonts.describe(fonts.markup.paragraph(text))
    }

    @Test func plainTextIsOneTextLine() {
        #expect(parse("Just text, no marks.") == ["Just text, no marks.|R"])
        #expect(parse("one\ntwo") == ["one two|R"])
    }

    @Test func boldItalicAndBoth() {
        #expect(parse("a **b** c") == ["a |R", "b|B", " c|R"])
        #expect(parse("a *b* c") == ["a |R", "b|I", " c|R"])
        #expect(parse("a ***b*** c") == ["a |R", "b|X", " c|R"])
        #expect(parse("**a *b* c**") == ["a |B", "b|X", " c|B"])
    }

    @Test func punctuationAfterAStyleIsJoinedToTheWord() {
        #expect(parse("Hello, **world**!") == ["Hello, |R", "world|B", "+!|R"])
        #expect(parse("un*believ*able") == ["un|R", "+believ|I", "+able|R"])
    }

    @Test func codeKeepsItsTextAsItIs() {
        #expect(parse("Call `a*b*c`.") == ["Call |R", "a*b*c|C", "+.|R"])
        #expect(parse("`` a `b` c ``") == ["a `b` c|C"])
        #expect(parse("**x`y`z**") == ["x|B", "+y|C", "+z|B"])
    }

    @Test func links() {
        #expect(parse("See [PDFjet](https://pdfjet.com).")
                == ["See |R", "PDFjet|R https://pdfjet.com", "+.|R"])
        #expect(parse("[a **b**](https://x)") == ["a |R https://x", "b|B https://x"])
        #expect(parse("**[go](u)**") == ["go|B u"])
    }

    @Test func aLinkNeedsItsBracketsAParenthesisAndAURLWithoutSpaces() {
        #expect(parse("[a] (b)") == ["[a] (b)|R"])
        #expect(parse("[a](b c)") == ["[a](b c)|R"])
        #expect(parse("[a]()") == ["[a]()|R"])
        #expect(parse("[a](b") == ["[a](b|R"])
        // A link is not in a link.
        #expect(parse("[[b](u) c](v)") == ["[b](u) c|R v"])
    }

    @Test func marksWithNoMatchAreText() {
        #expect(parse("2 * 3 = 6") == ["2 * 3 = 6|R"])
        #expect(parse("**a") == ["**a|R"])
        #expect(parse("a*") == ["a*|R"])
        #expect(parse("**a*") == ["*|R", "+a|I"])
        #expect(parse("`not code") == ["`not code|R"])
        #expect(parse("[not a link") == ["[not a link|R"])
    }

    @Test func aBackslashMakesAMarkText() {
        #expect(parse("\\*not italic\\*") == ["*not italic*|R"])
        #expect(parse("\\[a](b)") == ["[a](b)|R"])
        #expect(parse("a\\b \\") == ["a\\b \\|R"])
    }

    @Test func paragraphsAreSeparatedByEmptyLines() {
        let fonts = Fonts()
        let paragraphs = fonts.markup.paragraphs("One **two**\nthree.\n\n  \nFour.\r\n\r\n")
        #expect(paragraphs.count == 2)
        #expect(fonts.describe(paragraphs[0]) == ["One |R", "two|B", " three. |R"])
        #expect(fonts.describe(paragraphs[1]) == ["Four. |R"])
        #expect(Fonts().markup.paragraphs(" \n\n").count == 0)
    }

    @Test func aLinkIsColoredAndUnderlined() {
        let paragraph = Fonts().markup.setLinkColor(Color.red).paragraph("[a](u)")
        let line = paragraph.lines[0]
        #expect(line.getUnderline())
        TestSupport.expectRGB(1, 0, 0, line.getTextColor())
    }

    @Test func longInputsAreReadInLinearTime() {
        // Unmatched brackets, backticks of every length and runs of * would
        // each take quadratic time with a naive search.
        var text = ""
        for i in 0..<20000 {
            text += "[a](b *c `" + (i % 50 == 0 ? "``" : "") + " [["
        }
        let clock = ContinuousClock()
        var paragraph: Paragraph?
        let elapsed = clock.measure {
            paragraph = Fonts().markup.paragraph(text)
        }
        let milliseconds = elapsed.components.seconds * 1000
                + elapsed.components.attoseconds / 1_000_000_000_000_000
        #expect(!paragraph!.lines.isEmpty)
        #expect(milliseconds < 2000, "\(milliseconds) ms")
    }

    @Test func theParagraphDrawsWithNoSpaceBeforeJoinedPunctuation() {
        let pdf = TestSupport.newPDF()
        let fonts = Fonts(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        TextFrame([fonts.markup.paragraph("one **two**, three")])
                .setLocation(10, 10).setWidth(300).drawOn(page)
        let content = TestSupport.content(page)
        let two = TestSupport.positionOf(content, "two")
        let comma = TestSupport.positionOf(content, ",")
        TestSupport.expectNear(two[0] + fonts.bold.stringWidth("two"), comma[0])
    }
}
