/*
 * Example_20.cs
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
 * Example_20.cs
 * This example draws a page of results from a CSV file of electric vehicles:
 * two simple bar charts, the five makes and the model years, and under them a
 * table of the same numbers, then a table of one car with its photo and its
 * VIN as a barcode, in their cells.
 */
public class Example_20 {
    static readonly String[] MAKES = {"TESLA", "NISSAN", "CHEVROLET", "BMW", "FORD"};
    static readonly String[] NAMES = {"Tesla", "Nissan", "Chevrolet", "BMW", "Ford"};
    const int FIRST_YEAR = 2019;
    const int LAST_YEAR = 2023;

    public Example_20() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_20.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Electric Vehicles by Make and Model Year");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        // The cars of the file: by make, the battery electric ones and the
        // electric range of those that give one, and by model year
        List<String> lines = Content.LinesOfTextFile("resources/data/Electric_Vehicle_Population_10_Pages.csv");
        int[] cars = new int[MAKES.Length];
        int[] battery = new int[MAKES.Length];
        int[] ranges = new int[MAKES.Length];
        int[] ranged = new int[MAKES.Length];
        int[] years = new int[LAST_YEAR - FIRST_YEAR + 1];
        String[] modelX = null;     // The first Tesla Model X of the file
        for (int n = 1; n < lines.Count; n++) {
            String[] field = lines[n].Split(',');
            int year = int.Parse(field[5], CultureInfo.InvariantCulture);
            if (year >= FIRST_YEAR && year <= LAST_YEAR) {
                years[year - FIRST_YEAR]++;
            }
            int i = Array.IndexOf(MAKES, field[6]);
            if (i >= 0) {
                cars[i]++;
                if (field[8].StartsWith("Battery", StringComparison.Ordinal)) {
                    battery[i]++;
                }
                int range = int.Parse(field[10], CultureInfo.InvariantCulture);
                if (range > 0) {
                    ranges[i] += range;
                    ranged[i]++;
                }
            }
            if (modelX == null && field[7] == "MODEL X") {
                modelX = field;
            }
        }
        int total = lines.Count - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Electric Vehicles by Make and Model Year")
                .SetStructureType(StructElem.H1).SetFontSize(20f)
                .SetLocation(50f, 70f).DrawOn(page);
        List<Paragraph> intro = new List<Paragraph>();
        intro.Add(new Paragraph(new TextLine(f2, "The " + total + " electric vehicles of the first rows of "
                + "the data file that Example_24 draws whole: the five makes with the most cars, and the "
                + "cars of each model year from " + FIRST_YEAR + " to " + LAST_YEAR + ", with the numbers "
                + "in the table under the charts.").SetFontSize(10f)));
        TextFrame frame = new TextFrame(intro).SetLocation(50f, 84f).SetWidth(512f);
        frame.DrawOn(page);
        float y = frame.GetTextBottom() + 18f;

        SimpleBarChart byMake = new SimpleBarChart(f1, f2)
                .SetTitle("The five makes").SetSubtitle("cars").SetHighlight(0);
        for (int i = 0; i < MAKES.Length; i++) {
            byMake.Add(NAMES[i], cars[i]);
        }
        byMake.SetLocation(50f, y).SetWidth(248f).DrawOn(page);

        // The chart highlights the busiest year itself, and marks the
        // average, in whole cars, with a dashed line; its bars are rounded
        SimpleBarChart byYear = new SimpleBarChart(f1, f2)
                .SetTitle("By model year").SetSubtitle("cars")
                .SetHighlightHighest().SetRoundedBars(true);
        int allYears = 0;
        for (int i = 0; i < years.Length; i++) {
            byYear.Add((FIRST_YEAR + i).ToString(CultureInfo.InvariantCulture), years[i]);
            allYears += years[i];
        }
        byYear.SetReferenceLine(allYears / years.Length, "Average");
        byYear.SetLocation(314f, y).SetWidth(248f).DrawOn(page);
        y += Math.Max(byMake.GetHeight(), byYear.GetHeight()) + 30f;

