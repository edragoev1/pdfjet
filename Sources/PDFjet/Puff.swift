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
  Puff.swift is a conversion from the original puff.c by Mark Adler.
  All credit goes to the original author.

  Eugene Dragoev
  edragoev@protonmail.com
 */
import Foundation


struct Huffman {
    var count: [Int]    // number of symbols of each length
    var symbol: [Int]   // canonically ordered symbols
}


enum PuffError: Error {
    case read(error: Int)
}


public final class Puff {

    // Maximums for allocations and loops.
    // It is not useful to change these -- they are fixed by the deflate format.
    let MAXBITS: Int = 15       // maximum bits in a code
    let MAXLCODES: Int = 286    // maximum number of literal/length codes
    let MAXDCODES: Int = 30     // maximum number of distance codes
    var MAXCODES: Int = 316     // maximum codes lengths to read
    let FIXLCODES: Int = 288    // number of fixed literal/length codes

    // input state
    var incnt = 0               // bytes read so far from the input
    var bitbuf: UInt32 = 0      // bit buffer
    var bitcnt = 0              // number of bits in bit buffer

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
    public init(output: inout [UInt8], input: inout [UInt8]) throws {
        var last: Int?                              // block information
        var type: Int?

        var error = 0                               // return value

        // skip the zlib header
        self.incnt += 2

        // process blocks until last block or error
        repeat {
            last = try bits(1, &input)              // one if last block
            type = try bits(2, &input)              // block type 0..3
            if type == 0 {
                error = stored(&output, &input)
            }
            else if type == 1 {
                error = try fixed(&output, &input)
            }
            else if type == 2 {
                error = try dynamic(&output, &input)
            }
            else {
                error = -1                          // type == 3, invalid
            }
            if error != 0 {
                throw PuffError.read(error: error)  // return with error
            }
        } while last != 1
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
    private final func fixed(_ output: inout [UInt8], _ input: inout [UInt8]) throws -> Int {
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

            // do this just once
            virgin = false
        }
        // decode data until end-of-block code
        return try codes(&output, &input, &lencode!, &distcode!)
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


    private final func bits(_ need: Int, _ input: inout [UInt8]) throws -> Int {
        // bit accumulator (can use up to 20 bits)
        var buffer = self.bitbuf

        // load at least need bits into value
        while self.bitcnt < need {
            if self.incnt == input.count {
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


    private final func stored(_ output: inout [UInt8], _ input: inout [UInt8]) -> Int {

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
        while len > 0 {
            output.append(input[self.incnt])
            self.incnt += 1
            len -= 1
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
        _ output: inout [UInt8],
        _ input: inout [UInt8],
        _ lencode: inout Huffman,
        _ distcode: inout Huffman) throws -> Int {

        var symbol: Int         // decoded symbol
        var len: Int            // length for copy
        var dist: Int           // distance for copy

        // decode literals and length/distance pairs
        repeat {
            symbol = try decode(&lencode, &input)
            if symbol < 0 {
                return symbol               // invalid symbol
            }
            if symbol < 256 {               // literal: symbol is the byte
                // write out the literal
                output.append(UInt8(symbol))
            }
            else if symbol > 256 {          // length
                // get and compute length
                symbol -= 257
                if symbol >= 29 {
                    return -10              // invalid fixed code
                }
                len = try bits(lext[symbol], &input) + lens[symbol]

                // get and check distance
                symbol = try decode(&distcode, &input)
                if symbol < 0 {
                    return symbol           // invalid symbol
                }
                dist = try bits(dext[symbol], &input) + dists[symbol]

                if dist > output.count {
                    return -11              // distance too far back
                }

                // copy length bytes from distance bytes back
                while len > 0 {
                    output.append(output[output.count - dist])
                    len -= 1
                }
            }
        } while symbol != 256               // end of block symbol

        // done with a valid fixed or dynamic block
        return 0
    }


    private final func decode(_ huffman: inout Huffman, _ input: inout [UInt8]) throws -> Int {
        var code = 0                        // len bits being decoded
        var first = 0                       // first code of length len
        var index = 0                       // index of first code of length len in symbol table
        var len = 1                         // current number of bits in code
        while len <= MAXBITS {
            // code |= try bits(1, &input)     // get next bit
            var buffer = self.bitbuf
            if self.bitcnt < 1 {
                buffer |= UInt32(input[self.incnt]) << self.bitcnt
                self.incnt += 1
                self.bitcnt += 8
            }
            self.bitbuf = buffer >> 1
            self.bitcnt -= 1
            code |= Int(buffer & 1)

            // let count = huffman.count[len]  // number of codes of length len
            let count = huffman.count.withUnsafeBufferPointer { buf -> Int in
                return buf[len]
            }
            if (code - count) < first {     // if len, return symbol
                // return huffman.symbol[index + (code - first)]
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
    private final func decode(_ huffman: inout Huffman, _ input: inout [UInt8]) throws -> Int {
        var code = 0                            // len bits being decoded
        var first = 0                           // first code of length len
        var index = 0                           // index of first code of length len in symbol table
        var len = 1                             // current number of bits in code
        var left = self.bitcnt                  // bits left in to process

        while true {
            while left > 0 {
                left -= 1
                code |= Int(self.bitbuf) & 1    // get next bit
                self.bitbuf >>= 1
                let count = huffman.count[len]  // number of codes of length len
                if (code - count) < first {     // if len, return symbol
                    self.bitcnt = (self.bitcnt - len) & 7
                    return huffman.symbol[index + (code - first)]
                }
                index += count                  // else update for next length
                first += count
                first <<= 1
                code <<= 1
                len += 1
            }

            left = (MAXBITS + 1) - len
            if left == 0 {
                break
            }

            // if self.incnt == input.count {
            //     continue                        // out of input
            // }

            bitbuf = UInt32(input[self.incnt])
            self.incnt += 1
            if left > 8 {
                left = 8
            }
        }

        return -10                              // ran out of codes
    }
*/

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
     *   Each of 16, 17, and 18 are follwed by extra bits to define the length of
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
    private final func dynamic(_ output: inout [UInt8], _ input: inout [UInt8]) throws -> Int {

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
        let nlen = try bits(5, &input) + 257                    // number of lengths in descriptor
        let ndist = try bits(5, &input) + 1
        let ncode =  try bits(4, &input) + 4
        if nlen > MAXLCODES || ndist > MAXDCODES {
            return -3                                   // bad counts
        }

        // read code length code lengths (really), missing lengths are zero
        var index = 0                                   // index of lengths[]
        while index < ncode {
            lengths[order[index]] = try bits(3, &input)
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
            var symbol = try decode(&lencode, &input)   // decoded value
            if symbol < 0 {
                return symbol                           // invalid symbol
            }

            if symbol < 16 {                            // length in 0..15
                lengths[index] = symbol
                index += 1
            }
            else {                                      // repeat instruction
                var len = 0                             // last length to repeat. Assume repeating zeros
                if symbol == 16 {                       // repeat last length 3..6 times
                    if index == 0 {
                        return -5                       // no last length!
                    }
                    len = lengths[index - 1]            // last length
                    symbol = try bits(2, &input) + 3
                }
                else if symbol == 17 {                  // repeat zero 3..10 times
                    symbol = try bits(3, &input) + 3
                }
                else {                                  // == 18, repeat zero 11..138 times
                    symbol = try bits(7, &input) + 11
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
        return try codes(&output, &input, &lencode, &distcode)
    }

}
