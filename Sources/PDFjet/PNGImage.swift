/**
 *  PNGImage.swift
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
import Foundation

/**
 * Used to embed PNG images in the PDF document.
 * <p>
 * <strong>Please note:</strong>
 * <p>
 *     Interlaced images are not supported.
 * <p>
 *     To convert interlaced image to non-interlaced image use OptiPNG:
 * <p>
 *     optipng -i0 -o7 myimage.png
 */
public class PNGImage {

    var w: Int?                         // Image width in pixels
    var h: Int?                         // Image height in pixels

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
    public init(_ stream: InputStream) throws {
        var buffer = try readPNG(stream)
        let chunks = try processPNG(&buffer)
        for chunk in chunks {
            let chunkType = String(bytes: chunk.type!, encoding: .utf8)!
            // Swift.print(chunkType)
            if chunkType == "IHDR" {
                self.w = Int(getUInt32(chunk.getData()!, 0))    // Width
                self.h = Int(getUInt32(chunk.getData()!, 4))    // Height
                self.bitDepth = Int(chunk.getData()![8])        // Bit Depth
                self.colorType = Int(chunk.getData()![9])       // Color Type
/*
                Swift.print("bitDepth == " + String(self.bitDepth))
                Swift.print("colorType == " + String(self.colorType))
                Swift.print("Compression: " + String(chunk.getData()![10]))
                Swift.print("Filter: " + String(chunk.getData()![11]))
                Swift.print("Interlace: " + String(chunk.getData()![12]))
*/
                if chunk.getData()![12] == 1 {
                    Swift.print("Interlaced PNG images are not supported.")
                    Swift.print("Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png\n")
                }
            }
            else if chunkType == "IDAT" {
                iDAT.append(contentsOf: chunk.getData()!)
            }
            else if chunkType == "PLTE" {
                pLTE = chunk.getData()!
                if pLTE!.count % 3 != 0 {
                    Swift.print("Incorrect palette length: \(String(pLTE!.count))")
                }
            }
            else if chunkType == "gAMA" {
                // TODO:
            }
            else if chunkType == "tRNS" {
                if colorType == 3 {
                    tRNS = chunk.getData()
                }
            }
            else if chunkType == "cHRM" {
                // TODO:
            }
            else if chunkType == "sBIT" {
                // TODO:
            }
            else if chunkType == "bKGD" {
                // TODO:
            }
        }

        var inflatedImageData = [UInt8]()   // The inflated image data
        _ = try Puff(output: &inflatedImageData, input: &iDAT)

        var image: [UInt8]?                 // The image data
        if colorType == 0 {
            // Grayscale Image
            if bitDepth == 16 {
                image = getImageColorType0BitDepth16(inflatedImageData)
            }
            else if bitDepth == 8 {
                image = getImageColorType0BitDepth8(inflatedImageData)
            }
            else if bitDepth == 4 {
                image = getImageColorType0BitDepth4(inflatedImageData)
            }
            else if bitDepth == 2 {
                image = getImageColorType0BitDepth2(inflatedImageData)
            }
            else if bitDepth == 1 {
                image = getImageColorType0BitDepth1(inflatedImageData)
            }
            else {
                Swift.print("Image with unsupported bit depth == \(String(bitDepth))")
            }
        }
        else if colorType == 6 {
            if bitDepth == 8 {
                image = getImageColorType6BitDepth8(inflatedImageData)
            }
            else {
                Swift.print("Image with unsupported bit depth == \(String(bitDepth))")
            }
        }
        else {
            // Color Image
            if pLTE == nil {
                // Trucolor Image
                if bitDepth == 16 {
                    image = getImageColorType2BitDepth16(inflatedImageData)
                }
                else {
                    image = getImageColorType2BitDepth8(inflatedImageData)
                }
            }
            else {
                // Indexed Image
                if bitDepth == 8 {
                    image = try getImageColorType3BitDepth8(inflatedImageData)
                }
                else if bitDepth == 4 {
                    image = getImageColorType3BitDepth4(inflatedImageData)
                }
                else if bitDepth == 2 {
                    image = getImageColorType3BitDepth2(inflatedImageData)
                }
                else if bitDepth == 1 {
                    image = getImageColorType3BitDepth1(inflatedImageData)
                }
                else {
                    Swift.print("Image with unsupported bit depth == \(String(bitDepth))")
                }
            }
        }

        LZWEncode(&deflatedImageData, &image!)
/*
        Swift.print("image.count -> " + String(image!.count))
        let time0 = Int64(Date().timeIntervalSince1970 * 1000)
        LZWEncode(&deflatedImageData, &image!)
        // _ = FlateEncode(&deflatedImageData, &image!, RLE: true)
        let time1 = Int64(Date().timeIntervalSince1970 * 1000)
        Swift.print(time1 - time0)
        Swift.print("deflatedImageData.count -> " + String(deflatedImageData.count))
*/
    }


