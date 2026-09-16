/**
 * FlateEncode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

internal final class FlateEncode {
    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer: UInt8 = 0
    private let MASK: UInt32 = 0xFFFF
    // The head of the chain of positions with the same hash, and the position
    // before each one: a match can be looked for among several earlier
    // positions instead of the last one only.
    private var head: [Int32]
    private var prev: [Int32]

    private static let WINDOW = 32768       // The distance a match can reach back
    private static let WINDOW_MASK = 32767
    private static let MIN_MATCH = 3
    private static let MAX_MATCH = 258
    private static let MAX_CHAIN = 32       // Earlier positions tried per match
    private static let NICE_MATCH = 258     // Long enough to stop looking

    /// Compresses the input into zlib format and writes the result to the output.
    @discardableResult
    public init(_ output: inout [UInt8], _ input: [UInt8]) {
        let flateLength = FlateLength.shared
        let flateDistance = FlateDistance.shared
        let flateLiteral = FlateLiteral.shared

        let BUFSIZE = MASK + 1  // 2^16 bytes
        head = [Int32](repeating: -1, count: Int(BUFSIZE))
        prev = [Int32](repeating: -1, count: FlateEncode.WINDOW)
        output.reserveCapacity(output.count + input.count / 2 + 64)
        writeCode(&output, UInt32(0x9C78), 16)      // FLG | CMF
        writeCode(&output, UInt32(0x03), 3)         // BTYPE | BFINAL

        let count = input.count
        var i = 0
        while i < count {
            var length = 0
            var distance = 0
            if i + FlateEncode.MIN_MATCH <= count {
                let candidate = insert(input, i)
                if candidate >= 0 {
                    longestMatch(input, i, candidate, &length, &distance)
                }
            }
            if length >= FlateEncode.MIN_MATCH {
                writeCode(&output,
                        flateLength.codes[length - 3],
                        flateLength.nBits[length - 3])
                writeCode(&output,
                        flateDistance.codes[distance - 1],
                        flateDistance.nBits[distance - 1])
                // The positions the match covers are not hashed: doing so finds
                // a little more, and costs more time than the whole search.
                i += length
            } else {
                writeCode(&output,
                        flateLiteral.codes[Int(input[i])],
                        flateLiteral.nBits[Int(input[i])])
                i += 1
            }
        }
        writeCode(&output, UInt32(0), 7)            // END-OF-BLOCK
        if bitsInBuffer > 0 {
            output.append(UInt8(bitBuffer))
        }
        addAdler32(&output, input)
    }

    // Adds the position to the chain of its hash and returns the position that
    // was at the head of that chain, which is where a match is looked for.
    private func insert(_ input: [UInt8], _ i: Int) -> Int32 {
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
        let previous = head[index]
        prev[i & FlateEncode.WINDOW_MASK] = previous
        head[index] = Int32(i)
        return previous
    }

    // Walks the chain from the candidate and keeps the longest match.
    private func longestMatch(
            _ input: [UInt8],
            _ i: Int,
            _ candidate: Int32,
            _ length: inout Int,
            _ distance: inout Int) {
        let limit = min(FlateEncode.MAX_MATCH, input.count - i)
        var position = candidate
        var chain = FlateEncode.MAX_CHAIN
        while position >= 0 && chain > 0 {
            let j = Int(position)
            let back = i - j
            if back <= 0 || back > FlateEncode.WINDOW {
                break
            }
            // The byte that would extend the best match so far is compared
            // first: a candidate that fails it cannot be longer.
            if length == 0 || input[j + length] == input[i + length] {
                var matched = 0
                while matched < limit && input[j + matched] == input[i + matched] {
                    matched += 1
                }
                if matched >= FlateEncode.MIN_MATCH && matched > length {
                    length = matched
                    distance = back
                    if matched >= FlateEncode.NICE_MATCH || matched == limit {
                        break
                    }
                }
            }
            position = prev[j & FlateEncode.WINDOW_MASK]
            chain -= 1
        }
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

    private func addAdler32(_ output: inout [UInt8], _ input: [UInt8]) {
        let prime: UInt32 = 65521
        var s1: UInt32 = 1
        var s2: UInt32 = 0
        var i = 0
        while i < input.count {
            var chunk = min(5552, input.count - i)
            while chunk > 0 {
                s1 &+= UInt32(input[i])
                s2 &+= s1
                i += 1
                chunk -= 1
            }
            s1 %= prime
            s2 %= prime
        }
        let adler = (s2 &<< 16) &+ s1
        output.append(contentsOf: [
            UInt8((adler >> 24) & 0xFF), UInt8((adler >> 16) & 0xFF),
            UInt8((adler >>  8) & 0xFF), UInt8(adler & 0xFF)
        ])
    }
}
