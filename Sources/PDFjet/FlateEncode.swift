/**
 * FlateEncode.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

internal final class FlateEncode {
    // The bits not yet written, and how many, as plain memory, as the token
    // tables below are: they change for every code written (9 October 2026)
    private let bitBuffer: UnsafeMutablePointer<UInt32>
    private let bitsInBuffer: UnsafeMutablePointer<UInt32>
    private let MASK: UInt32 = 0xFFFF
    // The head of the chain of positions with the same hash, and the position
    // before each one: a match can be looked for among several earlier
    // positions instead of the last one only.
    // Plain memory too: both are read and written for every position.
    private let head: UnsafeMutablePointer<Int32>
    private let prev: UnsafeMutablePointer<Int32>

    private static let WINDOW = 32768       // The distance a match can reach back
    private static let WINDOW_MASK = 32767
    private static let MIN_MATCH = 3
    private static let MAX_MATCH = 258
    private static let MAX_CHAIN = 32       // Earlier positions tried per match
    private static let NICE_MATCH = 258     // Long enough to stop looking

    // A block ends after this many literals and matches. Each block carries
    // the Huffman codes it is written with, so a short block follows its own
    // data more closely while a long one spends less on describing the codes.
    private static let BLOCK_TOKENS = 16384
    private static let LITERAL_CODES = 286  // 0-255, 256 ends the block, 257-285 lengths
    private static let DISTANCE_CODES = 30
    private static let CODE_LENGTH_CODES = 19
    private static let MAX_CODE_BITS = 15
    private static let MAX_CODE_LENGTH_BITS = 7
    private static let MAX_STORED = 65535

    // The literals and matches of the block being filled. A distance of zero
    // marks a literal, whose byte is held where a match length would be.
    // These four are written once for every literal and every match, often
    // enough that the check Swift makes before writing into an array held by
    // a class is worth going without: they are plain memory, freed by deinit.
    private let tokenValue: UnsafeMutablePointer<UInt16>
    private let tokenDistance: UnsafeMutablePointer<UInt16>
    private var tokens = 0
    private let literalFrequency: UnsafeMutablePointer<Int32>
    private let distanceFrequency: UnsafeMutablePointer<Int32>
    private var tokenExtraBits = 0          // Extra bits owed by the tokens so far
    private var blockStart = 0              // Where in the input the block begins

    /// Compresses the input into zlib format and writes the result to the output.
    @discardableResult
    public init(_ output: inout [UInt8], _ input: [UInt8]) {
        let BUFSIZE = MASK + 1  // 2^16 bytes
        head = UnsafeMutablePointer<Int32>.allocate(capacity: Int(BUFSIZE))
        head.initialize(repeating: -1, count: Int(BUFSIZE))
        prev = UnsafeMutablePointer<Int32>.allocate(capacity: FlateEncode.WINDOW)
        prev.initialize(repeating: -1, count: FlateEncode.WINDOW)
        bitBuffer = UnsafeMutablePointer<UInt32>.allocate(capacity: 1)
        bitBuffer.initialize(to: 0)
        bitsInBuffer = UnsafeMutablePointer<UInt32>.allocate(capacity: 1)
        bitsInBuffer.initialize(to: 0)
        tokenValue = UnsafeMutablePointer<UInt16>.allocate(
                capacity: FlateEncode.BLOCK_TOKENS)
        tokenDistance = UnsafeMutablePointer<UInt16>.allocate(
                capacity: FlateEncode.BLOCK_TOKENS)
        literalFrequency = UnsafeMutablePointer<Int32>.allocate(
                capacity: FlateEncode.LITERAL_CODES)
        distanceFrequency = UnsafeMutablePointer<Int32>.allocate(
                capacity: FlateEncode.DISTANCE_CODES)
        literalFrequency.initialize(repeating: 0, count: FlateEncode.LITERAL_CODES)
        distanceFrequency.initialize(repeating: 0, count: FlateEncode.DISTANCE_CODES)
        output.reserveCapacity(output.count + input.count / 2 + 64)
        writeCode(&output, UInt32(0x9C78), 16)      // FLG | CMF

        let count = input.count
        // The input read through one pointer, with no check of each index
        input.withUnsafeBufferPointer { bytes in
            var i = 0
            while i < count {
                var length = 0
                var distance = 0
                if i + FlateEncode.MIN_MATCH <= count {
                    let candidate = insert(bytes, i)
                    if candidate >= 0 {
                        longestMatch(bytes, i, candidate, &length, &distance)
                    }
                }
                if length >= FlateEncode.MIN_MATCH {
                    addMatch(length, distance)
                    // The positions the match covers are not hashed: doing so finds
                    // a little more, and costs more time than the whole search.
                    i += length
                } else {
                    addLiteral(bytes[i])
                    i += 1
                }
                if tokens == FlateEncode.BLOCK_TOKENS {
                    writeBlock(&output, input, i, false)
                }
            }
        }
        writeBlock(&output, input, count, true)
        if bitsInBuffer.pointee > 0 {
            output.append(UInt8(truncatingIfNeeded: bitBuffer.pointee))
        }
        addAdler32(&output, input)
    }

    deinit {
        head.deallocate()
        prev.deallocate()
        bitBuffer.deallocate()
        bitsInBuffer.deallocate()
        tokenValue.deallocate()
        tokenDistance.deallocate()
        literalFrequency.deallocate()
        distanceFrequency.deallocate()
    }

    private func addLiteral(_ byte: UInt8) {
        tokenValue[tokens] = UInt16(byte)
        tokenDistance[tokens] = 0
        tokens += 1
        literalFrequency[Int(byte)] += 1
    }

    private func addMatch(_ length: Int, _ distance: Int) {
        let tables = FlateTables.shared
        tokenValue[tokens] = UInt16(length)
        tokenDistance[tokens] = UInt16(distance)
        tokens += 1
        let lengthSymbol = Int(tables.lengthCode[length - FlateEncode.MIN_MATCH])
        let distanceSymbol = FlateTables.distanceCode(distance)
        literalFrequency[257 + lengthSymbol] += 1
        distanceFrequency[distanceSymbol] += 1
        tokenExtraBits += Int(tables.lengthExtra[lengthSymbol])
                + Int(tables.distanceExtra[distanceSymbol])
    }

    // Writes the block that has been filled, in whichever of the three forms
    // RFC 1951 allows comes out smallest. The cost of all three is counted
    // first, in bits, so the block can never grow by being written one way
    // rather than another.
    private func writeBlock(
            _ output: inout [UInt8],
            _ input: [UInt8],
            _ end: Int,
            _ last: Bool) {
        let tables = FlateTables.shared
        literalFrequency[256] += 1          // The symbol that ends the block

        var staticBits = 3
        for symbol in 0..<FlateEncode.LITERAL_CODES {
            staticBits += Int(literalFrequency[symbol])
                    * Int(tables.fixedLiteralLengths[symbol])
        }
        for symbol in 0..<FlateEncode.DISTANCE_CODES {
            staticBits += Int(distanceFrequency[symbol]) * 5
        }
        staticBits += tokenExtraBits

        let literalLengths = FlateHuffman.codeLengths(
                Array(UnsafeBufferPointer(start: literalFrequency,
                        count: FlateEncode.LITERAL_CODES)),
                FlateEncode.MAX_CODE_BITS)
        let distanceLengths = FlateHuffman.codeLengths(
                Array(UnsafeBufferPointer(start: distanceFrequency,
                        count: FlateEncode.DISTANCE_CODES)),
                FlateEncode.MAX_CODE_BITS)
        // The codes the block never reaches are left out of the header.
        var hlit = FlateEncode.LITERAL_CODES
        while hlit > 257 && literalLengths[hlit - 1] == 0 {
            hlit -= 1
        }
        var hdist = FlateEncode.DISTANCE_CODES
        while hdist > 1 && distanceLengths[hdist - 1] == 0 {
            hdist -= 1
        }
        let encoded = FlateHuffman.runLengthEncode(
                Array(literalLengths[0..<hlit]) + Array(distanceLengths[0..<hdist]))
        var codeLengthFrequency = [Int32](
                repeating: 0, count: FlateEncode.CODE_LENGTH_CODES)
        for symbol in encoded.symbols {
            codeLengthFrequency[Int(symbol)] += 1
        }
        let codeLengthLengths = FlateHuffman.codeLengths(
                codeLengthFrequency, FlateEncode.MAX_CODE_LENGTH_BITS)
        var hclen = FlateEncode.CODE_LENGTH_CODES
        while hclen > 4 && codeLengthLengths[tables.codeLengthOrder[hclen - 1]] == 0 {
            hclen -= 1
        }

        var dynamicBits = 3 + 5 + 5 + 4 + 3 * hclen
        for i in 0..<encoded.symbols.count {
            dynamicBits += Int(codeLengthLengths[Int(encoded.symbols[i])])
                    + Int(encoded.extraBits[i])
        }
        for symbol in 0..<FlateEncode.LITERAL_CODES {
            dynamicBits += Int(literalFrequency[symbol]) * Int(literalLengths[symbol])
        }
        for symbol in 0..<FlateEncode.DISTANCE_CODES {
            dynamicBits += Int(distanceFrequency[symbol]) * Int(distanceLengths[symbol])
        }
        dynamicBits += tokenExtraBits

        // Data that does not compress is written as it stands, which costs the
        // padding to the next byte and a four byte header and nothing else.
        var storedBits = Int.max
        let storedLength = end - blockStart
        if storedLength <= FlateEncode.MAX_STORED {
            let padding = (8 - ((Int(bitsInBuffer.pointee) + 3) % 8)) % 8
            storedBits = 3 + padding + 32 + 8 * storedLength
        }

        writeCode(&output, UInt32(last ? 1 : 0), 1)
        if storedBits <= staticBits && storedBits <= dynamicBits {
            writeCode(&output, 0, 2)                // BTYPE: stored
            if bitsInBuffer.pointee > 0 {
                output.append(UInt8(truncatingIfNeeded: bitBuffer.pointee))
                bitBuffer.pointee = 0
                bitsInBuffer.pointee = 0
            }
            let length = UInt16(storedLength)
            let complement = ~length
            output.append(UInt8(length & 0xFF))
            output.append(UInt8(length >> 8))
            output.append(UInt8(complement & 0xFF))
            output.append(UInt8(complement >> 8))
            output.append(contentsOf: input[blockStart..<end])
        } else if staticBits <= dynamicBits {
            writeCode(&output, 1, 2)                // BTYPE: fixed codes
            writeTokens(&output,
                    tables.fixedLiteralCodes, tables.fixedLiteralLengths,
                    tables.fixedDistanceCodes, tables.fixedDistanceLengths)
        } else {
            writeCode(&output, 2, 2)                // BTYPE: codes of its own
            writeCode(&output, UInt32(hlit - 257), 5)
            writeCode(&output, UInt32(hdist - 1), 5)
            writeCode(&output, UInt32(hclen - 4), 4)
            for i in 0..<hclen {
                writeCode(&output,
                        UInt32(codeLengthLengths[tables.codeLengthOrder[i]]), 3)
            }
            let codeLengthCodes = FlateTables.canonicalCodes(codeLengthLengths)
            for i in 0..<encoded.symbols.count {
                let symbol = Int(encoded.symbols[i])
                writeCode(&output, codeLengthCodes[symbol], codeLengthLengths[symbol])
                if encoded.extraBits[i] > 0 {
                    writeCode(&output, encoded.extra[i], encoded.extraBits[i])
                }
            }
            writeTokens(&output,
                    FlateTables.canonicalCodes(literalLengths), literalLengths,
                    FlateTables.canonicalCodes(distanceLengths), distanceLengths)
        }

        tokens = 0
        tokenExtraBits = 0
        for symbol in 0..<FlateEncode.LITERAL_CODES {
            literalFrequency[symbol] = 0
        }
        for symbol in 0..<FlateEncode.DISTANCE_CODES {
            distanceFrequency[symbol] = 0
        }
        blockStart = end
    }

    // The literals and matches of the block, in the codes given. The code of a
    // symbol and the extra bits that follow it are written separately: a
    // dynamic code of 15 bits and a distance of 13 extra bits do not fit the
    // bit buffer together.
    private func writeTokens(
            _ output: inout [UInt8],
            _ literalCodes: [UInt32],
            _ literalLengths: [UInt8],
            _ distanceCodes: [UInt32],
            _ distanceLengths: [UInt8]) {
        // The tables are taken out of the shared object once rather than once
        // a token: reaching through a class for an array is not free.
        let tables = FlateTables.shared
        let lengthCode = tables.lengthCode
        let lengthExtra = tables.lengthExtra
        let lengthBase = tables.lengthBase
        let distanceExtra = tables.distanceExtra
        let distanceBase = tables.distanceBase
        for token in 0..<tokens {
            let distance = Int(tokenDistance[token])
            if distance == 0 {
                let literal = Int(tokenValue[token])
                writeCode(&output, literalCodes[literal], literalLengths[literal])
            } else {
                let length = Int(tokenValue[token])
                let lengthSymbol = Int(lengthCode[length - FlateEncode.MIN_MATCH])
                writeCode(&output, literalCodes[257 + lengthSymbol],
                        literalLengths[257 + lengthSymbol])
                if lengthExtra[lengthSymbol] > 0 {
                    writeCode(&output, UInt32(length - lengthBase[lengthSymbol]),
                            lengthExtra[lengthSymbol])
                }
                let distanceSymbol = FlateTables.distanceCode(distance)
                writeCode(&output,
                        distanceCodes[distanceSymbol], distanceLengths[distanceSymbol])
                if distanceExtra[distanceSymbol] > 0 {
                    writeCode(&output, UInt32(distance - distanceBase[distanceSymbol]),
                            distanceExtra[distanceSymbol])
                }
            }
        }
        writeCode(&output, literalCodes[256], literalLengths[256])
    }

    // Adds the position to the chain of its hash and returns the position that
    // was at the head of that chain, which is where a match is looked for.
    private func insert(_ input: UnsafeBufferPointer<UInt8>, _ i: Int) -> Int32 {
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
            _ input: UnsafeBufferPointer<UInt8>,
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
        var buffer = bitBuffer.pointee | (UInt32(code) << bitsInBuffer.pointee)
        var count = bitsInBuffer.pointee + UInt32(nBits)
        while count >= 8 {
            output.append(UInt8(buffer & 0xFF))
            buffer >>= 8
            count -= 8
        }
        bitBuffer.pointee = buffer
        bitsInBuffer.pointee = count
    }

    private func addAdler32(_ output: inout [UInt8], _ input: [UInt8]) {
        let adler = adler32(input)
        output.append(contentsOf: [
            UInt8((adler >> 24) & 0xFF), UInt8((adler >> 16) & 0xFF),
            UInt8((adler >>  8) & 0xFF), UInt8(adler & 0xFF)
        ])
    }
}

/// Returns the Adler-32 checksum of the data, which ends a zlib stream.
func adler32(_ data: [UInt8]) -> UInt32 {
    return data.withUnsafeBufferPointer { adler32($0) }
}

/// Returns the Adler-32 checksum of the bytes.
func adler32(_ data: UnsafeBufferPointer<UInt8>) -> UInt32 {
    let prime: UInt32 = 65521
    var s1: UInt32 = 1
    var s2: UInt32 = 0
    var i = 0
    while i < data.count {
        // 5552 bytes is the most that the sums can take before they overflow.
        var chunk = min(5552, data.count - i)
        while chunk > 0 {
            s1 &+= UInt32(data[i])
            s2 &+= s1
            i += 1
            chunk -= 1
        }
        s1 %= prime
        s2 %= prime
    }
    return (s2 &<< 16) &+ s1
}
