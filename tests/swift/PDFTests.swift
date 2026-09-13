/**
 * PDFTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Writing a document and reading it back.
@Suite struct PDFTests {
    private func document(_ sizes: PageSize...) throws -> [UInt8] {
        let memory = MemoryPDF()
        let font = TestSupport.helvetica(memory.pdf)
        for size in sizes {
            let page = Page(memory.pdf, size)
            TextLine(font, "Page").setLocation(50, 50).drawOn(page)
        }
        try memory.pdf.complete()
        return memory.bytes
    }

    /// An output stream that remembers whether it was closed.
    private final class ClosingStream: OutputStream {
        var closed = false
        override func close() {
            closed = true
            super.close()
        }
    }

    @Test func startsWithTheHeaderAndEndsWithEof() throws {
        let raw = TestSupport.latin1(try document(Letter.PORTRAIT))
        #expect(raw.hasPrefix("%PDF-1.7\n%"))
        #expect(raw.hasSuffix("%%EOF\n"))
    }

    @Test func theCrossReferenceTablePointsAtEveryObject() throws {
        let bytes = try document(Letter.PORTRAIT, A4.PORTRAIT)
        let raw = TestSupport.latin1(bytes)
        let header = try #require(raw.firstMatch(of: /xref\n0 (\d+)\n/))
        let count = Int(header.1)!
        let entries = raw.utf8.distance(from: raw.startIndex, to: header.range.upperBound)
        for number in 1..<count {
            let entry = TestSupport.latin1(Array(bytes[(entries + 20 * number)..<(entries + 20 * number + 20)]))
            if Array(entry)[17] == "n" {
                let offset = Int(entry.prefix(10))!
                let object = TestSupport.latin1(Array(bytes[offset..<min(bytes.count, offset + 20)]))
                #expect(object.hasPrefix("\(number) 0 obj"), "object \(number) at \(offset)")
            }
        }
        let startxref = try #require(raw.firstMatch(of: /startxref\n(\d+)\n%%EOF\n$/))
        let xref = Int(startxref.1)!
        #expect(TestSupport.latin1(Array(bytes[xref..<(xref + 5)])) == "xref\n")
    }

    @Test func documentIdsAreRandomAndDifferent() throws {
        var ids = Set<String>()
        for _ in 0..<100 {
            let memory = MemoryPDF()
            _ = Page(memory.pdf, A4.PORTRAIT)
            try memory.pdf.complete()
            let id = TestSupport.trailerID(memory.bytes)
            #expect(id.wholeMatch(of: /[0-9a-f]{32}/) != nil, "\(id)")
            ids.insert(id)
        }
        #expect(ids.count == 100)
    }

    @Test func theXmpDocumentIdIsTheTrailerId() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let id = TestSupport.trailerID(memory.bytes)
        #expect(TestSupport.latin1(memory.bytes).contains("<xapMM:DocumentID>uuid:" + id + "</xapMM:DocumentID>"))
    }

    @Test func theInfoDictionaryHasTheTitleInUtf16() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.setTitle("Grüße (x)").setAuthor("Author")
        TextLine(TestSupport.helvetica(memory.pdf), "x").setLocation(10, 10).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let info = try #require(TestSupport.findObject(try TestSupport.read(memory.bytes), "/Producer"))
        #expect(TestSupport.utf16Hex(info.getValue("/Title")) == "Grüße (x)")
        #expect(TestSupport.utf16Hex(info.getValue("/Author")) == "Author")
    }

    @Test func pageSizesSurviveReadingBack() throws {
        let objects = try TestSupport.read(try document(Letter.PORTRAIT, A4.LANDSCAPE, Letter.PORTRAIT))
        let pages = TestSupport.pageObjects(objects)
        #expect(pages.count == 3)
        guard pages.count == 3 else { return }
        #expect(pages[0].getPageSize().getWidth() == 612)
        #expect(pages[1].getPageSize().getWidth() == 842)
        #expect(pages[1].getPageSize().getHeight() == 595)
        #expect(pages[2].getPageSize().getHeight() == 792)
    }

    @Test func readsAPdfWhoseCrossReferenceOffsetIsWrong() throws {
        let raw = TestSupport.latin1(try document(Letter.PORTRAIT, Letter.PORTRAIT))
        let damaged = raw.replacing(/startxref\n\d+\n/, with: "startxref\n12\n", maxReplacements: 1)
        let bytes = damaged.unicodeScalars.map { UInt8($0.value) }
        #expect(TestSupport.pageObjects(try TestSupport.read(bytes)).count == 2)
    }

    @Test func crossReferenceOffsetsAreTenDigits() throws {
        #expect(try PDF.xrefOffset(17) == "0000000017")
        #expect(try PDF.xrefOffset(9999999999) == "9999999999")
        let error = #expect(throws: (any Error).self) { _ = try PDF.xrefOffset(10000000000) }
        #expect(TestSupport.message(error)
                == "The PDF is too large for a cross-reference table: an object starts at byte 10000000000.")
    }

    @Test func completeClosesTheStream() throws {
        let stream = ClosingStream(toMemory: ())
        let pdf = PDF(stream)
        _ = Page(pdf, Letter.PORTRAIT)
        try pdf.complete()
        #expect(stream.closed)
    }

    @Test func readsAPdfWithABlankPage() throws {
        let memory = MemoryPDF()
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        #expect(TestSupport.pageObjects(try TestSupport.read(memory.bytes)).count == 1)
    }
}
