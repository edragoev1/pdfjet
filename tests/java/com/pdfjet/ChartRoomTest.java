/*
 * ChartRoomTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

// A chart too small for its plot is an error, where the chart drew nothing
// and said nothing (the owner, 4 November 2026: "we can create more space if
// the user tries to jam too much info"). The same chart with room is drawn.
class ChartRoomTest {
    private static void noRoom(String name, Drawable chart) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        String message = assertThrows(IllegalArgumentException.class, () -> chart.drawOn(page)).getMessage();
        assertTrue(message.contains(name) && message.contains("has no room for its plot"), message);
    }

    @Test
    void aChartWithoutRoomIsAnError() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font f = TestSupport.helvetica(pdf);
        noRoom("SimpleLineChart", new SimpleLineChart(f, f).setTitle("Sales")
                .addLine("a", new float[] {1f, 5f, 3f}).setSize(40f, 30f));
        noRoom("SimpleScatterChart", new SimpleScatterChart(f, f).setTitle("Points")
                .add(1f, 2f).add(3f, 4f).setSize(40f, 30f));
        noRoom("BarChart", new BarChart(f, f).setCategories("a", "b")
                .addSeries("s", new float[] {1f, 2f}).setSize(30f, 20f));
        noRoom("SimpleBarChart", new SimpleBarChart(f, f).setWidth(90f).add("A long name", 1234567f));

        Page page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(f, f).setTitle("Sales").addLine("a", new float[] {1f, 5f, 3f})
                .setSize(300f, 200f).drawOn(page);
        new SimpleScatterChart(f, f).setTitle("Points").add(1f, 2f).add(3f, 4f).setSize(300f, 200f).drawOn(page);
        new BarChart(f, f).setCategories("a", "b").addSeries("s", new float[] {1f, 2f})
                .setSize(300f, 200f).drawOn(page);
        new SimpleBarChart(f, f).setWidth(300f).add("A long name", 1234567f).drawOn(page);
        // Drawn, with no exception: the four charts had room
    }
}
