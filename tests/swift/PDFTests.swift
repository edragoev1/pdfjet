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
        // The table holds byte offsets, and startxref is the byte offset of the
        // table, so the entries are found from it: a String index counts
        // characters, which are not bytes once the file holds a byte above 127.
        let startxref = try #require(raw.firstMatch(of: /startxref\n(\d+)\n%%EOF\n$/))
        let xref = Int(startxref.1)!
        let tableHeader = Array("xref\n0 \(count)\n".utf8)
        #expect(Array(bytes[xref..<(xref + tableHeader.count)]) == tableHeader)
        let entries = xref + tableHeader.count
        for number in 1..<count {
            let entry = TestSupport.latin1(Array(bytes[(entries + 20 * number)..<(entries + 20 * number + 20)]))
            if Array(entry)[17] == "n" {
                let offset = Int(entry.prefix(10))!
                let object = TestSupport.latin1(Array(bytes[offset..<min(bytes.count, offset + 20)]))
                #expect(object.hasPrefix("\(number) 0 obj"), "object \(number) at \(offset)")
            }
        }
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

    // A PDF with one stream whose data starts with a line feed, the byte that
    // ends the stream keyword. Every stream of an encrypted PDF starts with a
    // random IV, so one in 256 of them starts that way.
    private func pdfWithStream(_ data: String) -> [UInt8] {
        let o1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
        let o2 = "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n"
        let o3 = "3 0 obj\n<< /Length \(data.utf8.count) >>\nstream\n" + data + "\nendstream\nendobj\n"
        let header = "%PDF-1.4\n"
        let off1 = header.utf8.count
        let off2 = off1 + o1.utf8.count
        let off3 = off2 + o2.utf8.count
        let xref = off3 + o3.utf8.count
        func entry(_ offset: Int) -> String {
            return String(format: "%010d 00000 n \n", offset)
        }
        let body = header + o1 + o2 + o3
                + "xref\n0 4\n0000000000 65535 f \n" + entry(off1) + entry(off2) + entry(off3)
                + "trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n\(xref)\n%%EOF\n"
        return Array(body.utf8)
    }

    @Test func aStreamThatStartsWithALineFeedKeepsIt() throws {
        for obj in try TestSupport.read(pdfWithStream("\nHELLO")) where obj.getNumber() == 3 {
            #expect(TestSupport.latin1(obj.getData()) == "\nHELLO")
            return
        }
        Issue.record("object 3 not read")
    }

    @Test func anEmptyDocumentPropertyIsNotWritten() throws {
        let memory = MemoryPDF()
        _ = memory.pdf.setTitle("").setAuthor("").setSubject("").setKeywords("").setCreator("")
        _ = Page(memory.pdf, Letter.PORTRAIT)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        for key in ["/Title", "/Author", "/Subject", "/Keywords", "/Creator"] {
            #expect(!raw.contains(key), "\(key)")
        }
    }

    @Test func aShapeWithoutADescriptionWritesNoAltText() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        Line(10, 10, 100, 10).drawOn(page)
        Line(10, 20, 100, 20).setAltDescription("A rule").drawOn(page)
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/Alt <").count - 1 == 1)
        #expect(!raw.contains("/ActualText"))
    }

    @Test func pointsAndTwoDimensionalBarcodesAreArtifacts() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let drawables: [Drawable] = [
                Point(50, 50),
                try QRCode("https://pdfjet.com", ErrorCorrectionLevel.M),
                DataMatrix("PDFjet")]
        for drawable in drawables {
            let page = Page(memory.pdf, Letter.PORTRAIT)
            _ = drawable.drawOn(page)
            let content = TestSupport.content(page)
            #expect(content.hasPrefix("/Artifact BMC\n"), "\(content)")
            #expect(content.hasSuffix("EMC\n"), "\(content)")
        }
    }

    private static let streamFont = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"

    // The numbers of the page objects, in page order.
    private func pageNumbers(_ pdf: [UInt8]) throws -> [String] {
        return TestSupport.pageObjects(try TestSupport.read(pdf)).map { String($0.getNumber()) }
    }

    // The first group of every match of the pattern.
    private func matches(_ pattern: String, _ text: String) throws -> [String] {
        let regex = try NSRegularExpression(pattern: pattern)
        let string = text as NSString
        return regex.matches(in: text, range: NSRange(location: 0, length: string.length)).map {
            string.substring(with: $0.range(at: 1))
        }
    }

    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func aLinkIsInALinkElementAndAnyOtherAnnotationInAnAnnotElement() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let pdf = memory.pdf
        _ = pdf.setTitle("Title")
        let font = try Font(pdf, TestSupport.open(PDFTests.streamFont))
        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "PDFjet").setURIAction("https://pdfjet.com").setLocation(70, 80).drawOn(page)
        let note = TextAnnotation()
        note.setLocation(70, 100)
        note.setContents("A note")
        _ = note.drawOn(page)
        try pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.components(separatedBy: "/S /Link\n").count - 1 == 1)
        #expect(raw.components(separatedBy: "/S /Annot\n").count - 1 == 1)
    }

    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func aDetachedPageThatIsNeverAddedLeavesNoTrace() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let pdf = memory.pdf
        _ = pdf.setTitle("Title")
        let font = try Font(pdf, TestSupport.open(PDFTests.streamFont))
        // A dry run, like one that measures the text, on a page that is never added.
        let dry = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        TextLine(font, "PDFjet").setURIAction("https://pdfjet.com").setLocation(70, 80).drawOn(dry)
        let page1 = Page(pdf, Letter.PORTRAIT)
        TextLine(font, "Go to page 2").setGoToAction("dest2").setLocation(70, 80).drawOn(page1)
        let page2 = Page(pdf, Letter.PORTRAIT)
        _ = page2.addDestination("dest2", 100)
        try pdf.complete()

        let raw = TestSupport.latin1(memory.bytes)
        #expect(!raw.contains("/Pg 0 0 R"))
        #expect(raw.components(separatedBy: "/Type /Annot\n").count - 1 == 1)
        // The link leads to the second page, not to the object before it.
        let dest = try matches("/Dest \\[(\\d+) 0 R", raw)
        #expect(try dest.first == pageNumbers(memory.bytes)[1])
    }

    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func theStructureTreeFollowsThePagesNotTheOrderTheyWereDrawnIn() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        let pdf = memory.pdf
        _ = pdf.setTitle("Title")
        let font = try Font(pdf, TestSupport.open(PDFTests.streamFont))
        let second = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        TextLine(font, "Second").setLocation(70, 80).drawOn(second)
        let first = Page(pdf, Letter.PORTRAIT, Page.DETACHED)
        TextLine(font, "First").setLocation(70, 80).drawOn(first)
        pdf.addPage(first)
        pdf.addPage(second)
        try pdf.complete()

        // The structure elements are written, and listed by the document
        // element, in the order of their pages.
        let pages = try pageNumbers(memory.bytes)
        let pg = try matches("/Pg (\\d+) 0 R", TestSupport.latin1(memory.bytes))
        #expect(Array(pg.prefix(2)) == pages)
    }

    @Test func textStringsAreUtf16SoThatEveryReaderDecodesThem() throws {
        let memory = MemoryPDF(Compliance.PDF_UA_1)
        _ = memory.pdf.setTitle("Title")
        let page = Page(memory.pdf, Letter.PORTRAIT)
        Line(10, 20, 100, 20).setAltDescription("Gr\u{fc}\u{df}e \u{2013} \u{7dda}").drawOn(page)
        try memory.pdf.complete()
        let element = try #require(TestSupport.findObject(try TestSupport.read(memory.bytes), "/Alt"))
        #expect(element.getValue("/Alt").lowercased().hasPrefix("<feff"))
        #expect(TestSupport.utf16Hex(element.getValue("/Alt")) == "Gr\u{fc}\u{df}e \u{2013} \u{7dda}")
    }

    private func pdfWithObjects(_ objects: [String]) -> [UInt8] {
        var body = "%PDF-1.4\n"
        var offsets = [Int]()
        for (i, object) in objects.enumerated() {
            offsets.append(body.utf8.count)
            body += "\(i + 1) 0 obj\n" + object + "\nendobj\n"
        }
        let xref = body.utf8.count
        body += "xref\n0 \(objects.count + 1)\n0000000000 65535 f \n"
        for offset in offsets {
            body += String(format: "%010d 00000 n \n", offset)
        }
        body += "trailer\n<< /Size \(objects.count + 1) /Root 1 0 R >>\nstartxref\n\(xref)\n%%EOF\n"
        return TestSupport.bytes(body)
    }

    // A PDF whose font has its widths and its encoding in objects of their own,
    // as the PDFs that Word makes do.
    @Test func aFontIsImportedWithTheObjectsItRefersTo() throws {
        let source = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]"
                    + " /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            "<< /Length 32 >>\nstream\nBT /F1 24 Tf 72 700 Td (H) Tj ET\nendstream",
            "<< /Type /Font /Subtype /TrueType /BaseFont /Helvetica /FirstChar 72 /LastChar 72"
                    + " /Widths 6 0 R /Encoding 7 0 R >>",
            "[ 722 ]",
            "<< /Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [ 72 /H ] >>",
        ]))
        let memory = MemoryPDF()
        memory.pdf.addResourceObjects(from: source)
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let content = try #require(memory.pdf.getPageObjects(from: source)[0].getContentObject(source))
        page.drawContents(content.getData(), 792, 0, 0, 1, 1)
        try memory.pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        #expect(objects[4].getValue("/Type") == "/Font")
        #expect(objects[5].getDict().contains("722"), "\(objects[5].getDict())")
        #expect(objects[6].getValue("/Type") == "/Encoding")
    }

    @Test func aPageTreeThatLoopsIsReadOnce() throws {
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R 99 0 R] /Count 1 >>",
            "<< /Type /Pages /Parent 2 0 R /Kids [3 0 R 2 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        ]))
        #expect(TestSupport.pageObjects(objects).count == 1)
        let memory = MemoryPDF()
        try memory.pdf.merge(objects)
        try memory.pdf.complete()
    }

    @Test func objectsWithoutAPageTreeHaveNoPages() throws {
        let objects = try TestSupport.read(pdfWithObjects(["<< /Type /Catalog >>"]))
        #expect(TestSupport.pageObjects(objects).isEmpty)
    }

    // Java throws an exception for a malformed object stream; Swift used to trap.
    @Test func aMalformedObjectStreamIsAnErrorAndNotATrap() {
        for dict in ["/Type /ObjStm /N 1", "/Type /ObjStm /N 1 /First 4", "/Type /ObjStm /N 1 /First 900"] {
            let data = "4 x << /A 1 >>"
            let pdf = pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [] /Count 0 >>",
                "<< \(dict) /Length \(data.utf8.count) >>\nstream\n\(data)\nendstream",
            ])
            #expect(throws: PDFjetError.self) { try TestSupport.read(pdf) }
        }
    }

    @Test func theNameOfAnEmbeddedFileIsATextStringInFAndUF() throws {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let file = try EmbeddedFile(memory.pdf, "\u{dc}bersicht \u{2013} r\u{e9}sum\u{e9}.txt",
                InputStream(data: Data("Hello".utf8)), false)
        FileAttachment(file).setLocation(100, 100).drawOn(page)
        try memory.pdf.complete()
        let spec = try #require(TestSupport.findObject(try TestSupport.read(memory.bytes), "/UF"))
        #expect(spec.getValue("/Type") == "/Filespec")
        #expect(TestSupport.utf16Hex(spec.getValue("/UF")) == "\u{dc}bersicht \u{2013} r\u{e9}sum\u{e9}.txt")
        #expect(spec.getValue("/F") == spec.getValue("/UF"))
    }
}
