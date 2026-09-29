/**
 * ReviewReaderTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Reading PDFs that are made to be slow to read, or to break what is made of
/// them. The time limits are generous: each of these took from five seconds to
/// more than a minute, or overflowed the stack, and takes a time in proportion
/// to its size now.
@Suite struct ReviewReaderTests {
    private let earlier = "The PDF was not completed because of an earlier error: "

    /// Reads the PDF, which must take less than twenty seconds, and returns its
    /// objects, or nil when it cannot be read. The limit is four times that of
    /// the other ports, as the tests run in a debug build, and in parallel: the
    /// streams of theLengthOfAStreamIsFoundByItsNumber take seven seconds then.
    private func readQuickly(_ pdf: [UInt8], sourceLocation: SourceLocation = #_sourceLocation) -> [PDFobj]? {
        let start = Date()
        let objects = try? TestSupport.read(pdf)
        let elapsed = Date().timeIntervalSince(start)
        #expect(elapsed < 20.0, "reading \(pdf.count) bytes took \(elapsed) s", sourceLocation: sourceLocation)
        return objects
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

    // The message that reading the PDF fails with, or "(no error)" when it is
    // read.
    private func readError(_ raw: String) -> String {
        do {
            _ = try TestSupport.read(TestSupport.bytes(raw))
            return "(no error)"
        } catch {
            return TestSupport.message(error)
        }
    }

    // Returns the message that complete() throws, or "" when it completes the PDF.
    private func completeMessage(_ pdf: PDF) -> String {
        do {
            try pdf.complete()
            return ""
        } catch {
            return TestSupport.message(error)
        }
    }

    // Returns the message that the body throws, or "" when it throws none.
    private func thrown(_ body: () throws -> Void) -> String {
        do {
            try body()
            return ""
        } catch {
            return TestSupport.message(error)
        }
    }

    // Returns the objects of a small PDF of one page, as read returns them.
    private func existingObjects() throws -> [PDFobj] {
        let doc = MemoryPDF()
        let font = TestSupport.helvetica(doc.pdf)
        TextLine(font, "Existing").setLocation(50, 50).drawOn(Page(doc.pdf, Letter.PORTRAIT))
        try doc.pdf.complete()
        return try TestSupport.read(doc.bytes)
    }

    // Returns a PDF with no cross-reference table, of a catalog, a page tree
    // of one page and then count objects "<< /A i >>", with or without their
    // endobj.
    private func numberedObjects(_ count: Int, _ endobj: Bool) -> [UInt8] {
        var objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        ]
        for i in 0..<count {
            objects.append("<< /A \(i) >>")
        }
        var body = "%PDF-1.7\n"
        for (i, object) in objects.enumerated() {
            body += "\(i + 1) 0 obj\n" + object + "\n"
            if endobj {
                body += "endobj\n"
            }
        }
        body += "trailer\n<< /Size \(objects.count + 1) /Root 1 0 R >>\n"
        return TestSupport.bytes(body)
    }

    @Test func aRunOfWhiteSpaceIsScannedOnce() {
        // Every space looked at all the spaces after it for a number.
        _ = readQuickly(TestSupport.bytes("%PDF-1.7\n" + String(repeating: " ", count: 400000)))
        _ = readQuickly(TestSupport.bytes("%PDF-1.7\n1" + String(repeating: "\n", count: 400000)))
    }

    @Test func objectsWithNoEndobjAreReadOnce() throws {
        // Every object was read to the end of the PDF.
        _ = readQuickly(TestSupport.bytes(String(repeating: "1 0 obj\n", count: 31000)))

        // Each object ends where the next one starts.
        let objects = try #require(readQuickly(numberedObjects(20000, false)))
        #expect(objects.count == 20003)
        #expect(objects[10].getValue("/A") == "7")
        #expect(objects[1].getValue("/Kids") == "[ 3 0 R ]")
        #expect(TestSupport.pageObjects(objects).count == 1)
    }

    @Test func objectsWithNoEndobjThatTheTableListsAreReadOnce() throws {
        // Every object that the cross-reference table lists was read to the
        // end of the PDF.
        var objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        ]
        for i in 0..<20000 {
            objects.append("<< /A \(i) >>")
        }
        var body = "%PDF-1.7\n"
        var offsets = [Int]()
        for (i, object) in objects.enumerated() {
            offsets.append(body.utf8.count)
            body += "\(i + 1) 0 obj\n" + object + "\n"
        }
        let xref = body.utf8.count
        body += "xref\n0 \(objects.count + 1)\n0000000000 65535 f \n"
        for offset in offsets {
            body += String(format: "%010d 00000 n \n", offset)
        }
        body += "trailer\n<< /Size \(objects.count + 1) /Root 1 0 R >>\nstartxref\n\(xref)\n%%EOF\n"
        let read = try #require(readQuickly(TestSupport.bytes(body)))
        #expect(read.count == 20003)
        #expect(read[10].getValue("/A") == "7")
        // The object is "11 0 obj << /A 7 >>", without the objects after it.
        #expect(read[10].dict.joined(separator: " ") == "11 0 obj << /A 7 >>")
    }

    @Test func aCrossReferenceSectionThatIsItsOwnPrevIsReadOnce() throws {
        // The section of a big table was read a thousand times: its /Prev is
        // itself. The PDF is then read by looking for its objects.
        let pdf = TestSupport.latin1(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        ]))
        let xref = pdf.utf8.count - pdf[pdf.range(of: "xref")!.lowerBound...].utf8.count
        var body = String(pdf.prefix(xref))
        body += "xref\n0 4\n0000000000 65535 f \n"
        for i in 1...3 {
            let offset = pdf.utf8.count - pdf[pdf.range(of: "\(i) 0 obj")!.lowerBound...].utf8.count
            body += String(format: "%010d 00000 n \n", offset)
        }
        // Free entries, which make the table big.
        body += String(repeating: "0000000000 65535 f \n", count: 100000)
        body += "trailer\n<< /Size 4 /Root 1 0 R /Prev \(xref) >>\nstartxref\n\(xref)\n%%EOF\n"
        let objects = try #require(readQuickly(TestSupport.bytes(body)))
        #expect(TestSupport.pageObjects(objects).count == 1)
    }

    // Returns a PDF of count streams whose /Length is 1 and that have no
    // endstream, each followed by 1,000 bytes and its endobj, which the
    // cross-reference table lists.
    private func streamsWithNoEndstream(_ count: Int) -> [UInt8] {
        var body = TestSupport.bytes("%PDF-1.7\n")
        let filler = [UInt8](repeating: 0x78, count: 1000)    // "x"
        var offsets = [Int]()
        for i in 1...count {
            offsets.append(body.count)
            body += TestSupport.bytes("\(i) 0 obj\n<< /Length 1 >>\nstream\n")
            body += filler
            body += TestSupport.bytes("\nendobj\n")
        }
        let xref = body.count
        body += TestSupport.bytes("xref\n0 \(count + 1)\n0000000000 65535 f \n")
        for offset in offsets {
            body += TestSupport.bytes(String(format: "%010d 00000 n \n", offset))
        }
        body += TestSupport.bytes("trailer\n<< /Size \(count + 1) /Root 1 0 R >>\nstartxref\n\(xref)\n%%EOF\n")
        return body
    }

    @Test func streamsWithAWrongLengthAndNoEndstreamAreSearchedOnce() throws {
        // The endstream of every stream was looked for to the end of the PDF:
        // 8,000 streams, 8.5 MB, took 25 seconds. It is looked for in the
        // object.
        let objects = try #require(readQuickly(streamsWithNoEndstream(8000)))
        #expect(objects.count == 8000)
        #expect(objects[7999].getValue("/Length") == "1")
        #expect(objects[7999].stream == TestSupport.bytes("x"))
    }

    @Test func aStreamThatTheTableListsManyTimesIsSearchedWithinABudget() throws {
        // Each entry of the stream is an object of its own, which ends at the
        // end of the PDF, and its endstream was looked for there 10,000 times.
        // What the reader may read in all is eight times the length of the
        // PDF: the streams past it keep their /Length.
        var body = TestSupport.bytes("%PDF-1.7\n1 0 obj\n<< /Length 1 >>\nstream\n")
        body += [UInt8](repeating: 0x78, count: 1000000)  // "x"
        let xref = body.count
        body += TestSupport.bytes("\nxref\n" + String(repeating: "1 1\n0000000009 00000 n \n", count: 10000))
        body += TestSupport.bytes("trailer\n<< /Size 2 >>\nstartxref\n\(xref + 1)\n%%EOF\n")
        let objects = try #require(readQuickly(body))
        #expect(objects.count == 1)
        #expect(objects[0].getValue("/Length") == "1")
    }

    @Test func sectionsWithNoStartxrefAreReadWithinABudget() {
        // A thousand sections chained by /Prev, with no startxref after them,
        // were each read to the end of the PDF. The sections past the budget
        // are not read, and the PDF is read by looking for its objects.
        var body = TestSupport.bytes("%PDF-1.7\n")
        var prev = -1
        for _ in 0..<1000 {
            let offset = body.count
            if prev == -1 {
                body += TestSupport.bytes("xref\n0 0\ntrailer\n<< /Size 1 >>\n")
            } else {
                body += TestSupport.bytes("xref\n0 0\ntrailer\n<< /Size 1 /Prev \(prev) >>\n")
            }
            prev = offset
        }
        body += TestSupport.bytes(String(repeating: "a ", count: 500000))
        body += TestSupport.bytes("\nstartxref\n\(prev)\n%%EOF\n")
        _ = readQuickly(body)
    }

    @Test func anObjectThatTheTableListsManyTimesIsReadWithinABudget() throws {
        // The object with no endobj was read to the end of the PDF for each of
        // its 1,000 entries.
        var body = TestSupport.bytes("%PDF-1.7\n1 0 obj\n<< /A [")
        body += TestSupport.bytes(String(repeating: "1 ", count: 500000))
        body += TestSupport.bytes("] >>\n")
        let xref = body.count
        body += TestSupport.bytes("xref\n" + String(repeating: "1 1\n0000000009 00000 n \n", count: 1000))
        body += TestSupport.bytes("trailer\n<< /Size 2 >>\nstartxref\n\(xref)\n%%EOF\n")
        let objects = try #require(readQuickly(body))
        #expect(objects.count == 1)
        #expect(objects[0].dict.prefix(7).joined(separator: " ") == "1 0 obj << /A [ 1")
    }

    @Test func anObjectThatAnObjectStreamListsManyTimesIsReadWithinABudget() throws {
        // The objects of an object stream at offsets 0 and 1,000,000 by turns
        // were each read from 0 to 1,000,000, the next offset. Its objects are
        // read in no more than its length in all, and those past it have no
        // tokens.
        var header = ""
        for i in 0..<1000 {
            header += "\(i + 2) \((i % 2) * 1000000) "
        }
        let data = header + String(repeating: "1 ", count: 500000) + "<< >>"
        let objects = try #require(readQuickly(pdfWithObjects([
            "<< /Type /ObjStm /N 1000 /First \(header.utf8.count) /Length \(data.utf8.count) >>\nstream\n"
                + data + "\nendstream",
        ])))
        #expect(objects.count == 1001)
        #expect(objects[1].dict.prefix(5).joined(separator: " ") == "2 0 obj 1 1")
        #expect(objects[1000].dict.joined(separator: " ") == "1001 0 obj")
    }

    @Test func theEndstreamOfAStreamIsLookedForInItsObject() throws {
        // A stream with a wrong /Length ends at the endstream in its object,
        // and one with none keeps its /Length: the endstream of the next
        // object, which the search found, made it the bytes up to there.
        var objects = try TestSupport.read(pdfWithObjects([
            "<< /Length 3 >>\nstream\nBT (Hello) Tj ET\nendstream",
            "<< /Length 5 >>\nstream\nhello world",
            "<< /Length 3 >>\nstream\nabc\nendstream",
        ]))
        #expect(TestSupport.latin1(objects[0].getData()) == "BT (Hello) Tj ET")
        #expect(objects[0].getValue("/Length") == "16")
        #expect(TestSupport.latin1(objects[1].getData()) == "hello")
        #expect(objects[1].getValue("/Length") == "5")
        #expect(TestSupport.latin1(objects[2].getData()) == "abc")

        // A stream whose /Length is right goes on past where its object ends
        // at the latest, which is the next "number generation obj" in a PDF
        // with no cross-reference table, and past an endstream in it, and the
        // object in it is not read.
        let data = "x\n2 0 obj\nendstream y"
        objects = try TestSupport.read(TestSupport.bytes(
            "%PDF-1.4\n1 0 obj\n<< /Length \(data.utf8.count) >>\nstream\n\(data)\nendstream\nendobj\n"))
        #expect(objects.count == 1)
        #expect(TestSupport.latin1(objects[0].getData()) == data)
    }

    @Test func theLengthOfAStreamIsFoundByItsNumber() throws {
        // Every stream looked for its /Length among all the objects.
        var objects = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
        ]
        for _ in 0..<60000 {
            objects.append("<< /Length \(objects.count + 2) 0 R >>\nstream\nq Q\nendstream")
            objects.append("3")
        }
        let read = try #require(readQuickly(pdfWithObjects(objects)))
        #expect(read.count == 120003)
        #expect(TestSupport.latin1(read[119999].getData()) == "q Q")
    }

    @Test func theLengthOfAStreamIsItsNewestVersion() throws {
        // The /Length of the stream is updated from 2 to 15 at the end of the
        // PDF, and the stream holds the keyword that the first length ends at.
        let pdf = TestSupport.latin1(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>",
            "<< /Length 5 0 R >>\nstream\nAB endstream CD\nendstream",
            "2",
        ]))
        let prev = pdf.utf8.count - pdf[pdf.range(of: "xref")!.lowerBound...].utf8.count
        var body = pdf
        let offset = body.utf8.count
        body += "5 0 obj\n15\nendobj\n"
        let xref = body.utf8.count
        body += String(format: "xref\n0 1\n0000000000 65535 f \n5 1\n%010d 00000 n \n", offset)
        body += "trailer\n<< /Size 6 /Root 1 0 R /Prev \(prev) >>\nstartxref\n\(xref)\n%%EOF\n"
        let objects = try TestSupport.read(TestSupport.bytes(body))
        #expect(TestSupport.latin1(objects[3].getData()) == "AB endstream CD")
    }

    // Returns a PDF whose page uses a form XObject that refers to the next
    // object, which refers to the next, count times, and whose page tree has
    // count nodes, one under the other.
    private func chainOfObjects(_ count: Int) -> [UInt8] {
        var objects = [
            "",     // The catalog, below
            "<< /Type /Page /MediaBox [0 0 612 792] /Resources << /XObject << /X0 3 0 R >> >> >>",
        ]
        for _ in 0..<count {    // Objects 3 and on
            objects.append("<< /Next \(objects.count + 2) 0 R >>")
        }
        objects.append("<< >>")
        let first = objects.count + 1
        for i in 0..<count {
            let kid = (i == count - 1) ? 2 : objects.count + 2     // The last is the page.
            objects.append("<< /Type /Pages /Kids [\(kid) 0 R] /Count 1 >>")
        }
        objects[0] = "<< /Type /Catalog /Pages \(first) 0 R >>"
        return pdfWithObjects(objects)
    }

    @Test func longChainsOfObjectsAreFollowedWithoutRecursion() throws {
        // They overflowed the stack.
        let count = 100000
        let objects = try TestSupport.read(chainOfObjects(count))
        let doc = MemoryPDF()
        #expect(doc.pdf.getPageObjects(from: objects).count == 1)
        doc.pdf.addResourceObjects(from: objects)
        _ = Page(doc.pdf, Letter.PORTRAIT)
        try doc.pdf.complete()
        let written = try TestSupport.read(doc.bytes)
        #expect(PDF.valueOf(written[count + 2]) == ["<<", ">>"])
    }

    @Test func addObjectsIsRefusedWhereItWouldLosePages() throws {
        let objects = try existingObjects()
        let pdf = TestSupport.newPDF()
        _ = Page(pdf, Letter.PORTRAIT)
        #expect(thrown { try pdf.addObjects(objects) }
                == "The objects of an existing PDF cannot be added to a PDF that has pages of its own.")

        // A page after the objects is not in their page tree.
        let pdf2 = TestSupport.newPDF()
        try pdf2.addObjects(try existingObjects())
        _ = Page(pdf2, Letter.PORTRAIT)
        #expect(completeMessage(pdf2) == earlier
                + "A page cannot be added to a PDF that addObjects added the objects of an existing PDF to.")

        // The pages were not made for the compliance of the document.
        let pdf3 = MemoryPDF(Compliance.PDF_UA_1).pdf
        #expect(thrown { try pdf3.addObjects(objects) }
                == "The objects of an existing PDF cannot be added to a PDF/UA or PDF/A document.")
    }

    @Test func aNumberWithNoObjectIsAFreeEntry() throws {
        // Object 4 is not in the PDF, and was written as "4 0 obj endobj".
        var objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>",
            "<< /Removed true >>",
            "<< /Kept true >>",
        ]))
        let empty = PDFobj()    // As read gives a number with no object
        empty.number = 4
        objects[3] = empty
        let doc = MemoryPDF()
        try doc.pdf.addObjects(objects)
        try doc.pdf.complete()
        let raw = TestSupport.latin1(doc.bytes)
        #expect(!raw.contains("\n4 0 obj"), "an empty object is written")
        let entries = raw[raw.range(of: "\nxref\n", options: .backwards)!.lowerBound...]
                .split(separator: "\n", omittingEmptySubsequences: false)
        #expect(entries[7] == "0000000000 65535 f ")   // Object 4
        #expect(try TestSupport.read(doc.bytes)[4].getValue("/Kept") == "true")
    }

    @Test func twoPagesThatNameDifferentResourcesAlikeAreRefused() throws {
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 6 0 R >> >> >>",
            "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
            "<< /Subtype /Form /BBox [0 0 20 20] /Length 0 >>\nstream\n\nendstream",
        ]))
        let pdf = TestSupport.newPDF()
        pdf.addResourceObjects(from: objects)
        #expect(completeMessage(pdf) == earlier + "The pages of the PDF use the name /X0 for different resources, "
                + "and the pages of this document share one resources dictionary.")

        // Pages that use the same resource under the same name share it.
        let shared = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /XObject << /X0 5 0 R >> >> >>",
            "<< /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
        ]))
        let pdf2 = TestSupport.newPDF()
        pdf2.addResourceObjects(from: shared)
        _ = Page(pdf2, Letter.PORTRAIT)
        try pdf2.complete()
    }

    @Test func theTypeOfAnObjectIsAnEntryOfItsOwn() throws {
        // The /Type of the /Group was taken for the type of the page, which a
        // form refers to, and the page was copied with the form.
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Group << /Type /Group /S /Transparency >> /Type /Page /Parent 2 0 R"
                    + " /Resources << /XObject << /X0 4 0 R >> >> >>",
            "<< /Subtype /Form /BBox [0 0 10 10] /Page 3 0 R /Length 0 >>\nstream\n\nendstream",
        ]))
        #expect(objects[2].getValue("/Type") == "/Page")
        #expect(objects[2].getValue("/S") == "")
        let doc = MemoryPDF()
        doc.pdf.addResourceObjects(from: objects)
        _ = Page(doc.pdf, Letter.PORTRAIT)
        try doc.pdf.complete()
        #expect(!TestSupport.latin1(doc.bytes).contains("/Transparency"), "the page is copied with the form")
    }

    @Test func aNumberThatIsNotAnObjectNumberIsSkipped() throws {
        let objects = try TestSupport.read(pdfWithObjects([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /XObject <<"
                    + " /Im1 abc 0 R /Im2 99999999999 0 R /Im3 4 0 R >> >> >>",
            "<< /Subtype /Form /BBox [0 0 10 10] /Ref 2147483648 0 R /Length 0 >>\nstream\n\nendstream",
        ]))
        let pdf = TestSupport.newPDF()
        pdf.addResourceObjects(from: objects)
        _ = Page(pdf, Letter.PORTRAIT)
        try pdf.complete()
    }

    @Test func theNumbersOfAnObjectStreamAreDigits() {
        #expect(readError("1 0 obj<</Type/ObjStm/N 1/First 5/Length 9>>stream\n+5 0 <<>>\nendstream endobj")
                == "The object stream of the PDF is malformed: \"+5\" is not a number.")
        #expect(readError("1 0 obj<</Type/ObjStm/N 1/First 2147483648/Length 9>>stream\n5 0 <<>>\nendstream endobj")
                == "The object stream of the PDF is malformed: \"2147483648\" is not a number.")
        // An offset past the end of the stream, which overflowed an int of 32 bits.
        #expect(readError("1 0 obj<</Type/ObjStm/N 2/First 2147483000/Length 13>>stream\n5 1000 6 1001\nendstream endobj")
                == "(no error)")
    }
}
