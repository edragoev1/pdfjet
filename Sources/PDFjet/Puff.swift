/**
 * Puff.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/*
  puff.c

  Copyright 2002-2013 Mark Adler, all rights reserved
  version 2.3, 21 Jan 2013

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the author be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Mark Adler    madler@alumni.caltech.edu
 */

/*
 * Puff.swift is a conversion from the original puff.c by Mark Adler.
 * All credit goes to the original author.
 *
 * Evgeni Dragoev
 * edragoev@protonmail.com
 */
import Foundation

struct Huffman {
    var count: [Int]    // number of symbols of each length
    var symbol: [Int]   // canonically ordered symbols
}

enum PuffError: Error {
    case read(error: Int)
}

/// The largest number of bytes that a stream, or the samples of an image, may
/// decode to: 256 MiB. A few kilobytes of Flate, LZW or RunLength data can
/// decode to gigabytes, so a decoder that would go past it throws.
let MAX_DECODED_LENGTH = 256 * 1024 * 1024

/// Decodes a zlib stream, which must end and decode to at most maxLength
/// bytes. Bytes after the end of the stream are ignored.
func inflate(_ data: [UInt8], _ maxLength: Int = MAX_DECODED_LENGTH) throws -> [UInt8] {
    var input = data
    var output = [UInt8]()
    _ = try Puff(output: &output, input: &input, maxLength: maxLength, prefix: false)
    return output
}

/// Returns the first length bytes that a zlib stream decodes to, or all of them
/// when there are fewer, and ignores the rest of the stream. A stream that ends
/// in the middle, before those bytes, throws.
func inflatePrefix(_ data: [UInt8], _ length: Int) throws -> [UInt8] {
    var input = data
    var output = [UInt8]()
    _ = try Puff(output: &output, input: &input, maxLength: length, prefix: true, sized: true)
    return output
}

/// Returns the first length bytes that a zlib stream decodes to, as
/// inflatePrefix does, and whether the stream is those bytes and nothing more:
/// it decodes to no more bytes, ends with the right checksum, and has no bytes
/// after its end.
func inflateExact(_ data: [UInt8], _ length: Int) throws -> ([UInt8], Bool) {
    // A whole stream of at most length bytes, whose checksum is checked; any
    // other stream is decoded again as a prefix, which fails as it does.
    var input = data
    var output = [UInt8]()
    if let puff = try? Puff(output: &output, input: &input, maxLength: length, prefix: false, sized: true),
            output.count == length && puff.incnt + 4 == data.count {
        return (output, true)
    }
    return (try inflatePrefix(data, length), false)
}

/// Decompresses Deflate data. A Swift port of puff.c by Mark Adler, which
/// decodes the codes of up to FASTBITS bits with a table, a step for each code
/// instead of a step for each bit.
final class Puff {
    // Maximums for allocations and loops.
    // It is not useful to change these -- they are fixed by the deflate format.
    let MAXBITS: Int = 15       // maximum bits in a code
    let MAXLCODES: Int = 286    // maximum number of literal/length codes
    let MAXDCODES: Int = 30     // maximum number of distance codes
    var MAXCODES: Int = 316     // maximum codes lengths to read
    let FIXLCODES: Int = 288    // number of fixed literal/length codes

    // The bits a code table decodes in one step, and the most bits a length
    // and its distance take: two codes of 15 bits, and 5 and 13 extra bits.
    // A symbol is decoded bit by bit, as puff.c decodes it, when fewer bits
    // than that are left in the input.
    private static let FASTBITS = 12
    private static let MAXPAIRBITS = 48

    // output limit
    let maxLength: Int          // the most bytes the output may have
    let prefix: Bool            // stop at maxLength bytes instead of throwing

    // input state
    var incnt = 0               // bytes read so far from the input
    var bitbuf: UInt32 = 0      // bit buffer
    var bitcnt = 0              // number of bits in bit buffer
    // The input, while the stream is decoded.
    private var input = UnsafeBufferPointer<UInt8>(start: nil, count: 0)

    // The output: the memory of the output array, when the output is
    // expected to have maxLength bytes, or else memory of its own that grows
    // as it fills, up to maxLength bytes, and is appended to the output array
    // once the stream is decoded.
    private var out: UnsafeMutablePointer<UInt8>?
    private var outCount = 0
    private var outCapacity = 0
    private var ownsOut = false         // whether deinit frees the memory

    // The tables of the codes of a block, and of the fixed codes: for each
    // FASTBITS bits of the input, the symbol of the code that they begin
    // with, shifted left by 4, and the length of the code; 0 for a code that
    // is longer or is not in the table.
    private let lenTable: UnsafeMutablePointer<UInt16>
    private let distTable: UnsafeMutablePointer<UInt16>
    private let fixedLenTable: UnsafeMutablePointer<UInt16>
    private let fixedDistTable: UnsafeMutablePointer<UInt16>

    // Size base for length codes 257..285
    let lens = [
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
            35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 ]

    // Extra bits for length codes 257..285
    let lext = [
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 ]

