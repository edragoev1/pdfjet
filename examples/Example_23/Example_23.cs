/*
 * Example_23.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_23.cs
 *
 * A table of PDF/A-1a, tagged, over four pages: the life expectancy and the
 * children per woman of 217 countries and economies in 2023, from the World
 * Bank's World Development Indicators, with a marker on three of the rows,
 * each a link, and the longest and the shortest life in color.
 */
public class Example_23 {
    public Example_23() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_23.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_A_1A);
        pdf.SetTitle("Life Expectancy and Children per Woman, 2023");

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        Font f3 = new Font(pdf, IBMPlexSans.BoldItalic);

        f1.SetSize(7f);
        f2.SetSize(7f);
        f3.SetSize(7f);

        Table table = new Table();
        // Country|Code|Life expectancy|Children per woman, after a header line
        List<List<Cell>> tableData = GetData(
                "resources/data/life-expectancy-and-fertility-2023.txt", "|", 1, f1, f2);

        // A marker on three rows, found by their countries, each a link
        Point p1 = new Point();
        p1.SetShape(Shape.CIRCLE);
        p1.SetRadius(2f);
        p1.SetStrokeColor(Color.darkolivegreen);
        p1.SetURIAction("https://data.worldbank.org/country/india");
        tableData[RowOf(tableData, "India")][3].SetMarker(p1, Alignment.RIGHT);

        p1 = new Point();
        p1.SetShape(Shape.DIAMOND);
        p1.SetRadius(2.5f);
        p1.SetStrokeColor(Color.blue);
        p1.SetURIAction("https://data.worldbank.org/country/japan");
        tableData[RowOf(tableData, "Japan")][3].SetMarker(p1, Alignment.RIGHT);

        p1 = new Point();
        p1.SetShape(Shape.STAR);
        p1.SetRadius(3f);
        p1.SetStrokeColor(Color.red);
        p1.SetURIAction("https://data.worldbank.org/country/united-states");
        tableData[RowOf(tableData, "United States")][3].SetMarker(p1, Alignment.RIGHT);

        // The longest life in blue, the shortest in red, and Canada in bold italic
        int longest = 1;
        int shortest = 1;
        for (int i = 1; i < tableData.Count; i++) {
            float life = Life(tableData, i);
            if (life > Life(tableData, longest)) {
                longest = i;
            }
            if (life < Life(tableData, shortest)) {
                shortest = i;
            }
        }

        table.SetTableData(tableData, 1);
        table.SetBottomMargin(26f);
        // 400 points wide, in the middle of the page
        table.SetLocation(106f, 30f);
        table.SetTextColorInRow(longest, Color.blue);
        table.SetTextColorInRow(shortest, Color.firebrick);
        table.SetFontInRow(RowOf(tableData, "Canada"), f3);
        table.SetColumnWidth(0, 170f);
        table.SetColumnWidth(1, 50f);
        table.SetColumnWidth(2, 90f);
        table.SetColumnWidth(3, 90f);
        table.RightAlignNumbers();

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f2, "Source: World Bank, World Development Indicators, "
                    + "SP.DYN.LE00.IN and SP.DYN.TFRT.IN, 2023, licensed under CC BY 4.0. "
                    + "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    // The row of the country, in the first column.
    private static int RowOf(List<List<Cell>> tableData, String country) {
        for (int i = 0; i < tableData.Count; i++) {
            if (tableData[i][0].GetText() == country) {
                return i;
            }
        }
        throw new ArgumentException(country + " is not in the table");
    }

    // The life expectancy of the row.
    private static float Life(List<List<Cell>> tableData, int row) {
        return float.Parse(tableData[row][2].GetText(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public List<List<Cell>> GetData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2) {
        List<List<Cell>> tableData = new List<List<Cell>>();

        int currentRow = 0;
        StreamReader reader = new StreamReader(fileName);
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            List<Cell> row = new List<Cell>();
            String[] cols = null;
            if (delimiter.Equals("|")) {
                cols = line.Split(new Char[] {'|'});
            } else if (delimiter.Equals("\t")) {
                cols = line.Split(new Char[] {'\t'});
            } else {
                throw new Exception(
                        "Only pipes and tabs can be used as delimiters");
            }
            // The first five fields: not the two-letter code at the end
            for (int i = 0; i < Math.Min(cols.Length, 5); i++) {
                String text = cols[i].Trim();
                Cell cell = null;
                if (currentRow < numOfHeaderRows) {
                    cell = new Cell(f1, text);
                } else {
                    cell = new Cell(f2, text);
                }
                cell.SetTopPadding(2f);
                cell.SetBottomPadding(2f);
                cell.SetLeftPadding(2f);
                if (i == 3) {
                    cell.SetRightPadding(10f);
                } else {
                    cell.SetRightPadding(2f);
                }
                row.Add(cell);
            }
            tableData.Add(row);
            currentRow++;
        }
        reader.Close();
        AppendMissingCells(tableData, f2);

        return tableData;
    }


    private void AppendMissingCells(List<List<Cell>> tableData, Font f2) {
        List<Cell> firstRow = tableData[0];
        int numOfColumns = firstRow.Count;
        for (int i = 0; i < tableData.Count; i++) {
            List<Cell> dataRow = tableData[i];
            int dataRowColumns = dataRow.Count;
            if (dataRowColumns < numOfColumns) {
                for (int j = 0; j < (numOfColumns - dataRowColumns); j++) {
                    dataRow.Add(new Cell(f2));
                }
                dataRow[dataRowColumns - 1].SetColSpan(
                        (numOfColumns - dataRowColumns) + 1);
            }
        }
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_23();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_23 => {time1 - time0,4} ms");
    }
}   // End of Example_23.cs
