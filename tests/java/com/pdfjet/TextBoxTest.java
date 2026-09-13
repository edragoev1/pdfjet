/*
 * TextBoxTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class TextBoxTest {
    @Test
    void measuringDoesNotFixTheHeight() throws Exception {
        TextBox box = new TextBox(TestSupport.helvetica(TestSupport.newPDF()),
                "one two three four five six seven eight nine ten");
        box.setLocation(0f, 0f);
        box.setWidth(60f);
        TestSupport.assertXY(60f, 83.232f, box.drawOn(null));
        assertEquals(83.232f, box.getHeight(), TestSupport.DELTA);

        box.setText("one two three four five six seven eight nine ten eleven twelve thirteen fourteen");
        TestSupport.assertXY(60f, 124.848f, box.drawOn(null));
        assertEquals(124.848f, box.getHeight(), TestSupport.DELTA);
    }

    @Test
    void bordersAreOffByDefaultAndCanBeRemovedOneByOne() throws Exception {
        TextBox box = new TextBox(TestSupport.helvetica(TestSupport.newPDF()), "x");
        assertFalse(box.getBorder(Border.TOP));
        box.setBorders(true);
        box.setBorder(Border.TOP, false);
        assertFalse(box.getBorder(Border.TOP));
        assertTrue(box.getBorder(Border.LEFT));
        assertTrue(box.getBorder(Border.RIGHT));
        assertTrue(box.getBorder(Border.BOTTOM));
    }

    @Test
    void colorGettersReturnCopies() throws Exception {
        TextBox box = new TextBox(TestSupport.helvetica(TestSupport.newPDF()), "x");
        box.setTextColor(0x0000FF);
        box.getTextColor()[2] = 0f;
        TestSupport.assertRGB(0f, 0f, 1f, box.getTextColor());
        box.setBorderColor(0xFF0000);
        TestSupport.assertRGB(1f, 0f, 0f, box.getBorderColor());
    }
}
