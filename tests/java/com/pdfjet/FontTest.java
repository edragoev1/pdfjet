/*
 * FontTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.InputStream;
import org.junit.jupiter.api.Test;

class FontTest {
    @Test
    void coreFontWidthsComeFromTheAfmMetrics() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals("Helvetica", font.getName());
        assertEquals(12f, font.getSize(), 0f);
        // H 722, e 556, l 222, l 222, o 556 in 1/1000 em.
        assertEquals(27.336f, font.stringWidth(12f, "Hello"), 0.001f);
        assertEquals(27.336f, font.stringWidth("Hello"), 0.001f);
        font.setSize(24f);
        assertEquals(54.672f, font.stringWidth("Hello"), 0.001f);
    }

    @Test
    void theWidthOfNoTextIsZero() throws Exception {
        assertEquals(0f, TestSupport.helvetica(TestSupport.newPDF()).stringWidth(null), 0f);
    }

    @Test
    void kerningPairsNarrowTheText() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals(16.008f, font.stringWidth(12f, "AV"), 0.001f);
        font.setKernPairs(true);
        // KPX A V -70
        assertEquals(15.168f, font.stringWidth(12f, "AV"), 0.001f);
    }

    @Test
    void coreFontVerticalMetrics() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        assertEquals(11.172f, font.getAscent(12f), 0.001f);
        assertEquals(2.7f, font.getDescent(12f), 0.001f);
        assertEquals(13.872f, font.getBodyHeight(12f), 0.001f);
    }

    @Test
    void getFitCharsCountsTheCharactersThatFit() throws Exception {
        assertEquals(5, TestSupport.helvetica(TestSupport.newPDF()).getFitChars("Hello world", 30f));
    }

    @Test
    void everyCjkCharacterIsOneEmWideAndSurrogatePairsCountOnce() throws Exception {
        Font font = new Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        assertEquals(20f, font.stringWidth(10f, "日本"), 0f);
        assertEquals(10f, font.stringWidth(10f, "𠀋"), 0f);
    }

    @Test
    void readsAStreamFont() throws Exception {
        String path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        InputStream in = TestSupport.open(path);
        try {
            Font font = new Font(TestSupport.newPDF(), in);
            assertEquals("IBMPlexSans", font.getName());
            assertEquals(28.32f, font.stringWidth(12f, "Hello"), 0.001f);
        } finally {
            in.close();
        }
    }
}
