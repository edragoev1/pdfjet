/*
 * OptionalContentGroupTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import org.junit.jupiter.api.Test;

class OptionalContentGroupTest {
    private static String layer(boolean visible) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        OptionalContentGroup group = new OptionalContentGroup(pdf, "Layer").setVisible(visible).setPrintable(true);
        group.add(new Rect(10f, 10f, 20f, 20f));
        TestSupport.assertXY(30f, 30f, group.drawOn(page));
        pdf.complete();
        return TestSupport.latin1(bos.toByteArray());
    }

    @Test
    void aHiddenLayerHasTheViewStateOff() throws Exception {
        String pdf = layer(false);
        assertTrue(pdf.contains("/OCProperties"));
        assertTrue(pdf.contains("/View << /ViewState /OFF >>"));
        assertTrue(pdf.contains("/Print << /PrintState /ON >>"));
    }

    @Test
    void aVisibleLayerHasTheViewStateOn() throws Exception {
        String pdf = layer(true);
        assertTrue(pdf.contains("/View << /ViewState /ON >>"));
        assertFalse(pdf.contains("/ViewState /OFF"));
        // Export is off unless setExportable(true) is called.
        assertTrue(pdf.contains("/Export << /ExportState /OFF >>"));
    }

    @Test
    void clearRemovesTheDrawables() throws Exception {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.newPDF(), "Layer");
        group.add(new Rect(0f, 0f, 1f, 1f)).add(new Line(0f, 0f, 1f, 1f));
        assertEquals(2, group.getComponents().size());
        assertEquals(0, group.clear().getComponents().size());
        assertEquals("Layer", group.getName());
    }
}
