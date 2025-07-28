/**
 *  LZWHashTable.swift
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

class LZWHashTable {
    private let mask = 0x3FFF
    private var offsets: [Int]
    private var lengths: [Int]
    private var codes: [UInt32?]

    init() {
        offsets = [Int](repeating: -1, count: mask + 1)
        lengths = [Int](repeating: -1, count: mask + 1)
        codes = [UInt32?](repeating: nil, count: mask + 1)
    }

    func clear() {
        offsets = [Int](repeating: -1, count: mask + 1)
        lengths = [Int](repeating: -1, count: mask + 1)
        codes = [UInt32?](repeating: nil, count: mask + 1)
    }

    func get(
            _ source: [UInt8],
            _ index1: Int,
            _ index2: Int,
            _ code: UInt32) -> UInt32? {
        let length = (index2 - index1) + 1
        if length == 1 {
            return UInt32(source[index1])
        }

        // FNV-1a inline hash routine
        var hash: UInt32 = 2166136261
        let prime: UInt32 = 16777619
        var i = index1
        while i <= index2 {
            hash ^= UInt32(source[i])
            hash = hash &* prime
            i += 1
        }
        // Perform xor-folding operation
        var index = Int(((hash >> 18) ^ hash) & UInt32(mask))

        while codes[index] != nil {
            if lengths[index] == length {
                let offset = offsets[index]
                var match = true
                i = 0
                while i < length {
                    if source[offset + i] != source[index1 + i] {
                        match = false
                        break
                    }
                    i += 1
                }
                if match {
                    return codes[index]
                }
            }
            index += 1
            if index > mask {
                index = 0
            }
        }
        offsets[index] = index1
        lengths[index] = length
        codes[index] = code
        return nil
    }
}
