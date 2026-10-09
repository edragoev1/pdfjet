/**
 * JPGOrientationTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// A JPEG with an Exif orientation is drawn as it is meant to be seen:
/// tests/data/jpeg/orientation-N.jpg is stored turned so that it is seen as
/// 32 by 16 pixels, as orientation-1.jpg is; 6 and 8 are 16 by 32 as stored.
@Suite struct JPGOrientationTests {
    private func orientationJPEG(_ orientation: String) throws -> [UInt8] {
        return [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(
                "tests/data/jpeg/orientation-" + orientation + ".jpg"))))
    }

    // The content stream of a page the JPEG is drawn on, the PDF, and the image.
    private func drawn(_ jpeg: [UInt8]) throws -> (content: String, pdf: String, image: Image) {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let image = try Image(memory.pdf, InputStream(data: Data(jpeg)))
        image.setLocation(10.0, 20.0)
        image.drawOn(page)
        let content = TestSupport.content(page)
        try memory.pdf.complete()
        return (content, TestSupport.latin1(memory.bytes), image)
    }

    @Test(arguments: [
        ("1", 32, 16, ""),
        ("3", 32, 16, "-1 0 0 -1 1 1 cm\n"),
        ("6", 16, 32, "0 -1 1 0 0 1 cm\n"),
        ("8", 16, 32, "0 1 -1 0 1 0 cm\n"),
    ])
    func theSizeAndTheMatrixAreAsSeen(_ test: (String, Int, Int, String)) throws {
        let (orientation, width, height, matrix) = test
        let result = try drawn(try orientationJPEG(orientation))
        #expect(result.image.getWidth() == 32.0, "orientation \(orientation)")
        #expect(result.image.getHeight() == 16.0, "orientation \(orientation)")
        // The image object keeps the pixels as stored
        #expect(result.pdf.contains("/Width \(width)\n"), "orientation \(orientation)")
        #expect(result.pdf.contains("/Height \(height)\n"), "orientation \(orientation)")
        #expect(result.content.contains("32 0 0 16 10 756 cm\n" + matrix + "/Im"),
                "orientation \(orientation): \(result.content)")
    }

    // Orientation 1, upright as stored, draws the page as a JPEG without Exif.
    @Test func orientationOneIsDrawnAsWithoutExif() throws {
        let data = try orientationJPEG("1")
        #expect(try drawn(data).content == drawn(withoutSegment(data, 0xE1)).content)
    }

    // Returns the JPEG without its first segment of the marker.
    private func withoutSegment(_ jpeg: [UInt8], _ marker: UInt8) -> [UInt8] {
        var i = 2
        while i + 3 < jpeg.count {
            if jpeg[i] == 0xFF && jpeg[i + 1] == marker {
                let end = i + 2 + (Int(jpeg[i + 2]) << 8 | Int(jpeg[i + 3]))
                return Array(jpeg[0..<i]) + Array(jpeg[end...])
            }
            i += 1
        }
        return jpeg
    }

    // An Exif segment, without its marker and length, with one entry in IFD0,
    // in either byte order.
    private func exif(_ bigEndian: Bool, _ tag: Int, _ kind: Int, _ count: Int, _ value: Int) -> [UInt8] {
        func u16(_ v: Int) -> [UInt8] {
            let bytes = [UInt8((v >> 8) & 0xFF), UInt8(v & 0xFF)]
            return bigEndian ? bytes : bytes.reversed()
        }
        func u32(_ v: Int) -> [UInt8] {
            return bigEndian ? u16(v >> 16) + u16(v & 0xFFFF) : u16(v & 0xFFFF) + u16(v >> 16)
        }
        var segment = Array("Exif".utf8) + [0, 0]
        segment += bigEndian ? Array("MM".utf8) : Array("II".utf8)
        segment += u16(42) + u32(8) + u16(1)
        segment += u16(tag) + u16(kind) + u32(count) + u16(value) + u16(0)
        return segment + u32(0)
    }

    @Test func theOrientationIsReadInBothByteOrders() {
        for bigEndian in [false, true] {
            for value in 1...8 {
                #expect(JPGImage.exifOrientation(exif(bigEndian, 0x0112, 3, 1, value)) == value,
                        "big endian \(bigEndian)")
            }
        }
    }

    // A malformed Exif segment is passed over, never read out of its bounds.
    @Test func theOrientationOfAMalformedExifIsPassedOver() throws {
        var notExif = exif(false, 0x0112, 3, 1, 6)
        notExif[3] = UInt8(ascii: "v")
        var otherOrder = exif(false, 0x0112, 3, 1, 6)
        otherOrder[7] = UInt8(ascii: "M")
        var before8 = exif(false, 0x0112, 3, 1, 6)
        before8[10] = 4
        var pastTheEnd = exif(false, 0x0112, 3, 1, 6)
        pastTheEnd[10] = 0xFF
        pastTheEnd[11] = 0xFF
        pastTheEnd[12] = 0xFF
        pastTheEnd[13] = 0xFF
        let segments = [
            exif(false, 0x0112, 3, 1, 0),
            exif(true, 0x0112, 3, 1, 9),
            exif(false, 0x0112, 4, 1, 6),
            exif(true, 0x0112, 3, 2, 6),
            exif(false, 0x0110, 3, 1, 6),
            notExif, otherOrder, before8, pastTheEnd,
        ]
        for (i, segment) in segments.enumerated() {
            #expect(JPGImage.exifOrientation(segment) == 0, "segment \(i)")
        }
        let whole = exif(true, 0x0112, 3, 1, 6)
        for end in 0..<(whole.count - 4) {
            #expect(JPGImage.exifOrientation(Array(whole[..<end])) == 0, "cut at \(end)")
        }
        // The JPEG with a malformed Exif is drawn as stored
        var broken = try orientationJPEG("6")
        var i = 0
        while i + 3 < broken.count {
            if broken[i] == 0x4D && broken[i + 1] == 0x4D && broken[i + 2] == 0 && broken[i + 3] == 0x2A {
                broken[i + 3] = 0x2B
                break
            }
            i += 1
        }
        let image = try drawn(broken).image
        #expect(image.getWidth() == 16.0)
        #expect(image.getHeight() == 32.0)
    }
}
