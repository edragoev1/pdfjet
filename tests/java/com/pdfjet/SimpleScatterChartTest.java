/*
 * SimpleScatterChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.io.ByteArrayOutputStream;
import java.util.Arrays;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

class SimpleScatterChartTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static SimpleScatterChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new SimpleScatterChart(font, font).setSize(400f, 300f).setLocation(50f, 50f)
                .setTitle("Phones and internet").setSubtitle("by country")
                .setXAxisTitle("phones").setYAxisTitle("internet")
                .add(0.1f, 5f).add(0.5f, 30f).add("Canada", 0.5f, 65f).add(1.2f, 70f);
    }

    @Test
    void theTrendIsTheLeastSquaresLine() {
        double[] line = SimpleScatterChart.trend(Arrays.asList(new SimpleScatterChart.Point(0f, 1f, ""),
                new SimpleScatterChart.Point(1f, 3f, ""), new SimpleScatterChart.Point(2f, 5f, ""),
                new SimpleScatterChart.Point(3f, 7f, "")));
        assertNotNull(line);
        assertEquals(2.0, line[0], 1e-9);
        assertEquals(1.0, line[1], 1e-9);
        assertNull(SimpleScatterChart.trend(Arrays.asList(new SimpleScatterChart.Point(1f, 1f, ""),
                new SimpleScatterChart.Point(1f, 2f, ""))));    // points at one x
        assertNull(SimpleScatterChart.trend(Arrays.asList(new SimpleScatterChart.Point(1f, 1f, ""))));
    }

    @Test
    void aNamedPointIsHighlighted() throws Exception {
        for (boolean named : new boolean[] {true, false}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.helvetica(pdf);
            SimpleScatterChart chart = new SimpleScatterChart(font, font).setHighlightColor(Color.red)
                    .add(1f, 1f).add(2f, 3f);
            if (named) {
                chart.add("Canada", 1.5f, 2f);
            }
            chart.drawOn(page);
            String content = TestSupport.content(page);
            assertEquals(named, content.contains("1 0 0 rg"));
            assertEquals(named, content.contains(TestSupport.hex("Canada")));
        }
    }

    @Test
    void describesItselfFromItsData() throws Exception {
        SimpleScatterChart chart = chart(TestSupport.newPDF()).add(Float.NaN, 1f).setTrendLine(true);
        assertEquals("Phones and internet, by country: 4 points, internet against phones; "
                + "Canada 0.5 and 65; the trend rises.", chart.altDescription());
        chart.setAltDescription("Four countries");
        assertEquals("Four countries", chart.altDescription());
    }

    @Test
    void anAxisRangeSetIsUsed() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setYAxisRange(0f, 200f).drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("200")));
    }

    @Test
    void theDotsAreTransparentButInPDFA1() throws Exception {
        for (Compliance level : new Compliance[] {Compliance.PDF_UA_1, Compliance.PDF_A_1B}) {
            TestSupport.allowCoreFonts(true);
            PDF pdf = new PDF(new ByteArrayOutputStream(), level);
            Page page = new Page(pdf, Letter.PORTRAIT);
            chart(pdf).drawOn(page);
            assertEquals(level != Compliance.PDF_A_1B, TestSupport.content(page).contains(" gs"));
        }
    }

    @Test
    void withoutPointsDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        float[] xy = new SimpleScatterChart(font, font).setTitle("Nothing").add(Float.NaN, 1f).drawOn(page);
        assertEquals("", TestSupport.content(page));
        assertEquals(400f, xy[0], TestSupport.DELTA);
        assertEquals(300f, xy[1], TestSupport.DELTA);
    }
}
