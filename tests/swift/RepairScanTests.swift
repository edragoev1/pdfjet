/*
 * RepairScanTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

// The repair scan of a PDF whose cross-reference table is broken, which reads
// the PDF by looking for its objects.
@Suite struct RepairScanTests {
    static let inner = "BT /F1 12 Tf 72 720 Td (x) Tj ET\n"
            + "5 0 obj\n<< /Length 3 >>\nstream\nabc\nendstream\nendobj\n"
            + "q Q"

    // A PDF whose page's content is an unfiltered stream with a wrong /Length
    // that holds an object of an embedded PDF, with a stream of its own.
    static func pdfWithAnObjectInAStream() -> [UInt8] {
        var pdf = "%PDF-1.7\n"
        pdf += "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
        pdf += "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"
        pdf += "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n"
        pdf += "4 0 obj\n<< /Length 10 >>\nstream\n" + inner + "\nendstream\nendobj\n"
        pdf += "trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n999999\n%%EOF\n"
        return TestSupport.bytes(pdf)
    }

    @Test func aStreamThatHoldsAnObjectIsReadWhole() throws {
        // An object ended at the next "number generation obj": one in the
        // bytes of a stream, as an embedded PDF has, unfiltered, cut the stream
        // short and was read as an object of the PDF, and the first endstream
        // after the stream, that of the embedded object, was taken for the
        // stream's own.
        let objects = try TestSupport.read(RepairScanTests.pdfWithAnObjectInAStream())
        var content: PDFobj?
        for obj in objects {
            #expect(obj.number != 5, "the object in the stream is read as an object of the PDF")
            if obj.number == 4 {
                content = obj
            }
        }
        let stream = try #require(content)
        #expect(TestSupport.latin1(stream.getData()) == RepairScanTests.inner)
    }
}