    // Offset base for distance codes 0..29
    let dists = [
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
            257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
            8193, 12289, 16385, 24577 ]

    // Extra bits for distance codes 0..29
    let dext = [
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
            12, 12, 13, 13 ]

    var virgin = true

    var lencode: Huffman?
    var distcode: Huffman?

    /*
     * Inflate source to dest.  On return, destlen and sourcelen are updated to the
     * size of the uncompressed data and the size of the deflate data respectively.
     * On success, the return value of puff() is zero.  If there is an error in the
     * source data, i.e. it is not in the deflate format, then a negative value is
     * returned.  If there is not enough input available or there is not enough
     * output space, then a positive error is returned.  In that case, destlen and
     * sourcelen are not updated to facilitate retrying from the beginning with the
     * provision of more input data or more output space.  In the case of invalid
     * inflate data (a negative error), the dest and source pointers are updated to
     * facilitate the debugging of deflators.
     *
     * puff() also has a mode to determine the size of the uncompressed output with
     * no output written.  For this dest must be (unsigned char *)0.  In this case,
     * the input value of *destlen is ignored, and on return *destlen is set to the
     * size of the uncompressed output.
     *
     * The return codes are:
     *
     *   2:  available inflate data did not terminate
     *   1:  output space exhausted before completing inflate
     *   0:  successful inflate
     *  -1:  invalid block type (type == 3)
     *  -2:  stored block length did not match one's complement
     *  -3:  dynamic block code description: too many length or distance codes
     *  -4:  dynamic block code description: code lengths codes incomplete
     *  -5:  dynamic block code description: repeat lengths with no first length
     *  -6:  dynamic block code description: repeat more than specified lengths
     *  -7:  dynamic block code description: invalid literal/length code lengths
     *  -8:  dynamic block code description: invalid distance code lengths
     *  -9:  dynamic block code description: missing end-of-block code
     * -10:  invalid literal/length or distance code in fixed or dynamic block
     * -11:  distance is too far back in fixed or dynamic block
     *
     * Format notes:
     *
     * - Three bits are read for each block to determine the kind of block and
     *   whether or not it is the last block.  Then the block is decoded and the
     *   process repeated if it was not the last block.
     *
     * - The leftover bits in the last byte of the deflate data after the last
     *   block (if it was a fixed or dynamic block) are undefined and have no
     *   expected values to check.
     */
    public init(
            output: inout [UInt8],
            input: inout [UInt8],
            maxLength: Int = MAX_DECODED_LENGTH,
            prefix: Bool = false,
            sized: Bool = false) throws {
        self.maxLength = maxLength
        self.prefix = prefix
        let tableSize = 1 << Puff.FASTBITS
        self.lenTable = UnsafeMutablePointer<UInt16>.allocate(capacity: tableSize)
        self.distTable = UnsafeMutablePointer<UInt16>.allocate(capacity: tableSize)
        self.fixedLenTable = UnsafeMutablePointer<UInt16>.allocate(capacity: tableSize)
        self.fixedDistTable = UnsafeMutablePointer<UInt16>.allocate(capacity: tableSize)
        if sized && output.isEmpty && maxLength > 0 {
            // The output is expected to have maxLength bytes, as the rows of
            // an image do, and is decoded into the array, with no copy. The
            // memory of the bytes that are not written is never touched.
            output = try [UInt8](unsafeUninitializedCapacity: maxLength) { buffer, initialized in
                self.out = buffer.baseAddress
                self.outCapacity = buffer.count
                defer {
                    initialized = self.outCount
                    self.out = nil
                }
                try inflate(&input)
            }
            return
        }
        // Four bytes of output for each byte of input to begin with, which
        // grows as it fills.
        let (fourTimes, overflow) = input.count.multipliedReportingOverflow(by: 4)
        self.outCapacity = min(maxLength, max(65536, overflow ? Int.max : fourTimes))
        self.out = UnsafeMutablePointer<UInt8>.allocate(capacity: max(outCapacity, 1))
        self.ownsOut = true
        defer {
            output.append(contentsOf: UnsafeBufferPointer(start: out, count: outCount))
        }
        try inflate(&input)
    }

    deinit {
        if ownsOut {
            out?.deallocate()
        }
        lenTable.deallocate()
        distTable.deallocate()
        fixedLenTable.deallocate()
        fixedDistTable.deallocate()
    }

    // Decodes the input, which is held as a pointer while it is decoded.
    private final func inflate(_ input: inout [UInt8]) throws {
        try input.withUnsafeBufferPointer { buffer in
            self.input = buffer
            defer { self.input = UnsafeBufferPointer(start: nil, count: 0) }
            try inflate()
        }
    }

