/*
 * Example_27.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_27.cs
 *
 * Draws bar charts. The first page is a horizontal bar chart of the ten
 * longest rivers, each bar in its own color with its length inside it, and a
 * color key under the chart. The second page draws two charts with vertical
 * bars from the same data: the two series grouped by month, and the same
 * series stacked. The third, the units of 2026 with a line over the bars for
 * the target of each month.
 */
public class Example_27 {
    public Example_27() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_27.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Bar Charts");

        DrawHorizontal(pdf);
        DrawVertical(pdf);
        DrawTarget(pdf);

        pdf.Complete();
    }

    private void DrawHorizontal(PDF pdf) {
        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(15f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        Font f3 = new Font(pdf, IBMPlexSans.Bold);
        f3.SetSize(9f);

        Font f4 = new Font(pdf, IBMPlexSans.Regular);
        f4.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // The heading of the page, above the chart
        new TextLine(f1, "Rivers of the World")
                .SetStructureType(StructElem.H1)
                .SetFontSize(18f)
                .SetLocation(36f, 40f)
                .DrawOn(page);

        String[] rivers = {
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"};
        float[] lengths = {6650f, 6400f, 6300f, 5971f, 5540f, 5464f, 5410f, 4880f, 4700f, 4444f};
        int[] colors = {
                0x477aa8, 0x5f7e5f, 0x8b6f47, 0x8e7152, 0x517aa0,
                0x967110, 0x687b68, 0x8e7155, 0x5c5c5c, 0x897154};

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(36f, 70f);
        chart.SetSize(540f, 400f);
        chart.SetTitle("10 Longest Rivers in the World");
        chart.SetSubtitle("Length in kilometers · Color reflects typical sediment / pollution character");
        chart.SetCategories(rivers);
        chart.AddSeries("", lengths, colors);
        chart.SetHorizontal(true);
        chart.SetGroupGap(0.4f);
        chart.SetValueAxisMinMax(0f, 8000f, 4);
        chart.SetGridLineWidth(0.75f);
        chart.SetGridLineColor(0xe0e0e0);
        chart.SetGridLineDashPattern("[] 0");
        chart.SetAxisLineWidth(0f);
        chart.SetDrawValueLabels(true);
        chart.SetValueLabelsInside(true);
        chart.SetGroupingUsed(true);
        chart.SetAltDescription(
                "The ten longest rivers in the world, in kilometers: Nile 6,650, Amazon 6,400, Yangtze 6,300, Mississippi-Missouri 5,971, Yenisey-Baikal-Selenga 5,540, Huang He (Yellow) 5,464, Ob-Irtysh 5,410, Paraná 4,880, Congo 4,700, Amur 4,444.");
        chart.DrawOn(page);

        // The color key under the chart
        int gray = 0x444444;
        new TextLine(f3, "Color key (illustrative):")
                .SetTextColor(gray).SetLocation(171f, 496f).DrawOn(page);
        int[] keyColors = {0x477aa8, 0x5f7e5f, 0x8e7155, 0x967110, 0x5c5c5c};
        String[] keyTexts = {
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"};
        float[] keyX = {171f, 262f, 398f, 171f, 313f};
        float[] keyY = {512f, 512f, 512f, 527f, 527f};
        for (int i = 0; i < keyColors.Length; i++) {
            // The swatches are rectangles, drawn as artifacts next to their text.
            new Rect(keyX[i], keyY[i] - 8.5f, 10.5f, 10.5f).SetFillColor(keyColors[i]).DrawOn(page);
            new TextLine(f4, keyTexts[i])
                    .SetTextColor(gray).SetLocation(keyX[i] + 15f, keyY[i]).DrawOn(page);
        }

        String note = "Color mapping is illustrative; lengths and conditions vary by source and season.";
        new TextLine(f4, note)
                .SetTextColor(0x696969).SetLocation(576f - f4.StringWidth(note), 550f).DrawOn(page);

        // The data of the chart in a table under it
        String[] lengthTexts = {
                "6,650", "6,400", "6,300", "5,971", "5,540", "5,464", "5,410", "4,880", "4,700", "4,444"};
        String[] characters = {
                keyTexts[0], keyTexts[1], keyTexts[2], keyTexts[2], keyTexts[0],
                keyTexts[3], keyTexts[1], keyTexts[2], keyTexts[4], keyTexts[2]};
        List<List<Cell>> rows = new List<List<Cell>>();
        rows.Add(new List<Cell> {
                new Cell(f3, "River").SetWidth(180f),
                new Cell(f3, "Length, km").SetWidth(90f).SetTextAlignment(Alignment.RIGHT),
                new Cell(f3, "Character").SetWidth(270f)});
        for (int i = 0; i < rivers.Length; i++) {
            rows.Add(new List<Cell> {
                    new Cell(f2, rivers[i]).SetWidth(180f),
                    new Cell(f2, lengthTexts[i]).SetWidth(90f).SetTextAlignment(Alignment.RIGHT),
                    new Cell(f2, characters[i]).SetWidth(270f)});
        }
        Table table = new Table();
        table.SetTableData(rows, 1);
        table.SetHeaderRowStyle(f3, 0x000000, 0xEEF0F3);
        table.SetCellBorderColor(0xC8CCD2);
        table.SetLocation(36f, 572f);
        table.DrawOn(page);
    }

    private void DrawVertical(PDF pdf) {
        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        // The heading of the document, above the charts
        new TextLine(f1, "Sales in 2025 and 2026")
                .SetStructureType(StructElem.H1)
                .SetFontSize(18f)
                .SetLocation(70f, 45f)
                .DrawOn(page);

        String[] months = {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        float[] units2025 = {45f, 65f, 31f, 45f, 65f, 31f, 38f, 52f, 47f, 59f, 66f, 72f};
        float[] units2026 = {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f};

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(70f, 70f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Units sold by month");
        chart.SetXAxisTitle("Month");
        chart.SetYAxisTitle("Units");
        chart.SetCategories(months);
        chart.AddSeries("2025", units2025);
        chart.AddSeries("2026", units2026);
        chart.SetGroupGap(0.4f);
        chart.SetBarGap(0.1f);
        chart.SetAltDescription("Units sold by month in 2025 and 2026, side by side: from 31 to 72 a month in 2025 and from 20 to 80 in 2026, the most in December in both years.");
        chart.DrawOn(page);

        BarChart stacked = new BarChart(f1, f2);
        stacked.SetLocation(70f, 420f);
        stacked.SetSize(500f, 300f);
        stacked.SetTitle("Units sold by month, stacked");
        stacked.SetXAxisTitle("Month");
        stacked.SetYAxisTitle("Units");
        stacked.SetCategories(months);
        stacked.AddSeries("2025", units2025);
        stacked.AddSeries("2026", units2026);
        stacked.SetStacked(true);
        stacked.SetDrawValueLabels(true);
        stacked.SetAltDescription("Units sold by month in 2025 and 2026, stacked: from 85 a month, in February and May, to 152 in December.");
        stacked.DrawOn(page);
    }

    private void DrawTarget(PDF pdf) {
        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        String[] months = {
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        float[] units2026 = {75f, 20f, 73f, 75f, 20f, 73f, 61f, 58f, 69f, 64f, 77f, 80f};
        float[] target = {60f, 60f, 62f, 64f, 66f, 68f, 68f, 68f, 70f, 72f, 75f, 80f};

        // The description of the chart is made from its data
        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(70f, 70f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Units sold by month in 2026, and the target");
        chart.SetXAxisTitle("Month");
        chart.SetYAxisTitle("Units");
        chart.SetCategories(months);
        chart.AddSeries("2026", units2026);
        chart.AddLineSeries("Target", target);
        chart.DrawOn(page);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_27();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_27 => {time1 - time0,4} ms");
    }
}   // End of Example_27.cs
