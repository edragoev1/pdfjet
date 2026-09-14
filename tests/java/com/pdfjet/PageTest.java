/*
 * PageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

class PageTest {
    @Test
    void aNewPageTracksTheDefaultGraphicsState() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        assertEquals(1f, page.getPenWidth(), 0f);
        TestSupport.assertRGB(0f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
    }

    @Test
    void cmykSettersWriteCmykAndTrackTheRgbOfTheStandard() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setPenColorCMYK(0f, 1f, 1f, 0f);
        page.setBrushColorCMYK(0f, 0f, 0f, 1f);
        assertEquals("0 1 1 0 K\n0 0 0 1 k\n", TestSupport.content(page));
        TestSupport.assertRGB(1f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
    }

    @Test
    void restoreGraphicsStateRestoresTheTrackedState() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setPenColor(0xFF0000);
        page.saveGraphicsState();
        page.setPenWidth(3f);
        page.setPenColor(0x00FF00);
        page.restoreGraphicsState();
        assertEquals(1f, page.getPenWidth(), 0f);
        TestSupport.assertRGB(1f, 0f, 0f, page.getPenColor());
        assertTrue(TestSupport.content(page).endsWith("q\n3 w\n0 1 0 RG\nQ\n"), TestSupport.content(page));
    }

    @Test
    void gettersReturnCopies() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.getPenColor()[0] = 1f;
        page.getBrushColor()[0] = 1f;
        TestSupport.assertRGB(0f, 0f, 0f, page.getPenColor());
        TestSupport.assertRGB(0f, 0f, 0f, page.getBrushColor());
        page.drawLine(0f, 0f, 10f, 10f);
        page.getContent()[0] = 'X';
        assertTrue(TestSupport.content(page).charAt(0) != 'X');
    }

    @Test
    void drawLineWritesAStrokedPathWithTheYFlipped() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.drawLine(10f, 20f, 30f, 40f);
        String content = TestSupport.content(page);
        assertTrue(content.contains("10 772 m\n30 752 l\nS\n"), content);
    }

    @Test
    void aGoToLinkPointsAtItsDestinationOnAnotherPage() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go").setGoToAction("there").setLocation(50f, 50f).drawOn(page1);
        new Rect(10f, 10f, 20f, 20f).setGoToAction("there").drawOn(page1);
        new TextLine(font, "Nowhere").setGoToAction("missing").setLocation(50f, 100f).drawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        page2.addDestination("there", 30f, 100f);
        pdf.complete();
        String file = TestSupport.latin1(bos.toByteArray());
        // The text and the rect link to the destination, 100 points down page 2; the
        // link to a destination no page has is written without a /Dest
        assertEquals(2, file.split("/Dest \\[").length - 1, file);
        assertEquals(2, file.split("/XYZ 30 692 0\\]").length - 1, file);
        assertEquals(3, file.split("/Subtype /Link").length - 1, file);
    }

    @Test
    void aPathWithFewerThanTwoPointsPaintsNothing() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        List<Point> path = new ArrayList<Point>();
        page.drawPath(path, PathOperator.STROKE);
        path.add(new Point(10f, 10f));
        page.drawPath(path, PathOperator.STROKE);
        assertEquals("", TestSupport.content(page));
    }

    @Test
    void aRadioButtonFontSizeLeavesTheFontAlone() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new RadioButton(font, "Yes").setLocation(50f, 50f).setFontSize(20f).drawOn(page);
        assertEquals(12f, font.getSize(), 0f);
        assertTrue(TestSupport.content(page).contains(" 20 Tf\n"), TestSupport.content(page));
    }
}