        // The numbers of the charts
        new TextLine(f1, "The five makes").SetStructureType(StructElem.H2)
                .SetFontSize(14f).SetLocation(50f, y).DrawOn(page);
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(Row(f1, true, "Make", "Cars", "Share of all", "Battery electric", "Average range"));
        for (int i = 0; i < MAKES.Length; i++) {
            data.Add(Row(f2, false, NAMES[i], cars[i].ToString(CultureInfo.InvariantCulture),
                    (100f * cars[i] / total).ToString("F1", CultureInfo.InvariantCulture) + "%",
                    battery[i].ToString(CultureInfo.InvariantCulture),
                    ranged[i] == 0 ? "" : (ranges[i] / ranged[i]).ToString(CultureInfo.InvariantCulture) + " mi"));
        }
        Table table = new Table();
        table.SetTableData(data, 1);
        float[] widths = {142f, 80f, 90f, 100f, 100f};
        for (int i = 0; i < widths.Length; i++) {
            table.SetColumnWidth(i, widths[i]);
        }
        table.SetLocation(50f, y + 10f);
        y = table.DrawOn(page)[1] + 30f;

        // One car of the data, its photo and its VIN in their cells
        new TextLine(f1, "One car of the data").SetStructureType(StructElem.H2)
                .SetFontSize(14f).SetLocation(50f, y).DrawOn(page);
        Image image = new Image(pdf, "images/TeslaX.png");
        image.SetAltDescription("A white Tesla Model X with a black glass roof, seen from the front left.");
        image.ScaleBy(1f / 3f);     // The image is 120 DPI, so 0.6 of its pixels
        Barcode vin = new Barcode(Barcode.CODE_128, modelX[0]);
        vin.SetModuleLength(0.75f);
        vin.SetFont(f2);
        data = new List<List<Cell>>();
        data.Add(Row(f1, true, "Car", "Model", "VIN"));
        foreach (Cell cell in data[0]) {
            cell.SetTextAlignment(Alignment.LEFT);      // Text, not numbers
        }
        List<Cell> car = Row(f2, false, "", "Tesla Model X, " + modelX[5] + ", " + modelX[2] + ", "
                + modelX[10] + " mi of range", "");
        foreach (Cell cell in car) {
            cell.SetTextAlignment(Alignment.LEFT);
        }
        car[0].SetImage(image);
        car[2].SetBarcode(vin);
        data.Add(car);
        table = new Table();
        table.SetTableData(data, 1);
        table.SetColumnWidth(0, image.GetWidth() + 8f);
        table.SetColumnWidth(1, 220f);
        table.SetColumnWidth(2, 512f - image.GetWidth() - 228f);
        table.SetLocation(50f, y + 10f);
        table.DrawOn(page);

        pdf.Complete();
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the charts: a light band behind the header row, a thin gray
    // line under each row, and the numbers, the cells after the first,
    // aligned right.
    static List<Cell> Row(Font font, bool header, params String[] texts) {
        List<Cell> row = new List<Cell>();
        for (int i = 0; i < texts.Length; i++) {
            Cell cell = new Cell(font, texts[i]).SetFontSize(9f);
            cell.SetTopPadding(5f).SetBottomPadding(5f).SetLeftPadding(6f).SetRightPadding(6f);
            cell.SetBorders(false).SetBorder(Border.BOTTOM, true);
            cell.SetBorderWidth(0.5f).SetBorderColor(0xD5D9DE);
            if (header) {
                cell.SetBackgroundColor(0xF1F3F6);
            }
            if (i > 0) {
                cell.SetTextAlignment(Alignment.RIGHT);
            }
            row.Add(cell);
        }
        return row;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_20();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_20 => {time1 - time0,4} ms");
    }
}   // End of Example_20.cs
