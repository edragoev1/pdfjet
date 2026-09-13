/*
 * BidiTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

/** Right to left text in visual order: the Unicode Bidirectional Algorithm and Arabic shaping. */
class BidiTest {
    @Test
    void leftToRightTextIsUnchanged() {
        assertEquals("abc", Bidi.reorderVisually("abc"));
    }

    @Test
    void hebrewIsReversed() {
        assertEquals("םולש", Bidi.reorderVisually("שלום"));
    }

    @Test
    void digitsKeepTheirOrder() {
        assertEquals("123 םולש", Bidi.reorderVisually("שלום 123"));
    }

    @Test
    void aLineWithRightToLeftTextIsLaidOutRightToLeft() {
        // The README: a line that has right to left text is a right to left line.
        assertEquals("םולש abc", Bidi.reorderVisually("abc שלום"));
    }

    @Test
    void bracketsAroundLeftToRightTextAreKeptWithItBetweenMarks() {
        assertEquals("‎(abc)‎ םולש",
                Bidi.reorderVisually("שלום (abc)"));
        assertEquals("‏(ש‏)", Bidi.reorderVisually("(ש)"));
    }

    @Test
    void arabicIsShapedWithLigaturesAndReversed() {
        // seen initial, lam alef final ligature, meem isolated
        assertEquals("ﻡﻼﺳ", Bidi.reorderVisually("سلام"));
    }

    @Test
    void zeroWidthNonJoinerIsKept() {
        String shaped = Bidi.reorderVisually("می‌خواهم");
        assertEquals("ﻢﻫﺍﻮﺧ‌ﯽﻣ", shaped);
    }

    @Test
    void aRangeIsReorderedInTheContextOfTheWholeString() {
        assertEquals("ול", Bidi.reorderVisually("שלום", 1, 3));
    }

    @Test
    void isArabicTellsArabicLettersApart() {
        assertTrue(Bidi.isArabic(0x0633));
        assertFalse(Bidi.isArabic('a'));
        assertFalse(Bidi.isArabic(0x05E9));
    }
}
