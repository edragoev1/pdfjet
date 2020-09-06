using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_18.cs
 *
 */
public class Example_18 {

    public Example_18() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_18.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        page.SetPenWidth(5f);
        page.SetBrushColor(0x353638);

        float x1 = 300f;
        float y1 = 300f;
        float r1 = 50f;
        float r2 = 50f;

        List<Point> path = new List<Point>();

        List<Point> segment1 = PDFjet.NET.Path.GetCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_00_03);
        List<Point> segment2 = PDFjet.NET.Path.GetCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_03_06);
        List<Point> segment3 = PDFjet.NET.Path.GetCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_06_09);

        path.AddRange(segment1);

        segment2.RemoveAt(0);
        path.AddRange(segment2);

        segment3.RemoveAt(0);
        path.AddRange(segment3);

        // page.DrawPath(path, Operation.FILL);
        page.DrawPath(path, Operation.STROKE);

        List<Point> segment4 = PDFjet.NET.Path.GetCurvePoints(x1, y1, r1, r2, Segment.CLOCKWISE_09_12);
        page.SetPenWidth(15f);
        page.SetPenColor(Color.red);
        page.DrawPath(segment4, Operation.STROKE);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_18();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_18 => " + (time1 - time0));
    }

}   // End of Example_18.cs
