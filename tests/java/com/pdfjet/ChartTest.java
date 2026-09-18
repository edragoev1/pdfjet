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

import java.util.Locale;
import org.junit.jupiter.api.Test;

class ChartTest {
    private static Chart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new Chart(font, font).setLocation(50f, 50f).setSize(300f, 200f);
    }

    private static String draw(float... ys) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        Series series = chart.addSeries("");
        for (int i = 0; i < ys.length; i++) {
            series.addPoint(i + 1f, ys[i]);
        }
        chart.drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aChartWithoutPointsDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        chart.addSeries("empty");
        TestSupport.assertXY(350f, 250f, chart.drawOn(page));
        assertEquals(0, page.getContent().length);
    }

    @Test
    void allNegativeDataGetsNegativeAxisLabels() throws Exception {
        String content = draw(-5f, -2.5f, -1f);
        assertTrue(content.contains(TestSupport.hex("-5.0")), content);
        assertTrue(content.contains(TestSupport.hex("-1.0")), content);
        assertFalse(content.contains("NaN"));
    }

    @Test
    void flatDataIsDrawnWithoutNaN() throws Exception {
        String content = draw(3f, 3f, 3f);
        assertFalse(content.contains("NaN"));
        assertTrue(content.contains(TestSupport.hex("3.0")), content);
        assertTrue(content.contains(TestSupport.hex("4.0")), content);
    }

    @Test
    void wholeNumberStepsGetWholeNumberLabels() throws Exception {
        String content = draw(10f, 60f, 35f);
        assertTrue(content.contains(TestSupport.hex("60")), content);
        assertFalse(content.contains(TestSupport.hex("60.00")), content);
    }

    @Test
    void aPathSeriesKeepsItsStrokeWidthAndIsListedInTheLegend() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        chart.addSeries("label").setDrawPath(true).setShape(Shape.INVISIBLE)
                .setStrokeWidth(20f).setStrokeColor(Color.blue)
                .addPoint(1f, 2f).addPoint(3f, 2f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("20 w"), content);
        assertTrue(content.contains(TestSupport.hex("label")), content);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.setDrawLegend(false).drawOn(page);
        assertFalse(TestSupport.content(page).contains(TestSupport.hex("label")));
    }

    @Test
    void aPointWithoutAColorIsDrawnInTheColorOfItsSeries() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        chart.addSeries("").setStrokeColor(Color.red)
                .addPoint(1f, 1f)
                .addPoint(new Point(2f, 2f).setStrokeColor(Color.blue).setShape(Shape.BOX));
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("1 0 0 RG"), content);
        assertTrue(content.contains("0 0 1 RG"), content);
    }

    @Test
    void labelsUseAPeriodWhateverTheDefaultLocale() throws Exception {
        Locale saved = Locale.getDefault();
        Locale.setDefault(Locale.GERMANY);
        try {
            String content = draw(1f, 2f, 3f);
            assertTrue(content.contains(TestSupport.hex("1.25")), content);
            assertFalse(content.contains(TestSupport.hex("1,25")));
        } finally {
            Locale.setDefault(saved);
        }
    }

    @Test
    void theBordersTheAxisLinesTheGridColorAndTheSubtitleWorkAsInABarChart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        chart.addSeries("").setDrawPath(true).setShape(Shape.INVISIBLE)
                .setStrokeWidth(3f).setStrokeColor(Color.blue)
                .addPoint(1f, 1f).addPoint(2f, 2f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("l\ns\n"), content);   // a border width of 0 hides the border
        assertTrue(content.contains("0.5 w\n"), content);   // the axis lines
        assertFalse(content.contains("1 0 0 RG"), content);

        page = new Page(pdf, Letter.PORTRAIT);
        chart.setChartBorderWidth(2f).setInnerBorderWidth(1f).setAxisLineWidth(0f)
                .setGridLineColor(Color.red).setSubtitle("Subtitle");
        chart.drawOn(page);
        content = TestSupport.content(page);
        assertEquals(2, content.split("l\ns\n").length - 1, content);
        assertFalse(content.contains("0.5 w\n"), content);
        assertTrue(content.contains("1 0 0 RG"), content);
        assertTrue(content.contains(TestSupport.hex("Subtitle")), content);
    }

    @Test
    void theMarkerOfASeriesIsTheOneItHasWhenTheChartIsDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf);
        chart.addSeries("").addPoint(1f, 1f).addPoint(2f, 2f).setShape(Shape.INVISIBLE);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(" c\n"), content);  // no circles
    }

    @Test
    void aChartDrawnAgainHasTheRangeOfItsDataThen() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Chart chart = chart(pdf);
        Series series = chart.addSeries("").addPoint(0f, 0f).addPoint(10f, 10f);
        chart.drawOn(new Page(pdf, Letter.PORTRAIT));
        series.addPoint(100f, 100f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("<" + TestSupport.hex("100") + ">"), content);
    }

    @Test
    void anAxisWithoutGridLinesHasTheRangeOfItsData() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf).setXAxisMinMax(0f, 10f, -1).setYAxisMinMax(0f, 1000f, 0);
        chart.addSeries("").addPoint(1f, 1f).addPoint(2f, 2f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("<" + TestSupport.hex("1000") + ">"), content);
        assertFalse(content.contains("<" + TestSupport.hex("10") + ">"), content);
        assertTrue(content.contains("<" + TestSupport.hex("2.0") + ">"), content);
    }

    @Test
    void theSubtitleIsGray() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart chart = chart(pdf).setTitle("Title").setSubtitle("Subtitle");
        chart.addSeries("").addPoint(1f, 1f).addPoint(2f, 2f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertEquals("0 0 0 rg", TestSupport.fillColorBefore(content, "Title"));
        assertEquals("0.41 0.41 0.41 rg", TestSupport.fillColorBefore(content, "Subtitle"));
    }

    @Test
    void aChartIsAFigureDescribedByItsTitleOrItsAlternateDescription() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Chart titled = chart(pdf).setTitle("Sales");
        titled.addSeries("").addPoint(1f, 1f).addPoint(2f, 2f);
        titled.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/Figure <</MCID 0>>\nBDC\n"), content);
        assertTrue(content.endsWith("EMC\n"), content);
        Chart described = chart(pdf).setTitle("Sales").setAltDescription("Sales rose from 1 to 2.");
        described.addSeries("").addPoint(1f, 1f).addPoint(2f, 2f);
        described.drawOn(page);
        assertEquals("Sales", page.structures.get(0).altDescription);
        assertEquals("Sales rose from 1 to 2.", page.structures.get(1).altDescription);
    }
}
