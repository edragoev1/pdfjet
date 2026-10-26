/*
 * Example_27.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_27.java
 *
 * Draws bar charts. The first page is a horizontal bar chart of the ten
 * longest rivers, each bar in its own color with its length inside it, and a
 * color key under the chart. The second page draws two charts with vertical
 * bars from the same data: the two series grouped by month, and the same
 * series stacked.
 */
final public class Example_27 {
    public Example_27() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_27.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Bar Charts");

        drawHorizontal(pdf);
        drawVertical(pdf);

        pdf.complete();
    }

    private void drawHorizontal(PDF pdf) throws Exception {
        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(15f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(9f);

        Font f3 = new Font(pdf, IBMPlexSans.Bold);
        f3.setSize(9f);

        Font f4 = new Font(pdf, IBMPlexSans.Regular);
        f4.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading of the page, above the chart
        new TextLine(f1, "Rivers of the World")
                .setStructureType(StructElem.H1)
                .setFontSize(18f)
                .setLocation(36f, 40f)
                .drawOn(page);

        String[] rivers = {
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"};
        float[] lengths = {6650f, 6400f, 6300f, 5971f, 5540f, 5464f, 5410f, 4880f, 4700f, 4444f};
        int[] colors = {
                0x477aa8, 0x5f7e5f, 0x8b6f47, 0x8e7152, 0x517aa0,
                0x967110, 0x687b68, 0x8e7155, 0x5c5c5c, 0x897154};

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(36f, 70f);
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
        chart.setAltDescription(
                "The ten longest rivers in the world, in kilometers: Nile 6,650, Amazon 6,400, Yangtze 6,300, Mississippi-Missouri 5,971, Yenisey-Baikal-Selenga 5,540, Huang He (Yellow) 5,464, Ob-Irtysh 5,410, Paraná 4,880, Congo 4,700, Amur 4,444.");
        chart.drawOn(page);

        // The color key under the chart
        int gray = 0x444444;
        new TextLine(f3, "Color key (illustrative):")
                .setTextColor(gray).setLocation(171f, 496f).drawOn(page);
        int[] keyColors = {0x477aa8, 0x5f7e5f, 0x8e7155, 0x967110, 0x5c5c5c};
        String[] keyTexts = {
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"};
        float[] keyX = {171f, 262f, 398f, 171f, 313f};
        float[] keyY = {512f, 512f, 512f, 527f, 527f};
        for (int i = 0; i < keyColors.length; i++) {
            // The swatches are rectangles, drawn as artifacts next to their text.
            new Rect(keyX[i], keyY[i] - 8.5f, 10.5f, 10.5f).setFillColor(keyColors[i]).drawOn(page);
            new TextLine(f4, keyTexts[i])
                    .setTextColor(gray).setLocation(keyX[i] + 15f, keyY[i]).drawOn(page);
        }

        String note = "Color mapping is illustrative; lengths and conditions vary by source and season.";
        new TextLine(f4, note)
                .setTextColor(0x696969).setLocation(576f - f4.stringWidth(note), 550f).drawOn(page);

        // The data of the chart in a table under it
        String[] lengthTexts = {
                "6,650", "6,400", "6,300", "5,971", "5,540", "5,464", "5,410", "4,880", "4,700", "4,444"};
        String[] characters = {
                keyTexts[0], keyTexts[1], keyTexts[2], keyTexts[2], keyTexts[0],
                keyTexts[3], keyTexts[1], keyTexts[2], keyTexts[4], keyTexts[2]};
        List<List<Cell>> rows = new ArrayList<List<Cell>>();
        rows.add(Arrays.asList(
                new Cell(f3, "River").setWidth(180f),
                new Cell(f3, "Length, km").setWidth(90f).setTextAlignment(Alignment.RIGHT),
                new Cell(f3, "Character").setWidth(270f)));
        for (int i = 0; i < rivers.length; i++) {
            rows.add(Arrays.asList(
                    new Cell(f2, rivers[i]).setWidth(180f),
                    new Cell(f2, lengthTexts[i]).setWidth(90f)
                            .setTextAlignment(Alignment.RIGHT),
                    new Cell(f2, characters[i]).setWidth(270f)));
        }
        Table table = new Table();
        table.setTableData(rows, 1);
        table.setHeaderRowStyle(f3, 0x000000, 0xEEF0F3);
        table.setCellBorderColor(0xC8CCD2);
        table.setLocation(36f, 572f);
        table.drawOn(page);
    }

    private void drawVertical(PDF pdf) throws Exception {
        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        // The heading of the document, above the charts
        new TextLine(f1, "Sales in 2025 and 2026")
                .setStructureType(StructElem.H1)
                .setFontSize(18f)
                .setLocation(70f, 45f)
                .drawOn(page);

        String[] months = {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        float[] units2025 = {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f};
        float[] units2026 = {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f};

        BarChart chart = new BarChart(f1, f2);
        chart.setLocation(70f, 70f);
        chart.setSize(500f, 300f);
        chart.setTitle("Units sold by month");
        chart.setXAxisTitle("Month");
        chart.setYAxisTitle("Units");
        chart.setCategories(months);
        chart.addSeries("2025", units2025);
        chart.addSeries("2026", units2026);
        chart.setGroupGap(0.4f);
        chart.setBarGap(0.1f);
        chart.setAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.");
        chart.drawOn(page);

        BarChart stacked = new BarChart(f1, f2);
        stacked.setLocation(70f, 420f);
        stacked.setSize(500f, 300f);
        stacked.setTitle("Units sold by month, stacked");
        stacked.setXAxisTitle("Month");
        stacked.setYAxisTitle("Units");
        stacked.setCategories(months);
        stacked.addSeries("2025", units2025);
        stacked.addSeries("2026", units2026);
        stacked.setStacked(true);
        stacked.setDrawValueLabels(true);
        stacked.setAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.");
        stacked.drawOn(page);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_27();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_27 => %4d ms%n", time1 - time0);
    }
}   // End of Example_27.java
