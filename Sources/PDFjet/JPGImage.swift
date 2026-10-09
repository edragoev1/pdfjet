/**
 * JPGImage.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * JPGImage.swift
 *
 * The authors make NO WARRANTY or representation, either express or implied,
 * with respect to this software, its quality, accuracy, merchantability, or
 * fitness for a particular purpose. This software is provided "AS IS", and you,
 * its user, assume the entire risk as to its quality and accuracy.
 *
 * This software is copyright (C) 1991-1998, Thomas G. Lane.
 * All Rights Reserved except as specified below.
 *
 * Permission is hereby granted to use, copy, modify, and distribute this
 * software (or portions thereof) for any purpose, without fee, subject to these
 * conditions:
 * (1) If any part of the source code for this software is distributed, then this
 * README file must be included, with this copyright and no-warranty notice
 * unaltered; and any additions, deletions, or changes to the original files
 * must be clearly indicated in accompanying documentation.
 * (2) If only executable code is distributed, then the accompanying
 * documentation must state that "this software is based in part on the work of
 * the Independent JPEG Group".
 * (3) Permission for use of this software is granted only if the user accepts
 * full responsibility for any undesirable consequences; the authors accept
 * NO LIABILITY for damages of any kind.
 *
 * These conditions apply to any software derived from or based on the IJG code,
 * not just to the unmodified library.  If you use our work, you ought to
 * acknowledge us.
 *
 * Permission is NOT granted for the use of any IJG author's name or company name
 * in advertising or publicity relating to this software or products derived from
 * it.  This software may be referred to only as "the Independent JPEG Group's
 * software".
 *
 * We specifically permit and encourage the use of this software as the basis of
 * commercial products, provided that all warranty or liability claims are
 * assumed by the product vendor.
 */
import Foundation

///
/// Used to embed JPG images in the PDF document.
///
class JPGImage {

    let M_SOF0: UInt8  = 0xC0       // Start Of Frame N
    let M_SOF1: UInt8  = 0xC1       // N indicates which compression process
    let M_SOF2: UInt8  = 0xC2       // Only SOF0-SOF2 are now in common use
    let M_SOF3: UInt8  = 0xC3
    let M_SOF5: UInt8  = 0xC5       // NB: codes C4 and CC are NOT SOF markers
    let M_SOF6: UInt8  = 0xC6
    let M_SOF7: UInt8  = 0xC7
    let M_SOF9: UInt8  = 0xC9
    let M_SOF10: UInt8 = 0xCA
    let M_SOF11: UInt8 = 0xCB
    let M_SOF13: UInt8 = 0xCD
    let M_SOF14: UInt8 = 0xCE
    let M_SOF15: UInt8 = 0xCF
    let M_APP0: UInt8 = 0xE0   // The JFIF segment, among others
    let M_APP1: UInt8 = 0xE1   // The Exif segment, among others
    let M_APP14: UInt8 = 0xEE
    // The markers that stand alone, with no parameter segment to skip.
    let M_TEM: UInt8   = 0x01       // Temporary, for arithmetic coding
    let M_RST0: UInt8  = 0xD0       // ReSTart 0 to 7
    let M_RST7: UInt8  = 0xD7
    let M_SOI: UInt8   = 0xD8       // Start Of Image
    let M_EOI: UInt8   = 0xD9       // End Of Image
    let M_SOS: UInt8   = 0xDA       // Start Of Scan, the end of the header

    var width: UInt16 = 0
    var height: UInt16 = 0
    var colorComponents: UInt8 = 0
    // True when an APP14 segment says that Adobe software wrote the image,
    // which stores the inks of a CMYK image inverted, 255 for no ink.
    var adobe = false
    // The pixel density of the JFIF segment and the unit it is in: 1 is the
    // inch and 2 the centimetre, and 0 is the ratio of the two axes with no
    // size to it. The segment comes before the frame header, so the size the
    // image asks for is worked out after the frame header gives its pixels.
    private var densityUnit: UInt8 = 0
    private var xDensity: UInt16 = 0
    private var yDensity: UInt16 = 0
    // The orientation of an Exif segment, 1 to 8, or 0 when there is none:
    // how the image as stored is turned or flipped to be seen upright.
    var orientation = 0
    var data: [UInt8]
    var index = 0

