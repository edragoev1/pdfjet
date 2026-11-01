/*
 * UnicodeCategoriesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

/**
 * The table of categories of the Java port, which gives a runtime of Java 8,
 * of Unicode 6.2, the result of a later one (the review of 30 October 2026).
 */
class UnicodeCategoriesTest {
    private static int group(int type) {
        switch (type) {
        case Character.UPPERCASE_LETTER:
        case Character.LOWERCASE_LETTER:
        case Character.TITLECASE_LETTER:
        case Character.MODIFIER_LETTER:
        case Character.OTHER_LETTER:
            return Character.OTHER_LETTER;
        case Character.NON_SPACING_MARK:
        case Character.ENCLOSING_MARK:
        case Character.COMBINING_SPACING_MARK:
        case Character.DECIMAL_DIGIT_NUMBER:
        case Character.SPACE_SEPARATOR:
        case Character.LINE_SEPARATOR:
        case Character.PARAGRAPH_SEPARATOR:
        case Character.FORMAT:
        case Character.CURRENCY_SYMBOL:
            return type;
        default:
            return Character.UNASSIGNED;
        }
    }

    // Every code point has the category of the runtime, apart from what a
    // Unicode other than 15.0 assigned or changed: a code point that is
    // assigned on one side only, of the few that are, and the marks U+1171E
    // and U+11F41 and the like that later versions moved between categories.
    @Test
    void everyCodePointHasTheCategoryOfTheRuntime() {
        int different = 0;
        for (int c = 0; c <= 0x10FFFF; c++) {
            int table = UnicodeCategories.of(c);
            int runtime = group(Character.getType(c));
            if (table != runtime && table != Character.UNASSIGNED && runtime != Character.UNASSIGNED) {
                different++;
            }
        }
        assertTrue(different < 40, different + " code points differ");
    }

    @Test
    void theEndsOfTheTable() {
        assertEquals(Character.UNASSIGNED, UnicodeCategories.of(-1));
        assertEquals(Character.UNASSIGNED, UnicodeCategories.of(0x110000));
        assertEquals(Character.UNASSIGNED, UnicodeCategories.of(0x10FFFF));
        assertEquals(Character.OTHER_LETTER, UnicodeCategories.of('A'));
        assertEquals(Character.DECIMAL_DIGIT_NUMBER, UnicodeCategories.of('7'));
        assertEquals(Character.SPACE_SEPARATOR, UnicodeCategories.of(' '));
        assertEquals(Character.CURRENCY_SYMBOL, UnicodeCategories.of(0x20AC));
        assertEquals(Character.NON_SPACING_MARK, UnicodeCategories.of(0xE01EF));
    }

    // The Arabic mark U+08D4, of Unicode 9, is a mark on any runtime, and it
    // joins nothing, as the Go port has it.
    @Test
    void aMarkNewerThanTheRuntimeIsAMark() {
        assertEquals(Character.NON_SPACING_MARK, UnicodeCategories.of(0x08D4));
        assertTrue(Util.isMark(0x08D4));
        assertTrue(TextBlock.staysWithPrevious(0x08D4));
    }

    // U+180E was a space up to Unicode 6.2, and Java 8 has it as one.
    @Test
    void theMongolianVowelSeparatorIsNoSpace() {
        assertEquals(Character.FORMAT, UnicodeCategories.of(0x180E));
        assertFalse(Util.isWhitespace(0x180E));
        assertFalse(Util.isSpace(0x180E));
        assertFalse(SVGState.isSpace('\u180E'));
        assertTrue(Util.isWhitespace(' '));
        assertTrue(Util.isWhitespace('\u2003'));
        assertFalse(Util.isWhitespace('\u00A0'));
    }

    @Test
    void aShortColorIsAnArgumentError() {
        org.junit.jupiter.api.Assertions.assertThrows(IllegalArgumentException.class,
                () -> Util.toPackedRGB(new float[] {0.5f}));
    }
}
