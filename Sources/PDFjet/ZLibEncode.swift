/**
 *  ZLibEncode.swift
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

public class ZLibEncode {
    private var bitBuffer: UInt32 = 0
    private var bitsInBuffer: UInt8 = 0
    private let BUFSIZE = 32768
    private let HASHMAX = 2039
    private let FREE = -1

    @discardableResult
    public init(_ buf2: inout [UInt8], _ buf1: [UInt8]) {
        // buf2.reserveCapacity(buf1.count / 2)

        var hashtable = [Int](repeating: FREE, count: BUFSIZE)
        writeCode(&buf2, UInt16(0x9C78), 16)        // FLG | CMF
        writeCode(&buf2, UInt16(0x03), 3)           // BTYPE | BFINAL
print("hello!")
        var i = 0
        while i < (buf1.count - 3) {
            let hash = lz77_hash(buf1, i)
print(hash)
            let index = hashtable[hash]
print(index)
            if index != FREE &&
                    buf1[index] == buf1[i] &&
                    buf1[index + 1] == buf1[i + 1] &&
                    buf1[index + 2] == buf1[i + 2] {
                if i - index >= BUFSIZE {
                    writeCode(
                            &buf2,
                            FlateLiteral.instance.codes[Int(buf1[i])],
                            FlateLiteral.instance.nBits[Int(buf1[i])])
                    hashtable[hash] = i
                    i += 1
                } else {
                    var length = 0
                    while (i + length) < buf1.count {
                        if buf1[index + length] == buf1[i + length] {
                            length += 1
                            if length == 258 {
                                break
                            }
                        } else {
                            break
                        }
                    }
                    let distance = i - index
                    print(length)
                    print(distance)
                    writeCode(
                            &buf2,
                            FlateLength.instance.codes[length],
                            FlateLength.instance.nBits[length])
                    writeCode(
                            &buf2,
                            FlateDistance.instance.codes[distance],
                            FlateDistance.instance.nBits[distance])
                    hashtable[hash] = i
                    i += length
                }
            } else {
                print("lit")
                writeCode(
                        &buf2,
                        FlateLiteral.instance.codes[Int(buf1[i])],
                        FlateLiteral.instance.nBits[Int(buf1[i])])
                hashtable[hash] = i
                i += 1
            }
        }
        writeCode(&buf2, UInt16(0), 7)              // END-OF-BLOCK
        if bitsInBuffer > 0 {
            buf2.append(UInt8(bitBuffer))
        }

        addAdler32(&buf2, buf1)
    }

    func lz77_hash(_ data: [UInt8], _ index: Int) -> Int {
        var hash = 257 * Int(data[index])
        hash += 263 * Int(data[index + 1])
        hash += 269 * Int(data[index + 2])
        return hash % HASHMAX
    }

    func writeCode(
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

    func addAdler32(
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
}
