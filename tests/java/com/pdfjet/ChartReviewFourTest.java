/*
 * ChartReviewFourTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.Test;

/**
 * The findings of the fourth read-only review of the charts, of 26 October
 * 2026.
 */
class ChartReviewFourTest {
    private static int count(String content, String text) {
        int count = 0;
        for (int i = content.indexOf(text); i >= 0; i = content.indexOf(text, i + 1)) {
            count++;
        }
        return count;
    }

    // The right end of the text, drawn in the font at its size.
    private static float endOf(String content, Font font, String text) {
        return TestSupport.positionOf(content, text)[0] + font.stringWidth(text);
    }

    @Test
    void barChartAxisKeepsTheDigitsOfItsStepWithNoFractionDigits() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 300f)
                .setCategories("a", "b").setFractionDigits(0);
        chart.addSeries("", new float[] {1f, 2f});
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("<" + TestSupport.hex("0.25") + ">"), content);    // Not 0, 0, 1, 1

        page = new Page(pdf, Letter.PORTRAIT);
        chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 300f).setCategories("a", "b");
        chart.addSeries("", new float[] {0.002f, 0.004f});
        chart.drawOn(page);
        content = TestSupport.content(page);
        assertTrue(content.contains("<" + TestSupport.hex("0.0020") + ">"), content);     // The digits of 0.0005
        assertFalse(content.contains("<" + TestSupport.hex("0.00") + ">"), content);
    }

    @Test
    void barChartOfASpanPastTheLargestFloatDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f).setCategories("a", "b");
        chart.addSeries("", new float[] {-3e38f, 3e38f});
        TestSupport.assertXY(350f, 250f, chart.drawOn(page));
        assertEquals(0, page.getContent().length);
    }

    @Test
    void dataAtTheLargestFloatHasAnAxisOfItsOwn() {
        ChartScale.Axis axis = ChartScale.axisOf(new float[] {Float.MAX_VALUE}, 0f, 0f, false);
        assertTrue(ChartScale.drawable(axis));
        assertTrue(axis.min > 1e38f && axis.max == Float.MAX_VALUE, axis.min + " to " + axis.max);
        float[] flat = ChartScale.flatRange(5f);
        assertEquals(5f, flat[0]);
        assertEquals(6f, flat[1]);
    }

    @Test
    void groupDigitsLeavesWordsAsTheyAre() {
        assertEquals("Infinity", ChartScale.groupDigits("Infinity"));
        assertEquals("-Infinity", ChartScale.groupDigits("-Infinity"));
        assertEquals("NaN", ChartScale.groupDigits("NaN"));
        assertEquals("3e38", ChartScale.groupDigits("3e38"));
        assertEquals("-1,234,567.25", ChartScale.groupDigits("-1234567.25"));
    }

    @Test
    void simpleBarChartWritesNoValueThatIsNotANumber() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        SimpleBarChart chart = new SimpleBarChart(font, font).setTitle("Sizes")
                .add("one", 1200f).add("two", Float.POSITIVE_INFINITY).add("three", Float.NaN);
        assertEquals("", chart.label(Float.POSITIVE_INFINITY));
        assertEquals("", chart.label(Float.NaN));
        assertEquals("Sizes: one 1,200.", chart.altDescription());
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex("fin")), content);
        assertFalse(content.contains(TestSupport.hex("NaN")), content);
    }

    @Test
    void stepDigitsGoPastSix() {
        assertEquals(9, ChartScale.stepDigits(2.5e-8f));
        assertEquals(9, ChartScale.digitsOf(2.5e-8f));
        assertEquals(2, ChartScale.digitsOf(0.25f));
        assertEquals(2, ChartScale.digitsOf(10f));
        assertEquals(6, ChartScale.stepDigits(1f / 3f));
        assertEquals(2, ChartScale.digitsOf(Float.NaN));
    }

    @Test
    void lineChartCategoriesStayInsideTheChart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .setCategories("January 2026", "February 2026", "March 2026 and on")
                .addLine("a", new float[] {1f, 2f, 3f}).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(TestSupport.positionOf(content, "January 2026")[0] >= 50f, content);
        assertTrue(endOf(content, font, "March 2026 and on") <= 350f, content);
    }

    @Test
    void scatterChartNumbersStayInsideTheChart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setLocation(50f, 50f).setSize(400f, 200f)
                .add(0f, 1f).add(1000000f, 5f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(endOf(content, font, "1,000,000") <= 450f, content);
    }

    @Test
    void barChartCategoriesStayInsideTheChart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .setCategories("a", "b", "c", "d", "e", "f", "the last one");
        chart.addSeries("", new float[] {1f, 2f, 3f, 4f, 5f, 6f, 7f});
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(endOf(content, font, "the last one") <= 350f, content);
    }

    @Test
    void scatterDescriptionTellsWhatIsDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // A trend line that misses the plot is not drawn, nor described
        SimpleScatterChart chart = new SimpleScatterChart(font, font).setTitle("Flat")
                .add(1f, 1f).add(2f, 1f).add(3f, 1f).setTrendLine(true);
        assertEquals("Flat: 3 points; the trend is flat.", chart.altDescription());
        chart.setYAxisRange(100f, 200f);
        assertEquals("Flat: 0 points; 3 points outside the range.", chart.altDescription());
        // The points outside a range that is set are counted apart
        chart = new SimpleScatterChart(font, font).setTitle("Some")
                .add(1f, 5f).add(2f, 6f).add("far", 3f, 50f).setYAxisRange(0f, 10f);
        assertEquals("Some: 2 points; 1 point outside the range.", chart.altDescription());
    }

    @Test
    void lineChartDescriptionOfOneCategory() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        SimpleLineChart chart = new SimpleLineChart(font, font).setTitle("Sales")
                .setCategories("2019").addLine("a", new float[] {4f});
        assertTrue(chart.altDescription().startsWith("Sales, 2019: a 4."), chart.altDescription());
        chart.setCategories("2019", "2020", "");
        assertTrue(chart.altDescription().startsWith("Sales, 2019 to 2020: a 4."), chart.altDescription());
    }

    @Test
    void lineChartDrawsAValueAloneBetweenGaps() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page joined = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .addLine("a", new float[] {1f, 2f, 3f}).drawOn(joined);
        Page alone = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .addLine("a", new float[] {1f, Float.NaN, 3f}).drawOn(alone);
        assertEquals(0, count(TestSupport.content(joined), " c\n"));
        assertTrue(count(TestSupport.content(alone), " c\n") > 0);   // The dots of the two values
    }

    @Test
    void barChartMakesNoRoomForLabelsItDoesNotWrite() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        float[] at = new float[2];
        float[][] values = {{5f, 7f}, {5f, -1000000f}};     // The second wholly outside
        for (int k = 0; k < 2; k++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                    .setCategories("a", "b").setHorizontal(true).setValueLabels(true).setValueAxisRange(0f, 10f);
            chart.addSeries("", values[k]);
            chart.drawOn(page);
            at[k] = TestSupport.positionOf(TestSupport.content(page), "10")[0];
        }
        assertEquals(at[0], at[1], 0.01f);
    }
}
