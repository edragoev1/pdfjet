/**
 *  LZWEncode.swift
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

public class LZWEncode {
    private var table = LZWHashTable()
    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer = 0
    private var eight = 8

    @discardableResult
    public init(
            _ output: inout [UInt8],
            _ source: [UInt8]) {

        var code1: UInt32 = 0
        var code2: UInt32 = 258
        var length = 9
        writeCode(256, length, &output)                 // Clear Table code

        var i1 = 0
        var i2 = 0
        while i2 < source.count {
            if let code = table.get(source, i1, i2, /* put: */ code2) {
                code1 = code
                i2 += 1
                if i2 < source.count {
                    continue
                }
                writeCode(code1, length, &output)
            } else {
                writeCode(code1, length, &output)
                code2 += 1
                if code2 == 512 {
                    length = 10
                } else if code2 == 1024 {
                    length = 11
                } else if code2 == 2048 {
                    length = 12
                } else if code2 == 4095 {                 // EarlyChange is 1
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
