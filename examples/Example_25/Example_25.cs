/*
 * Example_25.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_25.cs
 */
public class Example_25 {
    public Example_25() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_25.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Fruit Donut Chart");

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(12.0f);
        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.SetSize(10.0f);

        DonutChart chart = new DonutChart(f1, f2);
        chart.SetLocation(100.0f, 200.0f);
        chart.SetRadii(200.0f, 120.0f);     // an inner radius of 0 makes a pie chart

        chart.AddSlice(new Slice(25f, 0xC1121F, "Apples"));    // deep red
        chart.AddSlice(new Slice(20f, 0x1D3557, "Oranges"));    // navy blue
        chart.AddSlice(new Slice(30f, 0x1A7468, "Bananas"));    // dark teal
        chart.AddSlice(new Slice(15f, 0xD97706, "Grapes"));    // burnt orange
        chart.AddSlice(new Slice(10f, 0xCAAA2F, "Lemons"));    // dark gold
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_25();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_25 => {time1 - time0,4} ms");
    }
}   // End of Example_25.cs
