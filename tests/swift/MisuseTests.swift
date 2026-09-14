/**
 * MisuseTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// A program that uses the API the wrong way has the misuse recorded, where
/// Java throws, and complete() then throws it, so no broken PDF is written.
@Suite struct MisuseTests {
    private let earlier = "The PDF was not completed because of an earlier error: "
    private let notWritable = "A coordinate, size or width is NaN, infinite or too large for a PDF."

    /// Returns the message complete() throws, or "" when it completes the PDF.
    private func completeMessage(_ pdf: PDF) -> String {
        do {
            try pdf.complete()
            return ""
        } catch {
            return TestSupport.message(error)
        }
    }

    @Test func aNumberThatIsNotFiniteOrTooLargeIsRefused() {
        let values: [Float] = [.nan, .infinity, -1e30, 2147483648]
        for bad in values {
            let pdf = TestSupport.newPDF()
            let page = Page(pdf, Letter.PORTRAIT)
            page.drawLine(10, 10, bad, 200)
            #expect(pdf.error == notWritable)
            #expect(completeMessage(pdf) == earlier + notWritable)
        }
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        page.drawLine(0, 0, 100000000, 0)
        #expect(TestSupport.content(page).contains("100000000 792 l\n"), "\(TestSupport.content(page))")
        #expect(page.pdf.error == nil)
    }

    @Test func aNaNFontSizeIsRefused() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        _ = font.setSize(.nan)
        TextLine(font, "Hello").setLocation(50, 50).drawOn(page)
        #expect(pdf.error == notWritable)
    }

    @Test func aDashPatternIsAnArrayAndAPhase() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        _ = page.setStrokeDashPattern("[] 0")
        _ = page.setStrokeDashPattern("[3 3] 0")
        _ = page.setStrokeDashPattern(" [2.5 .5 1]  0.5 ")
        #expect(TestSupport.content(page).hasSuffix(" [2.5 .5 1]  0.5  d\n"), "\(TestSupport.content(page))")
        #expect(page.pdf.error == nil)
        for bad in ["3 3", "[3 3]", "[a] 0", "[0 0] 0", "[-1 2] 0", "[3-3] 0", "[1.5.5] 0", "[3 3] 0 ET", ""] {
            let pdf = TestSupport.newPDF()
            let page2 = Page(pdf, Letter.PORTRAIT)
            _ = page2.setStrokeDashPattern(bad)
            #expect(pdf.error == "The dash pattern \"" + bad + "\" is not an array of non-negative numbers, " +
                    "not all zero, followed by a phase, such as \"[3 3] 0\".")
            #expect(TestSupport.content(page2) == "")
        }
    }

    @Test func aNegativePenWidthIsRefused() {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        _ = page.setPenWidth(0)
        #expect(page.pdf.error == nil)
        _ = page.setPenWidth(-2)
        #expect(page.pdf.error == "The pen width cannot be negative.")
    }

    @Test func theGraphicsStatesMustBePaired() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        page.restoreGraphicsState()
        #expect(pdf.error == "restoreGraphicsState was called without a matching saveGraphicsState.")
        #expect(TestSupport.content(page) == "")

        let pdf2 = TestSupport.newPDF()
        Page(pdf2, Letter.PORTRAIT).saveGraphicsState()
        _ = Page(pdf2, Letter.PORTRAIT)
        #expect(pdf2.error == "A page ends with a saveGraphicsState that has no restoreGraphicsState.")

        let pdf3 = TestSupport.newPDF()
        Page(pdf3, Letter.PORTRAIT).saveGraphicsState()
        #expect(completeMessage(pdf3) == "A page ends with a saveGraphicsState that has no restoreGraphicsState.")
    }

    @Test func theMarkedContentMustBePaired() {
        for compliance in [Compliance.PDF_1_7, Compliance.PDF_UA_1] {
            let pdf = PDF(OutputStream(toMemory: ()), compliance)
            let page = Page(pdf, Letter.PORTRAIT)
            page.addEMC()
            #expect(pdf.error == "addEMC was called without a matching addBDC or addArtifactBMC.")

            let pdf2 = PDF(OutputStream(toMemory: ()), compliance)
            Page(pdf2, Letter.PORTRAIT).addBDC(StructElem.P, "x", "x")
            #expect(completeMessage(pdf2) == "A page ends with an addBDC or addArtifactBMC that has no addEMC.")

            let pdf3 = PDF(OutputStream(toMemory: ()), compliance)
            let page3 = Page(pdf3, Letter.PORTRAIT)
            page3.addArtifactBMC()
            page3.addEMC()
            #expect(completeMessage(pdf3) == "")
        }
    }

    @Test func aWrittenPageCannotBeDrawnOn() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let page1 = Page(pdf, Letter.PORTRAIT)
        _ = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "Late").setLocation(50, 50).drawOn(page1)
        let message = "The page was already written to the PDF: draw on a page before " +
                "creating the next page or completing the PDF."
        #expect(pdf.error == message)
        #expect(completeMessage(pdf) == earlier + message)
    }

    @Test func completeFinishesADocumentOnce() {
        let pdf = TestSupport.newPDF()
        _ = Page(pdf, Letter.PORTRAIT)
        #expect(completeMessage(pdf) == "")
        #expect(completeMessage(pdf) == "complete() was already called.")

        let pdf2 = TestSupport.newPDF()
        _ = Page(pdf2, Letter.PORTRAIT)
        #expect(completeMessage(pdf2) == "")
        _ = Page(pdf2, Letter.PORTRAIT)
        #expect(pdf2.error == "The PDF was already completed.")
    }

    @Test func aDocumentNeedsAPage() {
        #expect(completeMessage(TestSupport.newPDF()) == "A PDF needs at least one page.")
    }

    @Test func aPageIsAddedOnceAndToItsOwnDocument() {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        pdf.addPage(page)
        #expect(pdf.error == nil)
        pdf.addPage(page)
        #expect(pdf.error == "The page was already added to the PDF.")

        let pdf2 = TestSupport.newPDF()
        let foreign = Page(TestSupport.newPDF(), Letter.PORTRAIT, Page.DETACHED)
        pdf2.addPage(foreign)
        #expect(pdf2.error == "The page belongs to another PDF.")
    }

    @Test func fontsImagesStampsAndGroupsBelongToOneDocument() throws {
        let other = TestSupport.newPDF()

        let pdf1 = TestSupport.newPDF()
        let font = TestSupport.helvetica(other)
        TextLine(font, "Hello").setLocation(50, 50).drawOn(Page(pdf1, Letter.PORTRAIT))
        #expect(pdf1.error == "The font belongs to another PDF.")

        let pdf2 = TestSupport.newPDF()
        let image = try Image(other, TestSupport.open("images/map407.png"))
        image.setLocation(50, 50).drawOn(Page(pdf2, Letter.PORTRAIT))
        #expect(pdf2.error == "The image belongs to another PDF.")

        let pdf3 = TestSupport.newPDF()
        let stamp = Stamp(other).setSize(50, 50)
        try stamp.complete()
        stamp.drawOn(Page(pdf3, Letter.PORTRAIT))
        #expect(pdf3.error == "The stamp belongs to another PDF.")

        let pdf4 = TestSupport.newPDF()
        Stamp(pdf4).setSize(50, 50).addFont(font)
        #expect(pdf4.error == "The font belongs to another PDF.")

        let pdf5 = TestSupport.newPDF()
        let group = OptionalContentGroup(other, "Layer")
        _ = group.add(Rect(0, 0, 1, 1))
        group.drawOn(Page(pdf5, Letter.PORTRAIT))
        #expect(pdf5.error == "The optional content group belongs to another PDF.")

        #expect(other.error == nil)
    }

    @Test func stampTextNeedsAFontAndAText() {
        let pdf = TestSupport.newPDF()
        _ = Stamp(pdf).setSize(100, 50).drawText(TextParameters().setText("Paid"))
        #expect(pdf.error == "Stamp text needs a font and a text.")

        let pdf2 = TestSupport.newPDF()
        _ = Stamp(pdf2).setSize(100, 50).drawText(TextParameters().setFont(TestSupport.helvetica(pdf2)))
        #expect(pdf2.error == "Stamp text needs a font and a text.")
    }

    @Test func aStampIsCompletedOnceBeforeItIsDrawn() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        Stamp(pdf).setSize(50, 50).drawOn(page)
        #expect(pdf.error == "Call complete() on the stamp before drawing it.")

        let pdf2 = TestSupport.newPDF()
        let stamp2 = Stamp(pdf2).setSize(50, 50)
        try stamp2.complete()
        do {
            try stamp2.complete()
            Issue.record("complete() did not throw")
        } catch {
            #expect(TestSupport.message(error) == "complete() was already called on the stamp.")
        }

        let pdf3 = TestSupport.newPDF()
        let stamp3 = Stamp(pdf3).setSize(50, 50)
        try stamp3.complete()
        stamp3.drawRect(0, 0, 10, 10)
        #expect(pdf3.error == "The stamp was already completed.")
    }

    @Test func stampTextAddsItsFont() throws {
        let pdf = TestSupport.newPDF()
        Stamp(pdf).setSize(100, 50).drawText(TestSupport.helvetica(pdf), 12, 5, 20, "Paid")
        #expect(pdf.error == "A stamp draws text with an embedded font, not a core or CJK font.")

        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
        let stamp = Stamp(memory.pdf).setSize(100, 50)
        stamp.drawText(font, 12, 5, 20, "Paid")
        try stamp.complete()
        stamp.setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.contains("/Font <<\n/F\(font.objNumber) \(font.objNumber) 0 R\n"))
    }

    @Test func encryptionAndComplianceComeBeforeTheContent() {
        let set = "Set the encryption before adding fonts, images or pages to the PDF."
        let setCompliance = "Set the compliance before adding fonts, images or pages to the PDF."

        let pdf = TestSupport.newPDF()
        _ = TestSupport.helvetica(pdf)
        _ = Encryption(pdf, Passwords(), Permissions())
        #expect(pdf.error == set)

        let pdf1 = TestSupport.newPDF()
        _ = TestSupport.helvetica(pdf1)
        _ = pdf1.setCompliance(Compliance.PDF_1_7)  // No change.
        #expect(pdf1.error == nil)
        _ = pdf1.setCompliance(Compliance.PDF_UA_1)
        #expect(pdf1.error == setCompliance)
        #expect(pdf1.getCompliance() == Compliance.PDF_1_7)

        let pdf2 = TestSupport.newPDF()
        _ = Page(pdf2, Letter.PORTRAIT, Page.DETACHED)
        _ = pdf2.setCompliance(Compliance.PDF_UA_1)
        #expect(pdf2.error == setCompliance)

        let pdf3 = TestSupport.newPDF()
        let encryption = Encryption(pdf3, Passwords(), Permissions())
        _ = TestSupport.helvetica(pdf3)
        _ = pdf3.setEncryption(encryption)
        #expect(pdf3.error == set)
    }

    @Test func aPageIsFromThreeTo14400PointsWideAndHigh() {
        let good = TestSupport.newPDF()
        _ = Page(good, PageSize(3, 3))
        _ = Page(good, PageSize(14400, 14400))
        #expect(good.error == nil)
        for bad in [PageSize(0, 0), PageSize(612, 2), PageSize(14401, 792), PageSize(.nan, 792)] {
            let pdf = TestSupport.newPDF()
            _ = Page(pdf, bad)
            #expect(pdf.error == "A page must be from 3 to 14400 points wide and high.")
        }
    }

    @Test func theXmpMetadataLeavesOutControlCharacters() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Report\u{01} 2026\u{FFFF} ")
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let raw = String(decoding: memory.bytes, as: UTF8.self)
        #expect(raw.contains("<rdf:li xml:lang=\"x-default\">Report 2026 </rdf:li>"))
    }

    @Test func aZeroSizeImageStampOrContainerDrawsNothing() throws {
        let pdf = TestSupport.newPDF()
        let page = Page(pdf, Letter.PORTRAIT)
        let image = try Image(pdf, TestSupport.open("images/map407.png"))
        _ = image.scaleBy(0)
        image.setLocation(50, 50).drawOn(page)
        let stamp = Stamp(pdf).setSize(50, 50)
        try stamp.complete()
        stamp.scaleBy(0).setLocation(50, 50).drawOn(page)
        let container = Container(100, 100)
        _ = container.add(Rect(0, 0, 10, 10))
        _ = container.scaleBy(0).setLocation(50, 50).drawOn(page)
        #expect(!TestSupport.content(page).contains(" cm\n"), "\(TestSupport.content(page))")
        #expect(pdf.error == nil)
    }
}
