/**
 * TextFrameTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
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
}
