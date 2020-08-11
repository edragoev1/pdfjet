/**
 *  LZWEncode.swift
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


public class LZWEncode {

    private var table = LZWHashTable()
    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer = 0
    private var eight = 8

    @discardableResult
    public init(
            _ output: inout [UInt8],
            _ source: inout [UInt8]) {

        var code1: UInt32 = 0
        var code2: UInt32 = 258
        var length = 9
        writeCode(256, length, &output)                 // Clear Table code

        var i1 = 0
        var i2 = 0
        while i2 < source.count {
            if let code = table.get(&source, i1, i2, /* put: */ code2) {
                code1 = code
                i2 += 1
                if i2 < source.count {
                    continue
                }
                writeCode(code1, length, &output)
            }
            else {
                writeCode(code1, length, &output)
                code2 += 1
                if code2 == 512 {
                    length = 10
                }
                else if code2 == 1024 {
                    length = 11
                }
                else if code2 == 2048 {
                    length = 12
                }
                else if code2 == 4095 {                 // EarlyChange is 1
                    writeCode(256, length, &output)     // Clear Table code
                    code2 = 258
                    length = 9
                    table.clear()
                }
                i1 = i2
            }
        }
        writeCode(257, length, &output)                 // EOD
        if bitsInBuffer > 0 {
            output.append(UInt8((bitBuffer &<< (eight - bitsInBuffer)) & 0xFF))
        }
    }

    private func writeCode(
            _ code: UInt32,
            _ length: Int,
            _ output: inout [UInt8]) {
        bitBuffer = bitBuffer &<< length
        bitBuffer |= code
        bitsInBuffer += length
        while bitsInBuffer >= 8 {
            output.append(UInt8((bitBuffer >> (bitsInBuffer - eight)) & 0xFF))
            bitsInBuffer -= 8
        }
    }

}
