/*
 * DrawGlyphsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;

import org.junit.jupiter.api.Test;

// drawGlyphs draws glyphs another engine shaped, by their numbers, as real
// text: a span of actual text around them, each glyph at its place, and the
// glyphs no character maps to mapped to their cluster's text in ToUnicode.
class DrawGlyphsTest {

    // The first glyph of the font no character maps to, like a conjunct
    private static int unmappedGlyph(Font font) {
        for (int gid = 1; gid < font.unicodeOf.length; gid++) {
            if (font.unicodeOf[gid] < 0) {
                return gid;
            }
        }
        throw new AssertionError("the font maps every glyph");
    }

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    @Test
    void theGlyphsAreRealTextInATaggedParagraph() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        font.setSize(12f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        int x = unmappedGlyph(font);
        page.addBDC(StructElem.P, "en", null, null);
        page.drawGlyphs(font, new int[] {a, x}, new float[] {50f, 100f, 60f, 100f},
                new int[] {0, 1}, "AXY", "hi");
        page.addEMC();
        String content = TestSupport.content(page);
        assertTrue(content.contains("/Span <</Lang <" + TestSupport.hex("hi") + "> /ActualText <"
                + Page.toUTF16Hex("AXY") + ">>> BDC\nBT\n"), content);
        assertTrue(content.contains("1 0 0 1 50 692 Tm\n<" + String.format("%04X", a) + "> Tj\n"), content);
        assertTrue(content.contains("1 0 0 1 60 692 Tm\n<" + String.format("%04X", x) + "> Tj\nET\nEMC\n"), content);
        assertEquals(2, count(content, "BDC\n"), content);   // The paragraph's, and the span's
        // The unmapped glyph maps to its cluster's characters, "XY"; the
        // mapped one keeps its own
        String map = FontWriter.toUnicodeCMap(font, null).toLowerCase();
        assertTrue(map.contains("<" + String.format("%04x", x) + "> <00580059>"), map);
        assertFalse(map.contains("<" + String.format("%04x", a) + "> <0058"), map);
        pdf.complete();
    }

    @Test
    void outsideAnyContentTheGlyphsAreAParagraphOfTheirOwn() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, null, "A", "en");
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/P <</MCID 0>>\nBDC\n/Span <<"), content);
        assertTrue(content.endsWith("ET\nEMC\nEMC\n"), content);
        assertEquals(1, page.structures.size());
    }

    @Test
    void anUntaggedPageHasTheSpanOfActualTextAlone() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, null, "A", null);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/Span <</ActualText <" + Page.toUTF16Hex("A") + ">>> BDC\nBT\n"), content);
        assertFalse(content.contains("MCID"), content);
    }

    @Test
    void misuseIsReported() throws Exception {
        TestSupport.allowCoreFonts(true);
        final PDF pdf = new PDF(new ByteArrayOutputStream());
        final Font plex = TestSupport.plexSans(pdf);
        final Font core = TestSupport.helvetica(pdf);
        final Page page = new Page(pdf, Letter.PORTRAIT);
        final int a = plex.unicodeToGID.get('A');
        assertThrows(IllegalArgumentException.class,
                () -> page.drawGlyphs(core, new int[] {1}, new float[] {0f, 0f}, null, "A", null));
        assertThrows(IllegalArgumentException.class,
                () -> page.drawGlyphs(plex, new int[] {a}, new float[] {0f}, null, "A", null));
        assertThrows(IllegalArgumentException.class,
                () -> page.drawGlyphs(plex, new int[] {plex.advanceWidth.length}, new float[] {0f, 0f}, null, "A", null));
        assertThrows(IllegalArgumentException.class,
                () -> page.drawGlyphs(plex, new int[] {a}, new float[] {0f, 0f}, new int[] {1}, "A", null));
        assertThrows(IllegalArgumentException.class,
                () -> page.drawGlyphs(plex, new int[] {a}, new float[] {0f, 0f}, new int[] {0, 0}, "A", null));
    }
}
