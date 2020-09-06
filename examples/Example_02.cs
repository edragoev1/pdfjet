using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_02.cs
 *
 *  Draw the Canadian flag using a Path object that contains both lines
 *  and curve segments. Every curve segment must have exactly 2 control points.
 */
public class Example_02 {

    public Example_02() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_02.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Box flag = new Box(85.0f, 85.0f, 64.0f, 32.0f);

        PDFjet.NET.Path path = new PDFjet.NET.Path();
        path.Add(new Point(13.0f,  0.0f));
        path.Add(new Point(15.5f,  4.5f));

        path.Add(new Point(18.0f,  3.5f));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(20.5f,  7.5f));

        path.Add(new Point(21.0f,  9.5f));
        path.Add(new Point(25.0f,  9.0f));
        path.Add(new Point(24.0f, 13.0f));
        path.Add(new Point(25.5f, 14.0f));
        path.Add(new Point(19.0f, 19.0f));
        path.Add(new Point(20.0f, 21.5f));
        path.Add(new Point(13.5f, 20.5f));
        path.Add(new Point(13.5f, 27.0f));
        path.Add(new Point(12.5f, 27.0f));
        path.Add(new Point(12.5f, 20.5f));
        path.Add(new Point( 6.0f, 21.5f));
        path.Add(new Point( 7.0f, 19.0f));
        path.Add(new Point( 0.5f, 14.0f));
        path.Add(new Point( 2.0f, 13.0f));
        path.Add(new Point( 1.0f,  9.0f));
        path.Add(new Point( 5.0f,  9.5f));

        path.Add(new Point( 5.5f,  7.5f));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point( 8.0f,  3.5f));

        path.Add(new Point(10.5f,  4.5f));
        path.SetClosePath(true);
        path.SetColor(Color.red);
        path.SetFillShape(true);
        path.PlaceIn(flag, 19.0f, 3.0f);
        path.DrawOn(page);

        Box box = new Box();
        box.SetSize(16, 32);
        box.SetColor(Color.red);
        box.SetFillShape(true);
        box.PlaceIn(flag, 0.0, 0.0);
        box.DrawOn(page);
        box.PlaceIn(flag, 48.0, 0.0);
        box.DrawOn(page);

        path.ScaleBy(15.0);
        path.SetFillShape(false);
        float[] xy = path.DrawOn(page);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_02();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_02 => " + (time1 - time0));
    }

}   // End of Example_02.cs
