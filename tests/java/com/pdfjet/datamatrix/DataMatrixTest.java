/*
 * DataMatrixTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.datamatrix;

import static org.junit.jupiter.api.Assertions.assertEquals;

import com.pdfjet.Letter;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import org.junit.jupiter.api.Test;

class DataMatrixTest {
    @Test
    void sixDigitsFitTheSmallestSquareWithItsFinderPattern() {
        boolean[][] modules = new DataMatrix("123456").getModules();
        assertEquals(10, modules.length);
        assertEquals(10, modules[0].length);
        for (int i = 0; i < 10; i++) {
            assertEquals(i % 2 == 0, modules[0][i], "top row alternates");
            assertEquals(true, modules[9][i], "bottom row is solid");
            assertEquals(true, modules[i][0], "left column is solid");
        }
    }

    @Test
    void longerDataGetsALargerSymbol() {
        assertEquals(32, new DataMatrix(new String(new char[60]).replace('\0', 'Z')).getModules().length);
    }

    @Test
    void theRectangleShapeIsWiderThanTall() {
        boolean[][] modules = new DataMatrix("Hello, World!", DataMatrix.RECTANGLE).getModules();
        assertEquals(12, modules.length);
        assertEquals(26, modules[0].length);
    }

    @Test
    void drawOnReturnsTheCornerOfTheModules() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("123456").setLocation(5f, 5f).setModuleLength(3f);
        TestSupport.assertXY(35f, 35f, dm.drawOn(page));
    }
}
