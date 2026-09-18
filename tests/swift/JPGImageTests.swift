/**
 * JPGImageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// A CMYK JPEG that Adobe software wrote, as images/cmyk.jpg is, has an APP14
/// marker and stores its inks inverted, so the image is written with a Decode
/// array that inverts them back; one without the marker is written as it is.
@Suite struct JPGImageTests {
    private static let cmyk = "images/cmyk.jpg"

    // Returns the JPEG without its APP14 segment.
    private func withoutAPP14(_ jpeg: [UInt8]) -> [UInt8] {
        var i = 2
        while i + 3 < jpeg.count {
            if jpeg[i] == 0xFF && jpeg[i + 1] == 0xEE {
                let end = i + 2 + (Int(jpeg[i + 2]) << 8 | Int(jpeg[i + 3]))
                return Array(jpeg[0..<i]) + Array(jpeg[end...])
            }
            i += 1
        }
        return jpeg
    }

    private func pdfWith(_ jpeg: [UInt8]) throws -> String {
        let memory = MemoryPDF()
        let image = try Image(memory.pdf, InputStream(data: Data(jpeg)))
        image.drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        return TestSupport.latin1(memory.bytes)
    }

    @Test(.enabled(if: TestSupport.exists(cmyk), "the images directory is not here"))
    func theInksOfACmykJpegAreInvertedBackOnlyWhenAdobeSoftwareWroteIt() throws {
        let jpeg = [UInt8](try Data(contentsOf: URL(fileURLWithPath: TestSupport.path(JPGImageTests.cmyk))))
        #expect(try JPGImage(InputStream(data: Data(jpeg))).isAdobe())
        #expect(try pdfWith(jpeg).contains("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]"))

        let plain = withoutAPP14(jpeg)
        #expect(try !JPGImage(InputStream(data: Data(plain))).isAdobe())
        let raw = try pdfWith(plain)
        #expect(raw.contains("/DeviceCMYK"))
        #expect(!raw.contains("/Decode ["))
    }
}
