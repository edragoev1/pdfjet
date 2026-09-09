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

        // Round to 2 decimal places
        let rounded = (value * 100).rounded() / 100

        var isNegative = rounded < 0
        let absoluteRounded = abs(rounded)

        let integerPart = Int(absoluteRounded)
        let decimalPart = absoluteRounded - Float(integerPart)
        var decimalDigits = Int((decimalPart * 100).rounded())

        // Handle carry-over from rounding
        var adjustedIntegerPart = integerPart
        if decimalDigits >= 100 {
            decimalDigits -= 100
            adjustedIntegerPart += 1
            isNegative = (rounded + 1) < 0
        }

        // Determine if we need decimal places
        var hasDecimal = decimalDigits > 0
        var trailingZeros = 0

        if hasDecimal {
            // Count trailing zeros
            if decimalDigits % 10 == 0 {
                trailingZeros = 1
                if decimalDigits / 10 % 10 == 0 {
                    trailingZeros = 2
                }
            }
            hasDecimal = trailingZeros < 2
        }

        // Calculate lengths
        let intDigits = adjustedIntegerPart == 0 ? 1 : Int(log10(Double(adjustedIntegerPart))) + 1
        let totalLength = (isNegative ? 1 : 0) + intDigits + (hasDecimal ? 1 + (2 - trailingZeros) : 0)

        var result = [UInt8](repeating: 0, count: totalLength)
        var pos = 0

        // Add sign
        if isNegative {
            result[pos] = UInt8(ascii: "-")
            pos += 1
        }

        // Add integer part
        pos = writeInt(adjustedIntegerPart, into: &result, at: pos, digits: intDigits)

        // Add decimal part if needed
        if hasDecimal {
            result[pos] = UInt8(ascii: ".")
            pos += 1
            if trailingZeros < 2 {
                result[pos] = UInt8(ascii: "0") + UInt8(decimalDigits / 10)
                pos += 1
            }
            if trailingZeros < 1 {
                result[pos] = UInt8(ascii: "0") + UInt8(decimalDigits % 10)
                pos += 1
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
