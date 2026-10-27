/*
 * DonutChartTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class DonutChartTest {
    private static DonutChart chart(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new DonutChart(font, font).setLocation(100f, 100f).setRadii(100f, 50f);
    }

    @Test
    void aChartWithoutValuesDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = chart(pdf).addSlice(new Slice(0f, Color.red, "none"));
        TestSupport.assertXY(300f, 300f, chart.drawOn(page));
        assertEquals(0, page.getContent().length);
    }

    @Test
    void slicesArePercentagesOfTheSumOfTheValues() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = chart(pdf);
        chart.addSlice(new Slice(1f, Color.red, "a"));
        chart.addSlice(new Slice(1f, Color.green, "b"));
        chart.addSlice(new Slice(2f, Color.blue, "c"));
        TestSupport.assertXY(300f, 300f, chart.drawOn(page));
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("25%")), content);
        assertTrue(content.contains(TestSupport.hex("50%")), content);
        assertFalse(content.contains(TestSupport.hex("100%")), content);
    }

    @Test
    void aPieChartHasAnInnerRadiusOfZero() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = chart(pdf).setRadii(100f, 0f);
        chart.addSlice(new Slice(3f, Color.red, "a")).addSlice(new Slice(1f, Color.blue, "b"));
        chart.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("75%")), content);
        assertTrue(content.contains("200 592 l"), content);   // the center, in PDF coordinates
    }

    @Test
    void aChartIsAFigureThatListsItsSlices() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart donut = chart(pdf).addSlice(new Slice(25f, Color.red, "Apples"))
                .addSlice(new Slice(75f, Color.blue, "Oranges"));
        donut.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.startsWith("/Figure <</MCID 0>>\nBDC\n"), content);
        assertTrue(content.endsWith("EMC\n"), content);
        assertEquals("0.13 0.15 0.17 rg", TestSupport.fillColorBefore(content, "Apples"));
        assertEquals("1 1 1 rg", TestSupport.fillColorBefore(content, "25%"));
        DonutChart pie = chart(pdf).setRadii(100f, 0f).addSlice(new Slice(1f, Color.red, ""));
        pie.drawOn(page);
        DonutChart described = chart(pdf).setAltDescription("Most are oranges.")
                .addSlice(new Slice(1f, Color.red, "Apples"));
        described.drawOn(page);
        assertEquals("Donut chart: Apples 25%, Oranges 75%", page.structures.get(0).altDescription);
        assertEquals("Pie chart: 100%", page.structures.get(1).altDescription);
        assertEquals("Most are oranges.", page.structures.get(2).altDescription);
    }

    @Test
    void theDefaultLookIsQuiet() throws Exception {
        // Slices in the palette, navy first; thin white lines between them;
        // thin gray leader lines; the percentage white on navy, ink on the
        // light steel blue; and no black line or text.
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).addSlice(new Slice(60f, "a")).addSlice(new Slice(40f, "b")).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("0.12 0.23 0.37 rg"), content);     // navy
        assertTrue(content.contains("0.44 0.58 0.76 rg"), content);     // steel blue
        assertTrue(content.contains("1 1 1 RG\n1.5 w\n"), content);    // the white lines between
        assertTrue(content.contains("0.55 0.59 0.64 RG\n0.75 w\n"), content);   // the leader lines
        assertFalse(content.contains("0 0 0 RG") || content.contains("0 0 0 rg"), content);
        assertEquals("1 1 1 rg", TestSupport.fillColorBefore(content, "60%"));
        assertEquals("0.13 0.15 0.17 rg", TestSupport.fillColorBefore(content, "40%"));
    }

    @Test
    void aTitleIsDrawnAboveTheCircle() throws Exception {
        // The title at the location, the subtitle under it, the circle under
        // both; a chart without them has the circle at its location, as before.
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart plain = chart(pdf).addSlice(new Slice(1f, "a"));
        TestSupport.assertXY(300f, 315f, plain.drawOn(page));    // The circle, and its label 15 under it
        DonutChart titled = chart(pdf).setTitle("Fruit").setSubtitle("This week").addSlice(new Slice(1f, "a"));
        float[] xy = titled.drawOn(page);
        assertEquals(300f, xy[0], 0.01f);
        assertTrue(xy[1] > 315f, "the circle is under the title: " + xy[1]);
        String content = TestSupport.content(page);
        assertTrue(content.contains(TestSupport.hex("Fruit")) && content.contains(TestSupport.hex("This week")));
        assertEquals("0.37 0.41 0.45 rg", TestSupport.fillColorBefore(content, "This week"));
        assertEquals("Fruit, This week: a 100%", page.structures.get(1).altDescription);
    }

    // A slice that is not a number or an infinity is not drawn, and the
    // others are: one made the arc's loop endless (review of 11 October 2026,
    // #2). A value whose 360ths are past the largest float is drawn, as it
    // compares with the others (review of 26 October 2026).
    @Test
    void aSliceThatIsNotANumberIsNotDrawn() throws Exception {
        for (float bad : new float[] {Float.NaN, Float.POSITIVE_INFINITY}) {
            PDF pdf = TestSupport.newPDF();
            Page page = new Page(pdf, Letter.PORTRAIT);
            DonutChart chart = chart(pdf);
            chart.addSlice(new Slice(bad, Color.red, "bad"));
            chart.addSlice(new Slice(1f, Color.green, "a"));
            chart.addSlice(new Slice(3f, Color.blue, "b"));
            chart.drawOn(page);
            String content = TestSupport.content(page);
            assertTrue(content.contains(TestSupport.hex("25%")) && content.contains(TestSupport.hex("75%")),
                    bad + ": the other slices are not 25% and 75% in " + content);
        }
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).addSlice(new Slice(3e38f, "a")).addSlice(new Slice(3e38f, "b")).drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("50%")), TestSupport.content(page));
    }

    // Returns the curves the chart of the radii, the location and the values
    // draws, a line each.
    private static String curves(float r1, float r2, float x, float y, float... values) throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        DonutChart chart = new DonutChart(null, null).setRadii(r1, r2);
        for (float value : values) {
            chart.addSlice(new Slice(value, 0x336699, ""));
        }
        chart.setLocation(x, y).drawOn(page);
        StringBuilder sb = new StringBuilder();
        for (String line : TestSupport.content(page).split("\n")) {
            if (line.endsWith(" c")) {
                sb.append(line).append('\n');
            }
        }
        return sb.toString();
    }

    // The curves of a slice are computed in floats rounded at each step, as
    // Go computes them: computed in doubles, 15 of 300 random charts were
    // 0.01 apart from Go's, as the last line of the first chart here and the
    // fifth of the second were (review of 11 October 2026, #25). The values are Go's.
    @Test
    void theCurvesOfTheSlicesAreTheSameAsGos() throws Exception {
        assertEquals(
                "321.02 538.8 379.14 509.82 416.28 460.55 c\n"
                + "307.24 409.27 283.99 420.86 259.31 420.86 c\n"
                + "470.54 388.56 468.88 288.89 412.26 218.75 c\n"
                + "343.14 320.89 343.8 360.76 322.1 389.56 c\n"
                + "371.16 167.84 307.43 140.72 242.25 146.4 c\n"
                + "278.56 261.63 304.05 272.48 320.49 292.84 c\n"
                + "134.1 155.82 54.06 251.14 63.48 359.29 c\n"
                + "72.31 460.64 157.02 538.51 258.75 538.8 c\n"
                + "218.39 420.74 184.51 389.6 180.98 349.05 c\n"
                + "177.21 305.79 209.22 267.67 252.49 263.9 c\n"
                + "258.94 538.8 259.12 538.8 259.31 538.8 c\n"
                + "259.23 420.86 259.16 420.86 259.09 420.86 c\n",
                curves(196.57f, 78.628006f, 62.74f, 253.2f, 218.66f, 313.27f, 231.33f, 721.5f, 0.67f));
        assertEquals(
                "556.56 622.69 627.89 551.36 627.89 463.38 c\n"
                + "627.89 448.39 625.77 433.48 621.61 419.08 c\n"
                + "544.03 449.03 545.05 456.19 545.05 463.38 c\n"
                + "545.05 505.61 510.81 539.85 468.58 539.85 c\n"
                + "620.66 415.79 619.6 412.54 618.44 409.31 c\n"
                + "541.07 438.97 541.58 440.54 542.03 442.12 c\n"
                + "588.58 326.55 497.28 283.67 414.51 313.52 c\n"
                + "407.02 316.23 399.74 319.49 392.73 323.28 c\n"
                + "435.54 394.31 439.03 392.75 442.63 391.45 c\n"
                + "482.35 377.12 526.18 397.7 540.51 437.43 c\n"
                + "331.52 356.43 298.91 425.47 312.2 493.8 c\n"
                + "387.14 445.19 402.79 412.04 432.17 396.13 c\n"
                + "320.29 535.35 344.57 571.98 379.7 595.59 c\n"
                + "409.06 515.51 397.4 497.93 393.52 477.98 c\n"
                + "405.98 613.26 436.92 622.69 468.58 622.69 c\n"
                + "453.38 539.85 438.53 535.32 425.92 526.84 c\n",
                curves(159.31f, 76.468796f, 309.27f, 169.31f, 671.72f, 23.38f, 623.93f, 459.3f, 285.28f, 214.6f));
        // A pie chart, whose inner curves are at its center
        assertEquals(
                "185.73 742 218.74 722.94 236.6 692 c\n"
                + "150 642 150 642 150 642 c\n"
                + "264.22 644.17 247.83 583.01 200 555.4 c\n"
                + "184.8 546.62 167.55 542 150 542 c\n"
                + "150 642 150 642 150 642 c\n"
                + "150 642 150 642 150 642 c\n"
                + "94.77 542 50 586.77 50 642 c\n"
                + "50 697.23 94.77 742 150 742 c\n"
                + "150 642 150 642 150 642 c\n"
                + "150 642 150 642 150 642 c\n",
                curves(100f, 0f, 50f, 50f, 1f, 2f, 3f));
    }

    // A null slice is left out: it threw when the chart was drawn.
    @Test
    void aNullSliceIsLeftOut() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).addSlice(null).addSlice(new Slice(1f, "a")).drawOn(page);
        assertTrue(TestSupport.content(page).contains(TestSupport.hex("a")));
    }

    // The labels of thin slices side by side are moved apart, a line apart at
    // least; the percentages add up to 100; the white line by a thin slice is
    // narrower than it; and drawOn returns the bottom right corner of the
    // labels too, measured as drawn (review of 26 October 2026).
    @Test
    void thinSlicesKeepTheirLabelsApart() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = chart(pdf).addSlice(new Slice(90f, "Big"));
        for (int i = 0; i < 6; i++) {
            chart.addSlice(new Slice(1.5f, "Small " + i));
        }
        float[] measured = chart.drawOn(null);
        float[] xy = chart.drawOn(page);
        TestSupport.assertXY(measured[0], measured[1], xy);
        String content = TestSupport.content(page);
        Font font = TestSupport.helvetica(pdf);
        for (int i = 1; i < 6; i++) {
            float above = TestSupport.positionOf(content, "Small " + (i - 1))[1];
            float below = TestSupport.positionOf(content, "Small " + i)[1];
            assertTrue(Math.abs(above - below) >= font.getBodyHeight() - 0.01f, above + " " + below);
        }
        assertEquals("Donut chart: Big 91%, Small 0 2%, Small 1 2%, Small 2 2%, Small 3 1%, Small 4 1%, Small 5 1%",
                page.structures.get(0).altDescription);
        assertTrue(content.contains("1.5 w\n"), content);                      // By the big slice
        assertTrue(content.matches("(?s).*\n0\\.\\d+ w\n.*"), content);     // Between two thin ones
    }

    // With a legend the chart has no leader lines: each slice's label and
    // percentage is in rows under the circle, which the returned corner
    // holds. The text in the hole is drawn and described; a pie chart has no
    // hole for it.
    @Test
    void aLegendAndATextInTheHole() throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Test");
        Page page = new Page(pdf, Letter.PORTRAIT);
        DonutChart chart = chart(pdf).setLegend(true).setCenterText("100 crates")
                .addSlice(new Slice(60f, "a")).addSlice(new Slice(40f, "b"));
        float[] xy = chart.drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("0.55 0.59 0.64 RG"), content);     // No leader lines
        assertTrue(content.contains(TestSupport.hex("a 60%")) && content.contains(TestSupport.hex("b 40%")), content);
        assertTrue(content.contains(TestSupport.hex("100 crates")), content);
        assertTrue(xy[1] > 300f + 15f, "the legend is under the circle: " + xy[1]);
        TestSupport.assertXY(xy[0], xy[1], chart.drawOn(null));
        assertEquals("100 crates: a 60%, b 40%", page.structures.get(0).altDescription);
        page = new Page(pdf, Letter.PORTRAIT);
        chart(pdf).setRadii(100f, 0f).setCenterText("none").addSlice(new Slice(1f, "a")).drawOn(page);
        assertFalse(TestSupport.content(page).contains(TestSupport.hex("none")));
    }
}
