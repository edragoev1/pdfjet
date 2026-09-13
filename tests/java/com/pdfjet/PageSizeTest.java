/*
 * PageSizeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.Test;

class PageSizeTest {
    private static void assertSize(float width, float height, PageSize portrait, PageSize landscape) {
        assertEquals(width, portrait.getWidth(), 0f);
        assertEquals(height, portrait.getHeight(), 0f);
        assertEquals(height, landscape.getWidth(), 0f);
        assertEquals(width, landscape.getHeight(), 0f);
    }

    @Test
    void isoSizesInPoints() {
        assertSize(842f, 1191f, A3.PORTRAIT, A3.LANDSCAPE);
        assertSize(595f, 842f, A4.PORTRAIT, A4.LANDSCAPE);
        assertSize(420f, 595f, A5.PORTRAIT, A5.LANDSCAPE);
        assertSize(499f, 709f, B5.PORTRAIT, B5.LANDSCAPE);
    }

    @Test
    void japaneseAndNorthAmericanSizesInPoints() {
        assertSize(516f, 729f, JISB5.PORTRAIT, JISB5.LANDSCAPE);
        assertSize(612f, 792f, Letter.PORTRAIT, Letter.LANDSCAPE);
        assertSize(612f, 1008f, Legal.PORTRAIT, Legal.LANDSCAPE);
        assertSize(522f, 756f, Executive.PORTRAIT, Executive.LANDSCAPE);
        assertSize(792f, 1224f, Tabloid.PORTRAIT, Tabloid.LANDSCAPE);
    }

    @Test
    void aPageTakesItsSizeFromThePageSize() throws Exception {
        Page page = new Page(TestSupport.newPDF(), A4.LANDSCAPE);
        assertEquals(842f, page.getWidth(), 0f);
        assertEquals(595f, page.getHeight(), 0f);
        PageSize custom = new PageSize(100f, 200f);
        assertEquals(100f, custom.getWidth(), 0f);
        assertEquals(200f, custom.getHeight(), 0f);
    }
}
