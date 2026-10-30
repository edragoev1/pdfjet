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
        assertEquals("Vehicles. By model year, 2021 to 2023: Battery from 14,755 to 11,853, highest 23,507 in 2022;"
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

    // A range that is set has grid lines on a round step: 3 to 17 was written
    // 3.00, 5.33, 7.67 in 6 lines, and is 3, 5, 7 in 7; a range of no round
    // step in 2 to 10 lines keeps its lines (review of 26 October 2026).
    @Test
    void aSetRangeHasARoundStep() throws Exception {
        assertEquals(7, ChartScale.axisOf(new float[] {5f}, 3f, 17f, true).lines);
        assertEquals(10, ChartScale.axisOf(new float[] {5f}, 0f, 10f, true).lines);
        assertEquals(6, ChartScale.roundLines(0f, 1.3f * 3.7f, 6));     // No round step in 2 to 10
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setYAxisRange(3f, 17f).addLine("a", new float[] {5f, 9f}).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("15")) && !content.contains(TestSupport.hex("5.33")), content);
    }

    // The labels at the ends of the lines, moved apart, stay over the plot:
    // they were pushed down past the bottom of the panel, and with many
    // lines up past its top. As many as the plot holds are drawn, the
    // highlighted one first, then the others in their order.
    @Test
    void theEndLabelsStayOverThePlot() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).setSize(400f, 120f);
        for (int i = 0; i < 30; i++) {
            chart.addLine("line " + i, new float[] {10f, 0f});
        }
        chart.addLine("top", new float[] {0f, 10f}).setHighlight(30);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("top 10")) && content.contains(TestSupport.hex("line 0 0")), content);
        assertFalse(content.contains(TestSupport.hex("line 29 0")), content);
        // The page is 792 high, the chart from 0 to 120 down from its top
        for (int i = 0; i < 30; i++) {
            if (content.contains(TestSupport.hex("line " + i + " 0"))) {
                float y = TestSupport.positionOf(content, "line " + i + " 0")[1];
                assertTrue(y > 792f - 120f && y < 792f, "line " + i + " at " + y);
            }
        }
    }

    // The values are written by their points but the last of each line, which
    // its end label has; the area under a line is filled in a tint of its
    // color, under the line.
    @Test
    void theValuesAndTheAreaAreDrawnWhenAsked() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setCategories("a", "b", "c").addLine("x", new float[] {12f, 37f, 23f})
                .setHighlight(0).setValueLabels(true).setAreaFilled(true).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("12")) && content.contains(TestSupport.hex("37")), content);
        assertFalse(content.contains("<" + TestSupport.hex("23") + ">"), content);   // Only in "x 23"
        int area = content.indexOf("0.78 0.8 0.84 rg\n");      // Navy, a quarter over white
        assertTrue(area > 0 && area < content.indexOf("0.12 0.23 0.37 RG\n"), content);
        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).addLine("x", new float[] {12f, 37f}).drawOn(page);
        assertFalse(TestSupport.content(page).contains(TestSupport.hex("12")));
    }

    // The review of 26 October 2026, the second: a chart without its fonts
    // draws nothing; the value of the first point of a rise is after it and
    // under the line, and that of a peak at the top of the plot under it.
    @Test
    void theSecondReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(null, null).addLine("a", new float[] {1f, 2f}).drawOn(page);
        assertEquals("", TestSupport.content(page));
        new SimpleLineChart(font, font).addLine("big", new float[] {0f, 3e38f}).drawOn(page);    // Did throw

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setCategories("a", "b", "c").setYAxisRange(0f, 50f)
                .addLine("x", new float[] {12f, 50f, 23f}).setValueLabels(true).drawOn(page);
        String content = TestSupport.content(page);
        // The points, from the line drawn in gray: "x y m" of the first
        // point, "x y l" of the peak
        int rg = content.indexOf("0.55 0.59 0.64 RG\n");
        int m = content.indexOf(" m\n", rg);
        String[] start = content.substring(content.lastIndexOf('\n', m) + 1, m).split(" ");
        float x0 = Float.parseFloat(start[0]);
        float y0 = Float.parseFloat(start[1]);
        float[] first = TestSupport.positionOf(content, "12");
        assertTrue(first[0] > x0 && first[1] < y0, "the first value is after its point and under it: "
                + first[0] + ", " + first[1] + " by " + x0 + ", " + y0);
        int peakLine = content.indexOf(" l\n", m);
        String[] peakAt = content.substring(content.lastIndexOf('\n', peakLine) + 1, peakLine).split(" ");
        float[] peak = TestSupport.positionOf(content, "50");     // The first "50" drawn is the axis'
        int value = content.lastIndexOf("<" + TestSupport.hex("50") + ">");
        int td = content.lastIndexOf(" Td\n", value);
        String[] valueAt = content.substring(content.lastIndexOf('\n', td) + 1, td).split(" ");
        assertTrue(Float.parseFloat(valueAt[1]) < Float.parseFloat(peakAt[1]),
                "the value of the peak at the top is under it: " + valueAt[1] + " by " + peakAt[1]);
        assertNotNull(peak);
    }

    // Values under 0.01 have the digits their axis needs, where every number
    // of the axis was 0.00 and the description said from 0 to 0 (the third
    // review of 26 October 2026).
    @Test
    void tinyValuesHaveTheirDigits() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).addLine("a", new float[] {0.001f, 0.004f});
        chart.drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("a 0.004")), TestSupport.content(page));
        assertTrue(chart.altDescription().contains("from 0.001 to 0.004"), chart.altDescription());
    }

    // The number of times the text is drawn in a core font at the start of a
    // string.
    static int drawn(String content, String text) {
        String hex = "<" + TestSupport.hex(text);
        int count = 0;
        for (int i = content.indexOf(hex); i >= 0; i = content.indexOf(hex, i + 1)) {
            count++;
        }
        return count;
    }

    // An axis title longer than the plot is shortened as the other texts of
    // the chart are: the y title of a chart 250 points high ran past its top
    // and its bottom (the review of 28 October 2026).
    @Test
    void aLongAxisTitleIsShortened() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String title = "Number of battery electric vehicles registered in the state, thousands, by county";
        chart(pdf).setSize(400f, 250f).setXAxisTitle(title).setYAxisTitle(title).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(title)), content);
        assertEquals(2, drawn(content, "Number of battery"), content);
    }

    // The chart keeps copies of its categories and its values: a caller that
    // reused one array for every line drew every line with the last values
    // (the review of 28 October 2026).
    @Test
    void keepsCopiesOfItsData() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String[] categories = {"2021", "2022"};
        float[] values = {11f, 12f};
        SimpleLineChart chart = new SimpleLineChart(font, font).setCategories(categories).addLine("a", values);
        values[0] = 21f;
        values[1] = 22f;
        chart.addLine("b", values);
        categories[0] = "1999";
        assertEquals("Line chart, 2021 to 2022: a from 11 to 12; b from 21 to 22.", chart.altDescription());
    }

    // A null category is an empty one, which crashed drawOn (the review of
    // 28 October 2026).
    @Test
    void aNullCategoryIsAnEmptyOne() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleLineChart chart = new SimpleLineChart(font, font).setCategories(null, "2022", null)
                .addLine("a", new float[] {1f, 2f, 3f});
        chart.drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("2022")), TestSupport.content(page));
        assertEquals("Line chart: a from 1 to 3.", chart.altDescription());
    }

    // A title that ends a sentence is not followed by a comma, and a line of
    // one value is described by it alone, where they were "Sales., 2019 to
    // 2024:" and "a from 5 to 5" (the review of 28 October 2026).
    @Test
    void theDescriptionReadsWell() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        SimpleLineChart chart = new SimpleLineChart(font, font).setTitle("Sales.").setCategories("2019", "2024")
                .addLine("a", new float[] {5f, Float.NaN}).addLine("b", new float[] {1f, 2f});
        assertEquals("Sales. 2019 to 2024: a 5; b from 1 to 2.", chart.altDescription());
        chart = new SimpleLineChart(font, font).setTitle("Vehicles?").addLine("a", new float[] {1f, 2f});
        assertEquals("Vehicles? a from 1 to 2.", chart.altDescription());
    }

    // The numbers of an axis written with a power of ten have the significant
    // digits that write them apart: an axis of 4.84e15 to 4.85e15 was written
    // 4.84e15 at each line (the review of 28 October 2026).
    @Test
    void largeNumbersOfAnAxisAreApart() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setYAxisRange(4.84e15f, 4.85e15f).drawOn(page);
        String content = TestSupport.content(page);
        for (String label : new String[] {"4.84e15", "4.841e15", "4.849e15"}) {
            assertEquals(1, drawn(content, label), label + ": " + content);
        }
        assertEquals(4, ChartScale.stepSignificant(4.84e15f, 2e12f));
        assertEquals(3, ChartScale.stepSignificant(12345f, 1000f));
    }

    // The content of the chart, on a page of its own.
    private static String noteContent(PDF pdf, SimpleLineChart chart) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.setLocation(50f, 50f).drawOn(page);
        return TestSupport.content(page);
    }

    private static SimpleLineChart noteChart(Font font) {
        return new SimpleLineChart(font, font).setTitle("T").setSize(300f, 200f).setCategories("a", "b", "c");
    }

    // A value outside a range that is set cuts its line, and the chart says
    // so, in print and in its description: an outlier was clipped without a
    // word. A value that is no number is not outside, and a chart with none
    // outside adds nothing.
    @Test
    void itSaysHowManyValuesAreOutsideTheRange() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        float nan = Float.NaN;
        float inf = Float.POSITIVE_INFINITY;
        String one = "1 value outside the range";
        String two = "2 values outside the range";
        Object[][] cases = {
            {noteChart(font).addLine("x", new float[] {1f, 100f, 3f}), ""},
            {noteChart(font).addLine("x", new float[] {0f, 5f, 10f}).setYAxisRange(0f, 10f), ""},
            {noteChart(font).addLine("x", new float[] {1f, nan, inf}).setYAxisRange(0f, 10f), ""},
            {noteChart(font).addLine("x", new float[] {1f, 2f, 3f, 50f}).setYAxisRange(0f, 10f), ""},
            {noteChart(font).addLine("x", new float[] {1f, 50f, 3f}).setYAxisRange(0f, 10f), one},
            {noteChart(font).addLine("x", new float[] {1f, 2f, -50f}).setYAxisRange(0f, 10f), one},
            {noteChart(font).addLine("x", new float[] {1f, 50f, -50f}).setYAxisRange(0f, 10f), two},
            {noteChart(font).addLine("x", new float[] {1f, 50f, 3f}).addLine("y", new float[] {50f, 2f, 3f})
                    .setYAxisRange(0f, 10f), two},
        };
        for (Object[] c : cases) {
            SimpleLineChart chart = (SimpleLineChart) c[0];
            String note = (String) c[1];
            String content = noteContent(pdf, chart);
            String description = chart.altDescription();
            if (note.isEmpty()) {
                assertFalse(content.contains(TestSupport.hex(" outside")), description);
                assertFalse(description.contains("outside"), description);
            } else {
                assertTrue(content.contains("<" + TestSupport.hex(note) + ">"), note);
                assertTrue(description.endsWith("; " + note + "."), description);
            }
        }
    }

    // The note is in the gray of the numbers, in the small size, on a row of
    // its own inside the chart, under the categories and the x title, with the
    // plot that much lower and nothing else moved; its right end at the plot's.
    @Test
    void theNoteIsUnderThePlot() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String note = "1 value outside the range";
        float size = font.getSize();
        float small = 0.8f * size;
        for (String title : new String[] {"", "year"}) {
            String[] drawn = new String[2];
            for (int k = 0; k < 2; k++) {
                drawn[k] = noteContent(pdf, new SimpleLineChart(font, font).setSize(300f, 200f)
                        .setXAxisTitle(title).setCategories("a", "b", "c").setYAxisRange(0f, 10f)
                        .addLine("x", new float[] {1f, k == 0 ? 2f : 50f, 3f}));
            }
            String with = drawn[1];
            float[] at = TestSupport.positionOf(with, note);
            float end = at[0] + font.stringWidth(small, note);
            float[] c = TestSupport.positionOf(with, "c");
            assertEquals(c[0] + font.stringWidth(size, "c") / 2f, end, 0.1f, "the right end");
            assertTrue(at[0] >= 50f && end <= 350f, "the note at " + at[0]);
            assertEquals(792f - (250f - size), at[1], 0.01f, "the baseline");
            assertEquals(TestSupport.positionOf(drawn[0], "c")[1] + 1.4f * small, c[1], 0.02f, "the plot");
            float last = title.isEmpty() ? c[1] : TestSupport.positionOf(with, title)[1];
            assertTrue(at[1] + font.getAscent(small) <= last - font.getDescent(size),
                    "the note over the row above it");
            assertEquals(TestSupport.fillColorBefore(with, "c"), TestSupport.fillColorBefore(with, note));
        }
        // In a chart too narrow for it, shortened and inside
        SimpleLineChart chart = new SimpleLineChart(font, font).setSize(110f, 200f)
                .setYAxisRange(0f, 10f).addLine("", new float[] {1f, 50f, 3f});
        assertTrue(TestSupport.positionOf(noteContent(pdf, chart), "1 value")[0] >= 50f + size);
        assertTrue(chart.altDescription().endsWith("; " + note + "."), chart.altDescription());
    }

    // A description that is set is followed by the note, for a screen reader
    // to have what the chart prints; with none outside it stays as it is.
    @Test
    void theNoteFollowsADescriptionThatIsSet() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        SimpleLineChart chart = new SimpleLineChart(font, font).addLine("a", new float[] {1f, 2f})
                .setAltDescription("A line");
        assertEquals("A line", chart.altDescription());
        chart.addLine("b", new float[] {1f, 50f}).setYAxisRange(0f, 10f);
        assertEquals("A line. 1 value outside the range.", chart.altDescription());
    }

    // In a tagged document the note is text of the chart's figure, and its
    // description says it.
    @Test
    void theNoteIsInTheFigure() throws Exception {
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(font, font).setSize(300f, 200f).setYAxisRange(0f, 10f)
                .addLine("a", new float[] {1f, 50f}).drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(content.indexOf("BDC\n"), content.lastIndexOf("BDC\n"), content);
        assertEquals(content.indexOf("EMC\n"), content.lastIndexOf("EMC\n"), content);
        assertTrue(content.lastIndexOf("ET\n") < content.lastIndexOf("EMC\n"), content);
        pdf.complete();
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/Alt "
                + TestSupport.textString("Line chart: a from 1 to 50; 1 value outside the range.")));
    }
}
