/**
 *  FlateLength.swift
 *
©2025 PDFjet Software

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

internal class FlateLength {
    //      Extra               Extra               Extra
    // Code Bits Length(s) Code Bits Lengths   Code Bits Length(s)
    // ---- ---- ------     ---- ---- -------   ---- ---- -------
    //  257   0     3       267   1   15,16     277   4   67-82
    //  258   0     4       268   1   17,18     278   4   83-98
    //  259   0     5       269   2   19-22     279   4   99-114
    //  260   0     6       270   2   23-26     280   4  115-130
    //  261   0     7       271   2   27-30     281   5  131-162
    //  262   0     8       272   2   31-34     282   5  163-194
    //  263   0     9       273   3   35-42     283   5  195-226
    //  264   0    10       274   3   43-50     284   5  227-257
    //  265   1  11,12      275   3   51-58     285   0    258
    //  266   1  13,14      276   3   59-66

    //  Huffman codes for the length alphabet:
    //  ==========================================
    //   Length Code  nBits       Codes
    //   ---------    ----        -----
    //   257 - 279     7          0000001 through
    //                            0010111
    //   280 - 285     8          11000000 through
    //                            11000101

    static let instance = FlateLength()

    var ebits1 = [
            0, 0, 0, 0,
            0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 4, 4, 4]
    var ebits2 = [4, 5, 5, 5, 5]

    var codes = [UInt32]()
    var nBits = [UInt8]()

    private init() {
        var code: UInt32 = 0b0000001
        for extra in ebits1 {
            let reversed = UInt32(FlateUtils.reverse(code, length: 7))
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt32 = 0
            while i < n {
                codes.append((i << 7) | reversed)
                nBits.append(UInt8(extra + 7))
                i += 1
            }
            code += 1
        }
        code = 0b11000000
        for extra in ebits2 {
            let reversed = UInt32(FlateUtils.reverse(code, length: 8))
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt32 = 0
            while i < n {
                codes.append((i << 8) | reversed)
                nBits.append(UInt8(extra + 8))
                i += 1
            }
            code += 1
        }
        codes.removeLast()
        nBits.removeLast()
        codes.append(UInt32(FlateUtils.reverse(0b11000101, length: 8)))
        nBits.append(8)
    }
}
