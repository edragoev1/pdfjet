/*
 * Example_28.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_28.cs
 */
public class Example_28 {
    public Example_28() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_28.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Fruit Donut Chart");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.SetSize(10.0f);

        // A heading, which PAC asks a document for
        new TextLine(f2, "Fruit Donut Chart")
                .SetStructureType(StructElem.H1)
                .SetFontSize(18f)
                .SetLocation(100f, 100f)
                .DrawOn(page);

        DonutChart chart = new DonutChart(f1, f2);
        chart.SetLocation(100.0f, 200.0f);
        chart.SetRadii(200.0f, 120.0f);     // an inner radius of 0 makes a pie chart

        chart.SetTitle("Fruit sold this week");
        chart.SetSubtitle("Share of 100 crates, in the default colors");
        chart.AddSlice(new Slice(25f, "Apples"));
        chart.AddSlice(new Slice(20f, "Oranges"));
        chart.AddSlice(new Slice(30f, "Bananas"));
        chart.AddSlice(new Slice(15f, "Grapes"));
        chart.AddSlice(new Slice(10f, "Lemons"));
        chart.DrawOn(page);

        // Many slices, some small: a legend under the circle in place of the
        // leader lines, and the total in the hole
        page = new Page(pdf, Letter.PORTRAIT);
        DonutChart many = new DonutChart(f1, f2);
        many.SetLocation(100.0f, 100.0f);
        many.SetRadii(150.0f, 90.0f);
        many.SetTitle("Fruit sold this month");
        many.SetSubtitle("Share of 100 crates, eight kinds, a color each");
        many.SetCenterText("100 crates");
        many.SetLegend(true);
        String[] kinds = {"Apples", "Bananas", "Oranges", "Grapes", "Lemons",
                "Pears", "Plums", "Cherries"};
        float[] crates = {31f, 24f, 17f, 10f, 7f, 5f, 4f, 2f};
        for (int i = 0; i < kinds.Length; i++) {
            many.AddSlice(new Slice(crates[i], kinds[i]));
        }
        many.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_28();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_28 => {time1 - time0,4} ms");
    }
}   // End of Example_28.cs
