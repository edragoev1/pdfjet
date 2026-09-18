/**
 * UtilTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Content and Util: reading text and binary files and streams.
@Suite struct UtilTests {
    private let tempDir: URL

    init() throws {
        tempDir = FileManager.default.temporaryDirectory.appendingPathComponent("pdfjet-tests-" + UUID().uuidString)
        try FileManager.default.createDirectory(at: tempDir, withIntermediateDirectories: true)
    }

    private func write(_ name: String, _ bytes: [UInt8]) throws -> String {
        let url = tempDir.appendingPathComponent(name)
        try Data(bytes).write(to: url)
        return url.path
    }

    /// An input stream that returns at most three bytes per read.
    private final class SlowStream: InputStream {
        private let data: [UInt8]
        private var position = 0

        init(_ data: [UInt8]) {
            self.data = data
            super.init(data: Data())
        }

        override var hasBytesAvailable: Bool { position < data.count }
        override var streamStatus: Stream.Status { .open }
        override func open() {}
        override func close() {}

        override func read(_ buffer: UnsafeMutablePointer<UInt8>, maxLength len: Int) -> Int {
            let count = min(3, len, data.count - position)
            for i in 0..<count {
                buffer[i] = data[position + i]
            }
            position += count
            return count
        }
    }

    @Test func ofTextFileReadsUtf8AndDropsAByteOrderMark() throws {
        let path = try write("bom.txt", Array("\u{FEFF}hello\nwörld".utf8))
        #expect(try Content.ofTextFile(path) == "hello\nwörld")
    }

    @Test func readLinesDropsAByteOrderMarkAndKeepsEmptyLines() throws {
        let path = try write("lines.txt", Array("\u{FEFF}a\n\nb".utf8))
        #expect(try Content.linesOfTextFile(path) == ["a", "", "b"])
    }

    @Test func readingAMissingFileThrows() {
        let missing = tempDir.appendingPathComponent("missing.txt").path
        #expect(throws: (any Error).self) { _ = try Content.ofTextFile(missing) }
        #expect(throws: (any Error).self) { _ = try Content.ofBinaryFile(missing) }
    }

    @Test func getFromStreamReadsAStreamThatReturnsFewBytesAtATime() throws {
        let data = TestSupport.randomBytes(10000, 7)
        #expect(try Content.getFromStream(SlowStream(data)) == data)
        #expect(try Content.getFromStream(InputStream(data: Data(data)), 17) == data)
    }

    @Test func ofBinaryFileReadsTheBytes() throws {
        let data: [UInt8] = [0, 1, 2, 0xFF]
        #expect(try Content.ofBinaryFile(try write("data.bin", data)) == data)
    }

    @Test func splitCutsTheLineAtTheDelimiterAndKeepsTheEmptyFields() {
        #expect(Util.split("a,b,c", ",") == ["a", "b", "c"])
        #expect(Util.split(",a,", ",") == ["", "a", ""])
        #expect(Util.split("", ",") == [""])
        #expect(Util.split("a||b", "||") == ["a", "b"])
        #expect(Util.split("a,b", "") == ["a,b"])
    }

    @Test func splitReadsAQuotedFieldAsRfc4180Does() {
        #expect(Util.split("\"Smith, John\",42", ",") == ["Smith, John", "42"])
        #expect(Util.split("\"a\"\"b\"", ",") == ["a\"b"])
        #expect(Util.split("\"\",x,\"\"", ",") == ["", "x", ""])
        #expect(Util.split("\"one\ttwo\"\tthree", "\t") == ["one\ttwo", "three"])
    }

    // A line that cannot be read stops the program with fatalError, as the
    // misuse of a font or a cell does in this port, so it has no test here.
    @Test func splitLeavesTheQuotesOfAFieldThatDoesNotStartWithOne() {
        #expect(Util.split("5\" pipe,b", ",") == ["5\" pipe", "b"])
        #expect(Util.split("a\"b\"c", ",") == ["a\"b\"c"])
    }

    /// Reads the first record of the text, as the data file readers do.
    private func firstRecord(_ text: String) -> [String] {
        let lines = text.components(separatedBy: "\n")
        var i = 1
        return Util.readRecord(lines[0], ",") {
            guard i < lines.count else {
                return nil
            }
            i += 1
            return lines[i - 1]
        }
    }

    /// The other ports also check that a quoted field that is never closed is
    /// refused; Swift stops with fatalError, which a test cannot catch.
    @Test func aQuotedFieldGoesOnOverItsLineBreaksAsSpaces() {
        #expect(firstRecord("a,\"12 Main St\nApt 4\",b\nnext,line") == ["a", "12 Main St Apt 4", "b"])
        #expect(firstRecord("\"x\"\"\ny\"") == ["x\" y"])
        #expect(firstRecord("\"a\nb\",\"c\nd\"") == ["a b", "c d"])
        #expect(firstRecord(",\"\n\",\nnext") == ["", " ", ""])
        #expect(firstRecord("a,b\n\"c\nd\"") == ["a", "b"])
    }

    @Test func lineBreaksAreDrawnAsSpaces() {
        #expect(Util.lineBreaksToSpaces("a\r\nb\rc\nd") == "a b c d")
        #expect(Util.lineBreaksToSpaces("no breaks") == "no breaks")
    }

    @Test func noLineOfChineseOrJapaneseTextStartsWithAClosingMarkOrEndsWithAnOpeningOne() {
        func end(_ line: String, _ next: Unicode.Scalar) -> Int {
            cjkLineEnd(Array(line.unicodeScalars), next)
        }
        #expect(end("あいう", "え") == 3)
        #expect(end("あいう", "。") == 2)  // う moves down with 。
        #expect(end("あい」", "。") == 1)  // and so does 」
        #expect(end("あい「", "う") == 2)  // 「 moves down
        #expect(end("中文", ",") == 1)
        #expect(end("」", "。") == 1)      // no other place to break
    }
}
