/**
 * FastFloat.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

struct FastFloat {
    static let NOT_WRITABLE = "A coordinate, size or width is NaN, infinite or too large for a PDF."

    // The most bytes a writable number takes: -2147483520, or -8388607.99.
    static let maxLength = 11

    // A PDF number has no exponent and no NaN or infinity, and readers keep
    // integers in 32 bits, so a number must be finite and below 2^31.
    static func isWritable(_ value: Float) -> Bool {
        return abs(value) < 2147483648.0    // False for NaN and the infinities
    }

    static func toByteArray(_ value: Float) -> [UInt8] {
        var result = [UInt8]()
        result.reserveCapacity(maxLength)
        append(value, to: &result)
        return result
    }

    // Appends the text of the number to the buffer, as toByteArray writes it.
    static func append(_ value: Float, to buffer: inout [UInt8]) {
        // The callers check isWritable and record the misuse; a number that is
        // not writable still gives valid syntax.
        if !isWritable(value) {
            buffer.append(UInt8(ascii: "0"))
            return
        }

        let magnitude = abs(Double(value))
        if magnitude >= 8388608.0 {
            // A float of 2^23 or more is a whole number: write all its digits
            let digits = String(format: "%.0f", magnitude)
            buffer.append(contentsOf: ((value < 0 ? "-" : "") + digits).utf8)
            return
        }

        let hundredths = roundedHundredths(magnitude)

        // A value that rounds to zero, like -0.001 and -0.0, is written 0
        let isNegative = value < 0 && hundredths > 0
        let integerPart = hundredths / 100
        let decimalDigits = hundredths % 100

        // Count the digits, leaving out the trailing zeros of the decimals
        var intDigits = 1
        var i = integerPart
        while i >= 10 {
            intDigits += 1
            i /= 10
        }
        var fractionDigits = 0
        if decimalDigits > 0 {
            fractionDigits = (decimalDigits % 10 == 0) ? 1 : 2
        }

        // Add sign
        if isNegative {
            buffer.append(UInt8(ascii: "-"))
        }

        // Add integer part
        let start = buffer.count
        buffer.append(contentsOf: repeatElement(UInt8(ascii: "0"), count: intDigits))
        writeInt(integerPart, into: &buffer, at: start, digits: intDigits)

        // Add decimal part if needed
        if fractionDigits > 0 {
            buffer.append(UInt8(ascii: "."))
            buffer.append(UInt8(ascii: "0") + UInt8(decimalDigits / 10))
            if fractionDigits > 1 {
                buffer.append(UInt8(ascii: "0") + UInt8(decimalDigits % 10))
            }
        }
    }

    // Returns the number of hundredths that append writes for a magnitude below
    // 2^23: rounded to 2 decimal places, halves away from zero. A float times
    // 100 is exact in a Double, so the exact value of the float is rounded.
    private static func roundedHundredths(_ magnitude: Double) -> Int {
        let scaled = magnitude * 100.0
        var hundredths = Int(scaled)
        if scaled - Double(hundredths) >= 0.5 {
            hundredths += 1
        }
        return hundredths
    }

    // Returns the value that append writes, in hundredths, with its sign, for
    // a value whose magnitude is below 2^23.
    static func toHundredths(_ value: Float) -> Int {
        let hundredths = roundedHundredths(abs(Double(value)))
        return (value < 0) ? -hundredths : hundredths
    }

    private static func writeInt(_ value: Int, into buffer: inout [UInt8], at pos: Int, digits: Int) {
        var value = value
        var position = pos + digits - 1
        for _ in 0..<digits {
            buffer[position] = UInt8(ascii: "0") + UInt8(value % 10)
            value /= 10
            position -= 1
        }
    }
}
