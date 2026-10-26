/*
 * SimpleLineChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import org.junit.jupiter.api.Test;

class SimpleLineChartTest {
    private static SimpleLineChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new SimpleLineChart(font, font).setSize(400f, 300f).setLocation(50f, 50f)
                .setTitle("Vehicles").setSubtitle("by model year")
                .setCategories("2021", "2022", "2023")
                .addLine("Battery", new float[] {14755f, 23507f, 11853f})
                .addLine("Hybrid", new float[] {3541f, 4015f, 1500f});
    }

    // The number of paths the page strokes.
    private static int strokes(String content) {
        int count = 0;
        for (int i = content.indexOf("\nS\n"); i >= 0; i = content.indexOf("\nS\n", i + 1)) {
            count++;
        }
        return count;
    }

    @Test
    void describesItselfFromItsData() throws Exception {
        SimpleLineChart chart = chart(TestSupport.newPDF());
        assertEquals("Vehicles, by model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;"
                + " Hybrid from 3,541 to 1,500, highest 4,015 in 2022.", chart.altDescription());
        chart.setAltDescription("Two lines");
        assertEquals("Two lines", chart.altDescription());
    }

    @Test
    void theHighlightedLineHasItsOwnColor() throws Exception {
        for (boolean highlighted : new boolean[] {true, false}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            SimpleLineChart chart = chart(pdf).setHighlightColor(Color.red);
            if (highlighted) {
                chart.setHighlight(0);
            }
            chart.drawOn(page);
            assertEquals(highlighted, TestSupport.content(page).contains("1 0 0 RG"));
        }
    }

    @Test
    void labelsTheEndsOfItsLines() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).drawOn(page);
        String content = TestSupport.content(page);
        for (String label : new String[] {"Battery 11,853", "Hybrid 1,500"}) {
            assertTrue(content.contains(TestSupport.hex(label)), label);
        }
    }

    @Test
    void aMissingValueLeavesAGap() throws Exception {
        int[] counts = new int[2];
        float[][] lines = {{1f, 2f, 3f, 4f, 5f}, {1f, 2f, Float.NaN, 4f, 5f}};
        for (int k = 0; k < 2; k++) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.helvetica(pdf);
            new SimpleLineChart(font, font).setCategories("a", "b", "c", "d", "e")
                    .addLine("line", lines[k]).drawOn(page);
            counts[k] = strokes(TestSupport.content(page));
        }
        assertEquals(counts[0] + 1, counts[1], "a gap in a line of five values strokes one path more");
    }

    @Test
    void endLabelsDoNotMeet() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        SimpleLineChart chart = new SimpleLineChart(font, font).setCategories("a", "b")
                .addLine("one", new float[] {1f, 5f}).addLine("two", new float[] {2f, 5f});  // Both end at 5
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.drawOn(page);
        // The two labels are drawn at two baselines a line apart
        String content = TestSupport.content(page);
        float[] one = TestSupport.positionOf(content, "one 5");
        float[] two = TestSupport.positionOf(content, "two 5");
        assertTrue(Math.abs(one[1] - two[1]) >= font.getBodyHeight() - TestSupport.DELTA,
                "the labels are less than a line apart");
    }

    @Test
    void withoutValuesDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        float[] xy = new SimpleLineChart(font, font).setTitle("Nothing")
                .addLine("line", new float[] {Float.NaN}).drawOn(page);
        assertEquals("", TestSupport.content(page));
        assertEquals(400f, xy[0], TestSupport.DELTA);
        assertEquals(300f, xy[1], TestSupport.DELTA);
    }

    // A value outside a range that is set is drawn past the plot, and the
    // lines are clipped at its edges; a value far past it, or values whose
    // span is past the largest float, are drawn without an exception that
    // lost the document (review of 26 October 2026).
    @Test
    void aValueOutsideTheRangeIsClipped() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).drawOn(page);
        assertFalse(TestSupport.content(page).contains("W\nn\n"));     // No clip in range
        for (float far : new float[] {60f, 1e37f, -1e37f}) {
            page = new Page(pdf, Letter.PORTRAIT);
            new SimpleLineChart(font, font).setYAxisRange(0f, 10f)
                    .addLine("a", new float[] {5f, far, 5f}).drawOn(page);
            String content = TestSupport.content(page);
            assertTrue(content.contains("W\nn\n"), content);
            assertTrue(content.contains(TestSupport.hex("a 5")), content);
        }
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).addLine("wide", new float[] {-3e38f, 3e38f}).drawOn(page);
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setYAxisRange(-3e38f, 3e38f).addLine("a", new float[] {1f, 2f}).drawOn(page);
    }

    // A null title, name or list is empty, as it was in DonutChart: it threw
    // when the chart was drawn.
    @Test
    void nullIsEmpty() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        new SimpleLineChart(font, font).setTitle(null).setSubtitle(null).setXAxisTitle(null)
                .setYAxisTitle(null).setAltDescription(null).setCategories((String[]) null)
                .addLine(null, new float[] {1f, 2f}).addLine("b", null)
                .drawOn(new Page(pdf, Letter.PORTRAIT));
    }
}
