/*
 * ReviewCodesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.datamatrix;

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
import org.junit.jupiter.api.Test;

class ReviewCodesTest {
    @Test
    void aDescribedBarcodeIsAFigure() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("Hello, World!").setAltDescription("Hello, World!");
        dm.setLocation(50f, 50f);
        dm.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/Figure <</MCID 0>>\nBDC\nq\n"), content);
        assertFalse(content.contains("/Artifact"));
        // Not described, it is decoration, as before
        Page plain = new Page(pdf, Letter.PORTRAIT);
        new DataMatrix("Hello, World!").drawOn(plain);
        assertTrue(TestSupport.content(plain).startsWith("/Artifact BMC\nq\n"));
    }

    @Test
    void theBrushOfThePageIsKept() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setBrushColor(Color.blue);
        new DataMatrix("Hello").setModuleColor(Color.red).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.endsWith("Q\n"));
        // The page knows the brush is blue again, and sets red when asked
        page.setBrushColor(Color.blue);
        page.setBrushColor(Color.red);
        assertEquals("1 0 0 rg\n", TestSupport.content(page).substring(content.length()));
    }
}
