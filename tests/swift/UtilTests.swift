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
        #expect(try Util.readLines(path) == ["a", "", "b"])
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
}
