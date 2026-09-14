using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_09.cs
 */
public class Example_09 {
    public Example_09() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_09.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.SetSize(8f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Chart chart = new Chart(f1, f2);
        chart.SetData(GetData("data/world-communications.txt", "|"));
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("World View - Communications");
        chart.SetXAxisTitle("Cell phones per capita");
        chart.SetYAxisTitle("Internet users % of the population");
        AddTrendLine(chart);
        chart.DrawOn(page);

        f1.SetSize(7f);
        f2.SetSize(7f);
        AddTableToChart(page, chart, f1, f2);

        pdf.Complete();
    }

    public void AddTrendLine(Chart chart) {
        List<Point> points = chart.GetData()[0];

        float m = Slope(points);
        float b = Intercept(points, m);

        List<Point> trendline = new List<Point>();
        float x = 0.0f;
        float y = m * x + b;
        Point p1 = new Point(x, y);
        p1.SetDrawPath(true);
        p1.SetStrokeColor(Color.blue);
        p1.SetShape(Shape.INVISIBLE);

        x = 1.5f;
        y = m * x + b;
        Point p2 = new Point(x, y);
        p2.SetShape(Shape.INVISIBLE);

        trendline.Add(p1);
        trendline.Add(p2);

        chart.GetData().Add(trendline);
    }

    public void AddTableToChart(
            Page page, Chart chart, Font f1, Font f2) {
        Table table = new Table();
        List<List<Cell>> tableData = new List<List<Cell>>();
        List<Point> points = chart.GetData()[0];
        for (int i = 0; i < points.Count; i++) {
            Point point = points[i];
            if (point.GetShape() != Shape.CIRCLE) {
                List<Cell> tableRow = new List<Cell>();

                point.SetRadius(2f);
                point.SetAlignment(Alignment.LEFT);

                Cell cell = new Cell(f2);
                cell.SetMarker(point);
                cell.SetText("");

                tableRow.Add(cell);

                cell = new Cell(f1);
                cell.SetText(point.GetText());
                tableRow.Add(cell);

                cell = new Cell(f2);
                cell.SetText(point.GetURIAction());
                tableRow.Add(cell);

                tableData.Add(tableRow);
            }
        }
        table.SetData(tableData);
        table.AutoAdjustColumnWidths();
        table.SetCellBorderWidth(0.2f);
        table.SetLocation(70f, 360f);
        table.SetColumnWidth(0, 9f);
        table.DrawOn(page);
    }

    public List<List<Point>> GetData(
            String fileName,
            String delimiter) {
        List<List<Point>> chartData = new List<List<Point>>();

        StreamReader reader =
                new StreamReader(fileName);
        List<Point> points = new List<Point>();
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

            Point point = new Point();
            try {
                double population =
                        Double.Parse(cols[1].Replace(",", ""));
                point.SetText(cols[0].Trim());
                String country_name = point.GetText();
                country_name = country_name.Replace(" ", "_");
                country_name = country_name.Replace("'", "_");
                country_name = country_name.Replace(",", "_");
                country_name = country_name.Replace("(", "_");
                country_name = country_name.Replace(")", "_");
                point.SetURIAction(
                        "http://pdfjet.com/country/" + country_name + ".txt");
                point.SetX((float) (Double.Parse(
                        cols[5].Replace(",", "")) / population));
                point.SetY((float) (Double.Parse(
                        cols[7].Replace(",", "")) / population * 100));

                point.SetRadius(2.0f);
                point.SetStrokeColor(Color.gray);

                if (point.GetX() > 1.25f) {
                    point.SetShape(Shape.RIGHT_ARROW);
                    point.SetStrokeColor(Color.black);
                } else if (point.GetY() > 80f) {
                    point.SetShape(Shape.UP_ARROW);
                    point.SetStrokeColor(Color.blue);
                } else if (point.GetText().Equals("France")) {
                    point.SetShape(Shape.MULTIPLY);
                    point.SetStrokeColor(Color.green);
                } else if (point.GetText().Equals("Canada")) {
                    point.SetShape(Shape.BOX);
                    point.SetStrokeColor(Color.orange);
                } else if (point.GetText().StartsWith("United States")) {
                    point.SetShape(Shape.STAR);
                    point.SetStrokeColor(Color.red);
                }

                points.Add(point);
            } catch (Exception) {
            }
        }
        reader.Close();
        chartData.Add(points);

        return chartData;
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
        Console.WriteLine("Example_09 => " + (time1 - time0) + " ms");
    }
}   // End of Example_09.cs
