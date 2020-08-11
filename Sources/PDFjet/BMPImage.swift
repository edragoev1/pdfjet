/**
 *
 *  Copyright 2020 Jonas Krogsböll

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


class BMPImage {

    var w = 0                           // Image width in pixels
    var h = 0                           // Image height in pixels

    var image: [UInt8]?                 // The reconstructed image data
    var deflated: [UInt8]?              // The deflated reconstructed image data

    private var bpp = 0
    private var palette: [[UInt8]]?
    private var r5g6b5: Bool = false    // If 16 bit image two encodings can occur

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


    // Tested with images created from GIMP
    public init(_ stream: InputStream) {
        let bm = getBytes(stream, 2)
        // From Wikipedia
        if (Unicode.Scalar(bm![0]) == "B" && Unicode.Scalar(bm![1]) == "M") ||
                (Unicode.Scalar(bm![0]) == "B" && Unicode.Scalar(bm![1]) == "A") ||
                (Unicode.Scalar(bm![0]) == "C" && Unicode.Scalar(bm![1]) == "I") ||
                (Unicode.Scalar(bm![0]) == "C" && Unicode.Scalar(bm![1]) == "P") ||
                (Unicode.Scalar(bm![0]) == "I" && Unicode.Scalar(bm![1]) == "C") ||
                (Unicode.Scalar(bm![0]) == "P" && Unicode.Scalar(bm![1]) == "T") {
            skipNBytes(stream, 8)
            let offset = readSignedInt(stream)
            readSignedInt(stream)               // size of header
            self.w = readSignedInt(stream)
            self.h = readSignedInt(stream)
            skipNBytes(stream, 2)
            self.bpp = read2BytesLE(stream)
            let compression = readSignedInt(stream)
            if bpp > 8 {
                r5g6b5 = (compression == 3)
                skipNBytes(stream, 20)
                if offset > 54 {
                    skipNBytes(stream, offset - 54)
                }
            }
            else {
                skipNBytes(stream, 12)
                var numPalColors = readSignedInt(stream)
                if numPalColors == 0 {
                    numPalColors = Int(pow(2.0, Double(bpp)))
                }
                skipNBytes(stream, 4)
                parsePalette(stream, numPalColors)
            }
            parseData(stream)
        }
        else {
            // TODO:
            Swift.print("BMP data could not be parsed!")
        }
    }

    private func parseData(_ stream: InputStream) {
        image = [UInt8](repeating: 0, count: (3 * w * h))
        let rowsize = 4 * Int(ceil(Double(w * bpp) / 32.0)) // 4 byte alignment
        var row: [UInt8]
        var index = 0
        for i in 0..<self.h {
            row = getBytes(stream, rowsize)!
            if self.bpp == 1 {
                row = bit1to8(row, w)           // opslag i palette
            }
            else if self.bpp == 4 {
                row = bit4to8(row, w)           // opslag i palette
            }
            else if self.bpp == 8 {             // opslag i palette
                //
            }
            else if self.bpp == 16 {
                if self.r5g6b5 {                // 5,6,5 bit
                    row = bit16to24(row, w)
                }
                else {
                    row = bit16to24b(row, w)
                }
            }
            else if self.bpp == 24 {            // bytes are correct
            }
            else if self.bpp == 32 {
                row = bit32to24(row, w)
            }
            else {
                Swift.print("Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images.")
            }

            index = 3*w*((h - i) - 1)
            if self.palette != nil {
                // indexed
                for j in 0..<self.w {
                    image![index] = self.palette![Int((row[j] < 0) ? row[j] : row[j])][2]
                    index += 1
                    image![index] = self.palette![Int((row[j] < 0) ? row[j] : row[j])][1]
                    index += 1
                    image![index] = self.palette![Int((row[j] < 0) ? row[j] : row[j])][0]
                    index += 1
                }
            }
            else {
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
        _ = LZWEncode(&deflated!, &image!)
        // _ = FlateEncode(&deflated!, &image!, RLE: true)
    }

    // 5 + 6 + 5 in B G R format 2 bytes to 3 bytes
    private func bit16to24(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: 3*width)
        var i = 0
        var j = 0
        while i < 2*width {
            ret[j] = UInt8((row[i] & 0x1F) << 3)
            j += 1
            ret[j] = UInt8(((row[i + 1] & 0x07) << 5) + (row[i] & 0xE0) >> 3)
            j += 1
            ret[j] = UInt8((row[i + 1] & 0xF8))
            j += 1
            i += 2
        }
        return ret
    }

    // 5 + 5 + 5 in B G R format 2 bytes to 3 bytes
    private func bit16to24b(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: 3*width)
        var i = 0
        var j = 0
        while i < 2*width {
            ret[j] = UInt8((row[i] & 0x1F) << 3)
            j += 1
            ret[j] = UInt8(((row[i + 1] & 0x03) << 6) + (row[i] & 0xE0) >> 2)
            j += 1
            ret[j] = UInt8((row[i + 1] & 0x7C) << 1)
            j += 1
            i += 2
        }
        return ret
    }

    /* alpha first? */
    private func bit32to24(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: 3*width)
        var i = 0
        var j = 0
        while i < 4*width {
            ret[j] = row[i + 1]
            j += 1
            ret[j] = row[i + 2]
            j += 1
            ret[j] = row[i + 3]
            j += 1
            i += 4
        }
        return ret
    }

    private func bit4to8(_ row: [UInt8], _ width: Int) -> [UInt8] {
        var ret = [UInt8](repeating: 0, count: width)
        for i in 0..<width {
            if i % 2 == 0 {
                ret[i] = UInt8((row[i/2] & m11110000) >> 4)
            }
            else {
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

    private func parsePalette(_ stream: InputStream, _ size: Int) {
        self.palette = [[UInt8]]()
        for _ in 0..<size {
            self.palette!.append(getBytes(stream, 4)!)
        }
    }

    private func skipNBytes(_ stream: InputStream, _ n: Int) {
        var buf = [UInt8](repeating: 0, count: n)
        if stream.read(&buf, maxLength: buf.count) == buf.count {
        }
    }

    private func getBytes(_ stream: InputStream, _ length: Int) -> [UInt8]? {
        var buf = [UInt8](repeating: 0, count: length)
        if stream.read(&buf, maxLength: buf.count) == buf.count {
            return buf
        }
        return nil
    }

    private func read2BytesLE(_ stream: InputStream) -> Int {
        let buf = getBytes(stream, 2)!
        var val: UInt32 = 0
        val |= UInt32(buf[1] & 0xff)
        val <<= 8
        val |= UInt32(buf[0] & 0xff)
        return Int(val)
    }

    @discardableResult
    private func readSignedInt(_ stream: InputStream) -> Int {
        let buf = getBytes(stream, 4)!
        var val: Int64 = 0
        val |= Int64(buf[3] & 0xff)
        val <<= 8
        val |= Int64(buf[2] & 0xff)
        val <<= 8
        val |= Int64(buf[1] & 0xff)
        val <<= 8
        val |= Int64(buf[0] & 0xff)
        return Int(val)
    }

    public func getWidth() -> Int {
        return self.w
    }

    public func getHeight() -> Int {
        return self.h
    }

    public func getData() -> [UInt8] {
        return self.deflated!
    }

}
