/*
 * FastFloat.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Text;

namespace PDFjet.NET {
internal static class FastFloat {
    internal const String NOT_WRITABLE = "A coordinate, size or width is NaN, infinite or too large for a PDF.";

    // The most bytes a writable number takes: -2147483520, or -8388607.99.
    internal const int MAX_LENGTH = 11;

    // A PDF number has no exponent and no NaN or infinity, and readers keep
    // integers in 32 bits, so a number must be finite and below 2^31.
    internal static bool IsWritable(float value) {
        return Math.Abs(value) < 2147483648f;   // False for NaN and the infinities
    }

    internal static byte[] ToByteArray(float value) {
        Span<byte> bytes = stackalloc byte[MAX_LENGTH];
        return bytes.Slice(0, Write(value, bytes)).ToArray();
    }

    // Writes the number at the start of the buffer, which must have MAX_LENGTH
    // bytes, and returns the number of bytes written.
    internal static int Write(float value, Span<byte> buffer) {
        if (!IsWritable(value)) {
            throw new ArgumentException(NOT_WRITABLE);
        }

        double magnitude = Math.Abs((double) value);
        if (magnitude >= 8388608.0) {
            // A float of 2^23 or more is a whole number: write all its digits
            String digits = magnitude.ToString("F0", CultureInfo.InvariantCulture);
            return Encoding.ASCII.GetBytes((value < 0f ? "-" : "") + digits, buffer);
        }

        int hundredths = RoundedHundredths(magnitude);

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

        int pos = 0;

        // Add sign
        if (negative) {
            buffer[pos++] = (byte)'-';
        }

        // Add integer part
        pos = WriteInt(integerPart, buffer, pos, intDigits);

        // Add decimal part if needed
        if (fractionDigits > 0) {
            buffer[pos++] = (byte)'.';
            buffer[pos++] = (byte)('0' + decimalDigits / 10);
            if (fractionDigits > 1) {
                buffer[pos++] = (byte)('0' + decimalDigits % 10);
            }
        }

        return pos;
    }

    // Returns the number of hundredths that Write writes for a magnitude below
    // 2^23: rounded to 2 decimal places, halves away from zero. A float times
    // 100 is exact in a double, so the exact value of the float is rounded.
    private static int RoundedHundredths(double magnitude) {
        double scaled = magnitude * 100.0;
        int hundredths = (int) scaled;
        if (scaled - hundredths >= 0.5) {
            hundredths++;
        }
        return hundredths;
    }

    // Returns the value that Write writes, in hundredths, with its sign, for
    // a value whose magnitude is below 2^23.
    internal static int ToHundredths(float value) {
        int hundredths = RoundedHundredths(Math.Abs((double) value));
        return (value < 0f) ? -hundredths : hundredths;
    }

    // Returns the text of the float rounded to 5 decimal places, halves away
    // from zero, leaving out the trailing zeros, for the sine and the cosine of
    // a rotation matrix: rounded to hundredths, a turn of 1 degree is written
    // as one of 1.15. A value that is not writable is written as 0.
    internal static byte[] ToPreciseByteArray(float value) {
        if (!IsWritable(value)) {
            return new byte[] {(byte) '0'};
        }
        // A float times 100000 is exact in a double, and below 2^31 it fits a long.
        double scaled = Math.Abs((double) value) * 100000.0;
        long units = (long) scaled;
        if (scaled - units >= 0.5) {
            units++;
        }
        StringBuilder text = new StringBuilder();
        if (value < 0f && units > 0) {
            text.Append('-');
        }
        text.Append((units / 100000).ToString(CultureInfo.InvariantCulture));
        long fraction = units % 100000;
        if (fraction != 0) {
            int digits = 5;
            while (fraction % 10 == 0) {
                fraction /= 10;
                digits--;
            }
            // The digits of the fraction, with the zeros that lead them
            text.Append('.');
            text.Append(fraction.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0'));
        }
        return Encoding.ASCII.GetBytes(text.ToString());
    }

    private static int WriteInt(int value, Span<byte> buffer, int pos, int digits) {
        for (int i = digits - 1; i >= 0; i--) {
            buffer[pos + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return pos + digits;
    }
}   // End of FastFloat.cs
}   // End of namespace PDFjet.NET
