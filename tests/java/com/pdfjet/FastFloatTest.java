/*
 * FastFloatTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.Test;

/** The numbers written in content streams, which must be the same bytes in the four ports. */
class FastFloatTest {
    private static String format(float value) {
        return TestSupport.latin1(FastFloat.toByteArray(value));
    }

    @Test
    void writesWholeNumbersWithoutDecimals() {
        assertEquals("0", format(0f));
        assertEquals("1", format(1f));
        assertEquals("-3", format(-3f));
        assertEquals("100", format(100f));
    }

    @Test
    void leavesOutTrailingZeros() {
        assertEquals("0.5", format(0.5f));
        assertEquals("1.25", format(1.25f));
        assertEquals("0.1", format(0.1f));
        assertEquals("1.1", format(1.1f));
    }

    @Test
    void roundsHundredthsHalfAwayFromZero() {
        assertEquals("1.13", format(1.125f));
        assertEquals("-1.13", format(-1.125f));
        assertEquals("2.38", format(2.375f));
        assertEquals("0.13", format(0.125f));
        assertEquals("-0.13", format(-0.125f));
        assertEquals("0.01", format(0.006f));
        assertEquals("100", format(99.995f));
    }

    @Test
    void writesZeroForValuesThatRoundToZero() {
        assertEquals("0", format(-0f));
        assertEquals("0", format(-0.001f));
        assertEquals("0", format(0.004f));
    }

    @Test
    void writesLargeNumbersWithAllTheirDigits() {
        assertEquals("8388607.5", format(8388607.5f));
        assertEquals("8388608", format(8388608f));
        assertEquals("21600000", format(21600000f));
        assertEquals("1000000000", format(1e9f));
        assertEquals("-1000000000", format(-1e9f));
        assertEquals("2147483520", format(2147483520f));
    }

    @Test
    void writesIntoABufferAfterWhatItHolds() {
        byte[] buffer = new byte[2 + FastFloat.MAX_LENGTH];
        buffer[0] = '1';
        buffer[1] = ' ';
        assertEquals(buffer.length, FastFloat.write(-2147483520f, buffer, 2));
        assertEquals("1 -2147483520", TestSupport.latin1(buffer));
    }

    @Test
    void refusesNumbersAPdfCannotHold() {
        for (final float value : new float[] {Float.NaN, Float.POSITIVE_INFINITY, Float.NEGATIVE_INFINITY, 2147483648f, -3.4e38f}) {
            assertThrows(IllegalArgumentException.class, () -> FastFloat.toByteArray(value));
        }
    }
}
