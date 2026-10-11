/**
 * ReadBudgetTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite(.serialized) struct ReadBudgetTests {
    // The bytes as a RunLength stream: runs of up to 128 bytes, literal or
    // repeated, and the end of data.
    private static func runLength(_ data: [UInt8]) -> [UInt8] {
        var out = [UInt8]()
        var i = 0
        while i < data.count {
            var run = 1
            while i + run < data.count && run < 128 && data[i + run] == data[i] {
                run += 1
            }
            if run > 1 {
                out.append(UInt8(257 - run))
                out.append(data[i])
                i += run
            } else {
                var end = i + 1
                while end < data.count && end - i < 128 && (end + 1 >= data.count || data[end] != data[end + 1]) {
                    end += 1
                }
                out.append(UInt8(end - i - 1))
                out.append(contentsOf: data[i..<end])
                i = end
            }
        }
        out.append(128)
        return out
    }

    // A PDF, without a cross-reference table, so that it is read by scanning,
    // of a catalog, an object stream that holds one object, and a content
    // stream: each RunLength, decoding to the given number of bytes.
    private static func pdfOfStreams(_ objectStreamBytes: Int, _ contentBytes: Int) -> [UInt8] {
        var pdf = [UInt8]()
        func write(_ text: String) { pdf.append(contentsOf: Array(text.utf8)) }
        write("%PDF-1.5\n")
        write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
        write("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
        var body = "5 0 << /Padding /"
        body += String(repeating: "x", count: max(0, objectStreamBytes - body.count - 3)) + " >>"
        let stream = runLength(Array(body.utf8))
        write("3 0 obj\n<< /Type /ObjStm /N 1 /First 4 /Length \(stream.count) /Filter /RunLengthDecode >>\nstream\n")
        pdf.append(contentsOf: stream)
        write("\nendstream\nendobj\n")
        let content = runLength(Array(String(repeating: "q Q\n", count: contentBytes / 4).utf8))
        write("4 0 obj\n<< /Length \(content.count) /Filter /RunLengthDecode >>\nstream\n")
        pdf.append(contentsOf: content)
        write("\nendstream\nendobj\n")
        write("trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n")
        return pdf
    }

    private static func read(_ pdf: [UInt8]) throws -> [PDFobj] {
        return try TestSupport.newPDF().read(from: InputStream(data: Data(pdf)))
    }

    private static func objectOfNumber(_ objects: [PDFobj], _ number: Int) -> PDFobj? {
        return objects.first { $0.number == number }
    }

    @Test func aStreamIsDecodedWhenItsDataIsFirstAskedFor() throws {
        let objects = try Self.read(Self.pdfOfStreams(100, 4000))
        let content = try #require(Self.objectOfNumber(objects, 4))
        #expect(content.undecoded, "the content stream is decoded before it is asked for")
        #expect(content.data.isEmpty)
        #expect(content.getData().count == 4000)
        #expect(!content.undecoded)
        // The object stream was read when the PDF was: its object is there.
        #expect(Self.objectOfNumber(objects, 5)?.getValue("/Padding") != "")
    }

    @Test func theStreamsOfAPDFDecodeToNoMoreThanTheBudgetTogether() throws {
        // A budget of 5000 for these PDFs alone: the other tests, which run at
        // the same time, read their PDFs with the budget as it is. 5000 for
        // the streams, and what the one object of the object stream takes of
        // the budget besides.
        func read(_ pdf: [UInt8]) throws -> [PDFobj] {
            return try TestSupport.newPDF().read(from: InputStream(data: Data(pdf)), password: "",
                    decodedTotal: 5000 + PDF.objectCost)
        }
        // The object stream, read with the PDF, is within the budget, and the
        // content stream is decoded up to what is left of it: 4000 of 5000 is
        // left after the object stream of 1000, so a content of 4000 is
        // decoded and one of 4004 is not.
        var objects = try read(Self.pdfOfStreams(1000, 4000))
        #expect(Self.objectOfNumber(objects, 4)?.getData().count == 4000)
        objects = try read(Self.pdfOfStreams(1000, 4004))
        #expect(Self.objectOfNumber(objects, 4)?.getData().count == 0)
        // An object stream past the budget is an error, as the PDF cannot be
        // read without its objects.
        #expect(throws: (any Error).self) {
            _ = try read(Self.pdfOfStreams(6000, 0))
        }
        do {
            _ = try read(Self.pdfOfStreams(6000, 0))
        } catch {
            #expect(String(describing: error) == "the streams of the PDF decode to more than \(5000 + PDF.objectCost) bytes together")
        }
    }
    // A /Contents array that names one stream again and again is charged to
    // the budget for each copy, as its data is copied each time: past the
    // budget the page has no content (review of 11 October 2026, #29).
    @Test func aContentStreamNamedAgainIsChargedAgain() throws {
        func read(_ pdf: [UInt8]) throws -> [PDFobj] {
            return try TestSupport.newPDF().read(from: InputStream(data: Data(pdf)), password: "", decodedTotal: 5000)
        }
        func page(_ times: Int) -> PDFobj {
            let obj = PDFobj()
            obj.dict.append(contentsOf: ["/Contents", "["])
            for _ in 0..<times {
                obj.dict.append(contentsOf: ["4", "0", "R"])
            }
            obj.dict.append("]")
            return obj
        }
        // 100 of the object stream, 1000 of the content decoded once, 1001
        // for each copy after the first: three copies take 3102 of 5000
        var objects = try read(Self.pdfOfStreams(100, 1000))
        let content = page(3).getContentObject(objects)
        #expect(content?.data.count == 3003, "three copies within the budget")
        // Five take 5104, past it
        objects = try read(Self.pdfOfStreams(100, 1000))
        #expect(page(5).getContentObject(objects) == nil, "five copies past the budget make a content")
    }

    // An object stream gives no more objects than its /N says, and each
    // object takes from the PDF's budget: a header of millions of "1 0 " is
    // refused, not millions of objects (review of 11 October 2026, #30).
    @Test func anObjectStreamGivesNoMoreObjectsThanItsN() throws {
        func objectStream(_ n: Int, _ header: String) -> [UInt8] {
            var pdf = [UInt8]()
            func write(_ text: String) { pdf.append(contentsOf: Array(text.utf8)) }
            write("%PDF-1.5\n")
            write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            write("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
            let stream = TestSupport.deflate(Array((header + "<< /A 1 >> << /B 2 >>").utf8))
            write("3 0 obj\n<< /Type /ObjStm /N \(n) /First \(header.utf8.count) /Length \(stream.count) /Filter /FlateDecode >>\nstream\n")
            pdf.append(contentsOf: stream)
            write("\nendstream\nendobj\n")
            write("trailer\n<< /Root 1 0 R /Size 7 >>\n%%EOF\n")
            return pdf
        }
        // /N 1 of a header of two: object 5 alone
        let objects = try Self.read(objectStream(1, "5 0 6 11 "))
        #expect(Self.objectOfNumber(objects, 5) != nil && Self.objectOfNumber(objects, 6) == nil,
                "an object stream of /N 1 gives objects other than its first")
        // A header of more objects than the budget takes is refused
        let header = String(repeating: "1 0 ", count: 1000)
        #expect(throws: (any Error).self, "an object stream of 1000 objects past the budget is read") {
            _ = try TestSupport.newPDF().read(from: InputStream(data: Data(objectStream(1000, header))),
                    password: "", decodedTotal: 100 * PDF.objectCost)
        }
    }

    // A cross-reference stream of more objects than the PDF has room for, or
    // of offsets past its end, is not taken: the objects are found by
    // scanning, and the entries are not all kept first (review of 11 October
    // 2026, #31).
    @Test func aCrossReferenceStreamOfTooManyEntriesIsNotTaken() throws {
        var pdf = [UInt8]()
        func write(_ text: String) { pdf.append(contentsOf: Array(text.utf8)) }
        write("%PDF-1.5\n")
        write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
        write("2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n")
        // 200,000 entries of type 1, at offset 0, with /W [1 0 0]
        let stream = TestSupport.deflate([UInt8](repeating: 1, count: 200000))
        let xref = pdf.count
        write("3 0 obj\n<< /Type /XRef /W [1 0 0] /Size 200000 /Root 1 0 R /Length \(stream.count) /Filter /FlateDecode >>\nstream\n")
        pdf.append(contentsOf: stream)
        write("\nendstream\nendobj\n")
        write("startxref\n\(xref)\n%%EOF\n")
        let objects = try Self.read(pdf)
        #expect(Self.objectOfNumber(objects, 1) != nil && Self.objectOfNumber(objects, 2) != nil,
                "the objects are not found by scanning")
        #expect(objects.count <= 10, "\(objects.count) objects of a PDF of three")
    }
}
