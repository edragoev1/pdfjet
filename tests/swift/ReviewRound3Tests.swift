/**
 * ReviewRound3Tests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// The findings of the third review, of 12 October 2026, as tests.
@Suite struct ReviewRound3Tests {
    /// A text rendering mode set back to 0 is written: Tr lasts past ET, and
    /// the text after an invisible line stayed invisible (F1).
    @Test func theTextRenderingModeIsSetBackToZero() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "plain").setLocation(50, 50).drawOn(page)
        #expect(!TestSupport.content(page).contains(" Tr\n"))
        _ = try page.setTextRenderingMode(3)
        TextLine(font, "hidden").setLocation(50, 100).drawOn(page)
        _ = try page.setTextRenderingMode(0)
        TextLine(font, "VISIBLE AGAIN").setLocation(50, 150).drawOn(page)
        let content = TestSupport.content(page)
        let before = String(content[..<content.range(of: TestSupport.hex("VISIBLE AGAIN"))!.lowerBound])
        let zero = before.range(of: "0 Tr\n", options: .backwards)
        let three = before.range(of: "3 Tr\n", options: .backwards)
        #expect(zero != nil && three != nil && three!.lowerBound < zero!.lowerBound, "\(content)")
    }

    /// A font of another PDF is refused and nothing is drawn with it, on a
    /// page or a stamp: drawing read what the font recorded for its own PDF,
    /// which another thread may be changing (S1, C3).
    @Test func aFontOfAnotherPDFDrawsNothing() throws {
        let owner = TestSupport.newPDF()
        let font = try TestSupport.plexSans(owner)
        TextLine(font, "\u{0661}A").setLocation(50, 50).drawOn(Page(owner, Letter.PORTRAIT))
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "\u{06F1}\u{0661} Hello").setLocation(50, 50).drawOn(page)
        #expect(pdf.error == "The font belongs to another PDF.")
        #expect(TestSupport.content(page).range(of: "<[0-9A-F]+> Tj", options: .regularExpression) == nil)
        let pdf2 = TestSupport.newPDF()
        let stamp = Stamp(pdf2).setSize(100, 100)
        stamp.drawText(font, 12, 0, 12, "\u{06F1}\u{0661} Hello")
        #expect(pdf2.error == "The font belongs to another PDF.")
        #expect(stamp.buf.isEmpty)
    }

    /// A text frame at NaN, or a Markdown text with NaN margins, is refused:
    /// no word was placed, and pages were made until the memory ran out (F4).
    @Test func aNaNLocationOrMarginIsRefused() throws {
        let cases: [[Float]] = [
            [.nan, 50, 200, 0], [50, .nan, 200, 0], [50, 50, .nan, 0], [50, 50, 200, .infinity]]
        for xywh in cases {
            let pdf = TestSupport.newPDF()
            let frame = TextFrame(TestSupport.helvetica(pdf), ["Hello frame text"])
                .setWidth(xywh[2]).setHeight(xywh[3])
            frame.setLocation(xywh[0], xywh[1])
            var pages = [Page]()
            frame.drawOn(pdf, &pages, Letter.PORTRAIT)
            #expect(pdf.error == "The text frame's location and size must be numbers, not NaN or infinite.")
            #expect(pages.isEmpty)
        }
        for margin: Float in [.nan, .infinity] {
            let pdf = TestSupport.newPDF()
            let font = TestSupport.helvetica(pdf)
            var pages = [Page]()
            let markdown = Markdown(font, font, font, font, font).setMargins(margin, 72, 72, 72)
            let error = #expect(throws: PDFjetError.self) {
                _ = try markdown.drawOn(pdf, "Some *text*.", &pages, Letter.PORTRAIT)
            }
            #expect(error?.message == "The margins of the Markdown must be numbers, not NaN or infinite.")
            #expect(pages.isEmpty)
        }
    }

    /// The rows of Markdown tables past 100,000 cells are counted, not kept:
    /// each row was padded to the header's 64 columns first, and 2 MB of rows
    /// of "|" made 1.65 GB of cells before the document was refused (C2).
    @Test func theRowsPastTheTableLimitAreNotKept() throws {
        let text = "|" + String(repeating: " a |", count: 64) + "\n|" + String(repeating: "---|", count: 64) + "\n"
            + String(repeating: "|\n", count: 3000)
        let blocks = MarkdownParser.parse(text)
        #expect(blocks[0].rows!.count <= 100000 / 64 + 1, "\(blocks[0].rows!.count) rows kept")
        #expect(MarkdownParser.tableCells(blocks) == 64 * 3001)
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        var pages = [Page]()
        let markdown = Markdown(font, font, font, font, font)
        let error = #expect(throws: PDFjetError.self) {
            _ = try markdown.drawOn(pdf, text, &pages, Letter.PORTRAIT)
        }
        #expect(error?.message == "The tables of the Markdown have more than 100000 cells: draw a table that large with BigTable.")
    }
}
