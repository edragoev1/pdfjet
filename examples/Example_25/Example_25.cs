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
 *
 * Draws a SimpleLineChart of two lines over the same years: the vehicles of
 * the data file by model year, battery electric highlighted in navy, each
 * line labeled at its end with its last count, so that the chart needs no
 * legend, and a dot at each year so that each count can be read off its line.
 * The second page draws the plug-in hybrids alone as an area chart, the area
 * under the line filled and each count written above its year.
 */
public class Example_25 {
    // The vehicles of data/Electric_Vehicle_Population_Data.csv by model year
    // and kind, which Example_24 draws as a table: the rows of the file whose
    // "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The file holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private static readonly int[] YEARS =
            {2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023};
    private static readonly float[] BATTERY =
            { 753,  798, 2936, 1805, 3612, 3891, 4458, 9946, 8576, 9315, 14755, 23507, 11853};
    private static readonly float[] HYBRID =
            {  75,  870, 1645, 1804, 1323, 1811, 4100, 4278, 1874, 1611,  3541,  4015,  1500};

    public Example_25() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_25.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicles by Model Year");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f1, "Electric Vehicles by Model Year");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(18f);
        title.SetLocation(50f, 50f);
        title.DrawOn(page);

        TextBlock textBlock = new TextBlock(f2,
                "A SimpleLineChart draws a few lines over the same categories, here " +
                "the model years, in their order left to right. Each line is labeled " +
                "at its end with its name and its last value, so the chart needs no " +
                "legend, and one line is highlighted in navy, the other gray. The dots " +
                "of setMarkers show the count for each model year. In a PDF/UA " +
                "document the chart describes itself from its data: each line's first " +
                "and last values and its highest.\n\n" +
                "The counts are the vehicles of " +
                "data/Electric_Vehicle_Population_Data.csv, the file Example_24 draws " +
                "as a table of 2,546 pages, by model year and by kind.");
        textBlock.SetFontSize(11f);
        textBlock.SetLineSpacing(1.4f);
        textBlock.SetLocation(50f, 72f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        f1.SetSize(12f);
        f2.SetSize(9f);

        String[] years = new String[YEARS.Length];
        for (int i = 0; i < YEARS.Length; i++) {
            years[i] = YEARS[i].ToString();
        }
        new SimpleLineChart(f1, f2)
                .SetTitle("Battery electric and plug-in hybrid vehicles")
                .SetSubtitle("By model year, in the Washington State data file")
                .SetYAxisTitle("Vehicles")
                .SetCategories(years)
                .AddLine("Battery electric", BATTERY)
                .AddLine("Plug-in hybrid", HYBRID)
                .SetHighlight(0)
                .SetMarkers(true)
                .SetLocation(50f, xy[1] + 30f)
                .SetSize(512f, 340f)
                .DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        new SimpleLineChart(f1, f2)
                .SetTitle("Plug-in hybrid vehicles")
                .SetSubtitle("By model year, the area under the line filled and each count written")
                .SetYAxisTitle("Vehicles")
                .SetCategories(years)
                .AddLine("Plug-in hybrid", HYBRID)
                .SetHighlight(0)
                .SetMarkers(true)
                .SetAreaFilled(true)
                .SetValueLabels(true)
                .SetLocation(50f, 50f)
                .SetSize(512f, 300f)
                .DrawOn(page);

        pdf.Complete();
    }

    public static void Main(string[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_25();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_25 => {time1 - time0,4} ms");
    }
}   // End of Example_25.cs
