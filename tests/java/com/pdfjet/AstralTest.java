/*
 * AstralTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

/**
 * The characters past U+FFFF, as the Chinese characters of Hong Kong, 𨋢 the
 * lift among them, which IBM Plex Sans TC maps in its format 12 subtable.
 */
class AstralTest {
    @Test
    void theGlyphMapGoesPastTheBasicMultilingualPlane() {
        GlyphMap m = new GlyphMap();
        m.set('A', 3);
        m.set(0x282E2, 7);
        m.setIfAbsent(0x282E2, 9);  // Kept
        m.setIfAbsent(0x20021, 5);
        m.set(0x110000, 4);         // Not a character
        assertEquals(3, m.get('A'));
        assertEquals(7, m.get(0x282E2));
        assertEquals(5, m.get(0x20021));
        assertEquals(0, m.get(0x110000));
        assertEquals(0, m.get(0x20000));
        final List<Integer> order = new ArrayList<Integer>();
        m.each((c, gid) -> order.add(c));
        assertEquals(java.util.Arrays.asList((int) 'A', 0x20021, 0x282E2), order);
        // A font of a million characters past U+FFFF is read no further than
        // MAX_ASTRAL
        GlyphMap big = new GlyphMap();
        for (int c = 0x10000; c < 0x10000 + 2 * GlyphMap.MAX_ASTRAL; c++) {
            big.setIfAbsent(c, 1);
        }
        assertEquals(GlyphMap.MAX_ASTRAL, big.astral.size());
    }

    @Test
    void aCharacterPastU0xFFFFIsDrawnInTheFallbackFont() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font plex = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Font tc = new Font(pdf, TestSupport.open("fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.ttf"));
        assertTrue(tc.hasGlyph(0x282E2), "TC has 𨋢");
        assertFalse(plex.hasGlyph(0x282E2), "Plex Sans has 𨋢");
        int gid = tc.unicodeToGID.get(0x282E2);
        assertTrue(gid != 0 && gid != tc.unicodeToGID.get('乘'), "𨋢 is glyph " + gid);
        // Each a Chinese character of TC, one em wide, none the box of Plex Sans
        assertEquals(3000f, plex.stringWidth(tc, 1000f, "乘𨋢到"), 0f);
        // Copied out of the PDF as 𨋢: in UTF-16, the two halves of a surrogate
        // pair
        assertTrue(FontWriter.toUnicodeCMap(tc, null).contains("<d860dee2>"));
        assertEquals("d860dee2", FontWriter.unicodeHex(0x282E2));
        assertEquals("0041", FontWriter.unicodeHex('A'));
    }
}
