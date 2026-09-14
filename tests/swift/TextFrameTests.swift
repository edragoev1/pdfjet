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
}
