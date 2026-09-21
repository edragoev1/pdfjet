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

    var deflatedImageData = [UInt8]()   // The deflated image data
    var deflatedAlphaData = [UInt8]()   // The deflated alpha channel data

    private var bitDepth = 8
    private var colorType = 0

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
                }
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
                image = getImageColorType0BitDepth16(inflatedImageData)
            } else if bitDepth == 8 {
                image = getImageColorType0BitDepth8(inflatedImageData)
            } else if bitDepth == 4 {
                image = getImageColorType0BitDepth4(inflatedImageData)
            } else if bitDepth == 2 {
                image = getImageColorType0BitDepth2(inflatedImageData)
            } else if bitDepth == 1 {
                image = getImageColorType0BitDepth1(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 4 {
            // Grayscale image with alpha
            if bitDepth == 8 {
                image = getImageColorType4BitDepth8(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 6 {
            if bitDepth == 8 {
                image = getImageColorType6BitDepth8(inflatedImageData)
            } else {
                throw PDFjetError(message: "Image with unsupported bit depth == \(bitDepth)")
            }
        } else if colorType == 2 {
            // True color image; a PLTE chunk in it is only a suggested palette
            if bitDepth == 16 {
                image = getImageColorType2BitDepth16(inflatedImageData)
            } else {
                image = getImageColorType2BitDepth8(inflatedImageData)
            }
        } else {
            // Indexed image
            image = getImageColorType3(inflatedImageData)
        }

        FlateEncode(&deflatedImageData, image)
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

    // Truecolor Image with Bit Depth == 16
    private func getImageColorType2BitDepth16(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8]()
        let bytesPerLine = 6 * self.w + 1
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters.append(buf[i])
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 6)
        return image
    }

    // Truecolor Image with Bit Depth == 8
    private func getImageColorType2BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8]()
        let bytesPerLine = 3 * self.w + 1
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters.append(buf[i])
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 3)
        return image
    }

    // Truecolor Image with Alpha Transparency
    // The gray samples are the image and the alpha samples its soft mask.
    private func getImageColorType4BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: 2 * self.w * self.h)
        var filters = [UInt8](repeating: 0, count: self.h)
        let bytesPerLine = 2 * self.w + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 2)

        var gray = [UInt8](repeating: 0, count: self.w * self.h)
        var alpha = [UInt8](repeating: 0, count: self.w * self.h)
        for i in 0..<gray.count {
            gray[i] = image[2 * i]
            alpha[i] = image[2 * i + 1]
        }
        FlateEncode(&deflatedAlphaData, alpha)
        return gray
    }

    private func getImageColorType6BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: 4 * self.w * self.h)
        var filters = [UInt8](repeating: 0, count: self.h)
        let bytesPerLine = 4 * self.w + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 4)

        var idata = [UInt8](repeating: 0, count: (3 * self.w * self.h))   // Image data
        var alpha = [UInt8](repeating: 0, count: (self.w * self.h))       // Alpha values

        k = 0
        j = 0
        var i = 0
        while i < image.count {
            idata[j]     = image[i]
            idata[j + 1] = image[i + 1]
            idata[j + 2] = image[i + 2]
            alpha[k]     = image[i + 3]
            i += 4
            j += 3
            k += 1
        }
        FlateEncode(&deflatedAlphaData, alpha)
        return idata
    }

    // Indexed-color image with bit depth == 1, 2, 4 or 8
    // Each value is a palette index; a PLTE chunk shall appear.
    // The filters are undone on the packed indexes, one byte per pixel whatever
    // the bit depth, before the indexes are looked up in the palette.
    private func getImageColorType3(_ buf: [UInt8]) -> [UInt8] {
        let bytesPerLine = (self.w * self.bitDepth + 7) / 8
        var indexes = [UInt8](repeating: 0, count: bytesPerLine * self.h)
        var filters = [UInt8](repeating: 0, count: self.h)
        for row in 0..<self.h {
            let offset = row * (bytesPerLine + 1)
            filters[row] = buf[offset]
            indexes.replaceSubrange(
                    row * bytesPerLine..<(row + 1) * bytesPerLine,
                    with: buf[(offset + 1)..<(offset + 1 + bytesPerLine)])
        }
        applyFilters(&filters, &indexes, bytesPerLine, self.h, 1)

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
    private func getImageColorType0BitDepth16(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8](repeating: 0, count: self.h)
        let bytesPerLine = 2 * self.w + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[j] = buf[i]
                j += 1
            } else {
                image[k] = buf[i]
                k += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 2)
        return image
    }

    // Grayscale Image with Bit Depth == 8
    private func getImageColorType0BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8](repeating: 0, count: self.h)
        let bytesPerLine = self.w + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[j] = buf[i]
                j += 1
            } else {
                image[k] = buf[i]
                k += 1
            }
        }
        applyFilters(&filters, &image, self.w, self.h, 1)
        return image
    }

    // Grayscale Image with Bit Depth == 4
    private func getImageColorType0BitDepth4(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8](repeating: 0, count: self.h)
        var bytesPerLine = self.w / 2 + 1
        if self.w % 2 > 0 {
            bytesPerLine += 1
        }
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(&filters, &image, bytesPerLine - 1, self.h, 1)
        return image
    }

    // Grayscale Image with Bit Depth == 2
    private func getImageColorType0BitDepth2(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8](repeating: 0, count: self.h)
        var bytesPerLine = self.w / 4 + 1
        if self.w % 4 > 0 {
            bytesPerLine += 1
        }
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(&filters, &image, bytesPerLine - 1, self.h, 1)
        return image
    }

    // Grayscale Image with Bit Depth == 1
    private func getImageColorType0BitDepth1(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h))
        var filters = [UInt8](repeating: 0, count: self.h)
        var bytesPerLine = self.w / 8 + 1
        if self.w % 8 > 0 {
            bytesPerLine += 1
        }
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            } else {
                image[j] = buf[i]
                j += 1
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(&filters, &image, bytesPerLine - 1, self.h, 1)
        return image
    }

    private func applyFilters(
            _ filters: inout [UInt8],
            _ image: inout [UInt8],
            _ width: Int,
            _ height: Int,
            _ bytesPerPixel: Int) {

        let bytesPerLine = width * bytesPerPixel
        var filter: UInt8 = 0x00
        for row in 0..<height {
            for col in 0..<bytesPerLine {
                if col == 0 {
                    filter = filters[row]
                }
                if filter == 0x00 {             // None
                    continue
                }

                var a = 0                       // The pixel on the left
                if col >= bytesPerPixel {
                    a = Int(image[(bytesPerLine * row + col) - bytesPerPixel] & 0xff)
                }
                var b = 0                       // The pixel above
                if row > 0 {
                    b = Int(image[bytesPerLine * (row - 1) + col] & 0xff)
                }
                var c = 0                       // The pixel diagonally left above
                if col >= bytesPerPixel && row > 0 {
                    c = Int(image[(bytesPerLine * (row - 1) + col) - bytesPerPixel] & 0xff)
                }

                let index = bytesPerLine * row + col
                if filter == 0x01 {             // Sub
                    image[index] = image[index] &+ UInt8(a)
                } else if filter == 0x02 {      // Up
                    image[index] = image[index] &+ UInt8(b)
                } else if filter == 0x03 {      // Average
                    image[index] = image[index] &+ UInt8(floor(Double(a + b) / 2.0))
                } else if filter == 0x04 {     // Paeth
                    let p = a + b - c
                    let pa = abs(p - a)
                    let pb = abs(p - b)
                    let pc = abs(p - c)
                    if pa <= pb && pa <= pc {
                        image[index] = image[index] &+ UInt8(a)
                    } else if pb <= pc {
                        image[index] = image[index] &+ UInt8(b)
                    } else {
                        image[index] = image[index] &+ UInt8(c)
                    }
                }
            }
        }
    }
}   // End of PNGImage.swift
