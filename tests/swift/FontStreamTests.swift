/**
 * FontStreamTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

// The stream fonts that are not valid, as the Go fuzz targets of the stream
// fonts found them: each throws with a message, and none reads past its data,
// allocates what it does not have or traps.
@Suite struct FontStreamTests {
    // The metrics of a stream font: the units per em, the first and the last
    // character, the advance widths and the character map.
    private func metrics(_ unitsPerEm: Int32, _ firstChar: Int32, _ lastChar: Int32,
            _ widths: Int, _ cmap: Int) -> [UInt8] {
        var buf = [UInt8]()
        for v in [unitsPerEm, 0, -200, 1000, 800, 800, -200, firstChar, lastChar, 700, -100, 50] {
            int32(&buf, v)
        }
        int32(&buf, Int32(widths))
        buf.append(contentsOf: [UInt8](repeating: 0, count: 2*widths))
        int32(&buf, Int32(cmap))
        buf.append(contentsOf: [UInt8](repeating: 0, count: 2*cmap))
        return buf
    }

    // A stream font with the name and the metrics, and a font file of 4 bytes.
    private func stream(_ name: String, _ metrics: [UInt8]) -> [UInt8] {
        var buf = [UInt8(name.utf8.count)]
        buf.append(contentsOf: Array(name.utf8))
        buf.append(contentsOf: [0, 0, 0])   // No license text
        let compressed = TestSupport.deflate(metrics)
        int32(&buf, Int32(compressed.count))
        buf.append(contentsOf: compressed)
        buf.append(UInt8(ascii: "N"))
        int32(&buf, 8)
        int32(&buf, 4)
        buf.append(contentsOf: [0, 0, 0, 0])
        return buf
    }

    private func int32(_ buf: inout [UInt8], _ v: Int32) {
        let u = UInt32(bitPattern: v)
        buf.append(contentsOf: [UInt8(u >> 24), UInt8((u >> 16) & 0xFF), UInt8((u >> 8) & 0xFF), UInt8(u & 0xFF)])
    }

    // The message that loading the stream font throws with.
    private func error(_ stream: [UInt8]) -> String {
        do {
            _ = try Font(TestSupport.newPDF(), InputStream(data: Data(stream)))
        } catch {
            return "\(error)"
        }
        return "(no error)"
    }

    @Test func aMarkAfterAGlyphPastTheAdvanceWidthsIsDrawn() throws {
        // IBM Plex Sans JP maps ↺ and 14 other arrows to glyphs past the end
        // of its advance widths, which the offsets of the marks looked up.
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream"))
        TextLine(font, "\u{21BA}\u{0301} x").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
    }

    @Test func lengthsThatTheStreamDoesNotHaveAreRejected() {
        // The metrics say they are 4 GB long, and the stream ends.
        #expect(error([1, UInt8(ascii: "A"), 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF]) ==
                "Unexpected end of the font stream.")
    }

    @Test func metricsItCannotDrawWithAreRejected() throws {
        #expect(error(stream("A", metrics(0, 32, 126, 1, 0x10000))) ==
                "Invalid font stream: the units per em.")
        #expect(error(stream("A", metrics(1000, -1, 126, 1, 0x10000))) ==
                "Invalid font stream: the first or last character.")
        #expect(error(stream("A", metrics(1000, 32, 0x10000, 1, 0x10000))) ==
                "Invalid font stream: the first or last character.")
        #expect(error(stream("A", metrics(1000, 32, 126, 0, 0x10000))) ==
                "Invalid font stream: no advance widths.")
        #expect(error(stream("A", metrics(1000, 32, 126, 1, 0x100))) ==
                "Invalid font stream: the character map.")
        #expect(error(stream("A", Array(metrics(1000, 32, 126, 1, 0x10000)[0..<100]))) ==
                "Invalid font stream: the metrics end too soon.")

        let valid = stream("A", metrics(1000, 32, 126, 1, 0x10000))
        #expect(error(valid) == "(no error)")
        var objects = [PDFobj]()
        _ = try Font(&objects, InputStream(data: Data(valid)))
    }

    @Test func aNameThatIsNotAPDFNameIsRejected() {
        for name in ["", "Noto Sans", "Noto/Sans", "Noto(Sans", "Noto#20Sans"] {
            #expect(error(stream(name, metrics(1000, 32, 126, 1, 0x10000))) ==
                    "Invalid font stream: the font name.")
        }
    }

    @Test func marksThatCannotBeReadFailTheDocument() throws {
        // The marks say they have 5 subtables, and end.
        var withMarks = metrics(1000, 32, 0x0301, 1, 0x10000)
        let marks = TestSupport.deflate([0, 0, 0, 5])
        int32(&withMarks, Int32(marks.count))
        withMarks.append(contentsOf: marks)
        let memory = MemoryPDF()
        let font = try Font(memory.pdf, InputStream(data: Data(stream("A", withMarks))))
        TextLine(font, "e\u{0301}").setLocation(50, 50).drawOn(Page(memory.pdf, Letter.PORTRAIT))
        do {
            try memory.pdf.complete()
            Issue.record("the document was completed")
        } catch {
            #expect("\(error)".contains("The marks of the font cannot be read: Invalid font stream: the marks end too soon."))
        }
    }

    @Test func aFontFileShorterThanItsSizeIsRejected() {
        #expect(error(Array(stream("A", metrics(1000, 32, 126, 1, 0x10000)).dropLast())) ==
                "Unexpected end of the font stream.")
    }
}
