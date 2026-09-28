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

    // The stream of the image object: the IDAT data as it is, whose PNG
    // filters the /DecodeParms of the image undo, or the deflated image data.
    var stream = [UInt8]()
    // The colors of a pixel of the IDAT data that the stream is, for the
    // /DecodeParms, or 0 when the stream is the deflated image data.
    var decodeColors = 0
    // The colors of the /Indexed color space of a palette image whose stream
    // is its IDAT data, one for each index the bit depth has, the ones past
    // the palette black; nil for an image of RGB samples.
    var palette: [UInt8]?

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
        let (inflatedImageData, exact) = try inflateExact(iDAT, imageDataLength)
        if inflatedImageData.count < imageDataLength {
            throw PDFjetError(message: "The PNG image data is shorter than the image.")
        }

        // The IDAT data that is the rows of the image and nothing more is
        // embedded as it is, and the reader undoes the filters of the rows, as
        // a PNG decoder does. The IDAT data of an image with alpha, whose alpha
        // goes in a soft mask of its own, and any other IDAT data are decoded
        // and compressed again.
        if exact && (colorType == 0 || colorType == 2 || colorType == 3) {
            try embedIDAT(inflatedImageData)
        } else {
            try decode(inflatedImageData)
            self.stream = deflatedImageData
        }
    }

    // Makes the IDAT data of a grayscale, truecolor or palette image the
    // stream of the image object, which is faster than decoding the samples
    // and compressing them again. The filter type of each row is checked, as
    // decoding checks it; the alpha of a palette image with a tRNS chunk is
    // decoded for its soft mask.
    private func embedIDAT(_ buf: [UInt8]) throws {
        let colors = (colorType == 2) ? 3 : 1
        let bytesPerRow = (w * colors * bitDepth + 7) / 8
        for row in 0..<h {
            let filter = buf[row * (bytesPerRow + 1)]
            if filter > 4 {
                throw PDFjetError(message: "Invalid PNG filter type \(filter).")
            }
        }
        stream = iDAT
        decodeColors = colors
        if colorType == 3 {
            palette = Array(pLTE![0..<(3 << bitDepth)])
            // A tRNS chunk of no alpha but 255 needs no decoding for its soft mask.
            if let tRNS = tRNS, !BMPImage.allBytesAre(tRNS, 255) {
                let indexes = paletteIndexes(try unfilter(buf, h, bytesPerRow, 1))
                setAlpha(paletteAlpha(indexes))
            }
        }
    }

    // Deflates the alpha of the pixels for the soft mask of the image, unless
    // every pixel is opaque: an image with no soft mask is drawn the same, is
    // smaller, and is one that PDF/A-1 can hold.
    private func setAlpha(_ alpha: [UInt8]) {
        if !BMPImage.allBytesAre(alpha, 255) {
            deflatedAlphaData = [UInt8]()
            FlateEncode(&deflatedAlphaData, alpha)
        }
    }

    // Decodes the samples of the rows of the image, and deflates them.
    private func decode(_ inflatedImageData: [UInt8]) throws {
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

        deflatedImageData = [UInt8]()
        FlateEncode(&deflatedImageData, image)
    }

    /// Returns the color space and the bits per component of the image
    /// object: RGB for a palette image, which is /Indexed on RGB when its
    /// stream is its IDAT data.
    func getColorSpace() -> (String, Int) {
        switch colorType {
        case 0:
            return ("DeviceGray", bitDepth)
        case 4:
            return ("DeviceGray", 8)
        case 3:
            return ("DeviceRGB", (palette != nil) ? bitDepth : 8)
        default:
            return ("DeviceRGB", (bitDepth == 16) ? 16 : 8)
        }
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

    /// Returns the image data: the samples, deflated. For an image whose
    /// stream is its IDAT data they are decoded on the first call.
    func getData() throws -> [UInt8] {
        if deflatedImageData.isEmpty {
            try decode(try inflatePrefix(iDAT, try getImageDataLength()))
        }
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
    // The bytes of the first pixel of a row have no byte on the left, and the
    // first row no row above, so they are undone apart from the rest, with the
    // bytes they do not have taken as 0.
    private func unfilter(
            _ buf: [UInt8],
            _ rows: Int,
            _ bytesPerRow: Int,
            _ bytesPerPixel: Int) throws -> [UInt8] {
        var image = [UInt8](repeating: 0, count: rows * bytesPerRow)
        let first = min(bytesPerPixel, bytesPerRow)     // The bytes of the first pixel
        var badFilter: UInt8?
        buf.withUnsafeBufferPointer { src in
            image.withUnsafeMutableBufferPointer { dst in
                for row in 0..<rows {
                    let offset = row * (bytesPerRow + 1)
                    let filter = src[offset]
                    let line = dst.baseAddress! + row * bytesPerRow         // The row
                    let prior = line - bytesPerRow      // The row above, none for the first row
                    line.update(from: src.baseAddress! + offset + 1, count: bytesPerRow)
                    if filter == 0x00 {                 // None
                    } else if filter == 0x01 || (filter == 0x04 && row == 0) {
                        // Sub, and Paeth of the first row, which is the byte on the left.
                        var i = bytesPerPixel
                        while i < bytesPerRow {
                            line[i] = line[i] &+ line[i - bytesPerPixel]
                            i += 1
                        }
                    } else if filter == 0x02 {          // Up
                        if row > 0 {
                            for i in 0..<bytesPerRow {
                                line[i] = line[i] &+ prior[i]
                            }
                        }
                    } else if filter == 0x03 && row == 0 {  // Average of the first row
                        var i = bytesPerPixel
                        while i < bytesPerRow {
                            line[i] = line[i] &+ line[i - bytesPerPixel] / 2
                            i += 1
                        }
                    } else if filter == 0x03 {          // Average
                        for i in 0..<first {
                            line[i] = line[i] &+ prior[i] / 2
                        }
                        var i = bytesPerPixel
                        while i < bytesPerRow {
                            let a = Int(line[i - bytesPerPixel])    // The byte on the left
                            let b = Int(prior[i])                   // The byte above
                            line[i] = line[i] &+ UInt8((a + b) / 2)
                            i += 1
                        }
                    } else if filter == 0x04 {          // Paeth, whose first pixel is the byte above
                        for i in 0..<first {
                            line[i] = line[i] &+ prior[i]
                        }
                        var i = bytesPerPixel
                        while i < bytesPerRow {
                            // The bytes on the left, above and above on the left
                            let a = Int(line[i - bytesPerPixel])
                            let b = Int(prior[i])
                            let c = Int(prior[i - bytesPerPixel])
                            line[i] = line[i] &+ UInt8(PNGImage.paeth(a, b, c))
                            i += 1
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
        setAlpha(alpha)
        return gray
    }

    private func getImageColorType6BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        let image = try unfilter(buf, self.h, 4 * self.w, 4)
        var idata = [UInt8](repeating: 0, count: (3 * self.w * self.h))   // Image data
        var alpha = [UInt8](repeating: 0, count: (self.w * self.h))       // Alpha values
        image.withUnsafeBufferPointer { pixels in
            idata.withUnsafeMutableBufferPointer { colors in
                alpha.withUnsafeMutableBufferPointer { alpha in
                    for i in 0..<alpha.count {
                        colors[3 * i]     = pixels[4 * i]
                        colors[3 * i + 1] = pixels[4 * i + 1]
                        colors[3 * i + 2] = pixels[4 * i + 2]
                        alpha[i]          = pixels[4 * i + 3]
                    }
                }
            }
        }
        setAlpha(alpha)
        return idata
    }

    // Indexed-color image with bit depth == 1, 2, 4 or 8
    // Each value is a palette index; a PLTE chunk shall appear.
    // The filters are undone on the packed indexes, one byte per pixel whatever
    // the bit depth, before the indexes are looked up in the palette.
    private func getImageColorType3(_ buf: [UInt8]) throws -> [UInt8] {
        let bytesPerLine = (self.w * self.bitDepth + 7) / 8
        let indexes = paletteIndexes(try unfilter(buf, self.h, bytesPerLine, 1))

        var image = [UInt8](repeating: 0x00, count: 3*indexes.count)
        var j = 0
        for index in indexes {
            let k = Int(index)
            image[j] = pLTE![3*k]
            image[j + 1] = pLTE![3*k + 1]
            image[j + 2] = pLTE![3*k + 2]
            j += 3
        }
        if tRNS != nil {
            setAlpha(paletteAlpha(indexes))
        }
        return image
    }

    // Returns the palette index of each pixel, a byte each, of the unfiltered
    // rows of a palette image.
    private func paletteIndexes(_ rows: [UInt8]) -> [UInt8] {
        let bytesPerLine = (self.w * self.bitDepth + 7) / 8
        var indexes = [UInt8](repeating: 0, count: self.w*self.h)
        let mask = (1 << self.bitDepth) - 1
        var n = 0
        for row in 0..<self.h {
            for col in 0..<self.w {
                let bit = col * self.bitDepth
                let b = Int(rows[row * bytesPerLine + bit / 8])
                indexes[n] = UInt8((b >> (8 - self.bitDepth - bit % 8)) & mask)
                n += 1
            }
        }
        return indexes
    }

    // Returns the alpha of each pixel of a palette image with a tRNS chunk:
    // the alpha of its index, or opaque for an index the chunk has none for.
    private func paletteAlpha(_ indexes: [UInt8]) -> [UInt8] {
        var alpha = [UInt8](repeating: 0xFF, count: indexes.count)
        for i in 0..<indexes.count {
            let k = Int(indexes[i])
            if k < tRNS!.count {
                alpha[i] = tRNS![k]
            }
        }
        return alpha
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
