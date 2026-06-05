/**
 * FlateEncode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

internal class FlateEncode {
    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer: UInt8 = 0
    private let MASK: UInt32 = 0xFFFF
    private var hashtable: [Int]

    @discardableResult
    public init(_ output: inout [UInt8], _ input: [UInt8]) {
        let flateLength = FlateLength()
        let flateDistance = FlateDistance()
        let flateLiteral = FlateLiteral()

        let BUFSIZE = MASK + 1  // 2^16 bytes
        hashtable = [Int](repeating: -1, count: Int(BUFSIZE))
        writeCode(&output, UInt32(0x9C78), 16)      // FLG | CMF
        writeCode(&output, UInt32(0x03), 3)         // BTYPE | BFINAL
        var i = 0
        while i < (input.count - 3) {
            var index = getMatchIndex(input, i, &hashtable)
            if index != -1 {
                let distance = i - index
                var length = 3
                index += 3
                i += 3
                while i < input.count {
                    if input[index] != input[i] || length == 258 {
                        break
                    }
                    length += 1
                    index += 1
                    i += 1
                }
                writeCode(&output,
                        flateLength.codes[length - 3],
                        flateLength.nBits[length - 3])
                writeCode(&output,
                        flateDistance.codes[distance - 1],
                        flateDistance.nBits[distance - 1])
            } else {
                writeCode(&output,
                        flateLiteral.codes[Int(input[i])],
                        flateLiteral.nBits[Int(input[i])])
                i += 1
            }
        }
        while i < input.count {
            writeCode(&output,
                    flateLiteral.codes[Int(input[i])],
                    flateLiteral.nBits[Int(input[i])])
            i += 1
        }
        writeCode(&output, UInt32(0), 7)            // END-OF-BLOCK
        if bitsInBuffer > 0 {
            output.append(UInt8(bitBuffer))
        }
        addAdler32(&output, input)
    }

    private func getMatchIndex(
            _ input: [UInt8],
            _ i: Int,
            _ hashtable: inout [Int]) -> Int {
        // FNV-1a inline hash routines
        var hash: UInt64 = 0xcbf29ce484222325
        let prime: UInt64 = 0x100000001b3
        hash ^= UInt64(input[i])
        hash = hash &* prime
        hash ^= UInt64(input[i + 1])
        hash = hash &* prime
        hash ^= UInt64(input[i + 2])
        hash = hash &* prime
        // Perform xor-folding operation
        let index = Int(((hash >> 30) ^ hash) & UInt64(MASK))
        let j = hashtable[index]
        hashtable[index] = i
        if j != -1 &&
                i - j <= 32768 &&
                input[j] == input[i] &&
                input[j + 1] == input[i + 1] &&
                input[j + 2] == input[i + 2] {
            return j
        }
        return -1
    }

    private func writeCode(
            _ output: inout [UInt8],
            _ code: UInt32,
            _ nBits: UInt8) {
        bitBuffer |= UInt32(code) << bitsInBuffer
        bitsInBuffer += nBits
        while bitsInBuffer >= 8 {
            output.append(UInt8(bitBuffer & 0xFF))
            bitBuffer >>= 8
            bitsInBuffer -= 8
        }
    }

    private func addAdler32(
            _ output: inout [UInt8], _ input: [UInt8]) {
        // Calculate the Adler-32 checksum
        let prime: UInt32 = 65521
        var s1: UInt32 = 1
        var s2: UInt32 = 0
        for i in 0..<input.count {
            s1 = (s1 &+ UInt32(input[i])) % prime
            s2 = (s2 &+ s1) % prime
        }
        let adler = (s2 &<< 16) &+ s1
        output.append(UInt8((adler >> 24) & 0xFF))
        output.append(UInt8((adler >> 16) & 0xFF))
        output.append(UInt8((adler >>  8) & 0xFF))
        output.append(UInt8((adler >>  0) & 0xFF))
    }
}
