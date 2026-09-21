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
        return InputStream(data: Data(bytes))
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

    // The 54 byte header of a BMP file of the size, bits per pixel and palette colors.
    private func header(_ width: Int32, _ height: Int32, _ bitsPerPixel: UInt16, _ colors: Int32) -> [UInt8] {
        var bytes: [UInt8] = [0x42, 0x4D]
        littleEndian32(54, &bytes)
        littleEndian32(0, &bytes)
        littleEndian32(54, &bytes)
        littleEndian32(40, &bytes)
        littleEndian32(width, &bytes)
        littleEndian32(height, &bytes)
        bytes.append(contentsOf: [1, 0, UInt8(bitsPerPixel & 0xFF), UInt8(bitsPerPixel >> 8)])
        littleEndian32(0, &bytes)
        littleEndian32(0, &bytes)
        littleEndian32(2835, &bytes)
        littleEndian32(2835, &bytes)
        littleEndian32(colors, &bytes)
        littleEndian32(0, &bytes)
        return bytes
    }

    private func decodeError(_ bmp: [UInt8], sourceLocation: SourceLocation = #_sourceLocation) -> String {
        let error = #expect(throws: (any Error).self, sourceLocation: sourceLocation) {
            _ = try BMPImage(stream(bmp))
        }
        return TestSupport.message(error)
    }

    @Test func rejectsAnInvalidSize() {
        #expect(decodeError(header(0, 2, 24, 0)) == "Invalid BMP image size.")
        #expect(decodeError(header(2, 0, 24, 0)) == "Invalid BMP image size.")
        #expect(decodeError(header(-2, 2, 24, 0)) == "Invalid BMP image size.")
        #expect(decodeError(header(2, Int32.min, 24, 0)) == "Invalid BMP image size.")
    }

    @Test func rejectsAnImageLargerThanTheLimitBeforeReadingIt() {
        // 20000 x 20000 pixels are 1.2 GB of RGB; the file has only the header.
        #expect(decodeError(header(20000, 20000, 24, 0)) == "The BMP image is larger than 268435456 bytes.")
        // The largest size, where the sizes multiplied overflow a 64-bit integer.
        #expect(decodeError(header(Int32.max, Int32.max, 32, 0)) == "The BMP image is larger than 268435456 bytes.")
        #expect(decodeError(header(Int32.max, 1, 32, 0)) == "The BMP image is larger than 268435456 bytes.")
    }

    @Test func rejectsAnUnsupportedBitDepthOrALargePalette() {
        #expect(decodeError(header(2, 2, 2, 0)) == "Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images")
        #expect(decodeError(header(2, 2, 8, 0x7FFFFFFF)) == "Invalid BMP palette size 2147483647.")
    }

    @Test func aTruncatedFileThrows() {
        let truncated = Array(bmp24(false).prefix(60))
        #expect(throws: (any Error).self) { _ = try BMPImage(stream(truncated)) }
    }

    // A BMP file of the 2 by 2 pixels with a header of the size, 40 bytes or
    // more, the bits per pixel, the compression, the masks after the first 40
    // bytes of the header, the palette of 0xRRGGBB colors, and the pixels of
    // the bottom row and then the top row, each padded to 4 bytes.
    private func bmp(_ headerSize: Int, _ bitsPerPixel: Int, _ compression: Int,
            _ masks: [UInt32]?, _ palette: [UInt32]?, _ bottomRow: [UInt8], _ topRow: [UInt8]) -> [UInt8] {
        let colors = palette?.count ?? 0
        let afterHeader = (headerSize == 40 && masks != nil) ? 12 : 0
        let offset = 14 + headerSize + afterHeader + 4 * colors
        let rowSize = (bottomRow.count + 3) & ~3
        var bytes: [UInt8] = [0x42, 0x4D]
        littleEndian32(Int32(offset + 2 * rowSize), &bytes)
        littleEndian32(0, &bytes)
        littleEndian32(Int32(offset), &bytes)
        littleEndian32(Int32(headerSize), &bytes)
        littleEndian32(2, &bytes)
        littleEndian32(2, &bytes)
        bytes.append(contentsOf: [1, 0, UInt8(bitsPerPixel), 0])
        for value in [compression, 2 * rowSize, 2835, 2835, colors, 0] {
            littleEndian32(Int32(value), &bytes)
        }
        for mask in masks ?? [] {
            littleEndian32(Int32(bitPattern: mask), &bytes)
        }
        // The rest of a larger header is 0
        bytes.append(contentsOf: [UInt8](repeating: 0, count: offset - 4 * colors - bytes.count))
        for color in palette ?? [] {
            bytes.append(contentsOf: [UInt8(color & 0xFF), UInt8((color >> 8) & 0xFF), UInt8(color >> 16), 0])
        }
        for row in [bottomRow, topRow] {
            bytes.append(contentsOf: row + [UInt8](repeating: 0, count: rowSize - row.count))
        }
        return bytes
    }

    private func decode(_ bmp: [UInt8]) throws -> [UInt8] {
        return try TestSupport.inflate(BMPImage(stream(bmp)).getData())
    }

    // The pixels as little endian 16 bit values.
    private func shorts(_ pixels: Int...) -> [UInt8] {
        return pixels.flatMap { [UInt8($0 & 0xFF), UInt8($0 >> 8)] }
    }

    @Test func aThirtyTwoBitPixelIsBlueGreenRedAndAByteThatIsNotAColor() throws {
        let bmp = bmp(40, 32, 0, nil, nil, [255, 0, 0, 0, 255, 255, 255, 0], [0, 0, 255, 0, 0, 255, 0, 0])
        #expect(try decode(bmp) == BMPImageTests.rgb)
    }

    @Test func theMasksOfSixteenAndThirtyTwoBitPixelsAreRead() throws {
        // 5 bits a color without masks; 31 of 5 bits is 255.
        #expect(try decode(bmp(40, 16, 0, nil, nil,
                shorts(0x001F, 0x7FFF), shorts(0x7C00, 0x03E0))) == BMPImageTests.rgb)
        // 5, 6 and 5 bits.
        #expect(try decode(bmp(40, 16, 3, [0xF800, 0x07E0, 0x001F], nil,
                shorts(0x001F, 0xFFFF), shorts(0xF800, 0x07E0))) == BMPImageTests.rgb)
        // Red in the low byte, in a 108 byte header.
        #expect(try decode(bmp(108, 32, 3, [0x000000FF, 0x0000FF00, 0x00FF0000], nil,
                [0, 0, 255, 0, 255, 255, 255, 0], [255, 0, 0, 0, 0, 255, 0, 0])) == BMPImageTests.rgb)
    }

    @Test func thePaletteFollowsAHeaderOfAnySize() throws {
        let palette: [UInt32] = [0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF]
        #expect(try decode(bmp(40, 8, 0, nil, palette, [2, 3], [0, 1])) == BMPImageTests.rgb)
        #expect(try decode(bmp(124, 8, 0, nil, palette, [2, 3], [0, 1])) == BMPImageTests.rgb)
    }

    @Test func rejectsACompressedImage() {
        let palette: [UInt32] = [0xFF0000, 0x00FF00, 0x0000FF, 0xFFFFFF]
        // RLE8: a run of 1 pixel of color 2, 1 of color 3, the end of the bitmap
        #expect(decodeError(bmp(40, 8, 1, nil, palette, [1, 2, 1, 3], [0, 1, 0, 0]))
                == "Compressed BMP images are not supported.")
    }

    @Test func aPaletteIndexPastThePaletteIsBlack() throws {
        // A palette of 2 colors, and the indexes 1 and 5 in the top row.
        let image = bmp(40, 8, 0, nil, [0x102030, 0x405060], [0, 0], [1, 5])
        #expect(try decode(image) == [0x40, 0x50, 0x60, 0, 0, 0, 0x10, 0x20, 0x30, 0x10, 0x20, 0x30])
    }

    @Test func theLastRowCanBeWithoutItsPadding() throws {
        let image = bmp(40, 24, 0, nil, nil, [1, 2, 3, 4, 5, 6], [7, 8, 9, 10, 11, 12])
        // The 2 bytes of padding of the top row, which is the last one.
        #expect(try decode(Array(image.dropLast(2))) == decode(image))
        #expect(throws: (any Error).self) { _ = try decode(Array(image.dropLast(3))) }
    }
    @Test func thePixelsPerMeterOfTheHeaderGiveTheSizeTheImageIsDrawnAt() throws {
        // The header of a BMP holds the pixels per metre of each axis.
        // palette.bmp is 100 by 100 pixels at 4724 per metre, which is 120
        // dots per inch and 60 by 60 points.
        let bmp = try BMPImage(TestSupport.open("images/palette.bmp"))
        #expect(bmp.getWidth() == 100)
        #expect(bmp.getHeight() == 100)
        #expect(abs(bmp.getPhysicalWidth() - 60.0) < 0.05)
        #expect(abs(bmp.getPhysicalHeight() - 60.0) < 0.05)

        let image = try Image(TestSupport.newPDF(), TestSupport.open("images/palette.bmp"))
        #expect(abs(image.getWidth() - 60.0) < 0.05, "the image is not drawn at the size it asks for")
        #expect(abs(image.getHeight() - 60.0) < 0.05, "the image is not drawn at the size it asks for")
    }

    @Test func aHeaderWithNoPixelsPerMeterLeavesTheSizeOfThePixels() throws {
        // Most writers leave the two fields at 0, which says nothing about
        // how large the image is, so it keeps one point for each of its
        // pixels. The header is built here because the files of the
        // repository both carry a resolution.
        var data = [UInt8](try Data(contentsOf: URL(fileURLWithPath:
                TestSupport.path("images/palette.bmp"))))
        for i in 38..<46 {
            data[i] = 0
        }
        let bmp = try BMPImage(InputStream(data: Data(data)))
        #expect(bmp.getPhysicalWidth() == 0.0)
        #expect(bmp.getPhysicalHeight() == 0.0)
    }

    @Test func rejectsAnOS2HeaderForItsSize() {
        // The 12 byte header of OS/2 1.x has a width and a height of 2 bytes,
        // so the bit depth is not where a header of 40 bytes has it: the
        // message says that the header is not supported, and not that the bit
        // depth is not. Here the bit depth is 8, with a palette of 3 bytes a
        // color.
        var bytes: [UInt8] = [0x42, 0x4D]
        for value in [26 + 6 + 4, 0, 26 + 6, 12] {
            littleEndian32(Int32(value), &bytes)
        }
        bytes.append(contentsOf: [2, 0, 2, 0, 1, 0, 8, 0])
        bytes.append(contentsOf: [0, 0, 0, 255, 255, 255])
        bytes.append(contentsOf: [0, 1, 0, 0, 1, 0, 0, 0])
        #expect(decodeError(bytes) == "Unsupported BMP header of 12 bytes.")
    }

    // The masks of 32 bit pixels of blue, green, red and alpha bytes.
    private static let masksBGRA: [UInt32] = [0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000]

    private func expectAlpha(_ want: [UInt8]?, _ bmp: [UInt8],
            sourceLocation: SourceLocation = #_sourceLocation) throws {
        let image = try BMPImage(stream(bmp))
        #expect(try TestSupport.inflate(image.getData()) == BMPImageTests.rgb, sourceLocation: sourceLocation)
        if let want = want {
            let alpha = try #require(image.getAlpha(), sourceLocation: sourceLocation)
            #expect(try TestSupport.inflate(alpha) == want, sourceLocation: sourceLocation)
        } else {
            #expect(image.getAlpha() == nil, sourceLocation: sourceLocation)
        }
    }

    @Test func theAlphaMaskOfAHeaderOf56BytesOrMoreIsRead() throws {
        // Blue and white of alpha 0x80 and 0xFF, red and green of 0 and 0x40.
        let bottom: [UInt8] = [255, 0, 0, 0x80, 255, 255, 255, 0xFF]
        let top: [UInt8] = [0, 0, 255, 0, 0, 255, 0, 0x40]
        try expectAlpha([0, 0x40, 0x80, 0xFF], bmp(124, 32, 3, BMPImageTests.masksBGRA, nil, bottom, top))
        try expectAlpha([0, 0x40, 0x80, 0xFF], bmp(56, 32, 3, BMPImageTests.masksBGRA, nil, bottom, top))
        // 4 bits of alpha, and of each color: 4 of 15 is 68.
        try expectAlpha([0, 68, 136, 255], bmp(108, 16, 3, [0x0F00, 0x00F0, 0x000F, 0xF000], nil,
                shorts(0x800F, 0xFFFF), shorts(0x0F00, 0x40F0)))
    }

    @Test func hasNoAlphaWithoutAnAlphaMaskOrWhenEveryPixelHasAnAlphaOf0() throws {
        let top: [UInt8] = [255, 0, 0, 0x12, 255, 255, 255, 0x34]
        let bottom: [UInt8] = [0, 0, 255, 0x56, 0, 255, 0, 0x78]
        let masks = Array(BMPImageTests.masksBGRA.prefix(3))
        // The fourth byte of a pixel without masks is not a color, and the
        // masks of a BI_RGB image, in a larger header, are not read.
        try expectAlpha(nil, bmp(40, 32, 0, nil, nil, top, bottom))
        try expectAlpha(nil, bmp(124, 32, 0, BMPImageTests.masksBGRA, nil, top, bottom))
        // The masks after a header of 40 bytes, and in one of 52, have no
        // alpha; nor has an alpha mask of 0.
        try expectAlpha(nil, bmp(40, 32, 3, masks, nil, top, bottom))
        try expectAlpha(nil, bmp(52, 32, 3, masks, nil, top, bottom))
        try expectAlpha(nil, bmp(124, 32, 3, [0xFF0000, 0xFF00, 0xFF, 0], nil, top, bottom))
        // Browsers draw an image whose alpha is 0 in every pixel opaque:
        // writers that do not know of the alpha leave it at 0.
        try expectAlpha(nil, bmp(124, 32, 3, BMPImageTests.masksBGRA, nil,
                [255, 0, 0, 0, 255, 255, 255, 0], [0, 0, 255, 0, 0, 255, 0, 0]))
    }

    @Test func theAlphaIsTheSoftMaskOfTheImage() throws {
        for alpha: UInt8 in [0x80, 0] {
            let image = bmp(124, 32, 3, BMPImageTests.masksBGRA, nil,
                    [255, 0, 0, alpha, 255, 255, 255, 0], [0, 0, 255, 0, 0, 255, 0, 0])
            let memory = MemoryPDF()
            let drawn = try Image(memory.pdf, InputStream(data: Data(image)))
            _ = drawn.drawOn(Page(memory.pdf, Letter.PORTRAIT))
            try memory.pdf.complete()
            let raw = TestSupport.latin1(memory.bytes)
            #expect(raw.contains("/SMask") == (alpha != 0), "alpha \(alpha): a soft mask")
        }
    }
}