    public func getWidth() -> Int? {
        return self.w
    }


    public func getHeight() -> Int? {
        return self.h
    }


    public func getColorType() -> Int {
        return self.colorType
    }


    public func getBitDepth() -> Int {
        return self.bitDepth
    }


    public func getData() -> [UInt8] {
        return self.deflatedImageData
    }


    public func getAlpha() -> [UInt8] {
        return self.deflatedAlphaData
    }


    private func readPNG(_ stream: InputStream) throws -> [UInt8] {
        let contents = try Content.ofInputStream(stream)
        if contents[0] == 0x89 &&
                contents[1] == 0x50 &&
                contents[2] == 0x4E &&
                contents[3] == 0x47 &&
                contents[4] == 0x0D &&
                contents[5] == 0x0A &&
                contents[6] == 0x1A &&
                contents[7] == 0x0A {
            // The PNG signature is correct.
        }
        else {
            Swift.print("Wrong PNG signature.")
        }
        return contents
    }


    private func processPNG(
            _ buffer: inout [UInt8]) throws -> [Chunk] {
        var chunks = [Chunk]()
        var offset = 8      // Skip the header!
        while true {
            let chunk = getChunk(&buffer, &offset)
            let chunkType = String(bytes: chunk.type!, encoding: .utf8)!
            if chunkType == "IEND" {
                break
            }
            chunks.append(chunk)
        }
        return chunks
    }


    private func getChunk(
            _ buffer: inout [UInt8],
            _ offset: inout Int) -> Chunk {
        let chunk = Chunk()

        // The length of the data chunk.
        chunk.length = getUInt32(getBytes(&buffer, &offset, 4), 0)

        // The chunk type.
        chunk.type = getBytes(&buffer, &offset, 4)

        // The chunk data.
        chunk.data = getBytes(&buffer, &offset, Int(chunk.length!))

        // CRC of the type and data chunks.
        chunk.crc = getUInt32(getBytes(&buffer, &offset, 4), 0)

        let crc = CRC32()
        crc.update(chunk.type!, 0, 4)
        crc.update(chunk.data!, 0, Int(chunk.length!))
        if crc.getValue() != chunk.crc {
            Swift.print("Chunk has bad CRC.")
        }

        return chunk
    }


