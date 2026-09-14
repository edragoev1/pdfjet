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
                #expect(png.getAlpha() == nil, "\(name)")
            } else {
                let alpha = try TestSupport.inflate(try #require(png.getAlpha()))
                #expect(alpha.count == row.7, "\(name)")
                #expect(TestSupport.crc32(alpha) == row.8, "\(name)")
            }
        }
    }

    @Test func truecolorTransparencyIsIgnored() throws {
        // tRNS applies to palette images only; TBRN2C08 has the samples of TP1N3P08.
        let png = try decode("TBRN2C08")
        #expect(TestSupport.crc32(try TestSupport.inflate(png.getData())) == "8b0a6c2c")
        #expect(png.getAlpha() == nil)
    }

    @Test func rejects16BitRgbaWithAMessage() {
        let error = #expect(throws: (any Error).self) { _ = try decode("BASN6A16") }
        #expect(TestSupport.message(error) == "Image with unsupported bit depth == 16")
    }

    @Test func rejectsDataThatIsNotAPng() {
        #expect(throws: (any Error).self) { _ = try PNGImage(InputStream(data: Data("not a png file".utf8))) }
    }

    @Test func anImageFromAPngHasItsSize() throws {
        let image = try Image(TestSupport.newPDF(), TestSupport.open("PngSuite/BASN2C08.PNG"))
        #expect(image.getWidth() == 32)
        #expect(image.getHeight() == 32)
    }

    private static func bigEndian32(_ value: UInt32, _ bytes: inout [UInt8]) {
        bytes.append(contentsOf: [
            UInt8(value >> 24), UInt8((value >> 16) & 0xFF), UInt8((value >> 8) & 0xFF), UInt8(value & 0xFF)])
    }

    // A PNG file with the IHDR of the size, bit depth and color type, a PLTE
    // chunk when there is a palette, and one IDAT chunk.
    private func png(
            _ width: Int32, _ height: Int32, _ bitDepth: UInt8, _ colorType: UInt8,
            _ palette: [UInt8]?, _ idat: [UInt8]?) -> [UInt8] {
        var bytes: [UInt8] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        var ihdr = [UInt8]()
        PNGImageTests.bigEndian32(UInt32(bitPattern: width), &ihdr)
        PNGImageTests.bigEndian32(UInt32(bitPattern: height), &ihdr)
        ihdr.append(contentsOf: [bitDepth, colorType, 0, 0, 0])
        chunk(&bytes, "IHDR", ihdr)
        if let palette = palette {
            chunk(&bytes, "PLTE", palette)
        }
        if let idat = idat {
            chunk(&bytes, "IDAT", idat)
        }
        chunk(&bytes, "IEND", [])
        return bytes
    }

    private func chunk(_ bytes: inout [UInt8], _ type: String, _ data: [UInt8]) {
        let name = Array(type.utf8)
        PNGImageTests.bigEndian32(UInt32(data.count), &bytes)
        bytes.append(contentsOf: name)
        bytes.append(contentsOf: data)
        PNGImageTests.bigEndian32(UInt32(TestSupport.crc32(name + data), radix: 16)!, &bytes)
    }

    private func decodeError(_ png: [UInt8], sourceLocation: SourceLocation = #_sourceLocation) -> String {
        let error = #expect(throws: (any Error).self, sourceLocation: sourceLocation) {
            _ = try PNGImage(InputStream(data: Data(png)))
        }
        return TestSupport.message(error)
    }

    /// An input stream that returns at most 3 bytes per read.
    private final class SlowStream: InputStream {
        override func read(_ buffer: UnsafeMutablePointer<UInt8>, maxLength len: Int) -> Int {
            return super.read(buffer, maxLength: min(len, 3))
        }
    }

    @Test func decodesTheRowsOfTheImageAndIgnoresDataAfterThem() throws {
        let rgb: [UInt8] = [1, 2, 3, 4, 5, 6]
        let rows: [UInt8] = [0, 1, 2, 3, 4, 5, 6]
        let longer: [UInt8] = [0, 1, 2, 3, 4, 5, 6, 9, 9, 9]
        for data in [rows, longer] {
            let image = try PNGImage(InputStream(data: Data(png(2, 1, 8, 2, nil, TestSupport.deflate(data)))))
            #expect(try TestSupport.inflate(image.getData()) == rgb)
        }
    }

    @Test func rejectsImageDataShorterThanTheImage() {
        let rows: [UInt8] = [0, 1, 2, 3]
        #expect(decodeError(png(2, 1, 8, 2, nil, TestSupport.deflate(rows)))
                == "The PNG image data is shorter than the image.")
    }

    @Test func rejectsAnImageLargerThanTheLimitBeforeDecodingIt() {
        // 20000 x 20000 RGB samples are 1.2 GB.
        #expect(decodeError(png(20000, 20000, 8, 2, nil, TestSupport.deflate([0])))
                == "The PNG image is larger than 268435456 bytes.")
        // 10000 x 10000 palette indexes of 1 bit are 12.5 MB, but 400 MB of RGB and alpha.
        #expect(decodeError(png(10000, 10000, 1, 3, [UInt8](repeating: 0, count: 6), TestSupport.deflate([0])))
                == "The PNG image is larger than 268435456 bytes.")
        // The largest size, where the sizes multiplied overflow a 64-bit integer.
        #expect(decodeError(png(2147483647, 2147483647, 16, 6, nil, TestSupport.deflate([0])))
                == "The PNG image is larger than 268435456 bytes.")
        #expect(decodeError(png(2147483647, 2147483647, 1, 3, [UInt8](repeating: 0, count: 6), TestSupport.deflate([0])))
                == "The PNG image is larger than 268435456 bytes.")
        #expect(decodeError(png(2147483647, 1, 16, 6, nil, TestSupport.deflate([0])))
                == "The PNG image is larger than 268435456 bytes.")
    }

    @Test func rejectsAnInvalidSizeBitDepthColorTypeOrPalette() {
        let idat = TestSupport.deflate([0, 0, 0, 0])
        #expect(decodeError(png(0, 1, 8, 2, nil, idat)) == "Invalid PNG image size.")
        #expect(decodeError(png(-1, 1, 8, 2, nil, idat)) == "Invalid PNG image size.")
        #expect(decodeError(png(1, 1, 4, 2, nil, idat)) == "Invalid PNG bit depth 4 for color type 2.")
        #expect(decodeError(png(1, 1, 8, 5, nil, idat)) == "Invalid PNG color type 5.")
        #expect(decodeError(png(1, 1, 8, 200, nil, idat)) == "Invalid PNG color type 200.")
        #expect(decodeError(png(1, 1, 200, 2, nil, idat)) == "Invalid PNG bit depth 200 for color type 2.")
        #expect(decodeError(png(1, 1, 8, 3, nil, idat)) == "The PNG palette image has no PLTE chunk.")
        #expect(decodeError(png(1, 1, 8, 2, nil, nil)) == "The PNG image has no image data.")
    }

    @Test func rejectsAChunkLengthThatTheFileDoesNotHave() {
        let valid = png(1, 1, 8, 2, nil, TestSupport.deflate([0, 0, 0, 0]))
        // The IDAT chunk starts after the signature and the 25 bytes of the IHDR chunk.
        var lying = Array(valid.prefix(33 + 18))
        lying[33] = 0x7F
        lying[34] = 0xFF
        lying[35] = 0xFF
        lying[36] = 0xF0
        #expect(decodeError(lying) == "Unexpected end of the PNG stream.")
    }

    @Test func readsAStreamThatReturnsFewBytesAtATime() throws {
        let data = try Data(contentsOf: URL(fileURLWithPath: TestSupport.path("PngSuite/BASN2C08.PNG")))
        let image = try PNGImage(SlowStream(data: data))
        #expect(TestSupport.crc32(try TestSupport.inflate(image.getData())) == "7855b9bf")
    }

    @Test func aTruecolorImageWithASuggestedPaletteIsDecodedAsTruecolor() throws {
        let image = try decode("PS1N2C16")
        #expect(image.getColorType() == 2)
        #expect(try TestSupport.inflate(image.getData()).count == 6144)
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
        let alpha = try TestSupport.inflate(try #require(png.getAlpha()))
        #expect(alpha.count == 1024)
        #expect(TestSupport.crc32(alpha) == "fa6029ad")
    }

    @Test func anImageFromAGrayscalePngWithAlphaIsGrayWithASoftMask() throws {
        let memory = MemoryPDF()
        let image = try Image(memory.pdf, TestSupport.open("PngSuite/BASN4A08.PNG"))
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