    /// Reads a JPEG image from the stream.
    public convenience init(_ stream: InputStream) throws {
        try self.init(try Content.getFromStream(stream))
    }

    /// Reads a JPEG image from the bytes of the file, which it keeps as they
    /// are, to embed.
    init(_ bytes: [UInt8]) throws {
        self.data = bytes
        try processImage(&data)
    }

    func getWidth() -> UInt16 {
        return self.width
    }

    func getHeight() -> UInt16 {
        return self.height
    }

    func getColorComponents() -> UInt8 {
        return self.colorComponents
    }

    func isAdobe() -> Bool {
        return self.adobe
    }

    func getData() -> [UInt8] {
        return self.data
    }

    private func processImage(_ buffer: inout [UInt8]) throws {
        if buffer.count < 2 || buffer[0] != 0xFF || buffer[1] != 0xD8 {
            throw JPGImageError.invalidJPEGHeader
        }
        index += 2

        while true {
            let ch = try nextMarker(&buffer)

            // The standalone markers carry no parameter segment, so there is
            // nothing to skip after them; reading two bytes of one as a length
            // would skip over the frame header that follows.
            if ch == M_TEM || ch == M_SOI || (ch >= M_RST0 && ch <= M_RST7) {
                continue
            }
            if ch == M_EOI {
                throw JPGImageError.endsBeforeTheFrameHeader
            }

            // Note that marker codes 0xC4, 0xC8, 0xCC are not,
            // and must not be treated as SOFn. C4 in particular
            // is actually DHT.
            if ch == M_SOF3 ||          // Lossless, Huffman
                    ch == M_SOF5 ||     // Differential sequential, Huffman
                    ch == M_SOF6 ||     // Differential progressive, Huffman
                    ch == M_SOF7 ||     // Differential lossless, Huffman
                    ch == M_SOF9 ||     // Extended sequential, arithmetic
                    ch == M_SOF10 ||    // Progressive, arithmetic
                    ch == M_SOF11 ||    // Lossless, arithmetic
                    ch == M_SOF13 ||    // Differential sequential, arithmetic
                    ch == M_SOF14 ||    // Differential progressive, arithmetic
                    ch == M_SOF15 {     // Differential lossless, arithmetic
                // The DCTDecode filter of a PDF reader decodes the sequential
                // and the progressive JPEG of Huffman coding, and not the
                // lossless, the hierarchical or the arithmetic coded one.
                throw JPGImageError.undecodableCoding(Int(ch - M_SOF0))
            } else if ch == M_SOF0 ||   // Baseline
                    ch == M_SOF1 ||     // Extended sequential, Huffman
                    ch == M_SOF2 {      // Progressive, Huffman
                // The length of the frame header, then the sample precision:
                // a PDF image stream of DCTDecode data delivers eight bits per
                // color component, so a JPEG of another precision, a 12-bit one
                // among them, cannot be embedded as it is.
                let segment = index
                let length = try getUInt16(&buffer)
                let precision = try readByte(&buffer)
                if precision != 8 {
                    throw JPGImageError.unsupportedSamplePrecision(Int(precision))
                }
                height = try getUInt16(&buffer)
                width = try getUInt16(&buffer)
                colorComponents = try readByte(&buffer)

                if width == 0 || height == 0 ||
                        (colorComponents != 1 && colorComponents != 3 && colorComponents != 4) {
                    throw JPGImageError.invalidDimensionsOrComponentCount
                }
                // The frame header holds three bytes for each component after
                // its eight, as libjpeg checks: a header of another length is
                // the "Bogus SOF length" of libjpeg and the "Bogus marker
                // length" of MuPDF, so the readers a PDF is drawn with refuse
                // the image where PDFjet embedded it.
                if Int(length) != 3*Int(colorComponents) + 8 {
                    throw JPGImageError.bogusFrameHeaderLength(
                            Int(length), 3*Int(colorComponents) + 8, Int(colorComponents))
                }
                // The component specifications fill the rest of the frame
                // header; they can hold any bytes, the 0xFF of a marker among
                // them, so the markers are read after the whole segment.
                let end = segment + Int(length)
                guard end > index && end <= buffer.count else {
                    throw JPGImageError.cutShort
                }
                index = end
                if !readAdobeMarker(&buffer) {
                    throw JPGImageError.cutShort
                }
                try checkScanEnds(&buffer, ch)
                break
            } else if ch == M_APP0 {
                try readAPP0(&buffer)
            } else if ch == M_APP1 {
                try readAPP1(&buffer)
            } else if ch == M_APP14 {
                try readAPP14(&buffer)
            } else {
                try skipVariable(&buffer)
            }
        }
    }

