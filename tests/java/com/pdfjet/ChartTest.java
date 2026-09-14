/*
 * ChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import org.junit.jupiter.api.Test;

class ChartTest {
    private static List<List<Point>> series(float... ys) {
        List<Point> points = new ArrayList<Point>();
        for (int i = 0; i < ys.length; i++) {
            points.add(new Point(i + 1f, ys[i]));
        }
        List<List<Point>> data = new ArrayList<List<Point>>();
        data.add(points);
        return data;
    }

    private static String draw(List<List<Point>> data) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new Chart(font, font).setLocation(50f, 50f).setSize(300f, 200f).setData(data).drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aChartWithoutPointsDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        Chart chart = new Chart(font, font).setLocation(50f, 50f).setSize(300f, 200f);
        chart.setData(new ArrayList<List<Point>>());
        TestSupport.assertXY(350f, 250f, chart.drawOn(page));
        assertEquals(0, page.getContent().length);
    }

    @Test
    void allNegativeDataGetsNegativeAxisLabels() throws Exception {
        String content = draw(series(-5f, -2.5f, -1f));
        assertTrue(content.contains(TestSupport.hex("-5.0")), content);
        assertTrue(content.contains(TestSupport.hex("-1.0")), content);
        assertFalse(content.contains("NaN"));
    }

    @Test
    void flatDataIsDrawnWithoutNaN() throws Exception {
        String content = draw(series(3f, 3f, 3f));
        assertFalse(content.contains("NaN"));
        assertTrue(content.contains(TestSupport.hex("3.0")), content);
        assertTrue(content.contains(TestSupport.hex("4.0")), content);
    }

    @Test
    void wholeNumberStepsGetWholeNumberLabels() throws Exception {
        String content = draw(series(10f, 60f, 35f));
        assertTrue(content.contains(TestSupport.hex("60")), content);
        assertFalse(content.contains(TestSupport.hex("60.00")), content);
    }

    @Test
    void aPathSeriesKeepsItsStrokeWidthAndWritesItsText() throws Exception {
        Point p1 = new Point(1f, 2f).setDrawPath(true).setShape(Shape.INVISIBLE);
        p1.setStrokeWidth(20f).setStrokeColor(Color.blue).setText("label");
        Point p2 = new Point(3f, 2f).setShape(Shape.INVISIBLE);
        List<Point> path = new ArrayList<Point>();
        path.add(p1);
        path.add(p2);
        List<List<Point>> data = new ArrayList<List<Point>>();
        data.add(path);
        String content = draw(data);
        assertTrue(content.contains("20 w"), content);
        assertTrue(content.contains(TestSupport.hex("label")), content);
    }

    @Test
    void labelsUseAPeriodWhateverTheDefaultLocale() throws Exception {
        Locale saved = Locale.getDefault();
        Locale.setDefault(Locale.GERMANY);
        try {
            String content = draw(series(1f, 2f, 3f));
            assertTrue(content.contains(TestSupport.hex("1.25")), content);
            assertFalse(content.contains(TestSupport.hex("1,25")));
        } finally {
            Locale.setDefault(saved);
        }
    }
}
