// PuTTY is copyright 1997-2022 Simon Tatham.

// Portions copyright Robert de Bath, Joris van Rantwijk, Delian
// Delchev, Andreas Schultz, Jeroen Massar, Wez Furlong, Nicolas Barry,
// Justin Bradford, Ben Harris, Malcolm Smith, Ahmad Khalifa, Markus
// Kuhn, Colin Watson, Christopher Staite, Lorenz Diener, Christian
// Brabandt, Jeff Smith, Pavel Kryukov, Maxim Kuznetsov, Svyatoslav
// Kuzmich, Nico Williams, Viktor Dukhovni, Josh Dersch, Lars Brinkhoff,
// and CORE SDI S.A.

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation files
// (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT.  IN NO EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

/*
 * Zlib (RFC1950 / RFC1951) compression for PuTTY.
 *
 * There will no doubt be criticism of my decision to reimplement
 * Zlib compression from scratch instead of using the existing zlib
 * code. People will cry `reinventing the wheel'; they'll claim
 * that the `fundamental basis of OSS' is code reuse; they'll want
 * to see a really good reason for me having chosen not to use the
 * existing code.
 *
 * Well, here are my reasons. Firstly, I don't want to link the
 * whole of zlib into the PuTTY binary; PuTTY is justifiably proud
 * of its small size and I think zlib contains a lot of unnecessary
 * baggage for the kind of compression that SSH requires.
 *
 * Secondly, I also don't like the alternative of using zlib.dll.
 * Another thing PuTTY is justifiably proud of is its ease of
 * installation, and the last thing I want to do is to start
 * mandating DLLs. Not only that, but there are two _kinds_ of
 * zlib.dll kicking around, one with C calling conventions on the
 * exported functions and another with WINAPI conventions, and
 * there would be a significant danger of getting the wrong one.
 *
 * Thirdly, there seems to be a difference of opinion on the IETF
 * secsh mailing list about the correct way to round off a
 * compressed packet and start the next. In particular, there's
 * some talk of switching to a mechanism zlib isn't currently
 * capable of supporting (see below for an explanation). Given that
 * sort of uncertainty, I thought it might be better to have code
 * that will support even the zlib-incompatible worst case.
 *
 * Fourthly, it's a _second implementation_. Second implementations
 * are fundamentally a Good Thing in standardisation efforts. The
 * difference of opinion mentioned above has arisen _precisely_
 * because there has been only one zlib implementation and
 * everybody has used it. I don't intend that this should happen
 * again.
 */
import Foundation

/* ----------------------------------------------------------------------
 * Basic LZ77 code. This bit is designed modularly, so it could be
 * ripped out and used in a different LZ77 compressor. Go to it,
 * and good luck :-)
 */

/*
 * This compressor takes a less slapdash approach than the
 * gzip/zlib one. Rather than allowing our hash chains to fall into
 * disuse near the far end, we keep them doubly linked so we can
 * _find_ the far end, and then every time we add a new byte to the
 * window (thus rolling round by one and removing the previous
 * byte), we can carefully remove the hash chain entry.
 */

    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer: UInt8 = 0


/*
 * Modifiable parameters.
 */
let WINSIZE = 32768                 /* window size. Must be power of 2! */
let HASHMAX = 2039                  /* one more than max hash value */
let MAXMATCH = 32                   /* how many matches we track */
let HASHCHARS = 3                   /* how many chars make a hash */
let INVALID = -1                    /* invalid hash _and_ invalid offset */

struct WindowEntry {
    var next: Int?                  /* array indices within the window */
    var prev: Int?
    var hashval: Int?
}

struct HashEntry {
    var first: Int?                 /* window index of first in chain */
}

class Match {
    var distance: Int?
    var len: Int?
}

class Outbuf {
    var outbuf: [UInt8]
    var outbits: UInt
    var noutbits: UInt
    var firstblock: Bool
    init(_ outbuf: inout [UInt8]) {
        self.outbuf = outbuf
        self.outbits = 0
        self.noutbits = 0
        self.firstblock = true
    }
}

