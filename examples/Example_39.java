/*
 * Example_39.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_39.java
 *
 * Draws a horizontal bar chart of the ten longest rivers, each bar in its own
 * color with its length written inside it, under a title and a subtitle, and
 * a color key and a source note under the chart.
 */
final public class Example_39 {
    public Example_39() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_39.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(15f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        Font f3 = new Font(pdf, IBMPlexSans.Bold);
        f3.setSize(9f);

        Font f4 = new Font(pdf, IBMPlexSans.Regular);
        f4.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        String[] rivers = {
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"};
        float[] lengths = {6650f, 6400f, 6300f, 5971f, 5540f, 5464f, 5410f, 4880f, 4700f, 4444f};
        int[] colors = {
                0x5b9bd5, 0x6b8e6b, 0x8b6f47, 0x9b7b5a, 0x6fa8dc,
                0xd4a017, 0x8fa98f, 0xa08060, 0x5c5c5c, 0x8b7355};

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(36f, 40f);
        chart.setSize(540f, 400f);
        chart.setTitle("10 Longest Rivers in the World");
        chart.setSubtitle("Length in kilometers · Color reflects typical sediment / pollution character");
        chart.setCategories(rivers);
        chart.addSeries("", lengths, colors);
        chart.setHorizontal(true);
        chart.setGroupGap(0.4f);
        chart.setValueAxisMinMax(0f, 8000f, 4);
        chart.setGridLineWidth(0.75f);
        chart.setGridLineColor(0xe0e0e0);
        chart.setGridLineDashPattern("[] 0");
        chart.setAxisLineWidth(0f);
        chart.setDrawValueLabels(true);
        chart.setValueLabelsInside(true);
        chart.setGroupingUsed(true);
        chart.drawOn(page);

        // The color key under the chart
        int gray = 0x444444;
        new TextLine(f3, "Color key (illustrative):")
                .setTextColor(gray).setLocation(171f, 466f).drawOn(page);
        int[] keyColors = {0x5b9bd5, 0x6b8e6b, 0xa08060, 0xd4a017, 0x5c5c5c};
        String[] keyTexts = {
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"};
        float[] keyX = {171f, 262f, 398f, 171f, 313f};
        float[] keyY = {482f, 482f, 482f, 497f, 497f};
        for (int i = 0; i < keyColors.length; i++) {
            page.setBrushColor(keyColors[i]);
            page.fillRect(keyX[i], keyY[i] - 8.5f, 10.5f, 10.5f);
            new TextLine(f4, keyTexts[i])
                    .setTextColor(gray).setLocation(keyX[i] + 15f, keyY[i]).drawOn(page);
        }

        String note = "Color mapping is illustrative; lengths and conditions vary by source and season.";
        new TextLine(f4, note)
                .setTextColor(0x999999).setLocation(576f - f4.stringWidth(note), 520f).drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_39();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_39 => %4d ms%n", time1 - time0);
    }
}   // End of Example_39.java
