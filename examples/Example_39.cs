using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_39.cs
 *  We will draw the American flag using Box, Line and Point objects.
 */
public class Example_39 {
    public Example_39() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_39.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetItalic(true);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f2.SetItalic(true);
        f2.SetSize(8f);

        Chart chart = new Chart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Horizontal Bar Chart Example");
        chart.SetXAxisTitle("");
        chart.SetYAxisTitle("");
        chart.SetData(GetData());
        chart.SetDrawYAxisLabels(false);
        chart.DrawOn(page);

        pdf.Complete();
    }

    public List<List<Point>> GetData() {
        List<List<Point>> chartData = new List<List<Point>>();

        List<Point> path1 = new List<Point>();
        Point point = new Point();
        point.SetDrawPath();
        point.SetX(0f);
        point.SetY(45f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.blue);
        point.SetLineWidth(20f);
        point.SetText(" Horizontal");
        point.SetTextColor(Color.white);
        path1.Add(point);

        point = new Point();
        point.SetX(35f);
        point.SetY(45f);
        point.SetShape(Point.INVISIBLE);
        path1.Add(point);

        List<Point> path2 = new List<Point>();
        point = new Point();
        point.SetDrawPath();
        point.SetX(0f);
        point.SetY(35f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.gold);
        point.SetLineWidth(20f);
        point.SetText(" Bar");
        point.SetTextColor(Color.black);
        path2.Add(point);

        point = new Point();
        point.SetX(22f);
        point.SetY(35f);
        point.SetShape(Point.INVISIBLE);
        path2.Add(point);

        List<Point> path3 = new List<Point>();
        point = new Point();
        point.SetDrawPath();
        point.SetX(0f);
        point.SetY(25f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.green);
        point.SetLineWidth(20f);
        point.SetText(" Chart");
        point.SetTextColor(Color.white);
        path3.Add(point);

        point = new Point();
        point.SetX(30f);
        point.SetY(25f);
        point.SetShape(Point.INVISIBLE);
        path3.Add(point);

        List<Point> path4 = new List<Point>();
        point = new Point();
        point.SetDrawPath();
        point.SetX(0f);
        point.SetY(15f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.red);
        point.SetLineWidth(20f);
        point.SetText(" Example");
        point.SetTextColor(Color.white);
        path4.Add(point);

        point = new Point();
        point.SetX(47f);
        point.SetY(15f);
        point.SetShape(Point.INVISIBLE);
        path4.Add(point);

        chartData.Add(path1);
        chartData.Add(path2);
        chartData.Add(path3);
        chartData.Add(path4);

        return chartData;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_39();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_39", time0, time1);
    }
}   // End of Example_39.cs
