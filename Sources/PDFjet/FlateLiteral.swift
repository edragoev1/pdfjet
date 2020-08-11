/**
 *  FlateLiteral.swift
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


public class FlateLiteral {

    //  Huffman codes for the literal alphabet:
    //  ==========================================
    //  Literal      nBits       Codes
    //  ---------    ----        -----
    //    0 - 143     8          00110000 through
    //                           10111111
    //  144 - 255     9          110010000 through
    //                           111111111

    static let instance = FlateLiteral()

    var codes = [UInt16]()
    var nBits = [UInt8]()

    private init() {
        var code: UInt16 = 0b00110000
        var i = 0
        while i < 144 {
            codes.append(UInt16(FlateUtils.reverse(UInt32(code), length: 8)))
            nBits.append(UInt8(8))
            code += 1
            i += 1
        }
        code = 0b110010000
        while i < 256 {
            codes.append(UInt16(FlateUtils.reverse(UInt32(code), length: 9)))
            nBits.append(UInt8(9))
            code += 1
            i += 1
        }
    }

}