    private final func inflate() throws {
        var last: Int?                              // block information
        var type: Int?
        var error = 0                               // return value
        // The zlib header: the Deflate method with a window of at most 32K, a
        // check that makes the two bytes a multiple of 31, and no preset
        // dictionary, as the other ports require.
        if input.count < 2 {
            throw PuffError.read(error: 2)          // not enough input
        }
        let cmf = Int(input[0])
        let flg = Int(input[1])
        if cmf & 0x0F != 8 || cmf >> 4 > 7 || (cmf << 8 | flg) % 31 != 0 || flg & 0x20 != 0 {
            throw PDFjetError(message: "Invalid zlib header")
        }
        self.incnt = 2

        // process blocks until last block or error; a prefix that has its
        // bytes ignores the rest of the stream, its checksum too
        repeat {
            if prefix && outCount >= maxLength {
                return
            }
            last = try bits(1)                      // one if last block
            type = try bits(2)                      // block type 0..3
            if type == 0 {
                error = try stored()
            } else if type == 1 {
                error = try fixed()
            } else if type == 2 {
                error = try dynamic()
            } else {
                error = -1                          // type == 3, invalid
            }
            if error == 1 && prefix {
                return                              // the output has its length
            }
            if error != 0 {
                throw PuffError.read(error: error)  // return with error
            }
        } while last != 1
        if prefix && outCount >= maxLength {
            return
        }

        // The Adler-32 of the output follows the last block, from the next
        // whole byte. A stream without it is cut short.
        if self.incnt + 4 > input.count {
            throw PuffError.read(error: 2)          // not enough input
        }
        let checksum = UInt32(input[self.incnt]) << 24 | UInt32(input[self.incnt + 1]) << 16 |
                UInt32(input[self.incnt + 2]) << 8 | UInt32(input[self.incnt + 3])
        if checksum != adler32(UnsafeBufferPointer(start: out, count: outCount)) {
            throw PDFjetError(message: "Invalid zlib checksum")
        }
    }

    // Makes room in the output for at least count bytes, up to maxLength.
    private final func reserve(_ count: Int) {
        if count <= outCapacity {
            return
        }
        let capacity = min(maxLength, max(count, outCapacity * 2))
        let grown = UnsafeMutablePointer<UInt8>.allocate(capacity: capacity)
        grown.initialize(from: out!, count: outCount)
        out!.deallocate()
        out = grown
        outCapacity = capacity
    }

    // Called when the output has maxLength bytes and needs another one: a
    // prefix stops there, with the return value 1, and any other stream throws.
    private final func stopAtFullOutput() throws -> Int {
        if prefix {
            return 1
        }
        throw PDFjetError(message: "Flate data decodes to more than \(maxLength) bytes")
    }

    /*
     * Process a fixed codes block.
     *
     * Format notes:
     *
     * - This block type can be useful for compressing small amounts of data for
     *   which the size of the code descriptions in a dynamic block exceeds the
     *   benefit of custom codes for that block.  For fixed codes, no bits are
     *   spent on code descriptions.  Instead the code lengths for literal/length
     *   codes and distance codes are fixed.  The specific lengths for each symbol
     *   can be seen in the "for" loops below.
     *
     * - The literal/length code is complete, but has two symbols that are invalid
     *   and should result in an error if received.  This cannot be implemented
     *   simply as an incomplete code since those two symbols are in the "middle"
     *   of the code.  They are eight bits long and the longest literal/length\
     *   code is nine bits.  Therefore the code must be constructed with those
     *   symbols, and the invalid symbols must be detected after decoding.
     *
     * - The fixed distance codes also have two invalid symbols that should result
     *   in an error if received.  Since all of the distance codes are the same
     *   length, this can be implemented as an incomplete code.  Then the invalid
     *   codes are detected while decoding.
     */
    private final func fixed() throws -> Int {
        // build fixed huffman tables if first call
        if virgin {
            // construct lencode and distcode
            lencode = Huffman(
                    count: [Int](repeating: 0, count: MAXBITS + 1),
                    symbol: [Int](repeating: 0, count: FIXLCODES))

            distcode = Huffman(
                    count: [Int](repeating: 0, count: MAXBITS + 1),
                    symbol: [Int](repeating: 0, count: MAXDCODES))

            // literal / length table
            var lengths = [Int](repeating: 8, count: FIXLCODES)
            for symbol in 144..<256 {
                lengths[symbol] = 9
            }
            for symbol in 256..<280 {
                lengths[symbol] = 7
            }
            construct(&lencode!, &lengths, FIXLCODES)

            // distance table
            lengths = [Int](repeating: 5, count: MAXDCODES)
            construct(&distcode!, &lengths, MAXDCODES)
            makeTable(lencode!, fixedLenTable)
            makeTable(distcode!, fixedDistTable)

            // do this just once
            virgin = false
        }
        // decode data until end-of-block code
        return try codes(&lencode!, &distcode!, fixedLenTable, fixedDistTable)
    }

