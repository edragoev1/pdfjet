/**
 *  FlateEncode.swift
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


public class FlateEncode {

    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer: UInt8 = 0

    private let mask = 0x1FFF
    private var indexes: [Int?]

    init(_ output: inout [UInt8], _ input: inout [UInt8], RLE: Bool) {

        output.reserveCapacity(input.count / 2)

        indexes = [Int?](repeating: nil, count: mask + 1)

        writeCode(&output, UInt16(0x9C78), 16)          // FLG | CMF
        writeCode(&output, UInt16(0x03), 3)             // BTYPE | BFINAL

        var i = 0
        while i < (input.count - 3) {
            if var index = getMatchIndex(&input, i, &indexes, RLE: RLE) {
                let distance = i - index
                var length = 3
                index += 3
                i += 3
                while i < input.count {
                    if input[index] != input[i] || length == 258 {
                        break
                    }
                    length += 1
                    index += 1
                    i += 1
                }
                writeCode(&output,
                        FlateLength.instance.codes[length - 3],
                        FlateLength.instance.nBits[length - 3])
                writeCode(&output,
                        FlateDistance.instance.codes[distance - 1],
                        FlateDistance.instance.nBits[distance - 1])
            }
            else {
                writeCode(&output,
                        FlateLiteral.instance.codes[Int(input[i])],
                        FlateLiteral.instance.nBits[Int(input[i])])
                i += 1
            }
        }
        while i < input.count {
            writeCode(&output,
                    FlateLiteral.instance.codes[Int(input[i])],
                    FlateLiteral.instance.nBits[Int(input[i])])
            i += 1
        }
        writeCode(&output, UInt16(0), 7)                // END-OF-BLOCK
        if bitsInBuffer > 0 {
            output.append(UInt8(bitBuffer))
        }

        addAdler32(&output, &input)
    }

    private func getMatchIndex(
            _ input: inout [UInt8],
            _ index: Int,
            _ indexes: inout [Int?],
            RLE: Bool) -> Int? {

        if RLE {
            if index >= 3 {
                if input[index - 3] == input[index] &&
                        input[index - 2] == input[index + 1] &&
                        input[index - 1] == input[index + 2] {
                    return index - 3
                }
                if input[index - 1] == input[index] &&
                        input[index] == input[index + 1] &&
                        input[index + 1] == input[index + 2] {
                    return index - 1
                }
            }
            return nil
        }

        // FNV-1a inline hash routine
        var hash: UInt32 = 2166136261
        let prime: UInt32 = 16777619

        hash ^= UInt32(input[index])
        hash = hash &* prime

        hash ^= UInt32(input[index + 1])
        hash = hash &* prime

        hash ^= UInt32(input[index + 2])
        hash = hash &* prime

        // Perform xor-folding operation
        var i = Int((hash >> 19) ^ hash) & mask

        while indexes[i] != nil &&
                index - indexes[i]! <= 4096 {
            let j = indexes[i]!
            if input[j] == input[index] &&
                    input[j + 1] == input[index + 1] &&
                    input[j + 2] == input[index + 2] {
                return j
            }
            i += 1
            if i > mask {
                i = 0
            }
        }
        indexes[i] = index

        return nil
    }

    private func writeCode(
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

    private func addAdler32(
            _ output: inout [UInt8], _ input: inout [UInt8]) {
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

}
