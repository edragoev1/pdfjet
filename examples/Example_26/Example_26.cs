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
 * data of 2023: a gray dot for each, seven of them named, and the trend line,
 * with a table of the named ones under the chart.
 */
public class Example_26 {
    // The countries named on the chart, by their codes
    static readonly List<String> NAMED = new List<String> {"BRA", "CAN", "IND", "JPN", "NER", "NGA", "USA"};

    public Example_26() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_26.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Life Expectancy and Children per Woman");

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.SetSize(12f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(9f);

        // Country|Code|Life expectancy|Children per woman, after a header line
        List<String> lines = Content.LinesOfTextFile("data/life-expectancy-and-fertility-2023.txt");
        int total = lines.Count - 1;

        Page page = new Page(pdf, Letter.PORTRAIT);

        new TextLine(f1, "Life Expectancy and Children per Woman")
                .SetStructureType(StructElem.H1).SetFontSize(20f)
                .SetLocation(50f, 70f).DrawOn(page);
        List<Paragraph> intro = new List<Paragraph>();
        intro.Add(new Paragraph(new TextLine(f2, "Where women have fewer children, people live longer: the "
                + total + " countries and economies of the World Bank's data of 2023, each a gray dot, "
                + "seven of them named, each a link to its page of the World Bank's data, and the trend line "
                + "through them, with the named ones in the table "
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
        for (int n = 1; n < lines.Count; n++) {
            String[] field = lines[n].Split('|');
            float life = float.Parse(field[2], CultureInfo.InvariantCulture);
            float children = float.Parse(field[3], CultureInfo.InvariantCulture);
            if (NAMED.Contains(field[1])) {
                chart.Add(field[0], children, life, WorldBank(field[0]));  // A navy dot, named, a link
                // The figures as the file writes them, two decimals and one,
                // not rounded again, so that every port writes them alike
                data.Add(Row(f2, false, field[0], Decimals(field[3], 2), Decimals(field[2], 1) + " years"));
            } else {
                chart.Add(children, life);              // A gray dot
            }
        }
        chart.SetLocation(50f, y).SetSize(512f, 340f);
        y = chart.DrawOn(page)[1] + 30f;

        // The countries named on the chart
        new TextLine(f1, "The countries named on the chart").SetStructureType(StructElem.H2)
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

    // The page of the country in the World Bank's data: its name in lower
    // case, a hyphen for each space, as data.worldbank.org/country/united-states
    static String WorldBank(String country) {
        return "https://data.worldbank.org/country/" + country.ToLowerInvariant().Replace(' ', '-');
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
