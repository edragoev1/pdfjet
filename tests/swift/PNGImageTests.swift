/**
 * PNGImageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// PNG decoding against the PngSuite images. The samples are compared as the
/// CRC-32 of the decompressed data. MuPDF and Pillow decode the 8-bit, filtered
/// and palette images, and the alpha of BASN6A08, BASN4A08 and TP1N3P08, to the
/// same samples, and Pillow the colors of BASN6A08 and the gray of BASN4A08; the
/// 1, 2, 4 and 16 bit grayscale and 16 bit RGB samples keep their bit depth here.
@Suite struct PNGImageTests {
    // name, width, height, color type, bit depth, sample bytes, sample CRC, alpha bytes, alpha CRC
    private static let suite: [(String, Int, Int, Int, Int, Int, String, Int, String)] = [
        ("BASN0G01", 32, 32, 0, 1, 128, "b71a0667", 0, ""),
        ("BASN0G02", 32, 32, 0, 2, 256, "c429db1d", 0, ""),
        ("BASN0G04", 32, 32, 0, 4, 512, "8089a6e9", 0, ""),
        ("BASN0G08", 32, 32, 0, 8, 1024, "784b4a4e", 0, ""),
        ("BASN0G16", 32, 32, 0, 16, 2048, "9362f0f0", 0, ""),
        ("BASN2C08", 32, 32, 2, 8, 3072, "7855b9bf", 0, ""),
        ("BASN2C16", 32, 32, 2, 16, 6144, "c278125a", 0, ""),
        ("BASN3P01", 32, 32, 3, 1, 3072, "31ec284b", 0, ""),
        ("BASN3P02", 32, 32, 3, 2, 3072, "279a463a", 0, ""),
        ("BASN3P04", 32, 32, 3, 4, 3072, "3a9e038e", 0, ""),
        ("BASN3P08", 32, 32, 3, 8, 3072, "ff6e2940", 0, ""),
        ("BASN6A08", 32, 32, 6, 8, 3072, "a9b0c6b5", 1024, "fa6029ad"),
        ("TP1N3P08", 32, 32, 3, 8, 3072, "8b0a6c2c", 1024, "f83b2838"),
        ("F00N2C08", 32, 32, 2, 8, 3072, "3f1d66ad", 0, ""),
        ("F01N2C08", 32, 32, 2, 8, 3072, "11c1b27e", 0, ""),
        ("F02N2C08", 32, 32, 2, 8, 3072, "7f1ca785", 0, ""),
        ("F03N2C08", 32, 32, 2, 8, 3072, "31645d89", 0, ""),
        ("F04N2C08", 32, 32, 2, 8, 3072, "77056a6f", 0, ""),
        ("F00N0G08", 32, 32, 0, 8, 1024, "1f18265f", 0, ""),
        ("F04N0G08", 32, 32, 0, 8, 1024, "b8006228", 0, ""),
        ("S01N3P01", 1, 1, 3, 1, 3, "d243369f", 0, ""),
        ("S05N3P02", 5, 5, 3, 2, 75, "1242b6fb", 0, ""),
    ]

    private func decode(_ name: String) throws -> PNGImage {
        return try PNGImage(TestSupport.open("PngSuite/\(name).PNG"))
    }

    @Test func decodesThePngSuiteImages() throws {
        for row in PNGImageTests.suite {
            let name = row.0
            let png = try decode(name)
            #expect(png.getWidth() == row.1, "\(name)")
            #expect(png.getHeight() == row.2, "\(name)")
            #expect(png.getColorType() == row.3, "\(name)")
            #expect(png.getBitDepth() == row.4, "\(name)")
            let samples = try TestSupport.inflate(png.getData())
            #expect(samples.count == row.5, "\(name)")
            #expect(TestSupport.crc32(samples) == row.6, "\(name)")
            if row.7 == 0 {
                // Java returns null for no alpha; Swift returns no bytes.
                #expect(png.getAlpha().isEmpty, "\(name)")
            } else {
                let alpha = try TestSupport.inflate(png.getAlpha())
                #expect(alpha.count == row.7, "\(name)")
                #expect(TestSupport.crc32(alpha) == row.8, "\(name)")
            }
        }
    }

    @Test func truecolorTransparencyIsIgnored() throws {
        // tRNS applies to palette images only; TBRN2C08 has the samples of TP1N3P08.
        let png = try decode("TBRN2C08")
        #expect(TestSupport.crc32(try TestSupport.inflate(png.getData())) == "8b0a6c2c")
        #expect(png.getAlpha().isEmpty)
    }

    @Test func rejects16BitRgbaWithAMessage() {
        let error = #expect(throws: (any Error).self) { _ = try decode("BASN6A16") }
        #expect(TestSupport.message(error) == "Image with unsupported bit depth == 16")
    }

    @Test func rejectsDataThatIsNotAPng() {
        #expect(throws: (any Error).self) { _ = try PNGImage(InputStream(data: Data("not a png file".utf8))) }
    }

    @Test func anImageFromAPngHasItsSize() throws {
        let image = try Image(TestSupport.newPDF(), TestSupport.open("PngSuite/BASN2C08.PNG"), ImageType.PNG)
        #expect(image.getWidth() == 32)
        #expect(image.getHeight() == 32)
    }

    @Test func decodesGrayscaleWithAlpha() throws {
        let png = try decode("BASN4A08")
        #expect(png.getWidth() == 32)
        #expect(png.getHeight() == 32)
        #expect(png.getColorType() == 4)
        #expect(png.getBitDepth() == 8)
        let gray = try TestSupport.inflate(png.getData())
        #expect(gray.count == 1024)
        #expect(TestSupport.crc32(gray) == "bfc7e22b")
        let alpha = try TestSupport.inflate(png.getAlpha())
        #expect(alpha.count == 1024)
        #expect(TestSupport.crc32(alpha) == "fa6029ad")
    }

    @Test func anImageFromAGrayscalePngWithAlphaIsGrayWithASoftMask() throws {
        let memory = MemoryPDF()
        let image = try Image(memory.pdf, TestSupport.open("PngSuite/BASN4A08.PNG"), ImageType.PNG)
        _ = image.drawOn(Page(memory.pdf, Letter.PORTRAIT))
        try memory.pdf.complete()
        let raw = TestSupport.latin1(memory.bytes)
        #expect(raw.contains("DeviceGray"), "gray color space")
        #expect(raw.contains("/SMask"), "soft mask")
        #expect(!raw.contains("DeviceRGB"), "no RGB color space")
    }

    @Test func rejects16BitGrayscaleWithAlphaWithAMessage() {
        let error = #expect(throws: (any Error).self) { _ = try decode("BASN4A16") }
        #expect(TestSupport.message(error) == "Image with unsupported bit depth == 16")
    }

    @Test func rejectsInterlacedImagesWithAClearError() {
        let error = #expect(throws: (any Error).self) { _ = try decode("BASI0G08") }
        #expect(TestSupport.message(error) ==
                "Interlaced PNG images are not supported.\nConvert the image using OptiPNG:\noptipng -i0 -o7 myimage.png")
    }
}
