/*
 * Example_43.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_43.cs
 */
public class Example_43 {
    public Example_43() {
        PDF pdf = new PDF(
            new BufferedStream(new FileStream("Example_43.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicle Population Data");    // Required for PDF/UA !

        // The whole file is 2000+ pages and is the one to time the library with.
        String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";
        // String fileName = "data/Electric_Vehicle_Population_Data.csv";

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        table.SetNumberOfColumns(9);        // The order of the
        table.SetTableData(fileName, ",");  // these statements
        table.SetLocation(0f, 0f);          // is
        table.SetBottomMargin(20f);         // very
        table.Complete();                   // important!

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_43();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_43 => {time1 - time0,4} ms");
    }
}   // End of Example_43.cs
