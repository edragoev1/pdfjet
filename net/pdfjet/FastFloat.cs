using System;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
internal static class FastFloat {
    internal const String NOT_WRITABLE = "A coordinate, size or width is NaN, infinite or too large for a PDF.";

    // A PDF number has no exponent and no NaN or infinity, and readers keep
    // integers in 32 bits, so a number must be finite and below 2^31.
    internal static bool IsWritable(float value) {
        return Math.Abs(value) < 2147483648f;   // False for NaN and the infinities
    }

    internal static byte[] ToByteArray(float value) {
        if (!IsWritable(value)) {
            throw new ArgumentException(NOT_WRITABLE);
        }

        double magnitude = Math.Abs((double) value);
        if (magnitude >= 8388608.0) {
            // A float of 2^23 or more is a whole number: write all its digits
            String digits = magnitude.ToString("F0", CultureInfo.InvariantCulture);
            return Encoding.ASCII.GetBytes((value < 0f ? "-" : "") + digits);
        }

        // Round to 2 decimal places, halves away from zero. A float times 100
        // is exact in a double, so the exact value of the float is rounded.
        double scaled = magnitude * 100.0;
        int hundredths = (int) scaled;
        if (scaled - hundredths >= 0.5) {
            hundredths++;
        }

        // A value that rounds to zero, like -0.001 and -0.0, is written 0
        bool negative = value < 0f && hundredths > 0;
        int integerPart = hundredths / 100;
        int decimalDigits = hundredths % 100;

        // Count the digits, leaving out the trailing zeros of the decimals
        int intDigits = 1;
        for (int i = integerPart; i >= 10; i /= 10) {
            intDigits++;
        }
        int fractionDigits = 0;
        if (decimalDigits > 0) {
            fractionDigits = (decimalDigits % 10 == 0) ? 1 : 2;
        }
        int totalLength = (negative ? 1 : 0) + intDigits + (fractionDigits > 0 ? 1 + fractionDigits : 0);

        byte[] result = new byte[totalLength];
        int pos = 0;

        // Add sign
        if (negative) {
            result[pos++] = (byte)'-';
        }

        // Add integer part
        pos = WriteInt(integerPart, result, pos, intDigits);

        // Add decimal part if needed
        if (fractionDigits > 0) {
            result[pos++] = (byte)'.';
            result[pos++] = (byte)('0' + decimalDigits / 10);
            if (fractionDigits > 1) {
                result[pos++] = (byte)('0' + decimalDigits % 10);
            }
        }

        return result;
    }

    private static int WriteInt(int value, byte[] buffer, int pos, int digits) {
        for (int i = digits - 1; i >= 0; i--) {
            buffer[pos + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return pos + digits;
    }
}   // End of FastFloat.cs
}   // End of namespace PDFjet.NET
