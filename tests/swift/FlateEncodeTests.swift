/**
 * FlateEncodeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// The deflate encoder: the Huffman codes it builds, the form it picks for
/// each block, and that everything it writes reads back as it went in.
@Suite struct FlateEncodeTests {
    private func roundTrips(_ data: [UInt8], _ what: String) throws {
        let deflated = TestSupport.deflate(data)
        #expect(try TestSupport.inflate(deflated) == data, "\(what)")
    }

    /// The type of the first block of a stream, as RFC 1951 3.2.3 numbers them.
    private func firstBlockType(_ deflated: [UInt8]) -> Int {
        return Int(deflated[2] >> 1) & 3
    }

    // A set of code lengths is a complete Huffman code when the lengths
    // satisfy Kraft's equality, which is what every decoder checks for.
    private func isComplete(_ lengths: [UInt8]) -> Bool {
        var total = 0
        for length in lengths where length > 0 {
            total += 1 << (15 - Int(length))
        }
        return total == 1 << 15
    }

    @Test func everyBlockTypeReadsBackAsItWentIn() throws {
        try roundTrips([], "no bytes at all")
        try roundTrips([42], "a single byte")
        try roundTrips([1, 2], "two bytes, too few for a match")
        try roundTrips([UInt8](repeating: 7, count: 300000), "one byte over and over")
        try roundTrips(TestSupport.randomBytes(200000, 11), "bytes that do not compress")
        try roundTrips(Array("hello ".utf8) + Array(repeating: 32, count: 70000),
                "a short head and a long run")
    }

    @Test func aBlockOfRandomBytesIsStoredRatherThanGrown() {
        let random = TestSupport.randomBytes(40000, 5)
        let deflated = TestSupport.deflate(random)
        #expect(firstBlockType(deflated) == 0, "\(firstBlockType(deflated))")
        // Storing costs five bytes a block and nothing else, so the stream is
        // never more than a hair larger than the bytes put into it.
        #expect(deflated.count < random.count + 64, "\(deflated.count)")
    }

    @Test func aBlockOfRepeatedTextGetsCodesOfItsOwn() {
        let text = Array("the quick brown fox jumps over the lazy dog. ".utf8)
        var data = [UInt8]()
        for _ in 0..<4000 {
            data.append(contentsOf: text)
        }
        let deflated = TestSupport.deflate(data)
        #expect(firstBlockType(deflated) == 2, "\(firstBlockType(deflated))")
    }

    @Test func theTokenBufferBoundaryIsCrossedCleanly() throws {
        // A block is closed every 16384 literals and matches, so the sizes
        // around that are the ones where a block can be lost or doubled.
        for count in [16383, 16384, 16385, 32768, 32769] {
            var data = [UInt8]()
            var state = UInt64(count)
            while data.count < count {
                // Bytes that repeat too little to ever form a match, so that
                // one byte is one token.
                state = state &* 6364136223846793005 &+ 1442695040888963407
                data.append(UInt8(truncatingIfNeeded: state >> 33))
            }
            try roundTrips(data, "\(count) bytes")
        }
    }

    @Test func matchesAtBothEndsOfTheDistanceRangeReadBack() throws {
        // A distance of 1 is the shortest there is, and the run below reaches
        // back the full 32768 of the window for the second copy.
        var data = [UInt8](repeating: 65, count: 100)
        data.append(contentsOf: TestSupport.randomBytes(32668, 9))
        data.append(contentsOf: data.prefix(300))
        try roundTrips(data, "the ends of the distance range")
    }

    @Test func codeLengthsAreNeverLongerThanTheLimit() {
        // Fibonacci weights are what makes a Huffman tree deepest: with this
        // many symbols the tree runs past 15 levels and has to be flattened.
        var frequency = [Int32](repeating: 0, count: 286)
        var a: Int32 = 1
        var b: Int32 = 1
        for symbol in 0..<40 {
            frequency[symbol] = a
            (a, b) = (b, a &+ b)
        }
        let lengths = FlateHuffman.codeLengths(frequency, 15)
        #expect(lengths.max() ?? 0 <= 15, "\(lengths.max() ?? 0)")
        #expect(isComplete(lengths))
    }

    @Test func aTreeIsAlwaysCompleteEvenWithNothingToCode() {
        // A block with no matches has no distances at all, and a block can use
        // a single symbol. Both still need a code a decoder will accept.
        #expect(isComplete(FlateHuffman.codeLengths([Int32](repeating: 0, count: 30), 15)))
        var one = [Int32](repeating: 0, count: 30)
        one[7] = 99
        let lengths = FlateHuffman.codeLengths(one, 15)
        #expect(lengths[7] == 1)
        #expect(isComplete(lengths))
    }

    @Test func theCommonerSymbolNeverGetsTheLongerCode() {
        var frequency = [Int32](repeating: 0, count: 286)
        for symbol in 0..<286 {
            frequency[symbol] = Int32((symbol &* 37) % 101) + 1
        }
        let lengths = FlateHuffman.codeLengths(frequency, 15)
        #expect(isComplete(lengths))
        for a in 0..<286 {
            for b in 0..<286 where frequency[a] > frequency[b] {
                #expect(lengths[a] <= lengths[b], "\(a) over \(b)")
            }
        }
    }

    @Test func runsOfCodeLengthsAreFoldedAndUnfold() {
        // RFC 1951 3.2.7: 16 repeats what came before, 17 and 18 run zeros.
        var lengths = [UInt8](repeating: 4, count: 20)
        lengths.append(contentsOf: [UInt8](repeating: 0, count: 200))
        lengths.append(9)
        let encoded = FlateHuffman.runLengthEncode(lengths)
        #expect(encoded.symbols.contains(16))
        #expect(encoded.symbols.contains(18))
        var unfolded = [UInt8]()
        for i in 0..<encoded.symbols.count {
            switch encoded.symbols[i] {
            case 16:
                let last = unfolded[unfolded.count - 1]
                unfolded.append(contentsOf: [UInt8](repeating: last,
                        count: Int(encoded.extra[i]) + 3))
            case 17:
                unfolded.append(contentsOf: [UInt8](repeating: 0,
                        count: Int(encoded.extra[i]) + 3))
            case 18:
                unfolded.append(contentsOf: [UInt8](repeating: 0,
                        count: Int(encoded.extra[i]) + 11))
            default:
                unfolded.append(encoded.symbols[i])
            }
        }
        #expect(unfolded == lengths)
    }

    @Test func theCodesOfTheFixedBlockAreTheOnesOfTheStandard() {
        // RFC 1951 3.2.6 gives the fixed codes outright: 0 is 00110000 and
        // 143 is 10111111, both eight bits, and 144 is 110010000 in nine.
        let tables = FlateTables.shared
        #expect(tables.fixedLiteralCodes[0] == FlateUtils.reverse(0b00110000, length: 8))
        #expect(tables.fixedLiteralCodes[143] == FlateUtils.reverse(0b10111111, length: 8))
        #expect(tables.fixedLiteralCodes[144] == FlateUtils.reverse(0b110010000, length: 9))
        #expect(tables.fixedLiteralCodes[256] == FlateUtils.reverse(0b0000000, length: 7))
        #expect(tables.fixedLiteralCodes[280] == FlateUtils.reverse(0b11000000, length: 8))
    }

    @Test func everyLengthAndDistanceMapsToTheCodeOfTheStandard() {
        let tables = FlateTables.shared
        #expect(tables.lengthCode[3 - 3] == 0)          // code 257
        #expect(tables.lengthCode[10 - 3] == 7)         // code 264
        #expect(tables.lengthCode[11 - 3] == 8)         // code 265, one extra bit
        #expect(tables.lengthCode[257 - 3] == 27)       // code 284
        #expect(tables.lengthCode[258 - 3] == 28)       // code 285, on its own
        for length in 3...258 {
            let symbol = Int(tables.lengthCode[length - 3])
            let base = tables.lengthBase[symbol]
            #expect(length >= base, "length \(length)")
            #expect(length - base < (1 << Int(tables.lengthExtra[symbol])), "length \(length)")
        }
        for distance in 1...32768 {
            let symbol = FlateTables.distanceCode(distance)
            let base = tables.distanceBase[symbol]
            #expect(distance >= base, "distance \(distance)")
            #expect(distance - base < (1 << Int(tables.distanceExtra[symbol])),
                    "distance \(distance)")
        }
    }
}
