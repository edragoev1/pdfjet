/*
 * BarcodeColorTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import com.pdfjet.barcodes.Barcode;
import com.pdfjet.datamatrix.DataMatrix;
import com.pdfjet.pdf417.PDF417;
import com.pdfjet.qrcode.ErrorCorrectionLevel;
import com.pdfjet.qrcode.QRCode;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

/**
 * The color of the barcodes, setModuleColor, black unless set.
 */
class BarcodeColorTest {
    private static final int NAVY = 0x1F3A5F;
    private static final String NAVY_STROKE = "0.12 0.23 0.37 RG";
    private static final String NAVY_FILL = "0.12 0.23 0.37 rg";

    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    @Test
    void aLinearBarcodeDrawsItsBarsAndItsTextInItsColor() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Barcode barcode = new Barcode(Barcode.CODE_128, "A1").setFont(TestSupport.helvetica(pdf));
        barcode.setLocation(50f, 50f);
        barcode.setModuleColor(NAVY).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(NAVY_STROKE), content);
        assertEquals(NAVY_FILL, TestSupport.fillColorBefore(content, "A1"));
    }

    @Test
    void aLinearBarcodeIsBlackUnlessSet() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setPenColor(Color.red);
        Barcode barcode = new Barcode(Barcode.CODE_128, "A1").setFont(TestSupport.helvetica(pdf));
        barcode.setLocation(50f, 50f);
        barcode.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("0 0 0 RG"), content);
    }

    @Test
    void aPDF417DrawsInItsColor() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new PDF417("A1").setLocation(50f, 50f).setModuleColor(NAVY).drawOn(page);
        assertTrue(TestSupport.content(page).contains(NAVY_STROKE), TestSupport.content(page));
    }

    @Test
    void theTwoDimensionalCodesDrawInTheirColor() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        QRCode qr = new QRCode("A1", ErrorCorrectionLevel.M).setModuleColor(NAVY);
        qr.setLocation(50f, 50f);
        qr.drawOn(page);
        assertTrue(TestSupport.content(page).contains(NAVY_FILL), TestSupport.content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("A1").setModuleColor(NAVY);
        dm.setLocation(50f, 50f);
        dm.drawOn(page);
        assertTrue(TestSupport.content(page).contains(NAVY_FILL), TestSupport.content(page));
    }
}
