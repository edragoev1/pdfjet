/*
 * BarChartReviewTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

/**
 * The findings of the review of BarChart, DonutChart and ChartScale, of 28
 * October 2026.
 */
class BarChartReviewTest {
    private static BarChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f);
    }

    private static String draw(BarChart chart, Page page) throws Exception {
        TestSupport.assertXY(350f, 250f, chart.drawOn(page));
        return TestSupport.content(page);
    }

    /** Returns how many times the text is drawn in a core font. */
    private static int count(String content, String text) {
        String hex = "<" + TestSupport.hex(text) + ">";
        int count = 0;
        for (int i = content.indexOf(hex); i != -1; i = content.indexOf(hex, i + 1)) {
            count++;
        }
        return count;
    }

    private static float[] twelve(float value) {
        float[] values = new float[12];
        java.util.Arrays.fill(values, value);
        return values;
    }

    // The total of a stack below a range that is set is not written: it was
    // written under the plot, or left of it by horizontal bars.
    @Test
    void aTotalBelowARangeThatIsSetIsNotWritten() throws Exception {
        PDF pdf = TestSupport.newPDF();
        for (boolean horizontal : new boolean[] {false, true}) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            BarChart chart = chart(pdf).setCategories("A", "B").setStacked(true).setValueLabels(true)
                    .setHorizontal(horizontal).setValueAxisRange(50f, 100f)
                    .addSeries("x", new float[] {10f, 30f}).addSeries("y", new float[] {10f, 30f});
            String content = draw(chart, page);
            assertEquals(0, count(content, "20"), content);
            assertEquals(1, count(content, "30"), content);     // B's second segment
        }
    }

    // Values under 0.01 are written with the digits of the step of the axis,
    // and described with theirs; the values that had their digits keep them.
    @Test
    void smallValuesAreNotWrittenAsZero() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = chart(pdf).setTitle("Rates").setCategories("A", "B").setValueLabels(true)
                .addSeries("s", new float[] {0.00012f, 0.00034f});
        String content = draw(chart, page);
        assertEquals(1, count(content, "0.00012"), content);
        assertEquals(1, count(content, "0.00034"), content);
        assertEquals("Rates: s: A 0.00012, B 0.00034.", page.structures.get(0).altDescription);

        page = new Page(pdf, Letter.PORTRAIT);
        chart = chart(pdf).setCategories("a", "b", "c").setValueLabels(true)
                .addSeries("", new float[] {12.5f, 1234f, 0.012f});
        content = draw(chart, page);
        for (String label : new String[] {"12.5", "1,234", "0.01"}) {
            assertEquals(1, count(content, label), label);
        }
        assertEquals("Bar chart: a 12.5, b 1,234, c 0.012.", page.structures.get(0).altDescription);
    }

    // A label wider than its bar is not written inside it: a stacked one is
    // left out, and one inside a grouped bar is written past its end. The
    // labels past the ends of narrow bars, and the totals over narrow stacks,
    // are written where they leave the one before them room.
    @Test
    void labelsKeepToTheWidthOfTheirBars() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String content = draw(chart(pdf).setStacked(true).setValueLabels(true)
                .addSeries("", twelve(12345f)).addSeries("", twelve(12345f)), page);
        assertEquals(0, count(content, "12,345"), content);
        int totals = count(content, "24,690");
        assertTrue(totals > 0 && totals < 12, totals + " totals of 12");

        page = new Page(pdf, Letter.PORTRAIT);
        content = draw(chart(pdf).setValueLabels(true).setValueLabelsInside(true)
                .addSeries("", twelve(12345f)), page);
        assertFalse(content.contains("1 1 1 rg"), content);
        int labels = count(content, "12,345");
        assertTrue(labels > 0 && labels < 12, labels + " labels of 12");
    }

    // By horizontal bars a category is written where it leaves the one before
    // it room: forty of them ran into each other.
    @Test
    void horizontalCategoriesThatDoNotFitAreSkipped() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String[] names = new String[40];
        float[] values = new float[40];
        for (int i = 0; i < 40; i++) {
            names[i] = "Category " + i;
            values[i] = i + 1;
        }
        String content = draw(chart(pdf).setHorizontal(true).setCategories(names).addSeries("", values), page);
        assertEquals(1, count(content, "Category 0"), content);
        assertEquals(0, count(content, "Category 1"), content);
    }

    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart.
    @Test
    void largeNumbersOfAnAxisAreApart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String content = draw(chart(pdf).setCategories("a").setValueAxisRange(4.84e15f, 4.85e15f)
                .addSeries("", new float[] {4.845e15f}), page);
        assertEquals(1, count(content, "4.84e15"), content);
        assertEquals(1, count(content, "4.841e15"), content);
        assertEquals("4.84e15", ChartScale.format(4.841e15f, 0, 0));
    }

    // A percentage is written inside its slice only when the slice is as long
    // across the middle of the ring as the percentage is wide.
    @Test
    void aPercentageWiderThanItsSliceIsNotWritten() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        DonutChart chart = new DonutChart(font, font).setLocation(100f, 100f).setRadii(20f, 0f)
                .add("a", 1f).add("b", 17f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(0, count(content, "6%"), content);
        assertEquals(1, count(content, "94%"), content);
    }

    // A null category is an empty one: it failed the drawing.
    @Test
    void aNullCategoryIsAnEmptyOne() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        String content = draw(chart(pdf).setCategories("a", null).addSeries("", new float[] {1f, 2f}), page);
        assertEquals(1, count(content, "a"), content);
        assertEquals("Bar chart: a 1, 2.", page.structures.get(0).altDescription);
    }
}
