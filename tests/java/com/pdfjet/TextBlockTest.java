/*
 * TextBlockTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class TextBlockTest {
    private static final String TEN_WORDS = "one two three four five six seven eight nine ten";

    @Test
    void aNewlineIsOneEmptyLine() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        float[] x = new TextBlock(font, "x").setLocation(0f, 0f).drawOn(null);
        TestSupport.assertXY(500f, 13.872f, x);
        TestSupport.assertXY(x[0], x[1], new TextBlock(font, "\n").setLocation(0f, 0f).drawOn(null));
        TestSupport.assertXY(x[0], x[1], new TextBlock(font, "").setLocation(0f, 0f).drawOn(null));
    }

    @Test
    void wrappedTextMakesTheBlockTallerThanItsSetHeight() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        TextBlock block = new TextBlock(font, TEN_WORDS).setLocation(0f, 0f).setSize(60f, 10f);
        // Six lines of 13.872 points.
        TestSupport.assertXY(60f, 83.232f, block.drawOn(null));
        assertEquals(83.232f, block.getHeight(), TestSupport.DELTA);
        assertEquals(60f, block.getWidth(), 0f);
    }

    @Test
    void drawOnAPageWritesEveryWord() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = new TextBlock(TestSupport.helvetica(pdf), TEN_WORDS).setLocation(0f, 0f).setSize(60f, 10f).drawOn(page);
        TestSupport.assertXY(60f, 83.232f, xy);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("one")), content);
        assertTrue(content.contains(TestSupport.hex("ten")), content);
    }
}
