/*
 * TextLineTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class TextLineTest {
    @Test
    void drawOnWritesTheTextAsHexAtTheFlippedY() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(TestSupport.helvetica(pdf), "Hello (x)").setLocation(10f, 20f);
        float[] xy = line.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("1 0 0 1 10 772 Tm\n"), content);
        assertTrue(content.contains("[<" + TestSupport.hex("Hello (x)") + ">] TJ\n"), content);
        assertEquals(44.664f, line.getWidth(), 0.001f);
        assertEquals(10f + line.getWidth(), xy[0], TestSupport.DELTA);
    }

    @Test
    void emptyTextDrawsNothingAndReturnsTheLocation() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        TestSupport.assertXY(5f, 6f, new TextLine(TestSupport.helvetica(pdf), "").setLocation(5f, 6f).drawOn(page));
        assertEquals(0, page.getContent().length);
    }

    @Test
    void colorSettersConvertAndCopy() throws Exception {
        TextLine line = new TextLine(TestSupport.helvetica(TestSupport.newPDF()), "x");
        line.setTextColor(0xFF8000);
        TestSupport.assertRGB(1f, 128f / 255f, 0f, line.getTextColor());

        float[] rgb = {0.1f, 0.2f, 0.3f};
        line.setTextColor(rgb);
        rgb[0] = 0.9f;
        line.getTextColor()[1] = 0.9f;
        TestSupport.assertRGB(0.1f, 0.2f, 0.3f, line.getTextColor());
    }

    @Test
    void underlineAddsAStrokedLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Hello").setLocation(10f, 20f).drawOn(plain);
        assertFalse(TestSupport.content(plain).contains("\nS\n"));

        Page underlined = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.helvetica(pdf), "Hello").setLocation(10f, 20f).setUnderline(true).drawOn(underlined);
        assertTrue(TestSupport.content(underlined).contains("\nS\n"));
    }
}
