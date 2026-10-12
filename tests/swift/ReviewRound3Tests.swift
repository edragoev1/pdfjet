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

    /// Two characters copied as the same text, as a letter and its
    /// presentation form, need no actual text for the second: Arabic text
    /// had three times the spans it needed, its content 27% larger (T2).
    @Test func aCharacterCopiedAsTheFirstHasNoActualText() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"))
        try #require(font.unicodeToGID[0x0644] == font.unicodeToGID[0xFEDD])
        font.useChar(0xFEDD, font.unicodeToGID[0xFEDD])
        #expect(!font.drawnAsAnother(0x0644))
        font.useChar(0x0661, font.unicodeToGID[0x0661])
        #expect(font.drawnAsAnother(0x06F1))
    }

    /// A glyph drawn for another character stays in the span of a word with
    /// moved marks, whose actual text gives the character: in a span of its
    /// own, the word's span began with a moved mark and the space that
    /// stands for it, and "ةِمى" was copied as "ىمِ ة" (T1).
    @Test func aWordWithMarksIsCopiedWithoutASpaceInside() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"))
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "\u{0628}").setLocation(50, 50).drawOn(page)
        // The glyph of ة as drawn first for another character, which no two
        // characters of this font copied as different text are
        font.program!.chars[font.unicodeToGID[0x0629]] = 0x58
        TextLine(font, "\u{0629}\u{0650}\u{0645}\u{0649}").setLocation(50, 100).drawOn(page)
        let content = TestSupport.content(page)
        #expect(!content.contains("/ActualText <FEFF0020"), "\(content)")
        #expect(content.contains("/ActualText <" + Page.toUTF16Hex("\u{0629}\u{0650}\u{0645}\u{0649}") + ">"), "\(content)")
    }

    /// A cell's word broken between its characters breaks again at a soft
    /// hyphen, as TextBlock and TextFrame do: "1234<SHY>5678" was
    /// 123 / 4<SHY>56 / 78 (T4).
    @Test func aCellBreaksAtASoftHyphenAfterACharacterBreak() {
        let font = TestSupport.helvetica(TestSupport.newPDF())
        let cell = Cell(font, "1234\u{00AD}5678")
        cell.setWidth(font.stringWidth("123"))
        cell.setLeftPadding(0)
        cell.setRightPadding(0)
        let lines = Table().wrapCellText([cell], 0).map { $0.replacingOccurrences(of: "\u{00AD}", with: "") }
        #expect(lines == ["123", "4-", "567", "8"])
        let wide = Cell(font, "W")
        wide.setWidth(1)
        #expect(Table().wrapCellText([wide], 0) == ["W"])
    }

    /// A CJK font fits all of a text in a width of more characters than it
    /// has, as 1e30 or an infinity, in every port: Go and C# fit none (T6).
    @Test func aCJKFontFitsAllOfATextInAHugeWidth() {
        let font = TestSupport.cjkFont(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT)
        let text = "\u{4E2D}\u{6587}\u{5B57}"
        for width: Float in [1e30, .infinity, .greatestFiniteMagnitude] {
            #expect(font.getFitChars(text, width) == 3, "width \(width)")
        }
        for width: Float in [.nan, -1, 0] {
            #expect(font.getFitChars(text, width) == 0, "width \(width)")
        }
        #expect(font.getFitChars(text, 2 * font.getSize()) == 2)
    }

    /// A zero width joiner the font has no glyph for, in left to right text
    /// between two LRMs, is in the actual text of the run, as the stamp keeps
    /// it: it was left out (T7).
    @Test func aJoinerInARunIsInItsActualText() throws {
        let pdf = TestSupport.newPDF()
        let font = try Font(pdf, TestSupport.open("fonts/NotoSansJP/NotoSansJP-Regular.ttf"))
        try #require(font.unicodeToGID[0x200D] == 0)
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "d\u{200D}\u{200E}\u{200C}e\u{200D}").setLocation(50, 50).drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("/ActualText <" + Page.toUTF16Hex("\u{200E}e\u{200D}\u{200E}") + ">"), "\(content)")
    }

    /// A /MediaBox that refers to an object of more than a box is not read:
    /// 1,000 pages over an array of a million numbers, a PDF of 2 MB, copied
    /// the array for each page, 15 GB in Go (N1).
    @Test func aMediaBoxOfAnotherObjectIsReadOnlyWhenItIsABox() throws {
        let pages = 1000
        let kids = (0..<pages).map { "\(5 + $0) 0 R" }.joined(separator: " ")
        var objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [\(kids)] /Count \(pages) >>",
            "[" + String(repeating: "0 ", count: 400000) + "]",
            "[0 0 612 792]"]
        for i in 0..<pages {
            objects.append("<< /Type /Page /Parent 2 0 R /MediaBox \(i == 0 ? 4 : 3) 0 R >>")
        }
        let read = try TestSupport.read(TestSupport.pdfWithObjects(objects))
        let pdf = TestSupport.newPDF()
        var found = [PDFobj]()
        let elapsed = ContinuousClock().measure {
            found = pdf.getPageObjects(from: read)
        }
        // A few milliseconds; the array copied for each page took 2.5 s
        #expect(elapsed < .seconds(1), "\(elapsed)")
        #expect(found.count == pages)
        #expect(found[0].getValue("/MediaBox") == "[ 0 0 612 792 ]")
        #expect(found[1].getValue("/MediaBox") == "3")
    }

    /// A /Font dictionary that is an object of its own, which small resources
    /// of each page refer to, is added once: 4,000 pages over one of 4,000
    /// fonts, a PDF of 1 MB, copied it for each page, 4.6 GB in Go (N2).
    @Test func aFontDictionaryOfItsOwnIsAddedOnce() throws {
        let pages = 2000
        let fonts = 2000
        let kids = (0..<pages).map { "\(5 + $0) 0 R" }.joined(separator: " ")
        let entries = (0..<fonts).map { "/F\($0) 4 0 R" }.joined(separator: " ")
        let objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [\(kids)] /Count \(pages) >>",
            "<< \(entries) >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"]
            + Array(repeating: "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font 3 0 R >> >>",
                    count: pages)
        let read = try TestSupport.read(TestSupport.pdfWithObjects(objects))
        let pdf = TestSupport.newPDF()
        let elapsed = ContinuousClock().measure {
            pdf.addResourceObjects(from: read)
        }
        #expect(elapsed < .seconds(2), "\(elapsed)")
        #expect(pdf.importedFonts.count == 4 * fonts)
    }
}
