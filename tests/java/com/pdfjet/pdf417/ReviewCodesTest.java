/*
 * ReviewCodesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.pdf417;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Color;
import com.pdfjet.Compliance;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import java.io.ByteArrayOutputStream;
import java.util.Arrays;
import org.junit.jupiter.api.Test;

class ReviewCodesTest {
    @Test
    void aControlCharacterIsShiftedToByteCompaction() throws Exception {
        // G S in alpha, then the pad and GS after the shift 913, then s e p
        // after the latch to lower case
        assertEquals(Arrays.asList(30*6 + 18, 30*26 + 29, 913, 0x1D, 30*27 + 18, 30*4 + 15),
                new PDF417("GS \u001dsep").dataCodewords());
        // HT, LF and CR are in text compaction
        assertFalse(new PDF417("a\tb\nc\r").dataCodewords().contains(913));
    }

    @Test
    void theBarsAreOneBlackArtifact() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setPenColor(Color.red);
        new PDF417("Hello, World!").drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("1 0 0 RG\n/Artifact BMC\nq\n0 0 0 RG\n") && content.endsWith("Q\nEMC\n"),
                content.substring(0, 80));
        assertEquals(1, content.split("BMC", -1).length - 1);
        // The page knows the pen is red again
        page.setPenColor(Color.red);
        assertEquals(content, TestSupport.content(page));
    }

    @Test
    void aDescribedBarcodeIsAFigure() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        PDF417 barcode = new PDF417("Hello, World!").setAltDescription("Hello, World!");
        barcode.setLocation(50f, 50f);
        barcode.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/Figure <</MCID 0>>\nBDC\nq\n"), content.substring(0, 80));
        assertFalse(content.contains("/Artifact"));
    }
}
