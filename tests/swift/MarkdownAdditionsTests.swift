/**
 * MarkdownAdditionsTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The additions to Markdown of 27 October 2026: hard line breaks, <br> in a
/// table cell, entities, and the bookmarks of the headings of a document that
/// is not tagged.
@Suite struct MarkdownAdditionsTests {
    private static func markup(_ pdf: PDF) -> Markup {
        let font = TestSupport.helvetica(pdf)
        return Markup(font, font, font, font, font)
    }

    // The breaks of the text lines of the paragraph, in order.
    private static func breaks(_ paragraph: Paragraph) -> [Bool] {
        return paragraph.lines.map { $0.lineBreaks > 0 }
    }

    @Test func hardLineBreaks() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        let breaks = MarkdownAdditionsTests.breaks
        #expect(breaks(markup.paragraph("one  \ntwo")) == [true, false])
        #expect(breaks(markup.paragraph("one\\\ntwo")) == [true, false])
        #expect(breaks(markup.paragraph("one<br>two")) == [true, false])
        #expect(breaks(markup.paragraph("one<BR />two")) == [true, false])
        #expect(breaks(markup.paragraph("one<br/>two")) == [true, false])
        // A line break after one space, and a <br> in code, are no breaks
        #expect(breaks(markup.paragraph("one \ntwo")) == [false])
        #expect(breaks(markup.paragraph("`a<br>b`")) == [false])
        #expect(markup.paragraph("<br").lines[0].text == "<br")
        // Emphasis goes on over a break
        let bold = markup.paragraph("**one<br>two**")
        #expect(bold.lines.count == 2)
        #expect(bold.lines[0].lineBreaks == 1)
        #expect(!bold.joinsPrevious(1))
    }

    // A hard line break at the end of a text is no break, as in CommonMark,
    // and the spaces before it are not in the last text line; paragraphs
    // strips the whitespace at the end of each paragraph.
    @Test func aHardLineBreakAtTheEndIsNoBreak() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["0  \n", "0  \r\n", "0   \n", "0  \n\n", "0  \n  "] {
            let paragraph = markup.paragraph(text)
            #expect(paragraph.lines.count == 1, "\(text)")
            #expect(paragraph.lines.first?.text == "0", "\(text)")
            #expect(paragraph.lines.first?.lineBreaks == 0, "\(text)")
            let paragraphs = markup.paragraphs(text)
            #expect(paragraphs.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.first?.text == "0", "\(text)")
        }
        // In bold, and a break before the last line
        let bold = markup.paragraph("**0**  \n")
        #expect(bold.lines.count == 1)
        #expect(bold.lines.first?.text == "0")
        #expect(bold.lines.first?.lineBreaks == 0)
        let two = markup.paragraph("one  \ntwo  \n")
        #expect(MarkdownAdditionsTests.breaks(two) == [true, false])
        #expect(two.lines.count == 2 && two.lines[1].text == "two")
        // Spaces at the end without a line break are text in paragraph
        #expect(markup.paragraph("0  ").lines.first?.text == "0  ")
        #expect(markup.paragraphs("0  ").first?.lines.first?.text == "0")
    }

    // The spaces before a hard line break are not in the text line before
    // it, as in CommonMark.
    @Test func theSpacesBeforeAHardLineBreakAreNotText() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["one  \ntwo", "one  \r\ntwo", "one   \ntwo", "one\t  \ntwo",
                     "**one**  \ntwo", "[one  \ntwo](u)"] {
            let paragraph = markup.paragraph(text)
            #expect(MarkdownAdditionsTests.breaks(paragraph) == [true, false], "\(text)")
            #expect(paragraph.lines.count == 2 && paragraph.lines[0].text == "one", "\(text)")
            #expect(paragraph.lines.count == 2 && paragraph.lines[1].text == "two", "\(text)")
        }
    }

    // A backslash at the end of a text is a backslash, and no hard line
    // break, as in CommonMark; in the middle it is a break, as before.
    @Test func aBackslashAtTheEndIsText() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["0\\\n", "0\\\r\n", "0\\\n  ", "0\\\n\n"] {
            let paragraph = markup.paragraph(text)
            #expect(paragraph.lines.count == 1, "\(text)")
            #expect(paragraph.lines.first?.text == "0\\", "\(text)")
            #expect(paragraph.lines.first?.lineBreaks == 0, "\(text)")
            let paragraphs = markup.paragraphs(text)
            #expect(paragraphs.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.first?.text == "0\\", "\(text)")
        }
        let two = markup.paragraph("one\\\ntwo\\\n")
        #expect(MarkdownAdditionsTests.breaks(two) == [true, false])
        #expect(two.lines.count == 2 && two.lines[1].text == "two\\")
    }

    // The spaces and tabs at the start of the line after a hard line break are
    // not in the text, as in CommonMark; a space written &#32; is, and so are
    // the spaces after a <br>, which is no line ending.
    @Test func theSpacesAfterAHardLineBreakAreNotText() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["one  \n  two", "one  \r\n  two", "one   \n\t two", "one\\\n  two",
                     "one\\\r\n  two", "**one**  \n  two", "[one  \n  two](u)"] {
            let paragraph = markup.paragraph(text)
            #expect(MarkdownAdditionsTests.breaks(paragraph) == [true, false], "\(text)")
            #expect(paragraph.lines.count == 2 && paragraph.lines[0].text == "one", "\(text)")
            #expect(paragraph.lines.count == 2 && paragraph.lines[1].text == "two", "\(text)")
            let paragraphs = markup.paragraphs(text)
            #expect(paragraphs.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.count == 2, "\(text)")
            #expect(paragraphs.first?.lines.last?.text == "two", "\(text)")
        }
        let entity = markup.paragraph("one  \n&#32;two")
        #expect(entity.lines.count == 2 && entity.lines[1].text == " two")
        let br = markup.paragraph("one<br>  two")
        #expect(br.lines.count == 2 && br.lines[1].text == "  two")
    }

    // The spaces and tabs at the start of the line after a soft line break
    // are not in the text, as in CommonMark: the line break is one space; a
    // space written &#32; is text.
    @Test func theSpacesAfterASoftLineBreakAreNotText() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["one\n  two", "one\r\n  two", "one\n\t two", "one\ntwo", "one\r\ntwo"] {
            let paragraph = markup.paragraph(text)
            #expect(paragraph.lines.count == 1, "\(text)")
            #expect(paragraph.lines.first?.text == "one two", "\(text)")
            #expect(paragraph.lines.first?.lineBreaks == 0, "\(text)")
            let paragraphs = markup.paragraphs(text)
            #expect(paragraphs.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.count == 1, "\(text)")
            #expect(paragraphs.first?.lines.first?.text == "one two", "\(text)")
        }
        #expect(markup.paragraph("one\n&#32;two").lines.first?.text == "one  two")
    }

    // The spaces and tabs before a soft line break are not in the text, as in
    // CommonMark, and those before any line break are counted as they were
    // appended: a space written &#32; stays.
    @Test func theSpacesBeforeASoftLineBreakAreNotText() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["one \ntwo", "one\t\ntwo", "one \t\n  two", "one \r\ntwo", "one \rtwo"] {
            #expect(markup.paragraph(text).lines.first?.text == "one two", "\(text)")
            #expect(markup.paragraphs(text).first?.lines.first?.text == "one two", "\(text)")
        }
        #expect(markup.paragraph("one&#32;\ntwo").lines.first?.text == "one  two")
        #expect(markup.paragraph("one&#32;\n  \ntwo").lines.first?.text == "one ")
    }

    // A line break right after a hard line break adds no space to the next row.
    @Test func noSpaceStartsARowAfterAHardLineBreak() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        for text in ["one<br>\ntwo", "one<br>\n  two", "one<br> \ntwo", "one<br>\r\ntwo",
                "one<br>\rtwo", "one  \n\ntwo", "one\\\n\ntwo"] {
            let paragraph = markup.paragraph(text)
            #expect(MarkdownAdditionsTests.breaks(paragraph) == [true, false], "\(text)")
            #expect(paragraph.lines.first?.text == "one", "\(text)")
            #expect(paragraph.lines.last?.text == "two", "\(text)")
        }
        #expect(markup.paragraph("\ntwo").lines.first?.text == "two")
    }

    // \r\n and a lone \r are line endings as \n is, in text, in hard line
    // breaks, in code and between paragraphs.
    @Test func everyLineEndingIsOne() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        #expect(markup.paragraph("one\rtwo").lines.first?.text == "one two")
        #expect(markup.paragraph("`a\r\nb`").lines.first?.text == "a b")
        #expect(markup.paragraph("`a\rb`").lines.first?.text == "a b")
        #expect(MarkdownAdditionsTests.breaks(markup.paragraph("one  \rtwo")) == [true, false])
        #expect(MarkdownAdditionsTests.breaks(markup.paragraph("one\\\rtwo")) == [true, false])
        #expect(markup.paragraph("0\\\r").lines.first?.text == "0\\")
        #expect(markup.paragraphs("one\r\rtwo").count == 2)
    }

    @Test func aTextFrameStartsARowAtABreak() {
        let page = TestSupport.newPage()
        let markup = MarkdownAdditionsTests.markup(page.pdf)
        TextFrame([markup.paragraph("p1<br>p2 p3")])
                .setLocation(50.0, 50.0).setWidth(400.0).drawOn(page)
        let content = TestSupport.content(page)
        let first = TestSupport.positionOf(content, "p1")
        let second = TestSupport.positionOf(content, "p2")
        TestSupport.expectNear(first[0], second[0], 0.01)
        #expect(second[1] < first[1], "\(content)")     // Below it, y up
    }

    @Test func entitiesAreTheirCharacters() {
        let markup = MarkdownAdditionsTests.markup(TestSupport.newPDF())
        #expect(markup.paragraph("Fish &amp; Chips &mdash; &#8212; &#x2014; &copy; &nbsp; &lt; &gt;")
                .lines[0].text!.trim() == "Fish & Chips \u{2014} \u{2014} \u{2014} \u{A9} \u{A0} < >")
        // Not an entity: an unknown name, no ;, no character, an escaped &
        #expect(markup.paragraph("&bogus; &amp &#0; &#xD800; &#12345678; \\&amp;").lines[0].text!.trim()
                == "&bogus; &amp &#0; &#xD800; &#12345678; &amp;")
        // &lt;br&gt; is the text <br>, not a break
        let paragraph = markup.paragraph("a &lt;br&gt; b")
        #expect(MarkdownAdditionsTests.breaks(paragraph) == [false])
        #expect(paragraph.lines[0].text!.trim() == "a <br> b")
        // In code an entity is its text
        #expect(markup.paragraph("`&amp;`").lines[0].text == "&amp;")
        // The ; is looked for in the ten code units after the &, as in Java:
        // a character outside the BMP is two of them
        #expect(markup.paragraph("&#x1F600;").lines[0].text == "\u{1F600}")
        #expect(markup.paragraph("&#1114111;").lines[0].text == "\u{10FFFF}")
        #expect(markup.paragraph("&#x110000;").lines[0].text == "&#x110000;")
        #expect(markup.paragraph("&\u{1F600}\u{1F600}\u{1F600}\u{1F600}\u{1F600};").lines[0].text
                == "&\u{1F600}\u{1F600}\u{1F600}\u{1F600}\u{1F600};")
        // Only ASCII digits are digits
        #expect(markup.paragraph("&#\u{0661}\u{0662};").lines[0].text == "&#\u{0661}\u{0662};")
    }

    @Test func aBrInATableCellBreaksItsLine() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font)
                .drawOn(pdf, "| r0 |\n|---|\n| r1<br>r2 |", &pages, Letter.PORTRAIT)
        let content = TestSupport.content(pages[0])
        let first = TestSupport.positionOf(content, "r1")
        let second = TestSupport.positionOf(content, "r2")
        TestSupport.expectNear(first[0], second[0], 0.01)
        #expect(second[1] < first[1], "\(content)")     // Below it, y up
        #expect(!content.contains(TestSupport.hex("<br>")), "\(content)")
    }

    @Test func theParagraphOfAMarkdownTextKeepsItsBreaks() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font)
                .drawOn(pdf, "p1  \np2\\\np3\np4", &pages, Letter.PORTRAIT)
        let content = TestSupport.content(pages[0])
        let y1 = TestSupport.positionOf(content, "p1")[1]
        let y2 = TestSupport.positionOf(content, "p2")[1]
        let y3 = TestSupport.positionOf(content, "p3")[1]
        #expect(y2 < y1 && y3 < y2, "\(content)")     // Each below the one before it, y up
        #expect(content.contains(TestSupport.hex("p4")), "\(content)")
        #expect(!content.contains(TestSupport.hex("\\")), "\(content)")
    }

    // The PDF of the text drawn as Markdown, in a document that is not tagged.
    private static func pdfOf(_ text: String) throws -> String {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font).drawOn(pdf, text, &pages, Letter.PORTRAIT)
        pdf.addPages(pages)
        try pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    @Test func theHeadingsOfAnUntaggedDocumentAreItsBookmarks() throws {
        let pdf = try MarkdownAdditionsTests.pdfOf("# One\n\ntext\n\n## Two\n\nmore")
        #expect(pdf.contains("/Outlines"), "no outline")
        #expect(pdf.contains(TestSupport.textString("One")) && pdf.contains(TestSupport.textString("Two")))
        #expect(try !MarkdownAdditionsTests.pdfOf("text alone").contains("/Outlines"))
    }

    @Test func aDocumentWithBookmarksOfItsOwnKeepsThem() throws {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        try Markdown(font, font, font, font, font).drawOn(pdf, "# One", &pages, Letter.PORTRAIT)
        let toc = Bookmark(pdf)
        let title = Title(font, "Mine", 50.0, 100.0)
        toc.addBookmark(pages[0], title)
        title.drawOn(pages[0])
        pdf.addPages(pages)
        try pdf.complete()
        let text = TestSupport.latin1(memory.bytes)
        #expect(text.contains(TestSupport.textString("Mine")), "their bookmark")
        #expect(!text.contains(TestSupport.textString("One")), "the heading's bookmark")
    }
}
