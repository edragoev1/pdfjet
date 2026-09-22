/**
 * TextFrameTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct TextFrameTests {
    // Draws two paragraphs of one line and returns how far below the first the second starts.
    private func paragraphDistance(_ gap: Float?) -> Float {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let first = Paragraph(TextLine(font, "one"))
        let second = Paragraph(TextLine(font, "two"))
        let frame = TextFrame([first, second]).setLocation(10, 10).setWidth(300)
        if let gap {
            frame.setParagraphGap(gap)
        }
        frame.drawOn(Page(pdf, Letter.PORTRAIT))
        return second.getY1() - first.getY1()
    }

    @Test func theGapIsAddedToTheLineSoParagraphsNeverOverlap() {
        let line = TestSupport.helvetica(TestSupport.newPDF()).getBodyHeight()
        TestSupport.expectNear(2 * line, paragraphDistance(nil))    // one empty line
        TestSupport.expectNear(line, paragraphDistance(0))
        TestSupport.expectNear(line + 10, paragraphDistance(10))
    }

    @Test func aNegativeGapIsTakenAsZero() {
        TestSupport.expectNear(paragraphDistance(0), paragraphDistance(-5))
    }

    @Test func theDefaultGapIsAnEmptyLineOfTheNextParagraph() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let heading = Paragraph(TextLine(font, "Heading").setFontSize(24))
        let body = Paragraph(TextLine(font, "body"))
        TextFrame([heading, body]).setLocation(10, 10).setWidth(300).drawOn(Page(pdf, Letter.PORTRAIT))
        // The heading, then one empty line in the size of the body text
        TestSupport.expectNear(font.getBodyHeight(24) + font.getBodyHeight(font.getSize()),
                body.getY1() - heading.getY1())
    }

    // Draws one paragraph with the alignment in a frame 200 wide at x 10, and returns it.
    private func drawAligned(_ alignment: Alignment, _ text: String) -> Paragraph {
        let pdf = TestSupport.newPDF()
        let paragraph = Paragraph(TextLine(TestSupport.helvetica(pdf), text))
        paragraph.setTextAlignment(alignment)
        TextFrame([paragraph]).setLocation(10, 10).setWidth(200).drawOn(Page(pdf, Letter.PORTRAIT))
        return paragraph
    }

    // Draws one paragraph in a frame of the width at x 0, and returns how far down its text reaches.
    private func textHeight(_ text: String, _ width: Float) -> Float {
        let pdf = TestSupport.newPDF()
        let paragraph = Paragraph(TextLine(TestSupport.helvetica(pdf), text))
        TextFrame([paragraph]).setLocation(0, 10).setWidth(width).drawOn(Page(pdf, Letter.PORTRAIT))
        return paragraph.getY2() - paragraph.getY1()
    }

    @Test func aRowTakesTheWordsThatFitWithoutTheSpaceAfterThem() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let oneRow = textHeight("one two", 300)
        let width = font.stringWidth("one ") + font.stringWidth("two")
        TestSupport.expectNear(oneRow, textHeight("one two", width))
        #expect(textHeight("one two", width - 0.1) > oneRow, "two rows")
        // A word as wide as the frame is not broken.
        TestSupport.expectNear(oneRow, textHeight("Hello", font.stringWidth("Hello")))
    }

    @Test func aRightAlignedParagraphEndsAtTheRightEdge() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let paragraph = drawAligned(Alignment.RIGHT, "Hello")
        // The text ends at the right edge; the space after it is past the edge.
        TestSupport.expectNear(210 - font.stringWidth("Hello"), paragraph.getTextX())
        TestSupport.expectNear(210 + font.stringWidth(" "), paragraph.getX2())
    }

    @Test func aCenteredParagraphHasTheSameSpaceOnBothSides() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let paragraph = drawAligned(Alignment.CENTER, "Hello")
        TestSupport.expectNear(10 + (200 - font.stringWidth("Hello")) / 2, paragraph.getTextX())
    }

    @Test func aJustifiedParagraphLeavesItsLastRowAsItIs() {
        let text = "one two three four five six seven eight nine ten eleven twelve thirteen"
        let left = drawAligned(Alignment.LEFT, text)
        let justified = drawAligned(Alignment.JUSTIFY, text)
        #expect(left.getY2() - left.getY1() > 20, "more than one row")
        TestSupport.expectNear(left.getY2(), justified.getY2())
        TestSupport.expectNear(left.getX2(), justified.getX2())
    }
    @Test func paragraphsWithALabelAreAList() throws {
        // The label of an item is drawn where the item begins, so that it
        // reads before the text of the item and not after all of the text.
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        var paragraphs = [Paragraph]()
        for (i, text) in ["alpha beta", "gamma delta"].enumerated() {
            paragraphs.append(Paragraph().add(TextLine(font, text))
                    .setListLabel(TextLine(font, "\(i + 1)."), 15.0))
        }
        // A paragraph with no label ends the list.
        paragraphs.append(Paragraph().add(TextLine(font, "epsilon")))
        let frame = TextFrame(paragraphs)
        frame.setLocation(70.0, 50.0)
        frame.setWidth(300.0)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        _ = frame.drawOn(page)
        let content = TestSupport.content(page)
        // The label of an item is drawn before the text of the item.
        #expect(content.range(of: TestSupport.hex("1."))!.lowerBound
                < content.range(of: TestSupport.hex("alpha"))!.lowerBound)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/S /L\n").count - 1 == 1)
        #expect(raw.components(separatedBy: "/S /LI\n").count - 1 == 2)
        #expect(raw.components(separatedBy: "/S /Lbl\n").count - 1 == 2)
        #expect(raw.components(separatedBy: "/S /LBody\n").count - 1 == 2)
    }

    // 200 paragraphs of a few lines each, the first word of each its number,
    // p000 to p199, which no other word begins with.
    private func novel(_ font: Font) -> TextFrame {
        var paragraphs = [String]()
        for i in 0..<200 {
            var text = String(format: "p%03d", i)
            for j in 0..<(30 + i % 17) {
                text += " word\(j)"
            }
            paragraphs.append(text)
        }
        return TextFrame(font, paragraphs).setLocation(72, 72).setWidth(468)
    }

    // The page each paragraph starts on, by its number.
    private func pagesOf(_ pages: [Page]) -> [Int] {
        var pageOf = [Int](repeating: -1, count: 200)
        for (p, page) in pages.enumerated() {
            let content = TestSupport.content(page)
            for i in 0..<200 where content.contains(TestSupport.hex(String(format: "p%03d", i))) {
                #expect(pageOf[i] == -1, "paragraph \(i) starts on two pages")
                pageOf[i] = p
            }
        }
        return pageOf
    }

    @Test func aFrameFlowsOntoAsManyPagesAsTheTextNeeds() throws {
        let pdf = TestSupport.newPDF()
        let frame = novel(TestSupport.helvetica(pdf))
        var pages = [Page]()
        frame.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count > 5, "\(pages.count) pages")
        #expect(!frame.hasMoreText())
        // Every paragraph is drawn once, in order, and every page has text.
        let pageOf = pagesOf(pages)
        for i in 0..<200 {
            #expect(pageOf[i] >= 0, "paragraph \(i) is not drawn")
            #expect(i == 0 || pageOf[i] >= pageOf[i - 1], "paragraph \(i) is out of order")
        }
        #expect(pageOf[199] == pages.count - 1)
        // The text keeps the margin of its location at the bottom too: no
        // baseline under 72 points from the bottom of the page.
        let td = try NSRegularExpression(pattern: "[-0-9.]+ ([-0-9.]+) Td\\n")
        var baselines = 0
        for page in pages {
            let content = TestSupport.content(page)
            for m in td.matches(in: content, range: NSRange(content.startIndex..., in: content)) {
                let y = Float(content[Range(m.range(at: 1), in: content)!])!
                #expect(y >= 72, "a baseline at y = \(y)")
                baselines += 1
            }
        }
        #expect(baselines > 100, "\(baselines) baselines")
        // The frame has no height of its own, as before.
        #expect(frame.getHeight() == 0)
    }

    @Test func aFrameWithAHeightHasItOnEveryPage() {
        let pdf = TestSupport.newPDF()
        var tall = [Page]()
        novel(TestSupport.helvetica(pdf)).drawOn(pdf, &tall, Letter.PORTRAIT)
        let frame = novel(TestSupport.helvetica(pdf)).setHeight(300)
        var pages = [Page]()
        let xy = frame.drawOn(pdf, &pages, Letter.PORTRAIT)
        #expect(pages.count > tall.count, "\(pages.count) pages, not more than \(tall.count)")
        #expect(frame.getHeight() == 300)
        TestSupport.expectXY(540, 372, xy)
        // An empty frame needs no page.
        var none = [Page]()
        TestSupport.expectXY(10, 20, TextFrame([Paragraph]()).setLocation(10, 20).drawOn(pdf, &none, Letter.PORTRAIT))
        #expect(none.isEmpty)
    }

    // A paragraph of text lines in the font, each added with add, or with
    // addJoined when it starts with "+", which is not part of its text.
    static func joinedParagraph(_ font: Font, _ texts: [String]) -> Paragraph {
        let paragraph = Paragraph()
        for text in texts {
            if text.hasPrefix("+") {
                paragraph.addJoined(TextLine(font, String(text.dropFirst())))
            } else {
                paragraph.add(TextLine(font, text))
            }
        }
        return paragraph
    }

    private func drawJoined(_ width: Float, _ alignment: Alignment?, _ texts: String...) -> String {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let paragraph = TextFrameTests.joinedParagraph(font, texts)
        if let alignment {
            paragraph.setTextAlignment(alignment)
        }
        let page = Page(pdf, Letter.PORTRAIT)
        TextFrame([paragraph]).setLocation(10, 10).setWidth(width).drawOn(page)
        return TestSupport.content(page)
    }

    @Test func aJoinedTextLineHasNoSpaceBeforeIt() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoined(300, nil, "one", "+,", "two")
        let one = TestSupport.positionOf(content, "one")
        let comma = TestSupport.positionOf(content, ",")
        let two = TestSupport.positionOf(content, "two")
        TestSupport.expectNear(one[0] + font.stringWidth("one"), comma[0])
        TestSupport.expectNear(comma[0] + font.stringWidth(", "), two[0])
        TestSupport.expectNear(one[1], two[1])
    }

    @Test func aRowDoesNotBreakInsideAJoinedWord() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        // "aaa bbb" fits in the row, and "aaa bbbccc" does not, so bbb goes on
        // the next row with the ccc joined to it.
        let width = font.stringWidth("aaa bbb") + 1
        let content = drawJoined(width, nil, "aaa bbb", "+ccc")
        let aaa = TestSupport.positionOf(content, "aaa")
        let bbb = TestSupport.positionOf(content, "bbb")
        let ccc = TestSupport.positionOf(content, "ccc")
        #expect(bbb[1] < aaa[1], "bbb is on the second row")
        TestSupport.expectNear(10, bbb[0])
        TestSupport.expectNear(bbb[1], ccc[1])
        TestSupport.expectNear(bbb[0] + font.stringWidth("bbb"), ccc[0])
    }

    @Test func aJoinedWordWiderThanTheFrameBreaksWhereItIsJoined() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoined(font.stringWidth("abc") + 1, nil, "abc", "+def")
        let abc = TestSupport.positionOf(content, "abc")
        let def = TestSupport.positionOf(content, "def")
        #expect(def[1] < abc[1], "def is on the second row")
        TestSupport.expectNear(10, def[0])
    }

    @Test func aSpaceWhereTheyMeetKeepsJoinedTextLinesApart() {
        #expect(drawJoined(300, nil, "one", "two") == drawJoined(300, nil, "one", "+ two"))
        #expect(drawJoined(300, nil, "one ", "two") == drawJoined(300, nil, "one ", "+two"))
    }

    @Test func aJustifiedRowDoesNotWidenAJoin() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let content = drawJoined(200, Alignment.JUSTIFY,
                "one two three", "+,", "four five six seven eight nine ten eleven twelve")
        let three = TestSupport.positionOf(content, "three")
        let comma = TestSupport.positionOf(content, ",")
        let four = TestSupport.positionOf(content, "four")
        TestSupport.expectNear(three[1], four[1])
        TestSupport.expectNear(three[0] + font.stringWidth("three"), comma[0])
        // The spaces are widened: four is further than one space after the comma.
        #expect(four[0] > comma[0] + font.stringWidth(", ") + 1, "the row is justified")
    }
}
