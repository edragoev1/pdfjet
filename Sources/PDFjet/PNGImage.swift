/**
 * PNGImage.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to embed PNG images in the PDF document.
 *
 * **Please note:** Interlaced images are not supported.
 * To convert an interlaced image to a non-interlaced image, use OptiPNG:
 *
 * ```
 * optipng -i0 -o7 myimage.png
 * ```
 */
class PNGImage {
    var w: Int = 0                      // Image width in pixels
    var h: Int = 0                      // Image height in pixels

    var iDAT = [UInt8]()                // The compressed data in the IDAT chunks
    var pLTE: [UInt8]?                  // The palette data
    var tRNS: [UInt8]?                  // The palette transparency data
    // The transparent color the tRNS chunk of a grayscale or truecolor image
    // names, as the ranges of a /Mask: the minimum and the maximum of each
    // component, both the value of the chunk, in the bits of the samples.
    private var colorKeyMask: [Int]?

    var deflatedImageData = [UInt8]()   // The deflated image data
    var deflatedAlphaData = [UInt8]()   // The deflated alpha channel data

    private var bitDepth = 8
    private var colorType = 0

    // The size the pHYs chunk gives the image, in points, or 0 when it gives
    // none: the chunk holds the pixels per unit of each axis, and only unit 1,
    // the metre, is a physical size. Unit 0 is the ratio of the two axes with
    // no size to it, so the image keeps the size of its pixels.
    private var physicalWidth: Float = 0.0
    private var physicalHeight: Float = 0.0

    // A metre is 72/0.0254 points.
    private static let POINTS_PER_METER = 72.0/0.0254

