/*
 * BMPImage.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Written by Jonas Krogsböll, who contributed it to PDFjet.
 */
import Foundation

class BMPImage {
    var w = 0                           // Image width in pixels
    var h = 0                           // Image height in pixels

    var image: [UInt8]?                 // The reconstructed image data
    var deflated: [UInt8]?              // The deflated reconstructed image data

    private var bpp = 0
    private var palette: [[UInt8]]?
    private var masks = [UInt32]()      // The red, green and blue masks of a 16 or 32 bit pixel
    private var topDown: Bool = false   // If the first row is the top row

    // The alpha mask of a 16 or 32 bit pixel, or 0 for an opaque image, and
    // the deflated alpha of the pixels, or nil for an image drawn opaque.
    private var alphaMask: UInt32 = 0
    private var deflatedAlpha: [UInt8]?

    // The size the header gives the image, in points, or 0 when it gives
    // none: the pixels per metre of each axis, which most writers leave at 0.
    private var physicalWidth: Float = 0.0
    private var physicalHeight: Float = 0.0

    // A metre is 72/0.0254 points.
    private static let POINTS_PER_METER = 72.0/0.0254

    private let BI_RGB = 0
    private let BI_BITFIELDS = 3

    private let m10000000: UInt8 = 0x80
    private let m01000000: UInt8 = 0x40
    private let m00100000: UInt8 = 0x20
    private let m00010000: UInt8 = 0x10
    private let m00001000: UInt8 = 0x08
    private let m00000100: UInt8 = 0x04
    private let m00000010: UInt8 = 0x02
    private let m00000001: UInt8 = 0x01
    private let m11110000: UInt8 = 0xF0
    private let m00001111: UInt8 = 0x0F

    enum BMPImageError: Error {
        case notParsed
        case unsupportedBitDepth
        case unexpectedEndOfStream
        case invalidImageData
    }

