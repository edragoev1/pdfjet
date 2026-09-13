/*
 * PageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class PageTest {
    @Test
    void aNewPageTracksTheDefaultGraphicsState() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        assertEquals(1f, page.getPenWidth(), 0f);
        TestSupport.assertRGB(0f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
    }

    @Test
    void cmykSettersWriteCmykAndTrackTheRgbOfTheStandard() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setPenColorCMYK(0f, 1f, 1f, 0f);
        page.setBrushColorCMYK(0f, 0f, 0f, 1f);
        assertEquals("0 1 1 0 K\n0 0 0 1 k\n", TestSupport.content(page));
        TestSupport.assertRGB(1f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
    }

    @Test
    void restoreGraphicsStateRestoresTheTrackedState() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setPenColor(0xFF0000);
        page.saveGraphicsState();
        page.setPenWidth(3f);
        page.setPenColor(0x00FF00);
        page.restoreGraphicsState();
        assertEquals(1f, page.getPenWidth(), 0f);
        TestSupport.assertRGB(1f, 0f, 0f, page.getPenColor());
        assertTrue(TestSupport.content(page).endsWith("q\n3 w\n0 1 0 RG\nQ\n"), TestSupport.content(page));
    }

    @Test
    void gettersReturnCopies() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.getPenColor()[0] = 1f;
        page.getBrushColor()[0] = 1f;
        TestSupport.assertRGB(0f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
        page.drawLine(0f, 0f, 10f, 10f);
        page.getContent()[0] = 'X';
        assertTrue(TestSupport.content(page).charAt(0) != 'X');
    }

    @Test
    void drawLineWritesAStrokedPathWithTheYFlipped() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.drawLine(10f, 20f, 30f, 40f);
        String content = TestSupport.content(page);
        assertTrue(content.contains("10 772 m\n30 752 l\nS\n"), content);
    }
}