    /*
     * Given the list of code lengths length[0..n-1] representing a canonical
     * Huffman code for n symbols, construct the tables required to decode those
     * codes.  Those tables are the number of codes of each length, and the symbols
     * sorted by length, retaining their original order within each length.  The
     * return value is zero for a complete code set, negative for an over-
     * subscribed code set, and positive for an incomplete code set.  The tables
     * can be used if the return value is zero or positive, but they cannot be used
     * if the return value is negative.  If the return value is zero, it is not
     * possible for decode() using that table to return an error--any stream of
     * enough bits will resolve to a symbol.  If the return value is positive, then
     * it is possible for decode() using that table to return an error for received
     * codes past the end of the incomplete lengths.
     *
     * Not used by decode(), but used for error checking, h->count[0] is the number
     * of the n symbols not in the code.  So n - h->count[0] is the number of
     * codes.  This is useful for checking for incomplete codes that have more than
     * one symbol, which is an error in a dynamic block.
     *
     * Assumption: for all i in 0..n-1, 0 <= length[i] <= MAXBITS
     * This is assured by the construction of the length arrays in dynamic() and
     * fixed() and is not verified by construct().
     *
     * Format notes:
     *
     * - Permitted and expected examples of incomplete codes are one of the fixed
     *   codes and any code with a single symbol which in deflate is coded as one
     *   bit instead of zero bits.  See the format notes for fixed() and dynamic().
     *
     * - Within a given code length, the symbols are kept in ascending order for
     *   the code bits definition.
     */
    @discardableResult
    private final func construct(
        _ huffman: inout Huffman,
        _ length: inout [Int],
        _ n: Int) -> Int {

        // offsets in symbol table for each length
        var offs = [Int](repeating: 0, count: MAXBITS + 1)

        // count number of codes of each length
        var len = 0                             // current length when stepping through huffman.count[]
        while len <= MAXBITS {
            huffman.count[len] = 0
            len += 1
        }

        var symbol = 0                          // current symbol when stepping through length[]
        while symbol < n {
            huffman.count[length[symbol]] += 1  // assumes lengths are within bounds
            symbol += 1
        }

        if huffman.count[0] == n {              // no codes!
            return 0                            // complete, but decode() will fail
        }

        var left: Int = 1                       // number of possible codes left of current length
                                                // one possible code of zero length

        // check for an over-subscribed or incomplete set of lengths
        len = 1
        while len <= MAXBITS {
            left <<= 1                          // one more bit, double codes left
            left -= huffman.count[len]          // deduct count from possible codes
            if left < 0 {
                return left                     // over-subscribed--return negative
            }
            len += 1
        }                                       // left > 0 means incomplete

        // generate offsets into symbol table for each length for sorting
        offs[1] = 0
        len = 1
        while len < MAXBITS {
            offs[len + 1] = offs[len] + huffman.count[len]
            len += 1
        }

        // put symbols in table sorted by length, by symbol order within each length
        symbol = 0
        while symbol < n {
            if length[symbol] != 0 {
                let offset = offs[length[symbol]]
                huffman.symbol[offset] = symbol
                offs[length[symbol]] += 1
            }
            symbol += 1
        }

        // return zero for complete set, positive for incomplete set
        return left
    }

    private final func bits(_ need: Int) throws -> Int {
        // bit accumulator (can use up to 20 bits)
        var buffer = self.bitbuf

        // load at least need bits into value
        while self.bitcnt < need {
            if self.incnt >= input.count {
                throw PuffError.read(error: 1)  // out of input
            }
            // load eight bits
            buffer |= UInt32(input[self.incnt]) << self.bitcnt
            self.incnt += 1
            self.bitcnt += 8
        }

        // drop need bits and update buffer, always zero to seven bits left
        self.bitbuf = buffer >> need
        self.bitcnt -= need

        // return need bits, zeroing the bits above that
        return Int(buffer & ((1 << need) - 1))
    }

    private final func stored() throws -> Int {
        // discard leftover bits from current byte (assumes self.bitcnt < 8)
        self.bitbuf = 0
        self.bitcnt = 0

        // get length and check against its one's complement
        if (self.incnt + 4) > input.count {
            return 2                                // not enough input
        }

        var len = Int(input[self.incnt])            // length of stored block
        self.incnt += 1
        len |= Int(input[self.incnt]) << 8
        self.incnt += 1

        var len2 = Int(input[self.incnt])           // complement of length of stored block
        self.incnt += 1
        len2 |= Int(input[self.incnt]) << 8
        self.incnt += 1

        if len + len2 != 0xFFFF {
            return -2                               // didn't match complement!
        }

        // copy len bytes from in to out
        if (self.incnt + len) > input.count {
            return 2                                // not enough input
        }
        // as many as the output has room for
        let count = min(len, maxLength - outCount)
        reserve(outCount + count)
        (out! + outCount).update(from: input.baseAddress! + self.incnt, count: count)
        outCount += count
        self.incnt += count
        if count < len {
            return try stopAtFullOutput()
        }

        // done with a valid stored block
        return 0
    }

