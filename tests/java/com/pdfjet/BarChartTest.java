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
        chart.addSeries("", new float[] {2.5f, -1f}).setValueLabels(true);
        chart.setFractionDigits(1);
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("2.5")), content);
        assertTrue(content.contains(TestSupport.hex("-1.0")), content);
    }

    @Test
    void aManualAxisRangeIsUsedAsGiven() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a");
        chart.addSeries("", new float[] {5f}).setValueAxisRange(0f, 12f);     // In steps of 2
        String content = draw(chart, page);
        assertTrue(content.contains(TestSupport.hex("12")), content);
        assertTrue(content.contains("<" + TestSupport.hex("2") + ">"), content);
        assertFalse(content.contains("<" + TestSupport.hex("3") + ">"), content);
        assertFalse(content.contains("NaN"));
    }

    @Test
    void axisLabelsThatWouldMeetAreDrawnAtEverySecondLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        // 8 steps of 10 across about 70 points: 9 points a step, where a label
        // of one decimal, 70.0, needs more
        BarChart chart = chart(pdf).setCategories("a").setHorizontal(true).setSize(120f, 200f);
        chart.addSeries("", new float[] {72.6f}).setValueAxisRange(0f, 80f).setFractionDigits(1);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("80.0")), content);
        assertFalse(content.contains(TestSupport.hex("70.0")), content);  // would meet 60.0 and 80.0
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
        chart.setStacked(true).setValueLabels(true);
        String stacked = draw(chart, page);
        assertTrue(stacked.contains(TestSupport.hex("75")), stacked);
        // The total of each stack above it
        assertTrue(stacked.contains(TestSupport.hex("50")) && stacked.contains(TestSupport.hex("85")), stacked);
    }

    @Test
    void theDefaultLookIsQuiet() throws Exception {
        // The first series in blue, the grid lines light gray and solid, the
        // text gray or ink, and no black line or text.
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setCategories("a", "b").setTitle("t").setSubtitle("s");
        chart.addSeries("one", new float[] {20f, 75f}).addSeries("two", new float[] {30f, 10f});
        String content = draw(chart, page);
        assertTrue(content.contains("0.31 0.47 0.65 rg"), content);     // blue
        assertTrue(content.contains("0.95 0.56 0.17 rg"), content);     // orange
        assertTrue(content.contains("0.88 0.89 0.91 RG\n0.5 w\n"), content);     // Thin and solid
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
        chart.setValueAxisRange(0f, 8000f).setValueLabels(true).setValueLabelsInside(true);
        chart.setGroupingUsed(true).setGridLineColor(Color.lightgray);
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
    void aChartIsAFigureDescribedByItsDataOrItsAlternateDescription() throws Exception {
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
        assertEquals("Sales: a 1, b 2.", page.structures.get(0).altDescription);
        assertEquals("Sales rose from 1 to 2.", page.structures.get(1).altDescription);
    }

    // A category name wider than two fifths of a horizontal chart is
    // shortened with an ellipsis, and a chart too small for its plot draws
    // nothing: its axis ran backwards, the bars over the names (review of 26
    // October 2026).
    @Test
    void aLongNameIsShortenedAndAChartWithNoRoomDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String name = "A very very long category name indeed, and longer";
        BarChart chart = chart(pdf).setSize(200f, 200f).setHorizontal(true)
                .setCategories(name).addSeries("s", new float[] {5f});
        TestSupport.assertXY(250f, 250f, chart.drawOn(page));
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(name)), content);
        String shortened = ChartScale.shorten(TestSupport.helvetica(pdf), TestSupport.helvetica(pdf).getSize(), name, 0.4f * 200f);
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        String start = shortened.substring(0, shortened.length() - 1);
        assertTrue(shortened.endsWith("\u2026") && content.contains(TestSupport.hex(start) + "85>"), content);
        page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setSize(20f, 20f).setCategories("a").addSeries("s", new float[] {5f}).drawOn(page);
        assertEquals("", TestSupport.content(page));
    }

    // A chart without a title has no room for one: its plot starts higher,
    // its bars taller. The legend wraps at the right edge, and a series with
    // a color per bar has the color of its first bar in it.
    @Test
    void theTopOfTheChartIsWhatItHas() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String titled = draw(chart(pdf).setTitle("t").setCategories("a").addSeries("", new float[] {1f}), page);
        page = new Page(pdf, Letter.PORTRAIT);
        String untitled = draw(chart(pdf).setCategories("a").addSeries("", new float[] {1f}), page);
        assertTrue(barHeight(untitled) > barHeight(titled) + 10f, barHeight(untitled) + " " + barHeight(titled));
        page = new Page(pdf, Letter.PORTRAIT);
        BarChart wide = chart(pdf).setCategories("a").setLegend(true);
        for (int i = 0; i < 8; i++) {
            wide.addSeries("Series number " + i, new float[] {1f});
        }
        String content = draw(wide, page);
        assertNotEquals(TestSupport.positionOf(content, "Series number 0")[1],
                TestSupport.positionOf(content, "Series number 7")[1]);
        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a", "b").setLegend(true)
                .addSeries("own", new float[] {1f, 2f}, new int[] {0x123456, 0x654321}), page);
        int swatch = content.indexOf("0.07 0.2 0.34 rg\n");
        assertTrue(swatch >= 0 && swatch < content.indexOf(TestSupport.hex("own")), content);
    }

    // The height of the first bar the content fills in blue.
    private static float barHeight(String content) {
        int i = content.indexOf(" re\n", content.indexOf("0.31 0.47 0.65 rg\n"));
        String[] parts = content.substring(content.lastIndexOf('\n', i) + 1, i).split(" ");
        return Math.abs(Float.parseFloat(parts[3]));
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

    // A line series is drawn over the bars, in ink, with a dot at each value;
    // the axis reaches its values, the legend lists it and the description
    // has its values after the bars'.
    @Test
    void aLineSeriesIsDrawnOverTheBars() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setTitle("Sales").setCategories("a", "b").setLegend(true)
                .addSeries("Sold", new float[] {1f, 2f}).addLineSeries("Target", new float[] {9f, Float.NaN});
        String content = draw(chart, page);
        assertTrue(content.contains("0.13 0.15 0.17 RG\n") && content.contains("\n2 w\n"), content);   // In ink
        assertTrue(content.contains(TestSupport.hex("Target")), content);
        assertTrue(content.contains("<" + TestSupport.hex("8") + ">"), content);   // The axis reaches 9
        assertEquals("Sales: Sold: a 1, b 2; Target, a line: a 9.", page.structures.get(0).altDescription);
        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a").addSeries("", new float[] {1f})
                .addLineSeries("", new float[] {2f}, 0x123456), page);
        assertTrue(content.contains("0.07 0.2 0.34 RG\n"), content);
    }

    // The review of 26 October 2026, the second: a chart without its fonts
    // draws nothing; the title, a long category under vertical bars and a
    // legend name are shortened; a line series longer than the categories
    // adds no slots; the line is on a white line a little wider; and a value
    // past the rounding of the largest float is drawn on the range of the
    // values.
    @Test
    void theSecondReviewOfTheCharts() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        new BarChart(null, null).setCategories("a").addSeries("", new float[] {1f}).drawOn(page);
        assertEquals("", TestSupport.content(page));

        String longName = "A category name far too long to be written under its bar";
        String title = "A title far too long for a chart of three hundred points, which ran past it";
        String content = draw(chart(pdf).setTitle(title).setLegend(true).setCategories(longName, "b")
                .addSeries("Sold", new float[] {1f, 2f})
                .addLineSeries("Target", new float[] {2f, 3f, 4f, 5f, 6f}), page);
        assertFalse(content.contains(TestSupport.hex(longName)) || content.contains(TestSupport.hex(title)), content);
        assertTrue(content.contains("1 1 1 RG\n3.5 w\n"), content);
        assertTrue(page.structures.get(0).altDescription.endsWith("Target, a line: " + longName + " 2, b 3."),
                page.structures.get(0).altDescription);

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a", "b").setValueLabels(true)
                .addSeries("", new float[] {1e30f, 3e38f}), page);
        assertTrue(content.contains(TestSupport.hex("3e38")), content);
    }

    // The third review of 26 October 2026: a set range past the largest float
    // draws nothing, where its NaN coordinates failed the document; a line
    // series longer than the categories does not reach the axis past them; a
    // bar below 0 has room for its value between the plot and the
    // categories; the gaps are kept in their ranges; a stacked sum past the
    // largest float is not written; and a bar below a set range has no value.
    @Test
    void theThirdReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        draw(chart(pdf).setCategories("a").addSeries("", new float[] {1f}).setValueAxisRange(-3e38f, 3e38f), page);
        assertEquals("", TestSupport.content(page));

        page = new Page(pdf, Letter.PORTRAIT);
        String content = draw(chart(pdf).setCategories("a", "b").addSeries("Sold", new float[] {1f, 2f})
                .addLineSeries("Target", new float[] {2f, 3f, 4f, 5f, 6f}), page);
        assertFalse(content.contains("<" + TestSupport.hex("6") + ">"), content);

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setHorizontal(true).setValueLabels(true).setCategories("A")
                .addSeries("", new float[] {-1234567f}), page);
        float label = TestSupport.positionOf(content, "-1,234,567")[0];
        float name = TestSupport.positionOf(content, "A")[0];
        Font font = TestSupport.helvetica(pdf);
        assertTrue(name + font.stringWidth("A") <= label, name + " " + label);

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a").setGroupGap(1.5f).setBarGap(-2f)
                .addSeries("", new float[] {1f}).addSeries("", new float[] {2f}), page);
        assertFalse(content.contains(" -"), content);   // No negative width

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a").setStacked(true).setValueLabels(true)
                .addSeries("", new float[] {2e38f}).addSeries("", new float[] {2e38f}), page);
        assertFalse(content.contains(TestSupport.hex("Infinity")), content);

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setCategories("a", "b").setValueLabels(true).setValueAxisRange(10f, 20f)
                .addSeries("", new float[] {5f, 15f}), page);
        assertTrue(content.contains("<" + TestSupport.hex("15") + ">"), content);
        assertFalse(content.contains("<" + TestSupport.hex("5") + ">"), content);
    }
}
