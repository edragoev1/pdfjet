/**
 *  FlateDistance.swift
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
import Foundation


public class FlateDistance {

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

    static let instance = FlateDistance()

    var eBits = [
            0,  0,
            0,  0,  1,  1,  2,  2,  3,  3,  4,  4,  5,  5,
            6,  6,  7,  7,  8,  8,  9,  9, 10, 10, 11, 11]
    var codes = [UInt16]()
    var nBits = [UInt8]()

    private init() {
        var code: UInt32 = 0
        for extra in eBits {
            let reversed = UInt16(FlateUtils.reverse(code, length: 5))
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt16 = 0
            while i < n {
                codes.append((i << 5) | reversed)
                nBits.append(UInt8(extra + 5))
                i += 1
            }
            code += 1
        }
    }

}