    /**
     * Used to embed PNG images in the PDF document.
     *
     */
    init(_ stream: InputStream) throws {
        var buffer = try readPNG(stream)
        let chunks = try processPNG(&buffer)
        var hasImageData = false
        for chunk in chunks {
            let chunkType = String(decoding: chunk.type!, as: UTF8.self)
            if chunkType == "IHDR" {
                if chunk.getData()!.count != 13 {
                    throw PDFjetError(message: "Invalid PNG IHDR chunk.")
                }
                self.w = Int(getUInt32(chunk.getData()!, 0))    // Width
                self.h = Int(getUInt32(chunk.getData()!, 4))    // Height
                self.bitDepth = Int(chunk.getData()![8])        // Bit Depth
                self.colorType = Int(chunk.getData()![9])       // Color Type
                // Only the deflate compression method and the adaptive filter
                // method are defined, and libpng refuses a file of another
                // one, where the rows of this one would be read as if it were 0.
                if chunk.getData()![10] != 0 {
                    throw PDFjetError(message: "Unknown PNG compression method.")
                }
                if chunk.getData()![11] != 0 {
                    throw PDFjetError(message: "Unknown PNG filter method.")
                }
                if chunk.getData()![12] == 1 {
                    throw PDFjetError(message: "Interlaced PNG images are not supported.\n" +
                            "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png")
                }
            } else if chunkType == "IDAT" {
                iDAT.append(contentsOf: chunk.getData()!)
                hasImageData = true
            } else if chunkType == "PLTE" {
                // 1 to 256 colors of 3 bytes each.
                let colors = chunk.getData()!
                if colors.count % 3 != 0 || colors.isEmpty || colors.count > 3*256 {
                    throw PDFjetError(message: "Incorrect palette length.")
                }
                // An index past the colors of the palette is drawn black, as
                // libpng and browsers draw it.
                pLTE = colors + [UInt8](repeating: 0, count: 3*256 - colors.count)
            } else if chunkType == "tRNS" {
                if colorType == 3 {
                    tRNS = chunk.getData()
                } else if colorType == 0 || colorType == 2 {
                    colorKeyMask = colorKeyMaskOf(chunk.getData()!)
                }
            } else if chunkType == "pHYs" {
                readPhysicalSize(chunk.getData()!)
            }
            // The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
            // ports: the samples are embedded as they are.
        }

        let imageDataLength = try getImageDataLength()
        if !hasImageData {
            throw PDFjetError(message: "The PNG image has no image data.")
        }
        // The rows of the image; data after the last row is ignored.
        let inflatedImageData = try inflatePrefix(iDAT, imageDataLength)
        if inflatedImageData.count < imageDataLength {
            throw PDFjetError(message: "The PNG image data is shorter than the image.")
        }

        let image: [UInt8]                  // The image data
        if colorType == 0 {
            // Grayscale Image
            if bitDepth == 16 {
                image = try getImageColorType0BitDepth16(inflatedImageData)
            } else if bitDepth == 8 {
                image = try getImageColorType0BitDepth8(inflatedImageData)
            } else if bitDepth == 4 || bitDepth == 2 || bitDepth == 1 {
                image = try getImageColorType0BitDepthBelow8(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 4 {
            // Grayscale image with alpha
            if bitDepth == 8 {
                image = try getImageColorType4BitDepth8(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 6 {
            if bitDepth == 8 {
                image = try getImageColorType6BitDepth8(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 2 {
            // True color image; a PLTE chunk in it is only a suggested palette
            if bitDepth == 16 {
                image = try getImageColorType2BitDepth16(inflatedImageData)
            } else {
                image = try getImageColorType2BitDepth8(inflatedImageData)
            }
        } else {
            // Indexed image
            image = try getImageColorType3(inflatedImageData)
        }

        FlateEncode(&deflatedImageData, image)
    }

    // Reads the size the image asks to be drawn at from the pHYs chunk: the
    // pixels per unit of each axis and the unit they are in. A chunk of
    // another length, another unit or no pixels at all gives no size, and the
    // image keeps the size of its pixels; so does one whose size is too large
    // for a PDF number.
    private func readPhysicalSize(_ data: [UInt8]) {
        if data.count != 9 || data[8] != 1 {
            return
        }
        let pixelsPerMeterX = getUInt32(data, 0)
        let pixelsPerMeterY = getUInt32(data, 4)
        // The PNG specification limits the pixels per unit to 2^31 - 1, as
        // the four byte numbers of all of its chunks.
        if pixelsPerMeterX == 0 || pixelsPerMeterY == 0 ||
                pixelsPerMeterX > UInt32(Int32.max) || pixelsPerMeterY > UInt32(Int32.max) {
            return
        }
        let width = Float(Double(self.w)*PNGImage.POINTS_PER_METER/Double(pixelsPerMeterX))
        let height = Float(Double(self.h)*PNGImage.POINTS_PER_METER/Double(pixelsPerMeterY))
        if FastFloat.isWritable(width) && FastFloat.isWritable(height) {
            self.physicalWidth = width
            self.physicalHeight = height
        }
    }

    /// Returns the width in points the pHYs chunk asks for, or 0 when the
    /// image has no pHYs chunk with a size in it.
    func getPhysicalWidth() -> Float {
        return self.physicalWidth
    }

    /// Returns the height in points the pHYs chunk asks for, or 0 when the
    /// image has no pHYs chunk with a size in it.
    func getPhysicalHeight() -> Float {
        return self.physicalHeight
    }

    /// Returns the image width.
    func getWidth() -> Int {
        return self.w
    }

    /// Returns the image height.
    func getHeight() -> Int {
        return self.h
    }

    /// Returns the PNG color type.
    func getColorType() -> Int {
        return self.colorType
    }

    /// Returns the bit depth.
    func getBitDepth() -> Int {
        return self.bitDepth
    }

    /// Returns the compressed image data.
    func getData() -> [UInt8] {
        return self.deflatedImageData
    }

    // Returns the /Mask of the transparent color the tRNS chunk of a grayscale
    // or truecolor image names: a sample of 2 bytes for each component, of
    // which the bits of the image are read, as libpng reads it, so that 255
    // in an image of 1 bit is white. A chunk of another length gives none.
    private func colorKeyMaskOf(_ data: [UInt8]) -> [Int]? {
        let components = (colorType == 0) ? 1 : 3
        if data.count != 2 * components {
            return nil
        }
        let maxSample = (1 << bitDepth) - 1
        var mask = [Int]()
        for i in 0..<components {
            let sample = (Int(data[2 * i]) << 8 | Int(data[2 * i + 1])) & maxSample
            mask.append(sample)
            mask.append(sample)
        }
        return mask
    }

    /// Returns the /Mask of the transparent color of a grayscale or truecolor
    /// image, the ranges of its components, or nil when it has none.
    func getColorKeyMask() -> [Int]? {
        return colorKeyMask
    }

    /// Returns the compressed alpha channel data.
    func getAlpha() -> [UInt8]? {
        // Deflated data is never empty, so no bytes means no alpha channel.
        return self.deflatedAlphaData.isEmpty ? nil : self.deflatedAlphaData
    }

    // Checks the size, the bit depth and the color type of the IHDR chunk, and
    // returns the length of the decompressed image data: each row is a filter
    // type byte and the packed samples. The size comes from the file, so it is
    // checked, without overflow, before any buffer is allocated for the image.
    private func getImageDataLength() throws -> Int {
        if w <= 0 || h <= 0 || w > Int(Int32.max) || h > Int(Int32.max) {
            throw PDFjetError(message: "Invalid PNG image size.")
        }
        let channels: Int
        let validBitDepths: [Int]
        switch colorType {
        case 0:
            channels = 1
            validBitDepths = [1, 2, 4, 8, 16]
        case 2:
            channels = 3
            validBitDepths = [8, 16]
        case 3:
            channels = 1
            validBitDepths = [1, 2, 4, 8]
        case 4:
            channels = 2
            validBitDepths = [8, 16]
        case 6:
            channels = 4
            validBitDepths = [8, 16]
        default:
            throw PDFjetError(message: "Invalid PNG color type \(colorType).")
        }
        if !validBitDepths.contains(bitDepth) {
            throw PDFjetError(message: "Invalid PNG bit depth \(bitDepth) for color type \(colorType).")
        }
        if colorType == 3 && pLTE == nil {
            throw PDFjetError(message: "The PNG palette image has no PLTE chunk.")
        }
        let tooLarge = PDFjetError(message: "The PNG image is larger than \(MAX_DECODED_LENGTH) bytes.")
        let (bits, bitsOverflow) = w.multipliedReportingOverflow(by: channels * bitDepth)
        if bitsOverflow {
            throw tooLarge
        }
        let bytesPerRow = bits / 8 + (bits % 8 == 0 ? 0 : 1)
        let (length, lengthOverflow) = h.multipliedReportingOverflow(by: 1 + bytesPerRow)
        if lengthOverflow || length > MAX_DECODED_LENGTH {
            throw tooLarge
        }
        if colorType == 3 {
            // A palette image becomes 3 bytes of RGB and 1 byte of alpha per pixel.
            let (pixels, pixelsOverflow) = w.multipliedReportingOverflow(by: h)
            if pixelsOverflow || pixels > MAX_DECODED_LENGTH / 4 {
                throw tooLarge
            }
        }
        return length
    }

    private func readPNG(_ stream: InputStream) throws -> [UInt8] {
        let contents = try Content.getFromStream(stream)
        if contents.count < 8 {
            throw PDFjetError(message: "Unexpected end of the PNG stream.")
        }
        if contents[0] == 0x89 &&
                contents[1] == 0x50 &&
                contents[2] == 0x4E &&
                contents[3] == 0x47 &&
                contents[4] == 0x0D &&
                contents[5] == 0x0A &&
                contents[6] == 0x1A &&
                contents[7] == 0x0A {
            // The PNG signature is correct.
        } else {
            throw PDFjetError(message: "Wrong PNG signature.")
        }
        return contents
    }

    private func processPNG(
            _ buffer: inout [UInt8]) throws -> [Chunk] {
        var chunks = [Chunk]()
        var offset = 8      // Skip the header!
        while true {
            let chunk = try getChunk(&buffer, &offset)
            let chunkType = String(decoding: chunk.type!, as: UTF8.self)
            if chunkType == "IEND" {
                break
            }
            chunks.append(chunk)
        }
        return chunks
    }

    private func getChunk(
            _ buffer: inout [UInt8],
            _ offset: inout Int) throws -> Chunk {
        let chunk = Chunk()

        // The length of the data chunk.
        chunk.length = getUInt32(try getBytes(&buffer, &offset, 4), 0)
        if chunk.length! > UInt32(Int32.max) {
            throw PDFjetError(message: "Invalid PNG chunk length \(chunk.length!).")
        }

        // The chunk type.
        chunk.type = try getBytes(&buffer, &offset, 4)

        // The chunk data.
        chunk.data = try getBytes(&buffer, &offset, Int(chunk.length!))

        // CRC of the type and data chunks.
        chunk.crc = getUInt32(try getBytes(&buffer, &offset, 4), 0)

        let crc = CRC32()
        crc.update(chunk.type!, 0, 4)
        crc.update(chunk.data!, 0, Int(chunk.length!))
        if crc.getValue() != chunk.crc {
            throw PDFjetError(message: "Chunk has bad CRC.")
        }

        return chunk
    }

    private func getBytes(
            _ buffer: inout [UInt8],
            _ offset: inout Int,
            _ length: Int) throws -> [UInt8] {
        // The file is in memory, so a length that it does not have fails here,
        // before anything is allocated for it.
        if length > buffer.count - offset {
            throw PDFjetError(message: "Unexpected end of the PNG stream.")
        }
        let bytes = Array(buffer[offset..<(offset + length)])
        offset += length
        return bytes
    }

    private func getUInt32(
            _ buf: [UInt8],
            _ off: Int) -> UInt32 {
        var value = UInt32(buf[off]) << 24
        value |= UInt32(buf[off + 1]) << 16
        value |= UInt32(buf[off + 2]) << 8
        value |= UInt32(buf[off + 3])
        return value
    }

    // Returns the rows of the image data without the filter type byte each
    // begins with, and with their filters undone. A row holds bytesPerRow bytes
    // after the filter type, and the byte of the pixel on the left is
    // bytesPerPixel bytes before, or the byte before for pixels smaller than a
    // byte. It throws on a filter type that PNG does not define, as libpng does.
    private func unfilter(
            _ buf: [UInt8],
            _ rows: Int,
            _ bytesPerRow: Int,
            _ bytesPerPixel: Int) throws -> [UInt8] {
        var image = [UInt8](repeating: 0, count: rows * bytesPerRow)
        var badFilter: UInt8?
        buf.withUnsafeBufferPointer { src in
            image.withUnsafeMutableBufferPointer { dst in
                for row in 0..<rows {
                    let offset = row * (bytesPerRow + 1)
                    let filter = src[offset]
                    let line = row * bytesPerRow        // The row
                    let prior = line - bytesPerRow      // The row above, none for the first row
                    for i in 0..<bytesPerRow {
                        dst[line + i] = src[offset + 1 + i]
                    }
                    if filter == 0x00 {                 // None
                    } else if filter == 0x01 {          // Sub
                        if bytesPerPixel < bytesPerRow {
                            for i in bytesPerPixel..<bytesPerRow {
                                dst[line + i] = dst[line + i] &+ dst[line + i - bytesPerPixel]
                            }
                        }
                    } else if filter == 0x02 {          // Up
                        if row > 0 {
                            for i in 0..<bytesPerRow {
                                dst[line + i] = dst[line + i] &+ dst[prior + i]
                            }
                        }
                    } else if filter == 0x03 {          // Average
                        for i in 0..<bytesPerRow {
                            let a = i >= bytesPerPixel ? Int(dst[line + i - bytesPerPixel]) : 0
                            let b = row > 0 ? Int(dst[prior + i]) : 0
                            dst[line + i] = dst[line + i] &+ UInt8((a + b) / 2)
                        }
                    } else if filter == 0x04 {          // Paeth
                        for i in 0..<bytesPerRow {
                            // The bytes on the left, above and above on the left
                            let a = i >= bytesPerPixel ? Int(dst[line + i - bytesPerPixel]) : 0
                            let b = row > 0 ? Int(dst[prior + i]) : 0
                            let c = row > 0 && i >= bytesPerPixel ? Int(dst[prior + i - bytesPerPixel]) : 0
                            dst[line + i] = dst[line + i] &+ UInt8(PNGImage.paeth(a, b, c))
                        }
                    } else {
                        badFilter = filter
                        return
                    }
                }
            }
        }
        if let filter = badFilter {
            throw PDFjetError(message: "Invalid PNG filter type \(filter).")
        }
        return image
    }

    // Returns whichever of the bytes on the left, above and above on the left
    // is nearest to their sum less the one above on the left.
    private static func paeth(_ a: Int, _ b: Int, _ c: Int) -> Int {
        let pa = abs(b - c)         // p - a, where p = a + b - c
        let pb = abs(a - c)         // p - b
        let pc = abs(a + b - 2*c)   // p - c
        if pa <= pb && pa <= pc {
            return a
        } else if pb <= pc {
            return b
        }
        return c
    }

    // Truecolor Image with Bit Depth == 16
    private func getImageColorType2BitDepth16(_ buf: [UInt8]) throws -> [UInt8] {
        return try unfilter(buf, self.h, 6 * self.w, 6)
    }

    // Truecolor Image with Bit Depth == 8
    private func getImageColorType2BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        return try unfilter(buf, self.h, 3 * self.w, 3)
    }

    // Truecolor Image with Alpha Transparency
    // The gray samples are the image and the alpha samples its soft mask.
    private func getImageColorType4BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        let image = try unfilter(buf, self.h, 2 * self.w, 2)
        var gray = [UInt8](repeating: 0, count: self.w * self.h)
        var alpha = [UInt8](repeating: 0, count: self.w * self.h)
        for i in 0..<gray.count {
            gray[i] = image[2 * i]
            alpha[i] = image[2 * i + 1]
        }
        FlateEncode(&deflatedAlphaData, alpha)
        return gray
    }

    private func getImageColorType6BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        let image = try unfilter(buf, self.h, 4 * self.w, 4)
        var idata = [UInt8](repeating: 0, count: (3 * self.w * self.h))   // Image data
        var alpha = [UInt8](repeating: 0, count: (self.w * self.h))       // Alpha values
        for i in 0..<alpha.count {
            idata[3 * i]     = image[4 * i]
            idata[3 * i + 1] = image[4 * i + 1]
            idata[3 * i + 2] = image[4 * i + 2]
            alpha[i]         = image[4 * i + 3]
        }
        FlateEncode(&deflatedAlphaData, alpha)
        return idata
    }

    // Indexed-color image with bit depth == 1, 2, 4 or 8
    // Each value is a palette index; a PLTE chunk shall appear.
    // The filters are undone on the packed indexes, one byte per pixel whatever
    // the bit depth, before the indexes are looked up in the palette.
    private func getImageColorType3(_ buf: [UInt8]) throws -> [UInt8] {
        let bytesPerLine = (self.w * self.bitDepth + 7) / 8
        let indexes = try unfilter(buf, self.h, bytesPerLine, 1)

        var image = [UInt8](repeating: 0x00, count: 3*self.w*self.h)
        var alpha: [UInt8]?
        if tRNS != nil {
            alpha = [UInt8](repeating: 0xFF, count: self.w*self.h)
        }
        let mask = (1 << self.bitDepth) - 1
        var n = 0
        var j = 0
        for row in 0..<self.h {
            for col in 0..<self.w {
                let bit = col * self.bitDepth
                let b = Int(indexes[row * bytesPerLine + bit / 8])
                let k = (b >> (8 - self.bitDepth - bit % 8)) & mask
                if tRNS != nil && k < tRNS!.count {
                    alpha![n] = tRNS![k]
                }
                n += 1
                image[j] = pLTE![3*k]
                j += 1
                image[j] = pLTE![3*k + 1]
                j += 1
                image[j] = pLTE![3*k + 2]
                j += 1
            }
        }
        if tRNS != nil {
            FlateEncode(&deflatedAlphaData, alpha!)
        }
        return image
    }

    // Grayscale Image with Bit Depth == 16
    private func getImageColorType0BitDepth16(_ buf: [UInt8]) throws -> [UInt8] {
        return try unfilter(buf, self.h, 2 * self.w, 2)
    }

    // Grayscale Image with Bit Depth == 8
    private func getImageColorType0BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        return try unfilter(buf, self.h, self.w, 1)
    }

    // Grayscale image with 1, 2 or 4 bits per pixel. The filters work on
    // bytes, one byte per pixel whatever the bit depth.
    private func getImageColorType0BitDepthBelow8(_ buf: [UInt8]) throws -> [UInt8] {
        return try unfilter(buf, self.h, (self.w * self.bitDepth + 7) / 8, 1)
    }
}   // End of PNGImage.swift