    /*
     * Decode literal/length and distance codes until an end-of-block code.
     *
     * Format notes:
     *
     * - Compressed data that is after the block type if fixed or after the code
     *   description if dynamic is a combination of literals and length/distance
     *   pairs terminated by and end-of-block code.  Literals are simply Huffman
     *   coded bytes.  A length/distance pair is a coded length followed by a
     *   coded distance to represent a string that occurs earlier in the
     *   uncompressed data that occurs again at the current location.
     *
     * - Literals, lengths, and the end-of-block code are combined into a single
     *   code of up to 286 symbols.  They are 256 literals (0..255), 29 length
     *   symbols (257..285), and the end-of-block symbol (256).
     *
     * - There are 256 possible lengths (3..258), and so 29 symbols are not enough
     *   to represent all of those.  Lengths 3..10 and 258 are in fact represented
     *   by just a length symbol.  Lengths 11..257 are represented as a symbol and
     *   some number of extra bits that are added as an integer to the base length
     *   of the length symbol.  The number of extra bits is determined by the base
     *   length symbol.  These are in the static arrays below, lens[] for the base
     *   lengths and lext[] for the corresponding number of extra bits.
     *
     * - The reason that 258 gets its own symbol is that the longest length is used
     *   often in highly redundant files.  Note that 258 can also be coded as the
     *   base value 227 plus the maximum extra value of 31.  While a good deflate
     *   should never do this, it is not an error, and should be decoded properly.
     *
     * - If a length is decoded, including its extra bits if any, then it is
     *   followed a distance code.  There are up to 30 distance symbols.  Again
     *   there are many more possible distances (1..32768), so extra bits are added
     *   to a base value represented by the symbol.  The distances 1..4 get their
     *   own symbol, but the rest require extra bits.  The base distances and
     *   corresponding number of extra bits are below in the static arrays dist[]
     *   and dext[].
     *
     * - Literal bytes are simply written to the output.  A length/distance pair is
     *   an instruction to copy previously uncompressed bytes to the output.  The
     *   copy is from distance bytes back in the output stream, copying for length
     *   bytes.
     *
     * - Distances pointing before the beginning of the output data are not
     *   permitted.
     *
     * - Overlapped copies, where the length is greater than the distance, are
     *   allowed and common.  For example, a distance of one and a length of 258
     *   simply copies the last byte 258 times.  A distance of four and a length of
     *   twelve copies the last four bytes three times.  A simple forward copy
     *   ignoring whether the length is greater than the distance or not implements
     *   this correctly.  You should not use memcpy() since its behavior is not
     *   defined for overlapped arrays.  You should not use memmove() or bcopy()
     *   since though their behavior -is- defined for overlapping arrays, it is
     *   defined to do the wrong thing in this case.
     */
    private final func codes(
        _ lencode: inout Huffman,
        _ distcode: inout Huffman,
        _ lenTable: UnsafeMutablePointer<UInt16>,
        _ distTable: UnsafeMutablePointer<UInt16>) throws -> Int {
        let lens = self.lens, lext = self.lext, dists = self.dists, dext = self.dext
        let mask = UInt64((1 << Puff.FASTBITS) - 1)
        let base = input.baseAddress!
        let end = input.count
        // The bits of the input in a buffer of 64 bits: the whole bytes past
        // the bits that puff.c would have read are given back when decoding
        // stops, so that it stops at the same bit of the input. The output
        // and its length are held here too, and written back when it stops.
        var buffer = UInt64(self.bitbuf)
        var count = self.bitcnt
        var next = self.incnt
        var out = self.out!
        var written = self.outCount

        // decode literals and length/distance pairs
        while true {
            // A prefix that has its bytes is done, and reads no further: the
            // symbol after them may not be in a stream that is cut short.
            if prefix && written >= maxLength {
                giveBack(buffer, count, next, written)
                return 1
            }
            // Room for the longest copy, unless the output is as long as it
            // can be, where every byte is checked against maxLength.
            if outCapacity - written < 258 && outCapacity < maxLength {
                self.outCount = written
                reserve(written + 258)
                out = self.out!
            }
            if next + 8 <= end {
                // Eight bytes at a time, of which the ones that fit are
                // taken; the bits of the rest are the ones that come next.
                let word = UInt64(littleEndian: UnsafeRawPointer(base + next).loadUnaligned(as: UInt64.self))
                buffer |= word << UInt64(count)
                let taken = (63 - count) >> 3
                next += taken
                count += taken << 3
            } else {
                while count <= 56 && next < end {
                    buffer |= UInt64(base[next]) << UInt64(count)
                    next += 1
                    count += 8
                }
            }
            if count < Puff.MAXPAIRBITS {
                // Near the end of the input, a symbol is decoded a bit at a
                // time, and stops where the input does.
                giveBack(buffer, count, next, written)
                let symbol = try slowCode(&lencode, &distcode)
                if symbol != 0 {
                    return symbol == 256 ? 0 : symbol
                }
                buffer = UInt64(self.bitbuf)
                count = self.bitcnt
                next = self.incnt
                written = self.outCount
                continue
            }

            var symbol = Puff.fastDecode(lenTable, &lencode, buffer & mask, buffer, &count)
            buffer >>= UInt64(symbol.bits)
            if symbol.value < 0 {
                giveBack(buffer, count, next, written)
                return symbol.value         // invalid symbol
            }
            if symbol.value < 256 {         // literal: symbol is the byte
                // write out the literal
                if written >= maxLength {
                    giveBack(buffer, count, next, written)
                    return try stopAtFullOutput()
                }
                out[written] = UInt8(truncatingIfNeeded: symbol.value)
                written += 1
            } else if symbol.value > 256 {  // length
                // get and compute length
                let index = symbol.value - 257
                if index >= 29 {
                    giveBack(buffer, count, next, written)
                    return -10              // invalid fixed code
                }
                let len = lens[index] + Int(buffer & ((1 << UInt64(lext[index])) - 1))
                buffer >>= UInt64(lext[index])
                count -= lext[index]

                // get and check distance
                symbol = Puff.fastDecode(distTable, &distcode, buffer & mask, buffer, &count)
                buffer >>= UInt64(symbol.bits)
                if symbol.value < 0 {
                    giveBack(buffer, count, next, written)
                    return symbol.value     // invalid symbol
                }
                let dist = dists[symbol.value] + Int(buffer & ((1 << UInt64(dext[symbol.value])) - 1))
                buffer >>= UInt64(dext[symbol.value])
                count -= dext[symbol.value]

                if dist > written {
                    giveBack(buffer, count, next, written)
                    return -11              // distance too far back
                }

                // copy length bytes from distance bytes back, as many as the
                // output has room for
                let room = min(len, maxLength - written)
                let to = out + written
                let from = to - dist
                if dist >= room {
                    to.update(from: from, count: room)
                } else {
                    for i in 0..<room {
                        to[i] = from[i]
                    }
                }
                written += room
                if room < len {
                    giveBack(buffer, count, next, written)
                    return try stopAtFullOutput()
                }
            } else {                        // end of block symbol
                giveBack(buffer, count, next, written)
                // done with a valid fixed or dynamic block
                return 0
            }
        }
    }

