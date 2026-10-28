/**
 * MemoryLeakTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// A document is freed once the caller lets go of it: no object it holds
/// holds it back, nor its pages, fonts or images. Swift counts references, and
/// a cycle among them would keep every document a program writes.
@Suite struct MemoryLeakTests {
    private final class Weak {
        weak var pdf: PDF?
        weak var page: Page?
        weak var font: Font?
        weak var image: Image?
        weak var stamp: Stamp?
        weak var bookmark: Bookmark?
        weak var child: Bookmark?
        weak var annotation: Annotation?
        weak var group: OptionalContentGroup?
        weak var file: EmbeddedFile?
    }

    // Writes a document with a little of everything that holds on to the
    // document or a page, and returns weak references to its objects.
    private func write(_ compliance: Compliance, complete: Bool) throws -> Weak {
        let weak = Weak()
        do {
            let pdf = PDF(OutputStream(toMemory: ()), compliance)
            pdf.setTitle("Memory")
            let font = try TestSupport.plexSans(pdf)
            font.setSize(12)
            let page = Page(pdf, Letter.PORTRAIT)
            let root = Bookmark(pdf)

            let heading = TextLine(font, "Heading").setStructureType(StructElem.H1).setLocation(50, 50)
            heading.drawOn(page)
            root.addBookmark(page, Title(font, "Heading", 50, 50))
                    .addBookmark(page, Title(font, "Below", 50, 80))
            let next = root.addBookmark(page, Title(font, "Next", 50, 110))

            let link = TextLine(font, "https://pdfjet.com").setLocation(50, 80)
            link.setURIAction("https://pdfjet.com")
            link.drawOn(page)
            weak.annotation = page.annots.last

            let image = try Image(pdf, TestSupport.open("images/circles.png"))
            _ = image.setAltDescription("A penguin").setURIAction("https://pdfjet.com").setLocation(50, 100).drawOn(page)

            var rows = [[Cell]]()
            for r in 0..<3 {
                rows.append([Cell(font, "a\(r)"), Cell(font, "b\(r)")])
            }
            _ = Table().setTableData(rows, 1).setLocation(50, 250).drawOn(page)

            let file = try EmbeddedFile(pdf, "hello.txt", InputStream(data: Data("Hello".utf8)), false)
            let attachment = FileAttachment(file)
            attachment.setLocation(50, 350)
            attachment.setTitle("Attached file")
            attachment.setContents("A file")
            attachment.drawOn(page)

            let note = TextAnnotation()
            note.setLocation(100, 350)
            note.setSize(24, 24)
            note.setTitle("Reviewer")
            note.setContents("A note")
            _ = note.drawOn(page)

            CheckBox(font, "Check").setLocation(50, 400).drawOn(page)

            let group = OptionalContentGroup(pdf, "Layer")
            _ = group.add(TextLine(font, "In a layer").setLocation(50, 430))
            _ = group.drawOn(page)

            let stamp = Stamp(pdf).setSize(100, 100).addFont(font)
            _ = stamp.drawRect(10, 10, 80, 80)
            stamp.drawText(TextParameters().setFont(font).setFontSize(12).setLocation(20, 20).setText("Stamp"))
            try stamp.complete()
            stamp.setLocation(300, 300).drawOn(page)

            // A second page, for a cycle between the pages.
            let second = Page(pdf, Letter.PORTRAIT)
            TextLine(font, "Second page").setLocation(50, 50).drawOn(second)

            if complete {
                try pdf.complete()
            }
            weak.pdf = pdf
            weak.page = page
            weak.font = font
            weak.image = image
            weak.stamp = stamp
            weak.bookmark = root
            weak.child = next
            weak.group = group
            weak.file = file
        }
        return weak
    }

    private func expectFreed(_ weak: Weak) {
        #expect(weak.pdf == nil, "the PDF")
        #expect(weak.page == nil, "a page")
        #expect(weak.font == nil, "a font")
        #expect(weak.image == nil, "an image")
        #expect(weak.stamp == nil, "a stamp")
        #expect(weak.bookmark == nil, "a bookmark")
        #expect(weak.child == nil, "a bookmark after another")
        #expect(weak.annotation == nil, "a link")
        #expect(weak.group == nil, "an optional content group")
        #expect(weak.file == nil, "an embedded file")
    }

    @Test func aCompletedDocumentIsFreed() throws {
        expectFreed(try write(Compliance.PDF_1_7, complete: true))
    }

    @Test func aCompletedTaggedDocumentIsFreed() throws {
        expectFreed(try write(Compliance.PDF_UA_1, complete: true))
    }

    @Test func aDocumentThatIsNotCompletedIsFreed() throws {
        expectFreed(try write(Compliance.PDF_1_7, complete: false))
        expectFreed(try write(Compliance.PDF_UA_1, complete: false))
    }
}
