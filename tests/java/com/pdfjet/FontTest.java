/*
 * FontTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assumptions.assumeTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.ByteBuffer;
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

    // The embedded font file of a PDF that draws a line of text in the font.
    private static byte[] embeddedFontFile(byte[] fontStream) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = new Font(pdf, new ByteArrayInputStream(fontStream));
        new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.complete();
        for (PDFobj obj : TestSupport.read(bos.toByteArray())) {
            if (obj.getValue("/Subtype").equals("/CIDFontType0C")) {
                return obj.getData();
            }
        }
        return null;
    }

    @Test
    void anOpenTypeStreamFontKeepsItsOtherTablesAndEmbedsOnlyItsCFFData() throws Exception {
        String path = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        InputStream in = TestSupport.open(path);
        byte[] whole;
        try {
            whole = TestSupport.readAll(in);
        } finally {
            in.close();
        }
        // The name, the info and the metrics come first, then 'R' with the
        // length of the other tables of the font, and then the CFF data.
        int i = 1 + (whole[0] & 0xFF);
        i += 3 + (((whole[i] & 0xFF) << 16) | ((whole[i + 1] & 0xFF) << 8) | (whole[i + 2] & 0xFF));
        i += 4 + ByteBuffer.wrap(whole, i, 4).getInt();
        assertEquals('R', whole[i]);
        int length = ByteBuffer.wrap(whole, i + 1, 4).getInt();
        // The same stream without the tables, as streams were written before.
        byte[] cffOnly = new byte[whole.length - 5 - length];
        System.arraycopy(whole, 0, cffOnly, 0, i);
        System.arraycopy(whole, i + 5 + length, cffOnly, i, whole.length - i - 5 - length);
        assertEquals('Y', cffOnly[i]);

        byte[] embedded = embeddedFontFile(whole);
        assertTrue(embedded.length > 0);
        assertArrayEquals(embedded, embeddedFontFile(cffOnly));
    }

    // The content of a page with a line of Thai in the font: po pla, the upper
    // vowel sara ii on it and the tone mark mai ek above the vowel.
    private static String thaiContent(String path) throws Exception {
        PDF pdf = TestSupport.newPDF();
        InputStream in = TestSupport.open(path);
        Font font;
        try {
            font = new Font(pdf, in);
        } finally {
            in.close();
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "\u0E1B\u0E35\u0E48").setLocation(50f, 50f).drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aStreamFontPlacesTheMarksAsTheOpenTypeFontDoes() throws Exception {
        assumeTrue(TestSupport.file("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf").exists(), "the fonts directory is not here");
        assertEquals(thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf"), thaiContent("fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.otf.stream"));
    }

    @Test
    void aCoreFontNumberOutsideTheFourteenIsRejected() throws Exception {
        PDF pdf = TestSupport.newPDF();
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 0));
        assertThrows(IllegalArgumentException.class, () -> new Font(pdf, 15));
    }

    @Test
    void theLineGapOfAFontSpacesTheLinesOfATextBlock() throws Exception {
        String path = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.stream";
        assumeTrue(TestSupport.file(path).exists(), "the fonts directory is not here");
        PDF pdf = TestSupport.newPDF();
        Font jp = new Font(pdf, TestSupport.open(path));
        assertEquals(10f, jp.getLineGap(10f), 0.001f);
        assertEquals(10f, new Font(pdf, TestSupport.open("fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf")).getLineGap(10f), 0.001f);
        assertEquals(0f, new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"))
                .getLineGap(10f), 0f);
        // The ascent, 8.8, the descent, 1.2, and the line gap, 10, for each line.
        jp.setSize(10f);
        TestSupport.assertXY(500f, 40f, new TextBlock(jp, "日本\n日本").setLocation(0f, 0f).drawOn(null));
    }
}