    // Gives back the whole bytes of the buffer past the bits that puff.c
    // would have read, and keeps the rest, of which there are 0 to 7; the
    // output has the bytes written.
    private final func giveBack(_ buffer: UInt64, _ count: Int, _ next: Int, _ written: Int) {
        self.outCount = written
        self.incnt = next - count >> 3
        self.bitcnt = count & 7
        self.bitbuf = UInt32(buffer & ((1 << UInt64(count & 7)) - 1))
    }

    // Decodes the next literal/length symbol, and the distance after a
    // length, a bit at a time, as puff.c decodes them. It returns 0 when the
    // symbol is decoded, 256 at the end of the block, and the return value of
    // codes() when it stops there.
    private final func slowCode(_ lencode: inout Huffman, _ distcode: inout Huffman) throws -> Int {
        var symbol = try decode(&lencode)
        if symbol < 0 {
            return symbol                   // invalid symbol
        }
        if symbol < 256 {                   // literal: symbol is the byte
            // write out the literal
            if outCount >= maxLength {
                return try stopAtFullOutput()
            }
            out![outCount] = UInt8(symbol)
            outCount += 1
        } else if symbol > 256 {            // length
            // get and compute length
            symbol -= 257
            if symbol >= 29 {
                return -10                  // invalid fixed code
            }
            var len = try bits(lext[symbol]) + lens[symbol]

            // get and check distance
            symbol = try decode(&distcode)
            if symbol < 0 {
                return symbol               // invalid symbol
            }
            let dist = try bits(dext[symbol]) + dists[symbol]

            if dist > outCount {
                return -11                  // distance too far back
            }

            // copy length bytes from distance bytes back
            while len > 0 {
                if outCount >= maxLength {
                    return try stopAtFullOutput()
                }
                out![outCount] = out![outCount - dist]
                outCount += 1
                len -= 1
            }
        } else {
            return 256                      // end of block symbol
        }
        return 0
    }

    // Decodes the symbol of the code at the start of the bits, with the table
    // when the code has FASTBITS bits or fewer, and a bit at a time otherwise,
    // as decode() does. It takes the bits of the code from the count, and
    // returns the symbol, or -10 for a code that is not in the table, and the
    // bits of the code.
    @inline(__always)
    private static func fastDecode(
            _ table: UnsafeMutablePointer<UInt16>,
            _ huffman: inout Huffman,
            _ peek: UInt64,
            _ buffer: UInt64,
            _ count: inout Int) -> (value: Int, bits: Int) {
        let entry = Int(table[Int(peek)])
        if entry != 0 {
            count -= entry & 15
            return (entry >> 4, entry & 15)
        }
        var code = 0                        // len bits being decoded
        var first = 0                       // first code of length len
        var index = 0                       // index of first code of length len in symbol table
        var len = 1                         // current number of bits in code
        while len <= 15 {
            code |= Int((buffer >> UInt64(len - 1)) & 1)
            let codes = huffman.count[len]
            if code - codes < first {       // if len, return symbol
                count -= len
                return (huffman.symbol[index + (code - first)], len)
            }
            index += codes                  // else update for next length
            first += codes
            first <<= 1
            code <<= 1
            len += 1
        }
        return (-10, 0)                     // ran out of codes
    }

