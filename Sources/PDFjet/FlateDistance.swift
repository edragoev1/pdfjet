/**
 * FlateDistance.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The Huffman codes for the fixed distance alphabet are constant (defined by
// RFC 1951), so a single shared instance is computed once and reused for
// every FlateEncode call instead of being rebuilt from scratch each time.
// This table alone has 32768 entries (one per possible match distance), so
// rebuilding it per call was by far the most expensive part of FlateEncode.
final internal class FlateDistance: @unchecked Sendable {
    //     Extra            Extra                Extra
    // Code Bits  Dist  Code Bits    Dist    Code Bits  Distance
    // ---- ----  ----  ---- ----  ------    ---- ----  --------
    //   0   0       1   10   4     33-48    20    9   1025-1536
    //   1   0       2   11   4     49-64    21    9   1537-2048
    //   2   0       3   12   5     65-96    22   10   2049-3072
    //   3   0       4   13   5    97-128    23   10   3073-4096
    //   4   1     5,6   14   6   129-192    24   11   4097-6144
    //   5   1     7,8   15   6   193-256    25   11   6145-8192
    //   6   2    9-12   16   7   257-384    26   12  8193-12288
    //   7   2   13-16   17   7   385-512    27   12 12289-16384
    //   8   3   17-24   18   8   513-768    28   13 16385-24576
    //   9   3   25-32   19   8  769-1024    29   13 24577-32768

    // Distance codes 0-29 are represented by (fixed-length) 5-bit
    // codes, with possible additional bits as shown in the table
    // above.

    let eBits = [
            0,  0,  0,  0,  1,  1,  2,  2,
            3,  3,  4,  4,  5,  5,  6,  6,
            7,  7,  8,  8,  9,  9, 10, 10,
            11,11, 12, 12, 13, 13]
    static let shared = FlateDistance()

    var codes = [UInt32]()
    var nBits = [UInt8]()

    private init() {
        var code = 0
        while code <= 29 {
            let reversed = FlateUtils.reverse(UInt32(code), length: 5)
            let extra = eBits[code]
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt32 = 0
            while i < n {
                codes.append((i << 5) | reversed)
                nBits.append(UInt8(5 + extra))
                i += 1
            }
            code += 1
        }
    }
}
