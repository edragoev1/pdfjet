/**
 * ReviewFollowupTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// What was left open by the review: CMYK colors in PDF/A, the footer of a big
/// table, and the destinations and headings in a container.
@Suite struct ReviewFollowupTests {
    private let delta: Float = 0.01

    @Test func aPDFADocumentUsesNoCMYKColor() {
        // The output intent of PDF/A is sRGB, so its colors are not CMYK, as
        // its images are not.
        for level in [Compliance.PDF_A_1B, Compliance.PDF_A_2B, Compliance.PDF_A_3A_UA_1] {
            for pen in [true, false] {
                let pdf = MemoryPDF(level).pdf
                let page = Page(pdf, Letter.PORTRAIT)
                let before = TestSupport.content(page)
                if pen {
                    page.setPenColorCMYK(0.1, 0.2, 0.3, 0.4)
                } else {
                    page.setBrushColorCMYK(0.1, 0.2, 0.3, 0.4)
                }
                #expect(pdf.error == "A document of \(level) cannot use a CMYK color: "
                        + "its output intent is sRGB, so its colors are gray or RGB.")
                // Nothing is written
                #expect(TestSupport.content(page) == before)
            }
        }
        // A document that is not PDF/A uses them
        for level in [Compliance.PDF_1_7, Compliance.PDF_UA_1] {
            let pdf = MemoryPDF(level).pdf
            let page = Page(pdf, Letter.PORTRAIT)
            page.setPenColorCMYK(0.1, 0.2, 0.3, 0.4)
            page.setBrushColorCMYK(0.1, 0.2, 0.3, 0.4)
            #expect(pdf.error == nil)
            let content = TestSupport.content(page)
            #expect(content.contains("0.1 0.2 0.3 0.4 K\n"))
            #expect(content.contains("0.1 0.2 0.3 0.4 k\n"))
        }
    }

    @Test func theFooterOfABigTableIsAPaginationArtifact() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let font = TestSupport.helvetica(memory.pdf)
        var rows = [[String]]()
        for i in 0..<100 {
            rows.append(["n\(i)", "City, \(i)", "\(i).5"])
        }
        let table = BigTable(memory.pdf, font, font, Letter.PORTRAIT)
        table.setNumberOfColumns(3)
        table.setTableData(["Name", "City", "Total"], rows)
        table.setLocation(10, 10)
        try table.complete()
        let pages = table.getPages()
        // The content of the last page, which is written when the PDF is
        let content = TestSupport.content(pages[pages.count - 1])
        // The footer is marked once, as a footer, and not inside a plain artifact
        let footer = content.range(of: "/Artifact <</Type /Pagination /Subtype /Footer>> BDC\n")
        let text = content.range(of: TestSupport.hex("Page 2 of 2"))
        #expect(footer != nil && text != nil && footer!.lowerBound < text!.lowerBound)
        #expect(!content.contains("/Artifact BMC\n/Artifact <<"))
        try memory.pdf.complete()
    }

    // Draws the text line with a destination, as a heading, in a container at
    // 100, 200 turned by the degrees, or on the page at x, y moved by 100, 200
    // when there is no container, and returns the page.
    private func drawInContainer(_ inContainer: Bool, _ degrees: Double) -> Page {
        let pdf = MemoryPDF(Compliance.PDF_UA_1).pdf
        _ = pdf.setTitle("Title")
        let page = Page(pdf, Letter.PORTRAIT)
        let font = TestSupport.helvetica(pdf)
        let line = TextLine(font, "Heading")
        line.setDestination("heading")
        line.setStructureType(StructElem.H1)
        if inContainer {
            line.setLocation(0, 50)
            let container = Container(100, 100)
            container.setLocation(100, 200)
            container.setRotation(degrees)
            container.add(line)
            _ = container.drawOn(page)
        } else {
            line.setLocation(100, 250)
            _ = line.drawOn(page)
        }
        return page
    }

    @Test func aDestinationAndAHeadingInAContainerAreWhereTheTextIs() {
        // Moved with the container, as if drawn where it puts the text
        let want = drawInContainer(false, 0)
        let got = drawInContainer(true, 0)
        // The left of the page, where the destination of a text line is, is
        // the left of the container
        #expect(abs(want.destinations[0].xPosition + 100 - got.destinations[0].xPosition) < delta)
        #expect(abs(want.destinations[0].yPosition - got.destinations[0].yPosition) < delta)
        #expect(abs(want.pdf.headings[0].top - got.pdf.headings[0].top) < delta)
        #expect(abs(250 - 12 - got.pdf.headings[0].top) < delta)

        // Turned a quarter, the text runs down the page from 50 left of the
        // center, 150 and 250, and its top 12 above the baseline is 12 right of it
        let turned = drawInContainer(true, 90)
        #expect(abs(162 - turned.destinations[0].xPosition) < delta)
        #expect(abs(792 - 200 - turned.destinations[0].yPosition) < delta)
        #expect(abs(200 - turned.pdf.headings[0].top) < delta)
    }
}