    // Makes the table of the codes of up to FASTBITS bits of the Huffman
    // code. The bits of a code are read first bit first, so the entries of a
    // code of len bits are at its bits reversed, and at each of the values of
    // the FASTBITS - len bits after them.
    private final func makeTable(_ huffman: Huffman, _ table: UnsafeMutablePointer<UInt16>) {
        let size = 1 << Puff.FASTBITS
        table.initialize(repeating: 0, count: size)
        var first = 0                       // first code of length len
        var index = 0                       // index of first code of length len in symbol table
        for len in 1...Puff.FASTBITS {
            let codes = huffman.count[len]
            for k in 0..<codes {
                var code = first + k
                if code >= 1 << len {
                    return                  // an over-subscribed code, never decoded
                }
                var reversed = 0
                for _ in 0..<len {
                    reversed = reversed << 1 | code & 1
                    code >>= 1
                }
                let entry = UInt16(huffman.symbol[index + k] << 4 | len)
                var i = reversed
                while i < size {
                    table[i] = entry
                    i += 1 << len
                }
            }
            index += codes
            first += codes
            first <<= 1
        }
    }

    private final func decode(_ huffman: inout Huffman) throws -> Int {
        var code = 0                        // len bits being decoded
        var first = 0                       // first code of length len
        var index = 0                       // index of first code of length len in symbol table
        var len = 1                         // current number of bits in code
        while len <= MAXBITS {
            // code |= try bits(1)             // get next bit
            var buffer = self.bitbuf
            if self.bitcnt < 1 {
                if self.incnt >= input.count {
                    throw PuffError.read(error: 1)  // out of input
                }
                buffer |= UInt32(input[self.incnt]) << self.bitcnt
                self.incnt += 1
                self.bitcnt += 8
            }
            self.bitbuf = buffer >> 1
            self.bitcnt -= 1
            code |= Int(buffer & 1)

            let count = huffman.count.withUnsafeBufferPointer { buf -> Int in
                return buf[len]
            }
            if (code - count) < first {     // if len, return symbol
                return huffman.symbol.withUnsafeBufferPointer { buf -> Int in
                    return buf[index + (code - first)]
                }
            }
            index += count                  // else update for next length
            first += count
            first <<= 1
            code <<= 1
            len += 1
        }
        return -10                          // ran out of codes
    }

