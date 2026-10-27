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
        // X for bold italic, or C for code, then ~ when it is struck through,
        // with the link after a space, and a + before the text of one that is
        // joined to the text line before it.
        func describe(_ paragraph: Paragraph) -> [String] {
            var list = [String]()
            for i in 0..<paragraph.lines.count {
                let line = paragraph.lines[i]
                let font = line.getFont()
                let style = (font === bold) ? "B" : (font === italic) ? "I"
                        : (font === boldItalic) ? "X" : (font === code) ? "C" : "R"
                let strike = line.getStrikeout() ? "~" : ""
                let link = line.getURIAction().map { " " + $0 } ?? ""
                list.append((paragraph.joinsPrevious(i) ? "+" : "") + line.getText()! + "|" + style + strike + link)
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

    // A backslash is text in code, as in GitHub's Markdown, and a backtick
    // after it closes the code; outside code, \` is a backtick that opens none.
    @Test func aBackslashInCodeIsText() {
        #expect(parse("`foo\\`bar`") == ["foo\\|C", "+bar`|R"])
        #expect(parse("`\\`") == ["\\|C"])
        #expect(parse("\\`not code`") == ["`not code`|R"])
        #expect(parse("``a\\b``") == ["a\\b|C"])
        #expect(parse("x ``a\\`` y") == ["x |R", "a\\|C", " y|R"])
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
        // The underline in the color of the link, not black
        TestSupport.expectRGB(1, 0, 0, line.getDecorationColor())
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

    // GitHub's Markdown: _ emphasizes as * does, but only at the edge of a
    // word, so that snake_case and file names are text.
    @Test func underscoresEmphasizeAtTheEdgesOfWords() {
        #expect(parse("_italic_ and __bold__") == ["italic|I", " and |R", "bold|B"])
        #expect(parse("snake_case_name stays") == ["snake_case_name stays|R"])
        #expect(parse("file_name and _x_") == ["file_name and |R", "x|I"])
        #expect(parse("__a _b_ c__") == ["a |B", "b|X", " c|B"])
        #expect(parse("(_x_)") == ["(|R", "+x|I", "+)|R"])
        #expect(parse("_open only") == ["_open only|R"])
        // The * closes first, and the _ inside it can no longer match.
        #expect(parse("*a _b* c_") == ["a _b|I", " c_|R"])
    }

    // Two ~ strike through, as in GitHub's Markdown; one or three are text.
    @Test func twoTildesStrikeThrough() {
        #expect(parse("~~gone~~ here") == ["gone|R~", " here|R"])
        #expect(parse("a ~~b *c*~~ d") == ["a |R", "b |R~", "c|I~", " d|R"])
        #expect(parse("~5 and ~~~x~~~") == ["~5 and ~~~x~~~|R"])
        #expect(parse("~~ a ~~") == ["~~ a ~~|R"])
        // They nest, and do not cross.
        #expect(parse("~~a *b~~ c*") == ["a *b|R~", " c*|R"])
        #expect(parse("*a ~~b* c~~") == ["a ~~b|I", " c~~|R"])
        #expect(parse("**a ~~b~~ c**") == ["a |B", "b|B~", " c|B"])
    }

    // An address of http:// or https:// is a link: in < and >, or alone,
    // without the punctuation at its end and a ) that closes no ( of its own.
    @Test func anAddressIsALink() {
        #expect(parse("see https://pdfjet.com.")
                == ["see |R", "https://pdfjet.com|R https://pdfjet.com", "+.|R"])
        #expect(parse("(https://pdfjet.com)")
                == ["(|R", "+https://pdfjet.com|R https://pdfjet.com", "+)|R"])
        #expect(parse("https://en.wikipedia.org/wiki/Foo_(bar)")
                == ["https://en.wikipedia.org/wiki/Foo_(bar)|R https://en.wikipedia.org/wiki/Foo_(bar)"])
        #expect(parse("go to http://a.b/c?d=1&e=2, then")
                == ["go to |R", "http://a.b/c?d=1&e=2|R http://a.b/c?d=1&e=2", "+, then|R"])
        #expect(parse("<https://pdfjet.com/a_b>") == ["https://pdfjet.com/a_b|R https://pdfjet.com/a_b"])
        #expect(parse("*https://pdfjet.com*") == ["https://pdfjet.com|I https://pdfjet.com"])
        #expect(parse("[text](https://x.com) https://y.com")
                == ["text|R https://x.com", "https://y.com|R https://y.com"])
        #expect(parse("xhttps://a.b") == ["xhttps://a.b|R"])
        #expect(parse("HTTPS://A.B/x") == ["HTTPS://A.B/x|R HTTPS://A.B/x"])
        #expect(parse("<Http://a.b>") == ["Http://a.b|R Http://a.b"])
        // Only the ASCII letters are folded: ſ is not an s.
        #expect(parse("httpſ://a.b") == ["httpſ://a.b|R"])
        #expect(parse("http:// alone") == ["http:// alone|R"])
        #expect(parse("<http://a b>") == ["<http://a b>|R"])
    }

    @Test func longAddressesAreReadInLinearTime() {
        // The ) at the end of an address, and < that no > closes, would each
        // take quadratic time with a naive search.
        let text = "http://a" + String(repeating: ")", count: 40000)
                + String(repeating: "<http://a_~", count: 20000)
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
