/**
 * BMPImageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct BMPImageTests {
    // Red, green on the top row; blue, white on the bottom row.
    private static let pixels: [[[UInt8]]] = [[[255, 0, 0], [0, 255, 0]], [[0, 0, 255], [255, 255, 255]]]
    private static let rgb: [UInt8] = [255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255]

    private func littleEndian32(_ value: Int32, _ bytes: inout [UInt8]) {
        let v = UInt32(bitPattern: value)
        bytes.append(contentsOf: [UInt8(v & 0xFF), UInt8((v >> 8) & 0xFF), UInt8((v >> 16) & 0xFF), UInt8(v >> 24)])
    }

    private func bmp24(_ topDown: Bool) -> [UInt8] {
        let width = 2
        let height = 2
        let rowSize = (width * 3 + 3) & ~3
        var bytes: [UInt8] = [0x42, 0x4D]
        littleEndian32(Int32(54 + rowSize * height), &bytes)
        littleEndian32(0, &bytes)
        littleEndian32(54, &bytes)
        littleEndian32(40, &bytes)
        littleEndian32(Int32(width), &bytes)
        littleEndian32(Int32(topDown ? -height : height), &bytes)
        bytes.append(contentsOf: [1, 0, 24, 0])
        littleEndian32(0, &bytes)
        littleEndian32(Int32(rowSize * height), &bytes)
        littleEndian32(2835, &bytes)
        littleEndian32(2835, &bytes)
        littleEndian32(0, &bytes)
        littleEndian32(0, &bytes)
        for i in 0..<height {
            for pixel in BMPImageTests.pixels[topDown ? i : height - 1 - i] {
                bytes.append(contentsOf: [pixel[2], pixel[1], pixel[0]])
            }
            bytes.append(contentsOf: [UInt8](repeating: 0, count: rowSize - width * 3))
        }
        return bytes
    }

    /// BMPImage reads the stream without opening it, as Image.init opens it first.
    private func stream(_ bytes: [UInt8]) -> InputStream {
        let stream = InputStream(data: Data(bytes))
        stream.open()
        return stream
    }

    @Test func bottomUpRowsAreReadTopRowFirst() throws {
        let bmp = try BMPImage(stream(bmp24(false)))
        #expect(bmp.getWidth() == 2)
        #expect(bmp.getHeight() == 2)
        #expect(try TestSupport.inflate(bmp.getData()) == BMPImageTests.rgb)
    }

    @Test func topDownRowsAreReadTheSame() throws {
        let bmp = try BMPImage(stream(bmp24(true)))
        #expect(try TestSupport.inflate(bmp.getData()) == BMPImageTests.rgb)
    }

    @Test func aTruncatedFileThrows() {
        let truncated = Array(bmp24(false).prefix(60))
        #expect(throws: (any Error).self) { _ = try BMPImage(stream(truncated)) }
    }
}
