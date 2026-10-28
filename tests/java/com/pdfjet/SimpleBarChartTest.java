/*
 * SimpleBarChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

class SimpleBarChartTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static SimpleBarChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new SimpleBarChart(font, font).setWidth(330f).setLocation(50f, 50f)
                .setTitle("Statement, 12 pages").setSubtitle("warm ms")
                .add("PDFjet", 7.3f).add("iText", 72.6f).add("OpenPDF", 18.4f).add("PDFBox", 44.1f);
    }

    // The widths of the rectangles filled on the page, in the order they were drawn.
    private static List<Float> rectWidths(String content) {
        List<Float> widths = new ArrayList<Float>();
        Matcher m = Pattern.compile("([-\\d.]+) ([-\\d.]+) ([-\\d.]+) ([-\\d.]+) re").matcher(content);
        while (m.find()) {
            widths.add(Float.parseFloat(m.group(3)));
        }
        return widths;
    }

    @Test
    void eachBarIsItsShareOfTheLargestValue() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).drawOn(page);
        List<Float> widths = rectWidths(TestSupport.content(page));
        assertEquals(5, widths.size(), "the panel and 4 bars: " + widths);
        assertEquals(330f, widths.get(0), TestSupport.DELTA);
        float longest = widths.get(2);      // iText's 72.6
        assertEquals(longest * 7.3f / 72.6f, widths.get(1), 0.05f);
        assertEquals(longest * 18.4f / 72.6f, widths.get(3), 0.05f);
        assertEquals(longest * 44.1f / 72.6f, widths.get(4), 0.05f);
    }

    @Test
    void theHighlightedBarHasItsOwnColor() throws Exception {
        for (boolean highlighted : new boolean[] {true, false}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            SimpleBarChart chart = chart(pdf).setHighlightColor(Color.red);
            if (highlighted) {
                chart.setHighlight(0);
            }
            chart.drawOn(page);
            assertEquals(highlighted, TestSupport.content(page).contains("1 0 0 rg"));
        }
    }

    @Test
    void describesItselfFromItsValues() throws Exception {
        SimpleBarChart chart = chart(TestSupport.newPDF()).setFractionDigits(1);
        assertEquals("Statement, 12 pages. Warm ms: PDFjet 7.3, iText 72.6, OpenPDF 18.4, PDFBox 44.1.",
                chart.altDescription());
        chart.setAltDescription("Four libraries");
        assertEquals("Four libraries", chart.altDescription());
    }

    @Test
    void valuesHaveTheDigitsTheyNeedUnlessSet() throws Exception {
        SimpleBarChart chart = chart(TestSupport.newPDF());
        assertEquals("7.25", chart.label(7.25f));
        assertEquals("72", chart.label(72f));
        chart.setFractionDigits(1);
        assertEquals("72.0", chart.label(72f));
    }

    @Test
    void isAsTallAsItsRows() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart chart = chart(pdf);
        float h = chart.getHeight();
        float[] xy = chart.drawOn(page);
        assertEquals(380f, xy[0], TestSupport.DELTA);
        assertEquals(50f + h, xy[1], TestSupport.DELTA);
        SimpleBarChart taller = chart(pdf).add("Another", 10f);
        assertEquals(h + chart.rowHeight(), taller.getHeight(), TestSupport.DELTA);
    }

    @Test
    void aNegativeValueHasNoBarButItsValue() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new SimpleBarChart(font, font).add("up", 5f).add("down", -2f).drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(2, rectWidths(content).size(), "the panel and one bar");
        assertTrue(content.contains(TestSupport.hex("-2")), content);
    }

    @Test
    void withoutBarsDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Font font = TestSupport.helvetica(pdf);
        new SimpleBarChart(font, font).setTitle("Nothing").drawOn(page);
        assertEquals("", TestSupport.content(page));
    }

    // A null title or name is empty: it threw when the chart was drawn.
    @Test
    void nullIsEmpty() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        new SimpleBarChart(font, font).setTitle(null).setSubtitle(null).setAltDescription(null)
                .add(null, 1f).add("b", 2f).drawOn(new Page(pdf, Letter.PORTRAIT));
    }

    // A name wider than two fifths of the chart is shortened with an
    // ellipsis, and the bars keep their room: in a chart narrower than its
    // names they went to nothing, the values past the panel (review of 26
    // October 2026). The description keeps the name whole.
    @Test
    void aLongNameIsShortened() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        String name = "A name far too long for the column of names of this chart";
        SimpleBarChart chart = new SimpleBarChart(font, font).setWidth(200f).add(name, 5f).add("b", 3f);
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(name)), content);
        String shortened = ChartScale.shorten(font, font.getSize(), name, 0.4f * (200f - 2f * font.getSize()));
        // The ellipsis is 85 in WinAnsi, the encoding of a core font
        String start = shortened.substring(0, shortened.length() - 1);
        assertTrue(shortened.endsWith("\u2026") && content.contains(TestSupport.hex(start) + "85>"), content);
        assertTrue(chart.altDescription().contains(name));
        assertEquals("A n\u2026", ChartScale.shorten(font, 10f, "A name", font.stringWidth(10f, "A n\u2026")));
        assertEquals("A name", ChartScale.shorten(font, 10f, "A name", 100f));
        assertEquals("A\u2026", ChartScale.shorten(font, 10f, "A  name", font.stringWidth(10f, "A \u2026") - 0.1f));
    }

    // A chart without its fonts draws nothing and has no height; a title too
    // long for the chart is shortened; a value of 3e38 is written short (the
    // review of 26 October 2026, the second).
    @Test
    void theSecondReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart none = new SimpleBarChart(null, null).add("a", 1f);
        none.drawOn(page);
        assertEquals("", TestSupport.content(page));
        assertEquals(0f, none.getHeight(), 0f);
        String title = "A title far too long for a chart two hundred points wide";
        new SimpleBarChart(font, font).setWidth(200f).setTitle(title).add("a", 3e38f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains(TestSupport.hex(title)), content);
        assertTrue(content.contains(TestSupport.hex("3e38")), content);
        assertEquals("1.23e20", ChartScale.format(1.2345e20f, 0, 2));
        assertEquals("-4.5e16", ChartScale.groupDigits(ChartScale.format(-4.5e16f, 0, 2)));
    }

    // Values are grouped in thousands, as in the other charts; and in a chart
    // narrower than its names and values the names give the values room,
    // which were written past the chart (the third review of 26 October 2026).
    @Test
    void theThirdReviewOfTheCharts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleBarChart(font, font).setWidth(300f).add("a", 23507f).drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("23,507")), TestSupport.content(page));
        page = new Page(pdf, Letter.PORTRAIT);
        SimpleBarChart narrow = new SimpleBarChart(font, font).setWidth(90f).add("A long name", 1234567f);
        narrow.setLocation(0f, 0f);
        narrow.drawOn(page);
        float[] at = TestSupport.positionOf(TestSupport.content(page), "1,234,567");
        assertTrue(at[0] + font.stringWidth("1,234,567") <= 90f + 0.01f, at[0] + " in 90");
    }

    // A title that ends a sentence is not followed by a colon, where it was
    // "Sales.: a 5" (the review of 28 October 2026).
    @Test
    void theDescriptionAfterATitleThatEndsASentence() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        SimpleBarChart chart = new SimpleBarChart(font, font).setTitle("Sales.").add("a", 5f).add("b", 6f);
        assertEquals("Sales. a 5, b 6.", chart.altDescription());
    }
}
