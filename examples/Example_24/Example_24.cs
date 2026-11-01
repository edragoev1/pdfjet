/*
 * Example_24.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_24.cs
 */
public class Example_24 {
    public Example_24(String fileName, bool pdfUA) {
        PDF pdf = new PDF(
            new BufferedStream(new FileStream("Example_24.pdf", FileMode.Create)));
        // The sample is a PDF/UA document: a BigTable is tagged as a table, a
        // TR for each row holding a TH or a TD with the text of each cell,
        // which is what a screen reader reads a table from. A file given as
        // the argument is left untagged, so that the benchmark stays
        // comparable with earlier releases: every tagged cell is an object of
        // its own, and the full data file goes from 5,108 objects and 11.8 MB
        // to 1.25 million objects and 249 MB.
        if (pdfUA) {
            pdf.SetCompliance(Compliance.PDF_UA_1);
        }
        pdf.SetTitle("Electric Vehicle Population Data");    // Required for PDF/UA !

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
        // The 10-page sample, or the file given as the first argument: the
        // full Electric Vehicle Population Data, 124,716 rows, makes 2,546
        // pages and is used for performance testing.
        String fileName = (args.Length > 0) ?
                args[0] : "resources/data/Electric_Vehicle_Population_10_Pages.csv";
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_24(fileName, args.Length == 0);
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_24 => {time1 - time0,4} ms");
    }
}   // End of Example_24.cs
