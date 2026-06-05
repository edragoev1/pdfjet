/**
 * FlateUtils.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

internal class FlateUtils {
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
        if exponent == 0 {
            return 1
        }
        return UInt32(2 << (exponent - 1))
    }
}
