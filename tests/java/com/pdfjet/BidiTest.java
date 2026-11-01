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
    // Text pasted from a PDF often has presentation forms, and Persian and
    // Urdu text the letters of Arabic Supplement.
    @Test
    void theOtherLettersOfRightToLeftScriptsAreRightToLeft() {
        assertEquals("\uFB2C\uFB2A abc", Bidi.reorderVisually("abc \uFB2A\uFB2C"));   // Hebrew presentation forms
        assertEquals("\uFEE2\uFE8D abc", Bidi.reorderVisually("abc \uFE8D\uFEE2"));   // Arabic presentation forms B
        assertEquals("\uFB51\uFB50 abc", Bidi.reorderVisually("abc \uFB50\uFB51"));   // Arabic presentation forms A
        assertEquals("\u0751\u0750 abc", Bidi.reorderVisually("abc \u0750\u0751"));   // Arabic Supplement
        assertEquals("\u08A1\u08A0 abc", Bidi.reorderVisually("abc \u08A0\u08A1"));   // Arabic Extended-A
        assertEquals("\u07CB\u07CA abc", Bidi.reorderVisually("abc \u07CA\u07CB"));   // NKo
        assertEquals(new StringBuilder().appendCodePoint(0x10801).appendCodePoint(0x10800).append(" abc").toString(),
                Bidi.reorderVisually(new StringBuilder("abc ").appendCodePoint(0x10800).appendCodePoint(0x10801).toString()));
    }

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