public class ZLib {

static func lz77_hash(_ data: [UInt8], _ index: Int) -> Int {
    var hash = 257 * Int(data[index])
    hash += 263 * Int(data[index + 1])
    hash += 269 * Int(data[index + 2])
    return hash % HASHMAX
}

/*
 * Supply data to be compressed. Will update the private fields of
 * the LZ77Context, and will call literal() and match() to output.
 * If `compress' is false, it will never emit a match, but will
 * instead call literal() for everything.
 */
static func lz77_compress(_ out: inout [UInt8], _ data: [UInt8]) {
    var hashtable = [Int](repeating: -1, count: WINSIZE)
    var i = 0
    while i < (data.count - 3) {
        let hash = lz77_hash(data, i)
        let j = hashtable[hash]
        if j != INVALID &&
                data[j] == data[i] &&
                data[j + 1] == data[i + 1] &&
                data[j + 2] == data[i + 2] {
            if i - j >= WINSIZE {
                zlib_literal(&out, data[i])
                hashtable[hash] = i
                i += 1
            } else {
                var length = 0
                while (i + length) < data.count {
                    if data[j + length] == data[i + length] {
                        length += 1
                        if length == 258 {
                            break
                        }
                    } else {
                        break
                    }
                }
                // print("\(i - j) = \(length)")
                zlib_match(&out, (i - j), length)
                hashtable[hash] = i
                i += length
            }
        } else {
            // print("lit")
            zlib_literal(&out, data[i])
            hashtable[hash] = i
            i += 1
        }
    }
    // Process the remaining data
    while i < data.count {
        // print("lit")
        zlib_literal(&out, data[i])
        i += 1
    }
}

// /* ----------------------------------------------------------------------
//  * Zlib compression. We always use the static Huffman tree option.
//  * Mostly this is because it's hard to scan a block in advance to
//  * work out better trees; dynamic trees are great when you're
//  * compressing a large file under no significant time constraint,
//  * but when you're compressing little bits in real time, things get
//  * hairier.
//  *
//  * I suppose it's possible that I could compute Huffman trees based
//  * on the frequencies in the _previous_ block, as a sort of
//  * heuristic, but I'm not confident that the gain would balance out
//  * having to transmit the trees.
//  */

static private func outbits(
        _ output: inout [UInt8],
        _ code: UInt16,
        _ nBits: UInt8) {
    bitBuffer |= UInt32(code) << bitsInBuffer
    bitsInBuffer += nBits
    while bitsInBuffer >= 8 {
        output.append(UInt8(bitBuffer & 0xFF))
        bitBuffer >>= 8
        bitsInBuffer -= 8
    }
}

static let mirrorbytes: [UInt8] = [
    0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0,
    0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
    0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8,
    0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
    0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4,
    0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
    0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec,
    0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
    0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
    0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,
    0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea,
    0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
    0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6,
    0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
    0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee,
    0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
    0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1,
    0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
    0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9,
    0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,
    0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5,
    0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
    0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed,
    0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
    0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3,
    0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
    0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
    0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
    0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7,
    0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,
    0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef,
    0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff,
]

struct coderecord {
    let code: Int16
    let extrabits: Int16
    let min: Int
    let max: Int
}

static let lencodes: [coderecord] = [
    coderecord(code: 257, extrabits: 0, min: 3, max: 3),
    coderecord(code: 258, extrabits: 0, min: 4, max: 4),
    coderecord(code: 259, extrabits: 0, min: 5, max: 5),
    coderecord(code: 260, extrabits: 0, min: 6, max: 6),
    coderecord(code: 261, extrabits: 0, min: 7, max: 7),
    coderecord(code: 262, extrabits: 0, min: 8, max: 8),
    coderecord(code: 263, extrabits: 0, min: 9, max: 9),
    coderecord(code: 264, extrabits: 0, min: 10, max: 10),
    coderecord(code: 265, extrabits: 1, min: 11, max: 12),
    coderecord(code: 266, extrabits: 1, min: 13, max: 14),
    coderecord(code: 267, extrabits: 1, min: 15, max: 16),
    coderecord(code: 268, extrabits: 1, min: 17, max: 18),
    coderecord(code: 269, extrabits: 2, min: 19, max: 22),
    coderecord(code: 270, extrabits: 2, min: 23, max: 26),
    coderecord(code: 271, extrabits: 2, min: 27, max: 30),
    coderecord(code: 272, extrabits: 2, min: 31, max: 34),
    coderecord(code: 273, extrabits: 3, min: 35, max: 42),
    coderecord(code: 274, extrabits: 3, min: 43, max: 50),
    coderecord(code: 275, extrabits: 3, min: 51, max: 58),
    coderecord(code: 276, extrabits: 3, min: 59, max: 66),
    coderecord(code: 277, extrabits: 4, min: 67, max: 82),
    coderecord(code: 278, extrabits: 4, min: 83, max: 98),
    coderecord(code: 279, extrabits: 4, min: 99, max: 114),
    coderecord(code: 280, extrabits: 4, min: 115, max: 130),
    coderecord(code: 281, extrabits: 5, min: 131, max: 162),
    coderecord(code: 282, extrabits: 5, min: 163, max: 194),
    coderecord(code: 283, extrabits: 5, min: 195, max: 226),
    coderecord(code: 284, extrabits: 5, min: 227, max: 257),
    coderecord(code: 285, extrabits: 0, min: 258, max: 258),
]

static let distcodes: [coderecord] = [
    coderecord(code: 0, extrabits: 0, min: 1, max: 1),
    coderecord(code: 1, extrabits: 0, min: 2, max: 2),
    coderecord(code: 2, extrabits: 0, min: 3, max: 3),
    coderecord(code: 3, extrabits: 0, min: 4, max: 4),
    coderecord(code: 4, extrabits: 1, min: 5, max: 6),
    coderecord(code: 5, extrabits: 1, min: 7, max: 8),
    coderecord(code: 6, extrabits: 2, min: 9, max: 12),
    coderecord(code: 7, extrabits: 2, min: 13, max: 16),
    coderecord(code: 8, extrabits: 3, min: 17, max: 24),
    coderecord(code: 9, extrabits: 3, min: 25, max: 32),
    coderecord(code: 10, extrabits: 4, min: 33, max: 48),
    coderecord(code: 11, extrabits: 4, min: 49, max: 64),
    coderecord(code: 12, extrabits: 5, min: 65, max: 96),
    coderecord(code: 13, extrabits: 5, min: 97, max: 128),
    coderecord(code: 14, extrabits: 6, min: 129, max: 192),
    coderecord(code: 15, extrabits: 6, min: 193, max: 256),
    coderecord(code: 16, extrabits: 7, min: 257, max: 384),
    coderecord(code: 17, extrabits: 7, min: 385, max: 512),
    coderecord(code: 18, extrabits: 8, min: 513, max: 768),
    coderecord(code: 19, extrabits: 8, min: 769, max: 1024),
    coderecord(code: 20, extrabits: 9, min: 1025, max: 1536),
    coderecord(code: 21, extrabits: 9, min: 1537, max: 2048),
    coderecord(code: 22, extrabits: 10, min: 2049, max: 3072),
    coderecord(code: 23, extrabits: 10, min: 3073, max: 4096),
    coderecord(code: 24, extrabits: 11, min: 4097, max: 6144),
    coderecord(code: 25, extrabits: 11, min: 6145, max: 8192),
    coderecord(code: 26, extrabits: 12, min: 8193, max: 12288),
    coderecord(code: 27, extrabits: 12, min: 12289, max: 16384),
    coderecord(code: 28, extrabits: 13, min: 16385, max: 24576),
    coderecord(code: 29, extrabits: 13, min: 24577, max: 32768),
]

static func zlib_literal(_ out: inout [UInt8], _ c: UInt8) {
    if c <= 143 {
        /* 0 through 143 are 8 bits long starting at 00110000. */
        outbits(&out, UInt16(mirrorbytes[0x30 + Int(c)]), 8)
    } else {
        /* 144 through 255 are 9 bits long starting at 110010000. */
        outbits(&out, 1 + 2 * UInt16(mirrorbytes[0x90 - 144 + Int(c)]), 9)
    }
}

static func zlib_match(_ out: inout [UInt8], _ distance: Int, _ length: Int) {
    var d: coderecord?
    var l: coderecord?

    /*
     * We can transmit matches of lengths 3 through 258
     * inclusive. So if len exceeds 258, we must transmit in
     * several steps, with 258 or less in each step.
     *
     * Specifically: if len >= 261, we can transmit 258 and be
     * sure of having at least 3 left for the next step. And if
     * len <= 258, we can just transmit len. But if len == 259
     * or 260, we must transmit len-3.
     */

    /*
     * Binary-search to find which length code we're
     * transmitting.
     */
    var i = -1
    var j = lencodes.count
    while true {
        assert(j - i >= 2)
        let k = (j + i) / 2
        if length < lencodes[k].min {
            j = k
        } else if length > lencodes[k].max {
            i = k
        } else {
            l = lencodes[k]
            break                   /* found it! */
        }
    }

    /*
     * Transmit the length code. 256-279 are seven bits
     * starting at 0000000; 280-287 are eight bits starting at
     * 11000000.
     */
    outbits(&out, UInt16(mirrorbytes[Int(l!.code - 256) * 2]), 7)

    /*
     * Transmit the extra bits.
     */
    if l!.extrabits > 0 {
        outbits(&out, UInt16(length) - UInt16(l!.min), UInt8(l!.extrabits))
    }

    /*
     * Binary-search to find which distance code we're
     * transmitting.
     */
    i = -1
    j = distcodes.count
    while true {
        assert(j - i >= 2)
        let k = (j + i) / 2
        if distance < distcodes[k].min {
            j = k
        } else if distance > distcodes[k].max {
            i = k
        } else {
            d = distcodes[k]
            break                   /* found it! */
        }
    }

    /*
     * Transmit the distance code. Five bits starting at 00000.
     */
    outbits(&out, UInt16(mirrorbytes[Int(d!.code) * 8]), 5)

    /*
     * Transmit the extra bits.
     */
    if d!.extrabits > 0 {
        outbits(&out, UInt16(distance) - UInt16(d!.min), UInt8(d!.extrabits))
    }
}

static func zlib_compress_block(_ out: inout [UInt8], _ inblock: [UInt8]) {
    /*
     * If this is the first block, output the Zlib (RFC1950) header
     * bytes 78 9C. (Deflate compression, 32K window size, default
     * algorithm.)
     */
    outbits(&out, 0x9C78, 16)

    /*
     * Start a Deflate (RFC1951) fixed-trees block. We
     * transmit a zero bit (BFINAL=0), followed by a zero
     * bit and a one bit (BTYPE=01). Of course these are in
     * the wrong order (01 0).
     */
    outbits(&out, 2, 3)

    /*
     * Do the compression.
     */
    lz77_compress(&out, inblock)

    /*
     * End the block (by transmitting code 256, which is
     * 0000000 in fixed-tree mode), and transmit some empty
     * blocks to ensure we have emitted the byte containing the
     * last piece of genuine data. There are three ways we can
     * do this:
     *
     *  - Minimal flush. Output end-of-block and then open a
     *    new static block. This takes 9 bits, which is
     *    guaranteed to flush out the last genuine code in the
     *    closed block; but allegedly zlib can't handle it.
     *
     *  - Zlib partial flush. Output EOB, open and close an
     *    empty static block, and _then_ open the new block.
     *    This is the best zlib can handle.
     *
     *  - Zlib sync flush. Output EOB, then an empty
     *    _uncompressed_ block (000, then sync to byte
     *    boundary, then send bytes 00 00 FF FF). Then open the
     *    new block.
     *
     * For the moment, we will use Zlib partial flush.
     */
    outbits(&out, 0, 7)         /* close block */
    // outbits(&out, 2, 3 + 7)     /* empty static block */
    // outbits(&out, 2, 3)         /* open new block */
}

static private func addAdler32(
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

public static func compress(_ buf2: inout [UInt8], _ buf1: [UInt8]) {
    zlib_compress_block(&buf2, buf1)
    print(buf2.count)
    addAdler32(&buf2, buf1)
}

}   // End of ZLib.swift
