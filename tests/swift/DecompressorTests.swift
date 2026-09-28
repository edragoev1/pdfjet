/**
 * DecompressorTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// The stream filters and predictors that reading a PDF needs, and the compressor.
/// Swift has these as the lzwDecode, asciiHexDecode, ascii85Decode,
/// runLengthDecode and applyPredictor functions, Puff and FlateEncode.
@Suite struct DecompressorTests {
    private func ascii(_ text: String) -> [UInt8] {
        return Array(text.utf8)
    }

    @Test func lzwDecodesTheExampleOfTheStandard() throws {
        // ISO 32000-1, 7.4.4.2: the encoding of "-----A---B".
        let encoded: [UInt8] = [0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01]
        #expect(try TestSupport.latin1(lzwDecode(encoded)) == "-----A---B")
    }

    @Test func asciiHexDecodeSkipsWhitespaceAndStopsAtTheEndMarker() {
        #expect(TestSupport.latin1(asciiHexDecode(ascii("48 65 6C6C6F>"))) == "Hello")
    }

    @Test func asciiHexDecodeReadsAMissingLastDigitAsZero() {
        #expect(asciiHexDecode(ascii("7>")) == [0x70])
    }

    @Test func ascii85DecodesWithAndWithoutThePrefix() {
        // base64.a85encode(b"Hello world") in Python.
        #expect(TestSupport.latin1(ascii85Decode(ascii("87cURD]j7BEbo7~>"))) == "Hello world")
        #expect(TestSupport.latin1(ascii85Decode(ascii("<~87cURD]j7BEbo7~>"))) == "Hello world")
    }

    @Test func ascii85DecodesZAsFourZeroBytesAndAPartialLastGroup() {
        #expect(ascii85Decode(ascii("z~>")) == [0, 0, 0, 0])
        #expect(ascii85Decode(ascii("z@:B~>")) == [0, 0, 0, 0, 97, 98])
    }

    @Test func runLengthDecodeCopiesLiteralsAndRepeatsRuns() throws {
        let encoded: [UInt8] = [2, 97, 98, 99, 254, 120, 128]
        #expect(try TestSupport.latin1(runLengthDecode(encoded)) == "abcxxx")
    }

    @Test func pngPredictorsUndoEachRowFilter() {
        // Each row is the filter type and three one byte samples.
        #expect(applyPredictor([1, 1, 1, 1], 11, 1, 8, 3) == [1, 2, 3])
        #expect(applyPredictor([0, 1, 2, 3, 2, 1, 1, 1], 12, 1, 8, 3) == [1, 2, 3, 2, 3, 4])
        #expect(applyPredictor([3, 2, 4, 6], 13, 1, 8, 3) == [2, 5, 8])
        #expect(applyPredictor([4, 1, 1, 1], 14, 1, 8, 3) == [1, 2, 3])
    }

    @Test func tiffPredictorAddsTheSampleToTheLeft() {
        #expect(applyPredictor([1, 1, 1, 5, 0, 0], 2, 1, 8, 3) == [1, 2, 3, 5, 5, 5])
    }

    @Test func predictorOneAndInvalidParametersLeaveTheDataAsItIs() {
        let data: [UInt8] = [9, 8, 7]
        #expect(applyPredictor(data, 1, 1, 8, 3) == data)
        #expect(applyPredictor(data, 12, 0, 8, 3) == data)
    }

    @Test func inflateUndoesDeflate() throws {
        var data = TestSupport.randomBytes(100000, 42)
        for i in 50000..<90000 {
            data[i] = 120
        }
        #expect(try TestSupport.inflate(TestSupport.deflate(data)) == data)
    }

    @Test func inflateRejectsATruncatedStream() {
        let deflated = TestSupport.deflate(ascii("hello hello hello hello"))
        #expect(throws: (any Error).self) {
            _ = try TestSupport.inflate(Array(deflated.prefix(6)))
        }
    }

    @Test func inflateIgnoresBytesAfterTheEndOfAStream() throws {
        let data = ascii("hello hello hello hello")
        var padded = TestSupport.deflate(data)
        padded.append(contentsOf: [13, 10, 0x42])
        #expect(try TestSupport.inflate(padded) == data)
    }

    @Test func inflateRejectsEveryTruncationOfAStream() throws {
        var data = TestSupport.randomBytes(1000, 3)
        data.append(contentsOf: [UInt8](repeating: UInt8(ascii: "a"), count: 1000))
        let deflated = TestSupport.deflate(data)
        #expect(try TestSupport.inflate(deflated) == data)
        // The last 4 bytes are the Adler-32 checksum, which is checked too.
        for length in 0..<deflated.count {
            #expect(throws: (any Error).self, "length \(length)") {
                _ = try TestSupport.inflate(Array(deflated.prefix(length)))
            }
        }
    }

    @Test func inflateRejectsAWrongChecksumOrHeader() {
        let deflated = TestSupport.deflate(Array("PDFjet PDFjet PDFjet".utf8))
        var wrongChecksum = deflated
        wrongChecksum[wrongChecksum.count - 1] ^= 1
        #expect(throws: (any Error).self) { _ = try TestSupport.inflate(wrongChecksum) }
        var wrongMethod = deflated
        wrongMethod[0] = 0x77      // Method 7, and the check no longer fits
        #expect(throws: (any Error).self) { _ = try TestSupport.inflate(wrongMethod) }
        var wrongCheck = deflated
        wrongCheck[1] ^= 1
        #expect(throws: (any Error).self) { _ = try TestSupport.inflate(wrongCheck) }
    }

    @Test func inflatePrefixWithItsBytesNeedsNoChecksum() throws {
        let data = Array("PDFjet PDFjet PDFjet".utf8)
        var deflated = TestSupport.deflate(data)
        deflated[deflated.count - 1] ^= 1
        #expect(try inflatePrefix(deflated, 5) == Array(data.prefix(5)))
        // All the bytes, and the checksum wrong or cut off.
        #expect(try inflatePrefix(deflated, data.count) == data)
        #expect(try inflatePrefix(Array(deflated.dropLast(4)), data.count) == data)
        #expect(throws: (any Error).self) { _ = try inflatePrefix(deflated, 100) }
    }

    @Test func inflateExactIsAStreamOfTheBytesAndNothingMore() throws {
        let data = Array("PDFjet PDFjet PDFjet".utf8)
        let deflated = TestSupport.deflate(data)
        var wrongChecksum = deflated
        wrongChecksum[wrongChecksum.count - 1] ^= 1
        let cases: [(String, [UInt8], Int, Bool)] = [
            ("the whole stream", deflated, data.count, true),
            ("fewer bytes than it decodes to", deflated, data.count - 1, false),
            ("a byte after its end", deflated + [0], data.count, false),
            ("a wrong checksum", wrongChecksum, data.count, false),
            ("no checksum", Array(deflated.dropLast(4)), data.count, false),
            ("part of the checksum", Array(deflated.dropLast(1)), data.count, false),
        ]
        for (name, stream, length, exact) in cases {
            let (prefix, isExact) = try inflateExact(stream, length)
            #expect(prefix == Array(data.prefix(length)), "\(name)")
            #expect(isExact == exact, "\(name)")
        }
    }

    @Test func theDecodedLengthLimitIs256MiB() {
        #expect(MAX_DECODED_LENGTH == 256 * 1024 * 1024)
    }

    @Test func inflateRejectsDataThatDecodesToMoreThanTheLimit() throws {
        let deflated = TestSupport.deflate([UInt8](repeating: 0, count: 1000))
        #expect(try inflate(deflated, 1000).count == 1000)
        let error = #expect(throws: (any Error).self) { _ = try inflate(deflated, 999) }
        #expect(TestSupport.message(error) == "Flate data decodes to more than 999 bytes")
    }

    @Test func inflatePrefixReadsNoSymbolPastTheBytesItNeeds() throws {
        // A zlib stream of one block of fixed codes that is cut short: it
        // gives 80 bytes and then ends in the middle of the symbol after
        // them. The prefix of those 80 bytes is there, and one of 81 is not.
        let data: [UInt8] = [0x78, 0x9C, 0x32] + [UInt8](repeating: 0x30, count: 80)
        #expect(try inflatePrefix(data, 80) == [UInt8](repeating: UInt8(ascii: "0"), count: 80))
        #expect(throws: (any Error).self) { try inflatePrefix(data, 81) }
        #expect(throws: (any Error).self) { try inflate(data) }
    }

    @Test func inflatePrefixReturnsTheFirstBytesAndIgnoresTheRest() throws {
        let data = ascii("hello hello hello hello")
        let deflated = TestSupport.deflate(data)
        #expect(try inflatePrefix(deflated, 5) == Array(data.prefix(5)))
        #expect(try inflatePrefix(deflated, 100) == data)
        #expect(throws: (any Error).self) { _ = try inflatePrefix(Array(deflated.prefix(6)), 20) }
    }

    @Test func lzwDecodeRejectsDataThatDecodesToMoreThanTheLimit() throws {
        let encoded: [UInt8] = [0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01]
        #expect(try lzwDecode(encoded, 10).count == 10)
        let error = #expect(throws: (any Error).self) { _ = try lzwDecode(encoded, 9) }
        #expect(TestSupport.message(error) == "LZW data decodes to more than 9 bytes")
    }

    @Test func runLengthDecodeRejectsDataThatDecodesToMoreThanTheLimit() throws {
        let encoded: [UInt8] = [2, 97, 98, 99, 254, 120, 128]
        #expect(try runLengthDecode(encoded, 6).count == 6)
        let error = #expect(throws: (any Error).self) { _ = try runLengthDecode(encoded, 5) }
        #expect(TestSupport.message(error) == "RunLength data decodes to more than 5 bytes")
    }

    @Test func deflateOfNoBytesIsAnEmptyZlibStream() {
        let deflated = TestSupport.deflate([])
        #expect(deflated.count == 8)
        #expect(deflated.first == 0x78)
    }

    @Test func inflateAcceptsAnEmptyStream() throws {
        #expect(try TestSupport.inflate(TestSupport.deflate([])).isEmpty)
    }

    @Test func inflateDecodesCodesLongerThanItsTableAndStopsAtEveryLength() throws {
        // Bytes of which each group of 16 is half as frequent as the one
        // before, whose codes are up to 15 bits long, bytes that do not
        // compress, and both.
        var x: UInt32 = 1
        var skewed = [UInt8]()
        var random = [UInt8]()
        for _ in 0..<50000 {
            x = x &* 1664525 &+ 1013904223
            skewed.append(UInt8(truncatingIfNeeded: (x >> 16).trailingZeroBitCount * 16 + Int(x >> 28)))
            random.append(UInt8(truncatingIfNeeded: x >> 24))
        }
        for data in [skewed, random, skewed + random] {
            let deflated = TestSupport.deflate(data)
            #expect(try inflate(deflated) == data)
            let (exact, isExact) = try inflateExact(deflated, data.count)
            #expect(isExact && exact == data)
            for length in stride(from: 0, through: data.count, by: 997) {
                #expect(try inflatePrefix(deflated, length) == Array(data.prefix(length)))
            }
            #expect(throws: (any Error).self) { _ = try inflate(deflated, data.count - 1) }
        }
    }

    @Test func crc32OfEightBytesAtATimeIsTheCRC32OfTheBytes() {
        var data = [UInt8]()
        for i in 0..<100003 {
            data.append(UInt8(truncatingIfNeeded: i &* 7919 &+ i >> 5))
        }
        let crc = CRC32()
        crc.update(data, 0, data.count)
        #expect(crc.getValue() == UInt32(TestSupport.crc32(data), radix: 16))
        // In pieces that do not begin at a multiple of eight.
        let pieces = CRC32()
        pieces.update(data, 0, 5)
        pieces.update(data, 5, 12)
        pieces.update(data, 12, data.count)
        #expect(pieces.getValue() == crc.getValue())
        let check = CRC32()
        check.update(ascii("123456789"), 0, 9)
        #expect(check.getValue() == 0xCBF43926)
    }
}
