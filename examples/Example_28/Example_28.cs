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
