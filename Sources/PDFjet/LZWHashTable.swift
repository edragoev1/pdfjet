/**
 *  LZWHashTable.swift
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
            _ source: inout [UInt8],
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
