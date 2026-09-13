using System;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
internal static class FastFloat {
    internal static byte[] ToByteArray(float value) {
        // Handle special cases
        if (float.IsNaN(value)) {
            return new byte[] { (byte)'N', (byte)'a', (byte)'N' };
        }
        if (float.IsPositiveInfinity(value)) {
            return new byte[] { (byte)'I', (byte)'n', (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'t', (byte)'y' };
        }
        if (float.IsNegativeInfinity(value)) {
            return new byte[] { (byte)'-', (byte)'I', (byte)'n', (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'t', (byte)'y' };
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