    // Reads the markers from the frame header to the scan, for an APP14 segment:
    // libjpeg reads a header to the scan too, so a segment that follows the frame
    // header says that Adobe software wrote the image as one before it does.
    // Returns whether it came to the scan, the index then after the marker of
    // it: a JPEG that ends before has no image data.
    private func readAdobeMarker(_ buffer: inout [UInt8]) -> Bool {
        do {
            while true {
                let ch = try nextMarker(&buffer)
                if ch == M_SOS {
                    return true
                }
                if ch == M_EOI {
                    return false
                }
                if ch == M_TEM || ch == M_SOI || (ch >= M_RST0 && ch <= M_RST7) {
                    continue
                }
                if ch == M_APP14 {
                    try readAPP14(&buffer)
                } else {
                    try skipVariable(&buffer)
                }
            }
        } catch {
            return false
        }
    }

    // Throws unless the end-of-image marker follows the header of the scan: in
    // the entropy-coded data a 0xFF is followed by a 0x00 or a restart marker,
    // so 0xFF 0xD9 after the header is the end of the image, and not of a
    // thumbnail before the scan, which has its own. Data after the end, which
    // cameras append, is left as it is. A sequential JPEG without the marker
    // whose scans hold every block of the image is whole but for it, and
    // viewers draw it: the marker is added to the data embedded (JPGScan). A
    // progressive one without it is refused.
    private func checkScanEnds(_ buffer: inout [UInt8], _ frame: UInt8) throws {
        guard let length = try? getUInt16(&buffer), length >= 2 else {
            throw JPGImageError.cutShort
        }
        var i = index + Int(length) - 2
        while i + 1 < buffer.count {
            if buffer[i] == 0xFF && buffer[i + 1] == M_EOI {
                return
            }
            i += 1
        }
        if (frame == M_SOF0 || frame == M_SOF1) && JPGScan.scansWhole(buffer) {
            buffer.append(0xFF)
            buffer.append(M_EOI)
            return
        }
        throw JPGImageError.cutShort
    }

    /// Reads one byte, advancing the index.
    /// Throws if the buffer is exhausted.
    private func readByte(_ buffer: inout [UInt8]) throws -> UInt8 {
        guard index < buffer.count else {
            throw JPGImageError.unexpectedEndOfJPEGData
        }
        let b = buffer[index]
        index += 1
        return b
    }

    /// Reads two bytes as a big-endian unsigned integer,
    /// advancing the index by two.
    private func getUInt16(_ buffer: inout [UInt8]) throws -> UInt16 {
        let b1 = try readByte(&buffer)
        let b2 = try readByte(&buffer)
        return UInt16(b1) << 8 | UInt16(b2)
    }

    // Find the next JPEG marker and return its marker code.
    // Non-FF garbage between markers is skipped over.
    // Duplicate FF bytes are legal padding and are swallowed, and an FF byte
    // that a zero byte follows is data and not a marker, so the search goes on.
    // NB: this routine must not be used after seeing SOS marker,
    // since it will not deal correctly with FF/00 sequences in the
    // compressed image data...
    private func nextMarker(_ buffer: inout [UInt8]) throws -> UInt8 {
        while true {
            // Find 0xFF byte; skip any non-FF garbage.
            var ch = try readByte(&buffer)
            while ch != 0xFF {
                ch = try readByte(&buffer)
            }

            // Get the marker code byte, swallowing any duplicate FF bytes.
            // Extra FFs are legal as pad bytes.
            repeat {
                ch = try readByte(&buffer)
            } while ch == 0xFF

            if ch != 0x00 {
                return ch
            }
        }
    }

