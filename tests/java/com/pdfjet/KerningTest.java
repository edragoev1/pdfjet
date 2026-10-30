/*
 * KerningTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.InputStream;
import java.util.HashMap;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

class KerningTest {
    private static final String PLEX_TTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
    private static final String PLEX_OTF = "tests/fonts/IBMPlexSans-Regular.otf";
    // IBM Plex Serif, whose kern lookups have LookupFlag 8, as those of Noto
    // Sans, which these tests read before 9.1.7, have; the lookups of IBM Plex
    // Sans have none. IBM Plex Sans JP, whose kern feature has Extension
    // lookups of its pairs, as Source Serif 4, which PDFjet carried before, has.
    private static final String SERIF_TTF = "fonts/IBMPlexSerif/IBMPlexSerif-Regular.ttf";
    private static final String EXTENSION_OTF = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf";

    private static final Pattern DRAWN_TEXT = Pattern.compile("<([0-9A-F]*)> Tj|\\[(-?[0-9.]+)\\] TJ");

    // Opens a font of the repository, with its kerning on or off.
    static Font kernedFont(PDF pdf, String path, boolean kerned) throws Exception {
        try (InputStream in = TestSupport.open(path)) {
            Font font = new Font(pdf, in);
            font.setKernPairs(kerned);
            return font;
        }
    }

    // Returns how wide the text is as the page draws it, at the size: the
    // advances of its glyphs, with the moves of the TJ arrays between them,
    // from the content of the page.
    private static float drawnWidth(Font font, String content, float size) {
        float units = 0f;
        float moves = 0f;
        Matcher m = DRAWN_TEXT.matcher(content);
        while (m.find()) {
            if (m.group(2) != null) {
                moves += Float.parseFloat(m.group(2));
                continue;
            }
            String hex = m.group(1);
            for (int i = 0; i + 4 <= hex.length(); i += 4) {
                units += font.glyphAdvance(Integer.parseInt(hex.substring(i, i + 4), 16));
            }
        }
        return units*size/font.unitsPerEm - moves*size/1000f;
    }

    // Draws the text with the font on a page of its own, and returns what the
    // page wrote for it.
    private static String draw(Font font, String text) throws Exception {
        Page page = new Page(font.pdf, Letter.PORTRAIT);
        int start = TestSupport.content(page).length();
        page.drawString(font, null, 10f, text, 50f, 50f);
        return TestSupport.content(page).substring(start);
    }

    @Test
    void kerningNarrowsTheKernedPairsOfAnOpenTypeFont() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font kerned = kernedFont(pdf, PLEX_TTF, true);
        Font plain = kernedFont(pdf, PLEX_TTF, false);
        for (String pair : new String[] {"AV", "To", "Ty", "LT"}) {
            assertTrue(kerned.stringWidth(10f, pair) < plain.stringWidth(10f, pair),
                    pair + ": kerned " + kerned.stringWidth(10f, pair) + ", not kerned " + plain.stringWidth(10f, pair));
        }
        assertEquals(plain.stringWidth(10f, "HH"), kerned.stringWidth(10f, "HH"), 0f, "HH kerned");
    }

    @Test
    void kerningIsDrawnAsItIsMeasured() throws Exception {
        PDF pdf = TestSupport.newPDF();
        for (String path : new String[] {PLEX_TTF, PLEX_OTF, SERIF_TTF, EXTENSION_OTF}) {
            Font font = kernedFont(pdf, path, true);
            for (String text : new String[] {
                    "AVATAR. To Wally, Tyler and LT: \"Yes,\" P.V. said.",
                    "Обява: Тъй, Уважаеми Г-н Петров, ТАТЯНА.",
                    "Ταύτα, ΑΥΤΟ ΤΟ ΒΑΓΟΝΙ.",
                    "A V, then A V again",
                    "soft­hy​phen, T﻿o"}) {
                float drawn = drawnWidth(font, draw(font, text), 10f);
                assertEquals(font.stringWidth(10f, text), drawn, 0.01f, path + ": " + text);
            }
        }
    }

    @Test
    void kerningOfColoredTextIsDrawnAsItIsMeasured() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = kernedFont(pdf, PLEX_TTF, true);
        String text = "AVATAR To \"Wally\" LT.";
        Page page = new Page(pdf, Letter.PORTRAIT);
        int start = TestSupport.content(page).length();
        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("To", 0xFF0000);
        page.drawString(font, 12f, text, 50f, 50f, new float[] {0f, 0f, 0f}, colors);
        String content = TestSupport.content(page).substring(start);
        assertEquals(font.stringWidth(12f, text), drawnWidth(font, content, 12f), 0.01f, "colored");
        // A word with a mark, drawn on the shaping path, kerns with the last
        // glyph of the word before it, as stringWidth measures it
        String shaped = "A V. To Te\u0301 LT";
        start = TestSupport.content(page).length();
        page.drawString(font, 12f, shaped, 50f, 50f, new float[] {0f, 0f, 0f}, colors);
        content = TestSupport.content(page).substring(start);
        assertEquals(font.stringWidth(12f, shaped), drawnWidth(font, content, 12f), 0.01f, "colored, shaped");
    }

    private static final Pattern DRAWN_CORE_TEXT = Pattern.compile("\\[([^\\]]*)\\] TJ");
    private static final Pattern DRAWN_CORE_PIECE = Pattern.compile("<([0-9A-F]*)>|(-?[0-9.]+)");

    // Returns how wide the text drawn in a core font is as the page draws it,
    // at the size: the widths of its characters, with the moves of the TJ
    // arrays before and between them, from the content of the page. The end of
    // its last character is then where stringWidth says the text ends.
    private static float coreDrawnWidth(Font font, String content, float size) {
        float units = 0f;
        Matcher tj = DRAWN_CORE_TEXT.matcher(content);
        while (tj.find()) {
            Matcher m = DRAWN_CORE_PIECE.matcher(tj.group(1));
            while (m.find()) {
                if (m.group(2) != null) {
                    units -= Float.parseFloat(m.group(2));
                    continue;
                }
                String hex = m.group(1);
                for (int i = 0; i + 2 <= hex.length(); i += 2) {
                    units += font.metrics[Integer.parseInt(hex.substring(i, i + 2), 16) - 32][1];
                }
            }
        }
        return units*size/1000f;
    }

    // Colored text in a core font kerns the pairs across its words and its
    // color changes, as stringWidth measures them: Helvetica kerns "Y." (KPX Y
    // period), and a red "Y" before the period was drawn without it (the
    // review of 28 October 2026). A color changes only between words, so a
    // pair inside a word, as "AV", is never split by one.
    @Test
    void kerningOfColoredCoreFontTextIsDrawnAsItIsMeasured() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA);
        font.setKernPairs(true);
        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("Y", 0xFF0000);
        colors.put("To", 0x0000FF);
        for (String text : new String[] {
                "Y.",
                "AVATAR Y. To \"Wally\", LT: P.V. said.",
                "T\u00adoo, Y\u00ad."}) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            int start = TestSupport.content(page).length();
            page.drawString(font, 12f, text, 50f, 50f, new float[] {0f, 0f, 0f}, colors);
            String content = TestSupport.content(page).substring(start);
            assertEquals(font.stringWidth(12f, text), coreDrawnWidth(font, content, 12f), 0.001f, text);
        }
        // "Y." kerns by 140 units, the period moved as the TJ array it starts
        Page page = new Page(pdf, Letter.PORTRAIT);
        int start = TestSupport.content(page).length();
        page.drawString(font, 12f, "Y.", 50f, 50f, new float[] {0f, 0f, 0f}, colors);
        String content = TestSupport.content(page).substring(start);
        assertTrue(content.contains("[140<2E>] TJ"), "the period is not kerned:\n" + content);
    }

    // Text in a core font that is not colored is drawn as before the kerning
    // of colored text across its words: one TJ array, kerned inside it.
    @Test
    void kerningOfCoreFontTextNotColoredIsUnchanged() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, CoreFont.HELVETICA);
        font.setKernPairs(true);
        Page page = new Page(pdf, Letter.PORTRAIT);
        int start = TestSupport.content(page).length();
        page.drawString(font, 12f, "AV Y. To", 50f, 50f);
        assertEquals("BT\n/F1 12 Tf\n50 742 Td\n0 0 0 rg\n[<41>70<5620>90<59>140<2E>60<20>50<54>120<6F>] TJ\nET\n",
                TestSupport.content(page).substring(start));
    }

    @Test
    void kerningIsOffUnlessAskedFor() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = kernedFont(pdf, PLEX_TTF, false);
        String content = draw(font, "AVATAR To");
        assertFalse(content.contains("] TJ"), "a font not kerned moved its glyphs:\n" + content);
    }

    @Test
    void kerningLeavesMarksAndRightToLeftTextAlone() throws Exception {
        PDF pdf = TestSupport.newPDF();
        // IBM Plex Sans Hebrew kerns שלום עולם by 20 units without its
        // marks, as Noto Sans Hebrew, which this test read before 9.1.7,
        // kerned it by 10.
        for (String path : new String[] {
                "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf",
                "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"}) {
            Font kerned = kernedFont(pdf, path, true);
            Font plain = kernedFont(pdf, path, false);
            for (String text : new String[] {"שָׁלוֹם עוֹלָם", "مرحبا بالعالم"}) {
                assertEquals(plain.stringWidth(10f, text), kerned.stringWidth(10f, text), 0f,
                        path + ": " + text + " kerned");
            }
        }
    }

    @Test
    void kerningFitsWhatIsMeasured() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = kernedFont(pdf, PLEX_TTF, true);
        String text = "AVATAR To Wally";
        // getFitChars measures at the font's own size
        float width = font.stringWidth(font.getSize(), text);
        assertEquals(text.length(), font.getFitChars(text, width + 0.001f),
                "the characters fit in their own width");
        assertNotEquals(text.length(), font.getFitChars(text, width - 0.1f),
                "all the characters fit in less than their width");
        // A pair kerned wider, "T after a quotation mark, takes its room only
        // with its second character: every start of the text fits in its width
        String wider = "\"Tea\" &co";
        for (int n = 1; n <= wider.length(); n++) {
            float w = font.stringWidth(font.getSize(), wider.substring(0, n));
            assertEquals(n, font.getFitChars(wider, w + 0.001f), wider + ": the width of " + n);
        }
    }

    @Test
    void kerningOfABrokenTableIsNone() {
        byte[] buf = {0, 1, 0, 0, 0, 10, 0, 10, 0, 10, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF};
        Kerning k = Kerning.of(GPOS.of(buf, 0, buf.length, 10, false));
        assertEquals(0, k.read(null).pair(1, 2), "a broken table kerns");
    }

    // The same widths in the four ports, at 10 points: what each port
    // measures, and so wraps and aligns, is the same to the hundredth of a
    // point.
    @Test
    void kerningWidthsAreThoseOfTheOtherPorts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Object[][] cases = {
            {PLEX_TTF, "AVATAR To Wally", 75.14f, -302},
            {PLEX_TTF, "Тъй ТАТЯНА", 55.94f, -165},
            {PLEX_TTF, "ΑΥΤΟ", 24.44f, -70},
            {PLEX_OTF, "AVATAR To Wally", 75.14f, -302},
            // IBM Plex Serif, from its hmtx and kern lookups (Noto Sans, before
            // 9.1.7: 75.83 and -310, 56.79 and -150, 24.62 and -80)
            {SERIF_TTF, "AVATAR To Wally", 80.59f, -315},
            {SERIF_TTF, "Тъй ТАТЯНА", 61.57f, -147},
            {SERIF_TTF, "ΑΥΤΟ", 26.40f, -48},
        };
        for (Object[] c : cases) {
            String path = (String) c[0];
            String text = (String) c[1];
            Font font = kernedFont(pdf, path, true);
            assertEquals((Float) c[2], font.stringWidth(10f, text), 0.005f, path + ": " + text);
            assertEquals((int) (Integer) c[3], font.textKerning(text), path + ": " + text + " in font units");
        }
    }

    @Test
    void kerningIsMeasuredFromManyThreadsAtOnce() throws Exception {
        PDF pdf = TestSupport.newPDF();
        String text = "AVATAR To Wally";
        float expected = kernedFont(pdf, PLEX_TTF, true).stringWidth(10f, text);
        ExecutorService threads = Executors.newFixedThreadPool(8);
        try {
            for (int round = 0; round < 20; round++) {
                final Font font = kernedFont(pdf, PLEX_TTF, true);
                List<Future<Float>> widths = new ArrayList<Future<Float>>();
                for (int t = 0; t < 8; t++) {
                    widths.add(threads.submit(() -> font.stringWidth(10f, text)));
                }
                for (Future<Float> width : widths) {
                    assertEquals(expected, width.get(), 0f, "the width measured in a thread");
                }
            }
        } finally {
            threads.shutdown();
        }
    }

    // Returns a GPOS table whose kern feature has the lookups, all one lookup
    // of the subtables, all one PairPos subtable of the format, which moves
    // the glyph after itself by -50: the pair of the glyph, or the pair of its
    // class 1.
    private static byte[] kernTable(int lookups, int subtables, int format, int glyph) {
        List<Integer> v = new ArrayList<Integer>();
        put(v, 1, 0, 10, 30, 42 + 2*lookups);
        put(v, 1, 0x4446, 0x4C54, 8);   // DFLT
        put(v, 4, 0, 0, 0xFFFF, 1, 0);
        put(v, 1, 0x6B65, 0x726E, 8);   // kern
        put(v, 0, lookups);
        for (int i = 0; i < lookups; i++) {
            put(v, i);
        }
        put(v, lookups);
        for (int i = 0; i < lookups; i++) {
            put(v, 2 + 2*lookups);
        }
        put(v, 2, 0, subtables);
        for (int i = 0; i < subtables; i++) {
            put(v, 6 + 2*subtables);
        }
        if (format == 1) {
            put(v, 1, 12, 4, 0, 1, 18);     // The coverage at 12, the pair set at 18
            put(v, 1, 1, glyph);
            put(v, 1, glyph, -50);
        } else {
            put(v, 2, 24, 4, 0, 30, 30, 2, 2);  // The coverage at 24, the classes at 30
            put(v, 0, 0, 0, -50);
            put(v, 1, 1, glyph);
            put(v, 1, glyph, 1, 1);
        }
        byte[] buf = new byte[2*v.size()];
        for (int i = 0; i < v.size(); i++) {
            buf[2*i] = (byte) (v.get(i) >> 8);
            buf[2*i + 1] = (byte) (int) v.get(i);
        }
        return buf;
    }

    private static void put(List<Integer> v, int... values) {
        for (int value : values) {
            v.add(value);
        }
    }

    private static Kerning kerning(byte[] buf, int numGlyphs) {
        return Kerning.of(GPOS.of(buf, 0, buf.length, numGlyphs, false)).read(null);
    }

    // A table of a few kilobytes whose subtables named glyph 65535 again and
    // again made gigabytes of arrays by glyph; the room they make is work,
    // and a subtable a lookup names again is read once (the review of 28
    // October 2026). The arrays grow to twice what they hold at most.
    @Test
    void kerningOfATableThatNamesGlyph65535AgainIsBounded() {
        int[][] cases = {{1, 400, 1}, {400, 1, 1}, {400, 1, 2}};
        for (int[] c : cases) {
            int room = kerning(kernTable(c[0], c[1], c[2], 65535), 0).room();
            assertTrue(room <= 2*GPOS.MAX_GPOS_WORK, c[0] + " lookups, " + c[1] + " subtables, format " + c[2] + ": " + room + " words");
        }
        assertEquals(-50, kerning(kernTable(1, 400, 1, 65535), 0).pair(65535, 65535), "the subtable named again");
        // A glyph past the glyphs of the font is left out; a glyph of the
        // font kerns as before.
        for (int format = 1; format <= 2; format++) {
            Kerning k = kerning(kernTable(1, 1, format, 65535), 1000);
            assertEquals(0, k.room(), "format " + format + ": words for a glyph the font does not have");
            assertEquals(0, k.pair(65535, 65535), "format " + format + ": a glyph the font does not have");
            assertEquals(-50, kerning(kernTable(1, 1, format, 7), 1000).pair(7, 7), "format " + format + ": glyph 7");
        }
    }
}
