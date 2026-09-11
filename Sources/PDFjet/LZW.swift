/**
 * LZW.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Decodes the data of an LZWDecode stream, with the default EarlyChange of 1:
/// the codes get one bit longer one code before the table needs it. Data that
/// ends without the end code, or with an invalid code, returns what was decoded
/// up to there, as a missing end is common in real files.
///
func lzwDecode(_ data: [UInt8]) -> [UInt8] {
    var decoded = [UInt8]()
    decoded.reserveCapacity(data.count * 2)
    var table = [[UInt8]](repeating: [], count: 4096)
    for i in 0..<256 {
        table[i] = [UInt8(i)]
    }
    var next = 258          // 256 clears the table and 257 ends the data.
    var codeLength = 9
    var bits = 0            // Only the low bitCount bits are still unread.
    var bitCount = 0
    var previous: [UInt8]? = nil
    for b in data {
        bits = ((bits << 8) | Int(b)) & 0xFFFFFF
        bitCount += 8
        while bitCount >= codeLength {
            bitCount -= codeLength
            let code = (bits >> bitCount) & ((1 << codeLength) - 1)
            if code == 256 {
                next = 258
                codeLength = 9
                previous = nil
                continue
            }
            let entry: [UInt8]
            if code < next && code != 257 && !table[code].isEmpty {
                entry = table[code]
            } else if code == next, let previous = previous {
                entry = previous + [previous[0]]
            } else {
                return decoded      // The end code or an invalid one.
            }
            decoded.append(contentsOf: entry)
            if let previous = previous, next < 4096 {
                table[next] = previous + [entry[0]]
                next += 1
            }
            previous = entry
            if next + 1 >= (1 << codeLength) && codeLength < 12 {
                codeLength += 1
            }
        }
    }
    return decoded
}
