/*
 * DefaultWidthTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.Collections;
import org.junit.jupiter.api.Test;

class DefaultWidthTest {
    // Every line PDFjet draws by default is 0.5 points wide (9.1.9), as Word's
    // table borders and TextBlock's and TextFrame's are: 0, the thinnest line
    // of the device, is a pixel on a screen and faint or gone in print, and the
    // PDF standard advises against it (ISO 32000, 8.4.3.2). 0 is still there
    // when set.
    @Test
    void everyLineIsHalfAPointUnlessSet() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        assertEquals(0.5f, new Cell(font, "x").getBorderWidth(), "a cell's border");
        // The width a rect, a line and a path draw with, on pages of their own
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Rect(0f, 0f, 10f, 10f).setBorderColor(Color.black).drawOn(page);
        assertTrue(TestSupport.content(page).contains("0.5 w\n"), "a rect's border: " + TestSupport.content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        new Line(0f, 0f, 10f, 10f).drawOn(page);
        assertTrue(TestSupport.content(page).contains("0.5 w\n"), "a line: " + TestSupport.content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        new Path().add(new Point(0f, 0f)).add(new Point(10f, 10f)).drawOn(page);
        assertTrue(TestSupport.content(page).contains("0.5 w\n"), "a path: " + TestSupport.content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        page.setDefaultPenWidth();
        assertTrue(TestSupport.content(page).contains("0.5 w\n"), "the default pen: " + TestSupport.content(page));

        BigTable table = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(1);
        table.setTableData(new String[] {"Name"}, Collections.singletonList(new String[] {"a"}));
        table.setLocation(20f, 20f);
        table.complete();
        String content = TestSupport.content(table.getPages().get(0));
        assertTrue(content.contains("0.5 w\n"), "a big table's lines: " + content);
        assertFalse(content.contains("\n0 w\n"), "a big table's lines: " + content);
        // Set to 0, the thinnest line, as before
        BigTable zero = new BigTable(pdf, font, font, Letter.PORTRAIT).setNumberOfColumns(1).setBorderWidth(0f);
        zero.setTableData(new String[] {"Name"}, Collections.singletonList(new String[] {"a"}));
        zero.setLocation(20f, 20f);
        zero.complete();
        content = TestSupport.content(zero.getPages().get(0));
        assertTrue(content.contains("0 w\n"), "a big table set to 0: " + content);
    }
}