    private func getBytes(
            _ buffer: inout [UInt8],
            _ offset: inout Int,
            _ length: Int) -> [UInt8] {
        var bytes = [UInt8]()
        var i = 0
        while i < length {
            bytes.append(buffer[offset + i])
            i += 1
        }
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
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var filters = [UInt8]()
        let bytesPerLine = 6 * self.w! + 1
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters.append(buf[i])
            }
            else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w!, self.h!, 6)
        return image
    }


    // Truecolor Image with Bit Depth == 8
    private func getImageColorType2BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var filters = [UInt8]()
        let bytesPerLine = 3 * self.w! + 1
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters.append(buf[i])
            }
            else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w!, self.h!, 3)
        return image
    }


    // Truecolor Image with Alpha Transparency
    private func getImageColorType6BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: 4 * self.w! * self.h!)
        var filters = [UInt8](repeating: 0, count: self.h!)
        let bytesPerLine = 4 * self.w! + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[k] = buf[i]
                k += 1
            }
            else {
                image[j] = buf[i]
                j += 1
            }
        }
        applyFilters(&filters, &image, self.w!, self.h!, 4)

        var idata = [UInt8](repeating: 0, count: (3 * self.w! * self.h!))   // Image data
        var alpha = [UInt8](repeating: 0, count: (self.w! * self.h!))       // Alpha values

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
        LZWEncode(&deflatedAlphaData, &alpha)
        // _ = FlateEncode(&deflatedAlphaData, &alpha, RLE: true)
        return idata
    }


    // Indexed-color image with bit depth == 8
    // Each value is a palette index; a PLTE chunk shall appear.
    private func getImageColorType3BitDepth8(_ buf: [UInt8]) throws -> [UInt8] {
        var image = [UInt8](repeating: 0x00, count: 3*self.w!*self.h!)

        var filters = [UInt8]()
        var alpha: [UInt8]?
        if tRNS != nil {
            alpha = [UInt8](repeating: 0xFF, count: self.w!*self.h!)
        }

        let bytesPerLine = self.w! + 1
        var n = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters.append(buf[i])
            }
            else {
                let k = Int(buf[i]) & Int(0xff)
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
        applyFilters(&filters, &image, self.w!, self.h!, 3)

        if tRNS != nil {
            LZWEncode(&deflatedAlphaData, &alpha!)
            // _ = FlateEncode(&deflatedAlphaData, &alpha!, RLE: true)
        }

        return image
    }


    // Indexed Image with Bit Depth == 4
    private func getImageColorType3BitDepth4(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: 6*(buf.count - self.h!))
        var bytesPerLine = self.w! / 2 + 1
        if self.w! % 2 > 0 {
            bytesPerLine += 1
        }

        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                // Skip the filter byte.
                continue
            }

            let l = buf[i]

            var k = Int(3 * ((l >> 4) & 0x0000000f))
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = Int(3 * (l & 0x0000000f))
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1
        }
        return image
    }


    // Indexed Image with Bit Depth == 2
    private func getImageColorType3BitDepth2(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (12 * (buf.count - self.h!)))

        var bytesPerLine = self.w! / 4 + 1
        if self.w! % 4 > 0 {
            bytesPerLine += 1
        }

        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                // Skip the filter byte.
                continue
            }

            let l = Int(buf[i])

            var k = 3 * ((l >> 6) & 0x00000003)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 4) & 0x00000003)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 2) & 0x00000003)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * (l & 0x00000003)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1
        }

        return image
    }


    // Indexed Image with Bit Depth == 1
    private func getImageColorType3BitDepth1(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (24 * (buf.count - self.h!)))

        var bytesPerLine = self.w! / 8 + 1
        if self.w! % 8 > 0 {
            bytesPerLine += 1
        }

        var j = 0
        for i in 0..<buf.count {

            if i % bytesPerLine == 0 {
                // Skip the filter byte.
                continue
            }

            let l = Int(buf[i])

            var k = 3 * ((l >> 7) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 6) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 5) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 4) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 3) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 2) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * ((l >> 1) & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

            if j % (3 * self.w!) == 0 {
                continue
            }

            k = 3 * (l & 0x00000001)
            image[j] = pLTE![k]
            j += 1
            image[j] = pLTE![k + 1]
            j += 1
            image[j] = pLTE![k + 2]
            j += 1

        }

        return image
    }


    // Grayscale Image with Bit Depth == 16
    private func getImageColorType0BitDepth16(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var filters = [UInt8](repeating: 0, count: self.h!)
        let bytesPerLine = 2 * self.w! + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[j] = buf[i]
                j += 1
            }
            else {
                image[k] = buf[i]
                k += 1
            }
        }
        applyFilters(&filters, &image, self.w!, self.h!, 2)
        return image
    }


    // Grayscale Image with Bit Depth == 8
    private func getImageColorType0BitDepth8(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var filters = [UInt8](repeating: 0, count: self.h!)
        let bytesPerLine = self.w! + 1
        var k = 0
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine == 0 {
                filters[j] = buf[i]
                j += 1
            }
            else {
                image[k] = buf[i]
                k += 1
            }
        }
        applyFilters(&filters, &image, self.w!, self.h!, 1)
        return image
    }


    // Grayscale Image with Bit Depth == 4
    private func getImageColorType0BitDepth4(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var bytesPerLine = self.w! / 2 + 1
        if self.w! % 2 > 0 {
            bytesPerLine += 1
        }
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine != 0 {
                image[j] = buf[i]
                j += 1
            }
        }
        return image
    }


    // Grayscale Image with Bit Depth == 2
    private func getImageColorType0BitDepth2(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var bytesPerLine = self.w! / 4 + 1
        if self.w! % 4 > 0 {
            bytesPerLine += 1
        }
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine != 0 {
                image[j] = buf[i]
                j += 1
            }
        }
        return image
    }


    // Grayscale Image with Bit Depth == 1
    private func getImageColorType0BitDepth1(_ buf: [UInt8]) -> [UInt8] {
        var image = [UInt8](repeating: 0, count: (buf.count - self.h!))
        var bytesPerLine: Int = self.w! / 8 + 1
        if self.w! % 8 > 0 {
            bytesPerLine += 1
        }
        var j = 0
        for i in 0..<buf.count {
            if i % bytesPerLine != 0 {
                image[j] = buf[i]
                j += 1
            }
        }
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
                }
                else if filter == 0x02 {        // Up
                    image[index] = image[index] &+ UInt8(b)
                }
                else if filter == 0x03 {        // Average
                    image[index] = image[index] &+ UInt8(floor(Double(a + b) / 2.0))
                }
                else if filter == 0x04 {        // Paeth
                    let p = a + b - c
                    let pa = abs(p - a)
                    let pb = abs(p - b)
                    let pc = abs(p - c)
                    if pa <= pb && pa <= pc {
                        image[index] = image[index] &+ UInt8(a)
                    }
                    else if pb <= pc {
                        image[index] = image[index] &+ UInt8(b)
                    }
                    else {
                        image[index] = image[index] &+ UInt8(c)
                    }
                }
            }
        }
    }
}   // End of PNGImage.swift
