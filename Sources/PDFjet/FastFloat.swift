import Foundation

struct FastFloat {
    static func toByteArray(_ value: Float) -> [UInt8] {
        // Handle special cases
        if value.isNaN {
            return Array("NaN".utf8)
        }
        if value == .infinity {
            return Array("Infinity".utf8)
        }
        if value == -.infinity {
            return Array("-Infinity".utf8)
        }

        let magnitude = abs(Double(value))
        if magnitude >= 8388608.0 {
            // A float of 2^23 or more is a whole number: write all its digits
            let digits = String(format: "%.0f", magnitude)
            return Array(((value < 0 ? "-" : "") + digits).utf8)
        }

        // Round to 2 decimal places, halves away from zero. A float times 100
        // is exact in a Double, so the exact value of the float is rounded.
        let scaled = magnitude * 100.0
        var hundredths = Int(scaled)
        if scaled - Double(hundredths) >= 0.5 {
            hundredths += 1
        }

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
        let totalLength = (isNegative ? 1 : 0) + intDigits + (fractionDigits > 0 ? 1 + fractionDigits : 0)

        var result = [UInt8](repeating: 0, count: totalLength)
        var pos = 0

        // Add sign
        if isNegative {
            result[pos] = UInt8(ascii: "-")
            pos += 1
        }

        // Add integer part
        pos = writeInt(integerPart, into: &result, at: pos, digits: intDigits)

        // Add decimal part if needed
        if fractionDigits > 0 {
            result[pos] = UInt8(ascii: ".")
            pos += 1
            result[pos] = UInt8(ascii: "0") + UInt8(decimalDigits / 10)
            pos += 1
            if fractionDigits > 1 {
                result[pos] = UInt8(ascii: "0") + UInt8(decimalDigits % 10)
            }
        }

        return result
    }

    private static func writeInt(_ value: Int, into buffer: inout [UInt8], at pos: Int, digits: Int) -> Int {
        var value = value
        var position = pos + digits - 1
        for _ in 0..<digits {
            buffer[position] = UInt8(ascii: "0") + UInt8(value % 10)
            value /= 10
            position -= 1
        }
        return pos + digits
    }
}