    // Tested with images created from GIMP
    /// Reads a BMP image from the stream, which it opens and closes itself.
    public init(_ stream: InputStream) throws {
        stream.open()
        defer { stream.close() }
        let bm = try getBytes(stream, 2)
        // From Wikipedia
        if (Unicode.Scalar(bm[0]) == "B" && Unicode.Scalar(bm[1]) == "M") ||
                (Unicode.Scalar(bm[0]) == "B" && Unicode.Scalar(bm[1]) == "A") ||
                (Unicode.Scalar(bm[0]) == "C" && Unicode.Scalar(bm[1]) == "I") ||
                (Unicode.Scalar(bm[0]) == "C" && Unicode.Scalar(bm[1]) == "P") ||
                (Unicode.Scalar(bm[0]) == "I" && Unicode.Scalar(bm[1]) == "C") ||
                (Unicode.Scalar(bm[0]) == "P" && Unicode.Scalar(bm[1]) == "T") {
            try skipNBytes(stream, 8)
            let offset = try readSignedInt(stream)      // Where the pixels start
            let headerSize = try readSignedInt(stream)
            // The older OS/2 header is 12 bytes; the others start with the 40
            // bytes of the BITMAPINFOHEADER. The size is checked first: the
            // width and the height of the OS/2 header are 2 bytes each, so the
            // fields that follow are not where the larger headers have them.
            if headerSize < 40 {
                throw PDFjetError(message: "Unsupported BMP header of \(headerSize) bytes.")
            }
            self.w = try readSignedInt(stream)
            self.h = try readSignedInt(stream)
            if self.h < 0 {
                // A negative height is that of a top-down bitmap.
                self.h = -self.h
                self.topDown = true
            }
            try skipNBytes(stream, 2)
            self.bpp = try read2BytesLE(stream)
            let compression = try readSignedInt(stream)
            // The size and the bit depth come from the file, so they are
            // checked, without overflow, before any buffer is allocated.
            if w <= 0 || h <= 0 || h > Int(Int32.max) {    // -Int32.min is not an Int32
                throw PDFjetError(message: "Invalid BMP image size.")
            }
            if ![1, 4, 8, 16, 24, 32].contains(bpp) {
                throw PDFjetError(message: "Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images")
            }
            // RLE and the JPEG and PNG compressions are not supported, and their
            // data is not read as pixels.
            if compression != BI_RGB && !(compression == BI_BITFIELDS && (bpp == 16 || bpp == 32)) {
                throw PDFjetError(message: "Compressed BMP images are not supported.")
            }
            let rowSize = 4 * ((bpp * w + 31) / 32)     // w is an Int32 and bpp at most 32
            let (samples, samplesOverflow) = (3 * w).multipliedReportingOverflow(by: h)
            let (rows, rowsOverflow) = rowSize.multipliedReportingOverflow(by: h)
            if samplesOverflow || rowsOverflow ||
                    samples > MAX_DECODED_LENGTH || rows > MAX_DECODED_LENGTH {
                throw PDFjetError(message: "The BMP image is larger than \(MAX_DECODED_LENGTH) bytes.")
            }
            try skipNBytes(stream, 4)   // The size of the pixels
            let pixelsPerMeterX = try readSignedInt(stream)
            let pixelsPerMeterY = try readSignedInt(stream)
            setPhysicalSize(pixelsPerMeterX, pixelsPerMeterY)
            let colorsUsed = try readSignedInt(stream)
            try skipNBytes(stream, 4)
            var read = 54       // The bytes read so far

            // The masks follow the first 40 bytes of the header, in the larger
            // headers or after the BITMAPINFOHEADER. Without them the pixels
            // have the masks of the format: 5 bits a color in 16 bits, and 8
            // bits a color in 32 bits.
            if compression == BI_BITFIELDS {
                for _ in 0..<3 {
                    masks.append(UInt32(truncatingIfNeeded: try readSignedInt(stream)))
                }
                read += 12
                // The alpha mask follows them in a header of 56 bytes or more:
                // the BITMAPV3INFOHEADER and the V4 and V5 headers. The masks
                // after a header of 40 bytes, and those of the 52 byte one,
                // have none.
                if headerSize >= 56 {
                    alphaMask = UInt32(truncatingIfNeeded: try readSignedInt(stream))
                    read += 4
                }
            } else if bpp == 16 {
                masks = [0x7C00, 0x03E0, 0x001F]
            } else if bpp == 32 {
                masks = [0x00FF0000, 0x0000FF00, 0x000000FF]
            }
            if 14 + headerSize > read {
                try skipNBytes(stream, 14 + headerSize - read)     // The rest of a larger header
                read = 14 + headerSize
            }

            if bpp <= 8 {
                let numPalColors = (colorsUsed == 0) ? (1 << bpp) : colorsUsed
                if numPalColors < 0 || numPalColors > 256 {
                    throw PDFjetError(message: "Invalid BMP palette size \(numPalColors).")
                }
                try parsePalette(stream, numPalColors)
                read += 4 * numPalColors
            }
            if offset > read {
                try skipNBytes(stream, offset - read)      // The pixels start at the offset
            }
            try parseData(stream)
        } else {
            throw BMPImageError.notParsed
        }
    }

    private func parseData(_ stream: InputStream) throws {
        if w < 0 {
            throw BMPImageError.invalidImageData
        }
        let rowsize = 4 * ((bpp * w + 31) / 32)         // 4 byte alignment
        // The rows are read before the image is allocated, so a size that the
        // stream does not have takes no memory.
        var rows = [[UInt8]]()
        rows.reserveCapacity(min(h, 4096))
        for _ in 0..<(h - 1) {
            rows.append(try getBytes(stream, rowsize))
        }
        // Some files end without the padding of the last row, which holds no
        // pixels, as Pillow and browsers read them.
        let last = try getBytes(stream, (bpp * w + 7) / 8)
        var padding = [UInt8](repeating: 0, count: rowsize - last.count)
        _ = stream.read(&padding, maxLength: padding.count)
        rows.append(last)

        image = [UInt8](repeating: 0, count: (3 * w * h))
        var alpha: [UInt8]? = (alphaMask != 0) ? [UInt8](repeating: 0, count: w * h) : nil
        var row: [UInt8]
        var index = 0
        for i in 0..<self.h {
            row = rows[i]
            if self.bpp == 1 {
                row = bit1to8(row, w)           // opslag i palette
            } else if self.bpp == 4 {
                row = bit4to8(row, w)           // opslag i palette
            } else if self.bpp == 8 {             // opslag i palette
                //
            } else if self.bpp == 16 {
                row = masksTo24(row, w, 2, masks)
            } else if self.bpp == 24 {            // bytes are correct
            } else if self.bpp == 32 {
                row = masksTo24(row, w, 4, masks)
            } else {
                // Only 1, 4, 8, 16, 24 and 32 bits per pixel are supported.
                throw BMPImageError.unsupportedBitDepth
            }

            index = topDown ? 3*w*i : 3*w*((h - i) - 1)
            if alpha != nil {
                masksToAlpha(rows[i], w, bpp / 8, alphaMask, &alpha!, index / 3)
            }
            if self.palette != nil {
                // indexed
                for j in 0..<self.w {
                    image![index] = self.palette![Int(row[j])][2]
                    index += 1
                    image![index] = self.palette![Int(row[j])][1]
                    index += 1
                    image![index] = self.palette![Int(row[j])][0]
                    index += 1
                }
            } else {
                // not indexed
                var j = 0
                while j < 3*self.w {
                    image![index] = row[j + 2]
                    index += 1
                    image![index] = row[j + 1]
                    index += 1
                    image![index] = row[j]
                    index += 1
                    j += 3
                }
            }
        }

        deflated = [UInt8]()
        FlateEncode(&deflated!, image!)
        // An image whose alpha is 0 in every pixel is drawn opaque, as
        // browsers draw it: writers that do not know of the alpha leave it at
        // 0. One whose alpha is 255 in every pixel needs no soft mask.
        if let alpha = alpha, !allBytesAre(alpha, 0), !allBytesAre(alpha, 255) {
            deflatedAlpha = [UInt8]()
            FlateEncode(&deflatedAlpha!, alpha)
        }
    }

