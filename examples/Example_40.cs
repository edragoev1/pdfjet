using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_40.cs
 *  We will draw the American flag using Box, Line and Point objects.
 */
public class Example_40 {
    public Example_40() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_40.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetItalic(true);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetItalic(true);
        f2.SetSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Chart chart = new Chart(f1, f2);
        chart.SetLocation(70f, 50f);
        chart.SetSize(500f, 300f);
        chart.SetTitle("Vertical Bar Chart Example");
        chart.SetXAxisTitle("Bar Chart");
        chart.SetYAxisTitle("Vertical");
        chart.SetData(GetData());
        chart.SetDrawXAxisLabels(false);
        chart.DrawOn(page);

        pdf.Complete();
    }

    public List<List<Point>> GetData() {
        List<List<Point>> chartData = new List<List<Point>>();

        float w = 14f;
        float x = 10f;
        float dx1 = 16f;
        float dx2 = 26f;
        AddVerticalBar(chartData, x, w, 45f, Color.green, " January", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 75f, Color.red, " January", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 65f, Color.green, " February", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 20f, Color.red, " February", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " March", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " March", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 45f, Color.green, " April", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 75f, Color.red, " April", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 65f, Color.green, " May", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 20f, Color.red, " May", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " June", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " June", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " July", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " July", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " August", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " August", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " Septemeber", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " Septemeber", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " October", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " October", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " November", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " November", Color.white);
        x += dx2;
        AddVerticalBar(chartData, x, w, 31f, Color.green, " December", Color.white);
        x += dx1;
        AddVerticalBar(chartData, x, w, 73f, Color.red, " December", Color.white);

        return chartData;
    }

    private void AddVerticalBar(
            List<List<Point>> chartData,
            float x,
            float w,
            float h,
            int color,
            String text,
            int textColor) {
        List<Point> path1 = new List<Point>();

        Point point = new Point();
        point.SetDrawPath();
        point.SetX(x);
        point.SetY(0f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(color);
        point.SetLineWidth(w);
        point.SetText(text);
        point.SetTextColor(textColor);
        point.SetTextDirection(90);
        path1.Add(point);

        point = new Point();
        point.SetX(x);
        point.SetY(h);
        point.SetShape(Point.INVISIBLE);
        path1.Add(point);

        chartData.Add(path1);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_40();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_40", time0, time1);
    }
}   // End of Example_40.cs