    /*
     * Process a dynamic codes block.
     *
     * Format notes:
     *
     * - A dynamic block starts with a description of the literal/length and
     *   distance codes for that block.  New dynamic blocks allow the compressor to
     *   rapidly adapt to changing data with new codes optimized for that data.
     *
     * - The codes used by the deflate format are "canonical", which means that
     *   the actual bits of the codes are generated in an unambiguous way simply
     *   from the number of bits in each code.  Therefore the code descriptions
     *   are simply a list of code lengths for each symbol.
     *
     * - The code lengths are stored in order for the symbols, so lengths are
     *   provided for each of the literal/length symbols, and for each of the
     *   distance symbols.
     *
     * - If a symbol is not used in the block, this is represented by a zero as
     *   as the code length.  This does not mean a zero-length code, but rather
     *   that no code should be created for this symbol.  There is no way in the
     *   deflate format to represent a zero-length code.
     *
     * - The maximum number of bits in a code is 15, so the possible lengths for
     *   any code are 1..15.
     *
     * - The fact that a length of zero is not permitted for a code has an
     *   interesting consequence.  Normally if only one symbol is used for a given
     *   code, then in fact that code could be represented with zero bits.  However
     *   in deflate, that code has to be at least one bit.  So for example, if
     *   only a single distance base symbol appears in a block, then it will be
     *   represented by a single code of length one, in particular one 0 bit.  This
     *   is an incomplete code, since if a 1 bit is received, it has no meaning,
     *   and should result in an error.  So incomplete distance codes of one symbol
     *   should be permitted, and the receipt of invalid codes should be handled.
     *
     * - It is also possible to have a single literal/length code, but that code
     *   must be the end-of-block code, since every dynamic block has one.  This
     *   is not the most efficient way to create an empty block (an empty fixed
     *   block is fewer bits), but it is allowed by the format.  So incomplete
     *   literal/length codes of one symbol should also be permitted.
     *
     * - If there are only literal codes and no lengths, then there are no distance
     *   codes.  This is represented by one distance code with zero bits.
     *
     * - The list of up to 286 length/literal lengths and up to 30 distance lengths
     *   are themselves compressed using Huffman codes and run-length encoding.  In
     *   the list of code lengths, a 0 symbol means no code, a 1..15 symbol means
     *   that length, and the symbols 16, 17, and 18 are run-length instructions.
     *   Each of 16, 17, and 18 are followed by extra bits to define the length of
     *   the run.  16 copies the last length 3 to 6 times.  17 represents 3 to 10
     *   zero lengths, and 18 represents 11 to 138 zero lengths.  Unused symbols
     *   are common, hence the special coding for zero lengths.
     *
     * - The symbols for 0..18 are Huffman coded, and so that code must be
     *   described first.  This is simply a sequence of up to 19 three-bit values
     *   representing no code (0) or the code length for that symbol (1..7).
     *
     * - A dynamic block starts with three fixed-size counts from which is computed
     *   the number of literal/length code lengths, the number of distance code
     *   lengths, and the number of code length code lengths (ok, you come up with
     *   a better name!) in the code descriptions.  For the literal/length and
     *   distance codes, lengths after those provided are considered zero, i.e. no
     *   code.  The code length code lengths are received in a permuted order (see
     *   the order[] array below) to make a short code length code length list more
     *   likely.  As it turns out, very short and very long codes are less likely
     *   to be seen in a dynamic code description, hence what may appear initially
     *   to be a peculiar ordering.
     *
     * - Given the number of literal/length code lengths (nlen) and distance code
     *   lengths (ndist), then they are treated as one long list of nlen + ndist
     *   code lengths.  Therefore run-length coding can and often does cross the
     *   boundary between the two sets of lengths.
     *
     * - So to summarize, the code description at the start of a dynamic block is
     *   three counts for the number of code lengths for the literal/length codes,
     *   the distance codes, and the code length codes.  This is followed by the
     *   code length code lengths, three bits each.  This is used to construct the
     *   code length code which is used to read the remainder of the lengths.  Then
     *   the literal/length code lengths and distance lengths are read as a single
     *   set of lengths using the code length codes.  Codes are constructed from
     *   the resulting two sets of lengths, and then finally you can start
     *   decoding actual compressed data in the block.
     *
     * - For reference, a "typical" size for the code description in a dynamic
     *   block is around 80 bytes.
     */
    private final func dynamic() throws -> Int {
        // descriptor code lengths
        var lengths = [Int](repeating: 0, count: MAXCODES)
        // construct lencode and distcode
        var lencode = Huffman(
                count: [Int](repeating: 0, count: MAXBITS + 1),
                symbol: [Int](repeating: 0, count: MAXLCODES))
        var distcode = Huffman(
                count: [Int](repeating: 0, count: MAXBITS + 1),
                symbol: [Int](repeating: 0, count: MAXDCODES))

        // permutation of code length codes
        let order = [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15]

        // get number of lengths in each table, check lengths
        let nlen = try bits(5) + 257                    // number of lengths in descriptor
        let ndist = try bits(5) + 1
        let ncode =  try bits(4) + 4
        if nlen > MAXLCODES || ndist > MAXDCODES {
            return -3                                   // bad counts
        }

        // read code length code lengths (really), missing lengths are zero
        var index = 0                                   // index of lengths[]
        while index < ncode {
            lengths[order[index]] = try bits(3)
            index += 1
        }

        while index < 19 {
            lengths[order[index]] = 0
            index += 1
        }

        // build huffman table for code lengths codes (use lencode temporarily)
        var error = construct(&lencode, &lengths, 19)   // construct() return value
        if error != 0 {                                 // require complete code set here
            return -4
        }

        // read length/literal and distance code length tables
        index = 0
        while index < (nlen + ndist) {
            var symbol = try decode(&lencode)   // decoded value
            if symbol < 0 {
                return symbol                           // invalid symbol
            }

            if symbol < 16 {                            // length in 0..15
                lengths[index] = symbol
                index += 1
            } else {                                    // repeat instruction
                var len = 0                             // last length to repeat. Assume repeating zeros
                if symbol == 16 {                       // repeat last length 3..6 times
                    if index == 0 {
                        return -5                       // no last length!
                    }
                    len = lengths[index - 1]            // last length
                    symbol = try bits(2) + 3
                } else if symbol == 17 {                // repeat zero 3..10 times
                    symbol = try bits(3) + 3
                } else {                                // == 18, repeat zero 11..138 times
                    symbol = try bits(7) + 11
                }

                if index + symbol > nlen + ndist {
                    return -6                           // too many lengths!
                }

                while symbol > 0 {                      // repeat last or zero symbol times
                    lengths[index] = len
                    index += 1
                    symbol -= 1
                }
            }
        }

        // check for end-of-block code -- there better be one!
        if lengths[256] == 0 {
            return -9
        }

        // build huffman table for literal/length codes
        error = construct(&lencode, &lengths, nlen)
        if error != 0 && (error < 0 || nlen != lencode.count[0] + lencode.count[1]) {
            // incomplete code ok only for single length 1 code
            return -7
        }

        // build huffman table for distance codes
        var lengths2 = Array(lengths.suffix(from: nlen))
        error = construct(&distcode, &lengths2, ndist)
        if error != 0 && (error < 0 || ndist != distcode.count[0] + distcode.count[1]) {
            // incomplete code ok only for single length 1 code
            return -8
        }

        // decode data until end-of-block code
        makeTable(lencode, lenTable)
        makeTable(distcode, distTable)
        return try codes(&lencode, &distcode, lenTable, distTable)
    }
}
