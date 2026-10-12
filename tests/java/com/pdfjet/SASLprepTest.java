/*
 * SASLprepTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.Test;

/** SASLprep, RFC 4013, which prepares an AES-256 password. */
class SASLprepTest {
    // The examples of RFC 4013, section 3.
    @Test
    void prepareTheExamplesOfRFC4013() {
        String[][] cases = {
            {"I­X", "IX"},
            {"user", "user"},
            {"USER", "USER"},
            {"ª", "a"},
            {"Ⅸ", "IX"},
        };
        for (String[] c : cases) {
            assertEquals(c[1], SASLprep.prepare(c[0]), c[0]);
        }
        for (final String in : new String[] {"\u0007", "ا1"}) {
            assertThrows(IllegalArgumentException.class, () -> SASLprep.prepare(in), in);
        }
        // Right to left text that starts and ends with a right to left character
        assertEquals("ا1ب", SASLprep.prepare("ا1ب"));
    }

    // The password as an accented letter is typed on one keyboard and another,
    // a full-width letter, a no-break space and a Hangul syllable written as
    // its letters come out the same.
    @Test
    void prepareNormalizes() {
        String[][] cases = {
            {"café", "café"},
            {"café", "café"},
            {"ＡＢ", "AB"},
            {"a b", "a b"},
            {"각", "각"},
            {"각", "각"},
            {"ﬁ", "fi"},
            {"x²", "x2"},
            // Marks put in the order of their classes: dot below (220) before
            // the acute accent (230), the q with no composite of either
            {"q̣́", "q̣́"},
            // A blocked mark: the ring above stays apart from the A that the
            // acute accent of the same class, before it, did not compose with
            {"Á̊", "Á̊"},
        };
        for (String[] c : cases) {
            assertEquals(c[1], SASLprep.prepare(c[0]), c[0]);
        }
        assertThrows(IllegalArgumentException.class, () -> SASLprep.prepare("pass"),
                "a private use character is not refused");
    }

    @Test
    void truncateCutsBeforeACharacterThatDoesNotFit() {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 126; i++) {
            sb.append('a');
        }
        assertEquals(126, SASLprep.truncate(sb + "é").length);
        for (int i = 126; i < 200; i++) {
            sb.append('a');
        }
        assertEquals(127, SASLprep.truncate(sb.toString()).length);
    }
}
