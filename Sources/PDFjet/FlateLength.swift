/**
 *  FlateLength.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


public class FlateLength {
/*
         Extra               Extra               Extra
    Code Bits Length(s) Code Bits Lengths   Code Bits Length(s)
    ---- ---- ------     ---- ---- -------   ---- ---- -------
     257   0     3       267   1   15,16     277   4   67-82
     258   0     4       268   1   17,18     278   4   83-98
     259   0     5       269   2   19-22     279   4   99-114
     260   0     6       270   2   23-26     280   4  115-130
     261   0     7       271   2   27-30     281   5  131-162
     262   0     8       272   2   31-34     282   5  163-194
     263   0     9       273   3   35-42     283   5  195-226
     264   0    10       274   3   43-50     284   5  227-257
     265   1  11,12      275   3   51-58     285   0    258
     266   1  13,14      276   3   59-66
*/

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

    var codes = [UInt16]()
    var nBits = [UInt8]()

    private init() {
        var code: UInt32 = 0b0000001
        for extra in ebits1 {
            let reversed = UInt16(FlateUtils.reverse(code, length: 7))
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt16 = 0
            while i < n {
                codes.append((i << 7) | reversed)
                nBits.append(UInt8(extra + 7))
                i += 1
            }
            code += 1
        }
        code = 0b11000000
        for extra in ebits2 {
            let reversed = UInt16(FlateUtils.reverse(code, length: 8))
            let n = FlateUtils.twoPowerOf(extra)
            var i: UInt16 = 0
            while i < n {
                codes.append((i << 8) | reversed)
                nBits.append(UInt8(extra + 8))
                i += 1
            }
            code += 1
        }
        codes.removeLast()
        nBits.removeLast()
        codes.append(UInt16(FlateUtils.reverse(0b11000101, length: 8)))
        nBits.append(8)
    }

}

