/**
 * Hexadecimal.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

internal class Hexadecimal {
    var digits = [UInt8]()

    internal init() {
        for row in 0..<16 {
            for col in 0..<16 {
                let offset1 = (row <= 9) ? 48 : 55
                digits.append(UInt8(row + offset1))
                let offset2 = (col <= 9) ? 48 : 55
                digits.append(UInt8(col + offset2))
            }
        }
    }
}
