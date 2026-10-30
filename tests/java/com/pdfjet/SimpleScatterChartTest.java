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
                .setTitle("Life expectancy and children per woman").setSubtitle("by country")
                .setXAxisTitle("children per woman").setYAxisTitle("life expectancy")
                .add(5.2f, 61f).add(3f, 70f).add("Canada", 1.3f, 82f).add(2f, 76f);
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
        assertEquals("Life expectancy and children per woman. By country: 4 points, life expectancy "
                + "against children per woman; Canada 1.3 and 82; the trend falls.", chart.altDescription());
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
    // In a PDF/UA document a highlighted point that is a link is a figure of
    // its own, described by its name, in the Link that holds its annotation;
    // a point that is not a link stays in the chart's figure.
    @Test
    void aLinkedPointIsAFigureInItsLink() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setSize(300f, 200f).setLocation(50f, 50f)
                .add(1f, 1f).add("Andorra", 2f, 2f, "https://pdfjet.com/a")
                .add("Belize", 3f, 1f, "https://pdfjet.com/b").add("Chad", 4f, 3f)
                .drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        java.util.Map<String, String[]> elements = TestSupport.structElements(raw);
        int links = 0;
        for (java.util.Map.Entry<String, String[]> entry : elements.entrySet()) {
            String[] link = entry.getValue();
            if (!link[0].equals("Link")) {
                continue;
            }
            links++;
            String figure = link[2].substring(1).split(" ")[0];
            assertEquals("Figure", elements.get(figure)[0], link[2]);
            assertEquals(entry.getKey(), elements.get(figure)[1]);
            assertTrue(link[2].contains("/Type /OBJR"), link[2]);
        }
        assertEquals(2, links);
        for (String name : new String[] {"Andorra", "Belize"}) {
            assertTrue(raw.contains("/Alt " + TestSupport.textString(name)), name);
        }
        assertEquals(2, raw.split("/Subtype /Link", -1).length - 1);
    }

    // In a document that is not tagged, a linked point is drawn in the chart,
    // its dot and its name the link, and no elements are made.
    @Test
    void aLinkedPointOfADocumentNotTaggedIsDrawnInTheChart() throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).add(1f, 1f).add("Andorra", 2f, 2f, "https://pdfjet.com/a").drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertFalse(raw.contains("/StructElem"));
        assertEquals(1, raw.split("/Subtype /Link", -1).length - 1);
        assertTrue(raw.contains("/URI <" + TestSupport.hex("https://pdfjet.com/a").toLowerCase() + ">"), "the URI");
    }

    // A point outside a range that is set is not drawn; a point far past it,
    // or points whose span is past the largest float, are drawn without an
    // exception that lost the document (review of 26 October 2026).
    @Test
    void aPointOutsideTheRangeIsNotDrawn() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setXAxisRange(0f, 2f).setYAxisRange(0f, 100f).add("Far", 20f, 30f).add(1e37f, 1e37f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("Canada")), content);
        assertFalse(content.contains(TestSupport.hex("Far")), content);
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).add(-3e38f, -3e38f).add(3e38f, 3e38f).drawOn(page);
    }

    // A null title or name is empty: it threw when the chart was drawn.
    @Test
    void nullIsEmpty() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setTitle(null).setSubtitle(null).setXAxisTitle(null)
                .setYAxisTitle(null).setAltDescription(null).add(null, 1f, 1f).add(2f, 3f).drawOn(page);
    }

    // The name of a highlighted point is on a patch of the panel's color, so
    // that the gray dots under it do not show through.
    @Test
    void aNameIsOnAPatchOfThePanel() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).drawOn(page);
        String content = TestSupport.content(page);
        int name = content.indexOf("<" + TestSupport.hex("Canada"));
        int patch = content.lastIndexOf("0.95 0.95 0.96 rg\n", name);
        assertTrue(patch > content.indexOf("0.12 0.23 0.37 rg\n"), content);     // After the navy dot
        assertTrue(content.substring(patch, name).contains(" re\n"), content);
    }

    // A chart without its fonts draws nothing; a long name of a point is
    // shortened and kept in the plot (the review of 26 October 2026, the
    // second).
    @Test
    void theSecondReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(null, null).add(1f, 1f).drawOn(page);
        assertEquals("", TestSupport.content(page));
        new SimpleScatterChart(font, font).add(0f, 0f).add(3e38f, 3e38f).drawOn(page);         // Did throw
        String name = "A name far too long to be written whole beside its point in the chart";
        chart(pdf).add(name, 3f, 65f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(name)), content);
    }

    // The third review of 26 October 2026: no trend line where it misses the
    // plot; an empty URI is no link; values under 0.01 have the digits their
    // axis needs; numbers are grouped in thousands; and points whose span is
    // past the largest float draw nothing.
    @Test
    void theThirdReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setYAxisRange(0f, 10f).add(0f, 50f).add(10f, 50f).setTrendLine(true)
                .drawOn(page);
        assertFalse(TestSupport.content(page).contains(TestSupport.hex("trend")), TestSupport.content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).add("A", 1f, 2f, "").add(2f, 3f).drawOn(page);
        assertTrue(page.annots.isEmpty());

        page = new Page(pdf, Letter.PORTRAIT);
        SimpleScatterChart tiny = new SimpleScatterChart(font, font).add(0.001f, 1f).add(0.004f, 2f);
        tiny.drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("0.001")), TestSupport.content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).add("Big", 23507f, 1f).add(1000f, 2f).drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("25,000")), TestSupport.content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).add(-3e38f, -3e38f).add(3e38f, 3e38f).drawOn(page);
        assertEquals("", TestSupport.content(page));
    }

    // An axis title longer than the plot is shortened as the other texts of
    // the chart are, and the description keeps it whole; a title that ends a
    // sentence is not followed by a colon (the review of 28 October 2026).
    @Test
    void aLongAxisTitleIsShortened() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String title = "Number of battery electric vehicles registered in the state, thousands, by county";
        SimpleScatterChart chart = chart(pdf).setSize(400f, 250f).setXAxisTitle(title).setYAxisTitle(title);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(title)), content);
        assertEquals(2, SimpleLineChartTest.drawn(content, "Number of battery"), content);
        assertTrue(chart.altDescription().contains(title + " against " + title), chart.altDescription());
        Font font = TestSupport.helvetica(pdf);
        chart = new SimpleScatterChart(font, font).setTitle("Life expectancy?").add(1f, 2f).add(2f, 3f);
        assertEquals("Life expectancy? 2 points.", chart.altDescription());
    }

    // The end of an axis rounded to steps is never inside the values: 591
    // steps of 0.1 come to 59.100002 in float, and a point at 59.1 was not
    // drawn (the review of 30 October 2026, E1).
    @Test
    void anAxisHoldsItsValues() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setSize(400f, 300f).add("Only", 59.1f, 59.1f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("Only")), content);
        for (int i = 1; i <= 20000; i++) {
            float value = i * 0.1f;
            ChartScale.Axis axis = ChartScale.axisOf(new float[] {value}, 0f, 0f, false);
            assertTrue(ChartScale.onAxis(value, axis), value + " on " + axis.min + " to " + axis.max);
            float[] flat = ChartScale.flatRange(value);
            Round round = ChartScale.roundMaxAndMinValues(flat[1], flat[0]);
            assertTrue(round.minValue <= flat[0] && round.maxValue >= flat[1], flat[0] + " to " + flat[1]);
        }
    }
}
