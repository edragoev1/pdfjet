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
    void axisLabelsThatWouldMeetAreDrawnAtEverySecondLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a").setHorizontal(true);
        // 16 steps of 5 across about 240 points: 15 points a step, where a
        // label of one decimal, 70.0, needs more
        chart.addSeries("", new float[] {72.6f}).setValueAxisMinMax(0f, 80f, 16).setMinimumFractionDigits(1);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("70.0")) && content.contains(TestSupport.hex("80.0")), content);
        assertFalse(content.contains(TestSupport.hex("75.0")), content);  // would meet 70.0 and 80.0
    }

    @Test
    void axisLabelStepIsTheFewestLinesALabelFits() {
        assertEquals(1, ChartScale.axisLabelStep(30f, 20f, 8));     // fits
        assertEquals(2, ChartScale.axisLabelStep(15f, 20f, 16));
        assertEquals(3, ChartScale.axisLabelStep(10f, 25f, 16));
        assertEquals(4, ChartScale.axisLabelStep(5f, 100f, 4));     // never more than the lines
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
        assertTrue(stacked.contains(TestSupport.hex("75")), stacked);
        // The total of each stack above it
        assertTrue(stacked.contains(TestSupport.hex("50")) && stacked.contains(TestSupport.hex("85")), stacked);
    }

    @Test
    void theDefaultLookIsQuiet() throws Exception {
        // The first series in navy, the grid lines light gray and solid, the
        // text gray or ink, and no black line or text.
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b").setTitle("t").setSubtitle("s");
        chart.addSeries("one", new float[] {20f, 75f}).addSeries("two", new float[] {30f, 10f});
        String content = draw(chart, page);
        assertTrue(content.contains("0.12 0.23 0.37 rg"), content);     // navy
        assertTrue(content.contains("0.44 0.58 0.76 rg"), content);     // steel blue
        assertTrue(content.contains("0.88 0.89 0.91 RG\n0.5 w\n[] 0 d"), content);
        assertFalse(content.contains("0 0 0 RG") || content.contains("0 0 0 rg"), content);
        assertEquals("0.13 0.15 0.17 rg", TestSupport.fillColorBefore(content, "t"));
        assertEquals("0.37 0.41 0.45 rg", TestSupport.fillColorBefore(content, "s"));
        // A label on a dark bar is white, on a light one ink: whichever
        // contrasts more. On sage and on the old sage, which a rule by
        // brightness gave white, ink contrasts more; on slate white.
        assertEquals(Color.white, BarChart.labelColorOn(0x1F3A5F));
        assertEquals(0x22262B, BarChart.labelColorOn(0x6F95C2));
        assertEquals(0x22262B, BarChart.labelColorOn(0x76987C));
        assertEquals(0x22262B, BarChart.labelColorOn(0x6B8F71));
        assertEquals(Color.white, BarChart.labelColorOn(0x4F7A8C));
    }

    @Test
    void barsHaveTheirOwnColorsAndLabelsInsideWithGroupedDigits() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b").setHorizontal(true).setSubtitle("sub");
        chart.addSeries("rivers", new float[] {6650f, 12f}, new int[] {Color.red, Color.blue});
        chart.setValueAxisMinMax(0f, 8000f, 4).setDrawValueLabels(true).setValueLabelsInside(true);
        chart.setGroupingUsed(true).setAxisLineWidth(0f).setGridLineColor(Color.lightgray);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("6,650")), content);
        assertTrue(content.contains(TestSupport.hex("8,000")), content);
        assertTrue(content.contains(TestSupport.hex("sub")), content);
        assertTrue(content.contains("1 0 0 rg"), content);     // the first bar
        assertTrue(content.contains("0 0 1 rg"), content);     // the second bar
        assertEquals("1 1 1 rg", TestSupport.fillColorBefore(content, "6,650"));  // inside the first bar
        assertEquals("0.37 0.41 0.45 rg", TestSupport.fillColorBefore(content, "sub"));
        assertTrue(content.contains(TestSupport.hex("12")), content);  // too short: next to the bar
    }

    @Test
    void aChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart titled = chart(pdf).setTitle("Sales").setCategories("a", "b");
        titled.addSeries("", new float[] {1f, 2f});
        String content = draw(titled, page);
        assertTrue(content.startsWith("/Figure <</MCID 0>>\nBDC\n"), content);
        assertTrue(content.endsWith("EMC\n"), content);
        BarChart described = chart(pdf).setTitle("Sales").setAltDescription("Sales rose from 1 to 2.")
                .setCategories("a", "b");
        described.addSeries("", new float[] {1f, 2f});
        draw(described, page);
        assertEquals("Sales", page.structures.get(0).altDescription);
        assertEquals("Sales rose from 1 to 2.", page.structures.get(1).altDescription);
    }

    // A horizontal chart narrower than its categories draws nothing: its
    // axis ran backwards, the bars over the categories (review of 26 October
    // 2026).
    @Test
    void aChartWithNoRoomForItsPlotDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setSize(200f, 200f).setHorizontal(true)
                .setCategories("A very very long category name indeed, and longer")
                .addSeries("s", new float[] {5f});
        TestSupport.assertXY(250f, 250f, chart.drawOn(page));
        assertEquals("", TestSupport.content(page));
    }

    // Under vertical bars a category is written where it leaves the one
    // before it room, as the numbers of the axis are: twelve months in full
    // ran into each other.
    @Test
    void categoriesThatDoNotFitAreSkipped() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String[] months = {"January", "February", "March", "April", "May", "June", "July",
                "August", "September", "October", "November", "December"};
        float[] values = new float[12];
        java.util.Arrays.fill(values, 1f);
        String content = draw(chart(pdf).setCategories(months).addSeries("s", values), page);
        assertTrue(content.contains(TestSupport.hex("January")), content);
        assertFalse(content.contains(TestSupport.hex("February")), content);
        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a", "b").addSeries("s", new float[] {1f, 2f}), page);
        assertTrue(content.contains(TestSupport.hex("a")) && content.contains(TestSupport.hex("b")), content);
    }

    // A null title, name or list is empty: it threw when the chart was drawn.
    @Test
    void nullIsEmpty() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setTitle(null).setSubtitle(null).setXAxisTitle(null).setYAxisTitle(null)
                .setAltDescription(null).setCategories((String[]) null).setCategories("a")
                .addSeries(null, new float[] {1f}).addSeries("b", null).addSeries("c", new float[] {2f}, (int[]) null)
                .drawOn(page);
    }
}
