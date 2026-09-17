/*
 * QRCodeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.qrcode;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import com.pdfjet.Letter;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import org.junit.jupiter.api.Test;

class QRCodeTest {
    private static String repeat(char ch, int count) {
        return new String(new char[count]).replace('\0', ch);
    }

    private static boolean dark(Boolean[][] modules, int row, int column) {
        return Boolean.TRUE.equals(modules[row][column]);
    }

    @Test
    void shortDataKeepsTheSymbolAt33ModulesAtEveryLevel() throws Exception {
        for (ErrorCorrectionLevel level : ErrorCorrectionLevel.values()) {
            Boolean[][] modules = new QRCode("Hello", level).getModules();
            assertEquals(33, modules.length, level.toString());
            assertEquals(33, modules[0].length, level.toString());
        }
    }

    @Test
    void longerDataMakesALargerSymbol() throws Exception {
        assertEquals(33, new QRCode(repeat('a', 78), ErrorCorrectionLevel.L).getModules().length);     // version 4
        assertEquals(37, new QRCode(repeat('a', 79), ErrorCorrectionLevel.L).getModules().length);     // version 5
        assertEquals(177, new QRCode(repeat('a', 2953), ErrorCorrectionLevel.L).getModules().length);  // version 40
        assertEquals(177, new QRCode(repeat('a', 1273), ErrorCorrectionLevel.H).getModules().length);  // version 40
    }

    @Test
    void versionsFrom7CarryTheirVersionNumber() throws Exception {
        // 120 bytes at level M need version 7, 45 modules, whose version information is 0x07C94.
        Boolean[][] modules = new QRCode(repeat('a', 120), ErrorCorrectionLevel.M).getModules();
        assertEquals(45, modules.length);
        for (int i = 0; i < 18; i++) {
            boolean bit = ((0x07C94 >> i) & 1) == 1;
            assertEquals(bit, dark(modules, i / 3, i % 3 + 45 - 11), "top right, bit " + i);
            assertEquals(bit, dark(modules, i % 3 + 45 - 11, i / 3), "bottom left, bit " + i);
        }
    }

    @Test
    void finderPatternsAreInThreeCorners() throws Exception {
        Boolean[][] modules = new QRCode("Hello", ErrorCorrectionLevel.L).getModules();
        for (int i = 0; i < 7; i++) {
            assertEquals(true, dark(modules, 0, i), "top left, top row");
            assertEquals(true, dark(modules, 0, 32 - i), "top right, top row");
            assertEquals(true, dark(modules, 32, i), "bottom left, bottom row");
            assertEquals(true, dark(modules, i, 0), "top left, left column");
        }
        assertEquals(false, dark(modules, 0, 7), "separator");
        assertEquals(false, dark(modules, 7, 0), "separator");
    }

    @Test
    void dataThatDoesNotFitVersion40Throws() throws Exception {
        assertThrows(IllegalArgumentException.class, () -> new QRCode(repeat('a', 2954), ErrorCorrectionLevel.L));
        assertThrows(IllegalArgumentException.class, () -> new QRCode(repeat('a', 1274), ErrorCorrectionLevel.H));
    }

    @Test
    void drawOnReturnsTheSameCornerEveryTime() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        QRCode qr = new QRCode("Hello", ErrorCorrectionLevel.L).setLocation(10f, 10f).setModuleLength(2f);
        TestSupport.assertXY(76f, 76f, qr.drawOn(page));
        TestSupport.assertXY(76f, 76f, qr.drawOn(page));
    }
}
