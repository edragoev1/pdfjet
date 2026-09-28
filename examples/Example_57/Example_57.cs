/*
 * Example_57.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_57.cs
 */
public class Example_57 {
    public Example_57() {
        PDF pdf = new PDF(
            new BufferedStream(new FileStream("Example_57.pdf", FileMode.Create)));
        // Example_43's table, cut to 550 rows, 12 pages, as a PDF/UA document: a BigTable is
        // tagged as a table, a TR for each row, holding a TH or a TD with the text
        // of each cell, which is what a screen reader reads a table from. It is
        // the size a PDF/UA checker such as PAC can open, which the 2,000 pages of
        // Example_43 are not.
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicle Population Data");    // Required for PDF/UA !

        String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        table.SetNumberOfColumns(9);        // The order of the
        table.SetTableData(fileName, ",");  // these statements
        // A heading on the first page only, which the table starts under
        Page first = new Page(pdf, Letter.LANDSCAPE);
        new TextLine(f1, "Electric Vehicle Population Data")
                .SetStructureType(StructElem.H1)
                .SetFontSize(14f)
                .SetLocation(10f, 24f)
                .DrawOn(first);
        table.SetFirstPage(first, 34f);
        table.SetLocation(0f, 0f);          // is
        table.SetBottomMargin(20f);         // very
        table.Complete();                   // important!

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_57();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_57 => {time1 - time0,4} ms");
    }
}   // End of Example_57.cs
