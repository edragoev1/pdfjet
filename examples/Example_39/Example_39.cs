/*
 * Example_39.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_39.cs
 *
 * Draws a horizontal bar chart of the ten longest rivers, each bar in its own
 * color with its length written inside it, under a title and a subtitle, and
 * a color key and a source note under the chart.
 */
public class Example_39 {
    public Example_39() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_39.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("10 Longest Rivers in the World");

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(15f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        Font f3 = new Font(pdf, IBMPlexSans.Bold);
        f3.SetSize(9f);

        Font f4 = new Font(pdf, IBMPlexSans.Regular);
        f4.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        String[] rivers = {
                "Nile", "Amazon", "Yangtze", "Mississippi-Missouri", "Yenisey-Baikal-Selenga",
                "Huang He (Yellow)", "Ob-Irtysh", "Paraná", "Congo", "Amur"};
        float[] lengths = {6650f, 6400f, 6300f, 5971f, 5540f, 5464f, 5410f, 4880f, 4700f, 4444f};
        int[] colors = {
                0x5b9bd5, 0x6b8e6b, 0x8b6f47, 0x9b7b5a, 0x6fa8dc,
                0xd4a017, 0x8fa98f, 0xa08060, 0x5c5c5c, 0x8b7355};

        BarChart chart = new BarChart(f1, f2);
        chart.SetLocation(36f, 40f);
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
                .SetTextColor(gray).SetLocation(171f, 466f).DrawOn(page);
        int[] keyColors = {0x5b9bd5, 0x6b8e6b, 0xa08060, 0xd4a017, 0x5c5c5c};
        String[] keyTexts = {
                "Clear / low sediment", "Sediment-rich, relatively clean", "Polluted / industrial & agricultural",
                "Heavy natural sediment (loess)", "Natural dark tannin stain (Congo)"};
        float[] keyX = {171f, 262f, 398f, 171f, 313f};
        float[] keyY = {482f, 482f, 482f, 497f, 497f};
        for (int i = 0; i < keyColors.Length; i++) {
            // The swatches are rectangles, drawn as artifacts next to their text.
            new Rect(keyX[i], keyY[i] - 8.5f, 10.5f, 10.5f).SetFillColor(keyColors[i]).DrawOn(page);
            new TextLine(f4, keyTexts[i])
                    .SetTextColor(gray).SetLocation(keyX[i] + 15f, keyY[i]).DrawOn(page);
        }

        String note = "Color mapping is illustrative; lengths and conditions vary by source and season.";
        new TextLine(f4, note)
                .SetTextColor(0x999999).SetLocation(576f - f4.StringWidth(note), 520f).DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_39();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_39 => {time1 - time0,4} ms");
    }
}   // End of Example_39.cs
