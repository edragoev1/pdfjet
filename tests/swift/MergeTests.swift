/**
 * MergeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Merging the pages of documents that were read.
@Suite struct MergeTests {
    // Returns a document with one page for each text, drawn with Helvetica.
    private func document(_ texts: String...) throws -> [UInt8] {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        for text in texts {
            let page = Page(memory.pdf, Letter.PORTRAIT)
            TextLine(font, text).setLocation(50, 50).drawOn(page)
        }
        try memory.pdf.complete()
        return memory.bytes
    }

    // Returns the decoded content of each page, in the order of the pages.
    private func pageContents(_ objects: [PDFobj]) -> [String] {
        return PDF().getPageObjects(from: objects).map {
            TestSupport.latin1($0.getContentObject(objects)?.getData() ?? [])
        }
    }

    private func isDigits(_ token: String) -> Bool {
        return !token.isEmpty && token.unicodeScalars.allSatisfy { $0.value >= 0x30 && $0.value <= 0x39 }
    }

    // Checks that every reference of the document is to an object it has.
    private func expectReferencesResolve(_ objects: [PDFobj], sourceLocation: SourceLocation = #_sourceLocation) {
        for obj in objects {
            let dict = obj.dict
            var i = 0
            while i + 2 < dict.count {
                if dict[i + 2] == "R" && isDigits(dict[i]) && isDigits(dict[i + 1]) {
                    let number = Int(dict[i])!
                    let resolves = number >= 1 && number <= objects.count && !objects[number - 1].dict.isEmpty
                    #expect(resolves, "object \(obj.number) refers to the missing object \(number)",
                            sourceLocation: sourceLocation)
                }
                i += 1
            }
        }
    }

    private func expectMessage(
            _ message: String,
            sourceLocation: SourceLocation = #_sourceLocation,
            _ body: () throws -> Void) {
        do {
            try body()
            Issue.record("no error was thrown", sourceLocation: sourceLocation)
        } catch {
            #expect(TestSupport.message(error) == message, sourceLocation: sourceLocation)
        }
    }

    @Test func mergesDocumentsInTheirOrder() throws {
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(document("A1", "A2")))
        try memory.pdf.merge(TestSupport.read(document("B1")))
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 3)
        #expect(contents[0].contains(TestSupport.hex("A1")))
        #expect(contents[1].contains(TestSupport.hex("A2")))
        #expect(contents[2].contains(TestSupport.hex("B1")))
        expectReferencesResolve(objects)
    }

    @Test func drawnPagesKeepTheirPlace() throws {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        TextLine(font, "G1").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.merge(TestSupport.read(document("B1")))
        TextLine(font, "G2").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 3)
        #expect(contents[0].contains(TestSupport.hex("G1")))
        #expect(contents[1].contains(TestSupport.hex("B1")))
        #expect(contents[2].contains(TestSupport.hex("G2")))
        expectReferencesResolve(objects)
    }

    @Test func aMergedPageInheritsFromThePageTree() throws {
        let content = "BT /F1 24 Tf 20 300 Td (Inherited) Tj ET"
        let source = "%PDF-1.4\n"
                + "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n"
                + "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 300 400] /Rotate 90"
                + " /Resources << /Font << /F1 4 0 R >> >> >> endobj\n"
                + "3 0 obj << /Type /Page /Parent 2 0 R /Contents 5 0 R >> endobj\n"
                + "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n"
                + "5 0 obj << /Length \(content.utf8.count) >>\nstream\n" + content + "\nendstream\nendobj\n"
                + "trailer << /Root 1 0 R >>\n%%EOF\n"
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(Array(source.utf8)))
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 1)
        let page = pages[0]
        #expect(page.getPageSize().getWidth() == 300)
        #expect(page.getPageSize().getHeight() == 400)
        #expect(page.getValue("/Rotate") == "90")
        #expect(page.dict.contains("/F1"))
        let parent = try #require(page.getObjectNumbers("/Parent").first)
        #expect(objects[parent - 1].getValue("/Type") == "/Pages")
        #expect(pageContents(objects)[0].contains("(Inherited) Tj"))
        expectReferencesResolve(objects)
    }

    @Test func linksPointAtTheMergedPages() throws {
        let source = MemoryPDF()
        let font1 = TestSupport.helvetica(source.pdf)
        let page1 = Page(source.pdf, Letter.PORTRAIT)
        TextLine(font1, "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        let page2 = Page(source.pdf, Letter.PORTRAIT)
        page2.addDestination("there", 30, 100)
        try source.pdf.complete()

        let memory = MemoryPDF()
        TextLine(TestSupport.helvetica(memory.pdf), "Cover").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.merge(TestSupport.read(source.bytes))
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 3)
        let link = try #require(objects.last(where: { $0.getValue("/Subtype") == "/Link" }))
        // The link on the second page points at the third page and is listed by the second.
        let dest = try #require(link.dict.firstIndex(of: "/Dest"))
        #expect(link.dict[dest + 1] == "[")
        #expect(link.dict[dest + 2] == String(pages[2].number))
        #expect(link.dict[dest + 4] == "R")
        #expect(pages[1].getObjectNumbers("/Annots").contains(link.number))
        expectReferencesResolve(objects)
    }

    @Test func mergesTheListedPagesInTheirOrder() throws {
        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(document("A1", "A2", "A3")), [3, 1])
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 2)
        #expect(contents[0].contains(TestSupport.hex("A3")))
        #expect(contents[1].contains(TestSupport.hex("A1")))
        expectReferencesResolve(objects)
    }

    @Test func splitsADocumentIntoOnePDFPerPage() throws {
        let source = try TestSupport.read(document("A1", "A2", "A3"))
        for i in 1...3 {
            let part = MemoryPDF()
            try part.pdf.merge(source, [i])
            try part.pdf.complete()
            let objects = try TestSupport.read(part.bytes)
            let contents = pageContents(objects)
            try #require(contents.count == 1)
            #expect(contents[0].contains(TestSupport.hex("A\(i)")))
            expectReferencesResolve(objects)
        }
    }

    @Test func aLinkToAPageThatIsNotMergedLeadsNowhere() throws {
        let source = MemoryPDF()
        let page1 = Page(source.pdf, Letter.PORTRAIT)
        TextLine(TestSupport.helvetica(source.pdf), "Go").setGoToAction("there").setLocation(50, 50).drawOn(page1)
        let page2 = Page(source.pdf, Letter.PORTRAIT)
        page2.addDestination("there", 30, 100)
        try source.pdf.complete()

        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(source.bytes), [1])
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        #expect(PDF().getPageObjects(from: objects).count == 1)
        let link = try #require(objects.last(where: { $0.getValue("/Subtype") == "/Link" }))
        let dest = try #require(link.dict.firstIndex(of: "/Dest"))
        #expect(link.dict[dest + 1] == "[")
        #expect(link.dict[dest + 2] == "null")
        expectReferencesResolve(objects)
    }

    @Test func anEncryptedDocumentMergesThePagesEncrypted() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.setEncryption(Encryption(memory.pdf, Passwords(), Permissions()))
        try memory.pdf.merge(TestSupport.read(document("Secret A", "Secret B")))
        try memory.pdf.complete()
        #expect(TestSupport.latin1(memory.bytes).contains("/Encrypt "))
        let objects = try TestSupport.read(memory.bytes, "")
        let contents = pageContents(objects)
        try #require(contents.count == 2)
        #expect(contents[0].contains(TestSupport.hex("Secret A")))
        #expect(contents[1].contains(TestSupport.hex("Secret B")))
        expectReferencesResolve(objects)
    }

    @Test func anEncryptedDocumentIsMergedDecrypted() throws {
        let source = MemoryPDF()
        _ = source.pdf.setEncryption(Encryption(source.pdf, Passwords(), Permissions()))
        TextLine(TestSupport.helvetica(source.pdf), "Plain").setLocation(50, 50).drawOn(Page(source.pdf, Letter.PORTRAIT))
        try source.pdf.complete()

        let memory = MemoryPDF()
        try memory.pdf.merge(TestSupport.read(source.bytes, ""))
        try memory.pdf.complete()
        #expect(!TestSupport.latin1(memory.bytes).contains("/Encrypt "))
        let objects = try TestSupport.read(memory.bytes)
        let contents = pageContents(objects)
        try #require(contents.count == 1)
        #expect(contents[0].contains(TestSupport.hex("Plain")))
        expectReferencesResolve(objects)
    }

    @Test func mergesTheTestDocuments() throws {
        let memory = MemoryPDF()
        for name in ["wirth.pdf", "rc65-16e.pdf", "PDFjetLogo.pdf"] {
            try memory.pdf.merge(PDF().read(from: TestSupport.open("data/testPDFs/" + name)))
        }
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let pages = PDF().getPageObjects(from: objects)
        #expect(pages.count == 8)
        for page in pages {
            #expect(page.getContentObject(objects) != nil)
        }
        expectReferencesResolve(objects)
    }

    @Test func mergeIsRefusedWhereItWouldBreakTheDocument() throws {
        let objects = try TestSupport.read(document("A"))

        let ua = MemoryPDF(Compliance.PDF_UA_1)
        expectMessage("Pages of an existing PDF cannot be merged into a PDF/UA or PDF/A document.") {
            try ua.pdf.merge(objects)
        }

        let completed = MemoryPDF()
        _ = Page(completed.pdf, Letter.PORTRAIT)
        try completed.pdf.complete()
        expectMessage("The PDF was already completed.") {
            try completed.pdf.merge(objects)
        }

        let rewritten = MemoryPDF()
        try rewritten.pdf.addObjects(TestSupport.read(document("B")))
        expectMessage("merge and addObjects cannot be used on the same PDF.") {
            try rewritten.pdf.merge(objects)
        }

        let merged = MemoryPDF()
        try merged.pdf.merge(objects)
        let other = try TestSupport.read(document("C"))
        expectMessage("merge and addObjects cannot be used on the same PDF.") {
            try merged.pdf.addObjects(other)
        }

        let empty = MemoryPDF()
        expectMessage("The objects have no root /Pages object.") {
            try empty.pdf.merge([PDFobj]())
        }

        let split = MemoryPDF()
        expectMessage("The document has no page 0.") {
            try split.pdf.merge(objects, [0])
        }
        expectMessage("The document has no page 2.") {
            try split.pdf.merge(objects, [2])
        }
        expectMessage("Page 1 is listed twice.") {
            try split.pdf.merge(objects, [1, 1])
        }
    }
}
