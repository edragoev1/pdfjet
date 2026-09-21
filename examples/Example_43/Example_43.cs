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
        // Uncomment the line below to make this a PDF/UA document. A BigTable
        // is tagged as a table: a TR for each row, holding a TH or a TD with
        // the text of each cell, which is what a screen reader reads a table
        // from. It is off here because of what it costs at this size: every
        // tagged cell is an object of its own, so this document goes from
        // 5,108 objects and 11.8 MB to 1.25 million objects and 249 MB. The
        // 10-page file below is the size to see the tagging at.
        // pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicle Population Data");    // Required for PDF/UA !

        // Used for performance testing. Results in 2000+ pages PDF.
        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

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
