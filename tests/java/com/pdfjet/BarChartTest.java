/*
 * BarChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class BarChartTest {
    private static BarChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f);
    }

    private static String draw(BarChart chart, Page page) throws Exception {
        TestSupport.assertXY(350f, 250f, chart.drawOn(page));
        return TestSupport.content(page);
    }

    @Test
    void aChartWithoutCategoriesDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        draw(chart(pdf), page);
        assertEquals(0, page.getContent().length);
    }

    @Test
    void theValueAxisStartsAtZeroWithWholeNumberLabels() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("abc", "def", "ghi");
        chart.addSeries("", new float[] {20f, 75f, 31f});
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("80")), content);
        assertFalse(content.contains(TestSupport.hex("0.00")), content);
        assertFalse(content.contains(TestSupport.hex("80.00")), content);
        assertTrue(content.contains(TestSupport.hex("ghi")), content);
    }

    @Test
    void theLegendListsTheNamedSeriesInTheirColors() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a");
        chart.addSeries("first", new float[] {1f}, Color.red);
        chart.addSeries("", new float[] {2f}, Color.blue);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("first")), content);
        assertTrue(content.contains("1 0 0 rg"), content);
        assertTrue(content.contains("0 0 1 rg"), content);
    }

    @Test
    void valueLabelsAreWrittenWithTheFractionDigitsSet() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b").setHorizontal(true);
        chart.addSeries("", new float[] {2.5f, -1f}).setDrawValueLabels(true);
        chart.setMinimumFractionDigits(1);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("2.5")), content);
        assertTrue(content.contains(TestSupport.hex("-1.0")), content);
    }

    @Test
    void aManualAxisRangeIsUsedAsGiven() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a");
        chart.addSeries("", new float[] {5f}).setValueAxisMinMax(0f, 12f, 4);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("12")), content);
        assertTrue(content.contains(TestSupport.hex("3")), content);
        assertFalse(content.contains("NaN"));
    }

    @Test
    void stackedBarsUseTheSumsOfTheCategoriesForTheValueAxis() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b");
        chart.addSeries("", new float[] {20f, 75f}).addSeries("", new float[] {30f, 10f});
        String grouped = draw(chart, page);
        assertFalse(grouped.contains(TestSupport.hex("90")), grouped);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.setStacked(true).setDrawValueLabels(true);
        String stacked = draw(chart, page);
        assertTrue(stacked.contains(TestSupport.hex("90")), stacked);
        assertTrue(stacked.contains(TestSupport.hex("75")), stacked);
    }

    @Test
    void barsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b").setHorizontal(true).setSubtitle("sub");
        chart.addSeries("", new float[] {6650f, 12f}, new int[] {Color.red, Color.blue});
        chart.setValueAxisMinMax(0f, 8000f, 4).setDrawValueLabels(true).setValueLabelsInside(true);
        chart.setGroupingUsed(true).setAxisLineWidth(0f).setGridLineColor(Color.lightgray);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("6,650")), content);
        assertTrue(content.contains(TestSupport.hex("8,000")), content);
        assertTrue(content.contains(TestSupport.hex("sub")), content);
        assertTrue(content.contains("1 0 0 rg"), content);     // the first bar
        assertTrue(content.contains("0 0 1 rg"), content);     // the second bar
        assertTrue(content.contains("1 1 1 rg"), content);     // the label inside the first bar
        assertTrue(content.contains(TestSupport.hex("12")), content);  // too short: next to the bar
    }
}
