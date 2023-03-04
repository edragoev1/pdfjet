/**
 *  FlateUtils.swift
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


public class FlateUtils {

    static func reverse(_ code: UInt32, length: Int) -> UInt32 {
        var temp = code
        var mirror: UInt32 = 0
        for _ in 0..<length {
            mirror |= temp & 1
            mirror <<= 1
            temp >>= 1
        }
        return mirror >> 1
    }

    static func twoPowerOf(_ exponent: Int) -> UInt32 {
        return [1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8128][exponent]
    }

}
