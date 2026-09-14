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
        // The default configuration lists it too, for the viewers that do not apply the usage
        assertTrue(pdf.contains("/OFF ["), pdf);
    }

    @Test
    void aVisibleLayerHasTheViewStateOn() throws Exception {
        String pdf = layer(true);
        assertTrue(pdf.contains("/View << /ViewState /ON >>"));
        assertFalse(pdf.contains("/ViewState /OFF"));
        assertFalse(pdf.contains("/OFF ["));
        // Export is off unless setExportable(true) is called.
        assertTrue(pdf.contains("/Export << /ExportState /OFF >>"));
    }

    @Test
    void aGroupWrapsItsContentInMarkedContentAndIsAPageProperty() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        OptionalContentGroup map = new OptionalContentGroup(pdf, "Map").setVisible(true);
        map.add(new Rect(10f, 10f, 20f, 20f)).drawOn(page1);
        OptionalContentGroup notes = new OptionalContentGroup(pdf, "Notes");
        notes.add(new Line(0f, 0f, 10f, 10f)).drawOn(page1);
        String content1 = TestSupport.content(page1);
        assertTrue(content1.contains("/OC /OC1 BDC\n"), content1);
        assertTrue(content1.contains("/OC /OC2 BDC\n"), content1);
        assertEquals(2, content1.split("EMC").length - 1, content1);
        // The same group on a second page is the same object
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        map.drawOn(page2);
        assertTrue(TestSupport.content(page2).contains("/OC /OC1 BDC\n"));
        pdf.complete();
        String file = TestSupport.latin1(bos.toByteArray());
        assertEquals(2, file.split("/Type /OCG").length - 1, "one object per group");
        assertTrue(file.contains("/Properties"), file);
        assertTrue(file.contains("/OC1 "), file);
        assertTrue(file.contains("/OC2 "), file);
        assertTrue(file.contains("/OCGs ["), file);
        assertTrue(file.contains("/Order ["), file);
    }

    @Test
    void clearRemovesTheDrawables() throws Exception {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.newPDF(), "Layer");
        group.add(new Rect(0f, 0f, 1f, 1f)).add(new Line(0f, 0f, 1f, 1f));
        assertEquals(2, group.getComponents().size());
        assertEquals(0, group.clear().getComponents().size());
        assertEquals("Layer", group.getName());
    }

    @Test
    void getComponentsReturnsACopy() throws Exception {
        OptionalContentGroup group = new OptionalContentGroup(TestSupport.newPDF(), "Layer");
        group.add(new Rect(0f, 0f, 1f, 1f));
        group.getComponents().clear();
        assertEquals(1, group.getComponents().size());
    }
}