    // Reads an APP14 segment, which Adobe software writes starting with "Adobe".
    // Reads the pixel density of a JFIF segment, which is the first segment of
    // a JFIF file: "JFIF", a zero byte, the version, the unit of the density
    // and the density of each axis. A segment of another kind, the JFXX
    // extension among them, is passed over. The resolution an Exif segment
    // holds is not read: it is a TIFF image file directory, which is a format
    // of its own, and a JFIF segment is what the density of a JPEG is.
    private func readAPP0(_ buffer: inout [UInt8]) throws {
        let length = try getUInt16(&buffer)
        if length < 2 {
            throw JPGImageError.invalidSegmentLength
        }
        let end = index + Int(length) - 2
        guard end <= buffer.count else {
            throw JPGImageError.unexpectedEndOfJPEGData
        }
        if end - index >= 12 &&
                buffer[index..<(index + 5)].elementsEqual([0x4A, 0x46, 0x49, 0x46, 0x00]) {
            densityUnit = buffer[index + 7]
            xDensity = UInt16(buffer[index + 8]) << 8 | UInt16(buffer[index + 9])
            yDensity = UInt16(buffer[index + 10]) << 8 | UInt16(buffer[index + 11])
        }
        index = end
    }

    // Reads the orientation of an Exif segment: "Exif", two zero bytes, and a
    // TIFF header, II or MM for the byte order, 42, and the offset of the first
    // image file directory, whose entry of the tag 0x0112 is the orientation, a
    // SHORT of 1 to 8. An Exif segment that is not whole or not read so is
    // passed over, as it is a camera's and not the image's: what it says
    // wrongly or does not say leaves the image as stored. A segment of another
    // kind, XMP among them, is passed over. The first orientation read is the
    // image's.
    private func readAPP1(_ buffer: inout [UInt8]) throws {
        let length = try getUInt16(&buffer)
        if length < 2 {
            throw JPGImageError.invalidSegmentLength
        }
        let end = index + Int(length) - 2
        guard end <= buffer.count else {
            throw JPGImageError.unexpectedEndOfJPEGData
        }
        if orientation == 0 {
            orientation = JPGImage.exifOrientation(Array(buffer[index..<end]))
        }
        index = end
    }

    // The orientation of the Exif segment, or 0.
    static func exifOrientation(_ segment: [UInt8]) -> Int {
        if segment.count < 14 || !segment[0..<6].elementsEqual([0x45, 0x78, 0x69, 0x66, 0x00, 0x00]) {
            return 0
        }
        let tiff = Array(segment[6...])
        let little: Bool
        if tiff[0] == 0x49 && tiff[1] == 0x49 {         // II
            little = true
        } else if tiff[0] == 0x4D && tiff[1] == 0x4D {  // MM
            little = false
        } else {
            return 0
        }
        func u16(_ i: Int) -> Int {
            return little ? Int(tiff[i]) | Int(tiff[i + 1]) << 8 : Int(tiff[i]) << 8 | Int(tiff[i + 1])
        }
        func u32(_ i: Int) -> Int {
            return little ? u16(i) | u16(i + 2) << 16 : u16(i) << 16 | u16(i + 2)
        }
        if u16(2) != 42 {
            return 0
        }
        let ifd = u32(4)
        if ifd < 8 || ifd + 2 > tiff.count {
            return 0
        }
        let count = u16(ifd)
        for i in 0..<count {
            let entry = ifd + 2 + 12*i
            if entry + 12 > tiff.count {
                return 0
            }
            // The tag, its type, SHORT, and a count of one, its value in the
            // first two bytes of the value field
            if u16(entry) == 0x0112 {
                if u16(entry + 2) != 3 || u32(entry + 4) != 1 {
                    return 0
                }
                let value = u16(entry + 8)
                return (value >= 1 && value <= 8) ? value : 0
            }
        }
        return 0
    }

    // The points a unit of the density is: an inch is 72 of them and a
    // centimetre 72/2.54. Unit 0 is a ratio of the axes with no size to it.
    private func pointsPerUnit() -> Double {
        if densityUnit == 1 {
            return 72.0
        } else if densityUnit == 2 {
            return 72.0/2.54
        }
        return 0.0
    }

