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

    // The frame header of the JPEG is what PDFjet reads it for, so a marker
    // before it that is read as one more segment hides it.

    // Returns a JPEG of 8 by 8 pixels: the SOI marker, the bytes given, and a
    // frame header of the number of color components.
    private func jpegOf(_ before: [UInt8], _ components: Int) -> [UInt8] {
        var jpeg: [UInt8] = [0xFF, 0xD8]
        jpeg.append(contentsOf: before)
        let length = 8 + 3*components
        jpeg.append(contentsOf: [0xFF, 0xC0, UInt8(length >> 8), UInt8(length & 0xFF),
                8, 0, 8, 0, 8, UInt8(components)])
        for i in 0..<components {
            jpeg.append(contentsOf: [UInt8(i + 1), 0x11, 0])
        }
        return jpeg
    }

    // Returns an APP14 segment of the bytes.
    private func app14(_ payload: [UInt8]) -> [UInt8] {
        return [0xFF, 0xEE, UInt8((payload.count + 2) >> 8), UInt8((payload.count + 2) & 0xFF)] + payload
    }

    // The segment Adobe software writes: "Adobe", the version, two flags and
    // the color transform.
    private var adobeAPP14: [UInt8] {
        return app14(Array("Adobe".utf8) + [0, 100, 0, 0, 0, 0, 2])
    }

    @Test func theMarkersWithoutAParameterSegmentAreNotReadAsSegments() throws {
        let markers: [[UInt8]] = [
            [0xFF, 0x00, 0x30],     // A stuffed 0xFF byte
            [0xFF, 0xD3],           // A restart marker
            [0xFF, 0x01],           // A TEM marker
            [0xFF, 0xD8],           // A nested SOI marker
            [0xFF, 0xFF],           // A fill byte
        ]
        for before in markers {
            let image = try JPGImage(InputStream(data: Data(jpegOf(before, 3))))
            #expect(image.getWidth() == 8)
            #expect(image.getHeight() == 8)
            #expect(image.getColorComponents() == 3)
        }
    }

    @Test func aJPEGThatEndsBeforeItsFrameHeaderFails() throws {
        #expect(throws: JPGImageError.endsBeforeTheFrameHeader) {
            try JPGImage(InputStream(data: Data([0xFF, 0xD8, 0xFF, 0xD9] as [UInt8])))
        }
    }

    @Test func aJPEGOfOtherThanEightBitsPerComponentFails() throws {
        // A PDF image stream of DCTDecode data delivers eight bit samples, and
        // PDFjet writes 8 as the bits per component, so a 12-bit JPEG would be
        // drawn as noise rather than refused.
        for precision: UInt8 in [0, 12, 16] {
            var jpeg = jpegOf([], 3)
            jpeg[6] = precision     // After the SOI marker, the SOF0 marker and the length
            #expect(throws: JPGImageError.unsupportedSamplePrecision) {
                try JPGImage(InputStream(data: Data(jpeg)))
            }
        }
        _ = try JPGImage(InputStream(data: Data(jpegOf([], 3))))
    }

    @Test func onlyTheWholeAdobeAPP14SegmentMarksTheImage() throws {
        #expect(try JPGImage(InputStream(data: Data(jpegOf(adobeAPP14, 4)))).isAdobe())

        // Adobe's segment is twelve bytes; a shorter one is another APP14.
        let short = app14(Array("Adobe".utf8))
        #expect(try !JPGImage(InputStream(data: Data(jpegOf(short, 4)))).isAdobe())
    }

    @Test func theAdobeAPP14SegmentIsFoundAfterTheFrameHeader() throws {
        // libjpeg reads the markers of a header to the scan, so an APP14 segment
        // between the frame header and the scan marks the image too; one after
        // the scan, which no header reads, does not.
        let after = jpegOf([], 4) + adobeAPP14
        #expect(try JPGImage(InputStream(data: Data(after))).isAdobe())

        let afterTheScan = jpegOf([], 4) + [0xFF, 0xDA, 0x00, 0x02] + adobeAPP14
        #expect(try !JPGImage(InputStream(data: Data(afterTheScan))).isAdobe())
    }

    @Test func theComponentsOfTheFrameHeaderAreNotReadAsMarkers() throws {
        // The component specifications fill the rest of the frame header and can
        // hold any bytes, the 0xFF of a marker among them; here they are the
        // bytes of an Adobe APP14 segment, which is none.
        var jpeg = jpegOf([], 4)
        let components: [UInt8] = [0xFF, 0xEE, 0x00, 0x0E] + Array("Adobe".utf8) + [0x00, 0x64, 0x00]
        jpeg.replaceSubrange((jpeg.count - 12)..., with: components)
        jpeg += [0, 0, 0, 0, 0xFF, 0xD9]
        #expect(try !JPGImage(InputStream(data: Data(jpeg))).isAdobe())
    }

    @Test func anotherAPP14SegmentDoesNotUnmarkAnAdobeImage() throws {
        let before = adobeAPP14 + app14(Array("Not Adobe".utf8) + [0, 0, 0])
        #expect(try JPGImage(InputStream(data: Data(jpegOf(before, 4)))).isAdobe())
    }
}
