package com.pdfjet;

class FastFloat {
    static byte[] toByteArray(float value) {
        // Handle special cases
        if (Float.isNaN(value)) return new byte[]{'N','a','N'};
        if (value == Float.POSITIVE_INFINITY) return new byte[]{'I','n','f','i','n','i','t','y'};
        if (value == Float.NEGATIVE_INFINITY) return new byte[]{'-','I','n','f','i','n','i','t','y'};

        // Round to 2 decimal places
        float rounded = ((float)Math.round(value * 100)) / 100.0f;

        boolean negative = rounded < 0;
        if (negative) rounded = -rounded;
        
        int integerPart = (int)rounded;
        float decimalPart = rounded - integerPart;
        int decimalDigits = (int)(decimalPart * 100 + 0.5f);
        
        // Handle carry-over from rounding
        if (decimalDigits >= 100) {
            decimalDigits -= 100;
            integerPart++;
        }
        
        // Determine if we need decimal places
        boolean hasDecimal = decimalDigits > 0;
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
        int intDigits = integerPart == 0 ? 1 : (int)Math.log10(integerPart) + 1;
        int totalLength = (negative ? 1 : 0) + intDigits + (hasDecimal ? 1 + (2 - trailingZeros) : 0);
        
        byte[] result = new byte[totalLength];
        int pos = 0;
        
        // Add sign
        if (negative) result[pos++] = '-';
        
        // Add integer part
        pos = writeInt(integerPart, result, pos, intDigits);
        
        // Add decimal part if needed
        if (hasDecimal) {
            result[pos++] = '.';
            if (trailingZeros < 2) {
                result[pos++] = (byte)('0' + decimalDigits / 10);
            }
            if (trailingZeros < 1) {
                result[pos++] = (byte)('0' + decimalDigits % 10);
            }
        }
        
        return result;
    }

    private static int writeInt(int value, byte[] buffer, int pos, int digits) {
        for (int i = digits-1; i >= 0; i--) {
            buffer[pos + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return pos + digits;
    }
}   // End of FastFloat.java
