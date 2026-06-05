using System;

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

        // Round to 2 decimal places
        float rounded = ((float)System.Math.Round(value * 100)) / 100f;

        bool negative = rounded < 0;
        if (negative) {
            rounded = -rounded;
        }
        
        int integerPart = (int)rounded;
        float decimalPart = rounded - integerPart;
        int decimalDigits = (int)(decimalPart * 100 + 0.5f);
        
        // Handle carry-over from rounding
        if (decimalDigits >= 100) {
            decimalDigits -= 100;
            integerPart++;
        }
        
        // Determine if we need decimal places
        bool hasDecimal = decimalDigits > 0;
        int trailingZeros = 0;
        
        if (hasDecimal) {
            // Count trailing zeros
            if (decimalDigits % 10 == 0) {
                trailingZeros = 1;
                if (decimalDigits / 10 % 10 == 0) {
                    trailingZeros = 2;
                }
            }
            hasDecimal = trailingZeros < 2;
        }
        
        // Calculate lengths
        int intDigits = integerPart == 0 ? 1 : (int)System.Math.Log10(integerPart) + 1;
        int totalLength = (negative ? 1 : 0) + intDigits + (hasDecimal ? 1 + (2 - trailingZeros) : 0);
        
        byte[] result = new byte[totalLength];
        int pos = 0;
        
        // Add sign
        if (negative) {
            result[pos++] = (byte)'-';
        }
        
        // Add integer part
        pos = WriteInt(integerPart, result, pos, intDigits);
        
        // Add decimal part if needed
        if (hasDecimal) {
            result[pos++] = (byte)'.';
            if (trailingZeros < 2) {
                result[pos++] = (byte)('0' + decimalDigits / 10);
            }
            if (trailingZeros < 1) {
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
