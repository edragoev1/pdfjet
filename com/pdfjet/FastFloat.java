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

        int hundredths = roundedHundredths(magnitude);

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

    // Returns the number of hundredths that write writes for a magnitude below
    // 2^23: rounded to 2 decimal places, halves away from zero. A float times
    // 100 is exact in a double, so the exact value of the float is rounded.
    private static int roundedHundredths(double magnitude) {
        double scaled = magnitude * 100.0;
        int hundredths = (int) scaled;
        if (scaled - hundredths >= 0.5) {
            hundredths++;
        }
        return hundredths;
    }

    // Returns the value that write writes, in hundredths, with its sign, for
    // a value whose magnitude is below 2^23.
    static int toHundredths(float value) {
        int hundredths = roundedHundredths(Math.abs((double) value));
        return (value < 0f) ? -hundredths : hundredths;
    }

    // Returns the text of the float rounded to 5 decimal places, halves away
    // from zero, leaving out the trailing zeros, for the sine and the cosine of
    // a rotation matrix: rounded to hundredths, a turn of 1 degree is written
    // as one of 1.15. A value that is not writable is written as 0.
    static byte[] toPreciseByteArray(float value) {
        if (!isWritable(value)) {
            return new byte[] {'0'};
        }
        // A float times 100000 is exact in a double, and below 2^31 it fits a long.
        double scaled = Math.abs((double) value) * 100000.0;
        long units = (long) scaled;
        if (scaled - units >= 0.5) {
            units++;
        }
        StringBuilder text = new StringBuilder();
        if (value < 0f && units > 0) {
            text.append('-');
        }
        text.append(units / 100000);
        long fraction = units % 100000;
        if (fraction != 0) {
            int digits = 5;
            while (fraction % 10 == 0) {
                fraction /= 10;
                digits--;
            }
            // The digits of the fraction, with the zeros that lead them
            String fractionDigits = Long.toString(fraction);
            text.append('.');
            for (int i = fractionDigits.length(); i < digits; i++) {
                text.append('0');
            }
            text.append(fractionDigits);
        }
        return text.toString().getBytes();
    }

    private static int writeInt(int value, byte[] buffer, int pos, int digits) {
        for (int i = digits-1; i >= 0; i--) {
            buffer[pos + i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return pos + digits;
    }
}   // End of FastFloat.java
