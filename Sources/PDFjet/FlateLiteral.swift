/**
 * FlateLiteral.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

internal class FlateLiteral {
    //  Huffman codes for the literal alphabet:
    //  ==========================================
    //  Literal      nBits       Codes
    //  ---------    ----        -----
    //    0 - 143     8          00110000 through
    //                           10111111
    //  144 - 255     9          110010000 through
    //                           111111111

    var codes = [UInt32]()
    var nBits = [UInt8]()

    internal init() {
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
