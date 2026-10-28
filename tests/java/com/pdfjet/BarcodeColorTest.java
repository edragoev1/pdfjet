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

    // The stroke and fill colors the content sets, as their operators, for
    // example "1 1 1 rg", in their order.
    private static java.util.List<String> colors(String content) {
        java.util.List<String> colors = new java.util.ArrayList<String>();
        for (String line : content.split("\n")) {
            if (line.endsWith(" RG") || line.endsWith(" rg")) {
                colors.add(line);
            }
        }
        return colors;
    }

    // Draws each linear barcode but Code 128 in each direction, with its text,
    // on a page of its own, in the module color when it is not null, and
    // returns the names and the contents of the pages.
    private static java.util.List<String[]> drawLinearBarcodes(Integer moduleColor) throws Exception {
        TestSupport.allowCoreFonts(true);
        Object[][] cases = {
            {"UPC-A", Barcode.UPC_A, "01234567890"},
            {"EAN-13", Barcode.EAN_13, "012345678901"},
            {"Code 39", Barcode.CODE_39, "HELLO-39"},
            {"ITF-14", Barcode.ITF_14, "1540014128876"},
        };
        java.util.List<String[]> drawn = new java.util.ArrayList<String[]>();
        for (Object[] c : cases) {
            for (Direction direction : Direction.values()) {
                PDF pdf = TestSupport.newPDF();
                Page page = new Page(pdf, Letter.PORTRAIT);
                Barcode barcode = new Barcode((Integer) c[1], (String) c[2]).setFont(TestSupport.helvetica(pdf));
                barcode.setDirection(direction);
                barcode.setLocation(50f, 50f);
                if (moduleColor != null) {
                    barcode.setModuleColor(moduleColor);
                }
                barcode.drawOn(page);
                drawn.add(new String[] {c[0] + ", " + direction, TestSupport.content(page)});
            }
        }
        return drawn;
    }

    // UPC-A, EAN-13, Code 39 and ITF-14 draw their bars and their text in
    // their color in every direction, as Code 128 does: only Code 128, from
    // left to right, was tested (the review of 28 October 2026).
    @Test
    void everyLinearBarcodeDrawsInItsColorInEveryDirection() throws Exception {
        for (String[] drawn : drawLinearBarcodes(NAVY)) {
            java.util.List<String> colors = colors(drawn[1]);
            assertTrue(colors.contains(NAVY_STROKE) && colors.contains(NAVY_FILL) && drawn[1].contains("] TJ"),
                    drawn[0] + ": not drawn in navy with its text: " + colors);
            for (String operator : colors) {
                assertTrue(operator.equals(NAVY_STROKE) || operator.equals(NAVY_FILL), drawn[0] + ": " + operator);
            }
        }
    }

    // Unless their color is set, they are black: drawn as they are with black
    // set.
    @Test
    void everyLinearBarcodeIsBlackUnlessSet() throws Exception {
        java.util.List<String[]> unset = drawLinearBarcodes(null);
        for (String[] drawn : unset) {
            java.util.List<String> colors = colors(drawn[1]);
            assertTrue(colors.contains("0 0 0 RG") && colors.contains("0 0 0 rg"),
                    drawn[0] + ": not drawn in black: " + colors);
            for (String operator : colors) {
                assertTrue(operator.equals("0 0 0 RG") || operator.equals("0 0 0 rg"), drawn[0] + ": " + operator);
            }
        }
        java.util.List<String[]> black = drawLinearBarcodes(Color.black);
        for (int i = 0; i < unset.size(); i++) {
            assertEquals(unset.get(i)[1], black.get(i)[1], unset.get(i)[0] + ": black set draws another barcode");
        }
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
