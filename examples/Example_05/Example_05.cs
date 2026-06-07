using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_05.cs
 */
public class Example_05 {
    public Example_05() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_05.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1);
        text.SetLocation(300f, 300f);
        for (int i = 0; i < 360; i += 15) {
            text.SetTextDirection(i);
            text.SetUnderline(true);
            // text.SetStrikeLine(true);
            text.SetText("             Hello, World -- " + i + " degrees.");
            text.DrawOn(page);
        }

        text = new TextLine(f1, "WAVE AWAY");
        text.SetLocation(70f, 50f);
        text.DrawOn(page);

        f1.SetKernPairs(true);
        text.SetLocation(70f, 70f);
        text.DrawOn(page);

        f1.SetKernPairs(false);
        text.SetLocation(70f, 90f);
        text.DrawOn(page);

        f1.SetSize(8.0f);
        text = new TextLine(f1, "-- font.SetKernPairs(false);");
        text.SetLocation(150f, 50f);
        text.DrawOn(page);
        text.SetLocation(150f, 90f);
        text.DrawOn(page);
        text = new TextLine(f1, "-- font.SetKernPairs(true);");
        text.SetLocation(150f, 70f);
        text.DrawOn(page);

        Point point = new Point(300f, 300f);
        point.SetShape(Point.CIRCLE);
        point.SetFillColor(Color.blue);
        point.SetRadius(37.0);
        point.DrawOn(page);
        point.SetRadius(25.0);
        point.SetFillColor(Color.white);
        point.DrawOn(page);

        float[] arcPoints = (new Arc())
            .SetCenterXY(300f, 600f)
            .SetRadiusX(75f)
            .SetRadiusY(75f)
            .SetStartAngle(0f)
            .SetSweepDegreesCW(270f)
            // .SetSweepDegreesCCW(270f)
            // .SetScaleFactor(2f)
            // .SetRotateDegreesCW(90f)
            // .SetRotateDegreesCCW(90f)
            .SetStrokeWidth(5f)
            .SetStrokeColor(Color.blue)
            .DrawOn(page);

        (new Ellipse())
            .SetCenterXY(300f, 720f)
            .SetRadiusX(100f)
            .SetRadiusY(50f)
            .SetFillColor(Color.azure)
            .SetStrokeWidth(1.5f)
            .SetStrokeColor(Color.blue)
            .SetScaleFactor(0.5f)
            .SetRotateDegreesCW(45f)
            // .SetRotateDegreesCCW(45f)
            .DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_05();
        long time1 = sw.ElapsedMilliseconds;
        TextUtils.PrintDuration("Example_05", time0, time1);
    }
}   // End of Example_05.cs
