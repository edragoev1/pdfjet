/**
 * FlateLiteral.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The Huffman codes for the fixed literal alphabet are constant (defined by
// RFC 1951), so a single shared instance is computed once and reused for
// every FlateEncode call instead of being rebuilt from scratch each time.
internal final class FlateLiteral: @unchecked Sendable {
    //  Huffman codes for the literal alphabet:
    //  ==========================================
    //  Literal      nBits       Codes
    //  ---------    ----        -----
    //    0 - 143     8          00110000 through
    //                           10111111
    //  144 - 255     9          110010000 through
    //                           111111111

    static let shared = FlateLiteral()

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