    // Converts a row of 16 or 32 bit little endian pixels to blue, green and
    // red bytes, with the red, green and blue masks. A color of fewer than 8
    // bits is scaled to the full range, so that 31 of 5 bits is 255.
    private func masksTo24(_ row: [UInt8], _ width: Int, _ bytesPerPixel: Int, _ masks: [UInt32]) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: 3*width)
        var j = 0
        var i = 0
        while i < width * bytesPerPixel {
            let pixel = pixelAt(row, i, bytesPerPixel)
            ret[j] = colorOf(pixel, masks[2])
            ret[j + 1] = colorOf(pixel, masks[1])
            ret[j + 2] = colorOf(pixel, masks[0])
            j += 3
            i += bytesPerPixel
        }
        return ret
    }

    // Writes the alpha of a row of 16 or 32 bit little endian pixels, under
    // the alpha mask, to alpha from the start, from 0 to 255.
    private func masksToAlpha(_ row: [UInt8], _ width: Int, _ bytesPerPixel: Int, _ mask: UInt32,
            _ alpha: inout [UInt8], _ start: Int) {
        var j = start
        var i = 0
        while i < width * bytesPerPixel {
            alpha[j] = colorOf(pixelAt(row, i, bytesPerPixel), mask)
            j += 1
            i += bytesPerPixel
        }
    }

    // Returns the 16 or 32 bit little endian pixel at the offset of the row.
    private func pixelAt(_ row: [UInt8], _ offset: Int, _ bytesPerPixel: Int) -> UInt32 {
        var pixel = UInt32(row[offset]) | UInt32(row[offset + 1]) << 8
        if bytesPerPixel == 4 {
            pixel |= UInt32(row[offset + 2]) << 16 | UInt32(row[offset + 3]) << 24
        }
        return pixel
    }

    // Reports whether every byte of the array is the value.
    private func allBytesAre(_ data: [UInt8], _ value: UInt8) -> Bool {
        for b in data where b != value {
            return false
        }
        return true
    }

    // Returns the color of the pixel under the mask, from 0 to 255.
    private func colorOf(_ pixel: UInt32, _ mask: UInt32) -> UInt8 {
        if mask == 0 {
            return 0
        }
        let shift = mask.trailingZeroBitCount
        let max = UInt64(mask >> shift)
        let value = UInt64((pixel & mask) >> shift)
        return UInt8(value * 255 / max)
    }

    private func bit4to8(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: width)
        for i in 0..<width {
            if i % 2 == 0 {
                ret[i] = UInt8((row[i/2] & m11110000) >> 4)
            } else {
                ret[i] = UInt8((row[i/2] & m00001111))
            }
        }
        return ret
    }

    private func bit1to8(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: width)
        for i in 0..<width {
            switch (i % 8) {
            case 0: ret[i] = UInt8((row[i/8] & m10000000) >> 7); break
            case 1: ret[i] = UInt8((row[i/8] & m01000000) >> 6); break
            case 2: ret[i] = UInt8((row[i/8] & m00100000) >> 5); break
            case 3: ret[i] = UInt8((row[i/8] & m00010000) >> 4); break
            case 4: ret[i] = UInt8((row[i/8] & m00001000) >> 3); break
            case 5: ret[i] = UInt8((row[i/8] & m00000100) >> 2); break
            case 6: ret[i] = UInt8((row[i/8] & m00000010) >> 1); break
            case 7: ret[i] = UInt8((row[i/8] & m00000001)); break
            default: break
            }
        }
        return ret
    }

    private func parsePalette(_ stream: InputStream, _ size: Int) throws {
        if size < 0 {
            throw BMPImageError.invalidImageData
        }
        // An index past the colors is drawn black, as browsers draw it, so the
        // palette has the 256 colors an index of 8 bits can take.
        self.palette = [[UInt8]]()
        for i in 0..<256 {
            self.palette!.append(i < size ? try getBytes(stream, 4) : [0, 0, 0, 0])
        }
    }

    // Skips in pieces, so that a count that the file does not have fails at the
    // end of the stream instead of allocating the count.
    private func skipNBytes(_ stream: InputStream, _ n: Int) throws {
        var remaining = n
        while remaining > 0 {
            let count = min(remaining, 65536)
            _ = try getBytes(stream, count)
            remaining -= count
        }
    }

    // Reads length bytes: a single read may return fewer bytes than asked for.
    private func getBytes(_ stream: InputStream, _ length: Int) throws -> [UInt8] {
        if length < 0 {
            throw BMPImageError.invalidImageData
        }
        var buf = [UInt8](repeating: 0, count: length)
        var offset = 0
        while offset < length {
            let read = buf.withUnsafeMutableBufferPointer {
                stream.read($0.baseAddress! + offset, maxLength: length - offset)
            }
            if read <= 0 {
                throw BMPImageError.unexpectedEndOfStream
            }
            offset += read
        }
        return buf
    }

    private func read2BytesLE(_ stream: InputStream) throws -> Int {
        let buf = try getBytes(stream, 2)
        var val: UInt32 = 0
        val |= UInt32(buf[1] & 0xff)
        val <<= 8
        val |= UInt32(buf[0] & 0xff)
        return Int(val)
    }

    @discardableResult
    private func readSignedInt(_ stream: InputStream) throws -> Int {
        let buf = try getBytes(stream, 4)
        var val: Int64 = 0
        val |= Int64(buf[3] & 0xff)
        val <<= 8
        val |= Int64(buf[2] & 0xff)
        val <<= 8
        val |= Int64(buf[1] & 0xff)
        val <<= 8
        val |= Int64(buf[0] & 0xff)
        return Int(Int32(truncatingIfNeeded: val))
    }

    /// Returns the image width.
    // Works out the size the image asks to be drawn at from the pixels per
    // metre of the header. Most writers leave the two at 0, and a size too
    // large for a PDF number is passed over; the image keeps the size of its
    // pixels for both.
    private func setPhysicalSize(_ pixelsPerMeterX: Int, _ pixelsPerMeterY: Int) {
        if pixelsPerMeterX <= 0 || pixelsPerMeterY <= 0 {
            return
        }
        let width = Float(Double(self.w)*BMPImage.POINTS_PER_METER/Double(pixelsPerMeterX))
        let height = Float(Double(self.h)*BMPImage.POINTS_PER_METER/Double(pixelsPerMeterY))
        if FastFloat.isWritable(width) && FastFloat.isWritable(height) {
            self.physicalWidth = width
            self.physicalHeight = height
        }
    }

    /// Returns the width in points the header asks for, or 0 when the header
    /// gives no size.
    public func getPhysicalWidth() -> Float {
        return self.physicalWidth
    }

    /// Returns the height in points the header asks for, or 0 when the header
    /// gives no size.
    public func getPhysicalHeight() -> Float {
        return self.physicalHeight
    }

    public func getWidth() -> Int {
        return self.w
    }

    /// Returns the image height.
    public func getHeight() -> Int {
        return self.h
    }

    /// Returns the compressed image data.
    public func getData() -> [UInt8] {
        return self.deflated!
    }

    /// Returns the deflated alpha of the pixels, one byte a pixel, or nil when
    /// the image is opaque.
    func getAlpha() -> [UInt8]? {
        return self.deflatedAlpha
    }
}