    // The size the image asks to be drawn at, in points, or 0 when the JFIF
    // segment gives none: no segment, a unit that is a ratio rather than a
    // size, no density at all, or a size too large for a PDF number.
    func getPhysicalWidth() -> Float {
        return physicalSize(self.width, self.xDensity)
    }

    func getPhysicalHeight() -> Float {
        return physicalSize(self.height, self.yDensity)
    }

    private func physicalSize(_ pixels: UInt16, _ density: UInt16) -> Float {
        let points = pointsPerUnit()
        if points == 0.0 || density == 0 || xDensity == 0 || yDensity == 0 {
            return 0.0
        }
        let size = Float(Double(pixels)*points/Double(density))
        return FastFloat.isWritable(size) ? size : 0.0
    }

    private func readAPP14(_ buffer: inout [UInt8]) throws {
        let length = try getUInt16(&buffer)
        if length < 2 {
            throw JPGImageError.invalidSegmentLength
        }
        let end = index + Int(length) - 2
        guard end <= buffer.count else {
            throw JPGImageError.unexpectedEndOfJPEGData
        }
        // Adobe's segment is twelve bytes: "Adobe", the version, two flags and
        // the color transform. A shorter one is not Adobe's, as in libjpeg. An
        // APP14 segment of another kind after it does not unmark the image.
        if end - index >= 12 && buffer[index..<(index + 5)].elementsEqual(Array("Adobe".utf8)) {
            adobe = true
        }
        index = end
    }

    // Most types of marker are followed by a variable-length parameter
    // segment. This routine skips over the parameters for any marker we
    // don't otherwise want to process.
    // Note that we MUST skip the parameter segment explicitly in order
    // not to be fooled by 0xFF bytes that might appear within the
    // parameter segment such bytes do NOT introduce new markers.
    private func skipVariable(_ buffer: inout [UInt8]) throws {
        // Get the marker parameter length count
        let length = try getUInt16(&buffer)
        if length < 2 {
            // Length includes itself, so must be at least 2
            throw JPGImageError.invalidSegmentLength
        }

        // Skip over the remaining bytes
        for _ in 2..<length {
            _ = try readByte(&buffer)   // throws on EOF
        }
    }
}

///
/// Errors that can occur while parsing a JPEG image.
///
/// Each has the message the other ports throw with.
///
enum JPGImageError: Error, Equatable, CustomStringConvertible, LocalizedError {
    case invalidJPEGHeader
    case unexpectedEndOfJPEGData
    case invalidDimensionsOrComponentCount
    case invalidSegmentLength
    case endsBeforeTheFrameHeader
    case unsupportedSamplePrecision(Int)            // The bits per color component
    case bogusFrameHeaderLength(Int, Int, Int)      // The length, the right one and the components
    case undecodableCoding(Int)                     // The n of the SOFn marker
    case cutShort                                   // Its image data has no end

    var description: String {
        switch self {
        case .invalidJPEGHeader:
            return "Error: Invalid JPEG header."
        case .unexpectedEndOfJPEGData:
            return "Unexpected end of JPEG data."
        case .invalidDimensionsOrComponentCount:
            return "Invalid JPEG dimensions or component count."
        case .invalidSegmentLength:
            return "Invalid marker segment length."
        case .endsBeforeTheFrameHeader:
            return "Error: The JPEG ends before its frame header."
        case .unsupportedSamplePrecision(let precision):
            return "Error: The JPEG has \(precision) bits per color component, not 8."
        case .bogusFrameHeaderLength(let length, let expected, let components):
            return "Error: The JPEG frame header is \(length) bytes, not the \(expected)"
                    + " of its \(components) color components."
        case .undecodableCoding(let n):
            return "Error: The JPEG is lossless, hierarchical or arithmetic coded (SOF\(n)),"
                    + " which a PDF reader cannot decode."
        case .cutShort:
            return "Error: The JPEG is cut short: its image data has no end."
        }
    }

    var errorDescription: String? {
        return description
    }
}   // End of JPGImage.swift
