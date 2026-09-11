/**
 * Filters.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Decodes the data of an ASCIIHexDecode stream. White space is skipped, >
/// ends the data, and a last digit without a pair is followed by 0.
///
func asciiHexDecode(_ data: [UInt8]) -> [UInt8] {
    var decoded = [UInt8]()
    decoded.reserveCapacity(data.count / 2)
    var high = -1
    for b in data {
        if b == 0x3E {          // ">"
            break
        }
        let digit = hexValue(b)
        if digit == -1 {
            continue            // White space, or a character that is not valid.
        }
        if high == -1 {
            high = digit
        } else {
            decoded.append(UInt8(high << 4 | digit))
            high = -1
        }
    }
    if high != -1 {
        decoded.append(UInt8(high << 4))
    }
    return decoded
}

///
/// Returns the value of a hexadecimal digit, or -1 when it is not one.
///
func hexValue(_ c: UInt8) -> Int {
    if c >= 0x30 && c <= 0x39 {         // "0" to "9"
        return Int(c) - 0x30
    } else if c >= 0x61 && c <= 0x66 {  // "a" to "f"
        return Int(c) - 0x61 + 10
    } else if c >= 0x41 && c <= 0x46 {  // "A" to "F"
        return Int(c) - 0x41 + 10
    }
    return -1
}

///
/// Decodes the data of an ASCII85Decode stream. Each group of five characters
/// from ! to u is four bytes, z is four zero bytes, and ~> ends the data. White
/// space is skipped, and a last group of n characters is n - 1 bytes.
///
func ascii85Decode(_ data: [UInt8]) -> [UInt8] {
    var decoded = [UInt8]()
    decoded.reserveCapacity(data.count * 4 / 5 + 4)
    var value: Int64 = 0
    var count = 0
    var i = 0
    if data.count >= 2 && data[0] == 0x3C && data[1] == 0x7E {    // "<~"
        i = 2                   // The start of the data in PostScript.
    }
    while i < data.count {
        let c = data[i]
        if c == 0x7E {                          // "~"
            break
        } else if c == 0x7A && count == 0 {     // "z"
            decoded.append(contentsOf: [0, 0, 0, 0])
        } else if c >= 0x21 && c <= 0x75 {      // "!" to "u"
            value = value * 85 + Int64(c - 0x21)
            count += 1
            if count == 5 {
                for j in stride(from: 24, through: 0, by: -8) {
                    decoded.append(UInt8(truncatingIfNeeded: value >> Int64(j)))
                }
                value = 0
                count = 0
            }
        }                       // White space, or a character that is not valid.
        i += 1
    }
    if count > 1 {
        for _ in count..<5 {
            value = value * 85 + 84
        }
        for j in 0..<(count - 1) {
            decoded.append(UInt8(truncatingIfNeeded: value >> Int64(24 - 8 * j)))
        }
    }
    return decoded
}

///
/// Decodes the data of a RunLengthDecode stream. A length byte from 0 to 127 is
/// followed by that many plus one bytes to copy, one from 129 to 255 by a byte
/// to repeat 257 minus that many times, and 128 ends the data.
///
func runLengthDecode(_ data: [UInt8]) -> [UInt8] {
    var decoded = [UInt8]()
    decoded.reserveCapacity(data.count * 2)
    var i = 0
    while i < data.count {
        let length = Int(data[i])
        i += 1
        if length < 128 {
            let n = min(length + 1, data.count - i)
            decoded.append(contentsOf: data[i..<(i + n)])
            i += n
        } else if length > 128 && i < data.count {
            decoded.append(contentsOf: repeatElement(data[i], count: 257 - length))
            i += 1
        } else {
            break
        }
    }
    return decoded
}
