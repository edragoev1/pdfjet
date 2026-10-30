/**
 * TextFlowTests.swift
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
@Suite struct TextFlowTests {
    private let margin: Float = 50
    private let bottom: Float = 742
    private let width: Float = 512
    private let height: Float = 692

    private func flow(_ pdf: PDF) -> TextFlow {
        return TextFlow(pdf, Letter.PORTRAIT).setMargins(margin)
    }

    private func paragraph(_ font: Font, _ text: String) -> Paragraph {
        return Paragraph(TextLine(font, text))
    }

    // An image of the repository, 100 points wide and as tall as the height,
    // narrower than the text.
    private func image(_ pdf: PDF, _ height: Float) throws -> Image {
        let image = try Image(pdf, TestSupport.open("resources/images/up-arrow.png"))
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

    @Test(arguments: [true, false])
    func anImageKeepsWithItsCaption(_ captioned: Bool) throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let caption = paragraph(font, "Figure 1: the image above")
        if captioned {
            caption.setStructureType(StructElem.CAPTION)
        }
        // Room for the image below the text, but not for its caption too
        let text = paragraph(font, "Above the image")
        let image = try self.image(pdf, height - (font.getAscent(font.getSize()) + font.getDescent(font.getSize())) - 4)
        var pages = [Page]()
        flow(pdf).add(text).add(image).addSpace(2).add(caption).drawOn(&pages)
        #expect(pages.count == 2)
        if captioned {
            TestSupport.expectNear(margin, image.y)     // on the next page with its caption
            TestSupport.expectNear(image.y + image.getHeight() + 2, caption.getY1())
        } else {
            TestSupport.expectNear(text.getY2(), image.y)
            TestSupport.expectNear(margin, caption.getY1())     // not a caption: on the next page
        }
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

    // A heading keeps a heading after it whole, with what that one keeps, and
    // an image with its caption: it kept the first line of the heading, or
    // the image alone, and was left at the bottom of the page when they went
    // to the next (the review of 28 October 2026).
    @Test func aHeadingKeepsTheHeadingOrTheCaptionedImageAfterIt() throws {
        var pdf = TestSupport.newPDF()
        var font = TestSupport.helvetica(pdf)
        var h1 = Paragraph(TextLine(font, "Chapter").setFontSize(22)).setStructureType(StructElem.H1)
        let h2 = Paragraph(TextLine(font, "Section").setFontSize(16)).setStructureType(StructElem.H2)
        let text = paragraph(font, "The text under the headings")
        let h1Height = font.getAscent(22) + font.getDescent(22)
        let h2Height = font.getAscent(16) + font.getDescent(16)
        // Room below the image for the two headings, 2 points more, but not
        // for a line after them too
        var filler = try image(pdf, height - h1Height - 6 - h2Height - 6 - 2)
        var pages = [Page]()
        flow(pdf).add(filler).add(h1).addSpace(6).add(h2).addSpace(6).add(text).drawOn(&pages)
        TestSupport.expectNear(margin, h1.getY1())     // on the next page
        TestSupport.expectNear(h1.getY2() + 6, h2.getY1())
        TestSupport.expectNear(h2.getY2() + 6, text.getY1())

        pdf = TestSupport.newPDF()
        font = TestSupport.helvetica(pdf)
        h1 = Paragraph(TextLine(font, "Figures").setFontSize(22)).setStructureType(StructElem.H1)
        let caption = paragraph(font, "Figure 1: the image above").setStructureType(StructElem.CAPTION)
        let picture = try image(pdf, 300)
        // Room below the filler for the heading and the image, 2 points more,
        // but not for the caption too
        filler = try image(pdf, height - h1Height - 300 - 2)
        pages = [Page]()
        flow(pdf).add(filler).add(h1).add(picture).addSpace(4).add(caption).drawOn(&pages)
        TestSupport.expectNear(margin, h1.getY1())     // on the next page
        TestSupport.expectNear(h1.getY2(), picture.y)
        TestSupport.expectNear(picture.y + 300 + 4, caption.getY1())
        #expect(pages.count == 2)
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
