/**
 * FlowTextTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

// The flows of these tests are on Letter pages with margins of 50 points: the
// text is 512 points wide, from x 50, and the pages hold 692 points of it,
// from y 50 to 742.
@Suite struct FlowTextTests {
    private let margin: Float = 50
    private let bottom: Float = 742
    private let width: Float = 512
    private let height: Float = 692

    private func flow(_ pdf: PDF) -> FlowText {
        return FlowText(pdf, Letter.PORTRAIT).setMargins(margin)
    }

    private func paragraph(_ font: Font, _ text: String) -> Paragraph {
        return Paragraph(TextLine(font, text))
    }

    // An image of the repository, 100 points wide and as tall as the height,
    // narrower than the text.
    private func image(_ pdf: PDF, _ height: Float) throws -> Image {
        let image = try Image(pdf, TestSupport.open("images/up-arrow.png"))
        return image.scaleBy(100 / image.getWidth(), height / image.getHeight())
    }

    @Test func paragraphsFlowAcrossPagesBetweenTheMargins() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var paragraphs = [Paragraph]()
        for i in 1...120 {
            paragraphs.append(paragraph(font, "Paragraph \(i), long enough to be wrapped onto a second line "
                    + "of the text, which is as wide as the page less its margins of fifty points."))
        }
        var pages = [Page]()
        let xy = flow(pdf).add(paragraphs, 6).drawOn(&pages)
        #expect(pages.count >= 2, "120 paragraphs on \(pages.count) page")
        for (i, p) in paragraphs.enumerated() {
            #expect(p.getY1() >= margin - TestSupport.delta && p.getY2() <= bottom + TestSupport.delta,
                    "paragraph \(i + 1) from \(p.getY1()) to \(p.getY2()), outside the margins")
        }
        TestSupport.expectNear(margin + width, xy[0])      // the right margin
        TestSupport.expectNear(paragraphs[119].getY2(), xy[1])
    }

    @Test func anImageThatDoesNotFitGoesWholeToTheNextPage() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let small = try image(pdf, 40)
        let large = try image(pdf, 200)
        let text = paragraph(font, "Above the images")
        var pages = [Page]()
        flow(pdf).add(text).addSpace(600).add(small).add(large).drawOn(&pages)
        TestSupport.expectNear(text.getY2() + 600, small.y)    // under the space
        #expect(pages.count == 2)
        TestSupport.expectNear(margin, large.y)     // at the top of the next page
        TestSupport.expectNear(200, large.getHeight())
    }

    @Test func anImageTooLargeIsMadeAsLargeAsTheTextAndThePage() throws {
        let pdf = TestSupport.newPDF()
        let wide = try image(pdf, 100)
        wide.scaleBy(20, 1)     // 2,000 points wide
        let tall = try image(pdf, 2000)
        let shape = tall.getWidth() / tall.getHeight()
        var pages = [Page]()
        flow(pdf).add(wide).add(tall).drawOn(&pages)
        TestSupport.expectNear(width, wide.getWidth())
        TestSupport.expectNear(25.6, wide.getHeight())     // 100 * 512 / 2,000
        TestSupport.expectNear(height, tall.getHeight())
        TestSupport.expectNear(shape, tall.getWidth() / tall.getHeight(), 1e-4)
        TestSupport.expectNear(margin, tall.y)      // at the top of the second page
        #expect(pages.count == 2)
    }

    @Test func anImageIsAlignedLeftCenterOrRight() throws {
        let pdf = TestSupport.newPDF()
        let left = try image(pdf, 50)
        let center = try image(pdf, 50)
        let right = try image(pdf, 50)
        var pages = [Page]()
        flow(pdf).add(left).add(center, Alignment.CENTER).add(right, Alignment.RIGHT).drawOn(&pages)
        let w = left.getWidth()
        TestSupport.expectNear(margin, left.x)
        TestSupport.expectNear(margin + (width - w) / 2, center.x)
        TestSupport.expectNear(margin + width - w, right.x)
        TestSupport.expectNear(left.y + 50, center.y)      // one below the other
    }

    @Test(arguments: [true, false])
    func aHeadingKeepsWithTheFirstLineAfterIt(_ followed: Bool) throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let heading = Paragraph(TextLine(font, "A heading").setFontSize(16)).setStructureType(StructElem.H1)
        let headingHeight = font.getAscent(16) + font.getDescent(16)
        // Room for the heading below the image, 2 points more, but not for a
        // line after it too
        let filler = try image(pdf, height - headingHeight - 2)
        let flow = self.flow(pdf).add(filler).add(heading)
        let text = paragraph(font, "The text under the heading")
        if followed {
            flow.add(text)
        }
        var pages = [Page]()
        flow.drawOn(&pages)
        if followed {
            TestSupport.expectNear(margin, heading.getY1())     // on the next page with its text
            TestSupport.expectNear(heading.getY2(), text.getY1())
            #expect(pages.count == 2)
        } else {
            TestSupport.expectNear(filler.y + filler.getHeight(), heading.getY1())
            #expect(pages.count == 1)
        }
    }

    @Test func aSpaceAtTheTopOfAPageIsLeftOut() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let first = paragraph(font, "At the top of the first page")
        let filler = try image(pdf, height)
        let second = paragraph(font, "At the top of the second page")
        var pages = [Page]()
        flow(pdf).addSpace(30).add(first).add(filler).addSpace(30).add(second).drawOn(&pages)
        TestSupport.expectNear(margin, first.getY1())      // a space before everything
        TestSupport.expectNear(margin, second.getY1())     // a space after a full page
        #expect(pages.count == 3)       // The text, the image, the text
    }

    @Test func startsOnAPageBelowTheFirstPageTop() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let first = Page(pdf, Letter.PORTRAIT, false)
        let text = paragraph(font, "Under the letterhead")
        var pages = [Page]()
        flow(pdf).setFirstPageTop(200).add(text).drawOn(first, &pages)
        TestSupport.expectNear(200, text.getY1())
        #expect(pages.isEmpty)      // the first page is not added
    }

    @Test func nothingToDrawMakesNoPage() {
        let pdf = TestSupport.newPDF()
        var pages = [Page]()
        flow(pdf).addSpace(20).add(Paragraph()).drawOn(&pages)
        #expect(pages.isEmpty)
    }

    @Test func marginsThatLeaveNoRoomAreRefused() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        flow(pdf).setMargins(400).add(paragraph(font, "text")).drawOn(&pages)
        #expect(pdf.error != nil)
        #expect(pages.isEmpty)
    }
}
