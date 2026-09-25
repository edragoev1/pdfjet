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
        let was = PDFobj.maxDecodedTotal
        PDFobj.maxDecodedTotal = 5000
        defer { PDFobj.maxDecodedTotal = was }
        // The object stream, read with the PDF, is within the budget, and the
        // content stream is decoded up to what is left of it: 4000 of 5000 is
        // left after the object stream of 1000, so a content of 4000 is
        // decoded and one of 4004 is not.
        var objects = try Self.read(Self.pdfOfStreams(1000, 4000))
        #expect(Self.objectOfNumber(objects, 4)?.getData().count == 4000)
        objects = try Self.read(Self.pdfOfStreams(1000, 4004))
        #expect(Self.objectOfNumber(objects, 4)?.getData().count == 0)
        // An object stream past the budget is an error, as the PDF cannot be
        // read without its objects.
        #expect(throws: (any Error).self) {
            _ = try Self.read(Self.pdfOfStreams(6000, 0))
        }
        do {
            _ = try Self.read(Self.pdfOfStreams(6000, 0))
        } catch {
            #expect(String(describing: error) == "the streams of the PDF decode to more than 5000 bytes together")
        }
    }
}
