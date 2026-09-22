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
    // The PDFs that are not valid, as the Go fuzz target of the reader found
    // them: each fails with a message or reads what it can, and none reads
    // past the file, allocates what the file does not have or traps.

    // The message that reading the PDF fails with, or "(no error)" when it
    // is read.
    private func readError(_ raw: String) -> String {
        do {
            _ = try TestSupport.read(raw.unicodeScalars.map { UInt8($0.value) })
            return "(no error)"
        } catch let error as PDFjetError {
            return error.message
        } catch {
            return "\(error)"
        }
    }

    @Test func anObjectNumberedHigherThanTheFileHasBytesIsRefused() {
        // One object of every number up to the one it says would take
        // gigabytes of memory for a file of 31 bytes.
        #expect(readError("44444441 0 obj/Filter/Fl streil")
                == "The PDF of 31 bytes cannot hold an object numbered 44444441.")
    }

    @Test func aStreamLongerThanTheFileIsRefused() {
        // The bytes of the stream are counted before it is made, so that a
        // file of a few bytes that says its stream is a gigabyte takes no
        // memory. A stream that has its endstream ends there, whatever its
        // /Length says.
        #expect(readError("1 0 obj<</Length 1000000000>>stream\nx")
                == "The stream of an object is not in the PDF.")
        #expect(readError("1 0 obj<</Length 1000000000>>stream\nx\nendstream endobj")
                == "(no error)")
    }

    @Test func aStreamWhoseLengthIsWrongEndsAtItsEndstream() throws {
        // A /Length that is too short, too long or missing, as pdf.js tests it
        // in issue6108, issue6069 and issue1293r: the stream ends at the end of
        // line before endstream, as MuPDF and pdf.js read it, and not at the
        // /Length, which cut the page's content when it was merged. The
        // /Length is set to it, so that the stream is written whole.
        for dict in ["<< /Length 3 >>", "<< /Length 300 >>", "<< >>", "<< /Length 16 >>"] {
            let objects = try TestSupport.read(pdfWithObjects([dict + "\nstream\nBT (Hello) Tj ET\nendstream"]))
            #expect(TestSupport.latin1(objects[0].getData()) == "BT (Hello) Tj ET", "\(dict)")
            #expect(objects[0].getValue("/Length") == "16", "\(dict)")
        }
        // A /Length that is right is kept, though the stream holds "endstream".
        let objects = try TestSupport.read(pdfWithObjects(["<< /Length 11 >>\nstream\nendstream x\nendstream"]))
        #expect(TestSupport.latin1(objects[0].getData()) == "endstream x")
    }

    @Test func aStreamThatCannotBeDecodedHasNoData() throws {
        // A Flate stream cut short or with a wrong checksum, as pdf.js tests
        // them in comments.pdf and bug1050040: the rest of the PDF is read,
        // and a merge copies the stream as it is. It made the whole PDF
        // unreadable.
        let objects = try TestSupport.read(pdfWithObjects(["<< /Length 5 /Filter /FlateDecode >>\nstream\nabcde\nendstream"]))
        #expect(objects[0].getData().isEmpty)
        #expect(TestSupport.latin1(objects[0].stream ?? []) == "abcde")
        // The objects of an object stream that cannot be decoded cannot be read.
        #expect(readError("1 0 obj<</Type/ObjStm/N 1/First 4/Length 5/Filter/FlateDecode>>stream\nabcde\nendstream endobj")
                == "Invalid zlib header")
    }

    @Test func anObjectNumberedHigherThanTheFileHasBytesIsRead() throws {
        // A PDF cut from a larger document can keep its object numbers, as
        // pdf.js tests it in issue16091: 41 objects numbered up to 156341 in
        // a file of 107,355 bytes, which MuPDF reads.
        let objects = try TestSupport.read(TestSupport.bytes("156337 0 obj<</Type/Catalog>>endobj\n"))
        try #require(objects.count == 156337)
        #expect(objects[156336].getValue("/Type") == "/Catalog")
    }

    @Test func anObjectStreamThatIsNotANumberIsRefused() {
        #expect(readError("1 0 obj<</Type/ObjStm/First x>>stream\n\nendstream endobj")
                == "The object stream of the PDF is malformed: \"x\" is not a number.")
        // An object stream with no stream of its own has no objects.
        #expect(readError("1 0 obj 1 0 obj/Type/ObjStm/First 0") == "(no error)")
    }

    @Test func aReferenceToAnObjectThatIsNotThereHasNoContents() throws {
        // A page whose /Contents names an object the PDF does not have, and
        // one whose dictionary ends where a value belongs.
        let raw = "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n"
                + "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n"
                + "3 0 obj<</Type/Page/Parent 2 0 R/Contents 99 0 R>>endobj\n"
        let objects = try TestSupport.read(raw.unicodeScalars.map { UInt8($0.value) })
        let pages = TestSupport.pageObjects(objects)
        #expect(pages.count == 1)
        #expect(pages[0].getContentObject(objects) == nil)
        #expect(pages[0].getResourcesObject(objects) == nil)
        #expect(pages[0].getValue("/Contents2") == "")
    }

    @Test func aLengthThatIsNotANumberIsAnError() {
        // The /Length is read for every stream, so a PDF that writes anything
        // there, or that ends before it, has to fail with a message: it read
        // past the tokens in the Java, C# and Go ports.
        #expect(readError("1 0 obj<</Length stream\nx\nendstream endobj")
                == "The /Length of a stream is not a number.")
        #expect(readError("1 0 obj<</Length 1 stream")
                == "The stream of an object is not in the PDF.")
        // A /Length that names an object with no length of its own.
        #expect(readError("1 0 obj<</Length 2 0 R>>stream\nx\nendstream endobj\n2 0 obj endobj")
                == "The /Length of a stream is not a number.")
    }

    @Test func aMediaBoxThatIsNotFourNumbersIsLetterSize() throws {
        // The size of a page is read from its /MediaBox, which a PDF that was
        // read can write as anything: it was four tokens past the key, which
        // trapped in Swift and read past the tokens in the other ports.
        for box in ["[0 0 612", "[a b c d]", "5 0 R", "[]", "", "[0 0 0 0]", "[0 0 612 0]"] {
            let objects = try TestSupport.read(pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox " + box + " >>",
            ]))
            let size = TestSupport.pageObjects(objects)[0].getPageSize()
            #expect(size.getWidth() == 612, "\(box)")
            #expect(size.getHeight() == 792, "\(box)")
        }
    }

    @Test func thePageSizeIsTheDistanceBetweenTheCornersOfTheMediaBox() throws {
        // The box is a rectangle of two opposite corners, in either order, and
        // its origin is not always 0 0: the size is what lies between them.
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [9 9 621 801] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [612 792 0 0] >>",
        ]))
        for page in TestSupport.pageObjects(objects) {
            #expect(page.getPageSize().getWidth() == 612)
            #expect(page.getPageSize().getHeight() == 792)
        }
    }

    @Test func aMediaBoxThatIsAnObjectOrRefersToItsNumbersIsRead() throws {
        // A box that is an object of its own, inherited here, and one whose
        // numbers are, as pdf.js tests them in bug852992_reduced and
        // issue7872: the size of both pages was letter size.
        var objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 /MediaBox 5 0 R >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 6 0 R 7 0 R] >>",
            "[0 0 540 190]",
            "250",
            "50",
        ]))
        var pages = TestSupport.pageObjects(objects)
        for (i, want) in [(Float(540), Float(190)), (Float(250), Float(50))].enumerated() {
            let size = pages[i].getPageSize()
            #expect(size.getWidth() == want.0, "page \(i + 1)")
            #expect(size.getHeight() == want.1, "page \(i + 1)")
        }
        #expect(pages[1].getValue("/MediaBox") == "[ 0 0 250 50 ]")

        // A box that refers to the page itself is left as it is, as the fuzz
        // target found it: the page, listed three times, grew each time it was
        // read, to 600 MB.
        objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 3 0 R 3 0 R] /Count 3 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [3 0 R 0 612 792] >>",
        ]))
        pages = TestSupport.pageObjects(objects)
        #expect(pages[2].getValue("/MediaBox") == "[ 3 0 R 0 612 792 ]")
    }

    // The pages of a PDF that a stamp is drawn on, each with a dictionary that
    // ends where a value belongs or that names an object the file does not have.
    private static let brokenPages = [
        "<< /Type /Page /Parent 2 0 R /Resources",
        "<< /Type /Page /Parent 2 0 R /Resources 99 0 R /Contents 99 0 R >>",
        "<< /Type /Page /Parent 2 0 R /Resources << /Font 99 0 R >> /Contents",
        "<< /Type /Page /Parent 2 0 R /Resources << /XObject 99 0 R >> /Contents 4 0 R >>",
        "<< /Type /Page /Parent 2 0 R /Resources << >> /Contents 4 0 R /MediaBox [0 0 612",
        "<< /Type /Page /Parent 2 0 R /Contents [ 4 0 R",
        "<< /Type /Page /Parent 2 0 R /Contents 4",
        "<< /Type /Page /Parent 2 0 R /Resources << /ExtGState 99 0 R >> /Contents 4 0 R >>",
    ]

    @Test func aPageHoldsTheEntriesItInheritsFromThePageTree() throws {
        // /Resources, /MediaBox, /CropBox and /Rotate can be written once on
        // a node above the pages, and a page of another program's PDF often
        // carries none of them: such a page read as letter size whatever its
        // size was, and had no resources, so its fonts and images were not
        // copied. The page that has one of its own keeps it.
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 /MediaBox [0 0 595 842]"
                    + " /Resources << /Font << /F1 5 0 R >> >> /Rotate 90 >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ]))
        let pages = TestSupport.pageObjects(objects)
        #expect(pages[0].getPageSize().getWidth() == 595)
        #expect(pages[0].getPageSize().getHeight() == 842)
        #expect(pages[0].getResourcesObject(objects) != nil)
        #expect(pages[0].getValue("/Rotate") == "90")
        #expect(pages[1].getPageSize().getWidth() == 612)
        // The entries are added once, however often the pages are returned.
        let count = pages[0].getDict().count
        #expect(TestSupport.pageObjects(objects)[0].getDict().count == count)
    }

    @Test func aPageTreeOfThousandsOfKidsIsReadAndMergedInLinearTime() throws {
        // Every page looked for the /CropBox and /Rotate that none of the
        // tree has in its parent, a /Pages node of 10,000 kids in veraPDF's
        // isartor-6-1-12-t01-fail-a, whose /Kids it copied and walked each
        // time: reading its pages and merging them took over a minute. What
        // a node has or inherits is looked for once.
        let count = 4000
        var objects = ["<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [" + (3..<(count + 3)).map { "\($0) 0 R" }.joined(separator: " ")
                        + "] /Count \(count) /Resources << >> >>"]
        for _ in 0..<count {
            objects.append("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>")
        }
        let source = try TestSupport.read(pdfWithObjects(objects))
        let clock = ContinuousClock()
        let elapsed = try clock.measure {
            #expect(TestSupport.pageObjects(source).count == count)
            let memory = MemoryPDF()
            try memory.pdf.merge(source)
            try memory.pdf.complete()
        }
        #expect(elapsed < .seconds(10), "\(elapsed)")
    }

    @Test func aResourcesObjectThatNamesItsOwnPageIsAddedToOnce() throws {
        // The font was added to the page for every "/Resources" left in its
        // dictionary, and adding it grew that dictionary, so a resources
        // object whose /Font names the page itself never ended.
        var objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /Resources 4 0 R /Contents 5 0 R >>",
            "<< /Font 3 0 R >>",
            "<< /Length 5 >>\nstream\nHELLO\nendstream",
        ]))
        let page = TestSupport.pageObjects(objects)[0]
        _ = try page.addResource(CoreFont.HELVETICA, &objects)
        #expect(page.getDict().count <= 32, "\(page.getDict())")
    }

    @Test func aStampOnAPageWhoseDictionaryIsBrokenDrawsNothing() throws {
        // Every one of these crashed a port: the methods that add a font, an
        // image, a content stream or a graphics state to a page that was read
        // indexed its dictionary and the objects of the PDF unchecked.
        for page in PDFTests.brokenPages {
            var objects = try TestSupport.read(pdfWithObjects([
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                page,
                "<< /Length 5 >>\nstream\nHELLO\nendstream",
            ]))
            let pages = TestSupport.pageObjects(objects)
            #expect(pages.count == 1, "\(page)")
            let obj = pages[0]
            _ = try obj.addResource(CoreFont.HELVETICA, &objects)
            var content = Array("BT ET\n".utf8)
            obj.addContent(&content, &objects)
            var prefix = Array("q Q\n".utf8)
            obj.addPrefixContent(&prefix, &objects)
            _ = obj.setGraphicsState(GraphicsState().setAlphaStroking(0.5), &objects)
            // The objects that were read are written as they are.
            let stamped = MemoryPDF()
            try stamped.pdf.addObjects(objects)
            try stamped.pdf.complete()
            // The fonts and the images of the pages are copied into a new PDF.
            let imported = MemoryPDF()
            imported.pdf.addResourceObjects(from: objects)
            _ = Page(imported.pdf, Letter.PORTRAIT)
            try imported.pdf.complete()
        }
    }

    @Test func aDictionaryThatEndsInTheMiddleOfAValueIsClosedThere() throws {
        let raw = "1 0 obj<</Type/Catalog/Kids[3 0 R\n"
        let objects = try TestSupport.read(raw.unicodeScalars.map { UInt8($0.value) })
        #expect(objects.count == 1)
        #expect(objects[0].getValue("/Kids") == "[ 3 0 R ]")
        #expect(objects[0].getValue("/Nothing") == "")
    }

    @Test func thePagesAreTheTreeThatTheCatalogOfTheTrailerNames() throws {
        // A second page tree with no /Parent, before the one that the catalog
        // names, as pdf.js tests it in issue19281 (a tree of one page left from
        // an earlier version) and in xfa_issue13556 (the pages of an XFA form
        // behind a tree of one page): MuPDF and pdf.js find the pages through
        // the trailer's /Root, and the first tree found was taken.
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 4 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
            "<< /Type /Pages /Kids [5 0 R 6 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 4 0 R /MediaBox [0 0 200 200] >>",
            "<< /Type /Page /Parent 4 0 R /MediaBox [0 0 300 300] >>",
        ]))
        var pages = PDF().getPageObjects(from: objects)
        try #require(pages.count == 2)
        #expect("\(pages[0].getNumber()) \(pages[1].getNumber())" == "5 6")
        // A catalog that the trailer does not name, like that of an older
        // version of the document, is not the one whose pages are read.
        let raw = TestSupport.latin1(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R >>",
            "<< /Type /Pages /Kids [5 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 4 0 R >>",
            "<< /Type /Catalog /Pages 4 0 R >>",
        ])).replacingOccurrences(of: "/Root 1 0 R", with: "/Root 6 0 R")
        pages = PDF().getPageObjects(from: try TestSupport.read(Array(raw.utf8)))
        #expect(pages.count == 1 && pages[0].getNumber() == 5, "pages \(pages.count)")
        // Objects with no trailer find the tree that has no /Parent, as before.
        let noTrailer = try TestSupport.read(Array(("1 0 obj<</Type/Pages/Kids[2 0 R]/Count 1>>endobj\n"
                + "2 0 obj<</Type/Page/Parent 1 0 R>>endobj\n").utf8))
        #expect(PDF().getPageObjects(from: noTrailer).count == 1)
    }

    @Test func anEntryForObjectZeroThatIsInUseIsSkipped() throws {
        // Object 0 heads the list of free objects, and a table that marks it in
        // use, as pdf.js tests it in issue10004, is read as MuPDF reads it: the
        // whole table was refused, and the objects were looked for by scanning.
        var raw = TestSupport.latin1(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R >>",
        ])).replacingOccurrences(of: "0000000000 65535 f ", with: "0000000009 00000 n ")
        // The objects follow each other with no white space between them, as
        // there, so that scanning for them does not find them either.
        raw = raw.replacingOccurrences(of: "\nendobj\n", with: "\n endobj")
        #expect(PDF().getPageObjects(from: try TestSupport.read(Array(raw.utf8))).count == 1)
    }

    @Test func theLengthOfAStreamIsTheEntryOfItsDictionary() throws {
        // A /Length that is the value of another entry, "/Height/Length", as
        // pdf.js tests it in issue19611, is not the /Length of the stream.
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /XObject /Height /Length /Length 5 >>\nstream\nabcde\nendstream",
        ]))
        #expect(TestSupport.latin1(objects[0].getData()) == "abcde")
    }

}
