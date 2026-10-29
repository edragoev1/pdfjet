/*
 * Example_25.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    // The vehicles of the Washington State Electric Vehicle Population Data
    // by model year and kind, summarized in
    // resources/data/Electric_Vehicles_by_Model_Year.csv: the rows of the data
    // whose "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The data holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private const int FIRST_YEAR = 2011;

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
                "The counts are of the Washington State Electric Vehicle Population " +
                "Data, summarized by model year and by kind in " +
                "resources/data/Electric_Vehicles_by_Model_Year.csv.");
        textBlock.SetFontSize(11f);
        textBlock.SetLineSpacing(1.4f);
        textBlock.SetLocation(50f, 72f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        f1.SetSize(12f);
        f2.SetSize(9f);

        // The model years from FIRST_YEAR on, after the header line
        List<String[]> rows = new List<String[]>();
        List<String> lines = Content.LinesOfTextFile("resources/data/Electric_Vehicles_by_Model_Year.csv");
        for (int n = 1; n < lines.Count; n++) {
            String[] field = lines[n].Split(',');
            if (int.Parse(field[0], CultureInfo.InvariantCulture) >= FIRST_YEAR) {
                rows.Add(field);
            }
        }
        String[] years = new String[rows.Count];
        float[] battery = new float[rows.Count];
        float[] hybrid = new float[rows.Count];
        for (int i = 0; i < rows.Count; i++) {
            years[i] = rows[i][0];
            battery[i] = float.Parse(rows[i][1], CultureInfo.InvariantCulture);
            hybrid[i] = float.Parse(rows[i][2], CultureInfo.InvariantCulture);
        }
        new SimpleLineChart(f1, f2)
                .SetTitle("Battery electric and plug-in hybrid vehicles")
                .SetSubtitle("By model year, in the Washington State data file")
                .SetYAxisTitle("Vehicles")
                .SetCategories(years)
                .AddLine("Battery electric", battery)
                .AddLine("Plug-in hybrid", hybrid)
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
                .AddLine("Plug-in hybrid", hybrid)
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
