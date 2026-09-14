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
        return new DonutChart(font, font).setLocation(200f, 200f).setRadii(100f, 50f);
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
}
