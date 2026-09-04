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

    var width: UInt16 = 0
    var height: UInt16 = 0
    var colorComponents: UInt8 = 0
    var data: [UInt8]
    var index = 0

    public init(_ stream: InputStream) throws {
        self.data = try Content.getFromStream(stream)
        try processImage(&data)
    }

    func getWidth() -> UInt16 {
        return self.width
    }

    func getHeight() -> UInt16 {
        return self.height
    }

    func getFileSize() -> Int64 {
        return Int64(self.data.count)
    }

    func getColorComponents() -> UInt8 {
        return self.colorComponents
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
            // Note that marker codes 0xC4, 0xC8, 0xCC are not,
            // and must not be treated as SOFn. C4 in particular
            // is actually DHT.
            if ch == M_SOF0 ||          // Baseline
                    ch == M_SOF1 ||     // Extended sequential, Huffman
                    ch == M_SOF2 ||     // Progressive, Huffman
                    ch == M_SOF3 ||     // Lossless, Huffman
                    ch == M_SOF5 ||     // Differential sequential, Huffman
                    ch == M_SOF6 ||     // Differential progressive, Huffman
                    ch == M_SOF7 ||     // Differential lossless, Huffman
                    ch == M_SOF9 ||     // Extended sequential, arithmetic
                    ch == M_SOF10 ||    // Progressive, arithmetic
                    ch == M_SOF11 ||    // Lossless, arithmetic
                    ch == M_SOF13 ||    // Differential sequential, arithmetic
                    ch == M_SOF14 ||    // Differential progressive, arithmetic
                    ch == M_SOF15 {     // Differential lossless, arithmetic
                // Skip 3 bytes to get to the image height and width
                index += 3
                height = try getUInt16(&buffer)
                width = try getUInt16(&buffer)
                colorComponents = try readByte(&buffer)

                if width == 0 || height == 0 ||
                        (colorComponents != 1 && colorComponents != 3 && colorComponents != 4) {
                    throw JPGImageError.invalidDimensionsOrComponentCount
                }
                break
            } else {
                try skipVariable(&buffer)
            }
        }
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
    // Duplicate FF bytes are legal padding and are swallowed.
    // NB: this routine must not be used after seeing SOS marker,
    // since it will not deal correctly with FF/00 sequences in the
    // compressed image data...
    private func nextMarker(_ buffer: inout [UInt8]) throws -> UInt8 {
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

        return ch
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
enum JPGImageError: Error {
    case invalidJPEGHeader
    case unexpectedEndOfJPEGData
    case invalidDimensionsOrComponentCount
    case invalidSegmentLength
}   // End of JPGImage.swift
