/*
 * PageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
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
    void aColorOrPenWidthThatIsSetAlreadyIsNotWrittenAgain() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setBrushColor(Color.black);    // Written, as a new page has written no color
        page.setBrushColor(Color.black);
        page.setPenColor(Color.red);
        page.setPenColor(new float[] {1f, 0f, 0f});
        page.setPenWidth(0f);
        page.setDefaultPenWidth();
        assertEquals("0 0 0 rg\n1 0 0 RG\n0 w\n", TestSupport.content(page));
    }

    @Test
    void qAndQKeepWhatTheContentHasWritten() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setBrushColor(Color.black);
        page.saveGraphicsState();
        page.setBrushColor(Color.blue);
        page.setBrushColor(Color.blue);
        page.restoreGraphicsState();
        page.setBrushColor(Color.black);    // Q restored black
        page.setBrushColor(Color.blue);
        assertEquals("0 0 0 rg\nq\n0 0 1 rg\nQ\n0 0 1 rg\n", TestSupport.content(page));
    }

    @Test
    void anRgbColorAfterACmykColorIsWritten() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setBrushColor(Color.black);
        page.setBrushColorCMYK(0f, 0f, 0f, 1f);
        page.setBrushColor(Color.black);
        assertEquals("0 0 0 rg\n0 0 0 1 k\n0 0 0 rg\n", TestSupport.content(page));
    }

    @Test
    void theFontOfTheTextIsWrittenWhenItChanges() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "a").setLocation(10f, 20f).drawOn(page);
        new TextLine(font, "b").setLocation(10f, 40f).drawOn(page);
        new TextLine(font, "c").setFontSize(14f).setLocation(10f, 60f).drawOn(page);
        page.saveGraphicsState();
        new TextLine(font, "d").setLocation(10f, 80f).drawOn(page);
        page.restoreGraphicsState();
        new TextLine(font, "e").setFontSize(14f).setLocation(10f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        List<String> fonts = new ArrayList<String>();
        for (String line : content.split("\n")) {
            if (line.endsWith(" Tf")) {
                fonts.add(line.substring(line.indexOf(' ') + 1));
            }
        }
        assertEquals(Arrays.asList("12 Tf", "14 Tf", "12 Tf"), fonts, content);
    }

    @Test
    void fillRectWritesOneRectangleWithTheEdgesOfThePath() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.fillRect(10f, 20f, 30f, 40f);
        assertEquals("10 732 30 40 re\nf\n", TestSupport.content(page));

        // A path wrote the top edge at 792 and the bottom one at 791.99, where
        // rounding the height alone would make it 0.
        page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.fillRect(0f, 0.004f, 1f, 0.004f);
        assertEquals("0 791.99 1 0.01 re\nf\n", TestSupport.content(page));

        // Far outside the page the rectangle is still a path.
        page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.fillRect(200000f, 0f, 10f, 10f);
        assertEquals("200000 792 m\n200010 792 l\n200010 782 l\n200000 782 l\nf\n", TestSupport.content(page));
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
    void appendWritesStringsInUtf8AndIntegersAndNumbersInDecimal() throws Exception {
        StringBuilder euros = new StringBuilder();
        for (int i = 0; i < 256; i++) {
            euros.append('€');
        }
        StringBuilder digits = new StringBuilder();     // Longer than the page's first buffer
        for (int i = 0; i < 1000; i++) {
            digits.append("0123456789");
        }
        digits.append('é');
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.append("BT é≠😀 ");
        page.append(-2147483648);
        page.append(' ');
        page.append(2147483647);
        page.append(' ');
        page.append(-8388607.5f);
        page.append(' ');
        page.append(0.125f);
        page.append(' ');
        page.append(euros.toString());
        page.append(' ');
        page.append(digits.toString());
        String expected = "BT é≠😀 -2147483648 2147483647 -8388607.5 0.13 " + euros + " " + digits;
        assertArrayEquals(expected.getBytes(StandardCharsets.UTF_8), page.getContent());
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
    void aTextLineAddsItsDestinationWhenItIsDrawn() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page1 = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Go").setGoToAction("there").setLocation(50f, 50f).drawOn(page1);
        Page page2 = new Page(pdf, Letter.PORTRAIT);
        TextLine target = new TextLine(font, "There").setDestination("there");
        target.setLocation(30f, 100f + font.getSize());
        target.drawOn(page2);
        pdf.complete();
        String file = TestSupport.latin1(bos.toByteArray());
        // The destination is at the left edge of page 2, a font size above the baseline.
        assertEquals("there", target.getDestination());
        assertEquals(1, file.split("/Dest \\[").length - 1, file);
        assertEquals(1, file.split("/XYZ 0 692 0\\]").length - 1, file);
    }

    @Test
    void positiveAnglesTurnClockwise() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        // y grows downward, so text turned a quarter clockwise runs down the page
        // and text turned a quarter counterclockwise runs up from its location.
        float[] down = new TextLine(font, "Down").setTextRotation(90).setLocation(100f, 100f).drawOn(page);
        float[] up = new TextLine(font, "Up").setTextRotation(-90).setLocation(300f, 100f).drawOn(page);
        assertTrue(down[1] > 100f + font.stringWidth("Down") / 2f, "down " + down[1]);
        assertEquals(100f, up[1], 0.01f);
        page.setRotation(90);
        pdf.complete();
        String file = TestSupport.latin1(bos.toByteArray());
        assertTrue(file.contains("/Rotate 90"), "no /Rotate 90");
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
    void theShapesAreDrawnOrFilled() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.drawCircle(50f, 50f, 10f);
        page.fillCircle(50f, 50f, 10f);
        page.drawRoundedRect(10f, 10f, 100f, 50f, 5f, 5f);
        page.fillRoundedRect(10f, 10f, 100f, 50f, 5f, 5f);
        // A drawn shape is stroked with S, and a filled one filled with f.
        List<String> painted = new ArrayList<String>();
        for (String token : TestSupport.content(page).split("\\s+")) {
            if (token.equals("S") || token.equals("f")) {
                painted.add(token);
            }
        }
        assertEquals(Arrays.asList("S", "f", "S", "f"), painted);
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

    @Test
    void importedContentIsSeparatedFromTheOperatorAfterIt() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.drawContents("BT ET".getBytes("ISO-8859-1"), 100f, 0f, 0f, 1f, 1f);
        assertTrue(TestSupport.content(page).contains("BT ET\nQ\n"), TestSupport.content(page));
    }
}
