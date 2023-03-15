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

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetItalic(true);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetItalic(true);
        f2.SetSize(8f);

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

        List<Point> path1 = new List<Point>();

        Point point = new Point();
        point.SetDrawPath();
        point.SetX(15f);
        point.SetY(0f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.blue);
        point.SetLineWidth(25f);
        point.SetText(" Vertical");
        point.SetTextColor(Color.white);
        point.SetTextDirection(90);
        path1.Add(point);

        point = new Point();
        point.SetX(15f);
        point.SetY(45f);
        point.SetShape(Point.INVISIBLE);
        path1.Add(point);


        List<Point> path2 = new List<Point>();

        point = new Point();
        point.SetDrawPath();
        point.SetX(25f);
        point.SetY(0f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.green);
        point.SetLineWidth(25f);
        point.SetText(" Bar");
        point.SetTextColor(Color.white);
        point.SetTextDirection(90);
        path2.Add(point);

        point = new Point();
        point.SetX(25f);
        point.SetY(20f);
        point.SetShape(Point.INVISIBLE);
        path2.Add(point);


        List<Point> path3 = new List<Point>();

        point = new Point();
        point.SetDrawPath();
        point.SetX(35f);
        point.SetY(0f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.red);
        point.SetLineWidth(25f);
        point.SetText(" Chart");
        point.SetTextColor(Color.white);
        point.SetTextDirection(90);
        path3.Add(point);

        point = new Point();
        point.SetX(35f);
        point.SetY(31);
        point.SetShape(Point.INVISIBLE);
        path3.Add(point);


        List<Point> path4 = new List<Point>();

        point = new Point();
        point.SetDrawPath();
        point.SetX(45f);
        point.SetY(0f);
        point.SetShape(Point.INVISIBLE);
        point.SetColor(Color.gold);
        point.SetLineWidth(25f);
        point.SetText(" Example");
        point.SetTextColor(Color.black);
        point.SetTextDirection(90);
        path4.Add(point);

        point = new Point();
        point.SetX(45f);
        point.SetY(73);
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
        new Example_40();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_40 => " + (time1 - time0));
    }

}   // End of Example_40.cs
