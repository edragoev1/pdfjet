/*
 * Example_17.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_17.cs
 *
 * Draws a line chart of two series over the same years: a Chart with a Series
 * for each line, drawn with setDrawPath so the points are joined, and the
 * markers left visible so each year can be read off the line.
 */
public class Example_17 {
    // The vehicles of data/Electric_Vehicle_Population_Data.csv by model year
    // and kind, which Example_43 draws as a table: the rows of the file whose
    // "Electric Vehicle Type" begins with "Battery Electric" and with
    // "Plug-in Hybrid", counted by the "Model Year" column. The file holds
    // 124,716 rows, of which 124,419 are registered in Washington State, and
    // model years from 1997; the years before 2011 are a few dozen vehicles
    // in all and are left out here.
    private static readonly int[] YEARS =
            {2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023};
    private static readonly int[] BATTERY =
            { 753,  798, 2936, 1805, 3612, 3891, 4458, 9946, 8576, 9315, 14755, 23507, 11853};
    private static readonly int[] HYBRID =
            {  75,  870, 1645, 1804, 1323, 1811, 4100, 4278, 1874, 1611,  3541,  4015,  1500};

    public Example_17() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_17.pdf", FileMode.Create)));
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
                "A line chart is a Chart with a Series for each line: the points of the " +
                "series are added in the order they are joined, and setDrawPath draws " +
                "the line through them. The markers are left visible here so that the " +
                "count for each model year can be read off the line; setShape with " +
                "Shape.INVISIBLE leaves the line alone.\n\n" +
                "The counts are the vehicles of " +
                "data/Electric_Vehicle_Population_Data.csv, the file Example_43 draws " +
                "as a table of 2,546 pages, by model year and by kind.");
        textBlock.SetFontSize(11f);
        textBlock.SetLineSpacing(1.4f);
        textBlock.SetLocation(50f, 72f);
        textBlock.SetWidth(512f);
        float[] xy = textBlock.DrawOn(page);

        f1.SetSize(12f);
        f2.SetSize(9f);

        Chart chart = new Chart(f1, f2);
        chart.SetLocation(70f, xy[1] + 30f);
        chart.SetSize(480f, 340f);
        chart.SetTitle("Battery electric and plug-in hybrid vehicles");
        chart.SetSubtitle("Registrations in the Washington State data file");
        chart.SetXAxisTitle("Model year");
        chart.SetYAxisTitle("Vehicles");
        // The years and the counts are whole numbers, so the axis labels are.
        chart.SetMaximumFractionDigits(0);
        // The axes are set rather than worked out from the points, so that
        // every label of the years is a year: 2011 to 2023 in six steps is one
        // every two years, where the range a chart picks for itself would be
        // 2010 to 2025 in steps of two and a half.
        chart.SetXAxisMinMax(2011f, 2023f, 6);
        chart.SetYAxisMinMax(0f, 25000f, 5);
        chart.SetAltDescription(
                "A line chart of the vehicles of the data file by model year, from 2011 " +
                "to 2023. Battery electric vehicles rise from 753 in 2011 to 23,507 in " +
                "2022 and fall to 11,853 in 2023, the last model year of the file. " +
                "Plug-in hybrids stay far lower, from 75 in 2011 to a high of 4,278 in " +
                "2018 and 1,500 in 2023.");

        Series battery = chart.AddSeries("Battery electric")
                .SetDrawPath(true)
                .SetStrokeColor(0x1D3557)       // navy blue
                .SetStrokeWidth(1.5f);
        for (int i = 0; i < YEARS.Length; i++) {
            battery.AddPoint(new Point(YEARS[i], BATTERY[i]));
        }

        Series hybrid = chart.AddSeries("Plug-in hybrid")
                .SetDrawPath(true)
                .SetStrokeColor(0xC1121F)       // deep red
                .SetStrokeWidth(1.5f);
        for (int i = 0; i < YEARS.Length; i++) {
            hybrid.AddPoint(new Point(YEARS[i], HYBRID[i]));
        }

        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(string[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_17();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_17 => {time1 - time0,4} ms");
    }
}   // End of Example_17.cs
