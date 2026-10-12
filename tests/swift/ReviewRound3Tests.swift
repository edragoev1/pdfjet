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

    /// An image turned a quarter of the way is as wide as it was tall: it gave
    /// its corner unturned, and in a table cell ran over the rows after it (F3).
    @Test func anImageTurnedAQuarterHasItsDrawnSize() throws {
        let pdf = TestSupport.newPDF()
        let image = try Image(pdf, TestSupport.open("PngSuite/BASN2C08.PNG"))   // 32 by 32
        _ = image.scaleBy(0.75, 0.375)                                           // 24 by 12
        _ = image.setLocation(50, 50)
        for degrees in [90, 270] {
            _ = try image.setRotation(degrees)
            TestSupport.expectXY(62, 74, image.drawOn(nil))
            TestSupport.expectNear(12, image.getWidth())
            TestSupport.expectNear(24, image.getHeight())
            TestSupport.expectXY(62, 74, image.drawOn(Page(pdf, Letter.PORTRAIT)))
        }
        _ = try image.setRotation(0)
        TestSupport.expectXY(74, 62, image.drawOn(nil))
        // Turned, the image fits the page as it is drawn
        _ = try image.setRotation(90)
        image.resizeToFit(Page(pdf, Letter.PORTRAIT), keepAspectRatio: false)
        TestSupport.expectXY(612, 792, image.drawOn(nil))
    }

    /// A text line in a text column is raised by its vertical offset once:
    /// the column added it, and drawing the line added it again, so a
    /// superscript raised 13 points rose 26 (F6).
    @Test func aTextColumnRaisesASuperscriptOnce() throws {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let paragraph = Paragraph()
            .add(TextLine(font, "Base")).add(TextLine(font, "sup").setVerticalOffset(-13))
        let column = TextColumn()
        _ = column.setLocation(50, 100)
        _ = column.setWidth(400)
        _ = column.addParagraph(paragraph)
        _ = column.drawOn(page)
        let content = TestSupport.content(page)
        let rise = TestSupport.positionOf(content, "sup")[1] - TestSupport.positionOf(content, "Base")[1]
        TestSupport.expectNear(13, rise)
    }

    /// A destination the document names keeps its name, and a bookmark whose
    /// name it is, dest#1, is named again: a link to the document's dest#1
    /// went to the bookmark (F7).
    @Test func aDestinationNameOfTheDocumentComesFirst() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        _ = page.addDestination("dest#1", 500)
        TextLine(font, "go").setGoToAction("dest#1").setLocation(50, 50).drawOn(page)
        let bookmark = Bookmark(memory.pdf).addBookmark(page, Title(font, "A", 10, 100))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        let regex = try NSRegularExpression(pattern: "/Dest \\[\\d+ 0 R /XYZ 0 ([\\d.]+) 0\\]")
        let ys = regex.matches(in: raw, range: NSRange(raw.startIndex..., in: raw)).map {
            String(raw[Range($0.range(at: 1), in: raw)!])
        }
        #expect(ys.count == 2 && ys[0] == "292" && ys[1] != "292", "\(ys)")
        #expect(bookmark.getDestinationName() != "dest#1")
    }

    /// A bookmark on a page that is not added fails the document: it went to
    /// /Dest [0 0 R], and readers refused the document (F8).
    @Test func aBookmarkOnAPageNotAddedFailsTheDocument() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        _ = Page(memory.pdf, Letter.PORTRAIT)
        let detached = Page(memory.pdf, Letter.PORTRAIT, false)
        _ = Bookmark(memory.pdf).addBookmark(detached, Title(font, "Lost", 10, 100))
        let error = #expect(throws: PDFjetError.self) { try memory.pdf.complete() }
        #expect(error?.message == "The bookmark Lost is on a page that is not added to the PDF.")
    }

    /// An empty dash pattern of an arc is refused, with a stroke color and
    /// without, in every port: Go took it for none with a stroke color (S3, C7).
    @Test func anArcRefusesAnEmptyDashPattern() {
        for colored in [false, true] {
            let pdf = TestSupport.newPDF()
            let page = Page(pdf, Letter.PORTRAIT)
            let arc = Arc().setRadius(50).setSweep(90).setStrokeDashPattern("")
            _ = arc.setLocation(100, 100)
            if colored {
                _ = arc.setStrokeColor(Color.red)
            }
            _ = arc.drawOn(page)
            #expect(pdf.error == "The dash pattern \"\" is not an array of non-negative numbers, not all zero, followed by a phase, such as \"[3 3] 0\".")
        }
    }

    /// A NaN or infinite angle fails the document, as a NaN coordinate does:
    /// it wrote 0 0 0 0 0 0 cm, and the arc or the container vanished (F12).
    @Test func aNaNRotationFailsTheDocument() {
        for degrees: Float in [.nan, .infinity] {
            let pdf = TestSupport.newPDF()
            let arc = Arc().setRadius(50).setSweep(90).setRotation(degrees)
            _ = arc.setLocation(100, 100)
            _ = arc.drawOn(Page(pdf, Letter.PORTRAIT))
            #expect(pdf.error == "A coordinate, size or width is NaN, infinite or too large for a PDF.")

            let pdf2 = TestSupport.newPDF()
            let container = Container(100, 100).setRotation(Double(degrees))
            _ = container.add(Arc().setRadius(10).setSweep(90))
            _ = container.drawOn(Page(pdf2, Letter.PORTRAIT))
            #expect(pdf2.error == "A coordinate, size or width is NaN, infinite or too large for a PDF.")
        }
    }

    /// A curve has two "c" control points, or one "v" or "y" one: a "c" curve
    /// of one control point wrote "c" with four numbers, not six, which
    /// readers refused (F11).
    @Test func aCurveOfTheWrongControlPointsIsRefused() throws {
        let start = Point(10, 10)
        let end = Point(100, 100)
        let paths: [[Point]] = [
            [start, Point(20, 20, Point.CONTROL_POINT_C), end],
            [start, Point(20, 20, Point.CONTROL_POINT_V), Point(30, 30, Point.CONTROL_POINT_V), end],
            [start, Point(20, 20, Point.CONTROL_POINT_C), Point(30, 30, Point.CONTROL_POINT_Y), end]]
        for path in paths {
            let pdf = TestSupport.newPDF()
            Page(pdf, Letter.PORTRAIT).drawPath(path, PathOperator.STROKE)
            #expect(pdf.error == "A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.")
            let pdf2 = TestSupport.newPDF()
            try Stamp(pdf2).setSize(200, 200).drawPath(path, PathOperator.STROKE)
            #expect(pdf2.error == "A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.")
        }
        let pdf = TestSupport.newPDF()
        Page(pdf, Letter.PORTRAIT).drawPath(
            [start, Point(20, 20, Point.CONTROL_POINT_C), Point(30, 30, Point.CONTROL_POINT_C), end], PathOperator.STROKE)
        #expect(pdf.error == nil)
    }

    /// A point a path lists twice is scaled once: (10, 10) listed twice and
    /// scaled by 2 became (40, 40), not (20, 20) (F9).
    @Test func aPointListedTwiceIsScaledOnce() {
        let shared = Point(10, 10)
        let other = Point(30, 5)
        _ = Path().add(shared).add(other).add(shared).scaleBy(2)
        TestSupport.expectNear(20, shared.getX())
        TestSupport.expectNear(20, shared.getY())
        TestSupport.expectNear(60, other.getX())
    }

    private func roundedRect(_ x: Float, _ y: Float, _ w: Float, _ h: Float) -> String {
        let page = TestSupport.newPage()
        _ = Rect(x, y, w, h).setCornerRadius(10).setFillColor(Color.red).drawOn(page)
        return TestSupport.content(page)
    }

    /// A rounded rectangle of a negative width and height is the same
    /// rectangle the other way round: its corners were turned inside out (F10).
    @Test func aRoundedRectangleOfANegativeSizeIsTheSame() {
        #expect(roundedRect(100, 100, -50, -40) == roundedRect(50, 60, 50, 40))
    }

    /// A stamp is given its size before anything is drawn on it, as its y
    /// runs down from its top: drawn before, the content was below the
    /// stamp, and was not seen (F13).
    @Test func aStampIsSizedBeforeItIsDrawnOn() {
        let pdf = TestSupport.newPDF()
        Stamp(pdf).fillRect(0, 0, 20, 10)
        #expect(pdf.error == "Set the size of the stamp before drawing on it: its y runs down from its top.")
        let pdf2 = TestSupport.newPDF()
        Stamp(pdf2).setSize(20, 10).fillRect(0, 0, 20, 10).setSize(40, 20)
        #expect(pdf2.error == "Set the size of the stamp before drawing on it: its y runs down from its top.")
        let pdf3 = TestSupport.newPDF()
        Stamp(pdf3).setSize(20, 10).fillRect(0, 0, 20, 10).setSize(40, 10)
        #expect(pdf3.error == nil)
    }

    /// A point that is a link, drawn on its own, makes its link, and a marker
    /// of lines is stroked in the point's color: the link was not made, and a
    /// plus was drawn in the pen's color (F15).
    @Test func aPointDrawnOnItsOwnHasItsLinkAndColor() {
        let page = TestSupport.newPage()
        _ = Point(100, 100).setURIAction("https://pdfjet.com").drawOn(page)
        #expect(page.annots.count == 1)
        let page2 = TestSupport.newPage()
        _ = Point(100, 100).setShape(Shape.PLUS).setFillColor(Color.red).drawOn(page2)
        #expect(TestSupport.content(page2).contains("1 0 0 RG\n"), "\(TestSupport.content(page2))")
    }

    /// A paragraph a text column draws has its coordinates, as one a text
    /// frame draws does: they stayed 0 (F17).
    @Test func aParagraphOfATextColumnHasItsCoordinates() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let paragraph = Paragraph().add(TextLine(font, "Hello, world"))
        let column = TextColumn()
        _ = column.setLocation(50, 100)
        _ = column.setWidth(400)
        _ = column.addParagraph(paragraph)
        _ = column.drawOn(Page(pdf, Letter.PORTRAIT))
        TestSupport.expectNear(50, paragraph.getX1())
        TestSupport.expectNear(100, paragraph.getY1())
        TestSupport.expectNear(100 + font.getAscent(), paragraph.getTextY())
        #expect(paragraph.getY2() > paragraph.getTextY() && paragraph.getX2() > 50)
    }

    /// A watermark on a page of a NaN size fails the document, as in the other
    /// ports: Int() of the NaN angle stopped the program (S2).
    @Test func aWatermarkOnAPageOfANaNSizeFailsTheDocument() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, PageSize(.nan, .nan))
        page.addWatermark(TestSupport.helvetica(pdf), "DRAFT")
        #expect(pdf.error != nil)
    }

    /// Drawing on a page of a PDF that was read: the page's own content is
    /// drawn in a q and a Q of its own; a page with no /Contents gets the
    /// drawing, which was lost; and the drawing starts at the corner of the
    /// /MediaBox (F2).
    @Test func drawingOnAPageOfAPDFThatWasRead() throws {
        var objects = try TestSupport.read(TestSupport.pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [100 100 712 892] /Contents 4 0 R >>",
                "<< /Length 18 >>\nstream\n0.5 0 0 0.5 0 0 cm\nendstream",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"]))
        let pdf = TestSupport.newPDF()
        let pages = pdf.getPageObjects(from: objects)
        for pageObj in pages {
            let page = Page(pdf, pageObj)
            page.fillRect(0, 0, 30, 30)
            page.complete(&objects)
        }
        let wrapped = pages[0].dict
        let i = wrapped.firstIndex(of: "/Contents")!
        #expect(wrapped[(i + 1)...(i + 11)].joined(separator: " ") == "[ 6 0 R 4 0 R 7 0 R ]")
        #expect(TestSupport.latin1(objects[5].stream!) == "q\n")
        #expect(TestSupport.latin1(objects[6].stream!).hasPrefix("Q\nq\n1 0 0 1 100 100 cm\n"))
        let added = pages[1].dict
        let j = added.firstIndex(of: "/Contents")!
        #expect(added[(j + 1)...(j + 5)].joined(separator: " ") == "[ 8 0 R ]")
    }

    /// A document of PDF/A-1 of more than 8191 pages, the most entries of an
    /// array of PDF/A-1, is refused, and a text of PDF/A of more than 16383
    /// characters, a string longer than 32767 bytes: veraPDF failed both (W1).
    @Test func theSizeLimitsOfPDFAAreChecked() throws {
        let memory = MemoryPDF(Compliance.PDF_A_1B)
        for _ in 0..<8192 {
            _ = Page(memory.pdf, Letter.PORTRAIT)
        }
        let error = #expect(throws: PDFjetError.self) { try memory.pdf.complete() }
        #expect(error?.message == "PDF/A-1 limits an array to 8191 entries, and the page tree has 8192: make the document PDF/A-2 or PDF/A-3.")

        let text = String(repeating: "a", count: 16383)
        let fits = MemoryPDF(Compliance.PDF_A_2B)
        let fitsFont = try TestSupport.plexSans(fits.pdf)
        TextLine(fitsFont, text).setLocation(10, 20).drawOn(Page(fits.pdf, Letter.PORTRAIT))
        #expect(fits.pdf.error == nil)
        let tooLong = MemoryPDF(Compliance.PDF_A_2B)
        let tooLongFont = try TestSupport.plexSans(tooLong.pdf)
        TextLine(tooLongFont, text + "a").setLocation(10, 20).drawOn(Page(tooLong.pdf, Letter.PORTRAIT))
        #expect(tooLong.pdf.error == "A text of more than 16383 characters is a string longer than PDF/A allows, 32767 bytes: draw it as several lines.")
    }
}
