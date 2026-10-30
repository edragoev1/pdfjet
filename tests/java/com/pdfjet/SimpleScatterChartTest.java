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

    // A linked dot is drawn as a point of add(x, y) is, stroke for stroke, with
    // nothing beside it: in a document that is not tagged it adds a link alone,
    // described by its name, and the chart's description counts it and does
    // not name it.
    @Test
    void aLinkedDotIsADotWithADescription() throws Exception {
        TestSupport.allowCoreFonts(true);
        for (boolean withSeries : new boolean[] {false, true}) {
            String[] plain = drawDot(withSeries, false);
            String[] dot = drawDot(withSeries, true);
            assertEquals(plain[0], dot[0], "not drawn as a plain point");
            assertEquals(plain[1], dot[1], "not described as a plain point");
            assertEquals(1, dot[2].split("/Subtype /Link", -1).length - 1);
            assertTrue(dot[2].contains("/Contents " + TestSupport.textString("Andorra: 84.0 years")), "the name");
            assertTrue(dot[2].contains("/URI <" + TestSupport.hex("https://pdfjet.com/a").toLowerCase() + ">"), "the URI");
        }
    }

    // The content, the description and the bytes of a chart with a third point
    // that is a linked dot, or a plain one.
    private static String[] drawDot(boolean withSeries, boolean linked) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        SimpleScatterChart chart = new SimpleScatterChart(font, font).setTitle("T").setTrendLine(true)
                .add(1f, 1f).add(3f, 4f);
        if (withSeries) {
            chart.addSeries("Series");
        }
        if (linked) {
            chart.addLinkedDot("Andorra: 84.0 years", 2f, 2f, "https://pdfjet.com/a");
        } else {
            chart.add(2f, 2f);
        }
        String alt = chart.altDescription();
        chart.drawOn(page);
        String content = TestSupport.content(page);
        pdf.complete();
        return new String[] {content, alt, TestSupport.latin1(bos.toByteArray())};
    }

    // In a PDF/UA document a linked dot is a figure of its own in the Link that
    // holds its annotation, as a linked point is, and looks as it does in the
    // chart: transparent, or the outline of its series, undashed. A highlighted
    // point stays over it.
    @Test
    void aLinkedDotIsAFigureInItsLink() throws Exception {
        for (boolean withSeries : new boolean[] {false, true}) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
            Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
            Page page = new Page(pdf, Letter.PORTRAIT);
            page.setStrokeDashPattern("[3 3] 0");
            SimpleScatterChart chart = new SimpleScatterChart(font, font).setSize(300f, 200f).setLocation(50f, 50f)
                    .setHighlightColor(Color.red).add(1f, 1f);
            if (withSeries) {
                chart.addSeries("Series");
            }
            chart.addLinkedDot("Andorra", 2f, 2f, "https://pdfjet.com/a")
                    .addLinkedDot("Belize", 3f, 1f, "https://pdfjet.com/b")
                    .add("Chad", 4f, 3f, "https://pdfjet.com/c").add("Dominica", 2f, 3f);
            String alt = chart.altDescription();
            chart.drawOn(page);
            String content = TestSupport.content(page);
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
            assertEquals(3, links);
            for (String name : new String[] {"Andorra", "Belize", "Chad"}) {
                assertTrue(raw.contains("/Alt " + TestSupport.textString(name)), name);
            }
            assertFalse(alt.contains("Andorra") || alt.contains("Belize"), alt);
            // The chart, the dots' figures, the named point over them, Chad's
            int first = content.indexOf("/Figure <</MCID 1>>");
            int second = content.indexOf("/Figure <</MCID 2>>");
            int last = content.indexOf("/Figure <</MCID 3>>");
            int artifact = content.indexOf("/Artifact BMC");
            assertTrue(content.indexOf("/Figure <</MCID 0>>") != -1 && first != -1 && second > first
                    && artifact > second && last > artifact, "the order of the figures");
            String dot = content.substring(first, second);
            assertFalse(dot.contains("1 0 0 rg"), dot);
            assertEquals(withSeries, dot.contains("\nS\n"), dot);
            assertEquals(!withSeries, dot.contains("\nf\n"), dot);
            String before = content.substring(content.lastIndexOf("EMC", first), first);
            assertTrue(before.contains("[] 0 d\n") && before.contains(" gs\n"), before);
        }
    }

    // A dot's description is its name, or its URI when it has no name, and a
    // dot without a URI is a plain point; one outside a range that is set is
    // no link.
    @Test
    void aLinkedDotWithoutANameOrAURI() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setSize(300f, 200f).setLocation(50f, 50f).setXAxisRange(0f, 5f)
                .addLinkedDot("", 1f, 1f, "https://pdfjet.com/a").addLinkedDot("Belize", 2f, 2f, "")
                .addLinkedDot("Chad", 9f, 3f, "https://pdfjet.com/c").drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, raw.split("/Subtype /Link", -1).length - 1);
        assertTrue(raw.contains("/Alt " + TestSupport.textString("https://pdfjet.com/a")), "the URI");
        assertFalse(raw.contains(TestSupport.textString("Belize")) || raw.contains(TestSupport.textString("Chad")));
    }

    // The link of a dot is at least 8 points square and holds the dot, on the
    // page as written, and inside the page, in a corner too.
    @Test
    void theLinkOfADotCanBeHit() throws Exception {
        TestSupport.allowCoreFonts(true);
        for (float size : new float[] {2f, 9f, 30f}) {
            ByteArrayOutputStream bos = new ByteArrayOutputStream();
            PDF pdf = new PDF(bos);
            Font font = TestSupport.helvetica(pdf);
            font.setSize(size);
            Page page = new Page(pdf, Letter.PORTRAIT);
            new SimpleScatterChart(font, font).setSize(612f, 792f).setLocation(0f, 0f)
                    .setXAxisRange(0f, 4f).setYAxisRange(0f, 4f)
                    .addLinkedDot("Middle", 2f, 2f, "https://pdfjet.com/m")
                    .addLinkedDot("Left", 0f, 2f, "https://pdfjet.com/l").drawOn(page);
            // Past the corners of the page, as a chart that hangs over it
            page.addAnnotation(corner(-1f, -1f, 0f, 0f));
            page.addAnnotation(corner(612f, 792f, 613f, 793f));
            pdf.complete();
            String[] rects = TestSupport.latin1(bos.toByteArray()).split("/Rect \\[");
            assertEquals(5, rects.length);
            for (int i = 1; i < rects.length; i++) {
                String[] box = rects[i].substring(0, rects[i].indexOf(']')).split(" ");
                float x1 = Float.parseFloat(box[0]);
                float y1 = Float.parseFloat(box[1]);
                float x2 = Float.parseFloat(box[2]);
                float y2 = Float.parseFloat(box[3]);
                String rect = rects[i].substring(0, rects[i].indexOf(']'));
                assertTrue(x2 - x1 >= 7.999f && Math.abs(y2 - y1) >= 7.999f, rect);
                assertTrue(i > 2 || x2 - x1 >= 0.9f * size, rect);
                assertTrue(x1 >= 0f && x2 <= 612f && Math.min(y1, y2) >= 0f && Math.max(y1, y2) <= 792f, rect);
            }
        }
    }

    // A link with the hit size of a dot, at the box.
    private static Annotation corner(float x1, float y1, float x2, float y2) {
        Annotation annotation = new Annotation(Annotation.Link, x1, y1, x2, y2, null, null, 0f, null, null,
                "https://pdfjet.com/c", null, null, null, "Corner");
        annotation.hitSize = SimpleScatterChart.HIT_SIZE;
        return annotation;
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

    // The content of the chart, on a page of its own.
    private static String noteContent(PDF pdf, SimpleScatterChart chart) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart.setLocation(50f, 50f).drawOn(page);
        return TestSupport.content(page);
    }

    private static SimpleScatterChart noteChart(Font font) {
        return new SimpleScatterChart(font, font).setTitle("T").setSize(300f, 200f).add(1f, 1f).add(2f, 2f);
    }

    // A point outside a range that is set is not drawn, and the chart says so,
    // in print and in its description: an outlier vanished without a word. A
    // value that is no number is not outside, and a chart with none outside
    // adds nothing.
    @Test
    void itSaysHowManyPointsAreOutsideTheRange() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        float nan = Float.NaN;
        float inf = Float.POSITIVE_INFINITY;
        String one = "1 point outside the range";
        String two = "2 points outside the range";
        Object[][] cases = {
            {noteChart(font).add(100f, 100f), ""},
            {noteChart(font).add(0f, 10f).add(10f, 0f).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f), ""},
            {noteChart(font).add(nan, 50f).add(50f, inf).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f), ""},
            {noteChart(font).add(50f, 1f).setXAxisRange(0f, 10f), one},
            {noteChart(font).add(1f, -50f).setYAxisRange(0f, 10f), one},
            {noteChart(font).add(50f, 50f).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f), one},
            {noteChart(font).add(50f, 1f).add(-50f, 1f).setXAxisRange(0f, 10f), two},
            {noteChart(font).add(1f, 50f).add("far", 2f, 60f).setYAxisRange(0f, 10f), two},
            {noteChart(font).add(50f, 1f).add(1f, 50f).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f), two},
            {noteChart(font).addLinkedDot("a", 50f, 1f, "https://pdfjet.com/a").setXAxisRange(0f, 10f), one},
        };
        for (Object[] c : cases) {
            SimpleScatterChart chart = (SimpleScatterChart) c[0];
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
    // its own inside the chart, under the numbers and the x title, with the
    // plot that much lower and nothing else moved; right under the plot's
    // right end.
    @Test
    void theNoteIsUnderThePlot() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        String note = "2 points outside the range";
        float size = font.getSize();
        float small = 0.8f * size;
        for (String title : new String[] {"", "children per woman"}) {
            String without = noteContent(pdf, new SimpleScatterChart(font, font).setSize(300f, 200f)
                    .setXAxisTitle(title).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f).add(1f, 1f).add(2f, 2f));
            String with = noteContent(pdf, new SimpleScatterChart(font, font).setSize(300f, 200f)
                    .setXAxisTitle(title).setXAxisRange(0f, 10f).setYAxisRange(0f, 10f).add(1f, 1f).add(2f, 2f)
                    .add(50f, 1f).add(1f, 50f));
            float[] at = TestSupport.positionOf(with, note);
            float end = at[0] + font.stringWidth(small, note);
            float right = Math.min(350f - size, 350f - size / 2f - font.stringWidth(size, "10") / 2f);
            assertEquals(right, end, 0.1f, "the right end");
            assertTrue(at[0] >= 50f && end <= 350f, "the note at " + at[0]);
            assertEquals(792f - (250f - size), at[1], 0.01f, "the baseline");
            float[] number = TestSupport.positionOf(with, "0");
            assertEquals(TestSupport.positionOf(without, "0")[1] + 1.4f * small, number[1], 0.02f, "the plot");
            // The numbers of the x axis, or the title under them
            float last = number[1] + font.getAscent(size) / 2f - 1.4f * size;
            if (!title.isEmpty()) {
                last = TestSupport.positionOf(with, title)[1];
            }
            assertTrue(at[1] + font.getAscent(small) <= last - font.getDescent(size), "the note over the row above it");
            assertEquals(TestSupport.fillColorBefore(with, "0"), TestSupport.fillColorBefore(with, note));
        }
        // In a chart too narrow for it, shortened and inside
        SimpleScatterChart chart = new SimpleScatterChart(font, font).setSize(90f, 200f)
                .setXAxisRange(0f, 1f).add(0f, 1f).add(5f, 1f).add(6f, 1f);
        assertTrue(TestSupport.positionOf(noteContent(pdf, chart), "2 points")[0] >= 50f + size);
        assertTrue(chart.altDescription().endsWith("; " + note + "."), chart.altDescription());
    }

    // A description that is set is followed by the note, for a screen reader
    // to have what the chart prints; with none outside it stays as it is.
    @Test
    void theNoteFollowsADescriptionThatIsSet() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        SimpleScatterChart chart = new SimpleScatterChart(font, font).add(1f, 1f).add(2f, 2f)
                .setAltDescription("Two points");
        assertEquals("Two points", chart.altDescription());
        chart.add(50f, 1f).setXAxisRange(0f, 10f);
        assertEquals("Two points. 1 point outside the range.", chart.altDescription());
        chart.setAltDescription("Two points.");
        assertEquals("Two points. 1 point outside the range.", chart.altDescription());
    }

    // In a tagged document the note is text of the chart's figure, and its
    // description says it.
    @Test
    void theNoteIsInTheFigure() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleScatterChart(font, font).setSize(300f, 200f).add(1f, 1f).add(50f, 1f)
                .setXAxisRange(0f, 10f).drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(content.indexOf("BDC\n"), content.lastIndexOf("BDC\n"), content);
        assertEquals(content.indexOf("EMC\n"), content.lastIndexOf("EMC\n"), content);
        assertTrue(content.lastIndexOf("ET\n") < content.lastIndexOf("EMC\n"), content);
        pdf.complete();
        assertTrue(TestSupport.latin1(bos.toByteArray()).contains("/Alt "
                + TestSupport.textString("Scatter chart: 1 points; 1 point outside the range.")));
    }
}
