/*
 * FastFloat.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.math.BigDecimal;
import java.util.Arrays;

class FastFloat {
    static final String NOT_WRITABLE = "A coordinate, size or width is NaN, infinite or too large for a PDF.";

    // A PDF number has no exponent and no NaN or infinity, and readers keep
    // integers in 32 bits, so a number must be finite and below 2^31.
    static boolean isWritable(float value) {
        return Math.abs(value) < 2147483648f;   // False for NaN and the infinities
    }

    // The most bytes a writable number takes: -2147483520, or -8388607.99.
    static final int MAX_LENGTH = 11;

    static byte[] toByteArray(float value) {
        byte[] bytes = new byte[MAX_LENGTH];
        return Arrays.copyOf(bytes, write(value, bytes, 0));
    }

    // Writes the number into the buffer at the position, where the buffer
    // must have MAX_LENGTH bytes, and returns the position after the number.
    static int write(float value, byte[] buffer, int pos) {
        if (!isWritable(value)) {
            throw new IllegalArgumentException(NOT_WRITABLE);
        }

        double magnitude = Math.abs((double) value);
        if (magnitude >= 8388608.0) {
            // A float of 2^23 or more is a whole number: write all its digits
            String digits = new BigDecimal(magnitude).toBigInteger().toString();
            byte[] bytes = ((value < 0f ? "-" : "") + digits).getBytes();
            System.arraycopy(bytes, 0, buffer, pos, bytes.length);
            return pos + bytes.length;
        }

        // Round to 2 decimal places, halves away from zero. A float times 100
        // is exact in a double, so the exact value of the float is rounded.
        double scaled = magnitude * 100.0;
        int hundredths = (int) scaled;
        if (scaled - hundredths >= 0.5) {
            hundredths++;
        }

        // A value that rounds to zero, like -0.001 and -0.0, is written 0
        boolean negative = value < 0f && hundredths > 0;
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

        // Add sign
        if (negative) buffer[pos++] = '-';

        // Add integer part
        pos = writeInt(integerPart, buffer, pos, intDigits);

        // Add decimal part if needed
        if (fractionDigits > 0) {
            buffer[pos++] = '.';
            buffer[pos++] = (byte)('0' + decimalDigits / 10);
            if (fractionDigits > 1) {
                buffer[pos++] = (byte)('0' + decimalDigits % 10);
            }
        }

        return pos;
    }

    private static int writeInt(int value, byte[] buffer, int pos, int digits) {
        for (int i = digits-1; i >= 0; i--) {
            buffer[pos + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return pos + digits;
    }
}   // End of FastFloat.java
