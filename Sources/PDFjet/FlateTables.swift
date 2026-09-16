/**
 * FlateTables.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The constant tables of RFC 1951, computed once and shared by every
// FlateEncode call. A code is kept apart from the extra bits that follow it,
// rather than packed together with them, because a dynamic code can be 15
// bits and a distance can carry 13 extra bits: the two together do not fit
// the bit buffer in one write.
internal final class FlateTables: @unchecked Sendable {
    static let shared = FlateTables()

    // RFC 1951 3.2.5: the lengths that codes 257-285 stand for, and the
    // distances that codes 0-29 stand for, each with its count of extra bits.
    let lengthBase = [
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
            35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258]
    let lengthExtra: [UInt8] = [
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0]
    let distanceBase = [
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
            257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193,
            12289, 16385, 24577]
    let distanceExtra: [UInt8] = [
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13]

    // The length code of every match length, indexed by length - 3.
    var lengthCode = [UInt8](repeating: 0, count: 256)

    // RFC 1951 3.2.6: the fixed Huffman codes, held as code lengths so that a
    // fixed block and a dynamic block are written by the same code.
    let fixedLiteralLengths: [UInt8]
    let fixedDistanceLengths: [UInt8]
    let fixedLiteralCodes: [UInt32]
    let fixedDistanceCodes: [UInt32]

    // RFC 1951 3.2.7: the order the code lengths of the code length alphabet
    // are written in, which puts the ones most often zero last.
    let codeLengthOrder = [
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15]

    private init() {
        for symbol in 0..<lengthBase.count {
            let n = 1 << Int(lengthExtra[symbol])
            for i in 0..<n {
                let length = lengthBase[symbol] + i
                if length <= 258 {
                    // Code 284 reaches 258 with its extra bits, but 258 has a
                    // code of its own, which is written here afterwards.
                    lengthCode[length - 3] = UInt8(symbol)
                }
            }
        }
        var literal = [UInt8](repeating: 8, count: 288)
        for symbol in 144..<256 {
            literal[symbol] = 9
        }
        for symbol in 256..<280 {
            literal[symbol] = 7
        }
        fixedLiteralLengths = literal
        fixedDistanceLengths = [UInt8](repeating: 5, count: 30)
        fixedLiteralCodes = FlateTables.canonicalCodes(literal)
        fixedDistanceCodes = FlateTables.canonicalCodes(fixedDistanceLengths)
    }

    // The distance code of a match distance, by RFC 1951 3.2.5. Distances 1-4
    // are their own codes; above that each power of two holds two codes, told
    // apart by the bit below the highest one.
    static func distanceCode(_ distance: Int) -> Int {
        let value = distance - 1
        if value < 4 {
            return value
        }
        let highest = 31 - UInt32(value).leadingZeroBitCount
        return (highest << 1) | ((value >> (highest - 1)) & 1)
    }

    // Assigns the codes that a set of code lengths stands for, by RFC 1951
    // 3.2.2: shorter codes first and, within a length, in the order of the
    // symbols. The codes come back reversed, ready to be written low bit
    // first like every other value in the stream.
    static func canonicalCodes(_ lengths: [UInt8]) -> [UInt32] {
        var countOfLength = [Int](repeating: 0, count: 16)
        for length in lengths where length > 0 {
            countOfLength[Int(length)] += 1
        }
        var nextCode = [UInt32](repeating: 0, count: 16)
        var code: UInt32 = 0
        for length in 1...15 {
            code = (code + UInt32(countOfLength[length - 1])) << 1
            nextCode[length] = code
        }
        var codes = [UInt32](repeating: 0, count: lengths.count)
        for symbol in 0..<lengths.count {
            let length = Int(lengths[symbol])
            if length > 0 {
                codes[symbol] = FlateUtils.reverse(nextCode[length], length: length)
                nextCode[length] += 1
            }
        }
        return codes
    }
}
