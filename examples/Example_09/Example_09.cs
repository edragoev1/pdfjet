/*
 * Example_09.cs
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
 * Example_09.cs
 *
 * Draws an XY chart of the countries of a data file, each a marker that links
 * to a page about the country, with the trend line of the points, and a table
 * of the countries with a marker of their own.
 */
public class Example_09 {
    /** A country of the data file: its name and its point on the chart. */
    private sealed class Country {
        internal readonly String name;
        internal readonly Point point;
        internal Country(String name, Point point) {
            this.name = name;
            this.point = point;
        }
    }

    public Example_09() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_09.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(8f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Country> countries = ReadCountries("data/world-communications.txt", "|");

        Chart chart = new Chart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("World View - Communications");
        chart.SetXAxisTitle("Cell phones per capita");
        chart.SetYAxisTitle("Internet users % of the population");
        Series markers = chart.AddSeries("").SetStrokeColor(Color.gray);
        foreach (Country country in countries) {
            markers.AddPoint(country.point);
        }
        AddTrendLine(chart, countries);
        chart.DrawOn(page);

        f1.SetSize(7f);
        f2.SetSize(7f);
        AddTableToChart(page, countries, f1, f2);

        pdf.Complete();
    }

    private void AddTrendLine(Chart chart, List<Country> countries) {
        List<Point> points = new List<Point>();
        foreach (Country country in countries) {
            points.Add(country.point);
        }
        float m = Slope(points);
        float b = Intercept(points, m);
        chart.AddSeries("Trend line")
                .SetDrawPath(true)
                .SetStrokeColor(Color.blue)
                .SetShape(Shape.INVISIBLE)
                .AddPoint(0f, b)
                .AddPoint(1.5f, m * 1.5f + b);
    }

    private void AddTableToChart(
            Page page, List<Country> countries, Font f1, Font f2) {
        Table table = new Table();
        List<List<Cell>> tableData = new List<List<Cell>>();
        foreach (Country country in countries) {
            if (country.point.GetShape() != Shape.CIRCLE) {
                List<Cell> tableRow = new List<Cell>();

                Cell cell = new Cell(f2);
                cell.SetMarker(country.point, Alignment.LEFT);
                cell.SetText("");
                tableRow.Add(cell);

                cell = new Cell(f1);
                cell.SetText(country.name);
                tableRow.Add(cell);

                cell = new Cell(f2);
                cell.SetText(country.point.GetURIAction());
                tableRow.Add(cell);

                tableData.Add(tableRow);
            }
        }
        table.SetTableData(tableData);
        table.AutoAdjustColumnWidths();
        table.SetCellBorderWidth(0.2f);
        table.SetLocation(70f, 360f);
        table.SetColumnWidth(0, 9f);
        table.DrawOn(page);
    }

    private List<Country> ReadCountries(
            String fileName,
            String delimiter) {
        List<Country> countries = new List<Country>();
        StreamReader reader = new StreamReader(fileName);
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            String[] cols = null;
            if (delimiter.Equals("|")) {
                cols = line.Split(new Char[] {'|'});
            } else if (delimiter.Equals("\t")) {
                cols = line.Split(new Char[] {'\t'});
            } else {
                throw new Exception(
                    "Only pipes and tabs can be used as delimiters");
            }

            try {
                double population =
                        Double.Parse(cols[1].Replace(",", ""));
                String name = cols[0].Trim();
                String country_name = name;
                country_name = country_name.Replace(" ", "_");
                country_name = country_name.Replace("'", "_");
                country_name = country_name.Replace(",", "_");
                country_name = country_name.Replace("(", "_");
                country_name = country_name.Replace(")", "_");
                Point point = new Point(
                        (float) (Double.Parse(cols[5].Replace(",", "")) / population),
                        (float) (Double.Parse(cols[7].Replace(",", "")) / population * 100));
                point.SetURIAction(
                        "http://pdfjet.com/country/" + country_name + ".txt");
                point.SetRadius(2.0f);

                if (point.GetX() > 1.25f) {
                    point.SetShape(Shape.RIGHT_ARROW);
                    point.SetStrokeColor(Color.black);
                } else if (point.GetY() > 80f) {
                    point.SetShape(Shape.UP_ARROW);
                    point.SetStrokeColor(Color.blue);
                } else if (name.Equals("France")) {
                    point.SetShape(Shape.MULTIPLY);
                    point.SetStrokeColor(Color.green);
                } else if (name.Equals("Canada")) {
                    point.SetShape(Shape.BOX);
                    point.SetStrokeColor(Color.orange);
                } else if (name.StartsWith("United States")) {
                    point.SetShape(Shape.STAR);
                    point.SetStrokeColor(Color.red);
                }

                countries.Add(new Country(name, point));
            } catch (Exception) {
            }
        }
        reader.Close();
        return countries;
    }

    // The slope and intercept of the ordinary least squares trend line of the points.
    private static float Slope(List<Point> points) {
        return Covar(points) / Devsq(points) * (points.Count - 1);
    }

    private static float Intercept(List<Point> points, float slope) {
        float[] mean = Mean(points);
        return mean[1] - slope * mean[0];
    }

    private static float[] Mean(List<Point> points) {
        float[] mean = new float[2];
        foreach (Point point in points) {
            mean[0] += point.GetX();
            mean[1] += point.GetY();
        }
        mean[0] /= points.Count;
        mean[1] /= points.Count;
        return mean;
    }

    private static float Covar(List<Point> points) {
        float covariance = 0f;
        float[] mean = Mean(points);
        foreach (Point point in points) {
            covariance += (point.GetX() - mean[0]) * (point.GetY() - mean[1]);
        }
        return covariance / (points.Count - 1);
    }

    private static float Devsq(List<Point> points) {
        float devsq = 0f;
        float[] mean = Mean(points);
        foreach (Point point in points) {
            devsq += (float) Math.Pow(point.GetX() - mean[0], 2);
        }
        return devsq;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_09();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_09 => {time1 - time0,4} ms");
    }
}   // End of Example_09.cs
