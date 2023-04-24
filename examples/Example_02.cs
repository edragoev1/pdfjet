using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_02.cs
 *
 *  Draw the Canadian Maple Leaf using a Path object that contains both lines
 *  and curve segments. Every curve segment must have exactly 2 control points.
 */
public class Example_02 {
    public Example_02() {
        PDF pdf = new PDF(
                new BufferedStream(
                        new FileStream("Example_02.pdf", FileMode.Create)));

        Page page = new Page(pdf, Letter.PORTRAIT);

        PDFjet.NET.Path path = new PDFjet.NET.Path();
        path.SetLocation(100f, 50f);

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
        path.ScaleBy(4f);
        path.DrawOn(page);

        path.ScaleBy(4f);
        path.SetFillShape(false);
        float[] xy = path.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        page.SetPenColorCMYK(1.0f, 0.0f, 0.0f, 0.0f);
        page.SetPenWidth(5.0f);
        page.DrawLine(50f, 500f, 300f, 500f);

        page.SetPenColorCMYK(0.0f, 1.0f, 0.0f, 0.0f);
        page.SetPenWidth(5.0f);
        page.DrawLine(50f, 550f, 300f, 550f);

        page.SetPenColorCMYK(0.0f, 0.0f, 1.0f, 0.0f);
        page.SetPenWidth(5.0f);
        page.DrawLine(50f, 600f, 300f, 600f);

        page.SetPenColorCMYK(0.0f, 0.0f, 0.0f, 1.0f);
        page.SetPenWidth(5.0f);
        page.DrawLine(50f, 650f, 300f, 650f);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_02();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_02", time0, time1);
    }
}   // End of Example_02.cs
