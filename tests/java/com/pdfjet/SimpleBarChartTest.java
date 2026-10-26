/*
 * SimpleBarChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

class SimpleBarChartTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static SimpleBarChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new SimpleBarChart(font, font).setWidth(330f).setLocation(50f, 50f)
                .setTitle("Statement, 12 pages").setSubtitle("warm ms")
                .add("PDFjet", 7.3f).add("iText", 72.6f).add("OpenPDF", 18.4f).add("PDFBox", 44.1f);
    }

    // The widths of the rectangles filled on the page, in the order they were drawn.
    private static List<Float> rectWidths(String content) {
        List<Float> widths = new ArrayList<Float>();
        Matcher m = Pattern.compile("([-\\d.]+) ([-\\d.]+) ([-\\d.]+) ([-\\d.]+) re").matcher(content);
        while (m.find()) {
            widths.add(Float.parseFloat(m.group(3)));
        }
        return widths;
    }

    @Test
    void eachBarIsItsShareOfTheLargestValue() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).drawOn(page);
        List<Float> widths = rectWidths(TestSupport.content(page));
        assertEquals(5, widths.size(), "the panel and 4 bars: " + widths);
        assertEquals(330f, widths.get(0), TestSupport.DELTA);
        float longest = widths.get(2);      // iText's 72.6
        assertEquals(longest * 7.3f / 72.6f, widths.get(1), 0.05f);
        assertEquals(longest * 18.4f / 72.6f, widths.get(3), 0.05f);
        assertEquals(longest * 44.1f / 72.6f, widths.get(4), 0.05f);
    }

    @Test
    void theHighlightedBarHasItsOwnColor() throws Exception {
        for (boolean highlighted : new boolean[] {true, false}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            SimpleBarChart chart = chart(pdf).setHighlightColor(Color.red);
            if (highlighted) {
                chart.setHighlight(0);
            }
            chart.drawOn(page);
            assertEquals(highlighted, TestSupport.content(page).contains("1 0 0 rg"));
        }
    }

    @Test
    void describesItselfFromItsValues() throws Exception {
        SimpleBarChart chart = chart(TestSupport.newPDF()).setFractionDigits(1);
        assertEquals("Statement, 12 pages, warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1.",
                chart.altDescription());
        chart.setAltDescription("Four libraries");
        assertEquals("Four libraries", chart.altDescription());
    }

    @Test
    void valuesHaveTheDigitsTheyNeedUnlessSet() throws Exception {
        SimpleBarChart chart = chart(TestSupport.newPDF());
        assertEquals("7.25", chart.label(7.25f));
        assertEquals("72", chart.label(72f));
        chart.setFractionDigits(1);
        assertEquals("72.0", chart.label(72f));
    }

    @Test
    void isAsTallAsItsRows() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart chart = chart(pdf);
        float h = chart.getHeight();
        float[] xy = chart.drawOn(page);
        assertEquals(380f, xy[0], TestSupport.DELTA);
        assertEquals(50f + h, xy[1], TestSupport.DELTA);
        SimpleBarChart taller = chart(pdf).add("Another", 10f);
        assertEquals(h + chart.rowHeight(), taller.getHeight(), TestSupport.DELTA);
    }

    @Test
    void aNegativeValueHasNoBarButItsValue() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new SimpleBarChart(font, font).add("up", 5f).add("down", -2f).drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(2, rectWidths(content).size(), "the panel and one bar");
        assertTrue(content.contains(TestSupport.hex("-2")), content);
    }

    @Test
    void withoutBarsDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new SimpleBarChart(font, font).setTitle("Nothing").drawOn(page);
        assertEquals("", TestSupport.content(page));
    }
}
