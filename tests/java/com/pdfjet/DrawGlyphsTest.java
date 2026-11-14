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
// text: a span of actual text for each cluster, each glyph at its place, and
// the glyphs no character maps to mapped to their cluster's letters in
// ToUnicode, or to the zero width space.
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
        // The language around them all, then a span of actual text for each
        // cluster, its glyphs in a TJ array
        assertTrue(content.contains("/Span <</Lang <" + TestSupport.hex("hi") + ">>> BDC\nBT\n"), content);
        assertTrue(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("A") + ">>> BDC\n"
                + "1 0 0 1 50 692 Tm\n[<" + String.format("%04X", a) + ">] TJ\nEMC\n"), content);
        assertTrue(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("XY") + ">>> BDC\n"
                + "1 0 0 1 60 692 Tm\n[<" + String.format("%04X", x) + ">] TJ\nEMC\nET\nEMC\n"), content);
        assertEquals(4, count(content, "BDC\n"), content);   // The paragraph's, the language's, two spans
        // The unmapped glyph maps to its cluster's letters, "XY"; the mapped
        // one keeps its own
        String map = FontWriter.toUnicodeCMap(font, null).toLowerCase();
        assertTrue(map.contains("<" + String.format("%04x", x) + "> <00580059>"), map);
        assertFalse(map.contains("<" + String.format("%04x", a) + "> <0058"), map);
        pdf.complete();
    }

    @Test
    void anUnmappedGlyphBesideALetterTheClusterLacksIsTheZeroWidthSpace() throws Exception {
        // As Khmer's ោ, drawn as the glyph of េ and a second part: the letters
        // cannot add up to the cluster's, and MuPDF would copy it twice
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int b = font.unicodeToGID.get('B');
        int x = unmappedGlyph(font);
        page.drawGlyphs(font, new int[] {b, x}, new float[] {50f, 100f, 60f, 100f},
                new int[] {0, 0}, "XY", "en");
        String map = FontWriter.toUnicodeCMap(font, null).toLowerCase();
        assertTrue(map.contains("<" + String.format("%04x", x) + "> <200b>"), map);
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
    void anUntaggedPageHasTheSpansOfActualTextAlone() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int a = font.unicodeToGID.get('A');
        page.drawGlyphs(font, new int[] {a}, new float[] {50f, 100f}, null, "A", null);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("BT\n"), content);
        assertTrue(content.contains("/Span <</ActualText <" + Page.toUTF16Hex("A") + ">>> BDC\n"), content);
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
