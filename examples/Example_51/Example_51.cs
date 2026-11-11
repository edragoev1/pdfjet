/*
 * Example_51.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_51.cs
 *
 * Draws eight columns of the Electric Vehicle data of Example_24 on portrait
 * pages, with SetWrapText: a field too long for its column wraps onto more
 * lines, the row as tall as its tallest field, so no column is cut short and
 * none is lost off the page; the memory stays flat, a row at a time.
 */
public class Example_51 {
    public Example_51() {
        PDF pdf = new PDF(
            new BufferedStream(new FileStream("Example_51.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicle Population Data, wrapped");  // Required for PDF/UA !

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(9f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.PORTRAIT);
        // Make, Model, Model Year, City, Electric Vehicle Type, Clean Alternative
        // Fuel Vehicle (CAFV) Eligibility, Electric Range and Electric Utility
        table.SetColumns(6, 7, 5, 2, 8, 9, 10, 15);
        table.SetWrapText(true);
        table.SetTableData("resources/data/Electric_Vehicle_Population_10_Pages.csv", ",");
        table.SetVerticalPadding(2f);
        table.SetLocation(20f, 20f);
        table.SetBottomMargin(30f);
        table.Complete();

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_51();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_51 => {time1 - time0,4} ms");
    }
}   // End of Example_51.cs
