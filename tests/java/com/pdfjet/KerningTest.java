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
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.InputStream;
import java.util.HashMap;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

class KerningTest {
    private static final String PLEX_TTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf";
    private static final String PLEX_OTF = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf";
    private static final String NOTO_TTF = "fonts/NotoSans/NotoSans-Regular.ttf";

    private static final Pattern DRAWN_TEXT = Pattern.compile("<([0-9A-F]*)> Tj|\\[(-?[0-9.]+)\\] TJ");

    // Opens a font of the repository, with its kerning on or off.
    private static Font kernedFont(PDF pdf, String path, boolean kerned) throws Exception {
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
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
        for (String path : new String[] {PLEX_TTF, PLEX_OTF, NOTO_TTF}) {
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
        for (String path : new String[] {
                "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf",
                "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"}) {
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
        Kerning k = Kerning.of(GPOS.of(buf, 0, buf.length, 10, false), GPOS.of(buf, 4, 900, 10, false));
        assertEquals(0, k.pair(1, 2), "a broken table kerns");
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
            {NOTO_TTF, "AVATAR To Wally", 75.83f, -310},
            {NOTO_TTF, "Тъй ТАТЯНА", 56.79f, -150},
            {NOTO_TTF, "ΑΥΤΟ", 24.62f, -80},
        };
        for (Object[] c : cases) {
            String path = (String) c[0];
            String text = (String) c[1];
            Font font = kernedFont(pdf, path, true);
            assertEquals((Float) c[2], font.stringWidth(10f, text), 0.005f, path + ": " + text);
            assertEquals((int) (Integer) c[3], font.textKerning(text), path + ": " + text + " in font units");
        }
    }
}
