/*
 * BarcodeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.barcodes;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.pdfjet.Direction;
import com.pdfjet.Font;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import org.junit.jupiter.api.Test;

class BarcodeTest {
    // type, text, direction, with font, corner x, corner y
    private static final Object[][] CORNERS = {
        {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, false, 171.25f, 145.5f},
        {Barcode.EAN_13, "012345678901", Direction.LEFT_TO_RIGHT, true, 171.25f, 151.31f},
        {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, false, 145.5f, 171.25f},
        {Barcode.EAN_13, "012345678901", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, false, 145.5f, 171.25f},
        {Barcode.EAN_13, "012345678901", Direction.TOP_TO_BOTTOM, true, 145.5f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, false, 171.25f, 145.5f},
        {Barcode.UPC_A, "01234567890", Direction.LEFT_TO_RIGHT, true, 179.59f, 151.31f},
        {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, false, 145.5f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.BOTTOM_TO_TOP, true, 151.31f, 179.59f},
        {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, false, 145.5f, 171.25f},
        {Barcode.UPC_A, "01234567890", Direction.TOP_TO_BOTTOM, true, 145.5f, 179.59f},
        {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, false, 167.5f, 137.5f},
        {Barcode.CODE_128, "Hello", Direction.LEFT_TO_RIGHT, true, 167.5f, 154.072f},
        {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, false, 137.5f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.BOTTOM_TO_TOP, true, 154.072f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, false, 137.5f, 167.5f},
        {Barcode.CODE_128, "Hello", Direction.TOP_TO_BOTTOM, true, 137.5f, 167.5f},
        {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, false, 219.25f, 137.5f},
        {Barcode.CODE_39, "HELLO-39", Direction.LEFT_TO_RIGHT, true, 219.25f, 154.072f},
        {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, false, 137.5f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.BOTTOM_TO_TOP, true, 154.072f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, false, 137.5f, 219.25f},
        {Barcode.CODE_39, "HELLO-39", Direction.TOP_TO_BOTTOM, true, 137.5f, 219.25f},
    };

    @Test
    void drawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        for (Object[] row : CORNERS) {
            Barcode barcode = new Barcode((Integer) row[0], (String) row[1])
                    .setLocation(100f, 100f).setDirection((Direction) row[2]);
            if ((Boolean) row[3]) {
                barcode.setFont(font);
            }
            String name = row[0] + " " + row[2] + " font " + row[3];
            float[] first = barcode.drawOn(page);
            assertEquals((Float) row[4], first[0], TestSupport.DELTA, name);
            assertEquals((Float) row[5], first[1], TestSupport.DELTA, name);
            float[] second = barcode.drawOn(page);
            assertEquals(first[0], second[0], 0f, name + " drawn again");
            assertEquals(first[1], second[1], 0f, name + " drawn again");
            assertEquals((Boolean) row[3] ? 51.372f : 37.5f, barcode.getHeight(), TestSupport.DELTA, name);
        }
    }

    @Test
    void code39RejectsCharactersItCannotEncode() throws Exception {
        final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        Exception e = assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_39, "hello").drawOn(page));
        assertEquals("The input string '*hello*' contains characters that are invalid in a Code39 barcode.", e.getMessage());
    }

    @Test
    void upcAndEanNeedTheirNumberOfDigits() {
        Exception upc = assertThrows(Exception.class, () -> new Barcode(Barcode.UPC_A, "123"));
        assertEquals("UPC-A barcodes must have exactly 11 digits!", upc.getMessage());
        Exception ean = assertThrows(Exception.class, () -> new Barcode(Barcode.EAN_13, "0123456789012"));
        assertEquals("EAN-13 barcodes must have exactly 12 digits!", ean.getMessage());
    }
}
