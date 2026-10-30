/*
 * Example_26.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PDFjet.NET;

/**
 * Example_26.cs
 * This example draws a SimpleScatterChart of the life expectancy and the
 * children per woman of 217 countries and economies, from the World Bank's
 * data of 2023: a series for each of the World Bank's seven regions, in its
 * color and shape, each point a link to its row
 * of a table on pdfjet.com, with its life expectancy and children per woman,
 * described by its figures, and the trend line, with a table of seven of them
 * under the chart.
 */
public class Example_26 {
    // Seven of the countries, by their codes
    static readonly List<String> NAMED = new List<String> {"BRA", "CAN", "IND", "JPN", "NER", "NGA", "USA"};
    // The World Bank's regions, a series each, in the order of its list
    static readonly String[] REGIONS = {
        "East Asia & Pacific",
        "Europe & Central Asia",
        "Latin America & Caribbean",
        "Middle East, North Africa, Afghanistan & Pakistan",
        "North America",
        "South Asia",
        "Sub-Saharan Africa"
    };

    public Example_26() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_26.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Life Expectancy and Children per Woman");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        // Country|Code|Life expectancy|Children per woman|Region|ISO2, after a header line
        List<String> lines = Content.LinesOfTextFile("resources/data/life-expectancy-and-fertility-2023.txt");
        int total = lines.Count - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Life Expectancy and Children per Woman")
                .SetStructureType(StructElem.H1).SetFontSize(20f)
                .SetLocation(50f, 70f).DrawOn(page);
        List<Paragraph> intro = new List<Paragraph>();
        intro.Add(new Paragraph(new TextLine(f2, "Where women have fewer children, people live longer: the "
                + total + " countries and economies of the World Bank's data of 2023, in a color and "
                + "a shape for each of its seven regions, each point a link to its row of a table on pdfjet.com, with its life expectancy and children per woman, and the trend line "
                + "through them, with seven of them in the table "
                + "under the chart.").SetFontSize(10f)));
        TextFrame frame = new TextFrame(intro).SetLocation(50f, 84f).SetWidth(512f);
        frame.DrawOn(page);
        float y = frame.GetTextBottom() + 18f;

        SimpleScatterChart chart = new SimpleScatterChart(f1, f2)
                .SetTitle("Life expectancy and children per woman")
                .SetSubtitle(total + " countries and economies, 2023")
                .SetXAxisTitle("Children per woman")
                .SetYAxisTitle("Life expectancy at birth, years")
                .SetTrendLine(true);
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(Row(f1, true, "Country", "Children per woman", "Life expectancy"));
        foreach (String region in REGIONS) {
            chart.AddSeries(region);
            for (int n = 1; n < lines.Count; n++) {
                String[] field = lines[n].Split('|');
                if (!field[4].Equals(region)) {
                    continue;
                }
                float life = float.Parse(field[2], CultureInfo.InvariantCulture);
                float children = float.Parse(field[3], CultureInfo.InvariantCulture);
                String uri = "https://pdfjet.com/countries.html#" + field[5];
                // The outline of its shape, a link described by its figures
                String name = field[0] + ": " + Decimals(field[2], 1) + " years, "
                        + Decimals(field[3], 2) + " children per woman";
                chart.AddLinkedDot(name, children, life, uri);
            }
        }
        for (int n = 1; n < lines.Count; n++) {
            String[] field = lines[n].Split('|');
            if (NAMED.Contains(field[1])) {
                // The figures as the file writes them, two decimals and one,
                // not rounded again, so that every port writes them alike
                data.Add(Row(f2, false, field[0], Decimals(field[3], 2), Decimals(field[2], 1) + " years"));
            }
        }
        chart.SetLocation(50f, y).SetSize(512f, 340f);
        y = chart.DrawOn(page)[1] + 30f;

        // Seven of the countries
        new TextLine(f1, "Seven of the countries").SetStructureType(StructElem.H2)
                .SetFontSize(14f).SetLocation(50f, y).DrawOn(page);
        Table table = new Table();
        table.SetTableData(data, 1);
        table.SetColumnWidth(0, 232f);
        table.SetColumnWidth(1, 140f);
        table.SetColumnWidth(2, 140f);
        table.SetLocation(50f, y + 10f);
        y = table.DrawOn(page)[1] + 16f;

        new TextLine(f2, "Source: World Bank, World Development Indicators, SP.DYN.LE00.IN and "
                + "SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0.")
                .SetFontSize(8f).SetTextColor(0x5F6873).SetLocation(50f, y).DrawOn(page);

        pdf.Complete();
    }

    // A row of cells of the font at 9 points, one for each text, in the
    // look of the chart: a light band behind the header row, a thin gray
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

    // The number of the text with the decimals, zeros added: 1.5 is 1.50.
    private static String Decimals(String number, int count) {
        int point = number.IndexOf('.');
        StringBuilder sb = new StringBuilder(point < 0 ? number + "." : number);
        int have = (point < 0) ? 0 : number.Length - point - 1;
        for (int i = have; i < count; i++) {
            sb.Append('0');
        }
        return sb.ToString();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_26();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_26 => {time1 - time0,4} ms");
    }
}   // End of Example_26.cs
