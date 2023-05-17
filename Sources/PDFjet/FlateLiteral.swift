/**
 *  FlateLiteral.swift
 *
Copyright 2023 Innovatics Inc.

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

internal class FlateLiteral {
    //  Huffman codes for the literal alphabet:
    //  ==========================================
    //  Literal      nBits       Codes
    //  ---------    ----        -----
    //    0 - 143     8          00110000 through
    //                           10111111
    //  144 - 255     9          110010000 through
    //                           111111111
    static let instance = FlateLiteral()

    var codes = [UInt32]()
    var nBits = [UInt8]()

    private init() {
        var code: UInt32 = 0b00110000
        var i = 0
        while i < 144 {
            codes.append(UInt32(FlateUtils.reverse(UInt32(code), length: 8)))
            nBits.append(UInt8(8))
            code += 1
            i += 1
        }
        code = 0b110010000
        while i < 256 {
            codes.append(UInt32(FlateUtils.reverse(UInt32(code), length: 9)))
            nBits.append(UInt8(9))
            code += 1
            i += 1
        }
    }
}
